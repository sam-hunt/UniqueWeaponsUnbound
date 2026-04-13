using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Converts all costs into spacer components by market value.
    /// Used for charge, crypto, and other advanced spacer-tech traits.
    /// </summary>
    public class ConvertToSpacerWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.ApplyConvertAllToSpacer(costs);
        }
    }
}
