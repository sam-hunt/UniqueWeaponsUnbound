using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Vanilla derives per-stat cacheability from the invariant that a
    // weapon's trait list is fixed at creation: StatDef.SetImmutability's
    // mutable-stat scan covers WeaponTraitDef.equippedStatOffsets (pawn-side
    // — pawns swap equipment, so those must stay live) but not the
    // weapon-side statOffsets/statFactors, which in vanilla can never change
    // after PostPostMake rolls the traits. Stats referenced only from there —
    // RangedWeapon_RangeMultiplier and RangedWeapon_WarmupMultiplier in
    // vanilla, plus whatever modded traits touch — therefore pass
    // IsImmutable(), and StatWorker.GetValue freezes each Thing's first
    // computed value in its per-Thing immutableStatCache forever.
    //
    // This mod's purpose is mutating trait lists at runtime, which breaks
    // that invariant: after a trait add/remove, the weapon's info card AND
    // its gameplay stats (Verb_LaunchProjectile.EffectiveRange / WarmupTime
    // read through the same frozen cache) keep the pre-customization values
    // until a save reload recreates the Thing. The customization dialog's
    // preview thing freezes the same way at its first info-card open.
    // Re-marking the trait-referenced stats mutable removes the per-Thing
    // cache entirely, restoring live computation — the same treatment
    // vanilla gives equippedStatOffsets, for the same reason.
    //
    // Two application points share MarkTraitStatsMutable: ModInitializer
    // calls it once at startup (the initial SetImmutability runs during def
    // load, before mod static ctors, so a patch alone can't catch it), and
    // StatDef_SetImmutability_Patch re-applies it whenever vanilla
    // recomputes immutability (dev-mode def hot-reload runs
    // ResetStaticDataPost again, which would otherwise silently restore the
    // stale caching).
    public static class TraitStatMutability
    {
        public static void MarkTraitStatsMutable()
        {
            MarkMutable(DefDatabase<WeaponTraitDef>.AllDefsListForReading);
        }

        // Core pass, separated from the DefDatabase walk for headless
        // testability. Returns the stats actually flipped; a stat referenced
        // by several traits is flipped (and reported) once.
        internal static List<StatDef> MarkMutable(IEnumerable<WeaponTraitDef> traits)
        {
            var marked = new List<StatDef>();
            foreach (WeaponTraitDef trait in traits)
            {
                Collect(trait.statOffsets, marked);
                Collect(trait.statFactors, marked);
            }
            return marked;
        }

        private static void Collect(List<StatModifier> mods, List<StatDef> marked)
        {
            if (mods == null)
                return;
            foreach (StatModifier mod in mods)
            {
                StatDef stat = mod?.stat;
                if (stat?.immutable != true)
                    continue;
                stat.immutable = false;
                stat.Worker.SetCacheability(false);
                marked.Add(stat);
            }
        }
    }
}
