using System;
using System.Runtime.Serialization;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Covers the pure half of the skill prerequisite: the tech-tier fallback
    // table, per-weapon requirement derivation under each kind, and the
    // Vanilla Skills Expanded fallback. Pawn evaluation needs a live skill
    // tracker (ModsConfig, Biotech aptitude paths) and stays in-game.
    //
    // VSE is never available under the headless harness (ModsConfig has no
    // data, so the integration's static ctor bails out), which is exactly the
    // state the fallback tests need.
    public class SkillCheckRulesTests
    {
        private static readonly SkillDef Crafting = new SkillDef { defName = "Crafting", skillLabel = "crafting" };
        private static readonly SkillDef Artistic = new SkillDef { defName = "Artistic", skillLabel = "artistic" };

        public SkillCheckRulesTests()
        {
            TraitCostTestHarness.Bootstrap();
            SkillDefOf.Crafting = Crafting;
        }

        private static ThingDef MakeWeapon(TechLevel techLevel, RecipeMakerProperties recipeMaker = null)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = "TestWeapon_" + Guid.NewGuid().ToString("N");
            def.techLevel = techLevel;
            def.recipeMaker = recipeMaker;
            return def;
        }

        private static RecipeMakerProperties RecipeRequiring(params SkillRequirement[] requirements)
        {
            return new RecipeMakerProperties
            {
                skillRequirements = requirements.Length == 0
                    ? null
                    : new System.Collections.Generic.List<SkillRequirement>(requirements),
            };
        }

        private static UWU_Settings SettingsFor(
            SkillCheckSubject subject, SkillCheckKind kind, int flatLevel = 10)
        {
            var settings = new UWU_Settings();
            settings.ResetToDefaults();
            settings.skillCheckSubject = subject;
            settings.skillCheckKind = kind;
            settings.skillCheckMinimumLevel = flatLevel;
            return settings;
        }

        [Fact]
        public void Defaults_AreOptOut()
        {
            var settings = new UWU_Settings();
            Assert.Equal(SkillCheckSubject.None, settings.skillCheckSubject);
            Assert.Equal(SkillCheckKind.RecipeOrTechTier, settings.skillCheckKind);
            Assert.Equal(10, settings.skillCheckMinimumLevel);

            settings.skillCheckSubject = SkillCheckSubject.BestAnywhere;
            settings.skillCheckKind = SkillCheckKind.FlatMinimum;
            settings.skillCheckMinimumLevel = 3;
            settings.ResetToDefaults();
            Assert.Equal(SkillCheckSubject.None, settings.skillCheckSubject);
            Assert.Equal(SkillCheckKind.RecipeOrTechTier, settings.skillCheckKind);
            Assert.Equal(10, settings.skillCheckMinimumLevel);
        }

        [Fact]
        public void TechTierTable_IsMonotonicAndCoversEveryTier()
        {
            int previous = int.MinValue;
            foreach (TechLevel tier in Enum.GetValues(typeof(TechLevel)))
            {
                int level = SkillCheckRules.TechTierMinimumCraftingSkill(tier);
                Assert.InRange(level, SkillCheckRules.MinFlatLevel, SkillCheckRules.MaxFlatLevel);
                Assert.True(level >= previous, $"{tier} ({level}) is below the previous tier ({previous})");
                previous = level;
            }
            // Anchors from the vanilla recipe survey: industrial uncraftables
            // (mech guns) sit at the craftable max of 7, spacer at 9.
            Assert.Equal(7, SkillCheckRules.TechTierMinimumCraftingSkill(TechLevel.Industrial));
            Assert.Equal(9, SkillCheckRules.TechTierMinimumCraftingSkill(TechLevel.Spacer));
            Assert.Equal(SkillCheckRules.WeaponsmithFallbackLevel,
                SkillCheckRules.TechTierMinimumCraftingSkill(TechLevel.Archotech));
        }

        [Fact]
        public void RecipeKind_UsesRecipeRequirements()
        {
            ThingDef weapon = MakeWeapon(TechLevel.Industrial,
                RecipeRequiring(new SkillRequirement { skill = Crafting, minLevel = 6 }));
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.CustomizingPawn, SkillCheckKind.RecipeOrTechTier)))
            {
                var req = SkillCheckRules.GetRequirement(weapon, null, TechLevel.Industrial);
                Assert.False(req.RequiresWeaponsmithExpertise);
                Assert.Single(req.Skills);
                Assert.Same(Crafting, req.Skills[0].skill);
                Assert.Equal(6, req.Skills[0].minLevel);
            }
        }

        [Fact]
        public void RecipeKind_PrefersBaseDefRecipe_ThenUnique()
        {
            ThingDef baseDef = MakeWeapon(TechLevel.Industrial,
                RecipeRequiring(new SkillRequirement { skill = Crafting, minLevel = 4 }));
            ThingDef uniqueDef = MakeWeapon(TechLevel.Industrial,
                RecipeRequiring(new SkillRequirement { skill = Crafting, minLevel = 9 }));
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.CustomizingPawn, SkillCheckKind.RecipeOrTechTier)))
            {
                Assert.Equal(4, SkillCheckRules.GetRequirement(baseDef, uniqueDef, TechLevel.Industrial).Skills[0].minLevel);
                Assert.Equal(9, SkillCheckRules.GetRequirement(null, uniqueDef, TechLevel.Industrial).Skills[0].minLevel);
            }
        }

        [Fact]
        public void RecipeKind_KeepsNonCraftingRequirements_DropsZeroAndNullSkill()
        {
            ThingDef weapon = MakeWeapon(TechLevel.Medieval,
                RecipeRequiring(
                    new SkillRequirement { skill = Crafting, minLevel = 3 },
                    new SkillRequirement { skill = Artistic, minLevel = 5 },
                    new SkillRequirement { skill = Crafting, minLevel = 0 },
                    new SkillRequirement { skill = null, minLevel = 8 }));
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.BestOnMap, SkillCheckKind.RecipeOrTechTier)))
            {
                var req = SkillCheckRules.GetRequirement(weapon, null, TechLevel.Medieval);
                Assert.Equal(2, req.Skills.Count);
                Assert.Contains(req.Skills, sr => sr.skill == Artistic && sr.minLevel == 5);
            }
        }

        [Fact]
        public void RecipeKind_CraftableWithoutRequirement_IsEmpty()
        {
            // A knife or grenade: anyone can craft it, so anyone may customize it.
            ThingDef weapon = MakeWeapon(TechLevel.Industrial, RecipeRequiring());
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.CustomizingPawn, SkillCheckKind.RecipeOrTechTier)))
            {
                Assert.True(SkillCheckRules.GetRequirement(weapon, null, TechLevel.Industrial).IsEmpty);
            }
        }

        [Fact]
        public void RecipeKind_Uncraftable_FallsBackToTechTier()
        {
            ThingDef weapon = MakeWeapon(TechLevel.Spacer);
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.CustomizingPawn, SkillCheckKind.RecipeOrTechTier)))
            {
                var req = SkillCheckRules.GetRequirement(weapon, null, TechLevel.Spacer);
                Assert.Single(req.Skills);
                Assert.Same(Crafting, req.Skills[0].skill);
                Assert.Equal(SkillCheckRules.TechTierMinimumCraftingSkill(TechLevel.Spacer), req.Skills[0].minLevel);
            }
        }

        [Fact]
        public void FlatKind_UsesSliderLevel_IgnoringRecipe()
        {
            ThingDef weapon = MakeWeapon(TechLevel.Industrial,
                RecipeRequiring(new SkillRequirement { skill = Crafting, minLevel = 3 }));
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum, flatLevel: 12)))
            {
                var req = SkillCheckRules.GetRequirement(weapon, null, TechLevel.Industrial);
                Assert.Single(req.Skills);
                Assert.Equal(12, req.Skills[0].minLevel);
            }
        }

        [Fact]
        public void FlatKind_ClampsOutOfRangeLevel_AndZeroIsEmpty()
        {
            ThingDef weapon = MakeWeapon(TechLevel.Industrial);
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum, flatLevel: 99)))
            {
                Assert.Equal(SkillCheckRules.MaxFlatLevel,
                    SkillCheckRules.GetRequirement(weapon, null, TechLevel.Industrial).Skills[0].minLevel);
            }
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.CustomizingPawn, SkillCheckKind.FlatMinimum, flatLevel: 0)))
            {
                Assert.True(SkillCheckRules.GetRequirement(weapon, null, TechLevel.Industrial).IsEmpty);
            }
        }

        [Fact]
        public void WeaponsmithKind_WithoutVse_FallsBackToFlatFifteen()
        {
            Assert.False(VanillaSkillsExpandedIntegration.Available);
            ThingDef weapon = MakeWeapon(TechLevel.Neolithic,
                RecipeRequiring(new SkillRequirement { skill = Crafting, minLevel = 2 }));
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.BestAnywhere, SkillCheckKind.WeaponsmithExpertise, flatLevel: 4)))
            {
                Assert.True(SkillCheckRules.WeaponsmithFallbackActive);
                Assert.Equal(SkillCheckKind.FlatMinimum, SkillCheckRules.EffectiveKind(out int level));
                Assert.Equal(SkillCheckRules.WeaponsmithFallbackLevel, level);

                var req = SkillCheckRules.GetRequirement(weapon, null, TechLevel.Neolithic);
                Assert.False(req.RequiresWeaponsmithExpertise);
                Assert.Single(req.Skills);
                Assert.Equal(SkillCheckRules.WeaponsmithFallbackLevel, req.Skills[0].minLevel);

                // The stored selection is left alone so installing VSE restores it.
                Assert.Equal(SkillCheckKind.WeaponsmithExpertise, UWU_Mod.Settings.skillCheckKind);
                Assert.Equal(4, UWU_Mod.Settings.skillCheckMinimumLevel);
            }
        }

        [Fact]
        public void EffectiveKind_PassesThroughWhenNoFallback()
        {
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.BestOnMap, SkillCheckKind.RecipeOrTechTier, flatLevel: 7)))
            {
                Assert.False(SkillCheckRules.WeaponsmithFallbackActive);
                Assert.Equal(SkillCheckKind.RecipeOrTechTier, SkillCheckRules.EffectiveKind(out int level));
                Assert.Equal(7, level);
                Assert.True(SkillCheckRules.Enabled);
            }
            using (TraitCostTestHarness.OverrideSettings(
                SettingsFor(SkillCheckSubject.None, SkillCheckKind.FlatMinimum)))
            {
                Assert.False(SkillCheckRules.Enabled);
            }
        }

        [Fact]
        public void PawnSatisfies_NullPawn_IsFalse()
        {
            var req = new SkillCheckRules.Requirement();
            req.Skills.Add(new SkillRequirement { skill = Crafting, minLevel = 1 });
            Assert.False(SkillCheckRules.PawnSatisfies(null, req));
        }
    }
}
