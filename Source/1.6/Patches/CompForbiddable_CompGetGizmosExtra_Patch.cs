using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace UniqueWeaponsUnbound.Patches
{
    // Comp-scoped postfix instead of ThingWithComps.GetGizmos so the patch
    // body only runs for Things whose def carries CompForbiddable — items,
    // some buildings, doors. Pawns, walls, plants, and terrain features
    // never enter this code. Every weapon that can be selected on the
    // ground carries CompForbiddable, so the gizmo's reachability set is
    // unchanged.
    //
    // CompUniqueWeapon would be the narrowest possible target (unique
    // weapons only) but vanilla doesn't override CompGetGizmosExtra on it,
    // so there's no method body to postfix. It would also miss base
    // weapons with a registered unique variant, which still need the
    // gizmo to start a base→unique conversion.
    [HarmonyPatch(typeof(CompForbiddable), nameof(CompForbiddable.CompGetGizmosExtra))]
    public static class CompForbiddable_CompGetGizmosExtra_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(
            IEnumerable<Gizmo> __result, CompForbiddable __instance)
        {
            foreach (Gizmo g in __result)
                yield return g;

            // Analysis is wrapped in a try/catch helper because an uncaught
            // throw here would propagate into vanilla's gizmo iterator and
            // break selection rendering on the host Thing — including
            // unrelated gizmos contributed by other comps.
            Gizmo customize = TryBuildCustomizeGizmo(__instance.parent);
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
            // Layer 1: Hidden — skip non-weapons and non-customizable weapons.
            // Registry membership isn't checked here so CustomizationRules.IsCustomizable
            // can still surface its HiddenUnlessDev rejection reasons as a
            // visible-but-disabled gizmo in dev mode.
            if (!parent.def.IsWeapon || !parent.Spawned)
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

            // Per-pawn skill tip, only under the CustomizingPawn subject (the
            // colony-wide subjects were already answered by the gizmo state).
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
                        string tip = SkillCheckRules.GetTargeterTip(p, requirement, out bool failing);
                        if (!tip.NullOrEmpty())
                        {
                            Widgets.MouseAttachedLabel(tip, 0f, 0f,
                                failing ? ColorLibrary.RedReadable : (Color?)null);
                        }
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
