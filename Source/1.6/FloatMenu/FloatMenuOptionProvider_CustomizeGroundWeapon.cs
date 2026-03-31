using RimWorld;
using Verse;
using Verse.AI;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Entry point 3: right-click a weapon on the ground to customize it.
    /// Auto-selects the best workbench via <see cref="WorkbenchUtility.FindBestWorkbench"/>.
    /// </summary>
    public class FloatMenuOptionProvider_CustomizeGroundWeapon : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        protected override FloatMenuOption GetSingleOptionFor(
            Thing clickedThing, FloatMenuContext context)
        {
            if (clickedThing is Building)
                return null;
            if (!clickedThing.def.IsWeapon)
                return null;
            if (!clickedThing.Spawned)
                return null;

            Pawn pawn = context.FirstSelectedPawn;
            if (pawn == null)
                return null;

            Thing weapon = clickedThing;

            // Variant exists + UniqueSmithing gate
            AcceptanceReport customizable = CustomizationRules.IsCustomizable(weapon);
            if (!customizable.Accepted && customizable.Reason.NullOrEmpty())
                return null;

            // Resolve base/unique defs
            WeaponRegistry.ResolveWeaponDefs(weapon,
                out ThingDef baseDef, out ThingDef uniqueDef);

            TechLevel weaponTechLevel = CustomizationRules.GetWeaponTechLevel(weapon);

            // Recipe research (craftability) — cheap O(1) check before workbench search
            AcceptanceReport craftable = CustomizationRules.GetCraftabilityReport(baseDef);
            if (!craftable.Accepted)
                return DisabledOrHidden(weapon, craftable);

            // Customization research
            if (!customizable.Accepted)
                return DisabledOrHidden(weapon, customizable);

            string label = "UWU_CustomizeWeapon".Translate(weapon.LabelShortCap);

            // Weapon reachability + forbidden checks
            if (!pawn.CanReach(weapon, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return new FloatMenuOption(
                    label + " (" + "NoPath".Translate() + ")",
                    null);
            }

            // No forbidden check on the weapon itself — this is a direct
            // player order, matching vanilla's behavior for equipping forbidden weapons.

            // Find best workbench (most expensive check — runs last)
            var result = WorkbenchUtility.FindBestWorkbench(
                pawn, baseDef, uniqueDef, weaponTechLevel, weapon.Position);

            if (!result.Found)
                return DisabledOrHidden(weapon, result.BestRejection);

            Building_WorkTable workbench = result.Workbench;
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
                pawn, weapon);
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
