using System.Collections.Generic;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // All trait-cost test classes share this collection so xunit serializes
    // them: the pipeline's rule list, the material caches, ThingDefOf/StatDefOf
    // and Prefs are process-global, and tests swap the rule list per scenario.
    [CollectionDefinition("TraitCost")]
    public class TraitCostCollection
    {
    }

    // Guards the harness's own assumptions about what the headless runner can
    // do. If one of these fails, every other trait-cost test result is suspect.
    [Collection("TraitCost")]
    public class TraitCostHarnessTests
    {
        public TraitCostHarnessTests()
        {
            TraitCostTestHarness.Bootstrap();
        }

        [Fact]
        public void SyntheticStatDefs_ResolveMarketValue()
        {
            Assert.Equal(1.9f, TraitCostTestHarness.Steel.BaseMarketValue, 3);
            Assert.Equal(10f, TraitCostTestHarness.Gold.BaseMarketValue, 3);
            Assert.Equal(200f, TraitCostTestHarness.ComponentSpacer.BaseMarketValue, 3);
        }

        [Fact]
        public void SyntheticWeaponDefs_ResolveWorkToMake()
        {
            Thing warhammer = TraitCostTestHarness.MakeWeapon(
                "TestHammer", TechLevel.Medieval, workToMake: 18000f, costStuffCount: 150);

            Assert.Equal(
                18000f, warhammer.def.GetStatValueAbstract(StatDefOf.WorkToMake), 1);
        }

        [Fact]
        public void DevMode_IsOff()
        {
            Assert.False(Prefs.DevMode);
        }

        [Fact]
        public void MaterialCaches_ClassifyRawResources()
        {
            Assert.True(CostRuleHelpers.IsRawResource(TraitCostTestHarness.Steel));
            Assert.True(CostRuleHelpers.IsRawResource(TraitCostTestHarness.Gold));
            Assert.True(CostRuleHelpers.IsRawResource(TraitCostTestHarness.Bioferrite));
            // Vanilla ComponentIndustrial declares stuffProps, so shipped code
            // classes it raw. Load-bearing: the stuff-agnostic split reaches it.
            Assert.True(CostRuleHelpers.IsRawResource(TraitCostTestHarness.ComponentIndustrial));

            Assert.False(CostRuleHelpers.IsRawResource(TraitCostTestHarness.ComponentSpacer));
            Assert.False(CostRuleHelpers.IsRawResource(TraitCostTestHarness.Chemfuel));
            Assert.False(CostRuleHelpers.IsRawResource(TraitCostTestHarness.MedicineHerbal));
            Assert.False(CostRuleHelpers.IsRawResource(TraitCostTestHarness.ChunkSlagSteel));
        }

        [Fact]
        public void MaterialCaches_ResolveOptionalDefs()
        {
            Assert.Same(TraitCostTestHarness.Birdskin, CostRuleHelpers.Birdskin);
            Assert.Same(TraitCostTestHarness.Bioferrite, CostRuleHelpers.Bioferrite);
            Assert.Same(TraitCostTestHarness.ChunkSlagSteel, CostRuleHelpers.SteelSlagChunk);
            Assert.Same(TraitCostTestHarness.Thrumbofur, CostRuleHelpers.Thrumbofur);
            Assert.Same(TraitCostTestHarness.MedicineHerbal, CostRuleHelpers.HerbalMedicine);
        }

        [Fact]
        public void Weapon_StuffRoundTrips()
        {
            Thing club = TraitCostTestHarness.MakeWeapon(
                "TestClub", TechLevel.Neolithic, costStuffCount: 50,
                stuff: TraitCostTestHarness.WoodLog);

            Assert.Same(TraitCostTestHarness.WoodLog, club.Stuff);
        }

        [Fact]
        public void ShippedRuleXml_LoadsEveryRule()
        {
            List<TraitCostRuleDef> rules = TraitCostTestHarness.LoadShippedRules();

            // Sanity on the parse: every rule has a worker, and the well-known
            // foundation and fallback rules are present.
            Assert.All(rules, r => Assert.NotNull(r.workerClass));
            Assert.Contains(rules, r => r.defName == "UWU_BaseCostFromTechLevel");
            Assert.Contains(rules, r => r.defName == "UWU_MaterialOverride");
            Assert.Contains(rules, r => r.defName == "UWU_CostPrune");
            Assert.Contains(rules, r => r.defName == "UWU_ToxSwap");

            // Phase 2 dropped the Bow gate: the swap only touches wood costs, so
            // it already no-ops on non-wood weapons, and the gate matched the
            // trait's category, which modded melee traits never carry.
            TraitCostRuleDef lightweight = rules.Find(r => r.defName == "UWU_Lightweight");
            Assert.NotNull(lightweight);
            Assert.Null(lightweight.weaponCategories);
        }
    }
}
