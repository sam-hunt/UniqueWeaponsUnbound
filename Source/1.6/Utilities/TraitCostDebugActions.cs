using System.Collections.Generic;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Dev-menu diagnostic for auditing the trait-cost keyword vocabulary: dumps
    // every loaded WeaponTraitDef with the keyword rules it matches, the
    // material override it would resolve, and whether it registers as negative.
    // Exists so keyword regressions (research doc, O9) show up at a glance
    // after a rule/vocabulary change or with a new mod list, without buying
    // traits one by one.
    public static class TraitCostDebugActions
    {
        // AllowedGameStates flags are conjunctive requirements, so Invalid (0)
        // means "no state requirement" — available from the entry screen and
        // in-game alike; the dump only reads defs and startup caches.
        [DebugAction("Unique Weapons Unbound", "Dump trait cost rule matches",
            allowedGameStates = AllowedGameStates.Invalid)]
        private static void DumpTraitCostRuleMatches()
        {
            IReadOnlyList<TraitCostRuleDef> rules = TraitCostUtility.CachedRules;
            if (rules == null)
            {
                Log.Warning("[Unique Weapons Unbound] Trait cost rules not initialized; nothing to dump.");
                return;
            }

            var sb = new StringBuilder();
            int matchedTraits = 0;
            int totalTraits = 0;

            foreach (WeaponTraitDef trait in DefDatabase<WeaponTraitDef>.AllDefsListForReading)
            {
                totalTraits++;

                // Same word set RunPipeline matches against: label words plus
                // defName tokens. Keep in sync with RunPipeline.
                HashSet<string> words = CostRuleHelpers.SplitLabelWords(trait.label);
                words.UnionWith(CostRuleHelpers.SplitDefNameWords(trait.defName));

                var parts = new List<string>();
                foreach (TraitCostRuleDef ruleDef in rules)
                {
                    // Only keyword-gated rules are informative here; the
                    // always-run pipeline rules would list on every trait.
                    if (!ruleDef.labelKeywords.NullOrEmpty()
                        && ruleDef.Worker.Matches(words, trait))
                        parts.Add(ruleDef.defName);
                }

                ThingDef overrideMaterial = CostRuleHelpers.GetMaterialOverride(trait);
                if (overrideMaterial != null)
                    parts.Add("override→" + overrideMaterial.defName);
                if (TraitCostUtility.IsNegativeTrait(trait))
                    parts.Add("negative");

                if (parts.Count > 0)
                    matchedTraits++;

                sb.Append("  ").Append(trait.defName)
                    .Append(" \"").Append(trait.label).Append("\"");
                if (trait.modContentPack != null)
                    sb.Append(" [").Append(trait.modContentPack.Name).Append("]");
                sb.Append(": ")
                    .AppendLine(parts.Count > 0 ? string.Join(", ", parts) : "(plain recipe cost)");
            }

            Log.Message("[Unique Weapons Unbound] Trait cost rule matches for "
                + totalTraits + " traits (" + matchedTraits + " with thematic costs):\n" + sb);
        }
    }
}
