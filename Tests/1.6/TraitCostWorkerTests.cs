using System.Collections.Generic;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Unit coverage for the workers Phase 1 added or generalized. These are
    // driven directly (rule def -> Worker.OnStartup -> Worker.Apply) rather than
    // through the pipeline, because no shipped XML uses them yet.
    [Collection("TraitCost")]
    public class TraitCostWorkerTests
    {
        public TraitCostWorkerTests()
        {
            TraitCostTestHarness.Bootstrap();
        }

        private static TraitCostRuleWorker StartedWorker(TraitCostRuleDef rule)
        {
            TraitCostRuleWorker worker = rule.Worker;
            worker.OnStartup();
            return worker;
        }

        // ---- Item 6: AddIngredientsWorker --------------------------------------

        private static TraitCostRuleDef IngredientRule(
            bool refundable, params TraitCostIngredient[] ingredients)
        {
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestAddIngredients", typeof(AddIngredientsWorker), 1050);
            rule.refundable = refundable;
            rule.addIngredients = new List<TraitCostIngredient>(ingredients);
            return rule;
        }

        [Fact]
        public void AddIngredients_FixedIngredientAddsItsCount()
        {
            TraitCostRuleDef rule = IngredientRule(
                refundable: true,
                new TraitCostIngredient { thingDef = "HemogenPack", count = 10 });
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 30));
            Thing weapon = TraitCostTestHarness.MakeWeapon("TestBloodBlade", TechLevel.Medieval);

            StartedWorker(rule).Apply(
                costs, weapon, TraitCostTestHarness.MakeTrait("TestBlood", "blood-stained"), false);

            Assert.Equal(30, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(10, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.HemogenPack));
        }

        [Fact]
        public void AddIngredients_MergesIntoAnExistingEntry()
        {
            TraitCostRuleDef rule = IngredientRule(
                refundable: true,
                new TraitCostIngredient { thingDef = "ComponentSpacer", count = 2 });
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.ComponentSpacer, 1));

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestSkipGun", TechLevel.Spacer),
                TraitCostTestHarness.MakeTrait("TestSkip", "skip"),
                false);

            Assert.Single(costs);
            Assert.Equal(3, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void AddIngredients_FallbackCoversAnUnloadedPrimary()
        {
            // The hellsphere shape: SignalChip needs Biotech, advanced
            // components are always there.
            TraitCostRuleDef rule = IngredientRule(
                refundable: true,
                new TraitCostIngredient
                {
                    thingDef = "TestDefThatIsNotLoaded",
                    fallbackDef = "ComponentSpacer",
                    count = 1,
                });
            var costs = new List<ThingDefCountClass>();

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestHellsphere", TechLevel.Spacer),
                TraitCostTestHarness.MakeTrait("TestHell", "hellsphere"),
                false);

            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void AddIngredients_UnresolvableLineIsInertAndTheRestStillApply()
        {
            TraitCostRuleDef rule = IngredientRule(
                refundable: true,
                new TraitCostIngredient
                {
                    thingDef = "TestMissingPrimary",
                    fallbackDef = "TestMissingFallback",
                    count = 5,
                },
                new TraitCostIngredient { thingDef = "HemogenPack", count = 10 });
            var costs = new List<ThingDefCountClass>();

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestPartialGun", TechLevel.Industrial),
                TraitCostTestHarness.MakeTrait("TestPartial", "partial"),
                false);

            Assert.Single(costs);
            Assert.Equal(10, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.HemogenPack));
        }

        [Fact]
        public void AddIngredients_UnrefundableSkipsTheRemovalPipeline()
        {
            TraitCostRuleDef rule = IngredientRule(
                refundable: false,
                new TraitCostIngredient { thingDef = "HemogenPack", count = 10 });
            Thing weapon = TraitCostTestHarness.MakeWeapon("TestUnrefundable", TechLevel.Medieval);
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait("TestUnref", "blood");
            TraitCostRuleWorker worker = StartedWorker(rule);

            var addition = new List<ThingDefCountClass>();
            var removal = new List<ThingDefCountClass>();
            worker.Apply(addition, weapon, trait, false);
            worker.Apply(removal, weapon, trait, true);

            Assert.Equal(10, TraitCostTestHarness.CountOf(
                addition, TraitCostTestHarness.HemogenPack));
            Assert.Empty(removal);
        }

        [Fact]
        public void AddIngredients_RefundableAppliesToBothPipelines()
        {
            TraitCostRuleDef rule = IngredientRule(
                refundable: true,
                new TraitCostIngredient { thingDef = "SignalChip", count = 1 });
            Thing weapon = TraitCostTestHarness.MakeWeapon("TestRefundable", TechLevel.Spacer);
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait("TestRef", "hellsphere");
            TraitCostRuleWorker worker = StartedWorker(rule);

            var addition = new List<ThingDefCountClass>();
            var removal = new List<ThingDefCountClass>();
            worker.Apply(addition, weapon, trait, false);
            worker.Apply(removal, weapon, trait, true);

            Assert.Equal(1, TraitCostTestHarness.CountOf(addition, TraitCostTestHarness.SignalChip));
            Assert.Equal(1, TraitCostTestHarness.CountOf(removal, TraitCostTestHarness.SignalChip));
        }

        [Theory]
        [InlineData(TechLevel.Neolithic, "MedicineHerbal")]
        [InlineData(TechLevel.Medieval, "MedicineHerbal")]
        [InlineData(TechLevel.Industrial, "MedicineIndustrial")]
        [InlineData(TechLevel.Spacer, "MedicineUltratech")]
        public void AddIngredients_ThreeTierIngredientFollowsWeaponTechLevel(
            TechLevel tech, string expectedDefName)
        {
            // The healing/lifesteal shape: 10x medicine, tier by weapon.
            TraitCostRuleDef rule = IngredientRule(
                refundable: false,
                new TraitCostIngredient
                {
                    lowDef = "MedicineHerbal",
                    industrialDef = "MedicineIndustrial",
                    spacerDef = "MedicineUltratech",
                    count = 10,
                });
            var costs = new List<ThingDefCountClass>();

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestHealer" + tech, tech),
                TraitCostTestHarness.MakeTrait("TestHealing", "healing"),
                false);

            Assert.Single(costs);
            Assert.Equal(expectedDefName, costs[0].thingDef.defName);
            Assert.Equal(10, costs[0].count);
        }

        [Theory]
        [InlineData(TechLevel.Neolithic, "ComponentIndustrial")]
        [InlineData(TechLevel.Industrial, "ComponentIndustrial")]
        [InlineData(TechLevel.Spacer, "ComponentSpacer")]
        public void AddIngredients_TwoTierIngredientSplitsAtSpacer(
            TechLevel tech, string expectedDefName)
        {
            // No lowDef: the industrial def covers everything below Spacer.
            TraitCostRuleDef rule = IngredientRule(
                refundable: false,
                new TraitCostIngredient
                {
                    industrialDef = "ComponentIndustrial",
                    spacerDef = "ComponentSpacer",
                    count = 2,
                });
            var costs = new List<ThingDefCountClass>();

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestTiered" + tech, tech),
                TraitCostTestHarness.MakeTrait("TestTieredTrait", "tiered"),
                false);

            Assert.Single(costs);
            Assert.Equal(expectedDefName, costs[0].thingDef.defName);
            Assert.Equal(2, costs[0].count);
        }

        [Fact]
        public void AddIngredients_NoIngredientsIsANoOp()
        {
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestEmptyIngredients", typeof(AddIngredientsWorker), 1050);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 10));

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestPlain", TechLevel.Industrial),
                TraitCostTestHarness.MakeTrait("TestPlainTrait", "plain"),
                false);

            Assert.Single(costs);
            Assert.Equal(10, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
        }

        // ---- Item 7: CostFactorWorker -----------------------------------------

        [Fact]
        public void CostFactor_DiscountRoundsUp()
        {
            // Undersized: 0.65x. 10 steel -> 6.5 -> 7, 2 components -> 1.3 -> 2.
            // A discount never rounds a material away entirely.
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestUndersized", typeof(CostFactorWorker), 1800);
            rule.costFactor = 0.65f;
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 10), (TraitCostTestHarness.ComponentIndustrial, 2));

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestSmallGun", TechLevel.Industrial),
                TraitCostTestHarness.MakeTrait("TestUnder", "undersized"),
                false);

            Assert.Equal(7, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(2, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        [Fact]
        public void CostFactor_DefaultsToDoubling()
        {
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestFactorDefault", typeof(CostFactorWorker), 1800);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 10));

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestBigGun", TechLevel.Industrial),
                TraitCostTestHarness.MakeTrait("TestFactor", "factor"),
                false);

            Assert.Equal(20, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
        }

        [Fact]
        public void DoubleCostWorker_StillDoubles()
        {
            // The shipped akimbo rule states no factor, so the alias must keep
            // behaving exactly as it did.
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestAkimbo", typeof(DoubleCostWorker), 1800);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 30), (TraitCostTestHarness.ComponentIndustrial, 4));

            StartedWorker(rule).Apply(
                costs,
                TraitCostTestHarness.MakeWeapon("TestPistol", TechLevel.Industrial),
                TraitCostTestHarness.MakeTrait("TestAkimboTrait", "akimbo"),
                false);

            Assert.Equal(60, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(8, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        // ---- Item 8 (def contract): translator-extensible keywords -------------

        [Fact]
        public void LabelKeywords_AllowLanguagePacksToChangeTheEntryCount()
        {
            // Without [TranslationCanChangeCount] a language pack can only
            // replace list entries one for one, so translators could not append
            // localized keywords. The attribute is the whole mechanism; losing it
            // would silently break every non-English keyword rule.
            Assert.Single(typeof(TraitCostRuleDef)
                .GetField(nameof(TraitCostRuleDef.labelKeywords))
                .GetCustomAttributes(typeof(TranslationCanChangeCountAttribute), false));
        }

        // ---- Item 8: StuffFittingsSwapWorker ----------------------------------

        [Fact]
        public void StuffFittings_SwapsToSteelOnAnIndustrialWeapon()
        {
            // floor(100 x 0.4) = 40 of the weapon's own stuff becomes steel.
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestFittings", typeof(StuffFittingsSwapWorker), 1250);
            Thing club = TraitCostTestHarness.MakeWeapon(
                "TestStuddedClub", TechLevel.Industrial, workToMake: 6000f, costStuffCount: 100,
                stuff: TraitCostTestHarness.WoodLog);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.WoodLog, 100));

            StartedWorker(rule).Apply(
                costs, club, TraitCostTestHarness.MakeTrait("TestStudded", "studded"), false);

            Assert.Equal(60, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.WoodLog));
            Assert.Equal(40, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
        }

        [Fact]
        public void StuffFittings_SwapsToPlasteelOnASpacerWeapon()
        {
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestFittingsSpacer", typeof(StuffFittingsSwapWorker), 1250);
            Thing blade = TraitCostTestHarness.MakeWeapon(
                "TestSpacerBlade", TechLevel.Spacer, workToMake: 18000f, costStuffCount: 100,
                stuff: TraitCostTestHarness.Jade);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Jade, 100));

            StartedWorker(rule).Apply(
                costs, blade, TraitCostTestHarness.MakeTrait("TestSerrated", "serrated"), false);

            Assert.Equal(60, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Jade));
            Assert.Equal(40, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Plasteel));
        }

        [Fact]
        public void StuffFittings_NoOpWhenTheWeaponHasNoStuff()
        {
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestFittingsUnstuffed", typeof(StuffFittingsSwapWorker), 1250);
            Thing gun = TraitCostTestHarness.MakeWeapon(
                "TestUnstuffedGun", TechLevel.Industrial, workToMake: 40000f);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 30), (TraitCostTestHarness.ComponentIndustrial, 4));

            StartedWorker(rule).Apply(
                costs, gun, TraitCostTestHarness.MakeTrait("TestBarbed", "barbed"), false);

            Assert.Equal(30, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(4, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
            Assert.Equal(2, costs.Count);
        }

        [Fact]
        public void StuffFittings_NoOpWhenTheStuffAlreadyIsTheFittingMaterial()
        {
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestFittingsSameMaterial", typeof(StuffFittingsSwapWorker), 1250);
            Thing sword = TraitCostTestHarness.MakeWeapon(
                "TestSteelSword", TechLevel.Industrial, workToMake: 18000f, costStuffCount: 100,
                stuff: TraitCostTestHarness.Steel);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 100));

            StartedWorker(rule).Apply(
                costs, sword, TraitCostTestHarness.MakeTrait("TestHoned", "honed"), false);

            Assert.Single(costs);
            Assert.Equal(100, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
        }

        [Fact]
        public void StuffFittings_HonorsTheDefSpecifiedFittingMaterials()
        {
            TraitCostRuleDef rule = TraitCostTestHarness.MakeRule(
                "TestFittingsOverride", typeof(StuffFittingsSwapWorker), 1250);
            rule.fittingsIndustrialDef = "Gold";
            rule.fittingsSpacerDef = "Uranium";
            Thing club = TraitCostTestHarness.MakeWeapon(
                "TestGildedClub", TechLevel.Medieval, workToMake: 6000f, costStuffCount: 100,
                stuff: TraitCostTestHarness.WoodLog);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.WoodLog, 100));

            StartedWorker(rule).Apply(
                costs, club, TraitCostTestHarness.MakeTrait("TestFlanged", "flanged"), false);

            Assert.Equal(40, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }
    }
}
