using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Scales all costs by the rule's costFactor (rounded up). Covers both
    // surcharges (akimbo 2x) and discounts (undersized 0.65x); a factor below 1
    // never rounds a material away entirely.
    public class CostFactorWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.ApplyCostMultiplier(costs, def.costFactor);
        }
    }
}
