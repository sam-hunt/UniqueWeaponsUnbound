using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace UniqueWeaponsUnbound.Patches
{
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetGizmos))]
    public static class ThingWithComps_GetGizmos_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(
            IEnumerable<Gizmo> __result, ThingWithComps __instance)
        {
            foreach (Gizmo g in __result)
                yield return g;

            // Layer 1: Hidden — skip non-weapons and non-customizable weapons
            if (!__instance.def.IsWeapon || !__instance.Spawned)
                yield break;

            AcceptanceReport customizable = CustomizationRules.IsCustomizable(__instance);
            if (!customizable.Accepted && customizable.Reason.NullOrEmpty())
                yield break;

            WeaponRegistry.ResolveWeaponDefs(__instance,
                out ThingDef baseDef, out ThingDef uniqueDef);
            TechLevel techLevel = CustomizationRules.GetWeaponTechLevel(__instance);

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
                var workbenchCheck = WorkbenchUtility.FindBestWorkbench(
                    __instance.Map, baseDef, uniqueDef, techLevel, __instance.Position);
                if (!workbenchCheck.Found)
                {
                    gizmo.Disabled = true;
                    gizmo.disabledReason = workbenchCheck.BestRejection.Reason;
                }
            }

            // Capture locals for the delegate closures
            Thing weapon = __instance;
            ThingDef capturedBaseDef = baseDef;
            ThingDef capturedUniqueDef = uniqueDef;
            TechLevel capturedTechLevel = techLevel;

            gizmo.action = delegate
            {
                TargetingParameters parms = TargetingParameters.ForColonist();

                // Layer 3: pawn-specific validation on the targeter
                parms.validator = delegate(TargetInfo targetInfo)
                {
                    if (!(targetInfo.Thing is Pawn p))
                        return false;
                    return WorkbenchUtility.FindBestWorkbench(
                        p, capturedBaseDef, capturedUniqueDef,
                        capturedTechLevel, weapon.Position).Found;
                };

                Find.Targeter.BeginTargeting(parms,
                    delegate(LocalTargetInfo target)
                    {
                        // Layer 4: create job
                        Pawn pawn = target.Pawn;
                        if (pawn == null)
                            return;

                        var result = WorkbenchUtility.FindBestWorkbench(
                            pawn, capturedBaseDef, capturedUniqueDef,
                            capturedTechLevel, weapon.Position);
                        if (!result.Found)
                        {
                            Messages.Message(
                                "UWU_CustomizeWeapon".Translate(weapon.LabelShortCap)
                                    + " (" + result.BestRejection.Reason + ")",
                                weapon, MessageTypeDefOf.RejectInput, false);
                            return;
                        }

                        Job job = JobMaker.MakeJob(UWU_JobDefOf.UWU_CustomizeWeapon);
                        job.targetB = weapon;
                        job.targetC = result.Workbench;
                        job.count = 1;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    });
            };

            yield return gizmo;
        }
    }
}
