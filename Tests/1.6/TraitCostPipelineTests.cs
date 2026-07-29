using System.Collections.Generic;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Stands in for the Phase 2/3 rules that will bill a tech-selected component
    // count: no shipped worker uses a component as its replacement material, so
    // the pipeline-level tech-tier and rule-ordering cases need one.
    public class TestComponentSignatureWorker : ComponentSwapOrSplitWorker
    {
        protected override ThingDef Replacement => CostRuleHelpers.ComponentIndustrial;
        protected override int ComponentMultiplier => 1;
    }

    // Pipeline-level coverage: the whole rule chain, driven through the public
    // entry points with the rule set that actually ships (parsed from
    // 1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml).
    //
    // Every expectation is hand-derived in a comment. The chain that produces
    // them, for reference: tech-level fallback (50), recipe costs (100), 0.5x
    // cost fraction and quality (200), negative-trait downgrade (300), the
    // keyword rules (1000-8000), material override (9000), prune to 3 (9900).
    [Collection("TraitCost")]
    public class TraitCostPipelineTests
    {
        public TraitCostPipelineTests()
        {
            TraitCostTestHarness.Bootstrap();
        }

        // An industrial gun with a component bill: Steel 60 + ComponentIndustrial
        // 7, WorkToMake 40000 (vanilla Gun_AssaultRifle).
        private static Thing AssaultRifle()
        {
            return TraitCostTestHarness.MakeWeapon(
                "TestGun_AssaultRifle", TechLevel.Industrial, workToMake: 40000f,
                costList: TraitCostTestHarness.Costs(
                    (TraitCostTestHarness.Steel, 60),
                    (TraitCostTestHarness.ComponentIndustrial, 7)));
        }

        // Vanilla Gun_ChargeRifle: Plasteel 50 + ComponentSpacer 2, work 45000.
        private static Thing ChargeRifle()
        {
            return TraitCostTestHarness.MakeWeapon(
                "TestGun_ChargeRifle", TechLevel.Spacer, workToMake: 45000f,
                costList: TraitCostTestHarness.Costs(
                    (TraitCostTestHarness.Plasteel, 50),
                    (TraitCostTestHarness.ComponentSpacer, 2)));
        }

        // ---- Item 9: matching on defName alone ---------------------------------

        [Fact]
        public void DefNameTokens_MatchARuleWhenTheLabelIsFullyLocalized()
        {
            // Label carries no English word; "UMW_ToxicCoating" tokenizes to
            // {toxic, coating} and "toxic" is a UWU_ToxSwap keyword.
            // Steel 60 + comp 7 -> x0.5 -> Steel 30 + comp 4 -> tox swaps the
            // 4 components for 4 x 3 = 12 herbal medicine.
            TraitCostTestHarness.UseRules(TraitCostTestHarness.LoadShippedRules());
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait("UMW_ToxicCoating", "혈독 강화");

            List<ThingDefCountClass> costs =
                TraitCostUtility.GetAdditionCost(AssaultRifle(), trait);

            Assert.Equal(30, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(12, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        [Fact]
        public void DefNameTokens_ControlTraitMatchesNothing()
        {
            // Same localized label, a defName with no keyword in it: the bill is
            // the plain halved recipe. Proves the match above came from the
            // defName and not from something matching unconditionally.
            TraitCostTestHarness.UseRules(TraitCostTestHarness.LoadShippedRules());
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait("UMW_PlainEdge", "혈독 강화");

            List<ThingDefCountClass> costs =
                TraitCostUtility.GetAdditionCost(AssaultRifle(), trait);

            Assert.Equal(30, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(4, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
        }

        // ---- Item 10: acronym collisions -------------------------------------

        [Fact]
        public void ModPrefixAcronym_DoesNotMatchAKeywordThatSpellsIt()
        {
            // One rule, keyword "arc". "ARC_HeavyBarrel" is Alpha Rim
            // Cybernetics' prefix plus a description; only the description may
            // match. "VWE_ArcThrower" genuinely describes an arc.
            TraitCostRuleDef arcRule = TraitCostTestHarness.MakeRule(
                "TestArcRule", typeof(AddIngredientsWorker), 1000,
                labelKeywords: new[] { "arc" });
            arcRule.addIngredients = new List<TraitCostIngredient>
            {
                new TraitCostIngredient { thingDef = "SignalChip", count = 1 },
            };
            TraitCostTestHarness.UseRules(arcRule);

            Thing gun = AssaultRifle();
            List<ThingDefCountClass> prefixed = TraitCostUtility.GetAdditionCost(
                gun, TraitCostTestHarness.MakeTrait("ARC_HeavyBarrel", "무거운 총열"));
            List<ThingDefCountClass> described = TraitCostUtility.GetAdditionCost(
                gun, TraitCostTestHarness.MakeTrait("VWE_ArcThrower", "무거운 총열"));

            Assert.Empty(prefixed);
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                described, TraitCostTestHarness.SignalChip));
        }

        [Fact]
        public void ModPrefixAcronym_DoesNotTriggerAShippedRule()
        {
            // Same collision against real XML: "rail" is a UWU_ChargeUnconditional
            // keyword. Prefixed, the bill stays the halved recipe (Steel 30 +
            // comp 4); in the body, everything converts to advanced components by
            // value: 30 x 1.9 + 4 x 32 = 185, / 200 -> 1.
            TraitCostTestHarness.UseRules(TraitCostTestHarness.LoadShippedRules());
            Thing gun = AssaultRifle();

            List<ThingDefCountClass> prefixed = TraitCostUtility.GetAdditionCost(
                gun, TraitCostTestHarness.MakeTrait("RAIL_HeavyBarrel", "무거운 총열"));
            List<ThingDefCountClass> described = TraitCostUtility.GetAdditionCost(
                gun, TraitCostTestHarness.MakeTrait("VWE_RailShot", "레일 사격"));

            Assert.Equal(30, TraitCostTestHarness.CountOf(prefixed, TraitCostTestHarness.Steel));
            Assert.Equal(4, TraitCostTestHarness.CountOf(
                prefixed, TraitCostTestHarness.ComponentIndustrial));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                prefixed, TraitCostTestHarness.ComponentSpacer));

            Assert.Single(described);
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                described, TraitCostTestHarness.ComponentSpacer));
        }

        // ---- Item 11: negative traits and the downgrade (O7) ------------------

        [Fact]
        public void NegativeTrait_DowngradesSpacerMaterialsWhenAdding()
        {
            // Charge rifle: Plasteel 50 + spacer comp 2 -> x0.5 -> Plasteel 25 +
            // spacer comp 1 -> downgrade -> Steel 25 + industrial comp 1 -> the
            // negative-trait discount (RefundRate 0.5, rounded up) -> Steel 13 +
            // comp 1.
            TraitCostTestHarness.UseRules(TraitCostTestHarness.LoadShippedRules());
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(
                "UMW_Cumbersome", "cumbersome", marketValueOffset: -50f);

            List<ThingDefCountClass> costs =
                TraitCostUtility.GetAdditionCost(ChargeRifle(), trait);

            Assert.True(TraitCostUtility.IsNegativeTrait(trait));
            Assert.Equal(13, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
            Assert.Equal(0, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Plasteel));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void NegativeTrait_KeepsProperTierMaterialsWhenRemoving()
        {
            // Removing a negative trait costs the player proper-tier materials:
            // Plasteel 25 + spacer comp 1, then RefundRate 0.5 rounded up ->
            // Plasteel 13 + spacer comp 1. No downgrade on this pipeline.
            TraitCostTestHarness.UseRules(TraitCostTestHarness.LoadShippedRules());
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(
                "UMW_Cumbersome", "cumbersome", marketValueOffset: -50f);

            List<ThingDefCountClass> costs =
                TraitCostUtility.GetRemovalCost(ChargeRifle(), trait);

            Assert.Equal(13, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Plasteel));
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
            Assert.Equal(0, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        [Fact]
        public void NegativeTrait_SignatureBillKeepsItsTierBecauseItIsAddedLater()
        {
            // Documents the rule ordering rather than a preference: the downgrade
            // rule runs at priority 300, so a signature bill added by a keyword
            // rule (1000+) is never seen by it. A spacer weapon therefore still
            // pays advanced components for the signature line even though the
            // trait is negative, while the base bill is downgraded.
            //
            // Plasteel-stuffed spacer sword, costStuffCount 100, work 18000:
            // recipe Plasteel 100 -> x0.5 -> Plasteel 50 -> downgrade -> Steel 50
            // -> signature complexity 3 x 1, tech-selected to advanced
            // components -> Steel 50 + spacer comp 3 -> negative discount ->
            // Steel 25 + spacer comp 2.
            List<TraitCostRuleDef> rules = TraitCostTestHarness.LoadShippedRules();
            rules.Add(TraitCostTestHarness.MakeRule(
                "TestSignatureRule", typeof(TestComponentSignatureWorker), 1000,
                labelKeywords: new[] { "cumbersome" }));
            TraitCostTestHarness.UseRules(rules);

            Thing sword = TraitCostTestHarness.MakeWeapon(
                "TestSpacerSword", TechLevel.Spacer, workToMake: 18000f, costStuffCount: 100,
                stuff: TraitCostTestHarness.Plasteel);
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(
                "UMW_Cumbersome", "cumbersome", marketValueOffset: -50f);

            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(sword, trait);

            Assert.Equal(25, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(2, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void PositiveTrait_RefundIsRoundedDown()
        {
            // Sanity on the other half of the removal contract: a positive trait
            // refunds floor(cost x 0.5) — Steel 30 + comp 4 -> Steel 15 + comp 2.
            TraitCostTestHarness.UseRules(TraitCostTestHarness.LoadShippedRules());
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait("UMW_PlainEdge", "혈독 강화");

            List<ThingDefCountClass> refund =
                TraitCostUtility.GetRemovalCost(AssaultRifle(), trait);

            Assert.Equal(15, TraitCostTestHarness.CountOf(refund, TraitCostTestHarness.Steel));
            Assert.Equal(2, TraitCostTestHarness.CountOf(
                refund, TraitCostTestHarness.ComponentIndustrial));
        }

        // ---- Item 12: the prune cap (O8) -------------------------------------

        // Wood-stuffed warhammer: costStuffCount 150, WorkToMake 18000.
        private static Thing WoodenWarhammer()
        {
            return TraitCostTestHarness.MakeWeapon(
                "TestMeleeWeapon_Warhammer", TechLevel.Medieval, workToMake: 18000f,
                costStuffCount: 150, stuff: TraitCostTestHarness.WoodLog);
        }

        private static TraitCostRuleDef SurchargeRule(
            string defName, int priority, string ingredientDefName, int count)
        {
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                defName, typeof(AddIngredientsWorker), priority,
                labelKeywords: new[] { "paralytic" });
            rule.addIngredients = new List<TraitCostIngredient>
            {
                new TraitCostIngredient { thingDef = ingredientDefName, count = count },
            };
            return rule;
        }

        [Fact]
        public void Prune_StuffPlusSignaturePlusSurchargeAllSurvive()
        {
            // Three material types is exactly the cap: the wood stuff pile
            // (150 -> x0.5 -> 75), the tox signature (complexity 3 x 3 = 9 herbal
            // medicine) and a 10x hemogen surcharge.
            List<TraitCostRuleDef> rules = TraitCostTestHarness.LoadShippedRules();
            rules.Add(SurchargeRule("TestHemogenSurcharge", 1050, "HemogenPack", 10));
            TraitCostTestHarness.UseRules(rules);

            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                WoodenWarhammer(),
                TraitCostTestHarness.MakeTrait("TestParalyticEdge", "paralytic"));

            Assert.Equal(3, costs.Count);
            Assert.Equal(75, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.WoodLog));
            Assert.Equal(9, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
            Assert.Equal(10, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.HemogenPack));
        }

        [Fact]
        public void Prune_DropsTheCheapestMaterialWhenAFourthAppears()
        {
            // A second surcharge takes the list to four types, so the prune drops
            // the cheapest by unit value: wood at 1.2, below herbal 10, hemogen 5
            // and signal chips 1000.
            List<TraitCostRuleDef> rules = TraitCostTestHarness.LoadShippedRules();
            rules.Add(SurchargeRule("TestHemogenSurcharge", 1050, "HemogenPack", 10));
            rules.Add(SurchargeRule("TestChipSurcharge", 1060, "SignalChip", 1));
            TraitCostTestHarness.UseRules(rules);

            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                WoodenWarhammer(),
                TraitCostTestHarness.MakeTrait("TestParalyticEdge", "paralytic"));

            Assert.Equal(3, costs.Count);
            Assert.Equal(0, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.WoodLog));
            Assert.Equal(9, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
            Assert.Equal(10, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.HemogenPack));
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.SignalChip));
        }
    }
}
