using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Static helper methods and material caches used by trait cost rule
    // workers. Extracted from TraitCostUtility to be accessible to the worker
    // class hierarchy. Partial class: this file holds the material caches and
    // material resolution; label/defName tokenization lives in .Tokenization,
    // cost-list mutation and the complexity figure in .CostOps.
    public static partial class CostRuleHelpers
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
            return rawResources?.Contains(def) == true;
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
