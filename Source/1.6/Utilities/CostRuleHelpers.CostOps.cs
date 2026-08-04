using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Cost-list mutation helpers operating on List<ThingDefCountClass> bills,
    // plus the weapon-complexity figure and the settings-backed knobs the
    // floors and caps read. Material resolution lives in the main file.
    public static partial class CostRuleHelpers
    {
        // Divides WorkToMake into a complexity figure that tracks vanilla
        // component counts (a value of 1 is roughly one component's worth of
        // build effort). Deliberately a hardcoded constant rather than a mod
        // setting — the floor scale setting below scales the floor's output
        // instead of redefining what one component's worth of effort is.
        private const float ComplexityWorkDivisor = 6000f;

        // Ceiling on the rarity multiplier (RarityMultiplierWorker), backed by
        // the "rare trait cost cap" slider; falls back to the shipped default
        // when settings are not yet loaded. The default of 2 is conservative
        // because the multiplier is a heuristic — a structurally-rare-but-mild
        // trait is overpriced by at most the cap — and the slider's minimum of
        // 1 pins every trait to the plain bill, disabling the rule.
        public static float RarityCapMax => UWU_Mod.Settings?.rarityCostCap ?? 2f;

        // Scale on the spacer conversion's complexity floor, backed by the
        // "advanced trait minimum cost" slider; falls back to 1 (the shipped
        // floor) when settings are not yet loaded. Scales both floor terms
        // together, so 0 removes the floor and restores pure by-value pricing.
        public static float ComplexityFloorScale => UWU_Mod.Settings?.complexityFloorScale ?? 1f;

        // Resolves the def a weapon's costs derive from: the base variant of a
        // unique weapon, falling back to the weapon's own def (base-def-less
        // unique weapons carry their own recipe and work value). Shared by
        // BaseCostFromRecipeWorker and the complexity branch below so the two
        // can never disagree about which def they are pricing.
        public static ThingDef ResolveCostBasisDef(Thing weapon)
        {
            if (weapon?.def == null)
                return null;

            ThingDef baseDef = WeaponRegistry.IsUniqueWeapon(weapon.def)
                ? WeaponRegistry.GetBaseVariant(weapon.def)
                : weapon.def;

            return baseDef ?? weapon.def;
        }

        // Stuff-independent measure of how involved a weapon is to build, used
        // in place of a component count when the cost list has no component
        // line to pivot on.
        public static float GetWeaponComplexity(Thing weapon)
        {
            ThingDef basisDef = ResolveCostBasisDef(weapon);
            if (basisDef == null)
                return 0f;

            return basisDef.GetStatValueAbstract(StatDefOf.WorkToMake) / ComplexityWorkDivisor;
        }

        // If components exist in the cost list (industrial first, spacer
        // second), replace them with multiplier * count of the replacement
        // material. Otherwise bill a complexity-derived signature count of the
        // replacement, additively, leaving the existing cost entries alone.
        public static void ApplyComponentSwapOrSplit(
            List<ThingDefCountClass> costs, Thing weapon, ThingDef replacement, int componentMultiplier)
        {
            if (replacement == null)
                return;

            ThingDefCountClass compEntry = costs.Find(c => c.thingDef == ComponentIndustrial)
                ?? costs.Find(c => c.thingDef == ComponentSpacer);

            if (compEntry != null)
            {
                int replacementCount = compEntry.count * componentMultiplier;
                costs.Remove(compEntry);
                AddOrMerge(costs, replacement, replacementCount);
                return;
            }

            int signatureCount = Mathf.Max(
                1, Mathf.CeilToInt(GetWeaponComplexity(weapon) * componentMultiplier));
            AddOrMerge(costs, SelectSignatureMaterial(weapon, replacement), signatureCount);
        }

        // Split off a fraction of wood/steel/plasteel and convert to the
        // replacement material by market value.
        public static void ApplyValueSplit(
            List<ThingDefCountClass> costs, ThingDef replacement, float splitFraction)
        {
            if (replacement == null)
                return;

            float splitValue = SplitBaseMaterials(costs, splitFraction);
            if (splitValue > 0f && replacement.BaseMarketValue > 0f)
                AddOrMerge(costs, replacement, Mathf.CeilToInt(splitValue / replacement.BaseMarketValue));
        }

        // Swap a fraction of source material count directly to the replacement
        // material (1:1 by count).
        public static void ApplyPartialSwapByCount(
            List<ThingDefCountClass> costs, ThingDef source, ThingDef replacement, float fraction)
        {
            ThingDefCountClass sourceEntry = costs.Find(c => c.thingDef == source);
            if (sourceEntry == null || sourceEntry.count <= 0)
                return;

            int swapAmount = Mathf.FloorToInt(sourceEntry.count * fraction);
            if (swapAmount <= 0)
                return;

            sourceEntry.count -= swapAmount;
            AddOrMerge(costs, replacement, swapAmount);
        }

        // Convert all non-spacer-component costs into spacer components by
        // market value (rounded up). A bill with no component line of its own
        // also takes a complexity floor: a cheap recipe would otherwise buy a
        // spacer-tech trait for a single component. The no-components condition
        // is the same one the complexity branch above uses, and it keeps ranged
        // weapons out of the floor's way — charge rifles (ComponentSpacer in
        // their recipe) and industrial guns (ComponentIndustrial) price purely
        // by value, exactly as they did before the floor existed.
        //
        // The floor rides the same multipliers as the bill it floors — the cost
        // fraction, quality (priority 200) and rarity (priority 250) — because
        // on cheap-stuff melee it always binds, and a floor computed from the
        // def alone would erase all three: a legendary steel longsword would
        // price exactly like an awful one. The outer max against the unscaled
        // complexity keeps it floor-only, so the multipliers can never discount
        // below what the floor billed before. ComplexityFloorScale multiplies
        // the complexity both terms share, so the setting moves the whole floor
        // together and 0 removes it.
        public static void ApplyConvertAllToSpacer(
            List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait)
        {
            if (ComponentSpacer == null || ComponentSpacer.BaseMarketValue <= 0f)
                return;

            float totalValue = 0f;
            int existingSpacerCount = 0;
            bool hasComponents = false;

            for (int i = costs.Count - 1; i >= 0; i--)
            {
                if (costs[i].thingDef == ComponentSpacer)
                {
                    existingSpacerCount += costs[i].count;
                    hasComponents = true;
                }
                else
                {
                    if (costs[i].thingDef == ComponentIndustrial)
                        hasComponents = true;
                    totalValue += costs[i].count * costs[i].thingDef.BaseMarketValue;
                }
                costs.RemoveAt(i);
            }

            int totalCount = existingSpacerCount
                + Mathf.CeilToInt(totalValue / ComponentSpacer.BaseMarketValue);
            if (!hasComponents)
            {
                float complexity = GetWeaponComplexity(weapon) * ComplexityFloorScale;
                int floor = Mathf.Max(
                    Mathf.CeilToInt(complexity),
                    Mathf.CeilToInt(complexity
                        * QualityMultiplierWorker.CostFraction
                        * QualityMultiplierWorker.GetQualityMultiplier(weapon)
                        * RarityMultiplierWorker.GetRarityMultiplier(trait)));
                totalCount = Mathf.Max(totalCount, floor);
            }

            if (totalCount > 0)
                costs.Add(new ThingDefCountClass(ComponentSpacer, totalCount));
        }

        // Replaces all costs with a single flat entry.
        public static void ApplyFlatCost(List<ThingDefCountClass> costs, ThingDef material, int count)
        {
            if (material == null)
                return;

            costs.Clear();
            costs.Add(new ThingDefCountClass(material, count));
        }

        // Multiplies all cost counts by the given factor (rounded up).
        public static void ApplyCostMultiplier(List<ThingDefCountClass> costs, float multiplier)
        {
            foreach (ThingDefCountClass cost in costs)
                cost.count = Mathf.CeilToInt(cost.count * multiplier);
        }

        // Removes a fraction of every raw resource entry from the cost list and
        // returns the total market value of what was removed. Stuff-agnostic, so
        // exotic stuffs (jade, gold, stony) split like wood and steel do; only
        // safe because signature counts no longer derive from the split value,
        // which across a tier gap produced nonsense amounts.
        public static float SplitBaseMaterials(List<ThingDefCountClass> costs, float fraction)
        {
            float splitValue = 0f;

            foreach (ThingDefCountClass cost in costs)
            {
                // Components pass IsRawResource (vanilla gives them stuffProps
                // purely for texture tinting), but they are the pipeline's pivot
                // currency with dedicated swap/removal paths — splitting them
                // here would reprice flare-style rules on every component
                // recipe. Split true base materials only.
                if (!IsRawResource(cost.thingDef)
                    || cost.thingDef == ComponentIndustrial
                    || cost.thingDef == ComponentSpacer)
                    continue;

                int splitAmount = Mathf.FloorToInt(cost.count * fraction);
                if (splitAmount <= 0)
                    continue;

                splitValue += splitAmount * cost.thingDef.BaseMarketValue;
                cost.count -= splitAmount;
            }

            return splitValue;
        }

        // Adds count to an existing entry for the given ThingDef, or creates a
        // new entry.
        public static void AddOrMerge(List<ThingDefCountClass> costs, ThingDef def, int count)
        {
            ThingDefCountClass existing = costs.Find(c => c.thingDef == def);
            if (existing != null)
                existing.count += count;
            else
                costs.Add(new ThingDefCountClass(def, count));
        }

        // Removes all entries matching the given ThingDefs from the cost list.
        public static void RemoveMaterials(List<ThingDefCountClass> costs, params ThingDef[] materials)
        {
            costs.RemoveAll(c =>
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    if (c.thingDef == materials[i])
                        return true;
                }
                return false;
            });
        }

        // Converts half (by count, floored) of every cost entry into the
        // replacement material.
        public static void ConvertHalfByCount(List<ThingDefCountClass> costs, ThingDef replacement)
        {
            if (replacement == null)
                return;

            int totalReplacement = 0;
            foreach (ThingDefCountClass cost in costs)
            {
                int half = Mathf.FloorToInt(cost.count * 0.5f);
                if (half <= 0)
                    continue;
                cost.count -= half;
                totalReplacement += half;
            }

            if (totalReplacement > 0)
                AddOrMerge(costs, replacement, totalReplacement);
        }
    }
}
