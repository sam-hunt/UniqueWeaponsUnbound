using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Calculates resource costs for adding weapon traits.
    /// Derives costs from the weapon's crafting recipe, applies a quality multiplier,
    /// and detects trait-specific material overrides by matching words in the trait's
    /// label against known raw resource ThingDefs.
    /// </summary>
    public static class TraitCostUtility
    {
        /// <summary>
        /// Fraction of the weapon's full recipe cost charged per trait addition.
        /// At 0.5, three traits total 1.5x the weapon's recipe cost.
        /// </summary>
        private const float CostFraction = 0.5f;

        /// <summary>
        /// Fraction of the trait's addition cost returned when removing a trait.
        /// Applied on top of the full addition cost (which already includes CostFraction and quality).
        /// Reads from mod settings; falls back to 0.5 if settings are not yet loaded.
        /// </summary>
        public static float RefundFraction => UWU_Mod.Settings?.refundFraction ?? 0.5f;

        private static readonly Dictionary<QualityCategory, float> QualityMultipliers =
            new Dictionary<QualityCategory, float>
            {
                { QualityCategory.Awful, 0.5f },
                { QualityCategory.Poor, 0.75f },
                { QualityCategory.Normal, 1.0f },
                { QualityCategory.Good, 1.25f },
                { QualityCategory.Excellent, 1.5f },
                { QualityCategory.Masterwork, 2.0f },
                { QualityCategory.Legendary, 2.5f },
            };

        private static Dictionary<string, ThingDef> materialsByLabel;
        private static HashSet<ThingDef> rawResources;
        private static readonly Dictionary<(ThingDef, ThingDef, float, WeaponTraitDef), List<ThingDefCountClass>> costCache =
            new Dictionary<(ThingDef, ThingDef, float, WeaponTraitDef), List<ThingDefCountClass>>();

        // Cached ThingDefs for thematic cost rules.
        private static ThingDef herbalMedicine;
        private static ThingDef chemfuel;
        private static ThingDef componentIndustrial;
        private static ThingDef componentSpacer;
        private static ThingDef woodLog;
        private static ThingDef steel;
        private static ThingDef plasteel;
        private static ThingDef birdskin;
        private static ThingDef bioferrite;
        private static ThingDef steelSlagChunk;

        /// <summary>
        /// Builds the raw resource and material label caches.
        /// Must be called during StaticConstructorOnStartup (after all defs are loaded).
        /// </summary>
        public static void Initialize()
        {
            costCache.Clear();
            materialsByLabel = new Dictionary<string, ThingDef>();
            rawResources = new HashSet<ThingDef>();

            ThingCategoryDef resourcesRaw =
                DefDatabase<ThingCategoryDef>.GetNamedSilentFail("ResourcesRaw");

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.IsStuff || IsInCategory(def, resourcesRaw))
                    rawResources.Add(def);
            }

            foreach (ThingDef def in rawResources)
            {
                string label = def.label?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(label) && label.Length >= 3)
                    materialsByLabel[label] = def;

                string defName = def.defName?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(defName) && defName.Length >= 3
                    && !materialsByLabel.ContainsKey(defName))
                    materialsByLabel[defName] = def;
            }

            herbalMedicine = ThingDefOf.MedicineHerbal;
            chemfuel = ThingDefOf.Chemfuel;
            componentIndustrial = ThingDefOf.ComponentIndustrial;
            componentSpacer = ThingDefOf.ComponentSpacer;
            woodLog = ThingDefOf.WoodLog;
            steel = ThingDefOf.Steel;
            plasteel = ThingDefOf.Plasteel;
            birdskin = DefDatabase<ThingDef>.GetNamedSilentFail("Leather_Bird");
            bioferrite = DefDatabase<ThingDef>.GetNamedSilentFail("Bioferrite");
            steelSlagChunk = DefDatabase<ThingDef>.GetNamedSilentFail("ChunkSlagSteel");

            Log.Message($"[Unique Weapons Unbound] Cached {rawResources.Count} raw resources, " +
                $"{materialsByLabel.Count} material labels.");
        }

        /// <summary>
        /// Calculates the resource cost of adding the given trait to the given weapon.
        /// Returns an empty list if the weapon has no determinable recipe cost.
        /// </summary>
        public static List<ThingDefCountClass> GetTraitCost(Thing weapon, WeaponTraitDef trait)
        {
            ThingDef baseDef = WeaponCustomizationUtility.IsUniqueWeapon(weapon.def)
                ? WeaponCustomizationUtility.GetBaseVariant(weapon.def)
                : weapon.def;

            if (baseDef == null)
                return new List<ThingDefCountClass>();

            float qualityMult = GetQualityMultiplier(weapon);
            var cacheKey = (baseDef, weapon.Stuff, qualityMult, trait);
            if (costCache.TryGetValue(cacheKey, out List<ThingDefCountClass> cached))
                return CloneCosts(cached);

            // TODO: Decide how to handle uncraftable weapons (no costList/costStuffCount).
            // Currently returns empty list — caller decides whether to show "free" or block.
            List<ThingDefCountClass> costs = GetBaseCosts(weapon, baseDef);
            if (costs.Count == 0)
                return costs;

            float totalMult = CostFraction * qualityMult;

            foreach (ThingDefCountClass cost in costs)
                cost.count = Mathf.CeilToInt(cost.count * totalMult);

            if (!ApplyThematicCostRules(costs, trait))
            {
                ThingDef overrideMaterial = GetMaterialOverride(trait);
                if (overrideMaterial != null)
                    costs = ApplyMaterialOverride(costs, overrideMaterial);
            }

            costs.RemoveAll(c => c.count <= 0);

            costCache[cacheKey] = CloneCosts(costs);
            return costs;
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
        /// </summary>
        public static List<ThingDefCountClass> GetAdditionCost(Thing weapon, WeaponTraitDef trait)
        {
            List<ThingDefCountClass> costs = GetTraitCost(weapon, trait);
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
        /// Both directions use RefundFraction — the symmetry keeps removal costs fair.
        /// Call <see cref="IsNegativeTrait"/> to determine whether the result is a refund or a cost.
        /// </summary>
        public static List<ThingDefCountClass> GetRemovalCost(Thing weapon, WeaponTraitDef trait)
        {
            List<ThingDefCountClass> costs = GetTraitCost(weapon, trait);
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
            List<ThingDefCountClass> raw = AggregateCosts(weapon,
                traits.Where(t => !IsNegativeTrait(t)));
            foreach (ThingDefCountClass entry in raw)
                entry.count = Mathf.FloorToInt(entry.count * RefundFraction);
            raw.RemoveAll(c => c.count <= 0);
            return raw;
        }

        private static List<ThingDefCountClass> AggregateCosts(
            Thing weapon, IEnumerable<WeaponTraitDef> traits)
        {
            var totals = new Dictionary<ThingDef, int>();

            foreach (WeaponTraitDef trait in traits)
            {
                foreach (ThingDefCountClass cost in GetTraitCost(weapon, trait))
                {
                    if (totals.ContainsKey(cost.thingDef))
                        totals[cost.thingDef] += cost.count;
                    else
                        totals[cost.thingDef] = cost.count;
                }
            }

            return totals.Select(kv => new ThingDefCountClass(kv.Key, kv.Value)).ToList();
        }

        /// <summary>
        /// Returns the material override ThingDef if the trait label contains a known
        /// raw resource name (e.g., "gold inlay" matches Gold, "bioferrite burner"
        /// matches Bioferrite). Returns null if no material match is detected.
        /// </summary>
        public static ThingDef GetMaterialOverride(WeaponTraitDef trait)
        {
            if (materialsByLabel == null || materialsByLabel.Count == 0)
                return null;

            ThingDef match = TryMatchWords(trait.label);
            if (match != null)
                return match;

            return TryMatchWords(SplitPascalCase(trait.defName));
        }

        private static List<ThingDefCountClass> GetBaseCosts(Thing weapon, ThingDef baseDef)
        {
            var costs = new List<ThingDefCountClass>();

            if (baseDef.costList != null)
            {
                foreach (ThingDefCountClass entry in baseDef.costList)
                    costs.Add(new ThingDefCountClass(entry.thingDef, entry.count));
            }

            if (baseDef.costStuffCount > 0)
            {
                // TODO: Unique variants lose the Stuff reference from their base weapon.
                // Falling back to GenStuff.DefaultStuffFor may not match the original material.
                ThingDef stuff = weapon.Stuff
                    ?? GenStuff.DefaultStuffFor(baseDef)
                    ?? ThingDefOf.Steel;
                costs.Add(new ThingDefCountClass(stuff, baseDef.costStuffCount));
            }

            return costs;
        }

        private static float GetQualityMultiplier(Thing weapon)
        {
            if (!weapon.TryGetQuality(out QualityCategory quality))
                return 1f;

            return QualityMultipliers.TryGetValue(quality, out float mult) ? mult : 1f;
        }

        private static List<ThingDefCountClass> ApplyMaterialOverride(
            List<ThingDefCountClass> costs, ThingDef overrideMaterial)
        {
            // TODO: Consider a special case for inlay traits (e.g., "Gold Inlay", "Jade Inlay").
            // Thematically, an inlay should cost only the inlay material — not components or
            // advanced components. Could detect inlay traits by checking if the label contains
            // "inlay", then drop all non-raw entries instead of passing them through.
            int rawTotal = 0;
            var result = new List<ThingDefCountClass>();

            foreach (ThingDefCountClass cost in costs)
            {
                if (rawResources.Contains(cost.thingDef))
                    rawTotal += cost.count;
                else
                    result.Add(cost);
            }

            if (rawTotal > 0)
                result.Insert(0, new ThingDefCountClass(overrideMaterial, rawTotal));

            return result;
        }

        /// <summary>
        /// Applies trait-specific cost transformations based on label keywords.
        /// Returns true if a rule matched (and was applied), false otherwise.
        /// </summary>
        private static bool ApplyThematicCostRules(List<ThingDefCountClass> costs, WeaponTraitDef trait)
        {
            if (costs.Count == 0)
                return false;

            HashSet<string> words = SplitLabelWords(trait.label);

            if (words.Contains("tox") || words.Contains("toxic") || words.Contains("paralytic")
                || words.Contains("acid-injector"))
            {
                ApplyComponentSwapOrSplit(costs, herbalMedicine, 3, 0.7f);
                return true;
            }

            if (words.Contains("incendiary") || words.Contains("blast")
                || words.Contains("pitch-soaked"))
            {
                ApplyComponentSwapOrSplit(costs, chemfuel, 15, 0.7f);
                return true;
            }

            if (words.Contains("emp"))
            {
                ApplyValueSplit(costs, componentIndustrial, 0.7f);
                return true;
            }

            if (words.Contains("lightweight"))
            {
                if (birdskin != null)
                    ApplyPartialSwapByCount(costs, woodLog, birdskin, 0.4f);
                return true;
            }

            if (words.Contains("flarestriker"))
            {
                if (bioferrite != null)
                    ApplyValueSplit(costs, bioferrite, 0.7f);
                return true;
            }

            if (words.Contains("heavy") && words.Contains("scrap"))
            {
                ApplyFlatCost(costs, steelSlagChunk, 1);
                return true;
            }

            bool isChargeCategory = trait.weaponCategory != null
                && (trait.weaponCategory.defName == "Pistol"
                    || trait.weaponCategory.defName == "PulseCharge"
                    || trait.weaponCategory.defName == "BeamWeapon");

            if (((words.Contains("charge") || words.Contains("charger") || words.Contains("frequency"))
                    && isChargeCategory)
                || words.Contains("crypto") || words.Contains("capacitor")
                || words.Contains("ultracoils") || words.Contains("thump")
                || words.Contains("rail"))
            {
                ApplyConvertAllToSpacer(costs);
                return true;
            }

            if (words.Contains("akimbo"))
            {
                ApplyCostMultiplier(costs, 2f);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Splits a trait label into a word set containing both the full space-delimited
        /// words and the hyphen-delimited parts of any hyphenated words.
        /// E.g. "crypto-coated rails" → {"crypto-coated", "rails", "crypto", "coated"}.
        /// </summary>
        private static HashSet<string> SplitLabelWords(string label)
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

        /// <summary>
        /// Tox/incendiary rule: if components exist, replace them with multiplier * count
        /// of the replacement material. Otherwise, split off a fraction of wood/steel/plasteel
        /// and convert by market value.
        /// </summary>
        private static void ApplyComponentSwapOrSplit(
            List<ThingDefCountClass> costs, ThingDef replacement, int componentMultiplier, float splitFraction)
        {
            if (replacement == null)
                return;

            ThingDefCountClass compEntry = costs.Find(c => c.thingDef == componentIndustrial);

            if (compEntry != null)
            {
                int replacementCount = compEntry.count * componentMultiplier;
                costs.Remove(compEntry);
                AddOrMerge(costs, replacement, replacementCount);
            }
            else
            {
                float splitValue = SplitBaseMaterials(costs, splitFraction);
                if (splitValue > 0f && replacement.BaseMarketValue > 0f)
                    AddOrMerge(costs, replacement, Mathf.CeilToInt(splitValue / replacement.BaseMarketValue));
            }
        }

        /// <summary>
        /// EMP rule: split off a fraction of wood/steel/plasteel and convert to the
        /// replacement material by market value.
        /// </summary>
        private static void ApplyValueSplit(
            List<ThingDefCountClass> costs, ThingDef replacement, float splitFraction)
        {
            if (replacement == null)
                return;

            float splitValue = SplitBaseMaterials(costs, splitFraction);
            if (splitValue > 0f && replacement.BaseMarketValue > 0f)
                AddOrMerge(costs, replacement, Mathf.CeilToInt(splitValue / replacement.BaseMarketValue));
        }

        /// <summary>
        /// Lightweight rule: swap a fraction of source material count directly to
        /// the replacement material (1:1 by count).
        /// </summary>
        private static void ApplyPartialSwapByCount(
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

        /// <summary>
        /// Charge/charger/frequency rule: convert all non-spacer-component costs
        /// into spacer components by market value (rounded up).
        /// </summary>
        private static void ApplyConvertAllToSpacer(List<ThingDefCountClass> costs)
        {
            if (componentSpacer == null || componentSpacer.BaseMarketValue <= 0f)
                return;

            float totalValue = 0f;
            int existingSpacerCount = 0;

            for (int i = costs.Count - 1; i >= 0; i--)
            {
                if (costs[i].thingDef == componentSpacer)
                {
                    existingSpacerCount += costs[i].count;
                }
                else
                {
                    totalValue += costs[i].count * costs[i].thingDef.BaseMarketValue;
                }
                costs.RemoveAt(i);
            }

            int totalCount = existingSpacerCount + Mathf.CeilToInt(totalValue / componentSpacer.BaseMarketValue);
            if (totalCount > 0)
                costs.Add(new ThingDefCountClass(componentSpacer, totalCount));
        }

        /// <summary>
        /// Replaces all costs with a single flat entry.
        /// </summary>
        private static void ApplyFlatCost(List<ThingDefCountClass> costs, ThingDef material, int count)
        {
            if (material == null)
                return;

            costs.Clear();
            costs.Add(new ThingDefCountClass(material, count));
        }

        /// <summary>
        /// Multiplies all cost counts by the given factor (rounded up).
        /// </summary>
        private static void ApplyCostMultiplier(List<ThingDefCountClass> costs, float multiplier)
        {
            foreach (ThingDefCountClass cost in costs)
                cost.count = Mathf.CeilToInt(cost.count * multiplier);
        }

        /// <summary>
        /// Removes a fraction of wood, steel, and plasteel from the cost list and returns
        /// the total market value of what was removed.
        /// </summary>
        private static float SplitBaseMaterials(List<ThingDefCountClass> costs, float fraction)
        {
            float splitValue = 0f;

            foreach (ThingDefCountClass cost in costs)
            {
                if (cost.thingDef != woodLog && cost.thingDef != steel && cost.thingDef != plasteel)
                    continue;

                int splitAmount = Mathf.FloorToInt(cost.count * fraction);
                if (splitAmount <= 0)
                    continue;

                splitValue += splitAmount * cost.thingDef.BaseMarketValue;
                cost.count -= splitAmount;
            }

            return splitValue;
        }

        private static void AddOrMerge(List<ThingDefCountClass> costs, ThingDef def, int count)
        {
            ThingDefCountClass existing = costs.Find(c => c.thingDef == def);
            if (existing != null)
                existing.count += count;
            else
                costs.Add(new ThingDefCountClass(def, count));
        }

        private static List<ThingDefCountClass> CloneCosts(List<ThingDefCountClass> costs)
        {
            var clone = new List<ThingDefCountClass>(costs.Count);
            foreach (ThingDefCountClass entry in costs)
                clone.Add(new ThingDefCountClass(entry.thingDef, entry.count));
            return clone;
        }

        private static ThingDef TryMatchWords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            ThingDef bestMatch = null;
            int bestLength = 0;

            foreach (string word in text.ToLowerInvariant().Split(' '))
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

        private static string SplitPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder(input.Length + 4);
            for (int i = 0; i < input.Length; i++)
            {
                if (i > 0 && char.IsUpper(input[i]))
                    result.Append(' ');
                result.Append(input[i]);
            }
            return result.ToString();
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
