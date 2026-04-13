# Modder's Guide: Integrating with Unique Weapons Unbound

Unique Weapons Unbound allows players to customize unique weapons by adding and removing traits at a workbench. The mod uses a data-driven pipeline to calculate the material cost of each trait operation. Third-party mods can hook into this pipeline by defining their own **TraitCostRuleDef** entries in XML and (optionally) writing custom **TraitCostRuleWorker** subclasses in C#.

For a complete reference of the rules this mod ships with, see [`1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`](1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml).

## Weapon detection: base and unique variants

Before cost rules come into play, the mod needs to identify which weapons are customizable. At startup, the mod scans all `ThingDef`s for those with a `CompUniqueWeapon` component (vanilla's marker for unique weapons). For each one it finds, it attempts to resolve the **base weapon** — the non-unique version the weapon is derived from — using two methods:

1. **descriptionHyperlinks (primary):** If the unique weapon's `descriptionHyperlinks` contain a link to a non-unique weapon `ThingDef`, that link target is used as the base weapon. This is the most reliable method and works for modded weapons regardless of naming conventions.
2. **Naming convention (fallback):** If no hyperlink is found, the mod checks whether the unique weapon's `defName` ends with `_Unique` and looks for a `ThingDef` with the prefix as its `defName` (e.g. `Gun_Revolver_Unique` → `Gun_Revolver`).

If neither method resolves a base weapon, the unique weapon is still customizable — it just won't have recipe-derived costs and will fall back to tech-level-based pricing instead.

**If your mod's unique weapons aren't showing up as customization targets**, check that:

- The weapon def has a `CompUniqueWeapon` component (this is how vanilla marks unique weapons via the Odyssey DLC).
- The weapon's `descriptionHyperlinks` include a link to the base weapon def, **or** the `defName` follows the `{BaseDefName}_Unique` convention.

## How the default pipeline works

When a player adds or removes a trait, the mod runs all `TraitCostRuleDef` entries in **priority order** (lower first). Each rule's worker is asked whether it matches the trait and, if so, it mutates the cost list. The built-in rules handle all vanilla traits without any additional configuration:

1. **Base cost establishment (priority 50–100):** First, a fallback cost is seeded from the weapon's tech level (neolithic → wood, medieval → steel, industrial → steel + components, spacer+ → plasteel + advanced components). Then, if the weapon has a craftable recipe, those costs replace the fallback entirely.

2. **Quality scaling (priority 200):** All costs are multiplied by `0.5 × qualityMultiplier`, so each trait costs roughly half the weapon's crafting recipe, scaled by quality. Awful weapons are cheapest (0.35x), legendary are most expensive (1.25x).

3. **Negative trait handling (priority 300):** Traits are detected as "negative" when they have a `MarketValue` stat factor below 1.0. When adding a negative trait, material costs are **downgraded one tech tier** (plasteel → steel, spacer components → industrial components, etc.) to reflect the cruder work involved. The final addition cost for negative traits is further reduced by the refund rate (default 50%). Removing a negative trait costs resources at the refund rate using the original (non-downgraded) materials, since restoring the weapon requires proper-tier work.

4. **Thematic rules (priority 1000–8000):** Keyword-matched rules transform costs to fit the trait's theme — toxic traits swap components for herbal medicine, incendiary traits use chemfuel, EMP traits convert to industrial components, charge/crypto traits convert to spacer components, and so on.

5. **Fallback and cleanup (priority 9000–9900):** A material override rule auto-detects material names in the trait label (e.g. "gold" in "Gold Inlay") and replaces raw resource costs with that material. Finally, a pruning rule caps the cost list at 3 material types to prevent UI overflow.

### Priority bands

| Priority  | Phase         | Purpose                                                                        |
| --------- | ------------- | ------------------------------------------------------------------------------ |
| 50–300    | Foundation    | Base costs from tech level/recipe, quality scaling, negative trait downgrades  |
| 1000–2100 | Thematic      | Keyword-matched material swaps and transformations (e.g. emp, incendiary, tox) |
| 8000      | Late thematic | Rules that must run after most thematic rules (e.g. inlay)                     |
| 9000      | Fallback      | Material override auto-detection from trait label                              |
| 9900      | Cleanup       | Prune excess material types to max 3 to prevent UI overflow                    |

**Recommended priority for third-party rules:** `3000–7000`. This places your rules after the foundation phase and before the late-running fallback rules.

## XML-only rules (no C# required)

Many cost transformations can be achieved by reusing the built-in worker classes. You only need to write C# if the built-in workers don't cover your use case.

### TraitCostRuleDef fields

```xml
<UniqueWeaponsUnbound.TraitCostRuleDef>
  <!-- Required -->
  <defName>MyMod_RuleName</defName>
  <workerClass>UniqueWeaponsUnbound.WorkerClassName</workerClass>
  <priority>3000</priority>

  <!-- Optional: human-readable metadata -->
  <label>my rule name</label>
  <description>What this rule does.</description>

  <!-- Optional: keyword matching (omit to match all traits) -->
  <labelKeywords>
    <li>keyword1</li>
    <li>keyword2</li>
  </labelKeywords>

  <!-- false (default) = match ANY keyword; true = require ALL keywords -->
  <requireAllKeywords>false</requireAllKeywords>

  <!-- Optional: restrict to specific weapon categories -->
  <weaponCategories>
    <li>Bow</li>
    <li>Pistol</li>
  </weaponCategories>
</UniqueWeaponsUnbound.TraitCostRuleDef>
```

### Keyword matching

Keywords are matched against the trait's `label` field, split on spaces and hyphens. For example, a trait labeled `"toxic short-bow"` produces the word set `{"toxic", "short-bow", "short", "bow"}`.

- If `labelKeywords` is empty or omitted, the rule matches **all traits** (unconditional).
- If `requireAllKeywords` is `false` (default), matching **any** keyword is sufficient.
- If `requireAllKeywords` is `true`, **all** keywords must be present.

### Weapon category filter

When `weaponCategories` is specified, the rule only matches traits whose `weaponCategory` is in the list. This is checked **before** keyword matching. If `weaponCategories` is omitted, the rule applies to all weapon categories.

### Reusable built-in worker classes

These workers are generic enough to be reused from XML without writing C#:

| Worker class             | What it does                                                |
| ------------------------ | ----------------------------------------------------------- |
| `ComponentRemovalWorker` | Removes industrial and spacer components from the cost list |
| `ConvertToSpacerWorker`  | Converts all costs into spacer components by market value   |

### Example: XML-only rules

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!--
    Any trait with "quantum" in the label on a PulseCharge weapon
    has all costs converted to spacer components.
  -->
  <UniqueWeaponsUnbound.TraitCostRuleDef>
    <defName>MyMod_QuantumCharge</defName>
    <label>quantum charge conversion</label>
    <description>Converts all costs to advanced components for quantum traits on pulse weapons.</description>
    <workerClass>UniqueWeaponsUnbound.ConvertToSpacerWorker</workerClass>
    <priority>3000</priority>
    <labelKeywords>
      <li>quantum</li>
    </labelKeywords>
    <weaponCategories>
      <li>PulseCharge</li>
    </weaponCategories>
  </UniqueWeaponsUnbound.TraitCostRuleDef>

  <!--
    Any trait with "stripped" in the label has components removed,
    leaving only raw material costs.
  -->
  <UniqueWeaponsUnbound.TraitCostRuleDef>
    <defName>MyMod_StrippedDown</defName>
    <label>stripped component removal</label>
    <description>Removes component costs for stripped-down weapon modifications.</description>
    <workerClass>UniqueWeaponsUnbound.ComponentRemovalWorker</workerClass>
    <priority>3100</priority>
    <labelKeywords>
      <li>stripped</li>
    </labelKeywords>
  </UniqueWeaponsUnbound.TraitCostRuleDef>

</Defs>
```

## Custom worker classes (C#)

For transformations the built-in workers can't express, subclass `TraitCostRuleWorker`.

### API

```csharp
public abstract class TraitCostRuleWorker
{
    // The def that owns this worker. Access labelKeywords,
    // weaponCategories, etc. through this field.
    public TraitCostRuleDef def;

    // Return true if this rule applies to the given trait.
    // Default implementation checks labelKeywords (with requireAllKeywords)
    // and weaponCategories from the def. Override for custom matching logic.
    public virtual bool Matches(HashSet<string> labelWords, WeaponTraitDef trait);

    // Mutate the costs list in place. Called only when Matches() returns true.
    // isRemoval is true when calculating costs for trait removal, false for addition.
    public abstract void Apply(
        List<ThingDefCountClass> costs, Thing weapon,
        WeaponTraitDef trait, bool isRemoval);
}
```

### Example: custom worker that adds a flat surcharge

This worker adds 5 of a specific material on top of the existing costs.

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MyMod
{
    public class GlitterworldSurchargeWorker : TraitCostRuleWorker
    {
        public override void Apply(
            List<ThingDefCountClass> costs, Thing weapon,
            WeaponTraitDef trait, bool isRemoval)
        {
            ThingDef glitterMed = ThingDefOf.MedicineUltratech;
            // Add 5 glitterworld medicine to existing costs
            ThingDefCountClass existing = costs.Find(c => c.thingDef == glitterMed);
            if (existing != null)
                existing.count += 5;
            else
                costs.Add(new ThingDefCountClass(glitterMed, 5));
        }
    }
}
```

```xml
<UniqueWeaponsUnbound.TraitCostRuleDef>
  <defName>MyMod_GlitterworldSurcharge</defName>
  <label>glitterworld surcharge</label>
  <description>Adds 5 glitterworld medicine to the cost of any trait with "nanite" in the label.</description>
  <workerClass>MyMod.GlitterworldSurchargeWorker</workerClass>
  <priority>4000</priority>
  <labelKeywords>
    <li>nanite</li>
  </labelKeywords>
</UniqueWeaponsUnbound.TraitCostRuleDef>
```

### Example: custom worker with overridden Matches()

Override `Matches()` when you need matching logic beyond keywords and categories — for example, matching based on stat modifiers.

```csharp
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MyMod
{
    /// <summary>
    /// Doubles costs for any trait that boosts ranged cooldown by more than 20%.
    /// </summary>
    public class HighFireRateSurchargeWorker : TraitCostRuleWorker
    {
        public override bool Matches(HashSet<string> labelWords, WeaponTraitDef trait)
        {
            if (trait.statFactors == null)
                return false;

            for (int i = 0; i < trait.statFactors.Count; i++)
            {
                if (trait.statFactors[i].stat == StatDefOf.RangedWeapon_Cooldown
                    && trait.statFactors[i].value < 0.8f)
                    return true;
            }
            return false;
        }

        public override void Apply(
            List<ThingDefCountClass> costs, Thing weapon,
            WeaponTraitDef trait, bool isRemoval)
        {
            foreach (ThingDefCountClass cost in costs)
                cost.count = Mathf.CeilToInt(cost.count * 2f);
        }
    }
}
```

```xml
<UniqueWeaponsUnbound.TraitCostRuleDef>
  <defName>MyMod_HighFireRateSurcharge</defName>
  <label>high fire rate surcharge</label>
  <description>Doubles costs for traits that significantly boost fire rate.</description>
  <workerClass>MyMod.HighFireRateSurchargeWorker</workerClass>
  <priority>5000</priority>
  <!-- No labelKeywords — Matches() handles all filtering -->
</UniqueWeaponsUnbound.TraitCostRuleDef>
```

### Example: addition vs removal asymmetry

The `isRemoval` parameter lets you apply different logic when a trait is being removed versus added. Most workers can ignore it.

```csharp
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MyMod
{
    /// <summary>
    /// Halves costs when adding (installation discount), but keeps
    /// full costs when removing (requires careful disassembly).
    /// </summary>
    public class InstallationDiscountWorker : TraitCostRuleWorker
    {
        public override void Apply(
            List<ThingDefCountClass> costs, Thing weapon,
            WeaponTraitDef trait, bool isRemoval)
        {
            if (isRemoval)
                return; // No discount for removal

            foreach (ThingDefCountClass cost in costs)
                cost.count = Mathf.CeilToInt(cost.count * 0.5f);
        }
    }
}
```

## CostRuleHelpers utilities

The `CostRuleHelpers` static class provides helper methods that your workers can use. These are available after mod initialization.

| Method                                                                | Description                                                                                           |
| --------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `AddOrMerge(costs, thingDef, count)`                                  | Adds to an existing entry or creates a new one                                                        |
| `RemoveMaterials(costs, params thingDefs)`                            | Removes all entries matching the given ThingDefs                                                      |
| `ApplyValueSplit(costs, replacement, fraction)`                       | Splits off a fraction of wood/steel/plasteel by market value and converts to the replacement material |
| `ApplyComponentSwapOrSplit(costs, replacement, multiplier, fraction)` | Replaces components with `multiplier * count` of replacement, or falls back to a value split          |
| `ApplyPartialSwapByCount(costs, source, replacement, fraction)`       | Swaps a fraction of one material to another (1:1 by count)                                            |
| `ApplyConvertAllToSpacer(costs)`                                      | Converts all costs into spacer components by market value                                             |
| `ApplyFlatCost(costs, material, count)`                               | Replaces all costs with a single entry                                                                |
| `ApplyCostMultiplier(costs, multiplier)`                              | Scales all cost counts by the multiplier (rounds up)                                                  |
| `ConvertHalfByCount(costs, replacement)`                              | Converts half of every cost entry to the replacement material                                         |
| `ApplyMaterialOverride(costs, overrideMaterial)`                      | Sums raw resource costs and replaces with the override material                                       |
| `IsRawResource(thingDef)`                                             | Returns true if the ThingDef is a stuff or ResourcesRaw item                                          |

## Tips

- **Test your priority.** Load order matters. If your rule should run before the material override fallback (priority 9000), keep your priority below that. If it should run after the quality multiplier (priority 200), keep it above that.
- **Mutate `costs` in place.** The list is passed by reference and shared across the pipeline. Use `costs.Clear()`, `costs.Add()`, `costs.RemoveAll()`, etc. directly.
- **Don't worry about zero counts.** The pipeline automatically removes entries with `count <= 0` after all rules have run.
- **Null-check DLC materials.** Materials from DLC (Bioferrite, Birdskin, etc.) may be null if the DLC isn't active.
- **Use `CostRuleHelpers.AddOrMerge()`** when adding materials that might already be in the list to avoid duplicate entries.
