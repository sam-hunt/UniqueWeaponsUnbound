# Unique Weapons Unbound - Design Document

## Background

RimWorld's Odyssey DLC introduced **unique weapons** — special weapon defs based on existing weapons, prefixed with `Unique` (e.g. `ChargeRifle` → `UniqueChargeRifle`). Unique weapons carry 1–3 **weapon traits** that alter properties like accuracy, weight, name, color, or grant abilities (grenade launcher, EMP pulser, etc.).

In vanilla Odyssey, unique weapons are only obtainable via quest rewards or rare world loot generation.

### Scope: Odyssey Unique Weapons Only

This mod targets **Odyssey unique weapons** exclusively. Royalty's **bladelink/persona weapons** (`CompBladelinkWeapon`) are categorically excluded, despite sharing the `WeaponTraitDef` type with Odyssey's system.

**Thematic rationale:** Odyssey weapon traits describe physical modifications — upgraded sights, ammo types, barrel attachments, material inlays — exactly the kind of adjustments a skilled crafter could make at a workbench with the right research and resources. Royalty's bladelink traits, by contrast, are psychically granted by the weapon's AI persona. Modifying an AI's personality at a fabrication table is not a believable player action, regardless of research level.

**Technical note:** The two systems are parallel but independent. They use different comps (`CompUniqueWeapon` vs `CompBladelinkWeapon`), non-overlapping `WeaponCategoryDef` sets (Odyssey's 15 categories vs Royalty's single `BladeLink` category), and no vanilla weapon has both comps. Our code keys off `CompUniqueWeapon` presence, so bladelink weapons are excluded by default. Any code that iterates `WeaponTraitDef`s should filter to traits whose `weaponCategory` is NOT `BladeLink`, to avoid displaying Royalty persona traits in our UI.

### Trait System Constraints (Vanilla)

- Maximum of **3 traits** per weapon.
- Some traits **cannot be the sole trait** on a weapon — they require at least one other trait.
- Traits are tagged with **categories**; traits from the same or thematically conflicting categories cannot coexist on a single weapon.

### Related Work

Our companion mod **BetterTradersGuild** provides:

- A custom weapon trait (`SilverInlay`) that alters value, color, and naming — see `../BetterTradersGuild/1.6/Defs/WeaponTraitDefs/SilverInlay.xml`.
- Code to programmatically add traits and update weapons — see `../BetterTradersGuild/Source/1.6/ScenParts/ScenPart_StartingUniqueWeapon.cs`.

---

## Core Features

### 1. Add Traits

- Add traits to **non-unique weapons**, converting them into their unique variant.
- Add traits to **existing unique weapons** (up to the trait limit).

### 2. Remove Traits

- Remove individual traits from unique weapons.
- If all traits are removed, the weapon **reverts to its non-unique variant**.

### 3. Trait Validation

All player-initiated trait changes must respect the same rules enforced for naturally generated unique weapons:

- Maximum trait count (3).
- Sole-trait restrictions.
- Category exclusion/conflict rules.
- Any other constraints the game enforces.

---

## Research Gating

Weapon customization is unlocked progressively through new research projects, gated by the weapon's **tech level**.

| Research Project   | Unlocks Tech Levels | Prerequisites                 | Research Bench           |
| ------------------ | ------------------- | ----------------------------- | ------------------------ |
| Unique Smithing    | Neolithic, Medieval | Smithing                      | —                        |
| Unique Machining   | Industrial          | Unique Smithing, Gunsmithing  | —                        |
| Unique Fabrication | Spacer, Ultratech   | Unique Machining, Fabrication | Hi-tech + multi-analyzer |

### Archotech Weapons

- **Not customizable by default.**
- A **mod setting** should allow grouping Archotech weapons with Spacer/Ultratech for players who want this.

### Techprint Requirement (Royalty DLC)

When the Royalty DLC is active, **Unique Fabrication** requires **1 techprint** before the research can begin. The `techprintCount` field on `ResearchProjectDef` has a built-in `ModLister.RoyaltyInstalled` guard — if Royalty is not active, the requirement is silently skipped.

- **Techprint count:** 1
- **Techprint market value:** 1500
- **Held by factions:** Empire, Outlander, TradersGuild
- **Applies to:** Unique Fabrication only (not Unique Smithing or Unique Machining)

### Unique Weapon Analysis Requirement (Core API)

**Status: To evaluate**

Each weapon customization research project should require the player to have **analyzed at least one unique weapon** of the related tech level (or higher) before the research can begin. This creates a natural gameplay flow: find a unique weapon first, study it, then unlock the ability to customize weapons of that tier.

The core game provides the `requiredAnalyzed` field on `ResearchProjectDef` and the `CompAnalyzableUnlockResearch` / `AnalysisManager` system. However, there are significant design and technical challenges:

**Challenge 1: "Any one of" vs "all of" semantics.** The vanilla `requiredAnalyzed` system requires ALL listed ThingDefs to be analyzed. Our design requires ANY ONE unique weapon of the right tech level — a fundamentally different semantic. If we list all unique weapons of a tech level in `requiredAnalyzed`, the player would need to find and analyze every single one.

**Challenge 2: Dynamic weapon set.** We don't know at def-authoring time which unique weapons exist — other mods may add more. Hard-coding specific `ThingDef` names in `requiredAnalyzed` would break our mod compatibility goals.

**Challenge 3: Missing comp.** Unique weapons don't natively have `CompAnalyzableUnlockResearch`. We'd need to add it via XML patches to all unique weapon ThingDefs.

Potential implementation approaches to evaluate:

1. **Harmony patch on `AnalyzedThingsRequirementsMet`** — Override this property for our research projects to implement "any one of" logic, dynamically checking all unique weapons of the right tech level.
2. **Proxy ThingDef per tier** — Create invisible "analysis token" ThingDefs (one per tier). XML-patch all unique weapons to add `CompAnalyzableUnlockResearch`. When any unique weapon of a tier is analyzed, our code marks the tier's token as satisfied. Put only the token in `requiredAnalyzed`. Works with the vanilla system.
3. **Fully custom gating** — Don't use `requiredAnalyzed` at all. Instead, Harmony-patch `CanStartNow` to add our own check that queries whether the player has ever analyzed a unique weapon of the right tech level. Track this state ourselves via `IExposable` + `GameComponent`.
4. **Simplest fallback** — Skip `requiredAnalyzed` entirely and instead use Harmony to patch the float menu / dialog to require that the player has _possessed_ (not necessarily analyzed) a unique weapon of the right tier. Less thematic but avoids the analysis system complexity.

---

## Craftability Gating

### Default Behavior

A weapon is only customizable if:

1. The weapon has a unique variant defined in the game.
2. The player has completed the appropriate **weapon customization research** for the weapon's tech level.
3. The weapon's non-unique variant is **craftable by the player** — meaning a recipe exists and its prerequisite research (if any) has been completed.

If a weapon has no crafting recipe at all (i.e. cannot be player-crafted), it is **not customizable by default**.

### Mod Setting Override

A mod setting should allow customization of weapons that lack crafting recipes, for players who find the default restrictive.

### Dynamic Detection

All of the above must be **dynamically detected from defs**, not hard-coded. This ensures automatic compatibility with other mods that add:

- New weapon/unique-weapon def pairs.
- New crafting recipes or research prerequisites.
- New weapon traits with appropriate categories/tags.

---

## Resource Costs

### Trait Addition Costs

Adding a trait costs resources based on:

- The **raw resource cost** of the weapon (derived from its crafting recipe).
- The weapon's **quality multiplier**.

### Trait-Specific Cost Overrides

Some traits should override the default resource types:

- Example: A "Gold Inlay" trait should require **gold** instead of the weapon's default materials.
- Example: A "Jade Inlay" trait should require **jade**.

### Dynamic Inlay Detection

Inlay-type traits may be **dynamically detectable** — their name and shader color likely match or partially match the stuff material they reference (e.g. "Silver Inlay" → silver stuff, matching color). This would provide automatic support for modded inlay traits.

### Trait Removal Costs

_To be determined._ Removal may be free or have a nominal labor cost.

---

## Workbench Integration

### Workbench Assignment by Tech Level

Weapons are customizable at existing workbenches appropriate to their tech level **or higher**:

| Workbench         | Tech Levels (with appropriate research)            |
| ----------------- | -------------------------------------------------- |
| Smithing Table    | Neolithic, Medieval                                |
| Machining Table   | Neolithic, Medieval, Industrial                    |
| Fabrication Table | Neolithic, Medieval, Industrial, Spacer, Ultratech |

### Float Menu Interaction

When a pawn is selected and the player right-clicks a workbench, the float menu option goes through the following checks **in order**. Each check either hides the option entirely (irrelevant context) or shows it disabled with a parenthesized reason (actionable blocker). This ordering ensures the most relevant blocker is shown first.

| #   | Check                                                             | If failed                                                                                                             |
| --- | ----------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| 1   | Workbench is a smithy, machining table, or fabrication bench      | **Hidden** — irrelevant workbench                                                                                     |
| 2   | Weapon has a unique variant AND Unique Smithing research is done  | **Hidden** — not a customizable weapon, or player hasn't engaged with the mod's research tree yet                     |
| 3   | Workbench tier is sufficient for the weapon's tech level          | **Disabled**: "requires machining table" / "requires fabrication bench"                                               |
| 4   | Base weapon's crafting recipe research is done                    | **Disabled**: "requires {research name}"                                                                              |
| 5   | Weapon customization research for the weapon's tech level is done | **Disabled**: "requires {research name}"                                                                              |
| 6   | Pawn can reach the workbench / workbench is not forbidden         | **Disabled**: "no path" / "forbidden"                                                                                 |
| 7   | All checks pass                                                   | **Enabled** — selecting it queues a job for the pawn to go to the workbench and opens the Weapon Customization Dialog |

**Design rationale:**

- **Check 1** scopes the float menu to weapon-crafting workbenches only, avoiding clutter on thematically irrelevant benches (tailoring, drug lab, etc.).
- **Check 2** gates all UI on Unique Smithing research, so players who haven't engaged with the mod's content never see customization options.
- **Checks 3–5** are ordered from most "fundamental" to most "mod-specific": workbench mismatch → missing vanilla research → missing mod research. This ensures the player sees the most actionable blocker first.
- **Workbench labels** in check 3 are derived from def labels at init time. For workbench tiers with multiple variants (e.g. fueled/electric smithy), the common label suffix is computed automatically (→ "smithy").

---

## Weapon Customization Dialog

The dialog should look and behave similarly to Ideology's **Styling Station**.

### Layout and Behavior

- **Weapon preview graphic** — updates live as traits are added/removed.
- **Current traits** — displayed on the weapon.
- **Available traits list** — valid traits that can be added, dynamically filtered as the player makes selections (respecting exclusion rules, max count, etc.).
- **Per-trait cost** — shown next to each addable trait.
- **Total cost summary** — aggregate cost of all previewed changes.
- **Confirm button** — queues individual jobs for each trait change.

### Job Queuing (Styling Station Model)

- Each trait addition or removal is queued as a **separate job**, mirroring the Styling Station's approach.
- Resources are consumed **on job completion**, not on confirmation.
- Interrupting the job sequence is **non-destructive** — no resources are lost for incomplete jobs, unlike crafting recipes that create unfinished items.

### Weapon Naming

- If the weapon is still unique after customization, the player should be able to **rename** it (possibly a separate job).
- A **randomize name button** should generate a new name from the weapon's current traits, since traits contribute 2–5 adjectives to the name pool, of which only 1–2 are typically selected.

---

## Mod Compatibility Goals

This mod should be **inherently compatible** with other mods that:

- Add new `Unique`-prefixed weapon defs paired with base weapons.
- Add new weapon traits with proper category/tag metadata.
- Add new crafting recipes or research prerequisites for weapons.
- Add new workbenches (future consideration).

No hard-coded def references should be used for weapon/trait detection. All logic should operate on def properties, categories, tags, and relationships.
