using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Downgrades cost materials by one tech level for negative traits when
    // adding. Skipped for removal — removing a negative trait requires
    // proper-tier materials to restore the weapon to standard quality.
    public class NegativeDowngradeWorker : TraitCostRuleWorker
    {
        // Rebuilt in OnStartup on every play-data load: ThingDefOf fields are
        // rebound to fresh instances on an in-process reload, so a map built
        // once per process would keep dead defs and silently stop matching.
        private static Dictionary<ThingDef, ThingDef> downgrades;

        public override bool Matches(HashSet<string> labelWords, WeaponTraitDef trait)
        {
            return TraitCostUtility.IsNegativeTrait(trait);
        }

        public override void OnStartup()
        {
            downgrades = BuildDowngrades();
        }

        private static Dictionary<ThingDef, ThingDef> BuildDowngrades()
        {
            return new Dictionary<ThingDef, ThingDef>
            {
                { ThingDefOf.ComponentSpacer, ThingDefOf.ComponentIndustrial },
                { ThingDefOf.Plasteel, ThingDefOf.Steel },
                { ThingDefOf.ComponentIndustrial, ThingDefOf.Steel },
                { ThingDefOf.Steel, ThingDefOf.WoodLog },
            };
        }

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            if (isRemoval)
                return;
            // Lazy fallback for direct instantiation outside the pipeline
            // (tests); production always passes through OnStartup first.
            if (downgrades == null)
                downgrades = BuildDowngrades();

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
}
