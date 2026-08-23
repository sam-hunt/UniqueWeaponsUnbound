using HarmonyLib;
using Verse;

namespace UniqueWeaponsUnbound
{
    [StaticConstructorOnStartup]
    public static class UniqueWeaponsUnboundMod
    {
        static UniqueWeaponsUnboundMod()
        {
            var report = new InitDiagnostics();

            report.Time("Harmony patching", () =>
            {
                var harmony = new Harmony("shunter.uniqueweaponsunbound");
                harmony.PatchAll();
            });

            // The load-time StatDef.SetImmutability ran before any mod static
            // ctor, so the postfix in StatDef_SetImmutability_Patch couldn't
            // catch it — apply the trait-stat mutability correction directly
            // once here. The postfix owns every later recompute, including the
            // one inside each in-process play-data reload (PlayDataLoader.
            // ResetStaticDataPost calls SetImmutability on every load).
            report.Time("TraitStatMutability", TraitStatMutability.MarkTraitStatsMutable);

            // Def-derived caches live in UWU_Startup.Run, which must fire once
            // per play-data LOAD (a mid-session language switch replaces every
            // def instance), not once per process. The CallAll postfix armed by
            // PatchAll above covers every reload, but it cannot fire for the
            // CallAll invocation this static ctor is running inside of — so the
            // first load calls Run directly, sharing this report and summary.
            UWU_Startup.Run(report);

            report.Time("reflection checks", () =>
            {
                WeaponModificationUtility.VerifyReflection();
                EquippableAbilityUtility.VerifyReflection();
                TraitEffectLinesIntegration.VerifyReflection();
            });

            // Force the optional-mod integrations to resolve now, so any API drift is
            // reported during startup rather than lazily on first use (availability
            // depends only on what's loaded, no game state). Each one's static ctor
            // self-reports. VEFRecipeInheritanceIntegration needs no probe — it's
            // already touched at load by WorkbenchUtility.Initialize.
            report.Time("integration probes", () =>
            {
                _ = VEFWeaponTraitGraphicsIntegration.Available;
                _ = AlphaArmouryIntegration.Available;
            });

            report.LogSummary();
        }
    }
}
