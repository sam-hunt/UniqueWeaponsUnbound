using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Pins the keyword vocabulary's *coverage* against two real trait corpora,
    // as opposed to the cost arithmetic the other trait-cost suites check.
    //
    // Why corpora and not hand-picked words: the vocabularies in
    // TraitCostRules.xml exist to catch traits published by other mods, and the
    // failure mode nobody notices is a keyword that stops matching (a trait
    // silently drops to plain recipe cost) or starts matching something it
    // shouldn't. Both corpora are verbatim defName/label pairs from the shipped
    // mods, so an edit to the vocabulary has to argue with a real trait list
    // rather than with a word the rule author already had in mind.
    //
    // Matching runs through the production path — CostRuleHelpers.SplitLabelWords
    // + SplitDefNameWords into TraitCostRuleWorker.Matches, over rules parsed out
    // of the shipped XML — so no tokenization is reimplemented here. See
    // TraitCostUtility.RunPipeline, which builds the same word set, and
    // TraitCostDebugActions, whose dev dump is the interactive form of this test.
    //
    // "Thematic" means keyword-gated: the foundation and fallback rules
    // (BaseCost*, QualityMultiplier, NegativeDowngrade, MaterialOverride,
    // CostPrune) carry no labelKeywords and run on every trait, so they are not
    // coverage and are filtered out — the same filter the dev dump applies.
    [Collection("TraitCost")]
    public class TraitRuleCoverageTests
    {
        private readonly List<TraitCostRuleDef> rules;

        public TraitRuleCoverageTests()
        {
            TraitCostTestHarness.Bootstrap();
            rules = TraitCostTestHarness.LoadShippedRules();
        }

        // The keyword-gated rules a trait matches, in XML order. Mirrors
        // RunPipeline's word set exactly: label words union defName tokens.
        private List<string> ThematicMatches(
            string defName, string label, string weaponCategory)
        {
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(
                defName, label,
                string.IsNullOrEmpty(weaponCategory)
                    ? null
                    : TraitCostTestHarness.Category(weaponCategory));

            HashSet<string> words = CostRuleHelpers.SplitLabelWords(trait.label);
            words.UnionWith(CostRuleHelpers.SplitDefNameWords(trait.defName));

            var matched = new List<string>();
            foreach (TraitCostRuleDef rule in rules)
            {
                if (!rule.labelKeywords.NullOrEmpty() && rule.Worker.Matches(words, trait))
                    matched.Add(rule.defName);
            }
            return matched;
        }

        // Set comparison rendered as a sorted string so a failure names both the
        // rule that appeared and the one that went missing. XML order is not part
        // of the contract here; the pipeline applies rules by priority.
        private void AssertThematicMatches(
            string defName, string label, string weaponCategory, string expected)
        {
            List<string> actual = ThematicMatches(defName, label, weaponCategory);
            actual.Sort(StringComparer.Ordinal);

            // Split(new[] { ',' }), not Split(','): the netstandard facade in the
            // test output exposes the .NET Core (char, StringSplitOptions)
            // overload, which the net472 runtime has no implementation for.
            var wanted = new List<string>(
                expected.Length == 0 ? new string[0] : expected.Split(new[] { ',' }));
            wanted.Sort(StringComparer.Ordinal);

            Assert.Equal(
                string.Join(", ", wanted.ToArray()),
                string.Join(", ", actual.ToArray()));
        }

        // Rule -> the traits in a corpus it matched, for the exact-set assertions.
        private Dictionary<string, List<string>> MatchesByRule(string[][] corpus)
        {
            var byRule = new Dictionary<string, List<string>>();
            foreach (string[] row in corpus)
            {
                foreach (string rule in ThematicMatches(row[0], row[1], row[2]))
                {
                    if (!byRule.TryGetValue(rule, out List<string> traits))
                    {
                        traits = new List<string>();
                        byRule[rule] = traits;
                    }
                    traits.Add(row[0]);
                }
            }
            foreach (List<string> traits in byRule.Values)
                traits.Sort(StringComparer.Ordinal);
            return byRule;
        }

        private static int CountWithAnyMatch(string[][] corpus)
        {
            int matched = 0;
            foreach (string[] row in corpus)
            {
                if (row[3].Length > 0)
                    matched++;
            }
            return matched;
        }

        // ===== Corpus 1: Unique Melee Weapons (28 traits) =====

        // Verbatim defName/label pairs from Unique Melee Weapons, with the
        // thematic rules each must match. Melee traits declare no
        // weaponCategory — vanilla's categories are ranged shapes and the
        // publisher leaves the field unset — so the category column is empty
        // throughout, which is also why UWU_ChargeCategoryGated cannot fire on
        // this corpus at all (no trait here carries its keywords either).
        //
        // Columns: defName, label, weaponCategory, expected thematic rules.
        private static readonly string[][] UniqueMeleeWeaponsRows =
        {
            new[] { "UMW_ArmorSpike", "armor spike", "", "UWU_MetalFittings" },
            new[] { "UMW_Barbed", "barbed", "", "UWU_MetalFittings" },
            new[] { "UMW_BloodStained", "blood-stained", "", "UWU_Blood" },
            new[] { "UMW_Counterweighted", "counterweighted", "", "UWU_MetalFittings" },
            new[] { "UMW_Enameled", "enameled", "", "UWU_Ornamental" },
            new[] { "UMW_Envenomed", "envenomed", "", "UWU_ToxSwap" },
            new[] { "UMW_Flanged", "flanged", "", "UWU_MetalFittings" },
            new[] { "UMW_GoldInlay", "gold inlay", "", "UWU_Inlay" },
            new[] { "UMW_HeadWeighted", "head-weighted", "", "UWU_MetalFittings" },
            new[] { "UMW_JadeInlay", "jade inlay", "", "UWU_Inlay" },
            new[] { "UMW_Lightweight", "lightweight", "", "UWU_Lightweight" },
            new[] { "UMW_Monomolecular", "monomolecular", "", "UWU_ChargeUnconditional" },
            new[] { "UMW_NeedlePoint", "needle point", "", "UWU_MetalFittings" },
            new[] { "UMW_Opiated", "opiated", "", "UWU_ToxSwap" },
            new[] { "UMW_Ornamental", "ornamental", "", "UWU_Ornamental" },
            new[] { "UMW_PlasmaCored", "plasma-cored", "", "UWU_ChargeUnconditional" },
            new[] { "UMW_Quilloned", "quilloned", "", "UWU_MetalFittings" },
            new[] { "UMW_Razored", "razored", "", "UWU_MetalFittings" },
            new[] { "UMW_Serrated", "serrated", "", "UWU_MetalFittings" },
            new[] { "UMW_Studded", "studded", "", "UWU_MetalFittings" },
            new[] { "UMW_ZeusHeaded", "zeus-headed", "", "UWU_EmpSplit" },

            // Deliberately plain: nothing in the vocabulary should reach these,
            // and they price off the weapon's own recipe. "dead-blow" is the one
            // to watch — it tokenizes to {dead, blow}, one letter away from
            // UWU_IncendiarySwap's "blast".
            new[] { "UMW_BellCast", "bell-cast", "", "" },
            new[] { "UMW_Carbonized", "carbonized", "", "" },
            new[] { "UMW_Cumbersome", "cumbersome", "", "" },
            new[] { "UMW_DeadBlow", "dead-blow", "", "" },
            new[] { "UMW_Piledriver", "piledriver", "", "" },
            new[] { "UMW_Storied", "storied", "", "" },
            new[] { "UMW_Ugly", "ugly", "", "" },
        };

        public static IEnumerable<object[]> UniqueMeleeWeaponsCorpus()
        {
            foreach (string[] row in UniqueMeleeWeaponsRows)
                yield return new object[] { row[0], row[1], row[2], row[3] };
        }

        [Theory]
        [MemberData(nameof(UniqueMeleeWeaponsCorpus))]
        public void UniqueMeleeWeapons_TraitMatchesExactlyItsExpectedRules(
            string defName, string label, string weaponCategory, string expected)
        {
            AssertThematicMatches(defName, label, weaponCategory, expected);
        }

        [Fact]
        public void UniqueMeleeWeapons_CorpusIsTheWholePublishedSet()
        {
            Assert.Equal(28, UniqueMeleeWeaponsRows.Length);
        }

        [Fact]
        public void UniqueMeleeWeapons_MostTraitsGetAThematicCost()
        {
            // The point of the melee vocabulary work: the great majority of the
            // publisher's traits must price thematically rather than falling
            // through to plain recipe cost. 21 of 28 do; the floor is 19 so a
            // single deliberate removal doesn't fail the suite, but a vocabulary
            // regression does.
            Assert.True(
                CountWithAnyMatch(UniqueMeleeWeaponsRows) >= 19,
                "Only " + CountWithAnyMatch(UniqueMeleeWeaponsRows)
                    + " of 28 Unique Melee Weapons traits match a thematic rule.");
        }

        [Fact]
        public void UniqueMeleeWeapons_ExactMatchSetPerRule()
        {
            Dictionary<string, List<string>> byRule = MatchesByRule(UniqueMeleeWeaponsRows);

            AssertRuleMatched(byRule, "UWU_MetalFittings",
                "UMW_ArmorSpike", "UMW_Barbed", "UMW_Counterweighted", "UMW_Flanged",
                "UMW_HeadWeighted", "UMW_NeedlePoint", "UMW_Quilloned", "UMW_Razored",
                "UMW_Serrated", "UMW_Studded");
            AssertRuleMatched(byRule, "UWU_ToxSwap", "UMW_Envenomed", "UMW_Opiated");
            AssertRuleMatched(byRule, "UWU_Ornamental", "UMW_Enameled", "UMW_Ornamental");
            AssertRuleMatched(byRule, "UWU_Inlay", "UMW_GoldInlay", "UMW_JadeInlay");
            AssertRuleMatched(byRule, "UWU_ChargeUnconditional",
                "UMW_Monomolecular", "UMW_PlasmaCored");
            AssertRuleMatched(byRule, "UWU_Blood", "UMW_BloodStained");
            AssertRuleMatched(byRule, "UWU_EmpSplit", "UMW_ZeusHeaded");
            AssertRuleMatched(byRule, "UWU_Lightweight", "UMW_Lightweight");

            AssertNoOtherRuleMatched(byRule);
        }

        // ===== Corpus 2: Alpha Armoury (89 traits) =====

        // Verbatim defName/label/weaponCategory triples from Alpha Armoury
        // (packageId sarg.alphaarmoury), all 89 WeaponTraitDefs it publishes,
        // with the thematic rules each must match. Every AA trait declares
        // exactly one weaponCategory, so UWU_ChargeCategoryGated's gate is
        // exercised for real here rather than being unreachable.
        //
        // The labels are as published, quirks included: AArmoury_HolyLauncher's
        // label contains literal double quotes, and AArmoury_SharpshootersFocus
        // uses U+2019 RIGHT SINGLE QUOTATION MARK, not an ASCII apostrophe (spelt
        // as an escape so this file stays ASCII).
        //
        // Columns: defName, label, weaponCategory, expected thematic rules.
        private static readonly string[][] AlphaArmouryRows =
        {
            new[] { "AArmoury_SelfSkipField", "self-skip field", "Ranged", "" },
            new[] { "AArmoury_SkipField", "skip field", "Ranged", "" },
            new[] { "AArmoury_InvisibilityField", "invisibility field", "Ranged", "" },
            new[] { "AArmoury_Sludge", "sludge spewer", "AArmoury_SingleBulletFiring", "" },
            new[] { "AArmoury_Detonation", "detonation", "Ranged", "UWU_IncendiarySwap" },
            new[] { "AArmoury_MimicCore", "mimic core", "AArmoury_All", "" },
            new[] { "AArmoury_Flechettes", "flechettes", "Gun", "" },
            new[] { "AArmoury_MiniRocket", "mini-rockets", "BulletFiring", "" },
            new[] { "AArmoury_Sonic", "sonic amplifier", "AArmoury_SingleBulletFiring", "UWU_EmpSplit" },
            new[] { "AArmoury_Cryo", "cryo rounds", "BulletFiring", "UWU_ChargeUnconditional" },
            new[] { "AArmoury_FertilizerCanisters", "fertilizer canisters", "AArmoury_Gun_Launcher_Unique", "" },
            new[] { "AArmoury_Corrosive", "corrosive burst", "AArmoury_Gun_Launcher_Unique", "" },
            new[] { "AArmoury_PotatoCannon", "potato cannon", "AArmoury_Gun_Launcher_Unique", "" },
            new[] { "AArmoury_IncendiaryLauncher", "incendiary launcher", "AArmoury_Gun_Launcher_Unique", "UWU_IncendiarySwap" },
            new[] { "AArmoury_EMPLauncher", "EMP launcher", "AArmoury_Gun_Launcher_Unique", "UWU_EmpSplit" },
            new[] { "AArmoury_ToxLauncherMain", "toxbomb launcher", "AArmoury_Gun_Launcher_Unique", "UWU_ToxSwap" },
            new[] { "AArmoury_Flamethrower", "flamethrower", "AArmoury_Gun_Launcher_Unique", "" },
            new[] { "AArmoury_Laser", "laser capacitor", "BulletFiring", "UWU_ChargeUnconditional" },
            new[] { "AArmoury_CoilLance", "coil lance conversion", "AArmoury_LongRangeSnipers", "" },
            new[] { "AArmoury_AcidicRepeater", "acidic repeater", "AArmoury_Gun_BeamRepeater_Unique", "" },
            new[] { "AArmoury_FrostRepeater", "cryo cannon", "AArmoury_Gun_BeamRepeater_Unique", "UWU_ChargeUnconditional" },
            new[] { "AArmoury_PlasmaCannon", "plasma cannon", "AArmoury_Gun_BeamRepeater_Unique", "UWU_ChargeUnconditional" },
            new[] { "AArmoury_Chakram", "chakram launcher", "AArmoury_Gun_SniperRifle_Unique", "" },
            new[] { "AArmoury_ThermalBlasts", "thermal blasts", "Ranged", "UWU_IncendiarySwap" },
            new[] { "AArmoury_RandomProjectiles", "vanometric 3D printer", "AArmoury_All", "" },
            // Accepted false positive, pinned deliberately: "needle projectiles"
            // is a poison-dart ammo swap, not a melee fitting, but it tokenizes
            // to UWU_MetalFittings' "needle". Left in place because it is
            // behaviourally inert — StuffFittingsSwapWorker returns immediately
            // when weapon.Stuff is null, and every AA trait rides a non-stuffed
            // ranged weapon. If this row ever changes, the change is deliberate.
            new[] { "AArmoury_NeedleProjectiles", "needle projectiles", "AArmoury_Machineguns", "UWU_MetalFittings" },
            // The one trait in the corpus whose category satisfies
            // UWU_ChargeCategoryGated's {Pistol, PulseCharge, BeamWeapon} gate.
            new[] { "AArmoury_Tesla", "tesla coil", "PulseCharge", "UWU_ChargeCategoryGated" },
            new[] { "AArmoury_NerveSpiker", "nerve spiker", "AArmoury_SingleBulletFiring", "" },
            new[] { "AArmoury_Thumper", "thumper rounds", "AArmoury_Gun_ChainShotGun_Unique", "" },
            new[] { "AArmoury_NetGun", "net gun", "AArmoury_Gun_Launcher_Unique", "" },
            new[] { "AArmoury_SyringerGun", "syringer gun", "AArmoury_BoltAndSnipers", "" },
            new[] { "AArmoury_ShatterRounds", "shatter rounds", "BulletFiring", "" },
            new[] { "AArmoury_ShatterPellets", "shatter pellets", "PelletFiring", "" },
            new[] { "AArmoury_Electric", "electrified", "AArmoury_SingleBulletFiring", "" },
            new[] { "AArmoury_TachyonicFeed", "tachyonic feed", "Ranged", "" },
            new[] { "AArmoury_Hemovoric", "hemovoric", "Ranged", "UWU_Blood" },
            new[] { "AArmoury_Psychic", "psychic", "Ranged", "" },
            new[] { "AArmoury_Splitting", "bullet splitting", "AArmoury_AllButBeamAndLauncher", "" },
            new[] { "AArmoury_HealingMinigun", "healing gun", "AArmoury_Gun_MiniGun_Unique", "" },
            new[] { "AArmoury_Lifesteal", "lifesteal", "AArmoury_AllButBeamAndLauncher", "" },
            new[] { "AArmoury_Chemburster", "chemburster", "AArmoury_SingleBulletFiring", "UWU_IncendiarySwap" },
            new[] { "AArmoury_Flux", "flux-charged", "AArmoury_All", "UWU_EmpSplit" },
            new[] { "AArmoury_Voltaic", "voltaic rounds", "AArmoury_AllButBows", "UWU_EmpSplit" },
            new[] { "AArmoury_TargetLocator", "target locator", "AArmoury_Gun_Revolver_Unique", "" },
            new[] { "AArmoury_RepairGun", "repair gun", "AArmoury_Gun_Revolver_Unique", "" },
            new[] { "AArmoury_BeeBeeGun", "bee-bee gun", "AArmoury_Gun_LMG_Unique", "" },
            new[] { "AArmoury_BubbleBurster", "bubble burster", "AArmoury_Gun_HeavySMG_Unique", "" },
            new[] { "AArmoury_EntropyRounds", "entropy rounds", "AArmoury_All", "" },
            new[] { "AArmoury_FlareLauncher", "flare launcher", "Attachable", "UWU_Flarestriker" },
            new[] { "AArmoury_HolyLauncher", "\"Holy hand\" launcher", "Attachable", "" },
            new[] { "AArmoury_DeadlifeLauncher", "deadlife launcher", "Attachable", "" },
            new[] { "AArmoury_FleshMelter", "fleshmelter launcher", "Attachable", "" },
            new[] { "AArmoury_ToxLauncher", "tox grenade launcher", "Attachable", "UWU_ToxSwap" },
            new[] { "AArmoury_HellsphereLauncher", "hellsphere launcher", "Attachable", "" },
            new[] { "AArmoury_RotstinkLauncher", "rotstink launcher", "Attachable", "" },
            new[] { "AArmoury_MagmaLauncher", "magma launcher", "Attachable", "" },
            new[] { "AArmoury_ArchoteggLauncher", "archotegg launcher", "Attachable", "" },
            new[] { "AArmoury_AcidLauncher", "acid launcher", "Attachable", "" },
            new[] { "AArmoury_ShrapnelLauncher", "shrapnel launcher", "Attachable", "" },
            new[] { "AArmoury_SwarmLauncher", "swarm launcher", "Attachable", "" },
            new[] { "AArmoury_FirefoamLauncher", "firefoam launcher", "Attachable", "" },
            new[] { "AArmoury_MineDeployer", "mine deployer", "Attachable", "" },
            new[] { "AArmoury_Caltrops", "caltrop launcher", "Attachable", "" },
            new[] { "AArmoury_BarricadeDeployer", "barricade deployer", "Attachable", "" },
            new[] { "AArmoury_PesticideCanisters", "pesticide canisters", "AArmoury_Gun_Launcher_Unique", "" },
            new[] { "AArmoury_DehydratorGrenades", "dehydrator grenades", "Attachable", "" },
            new[] { "AArmoury_ShieldLauncher", "broadshield launcher", "Attachable", "" },
            new[] { "AArmoury_AntiToxLauncher", "anti-tox launcher", "Attachable", "UWU_ToxSwap" },
            new[] { "AArmoury_GrappleGun", "grapple gun", "Attachable", "" },
            new[] { "AArmoury_Chainsaw", "attached chainsaw", "Attachable", "" },
            new[] { "AArmoury_Bayonet", "attached bayonet", "Attachable", "" },
            new[] { "AArmoury_ToxicSprayer", "toxic sprayer", "Attachable", "UWU_ToxSwap" },
            new[] { "AArmoury_AcidSprayer", "acid sprayer", "Attachable", "" },
            new[] { "AArmoury_PsychicResilience", "psychic resilience", "Ranged", "" },
            new[] { "AArmoury_Satiating", "satiating", "Ranged", "" },
            new[] { "AArmoury_SharpshootersFocus", "sharpshooter\u2019s focus", "Ranged", "" },
            new[] { "AArmoury_AdrenalRush", "adrenal rush", "Ranged", "" },
            new[] { "AArmoury_RadiantCore", "radiant core", "Ranged", "" },
            new[] { "AArmoury_Orgasmatron", "orgasmatron core", "AArmoury_Gun_AssaultRifle_Unique", "" },
            new[] { "AArmoury_SunburstCore", "sunburst core", "Ranged", "" },
            new[] { "AArmoury_VacuumSealed", "vacuum sealed", "Gun", "" },
            new[] { "AArmoury_ToxinTuned", "toxin-tuned", "Ranged", "" },
            // Phase 2 split "oversized" out of UWU_Akimbo into its own 1.5x rule:
            // this trait is AA's Mass x2 / melee damage x1.5 size trait, not a
            // second rendered weapon.
            new[] { "AArmoury_Oversized", "oversized", "Ranged", "UWU_Oversized" },
            new[] { "AArmoury_Undersized", "undersized", "Ranged", "" },
            new[] { "AArmoury_RevolverScope", "revolver scope", "AArmoury_Gun_Revolver_Unique", "" },
            new[] { "AArmoury_DoubleTap", "double-tap cylinder", "AArmoury_Gun_Revolver_Unique", "" },
            new[] { "AArmoury_ReinforcedFrame", "reinforced frame", "AArmoury_All", "" },
            new[] { "AArmoury_InsectKiller", "insectoid slayer", "AArmoury_All", "" },
            new[] { "AArmoury_AnomalyKiller", "entity slayer", "AArmoury_All", "" },
        };

        public static IEnumerable<object[]> AlphaArmouryCorpus()
        {
            foreach (string[] row in AlphaArmouryRows)
                yield return new object[] { row[0], row[1], row[2], row[3] };
        }

        [Theory]
        [MemberData(nameof(AlphaArmouryCorpus))]
        public void AlphaArmoury_TraitMatchesExactlyItsExpectedRules(
            string defName, string label, string weaponCategory, string expected)
        {
            AssertThematicMatches(defName, label, weaponCategory, expected);
        }

        [Fact]
        public void AlphaArmoury_CorpusIsTheWholePublishedSet()
        {
            Assert.Equal(89, AlphaArmouryRows.Length);
        }

        [Fact]
        public void AlphaArmoury_TwentyOneTraitsGetAThematicCost()
        {
            // Not a floor like the melee corpus: AA's traits are ranged bolt-ons
            // and most of them are meant to price off the gun's own recipe. The
            // exact count is pinned so a broadened keyword that starts sweeping
            // up unrelated launchers shows up here.
            Assert.Equal(21, CountWithAnyMatch(AlphaArmouryRows));
        }

        [Fact]
        public void AlphaArmoury_OversizedMatchesTheOversizedRuleAndNotAkimbo()
        {
            List<string> matched = ThematicMatches("AArmoury_Oversized", "oversized", "Ranged");

            Assert.Contains("UWU_Oversized", matched);
            Assert.DoesNotContain("UWU_Akimbo", matched);
        }

        [Fact]
        public void AlphaArmoury_AkimboAndHeavyScrapMatchNothing()
        {
            // Both rules exist for other publishers (Vanilla Expanded Weapons).
            // No AA trait tokenizes to "akimbo", and none carries both "heavy"
            // and "scrap", which UWU_HeavyScrap requires together.
            Dictionary<string, List<string>> byRule = MatchesByRule(AlphaArmouryRows);

            Assert.False(byRule.ContainsKey("UWU_Akimbo"));
            Assert.False(byRule.ContainsKey("UWU_HeavyScrap"));
        }

        [Fact]
        public void AlphaArmoury_ExactMatchSetPerRule()
        {
            Dictionary<string, List<string>> byRule = MatchesByRule(AlphaArmouryRows);

            AssertRuleMatched(byRule, "UWU_ToxSwap",
                "AArmoury_AntiToxLauncher", "AArmoury_ToxLauncher",
                "AArmoury_ToxLauncherMain", "AArmoury_ToxicSprayer");
            AssertRuleMatched(byRule, "UWU_IncendiarySwap",
                "AArmoury_Chemburster", "AArmoury_Detonation",
                "AArmoury_IncendiaryLauncher", "AArmoury_ThermalBlasts");
            AssertRuleMatched(byRule, "UWU_EmpSplit",
                "AArmoury_EMPLauncher", "AArmoury_Flux",
                "AArmoury_Sonic", "AArmoury_Voltaic");
            AssertRuleMatched(byRule, "UWU_ChargeUnconditional",
                "AArmoury_Cryo", "AArmoury_FrostRepeater",
                "AArmoury_Laser", "AArmoury_PlasmaCannon");
            AssertRuleMatched(byRule, "UWU_Flarestriker", "AArmoury_FlareLauncher");
            AssertRuleMatched(byRule, "UWU_Blood", "AArmoury_Hemovoric");
            AssertRuleMatched(byRule, "UWU_Oversized", "AArmoury_Oversized");
            // Fires for real: AArmoury_Tesla's weaponCategory is PulseCharge,
            // one of the rule's gate categories. Nothing else in the corpus both
            // carries a charge keyword and sits in a gated category.
            AssertRuleMatched(byRule, "UWU_ChargeCategoryGated", "AArmoury_Tesla");
            // Accepted false positive — see the AArmoury_NeedleProjectiles row.
            AssertRuleMatched(byRule, "UWU_MetalFittings", "AArmoury_NeedleProjectiles");

            AssertNoOtherRuleMatched(byRule);
        }

        // ===== Shared assertions =====

        // Exact set, and removes the entry so AssertNoOtherRuleMatched can prove
        // nothing else in the vocabulary fired.
        private static void AssertRuleMatched(
            Dictionary<string, List<string>> byRule, string rule, params string[] expected)
        {
            var wanted = new List<string>(expected);
            wanted.Sort(StringComparer.Ordinal);

            byRule.TryGetValue(rule, out List<string> actual);
            Assert.Equal(
                rule + ": " + string.Join(", ", wanted.ToArray()),
                rule + ": " + string.Join(", ", (actual ?? new List<string>()).ToArray()));

            byRule.Remove(rule);
        }

        private static void AssertNoOtherRuleMatched(Dictionary<string, List<string>> byRule)
        {
            var leftovers = new List<string>();
            foreach (KeyValuePair<string, List<string>> entry in byRule)
                leftovers.Add(entry.Key + " -> " + string.Join(", ", entry.Value.ToArray()));
            leftovers.Sort(StringComparer.Ordinal);

            Assert.Equal(
                string.Empty,
                string.Join(" | ", leftovers.ToArray()));
        }
    }
}
