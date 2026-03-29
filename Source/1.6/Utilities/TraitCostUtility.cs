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

        /// <summary>
        /// Builds the raw resource and material label caches.
        /// Must be called during StaticConstructorOnStartup (after all defs are loaded).
        /// </summary>
        public static void Initialize()
        {
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

            // TODO: Decide how to handle uncraftable weapons (no costList/costStuffCount).
            // Currently returns empty list — caller decides whether to show "free" or block.
            List<ThingDefCountClass> costs = GetBaseCosts(weapon, baseDef);
            if (costs.Count == 0)
                return costs;

            float qualityMult = GetQualityMultiplier(weapon);
            float totalMult = CostFraction * qualityMult;

            foreach (ThingDefCountClass cost in costs)
                cost.count = Mathf.CeilToInt(cost.count * totalMult);

            ThingDef overrideMaterial = GetMaterialOverride(trait);
            if (overrideMaterial != null)
                costs = ApplyMaterialOverride(costs, overrideMaterial);

            costs.RemoveAll(c => c.count <= 0);
            return costs;
        }

        /// <summary>
        /// Calculates the total resource cost of adding multiple traits to the given weapon.
        /// Aggregates costs by ThingDef, summing counts for duplicate materials.
        /// </summary>
        public static List<ThingDefCountClass> GetTotalCost(
            Thing weapon, IEnumerable<WeaponTraitDef> traits)
        {
            return AggregateCosts(weapon, traits);
        }

        /// <summary>
        /// Calculates the total resource refund for removing multiple traits from the
        /// given weapon. Aggregates raw addition costs across all traits first, then
        /// applies RefundFraction and floors once per material. This avoids cumulative
        /// rounding loss from flooring each trait individually.
        /// </summary>
        public static List<ThingDefCountClass> GetTotalRefund(
            Thing weapon, IEnumerable<WeaponTraitDef> traits)
        {
            List<ThingDefCountClass> raw = AggregateCosts(weapon, traits);
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
