using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace UniqueWeaponsUnbound
{
    // The ground-weapon customize gizmo, delivered through vanilla's own comp
    // hook (ThingWithComps.GetGizmos loops comps[i].CompGetGizmosExtra) rather
    // than a Harmony patch. The comp is not declared in XML: which defs have a
    // customization path is computed by WeaponRegistry (a unique def, or a
    // base weapon some unique def resolves to), so InjectIntoWeaponDefs
    // appends the props to exactly those defs once the registry is built.
    // Every other Thing in the game never sees this code.
    //
    // The comp scribes nothing, and vanilla rebuilds a thing's comp list from
    // def.comps on every load (ThingWithComps.ExposeData -> InitializeComps),
    // so it leaves no trace in a save: adding or removing the mod is safe.
    //
    // DefModExtension was the other candidate for a def-level marker, but it
    // has no runtime hooks (only ConfigErrors/ResolveReferences), so reading
    // one would still need a patch on a GetGizmos method. Verified on 1.6 and
    // 1.7.
    public class CompCustomizeGizmo : ThingComp
    {
        // Appends this comp's props to every def with a customization path.
        // Runs once per play-data load from UWU_Startup.Run, after
        // WeaponRegistry.Initialize and before any Thing exists (a thing made
        // before injection keeps its comp list until it is next loaded). Like
        // the other Initialize methods, a non-null report absorbs a fatal
        // exception so the rest of the mod still comes up.
        public static void InjectIntoWeaponDefs(InitDiagnostics report = null)
        {
            try
            {
                foreach (ThingDef def in WeaponRegistry.AllCustomizableDefs)
                    EnsureOn(def);
            }
            catch (Exception ex)
            {
                if (report == null) throw;
                report.RecordFailure(nameof(CompCustomizeGizmo), ex);
            }
        }

        // Adds the props to one weapon def unless it already carries them, and
        // reports whether it added. A reload hands us fresh defs, so the
        // HasComp check is what keeps a repeat call, a def listed twice, or
        // two defs sharing one comps list from ending up with a second copy.
        // ThingDef.HasComp walks comps without a null check, hence the guard
        // ahead of it.
        internal static bool EnsureOn(ThingDef def)
        {
            if (!def.IsWeapon)
                return false;
            if (def.comps == null)
                def.comps = new List<CompProperties>();
            else if (def.HasComp(typeof(CompCustomizeGizmo)))
                return false;
            def.comps.Add(new CompProperties(typeof(CompCustomizeGizmo)));
            return true;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Analysis is wrapped in a try/catch helper because an uncaught
            // throw here would propagate into vanilla's gizmo iterator and
            // break selection rendering on the host Thing, including the
            // gizmos contributed by its other comps.
            Gizmo customize = TryBuildCustomizeGizmo(parent);
            if (customize != null)
                yield return customize;
        }

        private static Gizmo TryBuildCustomizeGizmo(Thing parent)
        {
            try
            {
                return BuildCustomizeGizmo(parent);
            }
            catch (Exception ex)
            {
                // ErrorOnce keyed by defName so a recurring per-frame failure
                // on a selected weapon doesn't flood the log.
                string defName = parent?.def?.defName ?? "(null)";
                Log.ErrorOnce(
                    "[Unique Weapons Unbound] Customize gizmo failed for "
                        + defName + ": " + ex,
                    ("UWU_GizmoFail_" + defName).GetHashCode());
                return null;
            }
        }

        private static Gizmo BuildCustomizeGizmo(Thing parent)
        {
            // Layer 1: Hidden. Only defs with a customization path carry this
            // comp, so the dev-mode "no unique variant" reason never reaches
            // the gizmo (the right-click options still show it); the other
            // HiddenUnlessDev reasons surface below as a disabled gizmo.
            if (!parent.Spawned)
                return null;

            // Multi-select: identical customize gizmos would merge into one
            // unlabelled button that targets an arbitrary weapon from the
            // selection, so hide the gizmo entirely instead. This also
            // short-circuits the per-weapon rule and workbench evaluation for
            // large drag-selections (battlefield loot).
            if (MultipleWeaponsSelected())
                return null;

            AcceptanceReport customizable = CustomizationRules.IsCustomizable(parent);
            if (!customizable.Accepted && customizable.Reason.NullOrEmpty())
                return null;

            WeaponRegistry.ResolveWeaponDefs(parent,
                out ThingDef baseDef, out ThingDef uniqueDef);
            TechLevel techLevel = CustomizationRules.GetWeaponTechLevel(parent);

            Command_Action gizmo = new Command_Action();
            gizmo.defaultLabel = "UWU_CustomizeGizmoLabel".Translate();
            gizmo.defaultDesc = "UWU_CustomizeGizmoDesc".Translate();
            gizmo.icon = UWU_Textures.Customize;

            // Layer 2: Disabled state (pawn-independent checks)
            AcceptanceReport craftable = CustomizationRules.GetCraftabilityReport(baseDef, uniqueDef);
            if (!craftable.Accepted && !craftable.Reason.NullOrEmpty())
            {
                gizmo.Disabled = true;
                gizmo.disabledReason = craftable.Reason;
            }
            else if (!customizable.Accepted)
            {
                gizmo.Disabled = true;
                gizmo.disabledReason = customizable.Reason;
            }
            else
            {
                var workbenchCheck = GetCachedGizmoSearch(parent, baseDef, uniqueDef, techLevel);
                if (!workbenchCheck.Found)
                {
                    gizmo.Disabled = true;
                    gizmo.disabledReason = workbenchCheck.BestRejection.Reason;
                }
            }

            gizmo.action = delegate
            {
                // Click-time hardening: a throw here would bubble through
                // vanilla's UI handler. ErrorOnce keyed by defName so a
                // pathological weapon doesn't flood the log on repeat clicks.
                try
                {
                    BeginCustomizeTargeting(parent, baseDef, uniqueDef, techLevel);
                }
                catch (Exception ex)
                {
                    string defName = parent?.def?.defName ?? "(null)";
                    Log.ErrorOnce(
                        "[Unique Weapons Unbound] Customize action failed for "
                            + defName + ": " + ex,
                        ("UWU_GizmoAction_" + defName).GetHashCode());
                }
            };

            return gizmo;
        }

        // Whether more than one weapon is currently selected. Early-exits on
        // the second weapon, so large mixed selections stay cheap.
        private static bool MultipleWeaponsSelected()
        {
            List<object> selected = Find.Selector.SelectedObjectsListForReading;
            int weapons = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] is Thing thing && thing.def.IsWeapon && ++weapons > 1)
                    return true;
            }
            return false;
        }

        // The workbench search walks every colonist building and runs
        // reachability/reservation checks per candidate, and the gizmo is
        // rebuilt once per rendered frame while a weapon is selected
        // (GizmoGridDrawer caches per Time.frameCount). A short TTL keeps the
        // search off the per-frame path; frames rather than ticks so the
        // enabled/disabled state stays responsive while paused (e.g. the
        // player forbidding a bench). Keyed by thingIDNumber; stale entries
        // from a previous save fail the frame check and recompute. The
        // pawn-independent skill check (colony-wide subjects) shares the
        // cache: it walks the colonist list, and under the expertise kind
        // that's a reflection call per colonist.
        private const int SearchCacheTtlFrames = 30;

        private struct CachedSearch
        {
            public int Frame;
            public WorkbenchUtility.WorkbenchSearchResult Result;
        }

        private static readonly Dictionary<int, CachedSearch> gizmoSearchCache =
            new Dictionary<int, CachedSearch>();
        private static readonly Dictionary<int, CachedSearch> targeterSearchCache =
            new Dictionary<int, CachedSearch>();

        private static WorkbenchUtility.WorkbenchSearchResult GetCachedGizmoSearch(
            Thing weapon, ThingDef baseDef, ThingDef uniqueDef, TechLevel techLevel)
        {
            int frame = Time.frameCount;
            if (gizmoSearchCache.TryGetValue(weapon.thingIDNumber, out CachedSearch cached)
                && frame - cached.Frame < SearchCacheTtlFrames)
            {
                return cached.Result;
            }

            // Skill prerequisite first (cheaper, and the more fundamental
            // reason); a null pawn defers the CustomizingPawn subject to the
            // targeter, so this only ever rejects for the colony-wide subjects.
            AcceptanceReport skill = SkillCheckRules.GetReport(null, weapon, baseDef, uniqueDef);
            var result = skill.Accepted
                ? WorkbenchUtility.FindBestWorkbench(
                    weapon.Map, baseDef, uniqueDef, techLevel, weapon.Position)
                : new WorkbenchUtility.WorkbenchSearchResult { BestRejection = skill };
            if (gizmoSearchCache.Count > 128)
                gizmoSearchCache.Clear();
            gizmoSearchCache[weapon.thingIDNumber] = new CachedSearch
            {
                Frame = frame,
                Result = result,
            };
            return result;
        }

        // Same TTL treatment for the targeter validator, which runs the
        // pawn-specific search for hovered candidates every frame while
        // targeting. Keyed by pawn; cleared when targeting begins so a stale
        // verdict never carries over into a new targeting session.
        private static WorkbenchUtility.WorkbenchSearchResult GetCachedTargeterSearch(
            Pawn pawn, Thing weapon, ThingDef baseDef, ThingDef uniqueDef, TechLevel techLevel)
        {
            int frame = Time.frameCount;
            if (targeterSearchCache.TryGetValue(pawn.thingIDNumber, out CachedSearch cached)
                && frame - cached.Frame < SearchCacheTtlFrames)
            {
                return cached.Result;
            }

            var result = WorkbenchUtility.FindBestWorkbench(
                pawn, baseDef, uniqueDef, techLevel, weapon.Position);
            targeterSearchCache[pawn.thingIDNumber] = new CachedSearch
            {
                Frame = frame,
                Result = result,
            };
            return result;
        }

        private static void BeginCustomizeTargeting(
            Thing weapon, ThingDef baseDef, ThingDef uniqueDef, TechLevel techLevel)
        {
            targeterSearchCache.Clear();
            TargetingParameters parms = TargetingParameters.ForColonist();

            // Layer 3: pawn-specific validation on the targeter. The skill
            // check deliberately does NOT exclude pawns here: an under-skilled
            // colonist stays targetable, the mouse-attached tip below shows
            // their level against the requirement, and picking them anyway
            // surfaces the rejection as a message (Layer 4).
            parms.validator = delegate(TargetInfo targetInfo)
            {
                if (!(targetInfo.Thing is Pawn p))
                    return false;
                return GetCachedTargeterSearch(p, weapon, baseDef, uniqueDef, techLevel).Found;
            };

            // Per-pawn skill tip on under-skilled colonists, only under the
            // CustomizingPawn subject (the colony-wide subjects were already
            // answered by the gizmo state).
            Action<LocalTargetInfo> onGui = null;
            if (UWU_Mod.Settings.skillCheckSubject == SkillCheckSubject.CustomizingPawn)
            {
                SkillCheckRules.Requirement requirement =
                    SkillCheckRules.GetRequirement(baseDef, uniqueDef, techLevel);
                if (!requirement.IsEmpty)
                {
                    onGui = delegate(LocalTargetInfo hovered)
                    {
                        if (!(hovered.Thing is Pawn p) || !p.IsColonist)
                            return;
                        string tip = SkillCheckRules.GetTargeterTip(p, requirement);
                        if (!tip.NullOrEmpty())
                            Widgets.MouseAttachedLabel(tip, 0f, 0f, ColorLibrary.RedReadable);
                    };
                }
            }

            Find.Targeter.BeginTargeting(parms,
                delegate(LocalTargetInfo target)
                {
                    // Layer 4: create job
                    Pawn pawn = target.Pawn;
                    if (pawn == null)
                        return;

                    AcceptanceReport skill = SkillCheckRules.GetReport(pawn, weapon, baseDef, uniqueDef);
                    if (!skill.Accepted)
                    {
                        Messages.Message(
                            "UWU_CustomizeWeapon".Translate(weapon.LabelShort)
                                + " (" + skill.Reason + ")",
                            weapon, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    var result = WorkbenchUtility.FindBestWorkbench(
                        pawn, baseDef, uniqueDef, techLevel, weapon.Position);
                    if (!result.Found)
                    {
                        Messages.Message(
                            "UWU_CustomizeWeapon".Translate(weapon.LabelShort)
                                + " (" + result.BestRejection.Reason + ")",
                            weapon, MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    Job job = JobMaker.MakeJob(UWU_JobDefOf.UWU_CustomizeWeapon);
                    job.targetB = weapon;
                    job.targetC = result.Workbench;
                    job.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                },
                onGui);
        }
    }
}
