using System.Collections.Generic;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Unit coverage for TextureVariantDeduper.Compute: first-wins dedupe by key,
    // with null keys always unique and never merging with each other.
    public class TextureVariantDedupeTests
    {
        [Fact]
        public void NoDuplicates_UniqueIsIdentityAndCanonicalIsIdentity()
        {
            // The healthy-install case: nothing double-loaded, so the display
            // list and canonical map must be a bitwise no-op.
            TextureVariantDeduper.Compute(
                new[] { "A", "B", "C" }, out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Equal(new[] { 0, 1, 2 }, uniqueIndexes);
            Assert.Equal(new[] { 0, 1, 2 }, canonicalIndexes);
        }

        [Fact]
        public void FullDoubleLoad_SecondCopyMapsBackToTheFirst()
        {
            TextureVariantDeduper.Compute(
                new[] { "A", "B", "C", "A", "B", "C" },
                out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Equal(new[] { 0, 1, 2 }, uniqueIndexes);
            Assert.Equal(new[] { 0, 1, 2, 0, 1, 2 }, canonicalIndexes);
        }

        [Fact]
        public void InterleavedDuplicates_EachPairCollapsesToItsFirstIndex()
        {
            TextureVariantDeduper.Compute(
                new[] { "A", "A", "B", "B" }, out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Equal(new[] { 0, 2 }, uniqueIndexes);
            Assert.Equal(new[] { 0, 0, 2, 2 }, canonicalIndexes);
        }

        [Fact]
        public void TripleLoad_AllThreeCopiesCollapseToTheFirst()
        {
            TextureVariantDeduper.Compute(
                new[] { "A", "A", "A" }, out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Equal(new[] { 0 }, uniqueIndexes);
            Assert.Equal(new[] { 0, 0, 0 }, canonicalIndexes);
        }

        [Fact]
        public void NullKeys_NeverMergeWithEachOther()
        {
            TextureVariantDeduper.Compute(
                new[] { null, null, "A", null }, out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Equal(new[] { 0, 1, 2, 3 }, uniqueIndexes);
            Assert.Equal(new[] { 0, 1, 2, 3 }, canonicalIndexes);
        }

        [Fact]
        public void NullKeyBetweenDuplicates_DoesNotDisturbFirstWins()
        {
            TextureVariantDeduper.Compute(
                new[] { "A", null, "A" }, out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Equal(new[] { 0, 1 }, uniqueIndexes);
            Assert.Equal(new[] { 0, 1, 0 }, canonicalIndexes);
        }

        [Fact]
        public void EmptyList_ProducesEmptyOutputsWithoutThrowing()
        {
            TextureVariantDeduper.Compute(
                new string[0], out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Empty(uniqueIndexes);
            Assert.Empty(canonicalIndexes);
        }

        [Fact]
        public void SingleEntry_IsUniqueAndCanonicalOfItself()
        {
            TextureVariantDeduper.Compute(
                new[] { "A" }, out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Equal(new[] { 0 }, uniqueIndexes);
            Assert.Equal(new[] { 0 }, canonicalIndexes);
        }

        [Fact]
        public void MixedInput_EveryUniqueIndexIsCanonicalOfItself()
        {
            // Self-consistency check on a mixed case: every index the display
            // list keeps must map to itself in the canonical array.
            TextureVariantDeduper.Compute(
                new[] { "A", "B", "A", null, "B" },
                out List<int> uniqueIndexes, out int[] canonicalIndexes);

            Assert.Equal(new[] { 0, 1, 3 }, uniqueIndexes);
            Assert.Equal(new[] { 0, 1, 0, 3, 1 }, canonicalIndexes);
            foreach (int index in uniqueIndexes)
                Assert.Equal(index, canonicalIndexes[index]);
        }
    }
}
