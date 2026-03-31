using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // --- Component swap or split (shared base) ---

    public abstract class ComponentSwapOrSplitWorker : TraitCostRuleWorker
    {
        protected abstract ThingDef Replacement { get; }
        protected abstract int ComponentMultiplier { get; }
        protected virtual float SplitFraction => 0.7f;

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, Replacement, ComponentMultiplier, SplitFraction);
        }
    }

    public class ToxSwapWorker : ComponentSwapOrSplitWorker
    {
        protected override ThingDef Replacement => CostRuleHelpers.HerbalMedicine;
        protected override int ComponentMultiplier => 3;
    }

    public class IncendiarySwapWorker : ComponentSwapOrSplitWorker
    {
        protected override ThingDef Replacement => CostRuleHelpers.Chemfuel;
        protected override int ComponentMultiplier => 15;
    }

    // --- Value split (shared base) ---

    public abstract class ValueSplitWorker : TraitCostRuleWorker
    {
        protected abstract ThingDef Replacement { get; }
        protected virtual float SplitFraction => 0.7f;

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            CostRuleHelpers.ApplyValueSplit(costs, Replacement, SplitFraction);
        }
    }

    public class EmpSplitWorker : ValueSplitWorker
    {
        protected override ThingDef Replacement => CostRuleHelpers.ComponentIndustrial;
    }

    public class FlareSplitWorker : ValueSplitWorker
    {
        protected override ThingDef Replacement => CostRuleHelpers.Bioferrite;
    }

    // --- Standalone workers ---

    public class PartialSwapWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            if (CostRuleHelpers.Birdskin != null)
                CostRuleHelpers.ApplyPartialSwapByCount(
                    costs, CostRuleHelpers.WoodLog, CostRuleHelpers.Birdskin, 0.4f);
        }
    }

    public class FlatCostWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            CostRuleHelpers.ApplyFlatCost(costs, CostRuleHelpers.SteelSlagChunk, 1);
        }
    }

    public class ConvertToSpacerWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            CostRuleHelpers.ApplyConvertAllToSpacer(costs);
        }
    }

    public class CostMultiplierWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            CostRuleHelpers.ApplyCostMultiplier(costs, 2f);
        }
    }
}
