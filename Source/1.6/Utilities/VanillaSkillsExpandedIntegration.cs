using System;
using System.Collections;
using System.Reflection;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Optional integration with Vanilla Skills Expanded (packageId
    // vanillaexpanded.skills). VSE attaches an ExpertiseTracker to every
    // Pawn_SkillTracker through a static side table, reached via the extension
    // method VSE.ExpertiseTrackers.Expertise(Pawn); the tracker exposes
    // AllExpertise (List<ExpertiseRecord>), and each record's def field is an
    // ExpertiseDef (a plain Verse.Def). VSE ships no "has this expertise"
    // helper, so we walk that list and compare defNames.
    //
    // All access goes through reflection so this mod compiles and runs without
    // VSE installed. Shape drift (renamed type/member, unexpected member type)
    // nulls the surface, Available turns false, and the skill-check setting
    // falls back to a flat crafting minimum (see SkillCheckRules). The static
    // ctor logs a warning when VSE is active but the surface didn't resolve;
    // ModInitializer forces that resolution at startup by reading Available.
    // Type resolution depends only on loaded assemblies, never on defs, so it is
    // safe to do once per process; the expertise def itself is looked up live
    // (see ExpertiseLabel) because an in-process play-data reload replaces it.
    internal static class VanillaSkillsExpandedIntegration
    {
        public const string PackageId = "vanillaexpanded.skills";
        public const string WeaponsmithDefName = "Weaponsmith";

        private const string TrackersTypeName = "VSE.ExpertiseTrackers";
        private const string TrackerTypeName = "VSE.ExpertiseTracker";
        private const string RecordTypeName = "VSE.ExpertiseRecord";
        private const string ExpertiseDefTypeName = "VSE.Expertise.ExpertiseDef";

        private static readonly Type ExpertiseDefType;
        private static readonly MethodInfo ExpertiseOfPawn;
        private static readonly PropertyInfo AllExpertiseProperty;
        private static readonly FieldInfo RecordDefField;

        public static bool Available => RecordDefField != null;

        private static bool runtimeFailureLogged;

        static VanillaSkillsExpandedIntegration()
        {
            bool active;
            try
            {
                active = ModsConfig.IsActive(PackageId);
                if (!active)
                    return;

                Type trackersType = GenTypes.GetTypeInAnyAssembly(TrackersTypeName);
                Type trackerType = GenTypes.GetTypeInAnyAssembly(TrackerTypeName);
                Type recordType = GenTypes.GetTypeInAnyAssembly(RecordTypeName);
                ExpertiseDefType = GenTypes.GetTypeInAnyAssembly(ExpertiseDefTypeName);
                if (trackersType == null || trackerType == null || recordType == null
                    || ExpertiseDefType == null || !typeof(Def).IsAssignableFrom(ExpertiseDefType))
                {
                    ExpertiseDefType = null;
                }
                else
                {
                    ExpertiseOfPawn = trackersType.GetMethod("Expertise",
                        BindingFlags.Public | BindingFlags.Static, null,
                        new[] { typeof(Pawn) }, null);
                    AllExpertiseProperty = trackerType.GetProperty("AllExpertise",
                        BindingFlags.Public | BindingFlags.Instance);
                    FieldInfo defField = recordType.GetField("def",
                        BindingFlags.Public | BindingFlags.Instance);

                    bool shapeOk = ExpertiseOfPawn != null
                        && trackerType.IsAssignableFrom(ExpertiseOfPawn.ReturnType)
                        && AllExpertiseProperty != null
                        && typeof(IEnumerable).IsAssignableFrom(AllExpertiseProperty.PropertyType)
                        && defField != null
                        && typeof(Def).IsAssignableFrom(defField.FieldType);
                    if (shapeOk)
                        RecordDefField = defField;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Unique Weapons Unbound] Vanilla Skills Expanded reflection failed "
                    + "(expertise skill checks fall back to a flat crafting minimum): " + ex);
                return;
            }

            if (!Available)
            {
                Log.Warning("[Unique Weapons Unbound] Vanilla Skills Expanded active but its "
                    + "expertise API could not be resolved (" + TrackersTypeName + ".Expertise(Pawn) / "
                    + TrackerTypeName + ".AllExpertise / " + RecordTypeName + ".def). "
                    + "The weaponsmithing-expertise skill check falls back to a flat crafting minimum; "
                    + "this only affects you if that setting is selected.");
            }
        }

        // Whether the pawn currently holds the named expertise. False for pawns
        // without a skill tracker (mechs, animals) and whenever the integration
        // is unavailable.
        public static bool HasExpertise(Pawn pawn, string expertiseDefName)
        {
            if (!Available || pawn?.skills == null)
                return false;

            try
            {
                object tracker = ExpertiseOfPawn.Invoke(null, new object[] { pawn });
                if (tracker == null)
                    return false;
                if (!(AllExpertiseProperty.GetValue(tracker) is IEnumerable records))
                    return false;
                foreach (object record in records)
                {
                    if (record != null
                        && RecordDefField.GetValue(record) is Def def
                        && def.defName == expertiseDefName)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                // Defensive: the resolved surface shouldn't raise on a well-typed
                // pawn. If it does, log once and treat the pawn as lacking the
                // expertise so a per-frame caller can't flood the log.
                if (!runtimeFailureLogged)
                {
                    runtimeFailureLogged = true;
                    Log.Error("[Unique Weapons Unbound] Vanilla Skills Expanded expertise read failed: "
                        + ex);
                }
                return false;
            }
        }

        // The live ExpertiseDef's label, for player-facing text, or the given
        // fallback when the def can't be found. Resolved per call (not cached)
        // so an in-process play-data reload — which replaces every def and its
        // injected label — is always reflected.
        public static string ExpertiseLabel(string expertiseDefName, string fallback)
        {
            if (!Available)
                return fallback;
            try
            {
                Def def = GenDefDatabase.GetDef(ExpertiseDefType, expertiseDefName, errorOnFail: false);
                return def?.label ?? fallback;
            }
            catch (Exception)
            {
                return fallback;
            }
        }
    }
}
