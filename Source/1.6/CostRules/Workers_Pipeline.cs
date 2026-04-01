using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Provides a fallback base cost derived from the weapon's tech level.
    /// Runs first in the pipeline so that weapons without a craftable base def
    /// still have a reasonable cost. Weapons with recipes will have these costs
    /// replaced by <see cref="BaseCostFromRecipeWorker"/>.
    /// </summary>
    public class TechLevelCostWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            TechLevel tech = weapon.def.techLevel;

            switch (tech)
            {
                case TechLevel.Neolithic:
                    costs.Add(new ThingDefCountClass(ThingDefOf.WoodLog, 100));
                    break;
                case TechLevel.Medieval:
                    costs.Add(new ThingDefCountClass(ThingDefOf.Steel, 100));
                    break;
                case TechLevel.Industrial:
                    costs.Add(new ThingDefCountClass(ThingDefOf.Steel, 80));
                    costs.Add(new ThingDefCountClass(ThingDefOf.ComponentIndustrial, 6));
                    break;
                default: // Spacer, Ultra, Archotech
                    costs.Add(new ThingDefCountClass(ThingDefOf.Plasteel, 80));
                    costs.Add(new ThingDefCountClass(ThingDefOf.ComponentSpacer, 4));
                    break;
            }
        }
    }

    /// <summary>
    /// Replaces costs with the weapon's actual crafting recipe ingredients.
    /// Only acts when the weapon has a craftable base def with recipe costs;
    /// otherwise leaves the tech-level fallback costs in place.
    /// </summary>
    public class BaseCostFromRecipeWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            ThingDef baseDef = WeaponRegistry.IsUniqueWeapon(weapon.def)
                ? WeaponRegistry.GetBaseVariant(weapon.def)
                : weapon.def;

            // Fall back to the weapon's own def for recipe resolution.
            // Handles base-def-less unique weapons that have their own crafting recipe.
            ThingDef recipeDef = baseDef ?? weapon.def;
            if (recipeDef == null)
                return;

            var recipeCosts = new List<ThingDefCountClass>();

            if (recipeDef.costList != null)
            {
                foreach (ThingDefCountClass entry in recipeDef.costList)
                    recipeCosts.Add(new ThingDefCountClass(entry.thingDef, entry.count));
            }

            if (recipeDef.costStuffCount > 0)
            {
                ThingDef stuff = weapon.Stuff
                    ?? GenStuff.DefaultStuffFor(recipeDef)
                    ?? ThingDefOf.Steel;
                recipeCosts.Add(new ThingDefCountClass(stuff, recipeDef.costStuffCount));
            }

            if (recipeCosts.Count > 0)
            {
                costs.Clear();
                costs.AddRange(recipeCosts);
            }
        }
    }

    /// <summary>
    /// Applies the cost fraction (0.5x base recipe) and quality-based multiplier.
    /// </summary>
    public class QualityMultiplierWorker : TraitCostRuleWorker
    {
        private const float CostFraction = 0.5f;

        private static readonly Dictionary<QualityCategory, float> QualityMultipliers =
            new Dictionary<QualityCategory, float>
            {
                { QualityCategory.Awful, 0.7f },
                { QualityCategory.Poor, 0.85f },
                { QualityCategory.Normal, 1.0f },
                { QualityCategory.Good, 1.25f },
                { QualityCategory.Excellent, 1.5f },
                { QualityCategory.Masterwork, 2.0f },
                { QualityCategory.Legendary, 2.5f },
            };

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            float qualityMult = GetQualityMultiplier(weapon);
            float totalMult = CostFraction * qualityMult;

            foreach (ThingDefCountClass cost in costs)
                cost.count = Mathf.CeilToInt(cost.count * totalMult);
        }

        public static float GetQualityMultiplier(Thing weapon)
        {
            if (!weapon.TryGetQuality(out QualityCategory quality))
                return 1f;

            return QualityMultipliers.TryGetValue(quality, out float mult) ? mult : 1f;
        }
    }

    /// <summary>
    /// Downgrades cost materials by one tech level for negative traits when adding.
    /// Skipped for removal — removing a negative trait requires proper-tier materials
    /// to restore the weapon to standard quality.
    /// </summary>
    public class NegativeDowngradeWorker : TraitCostRuleWorker
    {
        private static Dictionary<ThingDef, ThingDef> downgrades;

        public override bool Matches(HashSet<string> labelWords, WeaponTraitDef trait)
        {
            return TraitCostUtility.IsNegativeTrait(trait);
        }

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            if (isRemoval)
                return;
            if (downgrades == null)
            {
                downgrades = new Dictionary<ThingDef, ThingDef>
                {
                    { ThingDefOf.ComponentSpacer, ThingDefOf.ComponentIndustrial },
                    { ThingDefOf.Plasteel, ThingDefOf.Steel },
                    { ThingDefOf.ComponentIndustrial, ThingDefOf.Steel },
                    { ThingDefOf.Steel, ThingDefOf.WoodLog },
                };
            }

            var downgraded = new List<ThingDefCountClass>();
            foreach (ThingDefCountClass cost in costs)
            {
                ThingDef mat = downgrades.TryGetValue(cost.thingDef, out ThingDef replacement)
                    ? replacement
                    : cost.thingDef;
                CostRuleHelpers.AddOrMerge(downgraded, mat, cost.count);
            }

            costs.Clear();
            costs.AddRange(downgraded);
        }
    }

    /// <summary>
    /// Prunes the cost list to at most 3 material types by removing the cheapest
    /// entries (by unit market value) until the limit is met. Prevents UI overflow
    /// from too many cost icons.
    /// </summary>
    public class CostPruneWorker : TraitCostRuleWorker
    {
        private const int MaxMaterialTypes = 3;

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            while (costs.Count > MaxMaterialTypes)
            {
                int cheapestIndex = 0;
                float cheapestValue = costs[0].thingDef.BaseMarketValue;
                for (int i = 1; i < costs.Count; i++)
                {
                    float value = costs[i].thingDef.BaseMarketValue;
                    if (value < cheapestValue)
                    {
                        cheapestValue = value;
                        cheapestIndex = i;
                    }
                }
                costs.RemoveAt(cheapestIndex);
            }
        }
    }

    /// <summary>
    /// Fallback: auto-detects material names in the trait label and swaps raw resource
    /// costs with the detected material. Runs at low priority after all thematic rules.
    /// </summary>
    public class MaterialOverrideWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            ThingDef overrideMaterial = CostRuleHelpers.GetMaterialOverride(trait);
            if (overrideMaterial != null)
                CostRuleHelpers.ApplyMaterialOverride(costs, overrideMaterial);
        }
    }
}
