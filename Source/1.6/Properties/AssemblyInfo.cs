using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("UniqueWeaponsUnbound")]
[assembly: AssemblyDescription("Customize unique weapons in RimWorld")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("UniqueWeaponsUnbound")]
[assembly: AssemblyCopyright("Copyright © 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

// Test access to HaulPlanning.Internal helpers and planner diagnostics
// (op-count instrument, guard-log kill-switch).
[assembly: InternalsVisibleTo("UniqueWeaponsUnbound.Tests")]

[assembly: Guid("c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f")]

[assembly: AssemblyVersion("1.6.0.0")]
[assembly: AssemblyFileVersion("1.6.0.0")]
// Mirrors About.xml <modVersion> verbatim, including any SemVer prerelease suffix
// (1.7.0-rc.1); the two numeric attributes above can't hold one and stay X.Y.Z.0.
[assembly: AssemblyInformationalVersion("1.6.0")]
