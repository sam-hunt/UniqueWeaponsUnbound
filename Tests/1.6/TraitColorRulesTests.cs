using System.Collections.Generic;
using RimWorld;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Pins the dialog's trait-color rules (TraitColorRules): which color the
    // staged traits force, which trait colors the color tab offers, and the
    // invariant that a trait-only color cannot outlive its trait in either
    // mode of the enforceTraitColors setting.
    public class TraitColorRulesTests
    {
        private static readonly ColorDef Gold = new ColorDef { defName = "TestGold" };
        private static readonly ColorDef Jade = new ColorDef { defName = "TestJade" };
        private static readonly ColorDef Blue = new ColorDef { defName = "TestBlue" };

        private static WeaponTraitDef Trait(string defName, ColorDef forcedColor = null)
        {
            return new WeaponTraitDef { defName = defName, forcedColor = forcedColor };
        }

        private static List<WeaponTraitDef> Traits(params WeaponTraitDef[] traits)
        {
            return new List<WeaponTraitDef>(traits);
        }

        // --- ForcedColor ---

        [Fact]
        public void ForcedColor_NoColorTraits_Null()
        {
            Assert.Null(TraitColorRules.ForcedColor(
                Traits(Trait("A"), Trait("B")), enforce: true));
        }

        [Fact]
        public void ForcedColor_Enforced_ReturnsTraitColor()
        {
            Assert.Same(Gold, TraitColorRules.ForcedColor(
                Traits(Trait("A"), Trait("Inlay", Gold)), enforce: true));
        }

        // Mirrors vanilla's iteration in CompUniqueWeapon.PostPostMake.
        [Fact]
        public void ForcedColor_Enforced_LastWins()
        {
            Assert.Same(Jade, TraitColorRules.ForcedColor(
                Traits(Trait("Inlay", Gold), Trait("A"), Trait("Inlay2", Jade)), enforce: true));
        }

        // The whole of the opt-out: nothing is forced, so the dialog falls
        // through to the player's own pick everywhere.
        [Fact]
        public void ForcedColor_NotEnforced_Null()
        {
            Assert.Null(TraitColorRules.ForcedColor(
                Traits(Trait("Inlay", Gold), Trait("Inlay2", Jade)), enforce: false));
        }

        // --- StagedTraitColors ---

        [Fact]
        public void StagedTraitColors_NoColorTraits_Empty()
        {
            var into = new List<ColorDef> { Blue };
            TraitColorRules.StagedTraitColors(Traits(Trait("A")), into);
            Assert.Empty(into);
        }

        [Fact]
        public void StagedTraitColors_DistinctInTraitOrder()
        {
            var into = new List<ColorDef>();
            TraitColorRules.StagedTraitColors(
                Traits(Trait("Inlay", Jade), Trait("A"), Trait("Inlay2", Gold), Trait("Inlay3", Jade)),
                into);
            Assert.Equal(new[] { Jade, Gold }, into);
        }

        // --- RemovalStrandsSelection ---

        [Fact]
        public void Removal_SelectedTraitColor_NoOtherCarrier_Strands()
        {
            WeaponTraitDef inlay = Trait("Inlay", Gold);
            Assert.True(TraitColorRules.RemovalStrandsSelection(
                inlay, Traits(Trait("A")), Gold, selectedOfferedByPalette: false));
        }

        [Fact]
        public void Removal_LastTrait_Strands()
        {
            WeaponTraitDef inlay = Trait("Inlay", Gold);
            Assert.True(TraitColorRules.RemovalStrandsSelection(
                inlay, Traits(), Gold, selectedOfferedByPalette: false));
        }

        // A second staged trait with the same color still entitles the
        // weapon to it.
        [Fact]
        public void Removal_AnotherStagedTraitCarriesColor_Keeps()
        {
            WeaponTraitDef inlay = Trait("Inlay", Gold);
            Assert.False(TraitColorRules.RemovalStrandsSelection(
                inlay, Traits(Trait("Inlay2", Gold)), Gold, selectedOfferedByPalette: false));
        }

        // The player had already picked something else, so the trait going
        // changes nothing.
        [Fact]
        public void Removal_SelectionIsAnotherColor_Keeps()
        {
            WeaponTraitDef inlay = Trait("Inlay", Gold);
            Assert.False(TraitColorRules.RemovalStrandsSelection(
                inlay, Traits(), Blue, selectedOfferedByPalette: true));
        }

        // A trait whose color a regular palette also offers does not make
        // that color trait-only.
        [Fact]
        public void Removal_ColorAlsoOfferedByPalette_Keeps()
        {
            WeaponTraitDef inlay = Trait("Inlay", Gold);
            Assert.False(TraitColorRules.RemovalStrandsSelection(
                inlay, Traits(), Gold, selectedOfferedByPalette: true));
        }

        [Fact]
        public void Removal_TraitWithoutColor_Keeps()
        {
            Assert.False(TraitColorRules.RemovalStrandsSelection(
                Trait("A"), Traits(Trait("Inlay", Gold)), Gold, selectedOfferedByPalette: false));
        }

        // A colorless weapon (null selection) must not match a colorless
        // trait's null forcedColor.
        [Fact]
        public void Removal_NullSelection_Keeps()
        {
            Assert.False(TraitColorRules.RemovalStrandsSelection(
                Trait("A"), Traits(), null, selectedOfferedByPalette: false));
        }

        // --- IsCarried ---

        [Fact]
        public void IsCarried_MatchesForcedColorOnly()
        {
            List<WeaponTraitDef> traits = Traits(Trait("A"), Trait("Inlay", Gold));
            Assert.True(TraitColorRules.IsCarried(traits, Gold));
            Assert.False(TraitColorRules.IsCarried(traits, Jade));
            Assert.False(TraitColorRules.IsCarried(traits, null));
        }
    }
}
