using HarmonyLib;
using RimWorld;

namespace UniqueWeaponsUnbound.Patches
{
    // Re-applies the trait-stat mutability correction whenever vanilla
    // recomputes stat immutability. The initial SetImmutability call happens
    // during def load, before mod static ctors run, so the startup half of
    // the correction is a direct call from ModInitializer; this postfix
    // covers every later recompute (dev-mode def hot-reload runs
    // ResetStaticDataPost → SetImmutability again, which would otherwise
    // silently restore the stale per-Thing caching). Rationale and the full
    // vanilla-invariant story live on TraitStatMutability.
    [HarmonyPatch(typeof(StatDef), nameof(StatDef.SetImmutability))]
    public static class StatDef_SetImmutability_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            TraitStatMutability.MarkTraitStatsMutable();
        }
    }
}
