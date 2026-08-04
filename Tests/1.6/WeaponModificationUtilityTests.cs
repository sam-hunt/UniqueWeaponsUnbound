using System.Collections.Generic;
using RimWorld;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Guards the ability-projection comparison that gates the preview's
    // ability-comp re-wire in WeaponModificationUtility.StampTraits. The gate
    // is what keeps the eager Ability construction (and its global ability-id
    // draw) off the common preview-rebuild path, so the cases that must NOT
    // re-wire matter as much as the ones that must.
    public class WeaponModificationUtilityTests
    {
        private static WeaponTraitDef Plain(string defName)
        {
            return new WeaponTraitDef { defName = defName };
        }

        private static WeaponTraitDef WithAbility(
            string defName, CompProperties_EquippableAbilityReloadable props)
        {
            return new WeaponTraitDef { defName = defName, abilityProps = props };
        }

        private static readonly CompProperties_EquippableAbilityReloadable PropsA =
            new CompProperties_EquippableAbilityReloadable();

        private static readonly CompProperties_EquippableAbilityReloadable PropsB =
            new CompProperties_EquippableAbilityReloadable();

        [Fact]
        public void BothEmpty_Same()
        {
            Assert.True(WeaponModificationUtility.SameAbilityProjection(
                new List<WeaponTraitDef>(), new List<WeaponTraitDef>()));
        }

        [Fact]
        public void NonAbilityTraitChanges_Same()
        {
            var current = new List<WeaponTraitDef> { Plain("Sharp") };
            var desired = new List<WeaponTraitDef> { Plain("Heavy"), Plain("Ornate") };
            Assert.True(WeaponModificationUtility.SameAbilityProjection(current, desired));
        }

        [Fact]
        public void SameAbilityTraitAmongDifferentNeighbors_Same()
        {
            WeaponTraitDef launcher = WithAbility("SmokeLauncher", PropsA);
            var current = new List<WeaponTraitDef> { Plain("Sharp"), launcher };
            var desired = new List<WeaponTraitDef> { launcher, Plain("Heavy") };
            Assert.True(WeaponModificationUtility.SameAbilityProjection(current, desired));
        }

        [Fact]
        public void AbilityTraitAdded_Different()
        {
            var current = new List<WeaponTraitDef> { Plain("Sharp") };
            var desired = new List<WeaponTraitDef>
            {
                Plain("Sharp"), WithAbility("SmokeLauncher", PropsA),
            };
            Assert.False(WeaponModificationUtility.SameAbilityProjection(current, desired));
        }

        [Fact]
        public void AbilityTraitRemoved_Different()
        {
            var current = new List<WeaponTraitDef> { WithAbility("SmokeLauncher", PropsA) };
            var desired = new List<WeaponTraitDef>();
            Assert.False(WeaponModificationUtility.SameAbilityProjection(current, desired));
        }

        [Fact]
        public void AbilityTraitSwapped_Different()
        {
            var current = new List<WeaponTraitDef> { WithAbility("SmokeLauncher", PropsA) };
            var desired = new List<WeaponTraitDef> { WithAbility("EMPLauncher", PropsB) };
            Assert.False(WeaponModificationUtility.SameAbilityProjection(current, desired));
        }

        // Vanilla exclusionTags make two ability traits rare but not
        // impossible (partially-healed saves, non-vanilla wiring), and the
        // comparison must not conflate one ability trait with two.
        [Fact]
        public void SecondAbilityTraitAdded_Different()
        {
            var current = new List<WeaponTraitDef> { WithAbility("SmokeLauncher", PropsA) };
            var desired = new List<WeaponTraitDef>
            {
                WithAbility("SmokeLauncher", PropsA), WithAbility("EMPLauncher", PropsB),
            };
            Assert.False(WeaponModificationUtility.SameAbilityProjection(current, desired));
        }
    }
}
