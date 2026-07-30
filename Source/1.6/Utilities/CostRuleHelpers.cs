using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Static helper methods and material caches used by trait cost rule
    // workers. Extracted from TraitCostUtility to be accessible to the worker
    // class hierarchy.
    public static class CostRuleHelpers
    {
        // Material lookup caches
        private static Dictionary<string, ThingDef> materialsByLabel;
        private static HashSet<ThingDef> rawResources;

        // Cached ThingDefs for cost rule workers
        public static ThingDef HerbalMedicine { get; private set; }
        public static ThingDef Chemfuel { get; private set; }
        public static ThingDef ComponentIndustrial { get; private set; }
        public static ThingDef ComponentSpacer { get; private set; }
        public static ThingDef WoodLog { get; private set; }
        public static ThingDef Steel { get; private set; }
        public static ThingDef Plasteel { get; private set; }
        public static ThingDef Birdskin { get; private set; }
        public static ThingDef Bioferrite { get; private set; }
        public static ThingDef SteelSlagChunk { get; private set; }
        public static ThingDef Thrumbofur { get; private set; }
        public static ThingDef Silver { get; private set; }

        // Builds the raw resource and material label caches and resolves
        // ThingDefs. Must be called during StaticConstructorOnStartup (after
        // all defs are loaded).
        public static void Initialize()
        {
            materialsByLabel = new Dictionary<string, ThingDef>();
            rawResources = new HashSet<ThingDef>();

            ThingCategoryDef resourcesRaw =
                DefDatabase<ThingCategoryDef>.GetNamedSilentFail("ResourcesRaw");

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                try
                {
                    if (def.IsStuff || IsInCategory(def, resourcesRaw))
                        rawResources.Add(def);
                }
                catch (Exception ex)
                {
                    Log.Error("[Unique Weapons Unbound] Skipped raw-resource scan for "
                        + def.SourceForLog() + " due to error: " + ex);
                }
            }

            foreach (ThingDef def in rawResources)
            {
                try
                {
                    RegisterMaterialLabels(def);
                }
                catch (Exception ex)
                {
                    Log.Error("[Unique Weapons Unbound] Skipped material label cache for "
                        + def.SourceForLog() + " due to error: " + ex);
                }
            }

            HerbalMedicine = ThingDefOf.MedicineHerbal;
            Chemfuel = ThingDefOf.Chemfuel;
            ComponentIndustrial = ThingDefOf.ComponentIndustrial;
            ComponentSpacer = ThingDefOf.ComponentSpacer;
            WoodLog = ThingDefOf.WoodLog;
            Steel = ThingDefOf.Steel;
            Plasteel = ThingDefOf.Plasteel;
            Birdskin = DefDatabase<ThingDef>.GetNamedSilentFail("Leather_Bird");
            Bioferrite = DefDatabase<ThingDef>.GetNamedSilentFail("Bioferrite");
            SteelSlagChunk = DefDatabase<ThingDef>.GetNamedSilentFail("ChunkSlagSteel");
            Thrumbofur = DefDatabase<ThingDef>.GetNamedSilentFail("Leather_Thrumbo");
            Silver = ThingDefOf.Silver;
        }

        private static void RegisterMaterialLabels(ThingDef def)
        {
            string label = def.label?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(label) && label.Length >= 3)
                materialsByLabel[label] = def;

            string defName = def.defName?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(defName) && defName.Length >= 3
                && !materialsByLabel.ContainsKey(defName))
                materialsByLabel[defName] = def;
        }

        // Splits a trait label into a word set containing both the full
        // space-delimited words and the hyphen-delimited parts of any
        // hyphenated words. E.g. "crypto-coated rails" → {"crypto-coated",
        // "rails", "crypto", "coated"}.
        public static HashSet<string> SplitLabelWords(string label)
        {
            var words = new HashSet<string>();
            if (string.IsNullOrEmpty(label))
                return words;

            foreach (string word in label.ToLowerInvariant().Split(' '))
            {
                words.Add(word);
                if (word.Contains("-"))
                {
                    foreach (string part in word.Split('-'))
                    {
                        if (part.Length > 0)
                            words.Add(part);
                    }
                }
            }

            return words;
        }

        // Splits a defName into a lowercased word set so rules still match when
        // the trait label is fully localized. A leading underscore-delimited
        // segment is a mod-prefix acronym and is dropped, so "AArmoury_Oversized"
        // → {"oversized"} and no rule can match on the prefix itself. DefNames
        // without an underscore follow the vanilla convention and are kept whole.
        // The remainder splits on PascalCase boundaries and on any non-letter
        // character: "EMPBlaster" → {"emp", "blaster"}, "ChargeRifle2X" →
        // {"charge", "rifle", "x"}.
        public static HashSet<string> SplitDefNameWords(string defName)
        {
            var words = new HashSet<string>();
            if (string.IsNullOrEmpty(defName))
                return words;

            int prefixEnd = defName.IndexOf('_');
            string body = prefixEnd >= 0 ? defName.Substring(prefixEnd + 1) : defName;
            if (string.IsNullOrEmpty(body))
                return words;

            var token = new System.Text.StringBuilder(body.Length);

            for (int i = 0; i < body.Length; i++)
            {
                char c = body[i];

                if (!char.IsLetter(c))
                {
                    FlushToken(token, words);
                    continue;
                }

                // Start a new token at a lower→upper transition, and at the last
                // uppercase of an acronym run when a lowercase letter follows
                // (so "EMPBlaster" breaks between "EMP" and "Blaster").
                if (token.Length > 0 && char.IsUpper(c))
                {
                    char prev = token[token.Length - 1];
                    bool endsAcronymRun = char.IsUpper(prev)
                        && i + 1 < body.Length && char.IsLower(body[i + 1]);
                    if (!char.IsUpper(prev) || endsAcronymRun)
                        FlushToken(token, words);
                }

                token.Append(c);
            }

            FlushToken(token, words);
            return words;
        }

        private static void FlushToken(System.Text.StringBuilder token, HashSet<string> words)
        {
            if (token.Length == 0)
                return;

            words.Add(token.ToString().ToLowerInvariant());
            token.Length = 0;
        }

        // Divides WorkToMake into a complexity figure that tracks vanilla
        // component counts (a value of 1 is roughly one component's worth of
        // build effort). Deliberately a hardcoded constant rather than a mod
        // setting.
        private const float ComplexityWorkDivisor = 6000f;

        // Ceiling on the rarity multiplier (RarityMultiplierWorker): the rarest
        // trait pays at most double the base bill. Deliberately a hardcoded
        // constant rather than a mod setting, same as the divisor above. Kept
        // conservative because the multiplier is a heuristic — a
        // structurally-rare-but-mild trait is overpriced by at most 2x.
        public const float RarityCapMax = 2f;

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

        // Normalizes a replacement material through its industrial/spacer pair
        // by the weapon's tech level. Single-tier materials (herbal medicine,
        // chemfuel, bioferrite) pass through unchanged.
        private static ThingDef SelectSignatureMaterial(Thing weapon, ThingDef replacement)
        {
            if (replacement == ComponentIndustrial || replacement == ComponentSpacer)
                return SelectByTechLevel(weapon, ComponentIndustrial, ComponentSpacer);

            if (replacement == Steel || replacement == Plasteel)
                return SelectByTechLevel(weapon, Steel, Plasteel);

            return replacement;
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
        // below what the floor billed before.
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
                float complexity = GetWeaponComplexity(weapon);
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

        // Returns the material override ThingDef if the trait label contains a
        // known raw resource name (e.g., "gold inlay" matches Gold). Returns
        // null otherwise.
        public static ThingDef GetMaterialOverride(WeaponTraitDef trait)
        {
            if (materialsByLabel == null || materialsByLabel.Count == 0)
                return null;

            ThingDef match = TryMatchWords(SplitLabelWords(trait.label));
            if (match != null)
                return match;

            return TryMatchWords(SplitDefNameWords(trait.defName));
        }

        // Replaces all raw resource costs with an equal market value of the
        // override material (rounded up, minimum 1). Non-raw costs (e.g.
        // components) pass through unchanged.
        public static void ApplyMaterialOverride(
            List<ThingDefCountClass> costs, ThingDef overrideMaterial)
        {
            int rawCount = 0;
            float rawValue = 0f;
            var result = new List<ThingDefCountClass>();

            foreach (ThingDefCountClass cost in costs)
            {
                if (IsRawResource(cost.thingDef))
                {
                    rawCount += cost.count;
                    rawValue += cost.count * cost.thingDef.BaseMarketValue;
                }
                else
                {
                    result.Add(cost);
                }
            }

            if (rawCount > 0)
            {
                // A valueless override material can't be priced by value, so
                // fall back to the old 1:1-by-count conversion.
                int overrideCount = overrideMaterial.BaseMarketValue > 0f
                    ? Mathf.Max(1, Mathf.CeilToInt(rawValue / overrideMaterial.BaseMarketValue))
                    : rawCount;
                result.Insert(0, new ThingDefCountClass(overrideMaterial, overrideCount));
            }

            costs.Clear();
            costs.AddRange(result);
        }

        // Returns true if the given ThingDef is a raw resource (stuff or in
        // ResourcesRaw category).
        public static bool IsRawResource(ThingDef def)
        {
            return rawResources != null && rawResources.Contains(def);
        }

        // Selects a bill material by the weapon's tech level: Industrial and
        // below (including Undefined) take the industrial-tier def, Spacer and
        // above take the spacer-tier def. Used where a material has a natural
        // industrial/spacer pair (ComponentIndustrial/ComponentSpacer,
        // Steel/Plasteel).
        public static ThingDef SelectByTechLevel(
            Thing weapon, ThingDef industrialDef, ThingDef spacerDef)
        {
            return weapon != null && weapon.def.techLevel >= TechLevel.Spacer
                ? spacerDef
                : industrialDef;
        }

        // Three-tier variant for materials with a low/industrial/ultra ladder
        // (e.g. herbal/industrial/ultratech medicine). Medieval and below
        // (including Undefined) take the low tier, Industrial the mid tier,
        // Spacer and above the high tier.
        public static ThingDef SelectByTechLevel(
            Thing weapon, ThingDef lowDef, ThingDef industrialDef, ThingDef spacerDef)
        {
            TechLevel tech = weapon?.def.techLevel ?? TechLevel.Undefined;
            if (tech >= TechLevel.Spacer)
                return spacerDef;
            return tech == TechLevel.Industrial ? industrialDef : lowDef;
        }

        // Finds the material whose cached label/defName key matches one of the
        // given tokens, longest token winning. Tokens are expected to already be
        // lowercased (both SplitLabelWords and SplitDefNameWords do so).
        private static ThingDef TryMatchWords(ICollection<string> words)
        {
            if (words == null || words.Count == 0)
                return null;

            ThingDef bestMatch = null;
            int bestLength = 0;

            foreach (string word in words)
            {
                if (word.Length < 3)
                    continue;

                if (materialsByLabel.TryGetValue(word, out ThingDef mat) && word.Length > bestLength)
                {
                    bestMatch = mat;
                    bestLength = word.Length;
                }
            }

            return bestMatch;
        }

        private static bool IsInCategory(ThingDef def, ThingCategoryDef targetCategory)
        {
            if (targetCategory == null || def.thingCategories == null)
                return false;

            foreach (ThingCategoryDef cat in def.thingCategories)
            {
                ThingCategoryDef current = cat;
                while (current != null)
                {
                    if (current == targetCategory)
                        return true;
                    current = current.parent;
                }
            }

            return false;
        }
    }
}
