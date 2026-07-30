using System.Collections.Generic;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // The behaviour-neutrality matrix the Phase 1 spec's Constraints section
    // demands: with the shipped rule set, a representative spread of scenarios
    // must still bill exactly what the pre-Phase-1 formulas billed, except where
    // items 1-5 deliberately changed the number.
    //
    // Each test derives its expectation from the inputs in a comment, and the
    // changed ones also record what the old formula produced, so a future edit
    // that moves one of these numbers has to argue with the derivation rather
    // than just re-baseline the assertion.
    //
    // Common chain: tech-level fallback (50) -> recipe costs (100) -> 0.5x cost
    // fraction x quality (200) -> rarity multiplier (250) -> negative downgrade
    // (300) -> keyword rules (1000-8000) -> material override (9000) -> prune to
    // 3 types (9900).
    // Settings are null headless, so the cost multiplier is 1 and the recipe
    // worker runs.
    [Collection("TraitCost")]
    public class TraitCostBehaviorMatrixTests
    {
        public TraitCostBehaviorMatrixTests()
        {
            TraitCostTestHarness.Bootstrap();
            TraitCostTestHarness.UseRules(TraitCostTestHarness.LoadShippedRules());
        }

        // Gun_AssaultRifle: Steel 60 + ComponentIndustrial 7, work 40000.
        private static Thing AssaultRifle()
        {
            return TraitCostTestHarness.MakeWeapon(
                "TestGun_AssaultRifle", TechLevel.Industrial, workToMake: 40000f,
                costList: TraitCostTestHarness.Costs(
                    (TraitCostTestHarness.Steel, 60),
                    (TraitCostTestHarness.ComponentIndustrial, 7)));
        }

        // MeleeWeapon_Warhammer: costStuffCount 150, work 18000.
        private static Thing Warhammer(ThingDef stuff)
        {
            return TraitCostTestHarness.MakeWeapon(
                "TestMeleeWeapon_Warhammer", TechLevel.Medieval, workToMake: 18000f,
                costStuffCount: 150, stuff: stuff);
        }

        private static List<ThingDefCountClass> Cost(Thing weapon, string defName, string label)
        {
            return TraitCostUtility.GetAdditionCost(
                weapon, TraitCostTestHarness.MakeTrait(defName, label));
        }

        // ===== Unchanged by Phase 1 (items 1-5 must not have moved these) =====

        [Fact]
        public void Unchanged_ToxSwapOnAnIndustrialComponentBill()
        {
            // Steel 60 + comp 7 -> x0.5 -> Steel 30 + comp 4; the component entry
            // is the pivot, so 4 x 3 = 12 herbal medicine. Identical before and
            // after: the industrial lookup still comes first.
            List<ThingDefCountClass> costs = Cost(AssaultRifle(), "TestToxCoating", "toxic");

            Assert.Equal(2, costs.Count);
            Assert.Equal(30, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(12, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void Unchanged_AkimboDoublesTheBill()
        {
            // Steel 30 + comp 4, doubled. CostFactorWorker's factor defaults to
            // 2, so the DoubleCostWorker alias the XML names is a no-op rename.
            List<ThingDefCountClass> costs = Cost(AssaultRifle(), "TestAkimboRig", "akimbo");

            Assert.Equal(60, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(8, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        [Fact]
        public void Unchanged_OrnamentalConvertsHalfToSilverByCount()
        {
            // Steel 30 + comp 4 -> components removed -> half of 30 steel by
            // count becomes silver -> Steel 15 + Silver 15.
            List<ThingDefCountClass> costs = Cost(AssaultRifle(), "TestOrnate", "ornamental");

            Assert.Equal(2, costs.Count);
            Assert.Equal(15, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(15, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Silver));
        }

        [Fact]
        public void Unchanged_GripRemovesComponents()
        {
            // Steel 30 + comp 4 -> comp removed -> Steel 30.
            List<ThingDefCountClass> costs = Cost(AssaultRifle(), "TestGripWrap", "grip");

            Assert.Single(costs);
            Assert.Equal(30, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
        }

        [Fact]
        public void Unchanged_InlayRemovesComponents()
        {
            // Same removal from the late-running inlay rule. No material word in
            // the label, so the override fallback stays out of it.
            List<ThingDefCountClass> costs = Cost(AssaultRifle(), "TestPlainInlay", "inlay");

            Assert.Single(costs);
            Assert.Equal(30, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
        }

        [Fact]
        public void Unchanged_HeavyScrapIsOneSlagChunk()
        {
            // requireAllKeywords: "heavy" and "scrap" both present -> the whole
            // bill is replaced by a single steel slag chunk.
            List<ThingDefCountClass> costs = Cost(AssaultRifle(), "TestScrapBolt", "heavy scrap");

            Assert.Single(costs);
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ChunkSlagSteel));
        }

        [Fact]
        public void Unchanged_LightweightBowSwapsFortyPercentOfWoodForBirdskin()
        {
            // Bow_Great: WoodLog 60 -> x0.5 -> Wood 30; floor(30 x 0.4) = 12
            // swapped 1:1 -> Wood 18 + bird leather 12. The rule is gated to the
            // Bow weapon category, so the trait has to carry it.
            Thing bow = TraitCostTestHarness.MakeWeapon(
                "TestBow_Great", TechLevel.Neolithic, workToMake: 9000f,
                costList: TraitCostTestHarness.Costs((TraitCostTestHarness.WoodLog, 60)));
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(
                "TestLightweightLimbs", "lightweight", TraitCostTestHarness.Category("Bow"));

            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(bow, trait);

            Assert.Equal(2, costs.Count);
            Assert.Equal(18, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.WoodLog));
            Assert.Equal(12, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Birdskin));
        }

        [Fact]
        public void Unchanged_EmpValueSplitOnASteelOnlyBill()
        {
            // Steel-stuffed spear, costStuffCount 75 -> x0.5 -> Steel 38
            // (ceil 37.5). EMP splits 70%: floor(38 x 0.7) = 26 steel removed =
            // 49.4 of value -> ceil(49.4 / 32) = 2 industrial components, 12 steel
            // left. Unchanged because steel was already one of the three
            // materials the old split hardcoded.
            Thing spear = TraitCostTestHarness.MakeWeapon(
                "TestMeleeWeapon_Spear", TechLevel.Medieval, workToMake: 12000f,
                costStuffCount: 75, stuff: TraitCostTestHarness.Steel);

            List<ThingDefCountClass> costs = Cost(spear, "TestEmpCoil", "emp");

            Assert.Equal(2, costs.Count);
            Assert.Equal(12, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(2, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        // ===== Changed on purpose by items 2-5 ================================

        [Fact]
        public void Changed_ChargeWeaponTakesTheSpacerComponentSwap()
        {
            // Item 2. Gun_ChargeRifle: Plasteel 50 + spacer comp 2 -> x0.5 ->
            // Plasteel 25 + spacer comp 1. The spacer entry is now a valid pivot:
            // 1 x 3 = 3 herbal medicine, plasteel untouched.
            //
            // Before: no industrial entry, so it fell through to the value split —
            // floor(25 x 0.7) = 17 plasteel = 153 of value -> 16 herbal medicine,
            // leaving Plasteel 8 + spacer comp 1 + herbal 16.
            Thing chargeRifle = TraitCostTestHarness.MakeWeapon(
                "TestGun_ChargeRifle", TechLevel.Spacer, workToMake: 45000f,
                costList: TraitCostTestHarness.Costs(
                    (TraitCostTestHarness.Plasteel, 50),
                    (TraitCostTestHarness.ComponentSpacer, 2)));

            List<ThingDefCountClass> costs = Cost(chargeRifle, "TestToxRounds", "toxic");

            Assert.Equal(2, costs.Count);
            Assert.Equal(25, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Plasteel));
            Assert.Equal(3, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
            Assert.Equal(0, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void Changed_MeleeToxBillIsComplexityDerived()
        {
            // Item 4. Wooden warhammer: recipe Wood 150 -> x0.5 -> Wood 75. No
            // component entry, so the signature count comes from complexity:
            // 18000 / 6000 = 3, x3 = 9 herbal medicine, added on top.
            //
            // Before: the value split took 70% of the wood — floor(75 x 0.7) = 52
            // wood = 62.4 of value -> 7 herbal medicine, leaving Wood 23.
            List<ThingDefCountClass> costs = Cost(
                Warhammer(TraitCostTestHarness.WoodLog), "TestToxEdge", "toxic");

            Assert.Equal(2, costs.Count);
            Assert.Equal(75, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.WoodLog));
            Assert.Equal(9, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void Changed_MeleeToxBillIgnoresTheStuffTier()
        {
            // Item 4 with item 3's hazard: the same warhammer in gold bills the
            // same 9 herbal medicine, because complexity is stuff-independent.
            // Before, the value split would have priced 70% of 750 silver of gold
            // as ~53 herbal medicine.
            List<ThingDefCountClass> costs = Cost(
                Warhammer(TraitCostTestHarness.Gold), "TestToxEdge", "toxic");

            Assert.Equal(75, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
            Assert.Equal(9, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void Changed_MeleeToxBillScalesDownToAKnife()
        {
            // Knife: recipe Wood 30 -> x0.5 -> Wood 15; complexity 1800 / 6000 =
            // 0.3, x3 = 0.9 -> the floor of 1 herbal medicine.
            Thing knife = TraitCostTestHarness.MakeWeapon(
                "TestMeleeWeapon_Knife", TechLevel.Neolithic, workToMake: 1800f,
                costStuffCount: 30, stuff: TraitCostTestHarness.WoodLog);

            List<ThingDefCountClass> costs = Cost(knife, "TestToxEdge", "toxic");

            Assert.Equal(15, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.WoodLog));
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void Changed_ExoticStuffNowSplits()
        {
            // Item 3. Jade warhammer: recipe Jade 150 -> x0.5 -> Jade 75. EMP
            // splits 70%: floor(75 x 0.7) = 52 jade = 260 of value ->
            // ceil(260 / 32) = 9 industrial components, 23 jade left.
            //
            // Before: jade was not one of the hardcoded wood/steel/plasteel trio,
            // so nothing split and the bill stayed Jade 75 — the trait was free
            // of any EMP flavour.
            List<ThingDefCountClass> costs = Cost(
                Warhammer(TraitCostTestHarness.Jade), "TestEmpCoil", "emp");

            Assert.Equal(2, costs.Count);
            Assert.Equal(23, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Jade));
            Assert.Equal(9, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        [Fact]
        public void Changed_GoldInlayOnAMasterworkWarhammerBillsTwentyNineGold()
        {
            // Item 5, the reference outcome from the research doc. Masterwork
            // steel warhammer: recipe Steel 150, x0.5 cost fraction x 2.0
            // masterwork = 1.0, so Steel 150 reaches the override. 150 x 1.9 =
            // 285 of value; 285 / 10 = 28.5 -> 29 gold.
            //
            // Before: 1:1 by count, i.e. 150 gold (1500 silver of value for a
            // decorative inlay).
            Thing warhammer = TraitCostTestHarness.MakeWeaponWithQuality(
                TraitCostTestHarness.MakeWeaponDef(
                    "TestMeleeWeapon_Warhammer", TechLevel.Medieval, workToMake: 18000f,
                    costStuffCount: 150),
                QualityCategory.Masterwork,
                TraitCostTestHarness.Steel);

            List<ThingDefCountClass> costs = Cost(warhammer, "TestGoldInlay", "gold inlay");

            Assert.Single(costs);
            Assert.Equal(29, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }

        [Fact]
        public void Unchanged_FlareSplitLeavesComponentsAlone()
        {
            // Vanilla ComponentIndustrial declares stuffProps (for texture
            // tinting), so IsRawResource accepts it — but SplitBaseMaterials
            // excludes both component defs explicitly, keeping the
            // stuff-agnostic split (item 3) a base-materials-only change.
            // Component bills therefore price exactly as they did before.
            //
            // Assault rifle -> Steel 30 + comp 4. Flare splits 70% of the steel
            // only: 21 steel (39.9 of value) -> ceil(39.9 / 0.75) = 54
            // bioferrite, leaving Steel 9 + comp 4.
            List<ThingDefCountClass> costs = Cost(AssaultRifle(), "TestFlareTube", "flare");

            Assert.Equal(3, costs.Count);
            Assert.Equal(9, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(4, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
            Assert.Equal(54, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.Bioferrite));
        }

        [Fact]
        public void Unchanged_EmpSplitOnAComponentBillIsValueNeutral()
        {
            // The counterpart to the flare case: with components excluded from
            // the split, only the steel converts and the component entry rides
            // through untouched, exactly as before item 3.
            //
            // Steel 30 + comp 4: 21 steel (39.9) -> ceil(39.9 / 32) = 2 comps
            // added to the 4 kept = 6, Steel 9.
            List<ThingDefCountClass> costs = Cost(AssaultRifle(), "TestEmpCoil", "emp");

            Assert.Equal(2, costs.Count);
            Assert.Equal(9, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(6, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentIndustrial));
        }

        [Fact]
        public void Changed_GoldInlayAtNormalQualityHalvesThat()
        {
            // Same weapon at normal quality: Steel 150 -> x0.5 -> Steel 75 =
            // 142.5 of value -> ceil(14.25) = 15 gold. Confirms the quality
            // multiplier still lands ahead of the override.
            List<ThingDefCountClass> costs = Cost(
                Warhammer(TraitCostTestHarness.Steel), "TestGoldInlay", "gold inlay");

            Assert.Single(costs);
            Assert.Equal(15, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }
    }
}
