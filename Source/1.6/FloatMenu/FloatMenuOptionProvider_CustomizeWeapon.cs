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

        protected override FloatMenuOption GetSingleOptionFor(
            Thing clickedThing, FloatMenuContext context)
        {
            if (!(clickedThing is Building_WorkTable workbench))
                return null;

            if (!WeaponCustomizationUtility.IsCustomizationWorkbench(workbench))
                return null;

            Pawn pawn = context.FirstSelectedPawn;
            if (pawn == null)
                return null;

            Thing weapon = pawn.equipment?.Primary;
            if (weapon == null)
                return null;

            // Variant exists + UniqueSmithing gate
            AcceptanceReport customizable = WeaponCustomizationUtility.IsCustomizable(weapon);
            if (!customizable.Accepted && customizable.Reason.NullOrEmpty())
                return null;

            // Resolve base/unique defs for workbench and craftability checks
            ThingDef baseDef, uniqueDef;
            if (WeaponCustomizationUtility.IsUniqueWeapon(weapon.def))
            {
                uniqueDef = weapon.def;
                baseDef = WeaponCustomizationUtility.GetBaseVariant(weapon.def);
            }
            else
            {
                baseDef = weapon.def;
                uniqueDef = WeaponCustomizationUtility.GetUniqueVariant(weapon.def);
            }

            // Workbench: recipe match, then tech-level tier fallback
            TechLevel weaponTechLevel = WeaponCustomizationUtility.GetWeaponTechLevel(weapon);
            AcceptanceReport workbenchReport = WeaponCustomizationUtility.CanCustomizeAtWorkbench(
                baseDef, uniqueDef, weaponTechLevel, workbench);
            if (!workbenchReport.Accepted)
                return DisabledOrHidden(weapon, workbenchReport);

            // Workbench operational (power/fuel)
            AcceptanceReport operational = WeaponCustomizationUtility.GetWorkbenchOperationalReport(workbench);
            if (!operational.Accepted)
                return DisabledOrHidden(weapon, operational);

            // Recipe research (craftability)
            AcceptanceReport craftable = WeaponCustomizationUtility.GetCraftabilityReport(baseDef);
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
                        job.targetC = workbench;
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
