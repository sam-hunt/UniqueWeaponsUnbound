using System.Collections.Generic;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Tests for the duck-typed trait-effect-lines contract read by
    // TraitEffectLinesIntegration. The publisher (Unique Melee Weapons) is a soft dependency we
    // hold no reference to, so the shape it must present is pinned here instead: a DefModExtension
    // whose SIMPLE type name is "TraitEffectLinesExtension" exposing a public List<string> "lines".
    //
    // Note what this can and cannot catch. It guards OUR reader against regressions — matching the
    // wrong name, missing the field, mangling the indent. It cannot detect the publisher renaming
    // its own type or field, since that lives in another assembly; the guard for that direction is
    // the contract note in both repos' CLAUDE.md.
    //
    // Publishers are resolved from an explicit type list rather than the startup scan, which walks
    // LoadedModManager and can't run headless. ResolvePublishers is the same code path the static
    // ctor drives, just fed a list instead of GenTypes.
    public class TraitEffectLinesIntegrationTests
    {
        // Stands in for the publisher's extension. Only the simple type name and the field matter,
        // which is exactly the point of the contract — this class shares no code with the real one.
        private class TraitEffectLinesExtension : DefModExtension
        {
            public List<string> lines = new List<string>();
        }

        private class UnrelatedExtension : DefModExtension
        {
            public List<string> lines = new List<string> { "should be ignored" };
        }

        // Matches the contract name but exposes the wrong shape — the drift case.
        private static class DriftedPublisher
        {
            internal class TraitEffectLinesExtension : DefModExtension
            {
                public string lines = "not a list";
            }
        }

        public TraitEffectLinesIntegrationTests()
        {
            TraitEffectLinesIntegration.ResolvePublishers(
                new[] { typeof(TraitEffectLinesExtension), typeof(UnrelatedExtension) });
        }

        private static WeaponTraitDef TraitWith(params DefModExtension[] extensions)
        {
            return new WeaponTraitDef
            {
                defName = "TestTrait",
                modExtensions = new List<DefModExtension>(extensions),
            };
        }

        [Fact]
        public void PublishedLines_AreAppendedWithIndent()
        {
            WeaponTraitDef trait = TraitWith(new TraitEffectLinesExtension
            {
                lines = { "On hit (25%): stun for 2s", "Grants ability: earthshake" },
            });
            var effectLines = new List<string>();

            TraitEffectLinesIntegration.AppendEffectLines(trait, effectLines, "  ");

            Assert.Equal(
                new[] { "  On hit (25%): stun for 2s", "  Grants ability: earthshake" },
                effectLines);
        }

        [Fact]
        public void PublishedLines_PreserveOrderAfterExistingRows()
        {
            WeaponTraitDef trait = TraitWith(new TraitEffectLinesExtension { lines = { "Cut damage x90%" } });
            var effectLines = new List<string> { "  Beauty: -2" };

            TraitEffectLinesIntegration.AppendEffectLines(trait, effectLines, "  ");

            Assert.Equal(new[] { "  Beauty: -2", "  Cut damage x90%" }, effectLines);
        }

        [Fact]
        public void EmptyLines_AreSkipped()
        {
            WeaponTraitDef trait = TraitWith(new TraitEffectLinesExtension
            {
                lines = { "Cut damage x90%", "", null },
            });
            var effectLines = new List<string>();

            TraitEffectLinesIntegration.AppendEffectLines(trait, effectLines, "  ");

            Assert.Equal(new[] { "  Cut damage x90%" }, effectLines);
        }

        // The return value drives tooltip de-duplication: the caller drops its own vanilla
        // equippedStatOffsets rows when a publisher already described the trait. "Produced a line" is
        // the contract, not "attached an extension" — an extension carrying nothing printable must
        // report false, or the caller would suppress a row and replace it with silence.
        [Fact]
        public void AppendEffectLines_ReportsWhetherALineWasProduced()
        {
            var effectLines = new List<string>();

            Assert.True(TraitEffectLinesIntegration.AppendEffectLines(
                TraitWith(new TraitEffectLinesExtension { lines = { "Cut damage x90%" } }),
                effectLines, "  "));

            Assert.False(TraitEffectLinesIntegration.AppendEffectLines(
                TraitWith(new TraitEffectLinesExtension { lines = { "", null } }), effectLines, "  "));

            Assert.False(TraitEffectLinesIntegration.AppendEffectLines(
                TraitWith(new UnrelatedExtension()), effectLines, "  "));

            Assert.False(TraitEffectLinesIntegration.AppendEffectLines(
                new WeaponTraitDef { defName = "TestTrait" }, effectLines, "  "));
        }

        [Fact]
        public void ExtensionWithDifferentName_IsIgnored()
        {
            WeaponTraitDef trait = TraitWith(new UnrelatedExtension());
            var effectLines = new List<string>();

            TraitEffectLinesIntegration.AppendEffectLines(trait, effectLines, "  ");

            Assert.Empty(effectLines);
        }

        [Fact]
        public void NoExtensions_IsNoOp()
        {
            var trait = new WeaponTraitDef { defName = "TestTrait" };
            var effectLines = new List<string>();

            TraitEffectLinesIntegration.AppendEffectLines(trait, effectLines, "  ");

            Assert.Empty(effectLines);
        }

        [Fact]
        public void NameMatchWithWrongFieldType_IsReportedAsDrift()
        {
            List<string> drifted = TraitEffectLinesIntegration.ResolvePublishers(
                new[] { typeof(DriftedPublisher.TraitEffectLinesExtension) });

            Assert.Equal(
                new[] { typeof(DriftedPublisher.TraitEffectLinesExtension).FullName },
                drifted);
        }

        [Fact]
        public void ContractShapedType_IsNotReportedAsDrift()
        {
            List<string> drifted = TraitEffectLinesIntegration.ResolvePublishers(
                new[] { typeof(TraitEffectLinesExtension), typeof(UnrelatedExtension) });

            Assert.Empty(drifted);
        }
    }
}
