namespace UniqueWeaponsUnbound
{
    // Startup work that must run against the CURRENT DefDatabase: the weapon
    // pair registry, the workbench tier sets (and their display labels), and
    // the trait-cost rule pipeline. Runs once per play-data LOAD, not once per
    // process: an in-process reload (a mid-session language change) replaces
    // every def instance, and a [StaticConstructorOnStartup] type initializer
    // never re-runs, which would leave these caches pointing at the previous
    // database's dead defs — customization would stop finding unique variants
    // and workbenches, and cost rules would emit materials no live thing can
    // match.
    //
    // First load: called directly from ModInitializer's static ctor (which
    // runs inside the first CallAll, too late for its own postfix). Every
    // reload: invoked by Patches/StaticConstructorOnStartupUtility_CallAll_
    // Patch.cs at exactly the moment static ctors run — after defs, DefOf
    // rebinding and full language injection; that file carries the verified
    // load ordering, the DoPlayLoad trap, and the hot-reload caveat.
    //
    // Everything called here must stay idempotent — it fires once per load,
    // arbitrarily many times per process.
    public static class UWU_Startup
    {
        // Shared-report entry: the first load passes ModInitializer's report so
        // the whole init block logs a single summary line.
        public static void Run(InitDiagnostics report)
        {
            report.Time("WeaponRegistry", () => WeaponRegistry.Initialize(report));
            report.Time("WorkbenchUtility", () => WorkbenchUtility.Initialize(report));
            report.Time("TraitCostUtility", () => TraitCostUtility.Initialize(report));
        }

        // Reload entry (the CallAll postfix): builds its own report so each
        // reload logs its own summary with the fresh per-mod def counts.
        public static void RunOnReload()
        {
            var report = new InitDiagnostics();
            Run(report);
            report.LogSummary();
        }
    }
}
