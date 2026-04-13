using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
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
