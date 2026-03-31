using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Orchestrates trait cost calculation by running data-driven rules from
    /// <see cref="TraitCostRuleDef"/> in priority order. Provides the public API
    /// for addition costs, removal costs/refunds, and negative trait detection.
    /// The pipeline supports asymmetric costs: some rules (e.g. NegativeDowngrade)
    /// behave differently for addition vs removal context.
    /// </summary>
    public static class TraitCostUtility
    {
        /// <summary>
        /// Fraction of the trait's addition cost returned when removing a trait.
        /// Reads from mod settings; falls back to 0.5 if settings are not yet loaded.
        /// </summary>
        public static float RefundFraction => UWU_Mod.Settings?.refundFraction ?? 0.5f;

        private static readonly Dictionary<(ThingDef, ThingDef, float, WeaponTraitDef, bool), List<ThingDefCountClass>> costCache =
            new Dictionary<(ThingDef, ThingDef, float, WeaponTraitDef, bool), List<ThingDefCountClass>>();

        private static List<TraitCostRuleDef> cachedRules;

        /// <summary>
        /// Initializes material caches and builds the sorted rule list from DefDatabase.
        /// Must be called during StaticConstructorOnStartup (after all defs are loaded).
        /// </summary>
        public static void Initialize()
        {
            CostRuleHelpers.Initialize();
            costCache.Clear();

            cachedRules = DefDatabase<TraitCostRuleDef>.AllDefs
                .OrderBy(d => d.priority)
                .ToList();

            Log.Message($"[Unique Weapons Unbound] Loaded {cachedRules.Count} trait cost rules.");
        }

        /// <summary>
        /// Calculates the base resource cost of a trait for addition context.
        /// Runs all matching rules in priority order with isRemoval=false.
        /// </summary>
        public static List<ThingDefCountClass> GetTraitCost(Thing weapon, WeaponTraitDef trait)
        {
            return RunPipeline(weapon, trait, isRemoval: false);
        }

        /// <summary>
        /// Returns true if the trait is "negative" (undesirable), detected by a
        /// MarketValue stat factor below 1.0. Negative traits have inverted costs:
        /// cheaper to add (RefundFraction), and cost RefundFraction to remove.
        /// </summary>
        public static bool IsNegativeTrait(WeaponTraitDef trait)
        {
            if (trait.statFactors == null)
                return false;
            for (int i = 0; i < trait.statFactors.Count; i++)
            {
                if (trait.statFactors[i].stat == StatDefOf.MarketValue
                    && trait.statFactors[i].value < 1f)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the per-trait cost of ADDING this trait. For negative traits,
        /// the cost is reduced by RefundFraction (nobody pays full price for a downgrade).
        /// Uses the addition pipeline (materials may be downgraded for negative traits).
        /// </summary>
        public static List<ThingDefCountClass> GetAdditionCost(Thing weapon, WeaponTraitDef trait)
        {
            List<ThingDefCountClass> costs = RunPipeline(weapon, trait, isRemoval: false);
            if (IsNegativeTrait(trait))
            {
                foreach (ThingDefCountClass c in costs)
                    c.count = Mathf.CeilToInt(c.count * RefundFraction);
                costs.RemoveAll(c => c.count <= 0);
            }
            return costs;
        }

        /// <summary>
        /// Returns the per-trait result of REMOVING this trait.
        /// For positive traits: materials the player receives back (refund, base * RefundFraction).
        /// For negative traits: materials the player must PAY (cost, base * RefundFraction).
        /// Uses the removal pipeline (original-tier materials preserved for negative traits).
        /// Call <see cref="IsNegativeTrait"/> to determine whether the result is a refund or a cost.
        /// </summary>
        public static List<ThingDefCountClass> GetRemovalCost(Thing weapon, WeaponTraitDef trait)
        {
            List<ThingDefCountClass> costs = RunPipeline(weapon, trait, isRemoval: true);
            foreach (ThingDefCountClass c in costs)
                c.count = IsNegativeTrait(trait)
                    ? Mathf.CeilToInt(c.count * RefundFraction)
                    : Mathf.FloorToInt(c.count * RefundFraction);
            costs.RemoveAll(c => c.count <= 0);
            return costs;
        }

        /// <summary>
        /// Calculates the total resource cost across all additions plus any negative
        /// trait removals (which cost resources instead of refunding them).
        /// </summary>
        public static List<ThingDefCountClass> GetTotalCost(
            Thing weapon, IEnumerable<WeaponTraitDef> traitsToAdd,
            IEnumerable<WeaponTraitDef> traitsToRemove = null)
        {
            var totals = new Dictionary<ThingDef, int>();

            foreach (WeaponTraitDef trait in traitsToAdd)
            {
                foreach (ThingDefCountClass cost in GetAdditionCost(weapon, trait))
                {
                    if (totals.ContainsKey(cost.thingDef))
                        totals[cost.thingDef] += cost.count;
                    else
                        totals[cost.thingDef] = cost.count;
                }
            }

            if (traitsToRemove != null)
            {
                foreach (WeaponTraitDef trait in traitsToRemove)
                {
                    if (!IsNegativeTrait(trait))
                        continue;
                    foreach (ThingDefCountClass cost in GetRemovalCost(weapon, trait))
                    {
                        if (totals.ContainsKey(cost.thingDef))
                            totals[cost.thingDef] += cost.count;
                        else
                            totals[cost.thingDef] = cost.count;
                    }
                }
            }

            return totals.Select(kv => new ThingDefCountClass(kv.Key, kv.Value)).ToList();
        }

        /// <summary>
        /// Calculates the total resource refund for removing traits from the given weapon.
        /// Only positive (non-negative) traits produce refunds. Negative trait removals
        /// cost resources instead and are included in <see cref="GetTotalCost"/>.
        /// Aggregates raw costs across positive traits first, then applies RefundFraction
        /// and floors once per material to avoid cumulative rounding loss.
        /// </summary>
        public static List<ThingDefCountClass> GetTotalRefund(
            Thing weapon, IEnumerable<WeaponTraitDef> traits)
        {
            var totals = new Dictionary<ThingDef, int>();
            foreach (WeaponTraitDef trait in traits)
            {
                if (IsNegativeTrait(trait))
                    continue;
                foreach (ThingDefCountClass cost in RunPipeline(weapon, trait, isRemoval: true))
                {
                    if (totals.ContainsKey(cost.thingDef))
                        totals[cost.thingDef] += cost.count;
                    else
                        totals[cost.thingDef] = cost.count;
                }
            }

            var raw = totals.Select(kv => new ThingDefCountClass(kv.Key, kv.Value)).ToList();
            foreach (ThingDefCountClass entry in raw)
                entry.count = Mathf.FloorToInt(entry.count * RefundFraction);
            raw.RemoveAll(c => c.count <= 0);
            return raw;
        }

        private static List<ThingDefCountClass> RunPipeline(
            Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            ThingDef baseDef = WeaponCustomizationUtility.IsUniqueWeapon(weapon.def)
                ? WeaponCustomizationUtility.GetBaseVariant(weapon.def)
                : weapon.def;

            if (baseDef == null)
                return new List<ThingDefCountClass>();

            float qualityMult = QualityMultiplierWorker.GetQualityMultiplier(weapon);
            var cacheKey = (baseDef, weapon.Stuff, qualityMult, trait, isRemoval);
            if (costCache.TryGetValue(cacheKey, out List<ThingDefCountClass> cached))
                return CloneCosts(cached);

            var costs = new List<ThingDefCountClass>();
            HashSet<string> labelWords = CostRuleHelpers.SplitLabelWords(trait.label);

            foreach (TraitCostRuleDef ruleDef in cachedRules)
            {
                if (ruleDef.Worker.Matches(labelWords, trait))
                    ruleDef.Worker.Apply(costs, weapon, trait, isRemoval);
            }

            costs.RemoveAll(c => c.count <= 0);

            costCache[cacheKey] = CloneCosts(costs);
            return costs;
        }

        private static List<ThingDefCountClass> CloneCosts(List<ThingDefCountClass> costs)
        {
            var clone = new List<ThingDefCountClass>(costs.Count);
            foreach (ThingDefCountClass entry in costs)
                clone.Add(new ThingDefCountClass(entry.thingDef, entry.count));
            return clone;
        }
    }
}
