# RimWorld Customization System — Styling Station API Reference

Reference document analyzing RimWorld's Ideology styling station system and how its patterns apply to the Customize Unique Weapons mod's design goals (see `DESIGN.md`).

---

## Table of Contents

1. [Styling Station Architecture Overview](#styling-station-architecture-overview)
2. [UI System — Dialog_StylingStation](#ui-system--dialog_stylingstation)
3. [Preview System](#preview-system)
4. [Job System & Queuing](#job-system--queuing)
5. [Float Menu Integration](#float-menu-integration)
6. [DLC Gating Analysis](#dlc-gating-analysis)
7. [Unique Weapon API Reference](#unique-weapon-api-reference)
8. [Design Mapping — Styling Station to Weapon Customization](#design-mapping--styling-station-to-weapon-customization)
9. [Implementation Architecture](#implementation-architecture)

---

## Styling Station Architecture Overview

The styling station system uses a **dialog → persistent state → think tree-driven job creation** pipeline. The dialog itself queues only ONE explicit job; subsequent operations are created autonomously by think tree nodes that detect pending state.

### Full Chain

```
Player right-clicks station
    → Building_StylingStation.GetFloatMenuOptions()
        → Queues JobDefOf.OpenStylingStationDialog

Pawn walks to station
    → JobDriver_OpenStylingStationDialog
        → Opens Dialog_StylingStation window

Player makes selections (hair, hair color, apparel color) and clicks Accept
    → Dialog stores pending state:
        pawn.style.SetupNextLookChangeData() → stores nextHairDef, nextBeardDef,
                                                nextFaceTattooDef, nextBodyTatooDef,
                                                nextHairColor
        ApplyApparelColors() → sets DesiredColor on each apparel CompColorable
    → Resets pawn's visual state to pre-dialog appearance
    → Queues ONE job: JobDefOf.UseStylingStation

Pawn walks to station
    → JobDriver_UseStylingStation
        → Toils_StyleChange.DoLookChange() — 300-tick wait with progress bar
        → Toils_StyleChange.FinalizeLookChange() — applies hair/beard/tattoo changes
    → Job completes, think tree runs

Think tree: JobGiver_DyeHair detects pawn.style.nextHairColor
    → Finds dye + station → creates JobDefOf.DyeHair
    → JobDriver_DyeHair: pick up 1 dye → go to station → 300-tick wait
        → consume dye → FinalizeHairColor()
    → Job completes, think tree runs

Think tree: JobGiver_OptimizeApparel detects AnyApparelNeedsRecoloring
    → Finds dye + station → creates JobDefOf.RecolorApparel
    → JobDriver_RecolorApparel: haul N dye → go to station → LOOP:
        → 1000-tick wait → consume 1 dye → Recolor() on apparel → next item
    → All recolors complete
```

### Class Responsibility Summary

| Class | Role |
|-------|------|
| `Building_StylingStation` | Building with float menu options ("Change Style", "Recolor Apparel") |
| `JobDriver_OpenStylingStationDialog` | Walk to station → open dialog window |
| `Dialog_StylingStation` | Full UI window — preview, select changes, confirm. Queues ONE job + sets persistent state. |
| `Pawn_StyleTracker` | Stores pending change data (`next*` fields) between dialog and think tree jobs |
| `JobDriver_UseStylingStation` | Walk to station → apply hair/beard/tattoo changes (300 tick wait) |
| `JobGiver_DyeHair` | Think tree node — detects `nextHairColor`, creates `DyeHair` job |
| `JobDriver_DyeHair` | Pick up 1 dye → station → 300-tick wait → consume dye → apply hair color |
| `JobGiver_OptimizeApparel` | Think tree node — detects `AnyApparelNeedsRecoloring`, creates `RecolorApparel` job |
| `JobDriver_RecolorApparel` | Haul N dye → station → loop (1000-tick wait → consume 1 dye → recolor 1 apparel) |
| `Toils_StyleChange` | Shared toils: `DoLookChange()` (wait + effects), `FinalizeLookChange()` (apply) |

---

## UI System — Dialog_StylingStation

### Window Properties

- **Size:** 950×750 pixels
- **Layout:** 30% left pane (pawn portrait), 70% right pane (tabbed content)
- **Behavior:** `forcePause = true`, `closeOnAccept = false`, `closeOnCancel = false`
- **Base class:** `Window`

### Tabs

Seven tabs via `StylingTab` enum:

| Tab | Condition |
|-----|-----------|
| `Hair` | Always shown |
| `Beard` | Shown if `pawn.style.CanWantBeard` or dev mode |
| `TattooFace` | Always shown |
| `TattooBody` | Always shown |
| `ApparelColor` | Always shown |
| `BodyType` | Dev mode only |
| `HeadType` | Dev mode only |

### Drawing Layout

```
┌──────────────────────────────────────────────────────────┐
│  Title                                                    │
├──────────────┬───────────────────────────────────────────┤
│              │  [Tab Bar: Hair | Beard | Tattoo | ...]    │
│  Portrait    │                                           │
│  (3 angles)  │  ┌──────────────────────────────────────┐ │
│  Front/Side/ │  │  Grid of style items (60px icons)     │ │
│  Back        │  │  with scroll view                     │ │
│              │  │                                       │ │
│  ☐ Headgear  │  │  Selected item highlighted             │ │
│  ☐ Clothes   │  │                                       │ │
│              │  └──────────────────────────────────────┘ │
├──────────────┴───────────────────────────────────────────┤
│  [Cancel]              [Reset]              [Accept]      │
└──────────────────────────────────────────────────────────┘
```

### Style Item Grid (`DrawStylingItemType<T>`)

Generic method that renders any `StyleItemDef` subclass in a grid:
- Filtered by `PawnStyleItemChooser.WantsToUseStyle()` (respects genes, ideo, gender)
- Sorted by `PawnStyleItemChooser.FrequencyFromGender`
- 60px icons, 10px spacing, inside a scroll view

### Bottom Buttons

Three buttons: Cancel (left), Reset (center), Accept (right).

**Accept flow:**
1. Compares current state to all `initial*` fields to detect changes.
2. If changes exist: calls `pawn.style.SetupNextLookChangeData(...)` with new selections.
3. Calls `Reset(resetColors: false)` to revert pawn's visual data (the job will apply the real changes).
4. Queues `JobDefOf.UseStylingStation` on the pawn — this is the ONLY explicitly queued job.
5. Calls `ApplyApparelColors()` — sets `DesiredColor` on apparel `CompColorable`. This does NOT queue a job; the think tree handles it later.
6. Closes the dialog.

---

## Preview System

### How Previewing Works

The styling station uses a **direct mutation with rollback** approach:

1. **Constructor** captures initial state into `initial*` fields:
   - `initialHairDef`, `initialBeardDef`, `initialFaceTattoo`, `initialBodyTattoo`
   - `initialSkinColor`, `initialBodyType`, `initialHeadType`
   - `apparelColors` dictionary (from all worn `CompColorable` apparel)

2. **During selection**, changes are applied **directly to the pawn's data**:
   - `pawn.story.hairDef = selectedHair` (immediate mutation)
   - This makes the portrait render correctly without a separate preview system.

3. **On cancel**, `Reset()` restores ALL initial values and calls `pawn.Drawer.renderer.SetAllGraphicsDirty()`.

4. **On accept**, `Reset(resetColors: false)` also restores initial values (since the job will apply the final changes later), but preserves apparel color assignments.

### Portrait Rendering

Three rotations rendered side by side:
- `Rot4(2)` — front facing (south)
- `Rot4(1)` — side facing (east)
- `Rot4(0)` — back facing (north)

Uses `PortraitsCache.Get()` with `stylingStation: true` parameter, plus `apparelColors` dictionary and `desiredHairColor` for apparel/hair color preview.

### Why This Pattern Won't Work for Weapon Customization

The styling station previews by mutating pawn fields, which is safe because:
- Only one dialog can be open (game is paused).
- The pawn is the thing being previewed.
- All fields are simple defs/colors that are cheap to swap.

For weapon customization, we'd need to preview **trait effects** (stat changes, name, color, graphic). We should NOT mutate the weapon's `CompUniqueWeapon` directly. Instead:

- **Weapon graphic/color:** Calculate the resulting color from selected traits (check `forcedColor`, fall back to random `ColorDef`) and render a preview texture.
- **Name:** Generate via `NameGenerator.GenerateName()` using the same grammar rules as `PostPostMake()`.
- **Stats:** Compute stat offsets/factors from the selected trait list.
- **Cost:** Calculate from selected traits (our custom logic).

This is a **computed preview** rather than a **mutate-and-rollback preview**.

---

## Job System & Queuing

### Styling Station: Three-Layer Job Architecture

The styling station uses three distinct mechanisms for its operations:

#### Layer 1: Explicit Job — Hair/Beard/Tattoo (UseStylingStation)

The dialog queues exactly one job. No resources consumed.

```
JobDriver_UseStylingStation:
  1. TryMakePreToilReservations → Reserve station (1 pawn)
  2. Toils_Goto.GotoThing(A, InteractionCell)
  3. Toils_StyleChange.DoLookChange(A) → WaitWith(300 ticks, progressBar, hair cutting sound)
  4. Toils_StyleChange.FinalizeLookChange() → Apply next* fields, make hair filth
```

#### Layer 2: Think Tree Job — Hair Dyeing (DyeHair)

After `UseStylingStation` completes, the think tree runs. `JobGiver_DyeHair` detects `pawn.style.nextHairColor` and creates a job:

```
JobGiver_DyeHair (ThinkNode):
  - Check: ModsConfig.IdeologyActive
  - Check: pawn.style.nextHairColor differs from current
  - Find nearest reachable StylingStation
  - Find nearest reachable Dye (1 unit)
  → Creates JobDefOf.DyeHair(station, dye)

JobDriver_DyeHair:
  1. Reserve station + interaction cell + 1 dye
  2. Toils_Goto → pick up dye (ClosestTouch)
  3. Toils_Haul.StartCarryThing → carry dye
  4. Toils_Goto → station interaction cell
  5. Toils_General.Wait(300 ticks, progressBar)
  6. Finalize: destroy 1 dye, call pawn.style.FinalizeHairColor()
```

#### Layer 3: Think Tree Job — Apparel Recoloring (RecolorApparel)

`JobGiver_OptimizeApparel` detects apparel with `DesiredColor` set and creates a **single job with internal loop**:

```
TryCreateRecolorJob():
  - Collects all worn apparel with DesiredColor.HasValue → queue B
  - Finds dye stacks, reserves 1 unit per apparel item → queue A
  - Sets styling station → target C
  → Creates JobDefOf.RecolorApparel with target queues

JobDriver_RecolorApparel:
  1. Reserve station + interaction cell + dye stacks (partial stack reservation)
  2. CollectIngredientsToils → haul ALL dye from queue A to station
  3. Toils_Goto → station interaction cell
  4. LOOP START:
     a. Toils_General.WaitWith(1000 ticks, progressBar)
     b. Toils_JobTransforms.ExtractNextTargetFromQueue(B) → next apparel
     c. Toils_General.Do:
        - Destroy 1 dye from placedThings
        - Call CompColorable.Recolor() on the apparel
        - Set job.count = remaining queue B count
     d. Toils_Jump.JumpIfHaveTargetInQueue(B, step 4a) → loop back
  5. All apparel recolored, job ends
```

**This internal loop pattern is the closest vanilla analogue to our weapon customization job.** All resources (dye) are hauled in one trip, then consumed individually as each apparel item is processed.

### Interruption Behavior

#### Styling Station: Persistent State Survives Interruption

The styling station's pending state (`nextHairColor` on `Pawn_StyleTracker`, `DesiredColor` on apparel) is **not** stored in the job queue. It persists on the pawn/apparel objects across interruptions. This means:

- If the pawn is drafted mid-chain: `StopAll()` kills the current job and clears the job queue. But think tree-driven jobs were never in the queue — they're created on-demand.
- After undrafting: the think tree runs again, detects the surviving pending state, and recreates the dye/recolor jobs.
- **The remaining operations are deferred, not permanently cancelled.** They resume automatically once the pawn is free.

This is by design for the styling station's low-cost operations (1 dye each), but would be problematic for high-value weapon customization resources (see [Implementation Architecture](#implementation-architecture)).

#### RecolorApparel Internal Loop: Partial Completion

If `RecolorApparel` is interrupted mid-loop:
- Completed recolors persist (dye consumed, apparel recolored).
- Remaining apparel still has `DesiredColor` set.
- Unconsumed dye sits at the workbench (hauled but not yet used).
- The think tree creates a new `RecolorApparel` job later to finish.

### RimWorld Job Queue System

For context, RimWorld's explicit job queue works as follows:

**`Pawn_JobTracker`** holds:
- `curJob` / `curDriver` — the currently executing job
- `jobQueue` (`JobQueue`) — FIFO list of `QueuedJob` entries

**`TryTakeOrderedJob(job, tag, requestQueueing)`:**
- Without `requestQueueing`: clears queue, enqueues at front, interrupts current job → new job runs immediately.
- With `requestQueueing` (or Shift held): enqueues at back, does NOT interrupt current job → runs after current + all queued jobs.

**Job advancement:** When a job ends with `Succeeded`, `EndCurrentJob` calls `TryFindAndStartJob`, which walks the pawn's think tree. A `ThinkNode_QueuedJob` node dequeues the next job from the queue.

**`StopAll()`** (called on drafting, downed, mental break): kills current job AND **clears the entire queue**. All queued jobs and their reservations are released.

---

## Float Menu Integration

### Styling Station Approach

`Building_StylingStation` overrides `GetFloatMenuOptions(Pawn)` directly:

```csharp
public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
{
    foreach (var opt in base.GetFloatMenuOptions(selPawn))
        yield return opt;

    if (!ModLister.IdeologyInstalled)
        yield break;

    if (!selPawn.CanReach(this, PathEndMode.OnCell, Danger.Deadly))
    {
        yield return new FloatMenuOption("CannotUseReason"...("NoPath"...), null);
    }
    else
    {
        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("ChangeStyle"..., delegate {
                selPawn.jobs.TryTakeOrderedJob(
                    JobMaker.MakeJob(JobDefOf.OpenStylingStationDialog, this),
                    JobTag.Misc);
            }), selPawn, this);
    }
    // Also "Recolor Apparel" option...
}
```

### RimWorld 1.6 Float Menu Provider System

RimWorld 1.6 uses a **provider-based architecture** for float menus:

1. `FloatMenuMakerMap` instantiates all `FloatMenuOptionProvider` subclasses at startup (via reflection).
2. On right-click, it creates a `FloatMenuContext` and iterates all providers.
3. `FloatMenuOptionProvider_FromThing` delegates to `Thing.GetFloatMenuOptions(Pawn)`.
4. `ThingWithComps.GetFloatMenuOptions()` iterates all comps and calls `CompFloatMenuOptions(Pawn)`.

### Recommended Approach for Our Mod

**Custom `FloatMenuOptionProvider` (preferred)**

```csharp
public class FloatMenuOptionProvider_CustomizeWeapon : FloatMenuOptionProvider
{
    protected override bool Drafted => false;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing,
        FloatMenuContext context)
    {
        if (clickedThing is not Building_WorkTable workTable)
            return null;
        // Check if workTable.def is a valid workbench for this weapon's tech level
        // Check if pawn has a customizable weapon equipped
        // Check research prerequisites
        // Return "Customize [weapon name]" option
    }
}
```

RimWorld auto-discovers all `FloatMenuOptionProvider` subclasses — no Harmony patch needed for registration. This is the cleanest approach.

**Alternative: Harmony postfix on `Building.GetFloatMenuOptions`**

Workbenches (`Building_WorkTable`) do NOT override `GetFloatMenuOptions` — they inherit from `Building`. A Harmony postfix on `Building.GetFloatMenuOptions` (filtered to `Building_WorkTable` instances) would work but is less targeted.

---

## DLC Gating Analysis

### Ideology Gating — What's Behind the Wall

**Every** aspect of the styling station is gated behind the Ideology DLC:

| Component | Check | Method |
|-----------|-------|--------|
| `Building_StylingStation.GetFloatMenuOptions` | `ModLister.IdeologyInstalled` | Returns no options if false |
| `JobDriver_OpenStylingStationDialog.MakeNewToils` | `ModLister.CheckIdeology(...)` | Yields no toils if false |
| `JobDriver_UseStylingStation.MakeNewToils` | `ModLister.CheckIdeology(...)` | Yields no toils if false |
| `JobDriver_UseStylingStationAutomatic.MakeNewToils` | `ModLister.CheckIdeology(...)` | Yields no toils if false |
| `JobGiver_UseStylingStationAutomatic.TryGiveJob` | `ModsConfig.IdeologyActive` | Returns null if false |
| `Dialog_StylingStation.PostOpen` | `ModLister.CheckIdeology(...)` | Closes dialog if false |
| `JobDriver_DyeHair.MakeNewToils` | `ModLister.CheckIdeology(...)` | Yields no toils if false |
| `JobGiver_DyeHair.TryGiveJob` | `ModsConfig.IdeologyActive` | Returns null if false |
| `JobDriver_RecolorApparel.MakeNewToils` | `ModLister.CheckIdeology(...)` | Yields no toils if false |
| `JobGiver_OptimizeApparel.TryCreateRecolorJob` | `ModLister.CheckIdeology(...)` | Returns false if not installed |

### DLC Check Patterns Explained

RimWorld uses two distinct DLC check patterns:

**`ModLister.CheckIdeology(string featureName)`** — Checks if the DLC is **installed** (on disk). If not, logs an error: *"[featureName] is an Ideology-specific game system..."* and returns `false`. Used as a **developer guard** to catch code that accidentally runs without the DLC. The error message is a debug aid, not a player-facing message.

**`ModsConfig.IdeologyActive`** — Checks if the DLC is **active** (enabled in mod list). Used for runtime branching in gameplay code. A DLC can be installed but not active.

```csharp
// ModLister — is it on disk?
public static bool IdeologyInstalled => ideologyInstalled;
public static bool OdysseyInstalled => odysseyInstalled;

// ModsConfig — is it enabled?
public static bool IdeologyActive => ideologyActive;
public static bool OdysseyActive => odysseyActive;

// ModLister.CheckIdeology logs an error if not installed, returns bool
public static bool CheckIdeology(string featureName)
    => CheckDLC(IdeologyInstalled, featureName, "an Ideology", "IdeologyInstalled");

// ModLister.CheckOdyssey — same pattern for Odyssey
public static bool CheckOdyssey(string featureName)
    => CheckDLC(OdysseyInstalled, featureName, "an Odyssey", "OdysseyInstalled");
```

### Critical Finding: No Ideology Dependency Needed

The styling station's **code** (dialog, jobs, toils, building, think tree nodes) is entirely Ideology-gated and cannot be reused without Ideology. However, the **patterns** (dialog → job execution, internal loop, resource hauling and consumption) are generic software patterns that can be reimplemented using Core APIs.

**What our mod reuses from the styling station:** Nothing directly. We implement the same architectural patterns independently.

**What our mod depends on:**
- **Odyssey DLC** (required) — provides `CompUniqueWeapon`, `WeaponTraitDef`, `WeaponTraitWorker`, unique weapon generation, trait validation (`CanAddTrait`), name/color generation.
- **Harmony** (required) — for patching float menus onto workbenches.
- **Core RimWorld** — `Window`, `JobDriver`, `Toil`, `FloatMenuOption`, `FloatMenuOptionProvider`, `JobDriver_DoBill.CollectIngredientsToils`, all standard modding infrastructure.

### Odyssey DLC Checks on Weapon Code

The unique weapon system has its own Odyssey guards:

| Method | Check |
|--------|-------|
| `CompUniqueWeapon.PostPostMake` | `ModLister.CheckOdyssey(...)` |
| `CompUniqueWeapon.CanAddTrait` | `ModLister.CheckOdyssey(...)` |
| `CompUniqueWeapon.AddTrait` | `ModLister.CheckOdyssey(...)` |
| `CompUniqueWeapon.Notify_Equipped` | `ModLister.CheckOdyssey(...)` |
| `WeaponTraitDef.Worker` getter | `ModLister.CheckRoyaltyOrOdyssey(...)` |
| `ThingSetMaker_UniqueWeapon.CanGenerateSub` | `ModsConfig.OdysseyActive` |

Since our mod already requires Odyssey, these guards will always pass.

---

## Unique Weapon API Reference

### CompUniqueWeapon — Core API

```csharp
public class CompUniqueWeapon : ThingComp
{
    // --- Fields (serialized) ---
    private List<WeaponTraitDef> traits;   // Scribe_Collections, LookMode.Def
    private ColorDef color;                 // Scribe_Defs
    private string name;                    // Scribe_Values

    // --- Fields (transient) ---
    private CompStyleable styleable;
    private bool? ignoreAccuracyMaluses;    // Cached from traits

    // --- Constants ---
    private static readonly IntRange NumTraitsRange = new IntRange(1, 3);

    // --- Properties ---
    public CompProperties_UniqueWeapon Props;
    public List<WeaponTraitDef> TraitsListForReading => traits;
    public bool IgnoreAccuracyMaluses { get; }  // Checks all traits for ignoresAccuracyMaluses

    // --- Trait Management ---
    public bool CanAddTrait(WeaponTraitDef trait);  // Validation (see below)
    public void AddTrait(WeaponTraitDef traitDef);  // Simple list append
    // NOTE: No RemoveTrait() method exists in vanilla!

    // --- Setup ---
    public void Setup(bool fromSave);  // Wires up ability props from traits

    // --- Display ---
    public override Color? ForceColor() => color?.color;
    public override string TransformLabel(string label);  // Returns name if set
    public override float GetStatOffset(StatDef stat);
    public override float GetStatFactor(StatDef stat);
}
```

### CompProperties_UniqueWeapon

```csharp
public class CompProperties_UniqueWeapon : CompProperties
{
    public List<WeaponCategoryDef> weaponCategories;  // Which trait categories this weapon accepts
    public List<string> namerLabels;                   // Labels used in name generation grammar
}
```

### Trait Validation — CanAddTrait Logic

```csharp
public bool CanAddTrait(WeaponTraitDef trait)
{
    // 1. Odyssey DLC must be installed
    if (!ModLister.CheckOdyssey("Unique Weapons")) return false;

    // 2. Trait's weapon category must match weapon's accepted categories
    if (!Props.weaponCategories.Contains(trait.weaponCategory)) return false;

    // 3. If weapon has NO traits yet, the trait must be allowed as a sole trait
    if (TraitsListForReading.Empty() && !trait.canGenerateAlone) return false;

    // 4. No overlap with existing traits (same def OR shared exclusion tags)
    foreach (var existing in traits)
        if (trait.Overlaps(existing)) return false;

    return true;
}
```

**Missing from CanAddTrait:** There is no max-traits check (the 3-trait limit is only enforced during `InitializeTraits()` via the `NumTraitsRange` loop). Our mod should add this check:

```csharp
// Additional check needed for our mod:
if (traits.Count >= 3) return false;  // NumTraitsRange.max
```

### WeaponTraitDef — Full Schema

```csharp
public class WeaponTraitDef : Def
{
    // --- Classification ---
    public Type workerClass = typeof(WeaponTraitWorker);
    public WeaponCategoryDef weaponCategory;
    public List<string> exclusionTags;         // Tags for overlap/conflict checking
    public float commonality;                   // Generation weight
    public bool canGenerateAlone = true;        // Can be the only trait on a weapon?

    // --- Combat Modifications ---
    public DamageDef damageDefOverride;
    public List<ExtraDamage> extraDamages;
    public List<StatModifier> statOffsets;
    public List<StatModifier> statFactors;
    public List<StatModifier> equippedStatOffsets;
    public float marketValueOffset;
    public float burstShotSpeedMultiplier = 1f;
    public float burstShotCountMultiplier = 1f;
    public float additionalStoppingPower;
    public bool ignoresAccuracyMaluses;

    // --- Appearance ---
    public ColorDef forcedColor;               // Overrides random weapon color
    public List<string> traitAdjectives;       // Words used in name generation

    // --- Pawn Effects ---
    public List<HediffDef> equippedHediffs;    // Applied when equipped
    public List<HediffDef> bondedHediffs;      // Applied when bonded
    public ThoughtDef bondedThought;
    public ThoughtDef killThought;
    public bool neverBond;

    // --- Abilities ---
    public CompProperties_EquippableAbilityReloadable abilityProps;  // Grenade launcher, EMP, etc.

    // --- Worker ---
    public WeaponTraitWorker Worker { get; }   // Lazy-initialized from workerClass
}
```

### Trait Overlap Detection

```csharp
public bool Overlaps(WeaponTraitDef other)
{
    if (other == this) return true;  // Same trait def
    if (exclusionTags.NullOrEmpty() || other.exclusionTags.NullOrEmpty())
        return false;
    // Any shared exclusion tag = overlap
    return exclusionTags.Any(x => other.exclusionTags.Contains(x));
}
```

### Name & Color Generation (from PostPostMake)

```csharp
// Color selection:
// 1. Pick random ColorDef where colorType == ColorType.Weapon && randomlyPickable
// 2. Iterate traits — last trait with forcedColor wins

// Name generation:
// Grammar rules: weapon_type (from Props.namerLabels), color (from ColorDef.label),
//                trait_adjective (from traits' traitAdjectives lists)
// Plus random pawn name data (for possessive names like "Kira's")
// RulePack: RulePackDefOf.NamerUniqueWeapon
// Generated via NameGenerator.GenerateName(request, ...)
```

### What Vanilla Does NOT Provide

The following operations **do not exist** in vanilla and must be implemented by our mod:

1. **RemoveTrait** — No method to remove a trait. We must implement: remove from `traits` list, clean up ability props, recalculate cached values.
2. **RegenerateNameAndColor** — `PostPostMake` generates name/color but has an implicit guard (only runs during initial creation). The companion mod BetterTradersGuild uses **reflection** to set the private `name` and `color` fields after trait modification. Our mod should either:
   - Use reflection (as BetterTradersGuild does), or
   - Use Harmony to add a `RegenerateNameAndColor()` method via extension, or
   - Directly access private fields via Harmony's `AccessTools.Field`.
3. **Max trait count validation** — `CanAddTrait` does not check the 3-trait limit. We must add this.
4. **Revert to non-unique** — No mechanism to convert a unique weapon back to its base variant when all traits are removed.

---

## Design Mapping — Styling Station to Weapon Customization

### Pattern Comparison

| Aspect | Styling Station | Weapon Customization (Our Mod) |
|--------|----------------|-------------------------------|
| **Trigger** | Right-click station → "Change Style" | Right-click workbench → "Customize [weapon]" |
| **Dialog opens via** | `JobDriver_OpenStylingStationDialog` | Custom `JobDriver_OpenCustomizationDialog` |
| **Preview method** | Direct pawn field mutation + rollback | Computed preview (calculate stats/name/color from trait selection) |
| **Job structure** | Single job for style changes + think tree jobs for dye/recolor | Single job with internal loop for all trait changes |
| **Internal loop** | `RecolorApparel`: haul N dye → loop (work → consume 1 → recolor 1) | `CustomizeWeapon`: unequip → haul resources → loop (work → consume → apply trait) → reequip |
| **Resource hauling** | `CollectIngredientsToils` hauls all dye in one trip | Same pattern — haul all resources (plasteel, components, gold, etc.) in one trip |
| **Resource cost** | 1 dye per color change (homogeneous) | Variable per trait, multiple resource types (heterogeneous) |
| **Cost timing** | Consumed per-item in loop iteration | Consumed per-trait in loop iteration |
| **Interruption (mid-loop)** | Completed recolors persist, pending resume via think tree | Completed trait changes persist, **pending operations cancelled** |
| **State persistence** | Pending state survives interruption (think tree recreates jobs) | **No persistent state** — interruption is a clean cancellation |
| **DLC requirement** | Ideology | Odyssey (already required) + Harmony |

### Where We Follow the Styling Station Pattern

1. **Float menu → "open dialog" job → dialog window** — Same flow.
2. **Dialog with preview and confirm/cancel** — Same UX pattern.
3. **Single job with internal loop** — Directly mirrors `RecolorApparel`'s architecture. All resources hauled in one trip, then consumed individually per operation.
4. **Resource hauling via `CollectIngredientsToils`** — Reuses the same vanilla ingredient collection system.

### Where We Diverge

1. **No persistent pending state** — The styling station stores `nextHairColor` and `DesiredColor` on pawn/apparel, so operations resume after interruption. We deliberately skip this so that interruption is a clear, clean cancellation. For low-cost operations (1 dye), silent auto-resume is fine. For high-value resources (100 gold), players need predictable cancellation behavior — the resources should not "mysteriously disappear" if the pawn resumes the job while the player's attention is elsewhere.
2. **Weapon unequip/reequip wrapping** — The styling station doesn't modify items; it modifies the pawn. We modify the weapon itself, so it must be physically placed at the workbench (unequipped + hauled) before work begins, and reequipped after all modifications are complete.
3. **Computed preview instead of mutate-and-rollback** — We calculate what the weapon would look like without modifying it.
4. **No dependency on Ideology** — We build equivalent patterns from Core APIs.
5. **Workbench targeting** — We add options to existing workbenches rather than requiring a custom building.

---

## Implementation Architecture

### Chosen Approach: Single Job with Internal Loop

Modeled directly on `JobDriver_RecolorApparel`, our customization job uses a **single job with target queues** that hauls all resources in one trip, then loops through trait operations at the workbench. The job is bookended by weapon unequip (before hauling) and reequip (after all work is complete).

**No persistent pending state.** If the job is interrupted, completed trait changes persist on the weapon, but remaining operations are simply cancelled. Unconsumed resources remain at the workbench (hauled but not used) and can be reclaimed by the player. This provides clear, predictable cancellation behavior for high-value operations.

### Proposed Class Structure

```
Source/1.6/
├── Core/
│   └── ModInitializer.cs                          # Harmony bootstrap (existing)
├── UI/
│   └── Dialog_WeaponCustomization.cs               # Main customization dialog (Window subclass)
├── Jobs/
│   ├── JobDriver_OpenCustomizationDialog.cs         # Walk to bench → open dialog
│   └── JobDriver_CustomizeWeapon.cs                 # Unequip → haul → loop (work → apply) → reequip
├── FloatMenu/
│   └── FloatMenuOptionProvider_CustomizeWeapon.cs   # Provider for workbench float menus
├── Utilities/
│   ├── WeaponCustomizationUtility.cs                # Trait validation, cost calculation, name generation
│   ├── UniqueWeaponPairUtility.cs                   # Dynamic base↔unique weapon pair detection
│   └── CraftabilityUtility.cs                       # Dynamic craftability + research prerequisite detection
└── Properties/
    └── AssemblyInfo.cs                              # (existing)
```

### Job Flow — Complete Lifecycle

```
1. Player right-clicks workbench with customizable weapon equipped
2. FloatMenuOptionProvider_CustomizeWeapon offers "Customize [weapon name]"
3. Player selects option → queues JobDriver_OpenCustomizationDialog
4. Pawn walks to workbench → dialog opens (game pauses)
5. Player selects trait changes, sees computed preview (name, color, stats, cost)
6. Player clicks Confirm:
   → Dialog creates ONE JobDefOf.CustomizeWeapon job with target queues:
      Target A: workbench
      Target B: weapon (for unequip/reequip tracking)
      Queue A: resource stacks to haul (plasteel, components, gold, etc.)
      Queue B: trait operations [(AddTrait, Lightweight), (AddTrait, GoldInlay)]
      + Precomputed final name/color stored in job custom data
   → Dialog closes, game unpauses

7. JobDriver_CustomizeWeapon executes:

   a. UNEQUIP: Pawn unequips weapon, places it at/near the workbench
      (weapon becomes a physical item at the bench, freeing the equipment slot)

   b. HAUL RESOURCES: CollectIngredientsToils → pawn hauls all resources
      from queue A to the workbench (one trip for all resource types)

   c. GO TO BENCH: Toils_Goto → workbench interaction cell

   d. WORK LOOP:
      i.   WaitWith(N ticks, progressBar) — work time for this operation
      ii.  ExtractNextTargetFromQueue(B) — next trait operation
      iii. Do:
           - Consume resources for THIS operation from placedThings
           - If adding first trait to non-unique weapon: transform to unique variant
           - Add/remove trait on CompUniqueWeapon
           - Update weapon name/color (intermediate state or final)
      iv.  JumpIfHaveTargetInQueue(B, step d.i) — loop back for next operation

   e. FINALIZE: Apply precomputed final name and color to weapon
      (ensures the name/color shown in the dialog preview is what the player gets)

   f. REEQUIP: Pawn picks up the weapon and equips it

8. Job complete — weapon is in its final customized state, pawn has it equipped
```

### Interruption Scenarios

| Interrupted During | Weapon State | Resources | Behavior |
|-------------------|-------------|-----------|----------|
| Unequip / hauling resources | Unchanged (or unequipped at bench) | At bench or on ground | Job cancelled. Weapon at bench, resources nearby. Player reclaims manually. |
| Work loop — after trait 1 applied, before trait 2 | Trait 1 applied, partially customized | Trait 1 resources consumed, trait 2 resources at bench | Job cancelled. Weapon has trait 1 only. Remaining resources at bench. |
| Reequip step | Fully customized | All consumed | Job cancelled. Weapon at bench in final state. Pawn can manually pick up. |

In all cases, the principle is: **completed operations are permanent, remaining operations are cleanly cancelled, no resources are lost without a corresponding trait change.**

### Resource Handling Detail

Resources are hauled using `JobDriver_DoBill.CollectIngredientsToils` — the same vanilla system used by `RecolorApparel` and crafting bills. This handles:
- Multi-stack collection (e.g., 50 plasteel from one stockpile, 3 components from another)
- Partial stack splitting
- Carrying items to the workbench

In the work loop, each iteration consumes only the resources for that specific trait operation:

```csharp
// Pseudocode for loop finalize action (per iteration):
TraitOperation op = currentOperation;
foreach (var cost in op.resourceCosts)
{
    // Find and destroy the required amount from placedThings at the bench
    DestroyResourceFromPlacedThings(cost.thingDef, cost.count);
}
ApplyTraitChange(weapon, op);
```

### Float Menu Provider Pattern

```csharp
public class FloatMenuOptionProvider_CustomizeWeapon : FloatMenuOptionProvider
{
    protected override bool Drafted => false;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    protected override FloatMenuOption GetSingleOptionFor(
        Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing is not Building_WorkTable workTable)
            return null;

        Pawn pawn = context.FirstSelectedPawn;
        if (pawn?.equipment?.Primary == null)
            return null;

        Thing weapon = pawn.equipment.Primary;

        // Check: Is this workbench valid for this weapon's tech level?
        // Check: Does the player have the required research?
        // Check: Is the weapon customizable (has unique variant, meets craftability gate)?
        // Check: Can the pawn reach the workbench?

        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                "Customize " + weapon.LabelShort,
                delegate {
                    pawn.jobs.TryTakeOrderedJob(
                        JobMaker.MakeJob(CUW_JobDefOf.OpenCustomizationDialog,
                            workTable, weapon),
                        JobTag.Misc);
                }),
            pawn, workTable);
    }
}
```

**Key decision:** The pawn must have the weapon equipped (or in inventory) when right-clicking. The weapon is passed as `targetB` in the initial dialog-opening job. The float menu option should also appear when the weapon is NOT unique but CAN become unique (adding traits to a non-unique weapon that has a unique variant).

### Example Walkthrough: Charge Rifle + Lightweight + Gold Inlay

```
1. Player selects a pawn wielding a non-unique Charge Rifle
2. Right-clicks a valid workbench → "Customize Charge Rifle" appears
3. Pawn walks to workbench → dialog opens (game paused)

4. Dialog shows:
   - Left pane: Charge Rifle graphic (non-unique), name "Charge Rifle"
   - Right pane: Available traits filtered for this weapon
   - Cost summary: (empty)

5. Player clicks "Lightweight" trait:
   - Preview updates: name → "Quick-draw Charge Rifle" (random from adjectives)
   - Preview graphic → unique charge rifle texture (random, no forced color)
   - Cost line: "Lightweight: 50× plasteel, 3× advanced component"

6. Player clicks "Gold Inlay" trait:
   - Preview updates: name → "Golden Quick-draw Charge Rifle"
   - Preview graphic → gold shader color applied (forcedColor from Gold Inlay)
   - Cost line: "Gold Inlay: 100× gold"
   - Total cost: 50× plasteel, 3× advanced component, 100× gold

7. Player clicks Confirm → dialog creates CustomizeWeapon job → closes

8. Pawn unequips Charge Rifle, places it at the workbench
9. Pawn hauls 50 plasteel, 3 components, 100 gold to the workbench
10. Pawn goes to workbench interaction cell

11. Work iteration 1 (N ticks, progress bar):
    → Consumes 50 plasteel + 3 components
    → Transforms Charge Rifle → Unique Charge Rifle with Lightweight trait
    → Intermediate name applied

12. Work iteration 2 (N ticks, progress bar):
    → Consumes 100 gold
    → Adds Gold Inlay trait
    → Final name "Golden Quick-draw Charge Rifle" + gold shader color applied

13. Pawn picks up and equips the "Golden Quick-draw Charge Rifle"
```

If interrupted after step 11 but before step 12: the weapon is a Unique Charge Rifle with Lightweight only. 100 gold sits at the workbench unconsumed. The Gold Inlay operation is cancelled. The player can re-open the customization dialog to add Gold Inlay again later.
