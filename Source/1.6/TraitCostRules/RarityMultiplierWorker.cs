using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Scales the base bill by how rare the trait is. commonality is the
    // selection weight vanilla rolls traits with, so 1 / commonality is roughly
    // how many rolls it takes to see the trait relative to a common one — the
    // only per-trait power signal the corpora actually carry. Floor-only by
    // design: the multiplier is clamped to [1, RarityCapMax], so a common trait
    // pays the plain bill and nothing here is ever a discount.
    //
    // Runs at priority 250, ahead of every thematic rule, so the by-value
    // conversions and material overrides inherit the scaled base instead of
    // being scaled themselves. That also keeps refunds symmetric for free: the
    // rule is inside RunPipeline, which both the addition and the removal path
    // go through.
    public class RarityMultiplierWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            float multiplier = GetRarityMultiplier(trait);
            if (multiplier == 1f)
                return;

            foreach (ThingDefCountClass cost in costs)
                cost.count = Mathf.CeilToInt(cost.count * multiplier);
        }

        // 1 / commonality, clamped into [1, RarityCapMax]. The cap is the
        // "rare trait cost cap" mod setting; at its minimum of 1 the clamp
        // prices every trait as common, turning the rule off.
        //
        // Negative traits are exempt: for them commonality weights how often a
        // downgrade shows up, not how strong the trait is, and a rare drawback
        // costing double to bolt on is backwards (vanilla Ugly, UMW Carbonized).
        //
        // commonality <= 0 is a misconfigured def — vanilla's own ConfigErrors
        // flags it — so it prices as common rather than dividing by zero.
        public static float GetRarityMultiplier(WeaponTraitDef trait)
        {
            if (trait == null || trait.commonality <= 0f)
                return 1f;
            if (TraitCostUtility.IsNegativeTrait(trait))
                return 1f;

            return Mathf.Clamp(1f / trait.commonality, 1f, CostRuleHelpers.RarityCapMax);
        }
    }
}
