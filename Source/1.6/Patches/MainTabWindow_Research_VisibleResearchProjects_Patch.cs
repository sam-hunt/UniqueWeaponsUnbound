using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_Research),
        nameof(MainTabWindow_Research.VisibleResearchProjects), MethodType.Getter)]
    public static class MainTabWindow_Research_VisibleResearchProjects_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(List<ResearchProjectDef> __result)
        {
            if (UWU_Mod.Settings.requireCustomizationResearch)
                return;

            __result.RemoveAll(def =>
                def == UWU_ResearchDefOf.UniqueSmithing
                || def == UWU_ResearchDefOf.UniqueMachining
                || def == UWU_ResearchDefOf.UniqueFabrication);
        }
    }
}
