using System.Collections.Generic;

namespace UniqueWeaponsUnbound
{
    // First-wins dedupe of the customization dialog's texture variant display
    // list. When the mod owning a weapon's textures is loaded more than once
    // (local Mods/ copy alongside the Workshop subscription), vanilla
    // ContentFinder.GetAllInFolder yields each texture once per loaded copy and
    // Graphic_Collection.Init never dedupes across mods, so subGraphics[] holds
    // every variant N times. Only the DISPLAY list is deduped here: selection
    // is applied via Thing.overrideGraphicIndex, which indexes the full
    // subGraphics[] array, so every surviving index stays a full-array index
    // and the index domain never changes.
    internal static class TextureVariantDeduper
    {
        // keys[i] is the dedupe key for full-array variant index i — the
        // variant's main texture name, or null when unresolvable (BadGraphic
        // fallbacks, non-random graphics). Duplicate copies of a double-loaded
        // texture share the filename, so first-wins per name matches the
        // direct ContentFinder.Get lookup semantics the map renderer sees.
        //
        // uniqueIndexes: display list — the first full-array index per key, in
        // array order. A null key never matches anything, so its index is
        // always kept (never dropped on a lookup failure).
        // canonicalIndexes: full index -> first full index sharing its key
        // (itself when unique), letting callers compare a selection that
        // points at a duplicate copy against the cell that represents it.
        internal static void Compute(IList<string> keys,
            out List<int> uniqueIndexes, out int[] canonicalIndexes)
        {
            int count = keys.Count;
            uniqueIndexes = new List<int>(count);
            canonicalIndexes = new int[count];

            var firstIndexByKey = new Dictionary<string, int>();
            for (int i = 0; i < count; i++)
            {
                string key = keys[i];
                if (key != null && firstIndexByKey.TryGetValue(key, out int first))
                {
                    canonicalIndexes[i] = first;
                    continue;
                }

                if (key != null)
                    firstIndexByKey[key] = i;
                canonicalIndexes[i] = i;
                uniqueIndexes.Add(i);
            }
        }
    }
}
