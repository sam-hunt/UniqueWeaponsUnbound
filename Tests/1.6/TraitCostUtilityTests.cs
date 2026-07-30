using System.Collections.Generic;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Tests for TraitCostUtility.IsNegativeTrait, which drives the inverted
    // cost/refund logic (cheap to add, costs to remove) for undesirable traits,
    // and for Phase 2.1's rarity multiplier, which the same predicate exempts.
    //
    // The rarity sections run the whole shipped rule chain, so every expectation
    // is derived in a comment from the harness's vanilla market values and
    // WorkToMake amounts. Chain, for reference: tech-level fallback (50) ->
    // recipe costs (100) -> 0.5x cost fraction x quality (200) -> rarity (250) ->
    // negative downgrade (300) -> keyword rules (1000-8000) -> material override
    // (9000) -> prune to 3 types (9900).
    [Collection("TraitCost")]
    public class TraitCostUtilityTests
    {
        public TraitCostUtilityTests()
        {
            TraitCostTestHarness.Bootstrap();
            TraitCostTestHarness.UseRules(TraitCostTestHarness.LoadShippedRules());
        }

        // ===== IsNegativeTrait: both detection signals =====

        [Fact]
        public void NegativeMarketValueOffset_IsNegativeTrait()
        {
            var trait = new WeaponTraitDef { defName = "TestNegativeOffset", marketValueOffset = -50f };

            Assert.True(TraitCostUtility.IsNegativeTrait(trait));
        }

        [Fact]
        public void PositiveMarketValueOffset_IsNotNegativeTrait()
        {
            var trait = new WeaponTraitDef { defName = "TestPositiveOffset", marketValueOffset = 50f };

            Assert.False(TraitCostUtility.IsNegativeTrait(trait));
        }

        [Fact]
        public void ZeroMarketValueOffsetAndNoStatFactors_IsNotNegativeTrait()
        {
            var trait = new WeaponTraitDef { defName = "TestNeutralTrait" };

            Assert.False(TraitCostUtility.IsNegativeTrait(trait));
        }

        // ===== Phase 2.1 item 1: the rarity multiplier's clamp =================

        // 1 / commonality, clamped into [1, RarityCapMax = 2]. commonality is a
        // selection weight, so 0.5 means "half as likely as a common trait" and
        // pays 2x; anything rarer than 0.5 is capped there. A commonality above
        // 1 is more common than the norm and still pays the plain bill — the
        // multiplier is floor-only and never a discount.
        [Theory]
        [InlineData(0f, 1f)]
        [InlineData(0.05f, 2f)]
        [InlineData(0.25f, 2f)]
        [InlineData(0.5f, 2f)]
        [InlineData(0.75f, 4f / 3f)]
        [InlineData(1f, 1f)]
        [InlineData(2f, 1f)]
        public void RarityMultiplier_ClampsToTheCapAndNeverDiscounts(
            float commonality, float expected)
        {
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(
                "TestRarity", "rarity probe", commonality: commonality);

            Assert.Equal(
                expected, RarityMultiplierWorker.GetRarityMultiplier(trait), 4);
        }

        [Fact]
        public void RarityMultiplier_NegativeCommonalityIsTreatedAsMisconfigured()
        {
            // Vanilla's own ConfigErrors flags commonality <= 0; the multiplier
            // prices such a def as common rather than dividing into it.
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(
                "TestBadRarity", "bad rarity", commonality: -1f);

            Assert.Equal(1f, RarityMultiplierWorker.GetRarityMultiplier(trait), 4);
        }

        [Theory]
        [InlineData(0.05f)]
        [InlineData(0.5f)]
        public void RarityMultiplier_NegativeTraitsAreExemptByOffset(float commonality)
        {
            // UMW Cumbersome's shape. A rare drawback is still a drawback, so
            // rarity must not double the price of bolting one on.
            WeaponTraitDef trait = TraitCostTestHarness.MakeTrait(
                "UMW_Cumbersome", "cumbersome", marketValueOffset: -50f,
                commonality: commonality);

            Assert.True(TraitCostUtility.IsNegativeTrait(trait));
            Assert.Equal(1f, RarityMultiplierWorker.GetRarityMultiplier(trait), 4);
        }

        [Fact]
        public void RarityMultiplier_NegativeTraitsAreExemptByMarketValueFactor()
        {
            // UMW Carbonized / vanilla Ugly: the other negative signal, a
            // MarketValue statFactor below 1, at the 0.5 cosmetic commonality.
            WeaponTraitDef trait = TraitCostTestHarness.MakeMarketValueFactorTrait(
                "UMW_Carbonized", "carbonized", marketValueFactor: 0.8f, commonality: 0.5f);

            Assert.True(TraitCostUtility.IsNegativeTrait(trait));
            Assert.Equal(1f, RarityMultiplierWorker.GetRarityMultiplier(trait), 4);
        }

        // ===== Reference outcomes (spec table, items 1-2) =====================

        // UMW warhammer: costStuffCount 150, WorkToMake 18000.
        private static Thing Warhammer(ThingDef stuff, QualityCategory quality)
        {
            return TraitCostTestHarness.MakeWeaponWithQuality(
                TraitCostTestHarness.MakeWeaponDef(
                    "TestMeleeWeapon_Warhammer", TechLevel.Medieval, workToMake: 18000f,
                    costStuffCount: 150),
                quality, stuff);
        }

        // Vanilla MeleeWeapon_LongSword: costStuffCount 100, WorkToMake 18000.
        private static Thing Longsword(ThingDef stuff, QualityCategory quality)
        {
            return TraitCostTestHarness.MakeWeaponWithQuality(
                TraitCostTestHarness.MakeWeaponDef(
                    "TestMeleeWeapon_LongSword", TechLevel.Medieval, workToMake: 18000f,
                    costStuffCount: 100),
                quality, stuff);
        }

        // Vanilla MeleeWeapon_Knife: costStuffCount 30, WorkToMake 1800.
        private static Thing Knife(ThingDef stuff, QualityCategory quality)
        {
            return TraitCostTestHarness.MakeWeaponWithQuality(
                TraitCostTestHarness.MakeWeaponDef(
                    "TestMeleeWeapon_Knife", TechLevel.Neolithic, workToMake: 1800f,
                    costStuffCount: 30),
                quality, stuff);
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

        // Vanilla Gun_AssaultRifle: Steel 60 + ComponentIndustrial 7, work 40000.
        private static Thing AssaultRifle()
        {
            return TraitCostTestHarness.MakeWeapon(
                "TestGun_AssaultRifle", TechLevel.Industrial, workToMake: 40000f,
                costList: TraitCostTestHarness.Costs(
                    (TraitCostTestHarness.Steel, 60),
                    (TraitCostTestHarness.ComponentIndustrial, 7)));
        }

        // UMW demoted Monomolecular to commonality 0.5 as a power throttle, so
        // it pays the cap.
        private static WeaponTraitDef Monomolecular()
        {
            return TraitCostTestHarness.MakeTrait(
                "UMW_Monomolecular", "monomolecular", commonality: 0.5f);
        }

        [Fact]
        public void Reference_MonomolecularOnAMasterworkSteelWarhammerBillsThreeSpacerComponents()
        {
            // Steel 150 -> x0.5 cost fraction x 2.0 masterwork = 1.0 -> Steel 150
            // -> rarity 2x -> Steel 300. UWU_ChargeUnconditional ("mono") converts
            // by value: 300 x 1.9 = 570, / 200 = 2.85 -> 3. The complexity floor
            // agrees rather than deciding it here: 18000 / 6000 = 3.
            //
            // Before Phase 2.1: 150 x 1.9 = 285 / 200 -> 2.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                Warhammer(TraitCostTestHarness.Steel, QualityCategory.Masterwork),
                Monomolecular());

            Assert.Single(costs);
            Assert.Equal(3, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void Reference_MonomolecularOnAMasterworkPlasteelLongswordBillsNineSpacerComponents()
        {
            // Plasteel 100 -> x1.0 (masterwork) -> 100 -> rarity 2x -> 200.
            // 200 x 9 = 1800 of value, / 200 = 9 exactly. Floor 3, so by-value
            // wins.
            //
            // Before Phase 2.1: 100 x 9 = 900 / 200 = 4.5 -> 5.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                Longsword(TraitCostTestHarness.Plasteel, QualityCategory.Masterwork),
                Monomolecular());

            Assert.Single(costs);
            Assert.Equal(9, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void Reference_MonomolecularOnANormalKnifeStillBillsOneSpacerComponent()
        {
            // Steel 30 -> x0.5 -> Steel 15 -> rarity 2x -> Steel 30 = 57 of
            // value, / 200 -> 1. Floor: 1800 / 6000 = 0.3 -> 1. Both paths land
            // on the same single component, which is the point of the row: the
            // cheapest melee weapon in the game does not get more expensive.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                Knife(TraitCostTestHarness.Steel, QualityCategory.Normal),
                Monomolecular());

            Assert.Single(costs);
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void Reference_GoldInlayOnAMasterworkWarhammerBillsFiftySevenGold()
        {
            // Vanilla GoldInlay sits at the 0.5 cosmetic commonality, so it pays
            // the cap. Steel 150 -> x1.0 (masterwork) -> 150 -> rarity 2x -> 300.
            // UWU_Inlay removes components (none here), then the material
            // override prices 300 x 1.9 = 570 of value in gold: 570 / 10 = 57.
            //
            // Before Phase 2.1: 29 gold (the Phase 1 reference outcome).
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                Warhammer(TraitCostTestHarness.Steel, QualityCategory.Masterwork),
                TraitCostTestHarness.MakeTrait("UMW_GoldInlay", "gold inlay", commonality: 0.5f));

            Assert.Single(costs);
            Assert.Equal(57, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }

        [Fact]
        public void Reference_GoldInlayOnANormalKnifeBillsSixGold()
        {
            // Steel 30 -> x0.5 -> 15 -> rarity 2x -> 30 = 57 of value / 10 =
            // 5.7 -> 6 gold. Before Phase 2.1: 15 steel = 28.5 -> 3 gold.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                Knife(TraitCostTestHarness.Steel, QualityCategory.Normal),
                TraitCostTestHarness.MakeTrait("UMW_GoldInlay", "gold inlay", commonality: 0.5f));

            Assert.Single(costs);
            Assert.Equal(6, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Gold));
        }

        [Fact]
        public void Reference_ACommonTraitIsUnchanged()
        {
            // UMW Envenomed is commonality 1 — as likely as any other trait — so
            // 1 / 1 clamps to 1 and the bill is exactly the pre-Phase-2.1 one:
            // Steel 150 -> x0.5 -> Steel 75, plus the tox rule's complexity
            // signature (18000 / 6000 = 3, x3) = 9 herbal medicine.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                Warhammer(TraitCostTestHarness.Steel, QualityCategory.Normal),
                TraitCostTestHarness.MakeTrait("UMW_Envenomed", "envenomed", commonality: 1f));

            Assert.Equal(2, costs.Count);
            Assert.Equal(75, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(9, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void Reference_NegativeTraitsBillTheSameAtAnyCommonality()
        {
            // Vanilla Ugly (0.5) and UMW Carbonized (MarketValue x0.8) are the
            // two negative shapes. Steel 150 -> x0.5 -> Steel 75 -> rarity
            // exempt -> the downgrade turns steel into wood -> Wood 75 -> the
            // negative-trait discount (RefundRate 0.5, rounded up) -> Wood 38.
            //
            // Asserted against the same trait at commonality 0 as well, so the
            // exemption is pinned at the pipeline level and not just in the
            // multiplier helper.
            Thing warhammer = Warhammer(TraitCostTestHarness.Steel, QualityCategory.Normal);

            List<ThingDefCountClass> ugly = TraitCostUtility.GetAdditionCost(
                warhammer,
                TraitCostTestHarness.MakeMarketValueFactorTrait(
                    "Ugly", "ugly", marketValueFactor: 0.8f, commonality: 0.5f));
            List<ThingDefCountClass> unweighted = TraitCostUtility.GetAdditionCost(
                warhammer,
                TraitCostTestHarness.MakeMarketValueFactorTrait(
                    "Ugly", "ugly", marketValueFactor: 0.8f, commonality: 0f));

            Assert.Equal(38, TraitCostTestHarness.CountOf(ugly, TraitCostTestHarness.WoodLog));
            Assert.Equal(
                TraitCostTestHarness.Describe(unweighted),
                TraitCostTestHarness.Describe(ugly));
        }

        [Fact]
        public void Reference_TeslaOnAChargeRifleTakesRarityButNotTheFloor()
        {
            // AA Tesla is commonality 0.5 and its PulseCharge category satisfies
            // UWU_ChargeCategoryGated. Plasteel 50 + spacer comp 2 -> x0.5 ->
            // Plasteel 25 + spacer 1 -> rarity 2x -> Plasteel 50 + spacer 2 ->
            // conversion: 2 kept + ceil(50 x 9 / 200) = 2 + 3 = 5.
            //
            // The floor deliberately stays out of it: the recipe carries a
            // component line, so the charge rifle's complexity (45000 / 6000 =
            // 7.5 -> 8) never enters the arithmetic. Before Phase 2.1 this bill
            // was 1 + ceil(225 / 200) = 3.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                ChargeRifle(),
                TraitCostTestHarness.MakeTrait(
                    "AArmoury_Tesla", "tesla coil",
                    TraitCostTestHarness.Category("PulseCharge"), commonality: 0.5f));

            Assert.Single(costs);
            Assert.Equal(5, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        [Fact]
        public void SpacerFloor_IndustrialGunPricesExactlyAsBefore()
        {
            // The other half of the no-components condition. Assault rifle
            // Steel 60 + comp 7 -> x0.5 -> Steel 30 + comp 4; a commonality-0
            // trait takes no rarity, so the conversion sees the same bill it
            // always did: 30 x 1.9 + 4 x 32 = 185, / 200 -> 1 advanced
            // component. The floor would have billed ceil(40000 / 6000) = 7.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                AssaultRifle(), TraitCostTestHarness.MakeTrait("VWE_RailShot", "rail shot"));

            Assert.Single(costs);
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ComponentSpacer));
        }

        // ===== Ordering properties (spec item 1) ==============================

        [Fact]
        public void Ordering_ByValueConversionsInheritTheScaledBase()
        {
            // The same weapon and rule chain, the trait's commonality the only
            // difference: 29 gold at commonality 1, 57 at 0.5. Rarity runs at
            // 250, so it scales the steel the override later prices, rather than
            // scaling a gold count that was already rounded.
            Thing warhammer = Warhammer(TraitCostTestHarness.Steel, QualityCategory.Masterwork);

            List<ThingDefCountClass> common = TraitCostUtility.GetAdditionCost(
                warhammer,
                TraitCostTestHarness.MakeTrait("UMW_GoldInlay", "gold inlay", commonality: 1f));
            List<ThingDefCountClass> rare = TraitCostUtility.GetAdditionCost(
                warhammer,
                TraitCostTestHarness.MakeTrait("UMW_GoldInlay", "gold inlay", commonality: 0.5f));

            Assert.Equal(29, TraitCostTestHarness.CountOf(common, TraitCostTestHarness.Gold));
            Assert.Equal(57, TraitCostTestHarness.CountOf(rare, TraitCostTestHarness.Gold));
        }

        [Fact]
        public void Ordering_ComplexitySignatureCountIsNotScaledByRarity()
        {
            // Theme owns the signature line, rarity owns the base bill. Wooden
            // warhammer, tox trait at commonality 0.5: Wood 150 -> x0.5 -> 75 ->
            // rarity 2x -> 150. The tox rule then adds its complexity-derived
            // 3 x 3 = 9 herbal medicine, the same 9 a commonality-1 trait pays,
            // because the signature is computed after rarity from WorkToMake.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                Warhammer(TraitCostTestHarness.WoodLog, QualityCategory.Normal),
                TraitCostTestHarness.MakeTrait("UMW_Opiated", "opiated", commonality: 0.5f));

            Assert.Equal(2, costs.Count);
            Assert.Equal(150, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.WoodLog));
            Assert.Equal(9, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.MedicineHerbal));
        }

        [Fact]
        public void Ordering_BloodSurchargeIsNotScaledByRarity()
        {
            // UWU_Blood is a fixed additive surcharge at priority 2200: 10
            // hemogen packs, whatever the trait's rarity. The base does scale —
            // Steel 150 -> x0.5 -> 75 -> rarity 2x -> 150.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                Warhammer(TraitCostTestHarness.Steel, QualityCategory.Normal),
                TraitCostTestHarness.MakeTrait(
                    "UMW_BloodStained", "blood-stained", commonality: 0.5f));

            Assert.Equal(150, TraitCostTestHarness.CountOf(costs, TraitCostTestHarness.Steel));
            Assert.Equal(10, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.HemogenPack));
        }

        [Fact]
        public void Ordering_HeavyScrapStaysOneSlagChunkAtAnyRarity()
        {
            // UWU_HeavyScrap (priority 1500) replaces the whole list, so
            // everything rarity did to the base is discarded — a heavy scrap
            // trait is one slag chunk even at the cap.
            List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(
                AssaultRifle(),
                TraitCostTestHarness.MakeTrait(
                    "VWE_HeavyScrapPlate", "heavy scrap", commonality: 0.05f));

            Assert.Single(costs);
            Assert.Equal(1, TraitCostTestHarness.CountOf(
                costs, TraitCostTestHarness.ChunkSlagSteel));
        }

        // ===== Refund symmetry ================================================

        [Fact]
        public void Refund_RemovalSeesTheSameScaledBase()
        {
            // Rarity lives inside RunPipeline, which GetAdditionCost and
            // GetRemovalCost both call, so the refund tracks the price with no
            // extra plumbing. Masterwork steel warhammer, gold inlay: addition
            // 57 gold at commonality 0.5 vs 29 at 1; removal refunds
            // floor(cost x RefundRate 0.5) -> 28 and 14.
            Thing warhammer = Warhammer(TraitCostTestHarness.Steel, QualityCategory.Masterwork);
            WeaponTraitDef rare = TraitCostTestHarness.MakeTrait(
                "UMW_GoldInlay", "gold inlay", commonality: 0.5f);
            WeaponTraitDef common = TraitCostTestHarness.MakeTrait(
                "UMW_GoldInlay", "gold inlay", commonality: 1f);

            Assert.Equal(28, TraitCostTestHarness.CountOf(
                TraitCostUtility.GetRemovalCost(warhammer, rare), TraitCostTestHarness.Gold));
            Assert.Equal(14, TraitCostTestHarness.CountOf(
                TraitCostUtility.GetRemovalCost(warhammer, common), TraitCostTestHarness.Gold));
        }

        [Fact]
        public void Refund_TotalRefundScalesWithRarityToo()
        {
            // GetTotalRefund aggregates raw pipeline output before applying
            // RefundRate, so it must see the scaled base as well: 57 gold ->
            // floor(57 x 0.5) = 28.
            List<ThingDefCountClass> refund = TraitCostUtility.GetTotalRefund(
                Warhammer(TraitCostTestHarness.Steel, QualityCategory.Masterwork),
                new List<WeaponTraitDef>
                {
                    TraitCostTestHarness.MakeTrait(
                        "UMW_GoldInlay", "gold inlay", commonality: 0.5f),
                });

            Assert.Equal(28, TraitCostTestHarness.CountOf(refund, TraitCostTestHarness.Gold));
        }

        [Fact]
        public void Refund_NegativeTraitRemovalIsAlsoExempt()
        {
            // Removing a negative trait costs the player materials. The
            // exemption applies on that pipeline too, so a rare drawback is no
            // more expensive to undo than a common one: Steel 150 -> x0.5 ->
            // Steel 75 (no downgrade on the removal path) -> RefundRate 0.5
            // rounded up -> Steel 38.
            Thing warhammer = Warhammer(TraitCostTestHarness.Steel, QualityCategory.Normal);

            List<ThingDefCountClass> rare = TraitCostUtility.GetRemovalCost(
                warhammer,
                TraitCostTestHarness.MakeTrait(
                    "UMW_Cumbersome", "cumbersome", marketValueOffset: -50f, commonality: 0.05f));

            Assert.Equal(38, TraitCostTestHarness.CountOf(rare, TraitCostTestHarness.Steel));
        }
    }
}
