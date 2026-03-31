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

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
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
        protected override int ComponentMultiplier => 10;

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            // Fold spacer components into industrial so the base swap catches both
            ThingDefCountClass spacer = costs.Find(c => c.thingDef == CostRuleHelpers.ComponentSpacer);
            if (spacer != null)
            {
                int count = spacer.count;
                costs.Remove(spacer);
                CostRuleHelpers.AddOrMerge(costs, CostRuleHelpers.ComponentIndustrial, count);
            }
            base.Apply(costs, weapon, trait, isRemoval);
        }
    }

    // --- Value split (shared base) ---

    public abstract class ValueSplitWorker : TraitCostRuleWorker
    {
        protected abstract ThingDef Replacement { get; }
        protected virtual float SplitFraction => 0.7f;

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.ApplyValueSplit(costs, Replacement, SplitFraction);
        }
    }

    public class EmpSplitWorker : ValueSplitWorker
    {
        protected override ThingDef Replacement => CostRuleHelpers.ComponentIndustrial;

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.RemoveMaterials(costs, CostRuleHelpers.ComponentSpacer);
            base.Apply(costs, weapon, trait, isRemoval);
        }
    }

    public class FlareSplitWorker : ValueSplitWorker
    {
        protected override ThingDef Replacement => CostRuleHelpers.Bioferrite;
    }

    // --- Standalone workers ---

    public class PartialSwapWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            if (CostRuleHelpers.Birdskin != null)
                CostRuleHelpers.ApplyPartialSwapByCount(
                    costs, CostRuleHelpers.WoodLog, CostRuleHelpers.Birdskin, 0.4f);
        }
    }

    public class FlatCostWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.ApplyFlatCost(costs, CostRuleHelpers.SteelSlagChunk, 1);
        }
    }

    public class ConvertToSpacerWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.ApplyConvertAllToSpacer(costs);
        }
    }

    public class CostMultiplierWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.ApplyCostMultiplier(costs, 2f);
        }
    }

    public class GripWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.RemoveMaterials(costs,
                CostRuleHelpers.ComponentIndustrial, CostRuleHelpers.ComponentSpacer);
        }
    }

    public class InlayWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.RemoveMaterials(costs,
                CostRuleHelpers.ComponentIndustrial, CostRuleHelpers.ComponentSpacer);
        }
    }

    public class ComfortWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.ConvertHalfByCount(costs, CostRuleHelpers.Thrumbofur);
        }
    }

    public class OrnamentalWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            CostRuleHelpers.RemoveMaterials(costs,
                CostRuleHelpers.ComponentIndustrial, CostRuleHelpers.ComponentSpacer);
            CostRuleHelpers.ConvertHalfByCount(costs, CostRuleHelpers.Silver);
        }
    }
}
