using System.Collections.Generic;
using RimWorld;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Guards the core pass of TraitStatMutability: vanilla's
    // StatDef.SetImmutability marks stats referenced only by weapon-side
    // WeaponTraitDef.statOffsets/statFactors as immutable (per-Thing cached
    // forever), because vanilla never mutates a trait list after creation.
    // MarkMutable is our correction; these tests pin which stats it flips
    // and, as importantly, which it leaves alone.
    public class TraitStatMutabilityTests
    {
        private static StatDef ImmutableStat(string defName)
        {
            return new StatDef { defName = defName, immutable = true };
        }

        private static WeaponTraitDef TraitWithOffset(string defName, StatDef stat)
        {
            return new WeaponTraitDef
            {
                defName = defName,
                statOffsets = new List<StatModifier> { new StatModifier { stat = stat, value = 0.2f } },
            };
        }

        [Fact]
        public void OffsetReferencedImmutableStat_FlippedAndReported()
        {
            StatDef stat = ImmutableStat("RangeMult");
            List<StatDef> marked = TraitStatMutability.MarkMutable(
                new[] { TraitWithOffset("ExtendedBarrel", stat) });

            Assert.False(stat.immutable);
            Assert.Equal(new[] { stat }, marked);
        }

        [Fact]
        public void FactorReferencedImmutableStat_Flipped()
        {
            StatDef stat = ImmutableStat("WarmupMult");
            var trait = new WeaponTraitDef
            {
                defName = "MatchTrigger",
                statFactors = new List<StatModifier> { new StatModifier { stat = stat, value = 1.15f } },
            };

            List<StatDef> marked = TraitStatMutability.MarkMutable(new[] { trait });

            Assert.False(stat.immutable);
            Assert.Single(marked);
        }

        [Fact]
        public void AlreadyMutableStat_LeftAloneAndUnreported()
        {
            var stat = new StatDef { defName = "MarketValue", immutable = false };
            List<StatDef> marked = TraitStatMutability.MarkMutable(
                new[] { TraitWithOffset("Ornate", stat) });

            Assert.False(stat.immutable);
            Assert.Empty(marked);
        }

        [Fact]
        public void NullModifierListsAndStats_Tolerated()
        {
            var bare = new WeaponTraitDef { defName = "Bare" };
            var nullStat = new WeaponTraitDef
            {
                defName = "NullStat",
                statOffsets = new List<StatModifier> { new StatModifier { stat = null, value = 1f } },
            };

            List<StatDef> marked = TraitStatMutability.MarkMutable(new[] { bare, nullStat });

            Assert.Empty(marked);
        }

        [Fact]
        public void StatSharedByTwoTraits_FlippedOnce()
        {
            StatDef stat = ImmutableStat("RangeMult");
            List<StatDef> marked = TraitStatMutability.MarkMutable(new[]
            {
                TraitWithOffset("ExtendedBarrel", stat),
                TraitWithOffset("LightweightArrows", stat),
            });

            Assert.Single(marked);
        }
    }
}
