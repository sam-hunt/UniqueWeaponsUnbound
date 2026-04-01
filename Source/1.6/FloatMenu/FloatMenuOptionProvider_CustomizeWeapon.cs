using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace UniqueWeaponsUnbound
{
    public class FloatMenuOptionProvider_CustomizeWeapon : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(
            Thing clickedThing, FloatMenuContext context)
        {
            if (!(clickedThing is Building_WorkTable workbench))
                yield break;

            if (!WorkbenchUtility.IsCustomizationWorkbench(workbench))
                yield break;

            Pawn pawn = context.FirstSelectedPawn;
            if (pawn == null)
                yield break;

            // Entry point 1: equipped weapon
            Thing equipped = pawn.equipment?.Primary;
            if (equipped != null)
            {
                FloatMenuOption option = GetOptionForWeapon(
                    pawn, equipped, workbench);
                if (option != null)
                    yield return option;
            }

            // Entry point 2: inventory weapons
            if (pawn.inventory?.innerContainer != null)
            {
                foreach (Thing item in pawn.inventory.innerContainer)
                {
                    if (!item.def.IsWeapon)
                        continue;

                    FloatMenuOption option = GetOptionForWeapon(
                        pawn, item, workbench);
                    if (option != null)
                        yield return option;
                }
            }
        }

        private static FloatMenuOption GetOptionForWeapon(
            Pawn pawn, Thing weapon, Building_WorkTable workbench)
        {
            // Variant exists + UniqueSmithing gate
            AcceptanceReport customizable = CustomizationRules.IsCustomizable(weapon);
            if (!customizable.Accepted && customizable.Reason.NullOrEmpty())
                return null;

            // Resolve base/unique defs for workbench and craftability checks
            WeaponRegistry.ResolveWeaponDefs(weapon,
                out ThingDef baseDef, out ThingDef uniqueDef);

            // Workbench: recipe match, then tech-level tier fallback
            TechLevel weaponTechLevel = CustomizationRules.GetWeaponTechLevel(weapon);
            AcceptanceReport workbenchReport = WorkbenchUtility.CanCustomizeAtWorkbench(
                baseDef, uniqueDef, weaponTechLevel, workbench);
            if (!workbenchReport.Accepted)
                return DisabledOrHidden(weapon, workbenchReport);

            // Workbench operational (power/fuel)
            AcceptanceReport operational = WorkbenchUtility.GetWorkbenchOperationalReport(workbench);
            if (!operational.Accepted)
                return DisabledOrHidden(weapon, operational);

            // Recipe research (craftability)
            AcceptanceReport craftable = CustomizationRules.GetCraftabilityReport(baseDef, uniqueDef);
            if (!craftable.Accepted)
                return DisabledOrHidden(weapon, craftable);

            // Customization research
            if (!customizable.Accepted)
                return DisabledOrHidden(weapon, customizable);

            string label = "UWU_CustomizeWeapon".Translate(weapon.LabelShortCap);

            if (!pawn.CanReach(workbench, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return new FloatMenuOption(
                    label + " (" + "NoPath".Translate() + ")",
                    null);
            }

            if (workbench.IsForbidden(pawn))
            {
                return new FloatMenuOption(
                    label + " (" + "ForbiddenLower".Translate() + ")",
                    null);
            }

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    label,
                    delegate
                    {
                        Job job = JobMaker.MakeJob(
                            UWU_JobDefOf.UWU_CustomizeWeapon);
                        job.targetB = weapon;
                        job.targetC = workbench;
                        job.count = 1;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }),
                pawn, workbench);
        }

        private static FloatMenuOption DisabledOrHidden(Thing weapon, AcceptanceReport report)
        {
            if (report.Reason.NullOrEmpty())
                return null;

            string label = "UWU_CustomizeWeapon".Translate(weapon.LabelShortCap)
                + " (" + report.Reason + ")";
            return new FloatMenuOption(label, null);
        }
    }
}
