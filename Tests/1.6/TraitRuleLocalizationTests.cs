using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Spot-checks the labelKeywords DefInjected translations, which are not
    // display text: a language pack replaces a rule's keyword list wholesale
    // (the field is [TranslationCanChangeCount]) and the cost pipeline then
    // matches those tokens against trait labels and defNames. So a translation
    // bug here silently mis-prices traits instead of showing a wrong string.
    //
    // Two invariants make the whole-list mechanism safe, and both are checked
    // per language file:
    //  - every injected list keeps the English entries verbatim as a prefix.
    //    The English tokens are what match the language-invariant defName words,
    //    so dropping one breaks matching for every mod in every language.
    //  - UWU_HeavyScrap is never injected. It sets requireAllKeywords, so a
    //    whole-list replacement that appended anything would demand all entries
    //    in one label and make the rule unsatisfiable.
    //
    // French and Korean are the sampled pair: French exercises accented,
    // inflected Latin tokens, Korean exercises non-Latin tokens in a
    // space-delimited script (which is why Korean gets word tokens at all,
    // unlike Chinese or Japanese). The other six shipped languages follow the
    // same convention; this is a spot check, not an exhaustive audit.
    [Collection("TraitCost")]
    public class TraitRuleLocalizationTests
    {
        public TraitRuleLocalizationTests()
        {
            TraitCostTestHarness.Bootstrap();
        }

        // Rule defName -> injected keyword list, read out of the shipped
        // language file. Located beside the test assembly the same way
        // LoadShippedRules finds TraitCostRules.xml: test execution happens from
        // a mirrored bin directory with no access to the repo tree, so the
        // csproj copies these in.
        private static Dictionary<string, List<string>> LoadInjectedKeywords(string language)
        {
            string path = Path.Combine(
                Path.GetDirectoryName(new Uri(typeof(TraitRuleLocalizationTests).Assembly.CodeBase).LocalPath)
                    ?? ".",
                "Languages", language, "DefInjected",
                "UniqueWeaponsUnbound.TraitCostRuleDef",
                "TraitCostRules_TraitCostRuleDef.xml");
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "The shipped " + language
                    + " TraitCostRuleDef injections were not copied next to the test assembly.",
                    path);

            var doc = new XmlDocument();
            doc.Load(path);

            const string suffix = ".labelKeywords";
            var injections = new Dictionary<string, List<string>>();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element || !node.Name.EndsWith(suffix))
                    continue;

                var keywords = new List<string>();
                foreach (XmlNode li in node.ChildNodes)
                {
                    if (li.NodeType == XmlNodeType.Element && li.Name == "li")
                        keywords.Add(li.InnerText.Trim());
                }
                injections[node.Name.Substring(0, node.Name.Length - suffix.Length)] = keywords;
            }

            if (injections.Count == 0)
                throw new InvalidOperationException(
                    "Parsed no labelKeywords injections out of the " + language + " file.");

            return injections;
        }

        // A rule nothing else shares. LoadShippedRules re-parses the XML into
        // brand-new defs on every call, so the returned rule is private to the
        // caller and swapping its labelKeywords cannot leak into the other test
        // classes in this collection (which each load their own).
        private static TraitCostRuleDef FreshRule(string defName)
        {
            TraitCostRuleDef rule = TraitCostTestHarness.LoadShippedRules()
                .Find(r => r.defName == defName);
            Assert.NotNull(rule);
            return rule;
        }

        // Matching exactly as TraitCostUtility.RunPipeline does it: label words
        // union defName tokens, through the rule's own worker.
        private static bool Matches(
            TraitCostRuleDef rule, List<string> keywords, string defName, string label)
        {
            rule.labelKeywords = keywords;

            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(defName, label);
            HashSet<string> words = CostRuleHelpers.SplitLabelWords(trait.label);
            words.UnionWith(CostRuleHelpers.SplitDefNameWords(trait.defName));
            return rule.Worker.Matches(words, trait);
        }

        // ===== Convention guards, per language file =====

        [Theory]
        [InlineData("French")]
        [InlineData("Korean")]
        public void InjectedListsKeepTheEnglishEntriesAsAVerbatimPrefix(string language)
        {
            List<TraitCostRuleDef> english = TraitCostTestHarness.LoadShippedRules();

            foreach (KeyValuePair<string, List<string>> injection in LoadInjectedKeywords(language))
            {
                TraitCostRuleDef rule = english.Find(r => r.defName == injection.Key);
                Assert.NotNull(rule);
                Assert.NotNull(rule.labelKeywords);

                // Rendered as joined strings so a failure shows which token
                // moved or went missing, not just "lists differ".
                string wanted = string.Join(", ", rule.labelKeywords.ToArray());
                string actualPrefix = string.Join(
                    ", ",
                    injection.Value
                        .GetRange(0, Math.Min(rule.labelKeywords.Count, injection.Value.Count))
                        .ToArray());

                Assert.Equal(language + " " + injection.Key + ": " + wanted,
                    language + " " + injection.Key + ": " + actualPrefix);
            }
        }

        [Theory]
        [InlineData("French")]
        [InlineData("Korean")]
        public void HeavyScrapIsNeverInjected(string language)
        {
            // requireAllKeywords plus whole-list replacement: any appended token
            // would have to appear in the same label as "heavy" and "scrap",
            // which nothing does, so the rule would stop firing entirely.
            Assert.False(
                LoadInjectedKeywords(language).ContainsKey("UWU_HeavyScrap"),
                language + " injects UWU_HeavyScrap.labelKeywords, which "
                    + "requireAllKeywords makes unsatisfiable.");
        }

        [Theory]
        [InlineData("French")]
        [InlineData("Korean")]
        public void InjectedListsHaveNoDuplicateTokens(string language)
        {
            foreach (KeyValuePair<string, List<string>> injection in LoadInjectedKeywords(language))
            {
                var seen = new HashSet<string>();
                var duplicates = new List<string>();
                foreach (string keyword in injection.Value)
                {
                    if (!seen.Add(keyword))
                        duplicates.Add(keyword);
                }

                // Compared as strings so the failure names the offending tokens.
                Assert.Equal(
                    language + " " + injection.Key + " duplicates: <none>",
                    language + " " + injection.Key + " duplicates: "
                        + (duplicates.Count == 0
                            ? "<none>"
                            : string.Join(", ", duplicates.ToArray())));
            }
        }

        // ===== Localized labels match through the injected tokens =====

        [Fact]
        public void French_LocalizedLabelMatchesThroughAnInjectedKeyword()
        {
            // "lame barbelée" — a plausible French label for a barbed-blade
            // trait. The defName is deliberately meaningless, so nothing but a
            // French keyword can carry the match.
            const string defName = "XYZ_Fictif";
            const string label = "lame barbelée";

            List<string> french = LoadInjectedKeywords("French")["UWU_MetalFittings"];
            // Guards the label above against a translator dropping the token
            // (and, incidentally, against this file being decoded as anything
            // other than UTF-8).
            Assert.Contains("barbelée", french);

            TraitCostRuleDef rule = FreshRule("UWU_MetalFittings");
            List<string> englishOnly = new List<string>(rule.labelKeywords);

            Assert.True(Matches(rule, french, defName, label));
            Assert.False(Matches(rule, englishOnly, defName, label));
        }

        [Fact]
        public void Korean_LocalizedLabelMatchesThroughAnInjectedKeyword()
        {
            // "톱니 칼날" (serrated blade). Korean labels are space-delimited, so
            // 톱니 is a real word token inside a longer label.
            const string defName = "XYZ_Gasang";
            const string label = "톱니 칼날";

            List<string> korean = LoadInjectedKeywords("Korean")["UWU_MetalFittings"];
            Assert.Contains("톱니", korean);

            TraitCostRuleDef rule = FreshRule("UWU_MetalFittings");
            List<string> englishOnly = new List<string>(rule.labelKeywords);

            Assert.True(Matches(rule, korean, defName, label));
            Assert.False(Matches(rule, englishOnly, defName, label));
        }

        // ===== The defName backbone carries untranslated rule sets =====

        [Fact]
        public void EnglishRulesStillMatchAFullyLocalizedLabelViaDefNameTokens()
        {
            // Why every injected list must keep its English entries: a player on
            // a language with no injection (or with one, since the English
            // entries survive) still gets thematic costs, because the trait's
            // defName is language-invariant. Here the label contributes nothing
            // — the Korean test above proves "톱니 칼날" matches no English token
            // — and SplitDefNameWords alone supplies "serrated".
            TraitCostRuleDef rule = FreshRule("UWU_MetalFittings");

            Assert.True(Matches(
                rule, rule.labelKeywords, "SomeMod_SerratedBlade", "톱니 칼날"));
        }
    }
}
