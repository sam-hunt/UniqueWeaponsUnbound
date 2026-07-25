using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Optional integration with mods that publish a melee trait's effects as structured display
    // lines. Unique Melee Weapons is the one that does today.
    //
    // WHY THIS EXISTS. Our trait tooltip's "Effects" block is built from vanilla WeaponTraitDef
    // fields, and for a MELEE trait almost all of them are dead: damageDefOverride, extraDamages and
    // equippedStatOffsets are read only by the projectile and bladelink code paths, so a melee trait
    // that stuns, converts a wound type or grants an ability has nothing for us to print and shows a
    // market value alone. A publisher that implements those effects itself knows what they do; this
    // lets it tell us, so its traits get the same neat bulleted block every other trait gets.
    //
    // THE CONTRACT (duck-typed, no assembly reference either way — mirrors the stuff_adjective
    // grammar symbol Unique Melee Weapons already publishes into our name generation). A publisher
    // attaches a DefModExtension to the WeaponTraitDef where:
    //   - the type's SIMPLE name is "TraitEffectLinesExtension" (namespace is the publisher's own), and
    //   - it exposes a public instance field "lines" of type List<string>.
    // Lines arrive unstyled and already localized — no bullet, no indent, no trailing punctuation —
    // so we apply our own indent and they sit alongside our stat rows looking native.
    //
    // Resolved once at startup, like every other reflection surface in this mod: VerifyReflection
    // scans loaded DefModExtension subclasses for the contract name and validates the field, so
    // drift surfaces during load rather than as a silently empty tooltip. Matching on the SIMPLE
    // name (rather than one publisher's full type name) is what keeps the contract open to any mod,
    // and it costs nothing extra — every mod assembly is loaded before startup runs, so the scan
    // sees them all. Draw time is then a dictionary lookup, no reflection resolution.
    //
    // Absence is the normal case and stays silent: only a type that answers to the contract name
    // while exposing no usable field warns.
    internal static class TraitEffectLinesIntegration
    {
        private const string ExtensionTypeName = "TraitEffectLinesExtension";
        private const string LinesFieldName = "lines";

        // Publisher extension type -> its validated "lines" field. Empty in the common case where
        // no publisher is installed.
        private static readonly Dictionary<Type, FieldInfo> LinesFields = new Dictionary<Type, FieldInfo>();

        // True when at least one publisher is installed and its contract resolved.
        public static bool Available => LinesFields.Count > 0;

        // Resolves every installed publisher and reports any that drifted. Called from
        // ModInitializer alongside the other VerifyReflection checks, so a contract break surfaces
        // during load rather than as a silently empty tooltip.
        //
        // An explicit call rather than a static ctor (the shape the VEF/Alpha Armoury integrations
        // use): resolution needs GenTypes, which walks LoadedModManager, and a throw from a type
        // initializer would poison every later call to this class rather than degrading to "no
        // publisher". It also keeps the type usable off a Unity runtime, which is what lets
        // TraitEffectLinesIntegrationTests drive ResolvePublishers directly.
        public static void VerifyReflection()
        {
            List<string> drifted;
            try
            {
                drifted = ResolvePublishers(typeof(DefModExtension).AllSubclassesNonAbstract());
            }
            catch (Exception ex)
            {
                Log.Warning("[Unique Weapons Unbound] trait-effect-lines reflection failed: " + ex);
                return;
            }

            // A publisher is "present" iff a type matched the contract name, so a plain install with
            // no publisher stays quiet and only a genuine shape mismatch reports.
            if (drifted.Count > 0)
            {
                Log.Warning("[Unique Weapons Unbound] " + string.Join(", ", drifted.ToArray())
                    + " matches the trait-effect-lines contract by name but exposes no public"
                    + " List<string> '" + LinesFieldName + "' field; those trait effects will be"
                    + " missing from customization tooltips. The publishing mod's API may have changed.");
            }
        }

        // Records every candidate that satisfies the contract and returns the full names of those
        // that match by name but not by shape. Split out from VerifyReflection so tests can drive it
        // with an explicit type list — the startup scan needs game state they don't have.
        internal static List<string> ResolvePublishers(IEnumerable<Type> candidates)
        {
            var drifted = new List<string>();
            foreach (Type type in candidates)
            {
                if (type.Name != ExtensionTypeName)
                    continue;

                FieldInfo field = type.GetField(LinesFieldName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && typeof(List<string>).IsAssignableFrom(field.FieldType))
                    LinesFields[type] = field;
                else
                    drifted.Add(type.FullName);
            }
            return drifted;
        }

        // Appends every published effect line for trait, indented to match the caller's other rows.
        // No-op when no publisher has annotated this trait, which is the common case.
        internal static void AppendEffectLines(WeaponTraitDef trait, List<string> effectLines, string indent)
        {
            List<DefModExtension> extensions = trait?.modExtensions;
            if (extensions == null || !Available)
                return;

            for (int i = 0; i < extensions.Count; i++)
            {
                DefModExtension extension = extensions[i];
                if (extension == null || !LinesFields.TryGetValue(extension.GetType(), out FieldInfo field))
                    continue;

                if (!(field.GetValue(extension) is List<string> lines))
                    continue;

                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                        effectLines.Add(indent + line);
                }
            }
        }
    }
}
