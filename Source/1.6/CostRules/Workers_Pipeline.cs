using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Populates the initial cost list from the weapon's crafting recipe.
    /// </summary>
    public class BaseCostWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            ThingDef baseDef = WeaponCustomizationUtility.IsUniqueWeapon(weapon.def)
                ? WeaponCustomizationUtility.GetBaseVariant(weapon.def)
                : weapon.def;
            if (baseDef == null)
                return;

            if (baseDef.costList != null)
            {
                foreach (ThingDefCountClass entry in baseDef.costList)
                    costs.Add(new ThingDefCountClass(entry.thingDef, entry.count));
            }

            if (baseDef.costStuffCount > 0)
            {
                ThingDef stuff = weapon.Stuff
                    ?? GenStuff.DefaultStuffFor(baseDef)
                    ?? ThingDefOf.Steel;
                costs.Add(new ThingDefCountClass(stuff, baseDef.costStuffCount));
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
                { QualityCategory.Awful, 0.5f },
                { QualityCategory.Poor, 0.75f },
                { QualityCategory.Normal, 1.0f },
                { QualityCategory.Good, 1.25f },
                { QualityCategory.Excellent, 1.5f },
                { QualityCategory.Masterwork, 2.0f },
                { QualityCategory.Legendary, 2.5f },
            };

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
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
    /// Fallback: auto-detects material names in the trait label and swaps raw resource
    /// costs with the detected material. Runs at low priority after all thematic rules.
    /// </summary>
    public class MaterialOverrideWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            ThingDef overrideMaterial = CostRuleHelpers.GetMaterialOverride(trait);
            if (overrideMaterial != null)
                CostRuleHelpers.ApplyMaterialOverride(costs, overrideMaterial);
        }
    }
}
