using HarmonyLib;
using Verse;

namespace UniqueWeaponsUnbound.Patches
{
    // Re-runs UWU_Startup on every play-data RELOAD, where
    // [StaticConstructorOnStartup] alone would run it only once per process.
    // This file is the full rationale for that divergence; UWU_Startup and
    // CLAUDE.md carry only pointers here.
    //
    // Why the attribute's contract is too weak for us: UWU_Startup's work is
    // all state derived from the live DefDatabase — the base↔unique weapon
    // pair registry, the workbench tier sets and their baked display labels,
    // and the trait-cost rule list whose workers resolve material ThingDefs at
    // startup. An in-process play-data reload (LanguageDatabase.SelectLanguage
    // runs ClearAllPlayData + LoadAllPlayData; the mid-session language switch
    // is the one player-facing trigger) replaces every def instance, but a
    // type initializer can never run twice (StaticConstructorOnStartupUtility.
    // CallAll goes through RuntimeHelpers.RunClassConstructor, which no-ops on
    // an initialized type). With attribute-only startup the fresh defs are
    // never registered: base↔unique conversion stops resolving, no live
    // workbench passes the weaponWorkbenchDefs gate (customization becomes
    // impossible), tier-requirement messages keep the previous language's
    // bench labels, and cost rules emit dead material defs that ingredient
    // counting can never match. Vanilla itself never needs a re-run hook: its
    // own cross-load state is either [DefOf] fields (rebound every load) or
    // load-agnostic static texture/material caches — mods that cache or mutate
    // defs own the re-application problem, and vanilla ships no standing
    // "play data loaded" callback.
    //
    // Why THIS hook (decompile-verified, RimWorld 1.6): PlayDataLoader.
    // DoPlayLoad queues its finishing work as ExecuteWhenFinished delegates
    // that run on the main thread after the method returns, in order:
    // InjectIntoData_AfterImpliedDefs (full DefInjected application) +
    // GenLabel.ClearCache → StaticConstructorOnStartupUtility.CallAll → atlas
    // baking. A postfix on CallAll therefore fires at exactly the moment
    // static ctors run — after defs, cross-refs, DefOf rebinding and full
    // language injection — and it stays correct for any future reload trigger
    // because it hooks the load pipeline, not the language switch.
    //
    // The trap this shape avoids: a postfix on PlayDataLoader.DoPlayLoad
    // itself LOOKS equivalent but fires before those queued delegates — i.e.
    // before DefInjected is applied — and would rebuild every cache from
    // untranslated labels, subtly wrong in exactly the scenario this fixes.
    //
    // First-load coverage: this repo applies its patches from ModInitializer's
    // [StaticConstructorOnStartup] ctor (load-bearing — see the patch-timing
    // hazard in CLAUDE.md), which executes INSIDE the first CallAll invocation;
    // detouring a method never affects the activation already on the stack, so
    // this postfix cannot fire for that first call. ModInitializer therefore
    // calls UWU_Startup.Run directly for the first load, and this postfix owns
    // every load after it.
    //
    // Deliberately out of scope: dev-mode PlayDataLoader.HotReloadDefs never
    // calls CallAll (nor RebindAllDefOfs — vanilla does not uphold even its
    // own DefOf contract there), so def hot reload stays best-effort for us
    // exactly as it is for vanilla.
    //
    // Everything UWU_Startup.Run calls must stay idempotent: reloads make this
    // fire once per load, arbitrarily many times per process.
    [HarmonyPatch(typeof(StaticConstructorOnStartupUtility), nameof(StaticConstructorOnStartupUtility.CallAll))]
    public static class StaticConstructorOnStartupUtility_CallAll_Patch
    {
        public static void Postfix()
        {
            UWU_Startup.RunOnReload();
        }
    }
}
