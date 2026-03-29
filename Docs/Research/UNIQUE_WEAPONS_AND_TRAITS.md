# Unique Weapons & Weapon Traits — API Deep Dive

Reference document for the Customize Unique Weapons mod. Covers the full internal structure of unique weapons, weapon traits, naming, colors, graphics, and base-to-unique pairing — with an emphasis on what we can offer players control over.

**Scope:** This document covers **Odyssey unique weapons** (`CompUniqueWeapon`) only. Royalty bladelink/persona weapons (`CompBladelinkWeapon`) are categorically excluded from our mod — see [Bladelink Weapon Exclusion](#bladelink-weapon-exclusion) for the technical rationale.

---

## Table of Contents

1. [Bladelink Weapon Exclusion](#bladelink-weapon-exclusion)
2. [CompUniqueWeapon — Internal Structure](#compuniqueweapon--internal-structure)
3. [WeaponTraitDef — Full Schema & Catalog](#weapontraitdef--full-schema--catalog)
4. [WeaponCategoryDef System](#weaponcategorydef-system)
5. [Base-to-Unique Weapon Pairing](#base-to-unique-weapon-pairing)
6. [Name System](#name-system)
7. [Color System](#color-system)
8. [Graphic & Texture System](#graphic--texture-system)
9. [WeaponTraitWorker System](#weapontraitworker-system)
10. [Ability System (Trait-Granted Abilities)](#ability-system-trait-granted-abilities)
11. [Quality System](#quality-system)
12. [CompArt Integration](#compart-integration)
13. [Additional Customizable Properties](#additional-customizable-properties)
14. [Obstacles & Challenges](#obstacles--challenges)
15. [Summary of Customization Opportunities](#summary-of-customization-opportunities)

---

## Bladelink Weapon Exclusion

### Two Parallel Systems Sharing One Trait Type

RimWorld has two independent weapon trait systems that happen to share the same `WeaponTraitDef` class:

| Aspect | Odyssey Unique Weapons | Royalty Bladelink Weapons |
|--------|----------------------|------------------------|
| **Component** | `CompUniqueWeapon` (extends `ThingComp`) | `CompBladelinkWeapon` (extends `CompBiocodable`) |
| **Weapon types** | Ranged only (13 weapons) | Melee only (3 weapons) |
| **Trait count** | 1–3 | 1–2 |
| **Categories** | 15 categories (`Ranged`, `Gun`, `Pistol`, etc.) | 1 category (`BladeLink`) |
| **Trait nature** | Physical modifications (sights, ammo, attachments) | Psychic properties (persona AI traits) |
| **Category validation** | `Props.weaponCategories.Contains(trait.weaponCategory)` | Hardcoded `trait.weaponCategory == WeaponCategoryDefOf.BladeLink` |
| **DLC guard** | `ModLister.CheckOdyssey()` | `ModLister.CheckRoyalty()` |
| **Bonding** | None | Built-in (extends `CompBiocodable`) |

The category sets are completely disjoint — no Odyssey category overlaps with `BladeLink`, and no vanilla weapon has both comps.

### Why Excluded

Odyssey weapon traits describe physical modifications — upgraded sights, ammo types, barrel attachments, material inlays — exactly the kind of adjustments a skilled crafter could make at a workbench with the right research and resources. Royalty's bladelink traits are psychically granted by the weapon's AI persona. Modifying an AI's personality at a fabrication table is not a believable player action.

### How Excluded

Our mod excludes bladelink weapons at multiple levels:

1. **Weapon detection:** All our code keys off `CompUniqueWeapon` presence. Bladelink weapons only have `CompBladelinkWeapon` and will never be detected.

2. **Trait filtering:** When enumerating available traits for a weapon (e.g. in the customization dialog), the category check `Props.weaponCategories.Contains(trait.weaponCategory)` naturally excludes `BladeLink` traits since no unique weapon lists that category.

3. **Defensive filter:** As an additional safeguard (and to avoid displaying irrelevant traits in our UI if a modded weapon somehow had both comps), any code that iterates all `WeaponTraitDef`s from `DefDatabase` should skip traits where `weaponCategory == WeaponCategoryDefOf.BladeLink`:

   ```csharp
   var odysseyTraits = DefDatabase<WeaponTraitDef>.AllDefs
       .Where(t => t.weaponCategory != WeaponCategoryDefOf.BladeLink);
   ```

---

## CompUniqueWeapon — Internal Structure

**Namespace:** `RimWorld.CompUniqueWeapon` (extends `ThingComp`)

### Serialized Fields

| Field | Type | Scribe Method | Save Key | Notes |
|-------|------|---------------|----------|-------|
| `traits` | `List<WeaponTraitDef>` | `Scribe_Collections.Look` | `"traits"` | `LookMode.Def` — saves defNames |
| `color` | `ColorDef` | `Scribe_Defs.Look` | `"color"` | Reference to a `ColorDef` |
| `name` | `string` | `Scribe_Values.Look` | `"name"` | Plain string, freely settable |

### Transient Fields

| Field | Type | Notes |
|-------|------|-------|
| `styleable` | `CompStyleable` | Declared but never assigned in CompUniqueWeapon code — accessed via `parent.compStyleable` instead |
| `ignoreAccuracyMaluses` | `bool?` | Lazy-cached from traits; cleared on first access |

### Constants

```csharp
static readonly IntRange NumTraitsRange = new IntRange(1, 3);  // 1-3 traits per weapon
```

### Key Properties

| Property | Returns | Notes |
|----------|---------|-------|
| `Props` | `CompProperties_UniqueWeapon` | Weapon categories + namer labels |
| `TraitsListForReading` | `List<WeaponTraitDef>` | **Direct reference** to the internal list (not a copy) |
| `IgnoreAccuracyMaluses` | `bool` | Cached check across all traits |

### Lifecycle Methods

#### `PostPostMake()` — Initial Generation (Called Once)

1. Checks `ModLister.CheckOdyssey`
2. Calls `InitializeTraits()` — randomly picks 1–3 traits using weighted selection
3. Forces quality to `QualityGenerator.Super` via `CompQuality`
4. Picks a random `ColorDef` where `colorType == ColorType.Weapon && randomlyPickable`
5. Iterates traits — last trait with `forcedColor != null` overrides the color
6. Collects `traitAdjectives` from all traits
7. Builds a `GrammarRequest` with rules: `weapon_type`, `color`, `trait_adjective`, plus `ANYPAWN` data
8. Generates name via `NameGenerator.GenerateName()`, strips tags
9. Sets `CompArt.Title` to the generated name
10. Calls `Setup(fromSave: false)`

#### `Setup(bool fromSave)` — Ability Wiring

For each trait with non-null `abilityProps`: finds the weapon's `CompEquippableAbilityReloadable`, replaces its `props` with the trait's `abilityProps`, and (if not from save) calls `Notify_PropsChanged()`.

Only the **first** trait with `abilityProps` takes effect — at most one ability per weapon, enforced by the `Ability` exclusion tag.

#### `PostExposeData()` — Save/Load

Serializes all three fields. On `PostLoadInit`: reconstitutes a null traits list as empty, then calls `Setup(fromSave: true)`.

### Trait Management

#### `CanAddTrait(WeaponTraitDef trait)` — Validation

```
1. ModLister.CheckOdyssey must pass
2. trait.weaponCategory must be in Props.weaponCategories
3. If traits list is EMPTY and trait.canGenerateAlone == false → rejected
4. No existing trait may Overlap with the new trait
```

**Missing from vanilla `CanAddTrait`:** There is no max-traits check. The 3-trait limit is only enforced during `InitializeTraits()` via the `NumTraitsRange` loop. Our mod must add this check.

#### `AddTrait(WeaponTraitDef traitDef)` — Simple List Append

Just appends to the `traits` list. No side effects — does not call `Setup()`, does not update name/color, does not fire equip events.

#### No `RemoveTrait()` Method

Vanilla provides no way to remove traits. Our mod must implement this, including:
- Removing from the `traits` list
- Calling `Worker.Notify_EquipmentLost()` if weapon is currently equipped (removes equippedHediffs)
- Resetting `CompEquippableAbilityReloadable` props if the removed trait had `abilityProps`
- Clearing the `ignoreAccuracyMaluses` cache (set to `null`)
- Updating name, color, and CompArt.Title as needed

### Stat System

| Method | Behavior |
|--------|----------|
| `GetStatOffset(StatDef)` | Sums `statOffsets` from all traits |
| `GetStatFactor(StatDef)` | Multiplies `statFactors` from all traits (starting at 1.0) |
| `GetStatsExplanation(StatDef)` | Formatted breakdown per trait |

### Equipment Events

| Method | Behavior |
|--------|----------|
| `Notify_Equipped(Pawn)` | Calls `Worker.Notify_Equipped(pawn)` on each trait |
| `Notify_EquipmentLost(Pawn)` | Calls `Worker.Notify_EquipmentLost(pawn)` on each trait |

### Label and Color

| Method | Behavior |
|--------|----------|
| `TransformLabel(string label)` | If ideology precept label exists → returns original label. Otherwise if `name` is non-empty → returns `name`. Otherwise returns original label. |
| `ForceColor()` | Returns `color?.color` (nullable `Color`). First non-null `ForceColor()` from any comp wins in `ThingWithComps.DrawColor`. |

### CompProperties_UniqueWeapon

```csharp
public class CompProperties_UniqueWeapon : CompProperties
{
    public List<WeaponCategoryDef> weaponCategories;  // Which trait categories this weapon accepts
    [MustTranslate] public List<string> namerLabels;  // Weapon-type synonyms for name generation
}
```

---

## WeaponTraitDef — Full Schema & Catalog

### Schema

```csharp
public class WeaponTraitDef : Def
{
    // --- Classification ---
    public Type workerClass = typeof(WeaponTraitWorker);
    public WeaponCategoryDef weaponCategory;
    public List<string> exclusionTags;
    public float commonality;
    public bool canGenerateAlone = true;

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
    public ColorDef forcedColor;
    [MustTranslate] public List<string> traitAdjectives;

    // --- Pawn Effects ---
    public List<HediffDef> equippedHediffs;
    public List<HediffDef> bondedHediffs;
    public ThoughtDef bondedThought;
    public ThoughtDef killThought;
    public bool neverBond;

    // --- Abilities ---
    public CompProperties_EquippableAbilityReloadable abilityProps;
}
```

### Overlap Detection

```csharp
public bool Overlaps(WeaponTraitDef other)
{
    if (other == this) return true;  // Same def
    if (exclusionTags.NullOrEmpty() || other.exclusionTags.NullOrEmpty()) return false;
    return exclusionTags.Any(x => other.exclusionTags.Contains(x));  // Any shared tag
}
```

### Complete Trait Catalog (Odyssey DLC — 37 Traits)

#### Gun Category (3)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `QuickReload` | Cooldown −20% | — | true |
| `AimAssistance` | All accuracy +20%, ignores accuracy maluses | — | true |
| `CustomGrip` | Touch/short accuracy +10%, beauty +2 | — | **false** |

#### Pistol Category (2)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `StabilizerBrace` | All accuracy +20% | — | true |
| `PulseCharger` | All accuracy −20%, damage +0.6, armor pen +0.3 | — | true |

#### BulletFiring Category (6)

| defName | Effects | Exclusion Tags | forcedColor | canGenerateAlone |
|---------|---------|----------------|-------------|------------------|
| `ToxRounds` | Damage → `Bullet_TraitTox` | AmmoType, Color | UniqueWeapon_Tox | true |
| `PiercingRounds` | Armor pen +0.2 | AmmoType | — | true |
| `IncendiaryRounds` | Damage → `Bullet_TraitIncendiary` | AmmoType, Color | UniqueWeapon_Fire | true |
| `EMPRounds` | Extra EMP damage (4) | AmmoType, Color | UniqueWeapon_EMP | true |
| `HollowPointRounds` | Damage +0.2 | AmmoType | — | true |
| `HighPowerRounds` | Damage +0.3, armor pen +0.15, all accuracy −10% | AmmoType | — | true |

#### PelletFiring Category (3)

| defName | Effects | Exclusion Tags | forcedColor | canGenerateAlone |
|---------|---------|----------------|-------------|------------------|
| `ToxPellets` | Damage → `Bullet_TraitTox` | AmmoType, Color | UniqueWeapon_Tox | true |
| `IncendiaryPellets` | Damage → `Bullet_TraitIncendiary` | AmmoType, Color | UniqueWeapon_Fire | true |
| `BirdshotPellets` | Close accuracy +, far accuracy − | AmmoType | — | **false** |

#### Bow Category (5)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `PiercingArrows` | Armor pen +0.3 | AmmoType | true |
| `ParalyticArrows` | Damage → `Nerve` | AmmoType | true |
| `BroadheadArrows` | Damage +0.3 | AmmoType | true |
| `LightweightArrows` | Range +0.3, damage −0.1 | AmmoType | true |
| `ReinforcedLimbs` | Range +0.15 | — | true |

#### Rifle Category (1)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `ExtendedBarrel` | Range +0.2 | Barrel | true |

#### Sighted Category (2)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `ImprovedSights` | Medium/long accuracy +20% | Sights | true |
| `ShoddySights` | Medium/long accuracy −20% | Sights | **false** |

#### Scoped Category (2)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `PrecisionScope` | Warmup −25% | Scope | true |
| `RangefinderScope` | Range +0.1 | Scope | true |

#### Shotgun Category (1)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `ShortenedBarrel` | Warmup −20%, range −20% | Barrel | true |

#### BurstFire Category (2)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `RapidFire` | Burst shot speed ×2 | — | true |
| `ExtendedMagazine` | Burst shot count ×1.5 | — | true |

#### PulseCharge Category (2)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `ChargeCapacitor` | Armor pen +0.2, damage +0.35 | — | true |
| `EMPPulser` | Ability: `EMPPulse` (cooldown-based, no ammo) | Ability | true |

#### BeamWeapon Category (1)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `FrequencyAmplifier` | Range +0.3, damage +0.5, cooldown ×1.5 | — | true |

#### LowStoppingPower Category (1)

| defName | Effects | Exclusion Tags | canGenerateAlone |
|---------|---------|----------------|------------------|
| `OversizedRounds` | Stopping power +2, damage +0.1 | AmmoType | true |

#### Ranged Category — Generic (6)

| defName | Effects | Exclusion Tags | forcedColor | canGenerateAlone |
|---------|---------|----------------|-------------|------------------|
| `Ornamental` | Damage −0.15, beauty +5 | Appearance | — | **false** |
| `Ugly` | Beauty −4, value ×0.8 | Appearance | — | **false** |
| `Lightweight` | Warmup −0.2, mass ×0.75 | Weight | — | true |
| `Cumbersome` | Warmup +0.2, value ×0.8 | Weight | — | **false** |
| `GoldInlay` | Beauty +20, value ×2 | Color | UniqueWeapon_Gold | **false** |
| `JadeInlay` | Beauty +10, value ×1.4 | Color | UniqueWeapon_Jade | **false** |

#### Attachable Category (5)

| defName | Effects | Exclusion Tags | MayRequire | canGenerateAlone |
|---------|---------|----------------|------------|------------------|
| `GrenadeLauncher` | Ability: `LaunchFragGrenade`, ammo=25 Steel | Attachment, Ability | — | true |
| `EMPLauncher` | Ability: `LaunchEMPShell`, ammo=15 Steel, 2 charges | Attachment, Ability | — | true |
| `SmokeLauncher` | Ability: `LaunchSmokeShell`, ammo=15 Steel, 2 charges | Attachment, Ability | — | true |
| `IncendiaryLauncher` | Ability: `LaunchIncendiaryShell`, ammo=15 Chemfuel | Attachment, Ability | — | true |
| `BioferriteBurner` | Ability: `HellcatBurner`, ammo=10 Bioferrite | Attachment, Ability | Anomaly | true |

### Exclusion Tag Summary

| Tag | Purpose | Traits Using It |
|-----|---------|-----------------|
| `AmmoType` | Prevents multiple ammo modifications | ToxRounds, PiercingRounds, IncendiaryRounds, EMPRounds, HollowPointRounds, HighPowerRounds, ToxPellets, IncendiaryPellets, BirdshotPellets, PiercingArrows, ParalyticArrows, BroadheadArrows, LightweightArrows, OversizedRounds |
| `Color` | Prevents multiple forced colors | ToxRounds, IncendiaryRounds, EMPRounds, ToxPellets, IncendiaryPellets, GoldInlay, JadeInlay |
| `Barrel` | One barrel modification | ExtendedBarrel, ShortenedBarrel |
| `Sights` | One sight modification | ImprovedSights, ShoddySights |
| `Scope` | One scope modification | PrecisionScope, RangefinderScope |
| `Weight` | One weight modification | Lightweight, Cumbersome |
| `Appearance` | One appearance modification | Ornamental, Ugly |
| `Attachment` | One physical attachment | GrenadeLauncher, EMPLauncher, SmokeLauncher, IncendiaryLauncher, BioferriteBurner |
| `Ability` | One granted ability | EMPPulser, GrenadeLauncher, EMPLauncher, SmokeLauncher, IncendiaryLauncher, BioferriteBurner |

### Traits with `canGenerateAlone = false` (8)

`CustomGrip`, `BirdshotPellets`, `ShoddySights`, `Ornamental`, `Ugly`, `Cumbersome`, `GoldInlay`, `JadeInlay`

These are all either negative traits or purely cosmetic/value traits. They require at least one other trait to exist on the weapon.

**Implication for customization:** When removing traits, we must validate that the remaining trait set doesn't leave a `canGenerateAlone=false` trait as the sole trait. When adding a `canGenerateAlone=false` trait to an empty weapon, it must be accompanied by at least one other trait.

---

## WeaponCategoryDef System

`WeaponCategoryDef` is an empty class extending `Def` — purely a label/tag for grouping traits.

### All Categories (Odyssey — 15)

`Ranged`, `BulletFiring`, `Bow`, `Rifle`, `Sighted`, `Scoped`, `Shotgun`, `BurstFire`, `LowStoppingPower`, `Gun`, `PelletFiring`, `PulseCharge`, `Pistol`, `BeamWeapon`, `Attachable`

Royalty adds one additional category: `BladeLink` (for persona weapons — separate system, not our concern).

### Unique Weapon → Category Mapping

Each unique weapon's `CompProperties_UniqueWeapon.weaponCategories` determines which traits can be added. The categories act as a tag-based filter: a trait is eligible for a weapon if the trait's `weaponCategory` is in the weapon's `weaponCategories` list.

| Unique Weapon | Accepted Categories |
|--------------|-------------------|
| `Gun_Revolver_Unique` | Ranged, BulletFiring, Gun, Pistol, Sighted |
| `Gun_BoltActionRifle_Unique` | Ranged, BulletFiring, Gun, Rifle, Sighted |
| `Gun_ChainShotgun_Unique` | Ranged, PelletFiring, Gun, Shotgun, BurstFire, Attachable |
| `Gun_HeavySMG_Unique` | Ranged, BulletFiring, Gun, Sighted, BurstFire, LowStoppingPower, Attachable |
| `Gun_LMG_Unique` | Ranged, BulletFiring, Gun, Sighted, BurstFire, Attachable |
| `Gun_AssaultRifle_Unique` | Ranged, BulletFiring, Gun, Sighted, Rifle, BurstFire, LowStoppingPower, Attachable |
| `Gun_SniperRifle_Unique` | Ranged, BulletFiring, Gun, Scoped, Rifle |
| `Gun_Minigun_Unique` | Ranged, BulletFiring, Gun, BurstFire, LowStoppingPower |
| `Gun_ChargeRifle_Unique` | Ranged, Gun, Sighted, Rifle, BurstFire, LowStoppingPower, PulseCharge, Attachable |
| `Gun_ChargeLance_Unique` | Ranged, Gun, PulseCharge |
| `Gun_HellcatRifle_Unique` | Ranged, BulletFiring, Sighted, Rifle, BurstFire, LowStoppingPower, Gun |
| `Bow_Great_Unique` | Ranged, Bow |
| `Gun_BeamRepeater_Unique` | Ranged, Gun, BurstFire, BeamWeapon |

### Dynamic Trait Filtering

Our mod does NOT hard-code which traits apply to which weapons. At runtime:

```csharp
// For a given weapon and candidate trait:
bool eligible = comp.Props.weaponCategories.Contains(trait.weaponCategory);
```

This means any mod that adds new `WeaponTraitDef`s with appropriate categories, or new unique weapons with appropriate category lists, will automatically work.

---

## Base-to-Unique Weapon Pairing

### How Vanilla Establishes the Relationship

There is **no explicit `baseDef` field** on unique weapons. The relationship is established through:

1. **XML `ParentName` inheritance** — e.g., `Gun_Revolver_Unique` has `ParentName="Gun_Revolver"`. This is how stats, verbs, tools, etc. are inherited. **Not accessible at runtime** (XML inheritance is resolved at def-loading time).

2. **`descriptionHyperlinks`** — Each unique weapon explicitly lists its base weapon: `<descriptionHyperlinks><ThingDef>Gun_Revolver</ThingDef></descriptionHyperlinks>`. **Accessible at runtime** via `thingDef.descriptionHyperlinks`.

3. **Naming convention** — `{BaseDefName}_Unique` suffix. Consistent across all 13 vanilla defs.

### All Vanilla Pairs

| Unique defName | Base defName | Tech Level | MayRequire |
|----------------|-------------|------------|------------|
| `Gun_Revolver_Unique` | `Gun_Revolver` | Industrial | — |
| `Gun_BoltActionRifle_Unique` | `Gun_BoltActionRifle` | Industrial | — |
| `Gun_ChainShotgun_Unique` | `Gun_ChainShotgun` | Industrial | — |
| `Gun_HeavySMG_Unique` | `Gun_HeavySMG` | Industrial | — |
| `Gun_LMG_Unique` | `Gun_LMG` | Industrial | — |
| `Gun_AssaultRifle_Unique` | `Gun_AssaultRifle` | Industrial | — |
| `Gun_SniperRifle_Unique` | `Gun_SniperRifle` | Industrial | — |
| `Gun_Minigun_Unique` | `Gun_Minigun` | Industrial | — |
| `Bow_Great_Unique` | `Bow_Great` | Medieval | — |
| `Gun_ChargeRifle_Unique` | `Gun_ChargeRifle` | Spacer | — |
| `Gun_ChargeLance_Unique` | `Gun_ChargeLance` | Spacer | — |
| `Gun_HellcatRifle_Unique` | `Gun_HellcatRifle` | Industrial | Anomaly |
| `Gun_BeamRepeater_Unique` | `Gun_BeamRepeater` | Spacer | — |

### Programmatic Detection Strategy

**Primary method — `descriptionHyperlinks`:**

```csharp
// Given a unique weapon ThingDef, find its base:
ThingDef FindBaseWeapon(ThingDef uniqueDef)
{
    if (uniqueDef.descriptionHyperlinks == null) return null;
    foreach (var link in uniqueDef.descriptionHyperlinks)
    {
        if (link is ThingDef linked && linked.IsWeapon && !linked.HasComp(typeof(CompUniqueWeapon)))
            return linked;
    }
    return null;
}

// Given a base weapon ThingDef, find its unique variant:
ThingDef FindUniqueVariant(ThingDef baseDef)
{
    foreach (var def in DefDatabase<ThingDef>.AllDefs)
    {
        if (!def.HasComp(typeof(CompUniqueWeapon))) continue;
        if (def.descriptionHyperlinks?.OfType<ThingDef>().Contains(baseDef) == true)
            return def;
    }
    return null;
}
```

**Fallback method — naming convention** (`{BaseDefName}_Unique`):

If `descriptionHyperlinks` fails but we know a def has `CompUniqueWeapon` (or we're searching for a unique variant of a known base def), fall back to the naming convention:

```csharp
ThingDef FindUniqueByConvention(ThingDef baseDef)
{
    return DefDatabase<ThingDef>.GetNamedSilentFail(baseDef.defName + "_Unique");
}

ThingDef FindBaseByConvention(ThingDef uniqueDef)
{
    if (uniqueDef.defName.EndsWith("_Unique"))
    {
        string baseName = uniqueDef.defName.Substring(0, uniqueDef.defName.Length - "_Unique".Length);
        return DefDatabase<ThingDef>.GetNamedSilentFail(baseName);
    }
    return null;
}
```

**Recommendation:** Use `descriptionHyperlinks` as primary (works for modded weapons that may not follow naming conventions), with the naming convention as a secondary fallback for unique defs where hyperlinks are missing or unhelpful. Cache the mapping at startup in a `Dictionary<ThingDef, ThingDef>` (both directions) for O(1) lookups.

### Key Structural Difference: comps Inherit="False"

All unique weapon defs use `<comps Inherit="False">` — they completely replace the base weapon's comp list. This is necessary because unique weapons need `CompEquippableAbilityReloadable` instead of the base weapon's `CompEquippable`.

**Implication for our mod:** When converting a non-unique weapon to its unique variant, we're effectively replacing the `Thing` entirely (different ThingDef). The weapon instance must be destroyed and recreated as the unique variant. This is unavoidable because the comp lists differ fundamentally.

---

## Name System

### Storage

The name is a `private string name` field on `CompUniqueWeapon`, serialized via `Scribe_Values.Look(ref name, "name")`. It's stored in save files as a plain XML element.

**The name is just a string. There is no integrity check, hash, seed, or regeneration mechanism.**

### Generation (PostPostMake — Called Once)

1. Collect `traitAdjectives` from all traits into a single pool
2. Pick one random adjective from the pool → inject as `trait_adjective` rule
3. Pick one random entry from `Props.namerLabels` → inject as `weapon_type` rule
4. Get `color.label` → inject as `color` rule
5. Generate a random pawn via `TaleData_Pawn.GenerateRandom(humanLike: true)` → inject as `ANYPAWN_*` rules
6. Include `RulePackDefOf.NamerUniqueWeapon` rule pack
7. Call `NameGenerator.GenerateName(request, null, appendNumberIfNameUsed: false, "r_weapon_name")`
8. Strip XML tags from result
9. Also set `CompArt.Title` to the same name

### Grammar Rules (NamerUniqueWeapon)

Root keyword: `r_weapon_name`

**Name patterns (with generation weights):**

| Pattern | Weight |
|---------|--------|
| `[weapon_adjective] [weapon_noun]` | p=2 (most common) |
| `The [weapon_type] of [badass_concept]` | p=0.5 |
| `The [weapon_adjective] [weapon_type]` | p=0.5 |
| `[badass_concept]'s [weapon_type]` | p=0.5 |
| `[ANYPAWN_nameIndef]'s [weapon_noun]` | p=0.3 |
| `[ANYPAWN_nameIndef]'s [weapon_adjective] [weapon_noun]` | p=0.3 |
| `The [badass_concept] of [ANYPAWN_nameIndef]` | p=0.3 |

**Sub-rules:**
- `weapon_noun` → `[weapon_type]` (p=2), `[badass_noun]`, `[badass_concept]`
- `weapon_adjective` → `[trait_adjective]` (p=2), `[badass_adjective]`

**Word lists (from XML):**
- **badass_adjective:** grim, eternal, night, grave, exceptional, deadly, infamous, custom, thunderous, cursed, great
- **badass_noun:** reaper, end, howl, tempest, phantom, widow, titan, juggernaut, serpent, anvil, killer, lover, beast, fang, arm, horn
- **badass_concept:** justice, revenge, vengeance, pain, agony, torment, legend, delight, regret, pride, conceit, honor, fury, reckoning, serenity, love, laughter

**Note on the `color` rule:** The code injects a `color` rule (set to the `ColorDef.label`, e.g. "red", "gold", "silver"), and no **explicit** pattern in the NamerUniqueWeapon rule pack references `[color]` directly. However, `GrammarResolver` always loads `GlobalUtility` rules, which may provide indirect resolution paths. Empirical testing with the BetterTradersGuild `SilverInlay` trait (which has `forcedColor` label "silver" and `traitAdjectives` including "silver"/"silvered") produces names like "Silvered Silver Charge Rifle" — confirming the color label does participate in name generation through some resolution path. The exact mechanism (possibly through GlobalUtility includes) warrants further investigation if we need precise control over name generation, but for practical purposes: **both `trait_adjective` and `color` contribute to generated names, and having overlapping words between them causes redundant-sounding names.**

### Nothing Regenerates the Name After Creation

- `PostPostMake()` generates the name exactly once at Thing creation time
- `PostExposeData()` saves/loads it — no regeneration logic
- `TransformLabel()` reads the name — never triggers regeneration
- No other code in the game writes to the `name` field

### Can We Rename Weapons?

**Yes, unambiguously.** The name is a simple private string. To change it:

1. Access via `Harmony.AccessTools.Field(typeof(CompUniqueWeapon), "name")` or `Traverse.Create(comp).Field("name")`
2. Set to any string value
3. Also update `CompArt.Title` (public setter: `comp.Title = newName`) to keep art inspection in sync
4. Call `thing.Notify_ColorChanged()` or equivalent to refresh UI caches if needed

### Can We Reproduce Name Generation?

**Yes.** All APIs used in `PostPostMake` are public:

```csharp
// Reproduce name generation for a "randomize" button:
List<string> adjectives = traits.SelectMany(t => t.traitAdjectives).ToList();
GrammarRequest request = default;
request.Rules.Add(new Rule_String("weapon_type", Props.namerLabels.RandomElement()));
request.Rules.Add(new Rule_String("color", color.label));
if (adjectives.Any())
    request.Rules.Add(new Rule_String("trait_adjective", adjectives.RandomElement()));
TaleData_Pawn.GenerateRandom(humanLike: true).AddRules(request, "ANYPAWN");
request.Includes.Add(RulePackDefOf.NamerUniqueWeapon);
string newName = NameGenerator.GenerateName(request, null, false, "r_weapon_name").StripTags();
```

### Can We Allow Free-Text Renaming?

**Yes.** Since the name is just a string with no validation, we can set it to any player-typed value. The only consideration is that `TransformLabel()` returns the name as the weapon's complete label — so a blank name would fall back to the ThingDef's default label.

---

## Color System

### Storage

The color is stored as a `private ColorDef color` field on `CompUniqueWeapon`, serialized via `Scribe_Defs.Look(ref color, "color")`.

### How Color Reaches the Renderer

```
CompUniqueWeapon.ForceColor() → returns color?.color
    ↓
ThingWithComps.DrawColor getter → iterates comps, first non-null ForceColor() wins
    ↓
GraphicData.GraphicColoredFor(Thing) → if DrawColor differs from base, creates tinted graphic
    ↓
CutoutComplex shader → uses mask texture (_m suffix) to apply color to specific regions
```

### ColorDef Catalog (Odyssey — Weapon Type)

All defined with `colorType = ColorType.Weapon`.

#### Randomly Pickable Colors (assigned during generation)

| defName | Label | RGB | displayInStylingStationUI |
|---------|-------|-----|---------------------------|
| `UniqueWeapon_Red` | red | (118, 49, 57) | true |
| `UniqueWeapon_MutedRed` | muted red | (101, 67, 71) | true |
| `UniqueWeapon_Purple` | purple | (107, 86, 107) | true |
| `UniqueWeapon_MutedPurple` | muted purple | (80, 64, 80) | true |
| `UniqueWeapon_Green` | green | (65, 82, 60) | true |
| `UniqueWeapon_MutedGreen` | muted green | (86, 93, 76) | true |
| `UniqueWeapon_IceBlue` | ice blue | (140, 148, 174) | true |
| `UniqueWeapon_MutedBlue` | muted blue | (67, 81, 101) | true |
| `UniqueWeapon_DarkBrown` | dark brown | (90, 69, 38) | true |

#### Non-Styling-Station Colors (randomly pickable but not shown in styling UI)

| defName | Label | RGB |
|---------|-------|-----|
| `UniqueWeapon_White` | white | (166, 166, 166) |
| `UniqueWeapon_Cream` | cream | (195, 192, 176) |
| `UniqueWeapon_Limestone` | limestone | (158, 153, 135) |
| `UniqueWeapon_Sandstone` | sandstone | (126, 104, 94) |
| `UniqueWeapon_Gray` | gray | (100, 100, 100) |
| `UniqueWeapon_Black` | black | (60, 60, 60) |

#### Forced Colors (not randomly picked — trait-assigned only)

| defName | Label | RGB | Used By |
|---------|-------|-----|---------|
| `UniqueWeapon_Gold` | gold | (207, 157, 2) | GoldInlay |
| `UniqueWeapon_Jade` | jade | (103, 143, 80) | JadeInlay |
| `UniqueWeapon_Tox` | tox | (89, 105, 62) | ToxRounds, ToxPellets |
| `UniqueWeapon_Fire` | fire | (167, 96, 39) | IncendiaryRounds, IncendiaryPellets |
| `UniqueWeapon_EMP` | EMP | (74, 100, 138) | EMPRounds |

### How forcedColor Interacts with the color Field

In `PostPostMake()`, the `color` field is **overwritten in-place** by trait forced colors:

```csharp
// 1. Always pick a random color first
color = DefDatabase<ColorDef>.AllDefs
    .Where(c => c.colorType == ColorType.Weapon && c.randomlyPickable)
    .RandomElement();

// 2. Iterate traits — last forcedColor overwrites the field entirely
foreach (WeaponTraitDef item in TraitsListForReading)
{
    if (item.forcedColor != null)
        color = item.forcedColor;  // Random color is permanently lost
}
```

And `ForceColor()` does NOT check traits at runtime — it just returns the stored field:

```csharp
public override Color? ForceColor() => color?.color;
```

**Critical implication:** The original random color is destroyed when a forcedColor trait overwrites it. There is no way to "revert" to the original color after removing a color-forcing trait — we must pick a new color (randomly or via player choice).

### Can We Change the Color?

**Yes.** Set the private `color` field to any `ColorDef` via reflection/Traverse, then call `thing.Notify_ColorChanged()` to clear the graphic cache and force a re-render.

### Color Change Constraints

When traits with `forcedColor` are present, the last such trait in the list determines the color. Our customization dialog should:

1. Allow free color selection from all weapon `ColorDef`s **only when no trait forces a color**
2. When a trait with `forcedColor` is present, lock the color to that trait's value and show it as non-editable
3. When removing a color-forcing trait, prompt the player to choose a new color (since the original random color was overwritten and is unrecoverable) or auto-assign a new random one

### Can We Offer a Color Picker?

**Yes.** We can enumerate all available weapon colors:

```csharp
var weaponColors = DefDatabase<ColorDef>.AllDefs
    .Where(c => c.colorType == ColorType.Weapon && c.randomlyPickable)
    .ToList();
```

We could also include the non-randomly-pickable colors (White, Cream, etc.) as additional options, since there's no technical restriction — just a generation-time filter.

---

## Graphic & Texture System

### How Unique Weapons Use Multiple Textures

All unique weapons use:
```xml
<graphicClass>Graphic_Random</graphicClass>
<shaderType>CutoutComplex</shaderType>
```

`Graphic_Random` extends `Graphic_Collection`. On initialization, it scans the `texPath` folder for all textures, groups them by prefix (text before the last `_`), and creates sub-graphics.

### Texture Variant Selection

```csharp
// Graphic_Random.SubGraphicFor(Thing thing):
int num = thing.OverrideGraphicIndex ?? thing.thingIDNumber;
return subGraphics[num % subGraphics.Length];
```

By default, `thingIDNumber` (a unique stable integer assigned at Thing creation) deterministically selects which texture variant is used. This is **not** recalculated — once a weapon exists, it always gets the same texture.

### Can We Change the Texture Variant?

**Yes, via `Thing.overrideGraphicIndex`** — a public `int?` field, already saved/loaded by `Thing.ExposeData()` via `Scribe_Values.Look`.

```csharp
thing.overrideGraphicIndex = desiredIndex;
thing.Notify_ColorChanged();  // Clears graphicInt cache, forces re-resolve
```

When `overrideGraphicIndex` is non-null, `Graphic_Random.SubGraphicFor()` uses it instead of `thingIDNumber`. This field persists across saves automatically.

### Can We Offer a Texture Selector?

**Yes.** The full implementation path:

1. **Count available variants:**
   ```csharp
   Graphic baseGraphic = thing.DefaultGraphic;
   // Unwrap Graphic_RandomRotated if present:
   if (baseGraphic is Graphic_RandomRotated rotated)
       baseGraphic = rotated.SubGraphic;
   if (baseGraphic is Graphic_Random random)
       int count = random.SubGraphicsCount;
   ```

2. **Get preview for each variant:**
   ```csharp
   Graphic sub = random.SubGraphicAtIndex(i);
   Texture2D preview = (Texture2D)sub.MatSingle.mainTexture;
   ```

3. **Apply player's choice:**
   ```csharp
   thing.overrideGraphicIndex = chosenIndex;
   thing.Notify_ColorChanged();
   ```

4. **Persistence:** Already handled — `overrideGraphicIndex` is saved by `Thing.ExposeData()`.

### CompStyleable — NOT Relevant to Texture Selection

`CompStyleable` handles **ideology style themes** (precept-based styling). When an ideology precept styles a weapon, `CompStyleable` assigns a `ThingStyleDef` which provides an entirely different `graphicData` path. This is a separate system from `Graphic_Random` texture variant selection.

`CompStyleable` is present on unique weapons but only matters for ideology integration. For our purposes:
- It does **not** control which base texture variant is shown
- `CompUniqueWeapon.TransformLabel` checks `parent.compStyleable?.SourcePrecept?.Label` — if set, the ideology precept label overrides the unique weapon name

### Graphic Refresh

After changing color or texture index:
```csharp
thing.Notify_ColorChanged();
// This sets graphicInt = null and styleGraphicInt = null
// If spawned on map, also dirties the map mesh for redraw
```

---

## WeaponTraitWorker System

### What Workers Are (and Are Not)

Workers are **event notification hooks** — a class that backs each trait def to receive pawn lifecycle events (equip, unequip, bond, kill). They are NOT responsible for abilities. The grenade launcher, EMP pulser, and other ability-granting traits work through an entirely separate system (`CompEquippableAbilityReloadable` + `abilityProps`, see [Ability System](#ability-system-trait-granted-abilities)).

Workers handle a narrow set of concerns: applying/removing hediffs on equip/unequip, applying/removing hediffs on bond/unbond, and giving thoughts on kill.

### Base Class

```csharp
public class WeaponTraitWorker
{
    public WeaponTraitDef def;  // Back-reference to the trait def

    // All methods are virtual, stateless:
    public virtual void Notify_Equipped(Pawn pawn)      // Adds def.equippedHediffs to brain
    public virtual void Notify_EquipmentLost(Pawn pawn) // Removes def.equippedHediffs
    public virtual void Notify_Bonded(Pawn pawn)        // Adds def.bondedHediffs to brain
    public virtual void Notify_Unbonded(Pawn pawn)      // Removes def.bondedHediffs
    public virtual void Notify_KilledPawn(Pawn pawn)    // Gives def.killThought
    public virtual void Notify_OtherWeaponWielded(CompBladelinkWeapon comp)  // Empty
}
```

### Subclasses (Only 2, Both Royalty)

| Worker | Behavior |
|--------|----------|
| `WeaponTraitWorker_PsyfocusOnKill` | Calls base `Notify_KilledPawn`, then adds 0.2 psyfocus |
| `WeaponTraitWorker_Jealous` | On `Notify_OtherWeaponWielded`, gives JealousRage thought (−15 mood, 1 day) |

### Critical Finding: Workers Are Stateless

Workers hold no mutable state — only the `def` back-reference. This means:

- **Adding a trait:** Just append to the list. If the weapon is equipped, call `Worker.Notify_Equipped(pawn)` to apply hediffs.
- **Removing a trait:** Remove from the list. If the weapon is equipped, call `Worker.Notify_EquipmentLost(pawn)` to clean up hediffs.
- **No worker state to serialize/deserialize** when traits change.

### Odyssey Traits — No Custom Workers

All 37 Odyssey weapon traits use the default `WeaponTraitWorker` base class. The two custom workers (`PsyfocusOnKill`, `Jealous`) are Royalty-only and apply to persona/bladelink weapons.

This means for our mod's purposes, the worker system is straightforward — adding/removing traits only needs to manage the hediff add/remove via the base worker methods. Modded traits could define custom worker subclasses, but since workers are stateless and follow the same virtual method pattern, our add/remove logic remains the same (call the appropriate notification methods).

---

## Ability System (Trait-Granted Abilities)

### How It Works

1. Every unique weapon ThingDef includes an **empty** `CompProperties_EquippableAbilityReloadable` in its comp list
2. `CompUniqueWeapon.Setup()` finds the first trait with `abilityProps != null` and replaces the comp's `props`
3. `CompEquippableAbility` lazily creates an `Ability` object from the props' `abilityDef`
4. On equip, the ability is assigned to the pawn; on unequip, it's removed

### Ability-Granting Traits

| Trait | Ability | Charges | Ammo | Notes |
|-------|---------|---------|------|-------|
| `GrenadeLauncher` | `LaunchFragGrenade` | 1 | 25 Steel | Range 12.9 |
| `EMPLauncher` | `LaunchEMPShell` | 2 | 15 Steel each | Range 23.9 |
| `SmokeLauncher` | `LaunchSmokeShell` | 2 | 15 Steel each | Range 23.9 |
| `IncendiaryLauncher` | `LaunchIncendiaryShell` | 1 | 15 Chemfuel | Range 23.9 |
| `BioferriteBurner` | `HellcatBurner` | 1 | 10 Bioferrite | Anomaly DLC |
| `EMPPulser` | `EMPPulse` | — | — | Cooldown-based (12h), no ammo |

### Implications for Adding/Removing Ability Traits

**Adding:** After appending the trait, call `Setup(fromSave: false)` to wire up the ability props and trigger `Notify_PropsChanged()`.

**Removing:** When removing a trait that had `abilityProps`:
- The `CompEquippableAbilityReloadable` needs its props reset to the empty default
- If the weapon is equipped, the pawn's ability list needs updating
- If another remaining trait has `abilityProps` (shouldn't happen due to `Ability` exclusion tag, but defensive), run `Setup()` again

**Only one ability trait at a time** — enforced by the `Ability` exclusion tag. All launcher and ability traits share this tag.

### Hellcat Rifle Special Case

`Gun_HellcatRifle_Unique` is unique among unique weapons: its `CompProperties_EquippableAbilityReloadable` is **pre-populated** with the `HellcatBurner` ability as a base weapon property (not from a trait). This means the hellcat has the bioferrite burner ability even with zero ability traits. Adding a `BioferriteBurner` trait would be redundant (and is likely handled by the weapon not having the `Attachable` category — though it does have it, the exclusion tag system would prevent doubling up).

**Actually, checking the categories:** `Gun_HellcatRifle_Unique` does NOT list `Attachable` in its categories, so no attachment traits can be added to it. The base ability is thus permanently baked in.

---

## Quality System

### Forced Super Quality

In `PostPostMake()`, unique weapons have their quality forced to `QualityGenerator.Super`:

```csharp
// CompUniqueWeapon.PostPostMake():
parent.TryGetComp<CompQuality>()?.SetQuality(
    QualityUtility.GenerateQualityFrom(QualityGenerator.Super),
    ArtGeneratedReason.Colony);
```

`QualityGenerator.Super` generates quality in the Excellent–Legendary range (weighted toward Masterwork).

### Can We Offer Quality Control?

**Technically possible but not recommended.** Quality is a general `CompQuality` field, not specific to the unique weapon system. Allowing quality changes would be:
- Out of scope for "weapon trait customization"
- Potentially balance-breaking (crafting Legendary weapons on demand)
- Not thematically connected to the trait system

**Recommendation:** Leave quality as-is. If the weapon starts non-unique (Normal quality) and is converted to unique, we should probably force it to `QualityGenerator.Super` to match vanilla behavior.

---

## CompArt Integration

### Title Coupling

`PostPostMake()` sets `CompArt.Title` to the same string as the unique weapon name. CompArt is the component that enables the "Art" tab on items, showing title and description.

`CompArt.Title` has a **public setter** — easy to update. When renaming or regenerating a weapon name, we should also update:

```csharp
if (thing.TryGetComp<CompArt>(out var artComp))
    artComp.Title = newName;
```

### Art Description

`CompArt` also has a description (`AuthorName`, story text). These are generated separately from the weapon name. We could potentially offer editing of the art description as well, though this is lower priority.

### Not All Unique Weapons Have CompArt

`Bow_Great_Unique` does **not** have `CompProperties_Art` in its comps. It also lacks `CompProperties_Biocodable`. This is likely because the great bow is a simpler, lower-tech weapon.

---

## Additional Customizable Properties

Beyond traits, name, color, and texture, here are other properties we could potentially expose:

### 1. Biocodability (`CompBiocodable`)

Most unique weapons (all except `Bow_Great_Unique`) have `CompBiocodable`. This allows the weapon to be biocoded to a specific pawn. We could potentially offer:
- Un-biocoding a weapon (if biocoded)
- This is already possible via dev tools but could be surfaced as a customization option

**Assessment:** Low priority, tangential to our core goals.

### 2. Weapon Bonding (Separate System — Royalty)

Bonding is handled by `CompBladelinkWeapon`, which is for persona weapons (Royalty DLC), NOT unique weapons (Odyssey). Unique weapons do not bond. Some traits reference bonding fields (`bondedHediffs`, `bondedThought`, `neverBond`) but these are only used by Royalty's bladelink traits.

**Assessment:** Not applicable to our mod.

### 3. Ideology Styling (`CompStyleable`)

If Ideology is active, weapons can be styled by ideology precepts. This is a separate system from our trait customization. When a weapon is styled:
- `CompStyleable.SourcePrecept?.Label` overrides the unique weapon name in `TransformLabel()`
- A different graphic may be used (from `ThingStyleDef.graphicData`)

**Assessment:** Our mod should coexist with ideology styling. If a weapon has an ideology style, our name changes won't be visible (the ideology label takes precedence). This is an edge case to document, not a feature to add.

---

## Obstacles & Challenges

### 1. No RemoveTrait API

Vanilla provides `AddTrait()` but no `RemoveTrait()`. We must implement removal ourselves, handling:
- List removal
- Hediff cleanup (if equipped)
- Ability props reset
- Cache invalidation (`ignoreAccuracyMaluses = null`)
- Name/color regeneration

**Severity: Medium.** Straightforward to implement, but must be thorough about side effects.

### 2. Private Fields

`name` and `color` are private fields with no public setters. Access requires:
- `Harmony.AccessTools.Field(typeof(CompUniqueWeapon), "name")`
- `Harmony.AccessTools.Field(typeof(CompUniqueWeapon), "color")`
- Or `Traverse.Create(comp).Field("name").SetValue(newName)`

**Severity: Low.** Standard Harmony pattern, well-understood.

### 3. No Max-Trait Check in CanAddTrait

`CanAddTrait()` doesn't check against the 3-trait maximum. Our mod must add this validation.

**Severity: Low.** Simple `traits.Count >= 3` check.

### 4. canGenerateAlone Validation on Removal

When removing a trait, we must verify the remaining traits don't leave a `canGenerateAlone=false` trait as the sole trait. Vanilla only checks this during addition (and only when the list is empty).

**Severity: Low.** Simple check: if after removal only one trait remains and that trait has `canGenerateAlone=false`, block the removal (or force removal of both).

### 5. Color Not Recoverable After forcedColor Trait Removal

When a trait with `forcedColor` is present, the `color` field is overwritten in-place during `PostPostMake()`. The original randomly-assigned color is permanently lost. When removing a color-forcing trait, we must assign a new color — either randomly or via player choice.

**Severity: Low.** Straightforward design decision. The customization dialog should surface this clearly.

### 6. Base-to-Unique Conversion (ThingDef Swap)

Converting a non-unique weapon to unique requires creating a new `Thing` with a different `ThingDef` (the unique variant). This is because comp lists differ (`CompEquippableAbilityReloadable` vs `CompEquippable`). We must:
- Create a new Thing from the unique ThingDef
- Transfer relevant state (hit points, quality if applicable)
- Destroy the old Thing
- The new Thing gets its own `thingIDNumber`, which changes the default graphic variant

**Severity: Medium.** Requires careful state transfer. The `thingIDNumber` change means the graphic will change — we should set `overrideGraphicIndex` to control this.

### 7. Unique-to-Base Reversion (Removing All Traits)

The reverse — converting a unique weapon back to its non-unique base when all traits are removed — has the same ThingDef swap challenge, plus:
- The weapon loses all unique-weapon properties (CompUniqueWeapon, CompEquippableAbilityReloadable, CompArt, etc.)
- Quality may change (unique weapons are Super quality; base weapons have whatever quality they were crafted at)

**Severity: Medium.** Design question: what quality should the reverted weapon have? Options include preserving the unique quality, resetting to Normal, or using the crafting pawn's skill.

### 8. Ideology Label Override

When a weapon has an ideology style precept (`CompStyleable.SourcePrecept`), `TransformLabel()` returns the original ThingDef label instead of the unique name. Our custom name would be invisible.

**Severity: Low.** Edge case. We can detect this and show a note in the UI ("Name overridden by ideology styling").

### 9. Color Priority with CompColorable

`ThingWithComps.DrawColor` checks `CompColorable` first (if active), then iterates comps for `ForceColor()`. If something sets `CompColorable` on a weapon, it would override our color. In practice, unique weapons don't have `CompColorable`, so this shouldn't be an issue.

**Severity: Very low.** Theoretical edge case only.

### 10. Modded Trait Compatibility

Our mod automatically supports modded traits via the category system. However:
- Modded traits with custom `WeaponTraitWorker` subclasses may have unexpected side effects
- Modded traits may use fields in unexpected combinations
- Our cost calculation system needs to handle arbitrary modded traits gracefully

**Severity: Low.** Our dynamic approach handles this by design. Cost calculation is our logic, not dependent on trait internals.

---

## Summary of Customization Opportunities

### Fully Supported (Core Features)

| Feature | Mechanism | Difficulty |
|---------|-----------|------------|
| **Add traits** | `CompUniqueWeapon.AddTrait()` + `Setup()` | Low |
| **Remove traits** | Custom implementation (list removal + side effects) | Medium |
| **Rename weapon** | Set private `name` field + `CompArt.Title` | Low |
| **Randomize name** | Reproduce `PostPostMake` grammar logic (all APIs public) | Low |
| **Free-text rename** | Set `name` to any string | Trivial |
| **Change shader color** | Set private `color` field + `Notify_ColorChanged()` | Low |
| **Select texture variant** | Set `overrideGraphicIndex` + `Notify_ColorChanged()` | Low |
| **Convert base → unique** | ThingDef swap (destroy + create) | Medium |
| **Convert unique → base** | ThingDef swap (destroy + create) | Medium |

### Possible Extensions (Lower Priority)

| Feature | Mechanism | Difficulty | Notes |
|---------|-----------|------------|-------|
| **Edit art description** | `CompArt` fields | Low | Niche interest |
| **Un-biocode weapon** | `CompBiocodable` reset | Low | Dev-tool-like |
| **Preview stat changes** | Calculate from `WeaponTraitDef.statOffsets/statFactors` | Low | For the dialog UI |
| **Preview name from traits** | Grammar reproduction | Low | "Randomize" button |
| **Bulk color palette** | Show all `ColorDef` where `colorType == Weapon` | Low | Including non-random ones |

### Not Feasible / Out of Scope

| Feature | Reason |
|---------|--------|
| Quality control | Balance-breaking, not related to trait system |
| Custom trait creation | Would require XML authoring by players |
| Melee unique weapons | No unique melee defs exist in vanilla |
| Ideology style control | Separate system (CompStyleable / precepts) |
