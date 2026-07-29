using System.Collections.Generic;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Unit coverage for the cost-math helpers the Phase 1 rework touched:
    // the defName tokenizer, the component swap / complexity branch, tech-tier
    // material selection, the stuff-agnostic split, and the by-value material
    // override. Expected numbers are derived in comments from the vanilla market
    // values and WorkToMake amounts the harness installs.
    [Collection("TraitCost")]
    public class CostRuleHelpersTests
    {
        public CostRuleHelpersTests()
        {
            TraitCostTestHarness.Bootstrap();
        }

        // ---- Item 1: defName tokenization -------------------------------------

        [Fact]
        public void SplitDefNameWords_DropsModPrefixAcronym()
        {
            HashSet<string> words = CostRuleHelpers.SplitDefNameWords("AArmoury_Oversized");

            Assert.Equal(new HashSet<string> { "oversized" }, words);
        }

        [Fact]
        public void SplitDefNameWords_PrefixCannotMatchAKeyword()
        {
            // The whole point of stripping the first segment: "arc" is a keyword
            // candidate, and ARC_ is a mod prefix, not a description.
            HashSet<string> words = CostRuleHelpers.SplitDefNameWords("ARC_HeavyBarrel");

            Assert.Equal(new HashSet<string> { "heavy", "barrel" }, words);
            Assert.DoesNotContain("arc", words);
        }

        [Fact]
        public void SplitDefNameWords_NoUnderscoreKeepsWholeName()
        {
            // Vanilla convention: no mod prefix to strip.
            HashSet<string> words = CostRuleHelpers.SplitDefNameWords("AimAssistance");

            Assert.Equal(new HashSet<string> { "aim", "assistance" }, words);
        }

        [Fact]
        public void SplitDefNameWords_BreaksAfterAcronymRun()
        {
            HashSet<string> words = CostRuleHelpers.SplitDefNameWords("EMPBlaster");

            Assert.Equal(new HashSet<string> { "emp", "blaster" }, words);
        }

        [Fact]
        public void SplitDefNameWords_StripsPrefixAndSplitsPascalCase()
        {
            HashSet<string> words = CostRuleHelpers.SplitDefNameWords("VWE_ChargeRifle");

            Assert.Equal(new HashSet<string> { "charge", "rifle" }, words);
        }

        [Fact]
        public void SplitDefNameWords_SplitsOnDigits()
        {
            HashSet<string> words = CostRuleHelpers.SplitDefNameWords("VWE_ChargeRifle2X");

            Assert.Equal(new HashSet<string> { "charge", "rifle", "x" }, words);
        }

        [Fact]
        public void SplitDefNameWords_PrefixOnlyNameYieldsNothing()
        {
            Assert.Empty(CostRuleHelpers.SplitDefNameWords("Weird_"));
        }

        [Fact]
        public void SplitDefNameWords_EmptyInputYieldsNothing()
        {
            Assert.Empty(CostRuleHelpers.SplitDefNameWords(null));
            Assert.Empty(CostRuleHelpers.SplitDefNameWords(""));
        }

        // ---- Item 2: component swap, spacer swap, complexity branch -----------

        [Fact]
        public void ComponentSwap_IndustrialEntryIsSwappedByCount()
        {
            // Assault rifle bill. 7 components x 3 = 21 herbal medicine; steel
            // is left exactly as it was (the pre-Phase-1 behavior).
            Thing gun = TraitCostTestHarness.MakeWeapon(
                "TestRifle", TechLevel.Industrial, workToMake: 40000f);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 60), (TraitCostTestHarness.ComponentIndustrial, 7));

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, gun, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Equal(60, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(21, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        [Fact]
        public void ComponentSwap_SpacerEntryIsSwappedWhenNoIndustrialPresent()
        {
            // Charge rifle bill (Plasteel 50 + ComponentSpacer 2). 2 x 3 = 6
            // herbal medicine; the plasteel is untouched.
            Thing chargeRifle = TraitCostTestHarness.MakeWeapon(
                "TestChargeRifle", TechLevel.Spacer, workToMake: 45000f);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Plasteel, 50), (TraitCostTestHarness.ComponentSpacer, 2));

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, chargeRifle, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Equal(50, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Plasteel));
            Assert.Equal(6, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void ComponentSwap_IndustrialWinsOverSpacer()
        {
            // Both kinds present: the industrial entry is the pivot, the spacer
            // entry is left alone.
            Thing gun = TraitCostTestHarness.MakeWeapon("TestHybrid", TechLevel.Spacer);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.ComponentIndustrial, 2),
                (TraitCostTestHarness.ComponentSpacer, 5));

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, gun, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Equal(6, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
            Assert.Equal(5, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void ComplexityBranch_BillsAdditivelyAndLeavesTheStuffPile()
        {
            // Warhammer: WorkToMake 18000 / 6000 = complexity 3, x3 = 9 herbal.
            // Added on top; the 150-wood stuff pile is not converted.
            Thing warhammer = TraitCostTestHarness.MakeWeapon(
                "TestWarhammer", TechLevel.Medieval, workToMake: 18000f, costStuffCount: 150,
                stuff: TraitCostTestHarness.WoodLog);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.WoodLog, 150));

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, warhammer, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Equal(150, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.WoodLog));
            Assert.Equal(9, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void ComplexityBranch_IsStuffIndependent()
        {
            // Same warhammer in gold: complexity comes from WorkToMake, so the
            // signature count is still 9 — the R2/R3 pairing that makes the
            // stuff-agnostic split safe.
            Thing warhammer = TraitCostTestHarness.MakeWeapon(
                "TestGoldWarhammer", TechLevel.Medieval, workToMake: 18000f, costStuffCount: 150,
                stuff: TraitCostTestHarness.Gold);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Gold, 150));

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, warhammer, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Equal(150, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
            Assert.Equal(9, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void ComplexityBranch_KnifeRoundsUpToOne()
        {
            // Knife: 1800 / 6000 = 0.3, x3 = 0.9 -> ceil 1.
            Thing knife = TraitCostTestHarness.MakeWeapon(
                "TestKnife", TechLevel.Neolithic, workToMake: 1800f, costStuffCount: 30,
                stuff: TraitCostTestHarness.Steel);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 30));

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, knife, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Equal(1, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void ComplexityBranch_NoWorkToMakeStillBillsOne()
        {
            // A weapon def with no WorkToMake of its own resolves to the stat's
            // defaultBaseValue of 1, i.e. complexity ~0. The floor of 1 keeps
            // the trait from being free.
            Thing oddity = TraitCostTestHarness.MakeWeapon("TestOddity", TechLevel.Spacer);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Plasteel, 20));

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, oddity, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Equal(1, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void ComplexityBranch_MergesIntoAnExistingEntry()
        {
            Thing warhammer = TraitCostTestHarness.MakeWeapon(
                "TestMergeHammer", TechLevel.Medieval, workToMake: 18000f);
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.MedicineHerbal, 2));

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, warhammer, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Single(costs);
            Assert.Equal(11, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
        }

        // ---- Item 3: tech-tier selection --------------------------------------

        [Fact]
        public void ComplexityBranch_ComponentBillFollowsWeaponTechLevel()
        {
            // Same rule replacement (industrial components), both sides of the
            // Spacer boundary. Complexity 3 x 1 = 3 of the tier-appropriate def.
            Thing industrial = TraitCostTestHarness.MakeWeapon(
                "TestIndustrialBody", TechLevel.Industrial, workToMake: 18000f);
            Thing spacer = TraitCostTestHarness.MakeWeapon(
                "TestSpacerBody", TechLevel.Spacer, workToMake: 18000f);

            var industrialCosts = new List<ThingDefCountClass>();
            var spacerCosts = new List<ThingDefCountClass>();
            CostRuleHelpers.ApplyComponentSwapOrSplit(
                industrialCosts, industrial, TraitCostTestHarness.ComponentIndustrial, 1);
            CostRuleHelpers.ApplyComponentSwapOrSplit(
                spacerCosts, spacer, TraitCostTestHarness.ComponentIndustrial, 1);

            Assert.Equal(3, TraitCostTestHarness.CountOf(
                industrialCosts, TraitCostTestHarness.ComponentIndustrial));
            Assert.Equal(3, TraitCostTestHarness.CountOf(
                spacerCosts, TraitCostTestHarness.ComponentSpacer));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                spacerCosts, TraitCostTestHarness.ComponentIndustrial));
        }

        [Fact]
        public void ComplexityBranch_MetalBillFollowsWeaponTechLevel()
        {
            Thing industrial = TraitCostTestHarness.MakeWeapon(
                "TestIndustrialMetal", TechLevel.Industrial, workToMake: 12000f);
            Thing spacer = TraitCostTestHarness.MakeWeapon(
                "TestSpacerMetal", TechLevel.Ultra, workToMake: 12000f);

            var industrialCosts = new List<ThingDefCountClass>();
            var spacerCosts = new List<ThingDefCountClass>();
            CostRuleHelpers.ApplyComponentSwapOrSplit(
                industrialCosts, industrial, TraitCostTestHarness.Steel, 2);
            CostRuleHelpers.ApplyComponentSwapOrSplit(
                spacerCosts, spacer, TraitCostTestHarness.Steel, 2);

            // Complexity 2 x 2 = 4.
            Assert.Equal(4, TraitCostTestHarness.CountOf(
                industrialCosts, TraitCostTestHarness.Steel));
            Assert.Equal(4, TraitCostTestHarness.CountOf(
                spacerCosts, TraitCostTestHarness.Plasteel));
        }

        [Fact]
        public void ComplexityBranch_SingleTierReplacementIgnoresTechLevel()
        {
            // Herbal medicine has no spacer sibling, so a spacer weapon still
            // bills herbal medicine.
            Thing spacer = TraitCostTestHarness.MakeWeapon(
                "TestSpacerBlade", TechLevel.Spacer, workToMake: 18000f);
            var costs = new List<ThingDefCountClass>();

            CostRuleHelpers.ApplyComponentSwapOrSplit(
                costs, spacer, TraitCostTestHarness.MedicineHerbal, 3);

            Assert.Equal(9, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Theory]
        [InlineData(TechLevel.Undefined, false)]
        [InlineData(TechLevel.Animal, false)]
        [InlineData(TechLevel.Neolithic, false)]
        [InlineData(TechLevel.Medieval, false)]
        [InlineData(TechLevel.Industrial, false)]
        [InlineData(TechLevel.Spacer, true)]
        [InlineData(TechLevel.Ultra, true)]
        [InlineData(TechLevel.Archotech, true)]
        public void SelectByTechLevel_TwoTier_SplitsAtSpacer(TechLevel tech, bool expectSpacer)
        {
            Thing weapon = TraitCostTestHarness.MakeWeapon("TestTier" + tech, tech);

            ThingDef picked = CostRuleHelpers.SelectByTechLevel(
                weapon, TraitCostTestHarness.Steel, TraitCostTestHarness.Plasteel);

            Assert.Same(
                expectSpacer ? TraitCostTestHarness.Plasteel : TraitCostTestHarness.Steel, picked);
        }

        [Fact]
        public void SelectByTechLevel_TwoTier_NullWeaponTakesIndustrial()
        {
            Assert.Same(
                TraitCostTestHarness.Steel,
                CostRuleHelpers.SelectByTechLevel(
                    null, TraitCostTestHarness.Steel, TraitCostTestHarness.Plasteel));
        }

        [Theory]
        [InlineData(TechLevel.Undefined, 0)]
        [InlineData(TechLevel.Neolithic, 0)]
        [InlineData(TechLevel.Medieval, 0)]
        [InlineData(TechLevel.Industrial, 1)]
        [InlineData(TechLevel.Spacer, 2)]
        [InlineData(TechLevel.Archotech, 2)]
        public void SelectByTechLevel_ThreeTier_LowIsMedievalAndBelow(TechLevel tech, int expectedTier)
        {
            Thing weapon = TraitCostTestHarness.MakeWeapon("TestTriTier" + tech, tech);

            ThingDef picked = CostRuleHelpers.SelectByTechLevel(
                weapon,
                TraitCostTestHarness.MedicineHerbal,
                TraitCostTestHarness.MedicineIndustrial,
                TraitCostTestHarness.MedicineUltratech);

            ThingDef[] tiers =
            {
                TraitCostTestHarness.MedicineHerbal,
                TraitCostTestHarness.MedicineIndustrial,
                TraitCostTestHarness.MedicineUltratech,
            };
            Assert.Same(tiers[expectedTier], picked);
        }

        [Fact]
        public void SelectByTechLevel_ThreeTier_NullWeaponTakesLowTier()
        {
            Assert.Same(
                TraitCostTestHarness.MedicineHerbal,
                CostRuleHelpers.SelectByTechLevel(
                    null,
                    TraitCostTestHarness.MedicineHerbal,
                    TraitCostTestHarness.MedicineIndustrial,
                    TraitCostTestHarness.MedicineUltratech));
        }

        // ---- Item 4: stuff-agnostic split -------------------------------------

        [Fact]
        public void SplitBaseMaterials_SplitsJade()
        {
            // Was a no-op before Phase 1. floor(150 x 0.7) = 105 jade removed,
            // 105 x 5 = 525 silver of value returned, 45 jade left.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Jade, 150));

            float value = CostRuleHelpers.SplitBaseMaterials(costs, 0.7f);

            Assert.Equal(525f, value, 2);
            Assert.Equal(45, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Jade));
        }

        [Fact]
        public void SplitBaseMaterials_SplitsGold()
        {
            // floor(150 x 0.7) = 105 gold removed = 1050 of value, 45 left.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Gold, 150));

            float value = CostRuleHelpers.SplitBaseMaterials(costs, 0.7f);

            Assert.Equal(1050f, value, 2);
            Assert.Equal(45, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }

        [Fact]
        public void SplitBaseMaterials_LeavesNonRawEntriesAlone()
        {
            // Advanced components and chemfuel are manufactured, not raw, so
            // they contribute nothing and keep their counts.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.ComponentSpacer, 4), (TraitCostTestHarness.Chemfuel, 10));

            float value = CostRuleHelpers.SplitBaseMaterials(costs, 0.7f);

            Assert.Equal(0f, value, 2);
            Assert.Equal(4, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
            Assert.Equal(10, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Chemfuel));
        }

        [Fact]
        public void SplitBaseMaterials_ExcludesComponentsDespiteTheirStuffProps()
        {
            // Vanilla ComponentIndustrial declares stuffProps (for texture
            // tinting), so IsRawResource accepts it — but the split excludes
            // both component defs explicitly: they are the pipeline's pivot
            // currency with dedicated swap/removal paths, and splitting them
            // would reprice flare-style rules on every component recipe.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.ComponentIndustrial, 6),
                (TraitCostTestHarness.ComponentSpacer, 2));

            float value = CostRuleHelpers.SplitBaseMaterials(costs, 0.7f);

            Assert.Equal(0f, value, 2);
            Assert.Equal(6, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
            Assert.Equal(2, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void SplitBaseMaterials_SkipsAmountsThatFloorToZero()
        {
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 1));

            float value = CostRuleHelpers.SplitBaseMaterials(costs, 0.7f);

            Assert.Equal(0f, value, 2);
            Assert.Equal(1, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
        }

        // ---- Item 5: material override by value -------------------------------

        [Fact]
        public void MaterialOverride_ConvertsByMarketValue()
        {
            // 150 steel x 1.9 = 285 of value; 285 / 10 (gold) = 28.5 -> 29 gold.
            // The pre-Phase-1 1:1-by-count rule billed 150 gold.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.Steel, 150));

            CostRuleHelpers.ApplyMaterialOverride(costs, TraitCostTestHarness.Gold);

            Assert.Single(costs);
            Assert.Equal(29, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }

        [Fact]
        public void MaterialOverride_NeverBillsLessThanOne()
        {
            // 1 wood = 1.2 of value; 1.2 / 10 = 0.12, which the floor of 1 saves
            // from rounding the trait away to free.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.WoodLog, 1));

            CostRuleHelpers.ApplyMaterialOverride(costs, TraitCostTestHarness.Gold);

            Assert.Equal(1, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }

        [Fact]
        public void MaterialOverride_NonRawEntriesPassThroughAheadOfNothing()
        {
            // Advanced components survive untouched; the override lands first in
            // the list.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.ComponentSpacer, 2), (TraitCostTestHarness.Steel, 150));

            CostRuleHelpers.ApplyMaterialOverride(costs, TraitCostTestHarness.Gold);

            Assert.Equal(2, costs.Count);
            Assert.Same(TraitCostTestHarness.Gold, costs[0].thingDef);
            Assert.Equal(29, costs[0].count);
            Assert.Same(TraitCostTestHarness.ComponentSpacer, costs[1].thingDef);
            Assert.Equal(2, costs[1].count);
        }

        [Fact]
        public void MaterialOverride_SumsEveryRawEntry()
        {
            // 20 wood (24) + 30 steel (57) + 2 plasteel (18) = 99 of value;
            // 99 / 5 (jade) = 19.8 -> 20 jade.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.WoodLog, 20),
                (TraitCostTestHarness.Steel, 30),
                (TraitCostTestHarness.Plasteel, 2));

            CostRuleHelpers.ApplyMaterialOverride(costs, TraitCostTestHarness.Jade);

            Assert.Single(costs);
            Assert.Equal(20, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Jade));
        }

        [Fact]
        public void MaterialOverride_ValuelessMaterialFallsBackToCount()
        {
            // A material with no market value can't be priced by value, so the
            // old 1:1-by-count conversion still applies: 20 + 30 = 50.
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.WoodLog, 20), (TraitCostTestHarness.Steel, 30));

            CostRuleHelpers.ApplyMaterialOverride(costs, TraitCostTestHarness.ValuelessMaterial);

            Assert.Single(costs);
            Assert.Equal(50, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ValuelessMaterial));
        }

        [Fact]
        public void MaterialOverride_NoRawEntriesLeavesTheListAlone()
        {
            List<ThingDefCountClass> costs = TraitCostTestHarness.Costs(
                (TraitCostTestHarness.ComponentSpacer, 2), (TraitCostTestHarness.Chemfuel, 5));

            CostRuleHelpers.ApplyMaterialOverride(costs, TraitCostTestHarness.Gold);

            Assert.Equal(2, costs.Count);
            Assert.Equal(0, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }

        // ---- GetMaterialOverride: label and defName paths see the same tokens --

        [Fact]
        public void GetMaterialOverride_MatchesOnLabel()
        {
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait("TestInlayA", "gold inlay");

            Assert.Same(TraitCostTestHarness.Gold, CostRuleHelpers.GetMaterialOverride(trait));
        }

        [Fact]
        public void GetMaterialOverride_FallsBackToDefNameTokens()
        {
            // Localized label, English defName — the F1 fallback, now using the
            // same tokenizer the matcher does (prefix stripped, PascalCase split).
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait("UMW_JadeInlay", "옥 상감");

            Assert.Same(TraitCostTestHarness.Jade, CostRuleHelpers.GetMaterialOverride(trait));
        }

        [Fact]
        public void GetMaterialOverride_IgnoresTheModPrefix()
        {
            // A mod prefix that happens to spell a material must not pick it:
            // "Gold_Whatever" is a prefix, not a description.
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait("Gold_Reinforced", "reinforced");

            Assert.Null(CostRuleHelpers.GetMaterialOverride(trait));
        }
    }
}
