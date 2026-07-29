using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Swaps a fraction of one base material for another by count (1:1). Used
    // for lightweight bow traits that replace wood with birdskin.
    public class PartialSwapWorker : TraitCostRuleWorker
    {
        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            if (CostRuleHelpers.Birdskin != null)
                CostRuleHelpers.ApplyPartialSwapByCount(
                    costs, CostRuleHelpers.WoodLog, CostRuleHelpers.Birdskin, def.swapFraction);
        }
    }

    // Swaps a fraction of the weapon's own stuff for plain metal fittings,
    // whatever that stuff happens to be. The fitting material follows the
    // weapon's tech level: steel for industrial and below, plasteel for spacer
    // and above (overridable per rule). Models the metal parts on an otherwise
    // wooden or leather weapon — a steel-studded club is the normal object.
    //
    // A sibling of PartialSwapWorker rather than a mode of it: that worker's
    // source is a fixed material and its replacement is fixed too, while this
    // one derives both from the weapon, so folding them together would need two
    // orthogonal mode flags whose defaults exist only to preserve one rule.
    public class StuffFittingsSwapWorker : TraitCostRuleWorker
    {
        private ThingDef industrialMaterial;
        private ThingDef spacerMaterial;

        public override void OnStartup()
        {
            industrialMaterial = ResolveOverride(def.fittingsIndustrialDef, CostRuleHelpers.Steel);
            spacerMaterial = ResolveOverride(def.fittingsSpacerDef, CostRuleHelpers.Plasteel);
        }

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            ThingDef stuff = weapon?.Stuff;
            if (stuff == null)
                return;

            ThingDef fittings = CostRuleHelpers.SelectByTechLevel(
                weapon, industrialMaterial, spacerMaterial);
            if (fittings == null || fittings == stuff)
                return;

            CostRuleHelpers.ApplyPartialSwapByCount(costs, stuff, fittings, def.swapFraction);
        }

        private ThingDef ResolveOverride(string defName, ThingDef fallback)
        {
            if (defName.NullOrEmpty())
                return fallback;

            ThingDef resolved = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (resolved == null && Prefs.DevMode)
                Log.Message("[Unique Weapons Unbound] Cost rule " + def.defName
                    + ": no loaded ThingDef named " + defName
                    + " for the fittings material; using " + fallback?.defName + " instead.");

            return resolved ?? fallback;
        }
    }
}
