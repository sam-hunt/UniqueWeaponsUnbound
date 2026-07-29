using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // One ingredient line an additive cost rule appends. Comes in two forms:
    //
    //   fixed  - thingDef, optionally with fallbackDef used when the primary is
    //            not loaded (e.g. SignalChip -> ComponentSpacer without Biotech)
    //   tiered - industrialDef/spacerDef (plus optional lowDef), picked from the
    //            weapon's tech level when the cost is calculated
    //
    // DefNames are strings rather than typed ThingDef fields on purpose: a typed
    // field would make the ingredient's source mod or DLC a hard dependency,
    // whereas a string resolves silently and leaves the line out when nothing
    // matches. Fields mirror ThingDefCountClass's XML shape (thingDef/count) so
    // rule XML reads familiarly.
    public class TraitCostIngredient
    {
        public string thingDef;
        public string fallbackDef;
        public string lowDef;
        public string industrialDef;
        public string spacerDef;
        public int count = 1;

        [Unsaved(false)]
        private ThingDef primaryResolved;
        [Unsaved(false)]
        private ThingDef lowResolved;
        [Unsaved(false)]
        private ThingDef industrialResolved;
        [Unsaved(false)]
        private ThingDef spacerResolved;

        // A spec is tech-tiered as soon as it names any tier def; the fixed
        // thingDef/fallbackDef pair is then unused (ConfigErrors flags mixing
        // the two forms).
        public bool Tiered =>
            !lowDef.NullOrEmpty() || !industrialDef.NullOrEmpty() || !spacerDef.NullOrEmpty();

        // Whether anything at all resolved. A spec that resolved to nothing is
        // inert: the worker skips it and the rule's other lines still apply.
        public bool IsResolved =>
            Tiered
                ? lowResolved != null || industrialResolved != null || spacerResolved != null
                : primaryResolved != null;

        // Resolves every declared defName once, at startup. Returns a
        // comma-separated list of the defNames nothing matched, or null when
        // everything needed resolved — the caller reports it in dev mode. A
        // missing primary is not reported when its fallback resolved: that is
        // the fallback's whole purpose.
        public string ResolveDefs()
        {
            List<string> missing = null;

            if (Tiered)
            {
                lowResolved = Resolve(lowDef, ref missing);
                industrialResolved = Resolve(industrialDef, ref missing);
                spacerResolved = Resolve(spacerDef, ref missing);
            }
            else
            {
                primaryResolved = Resolve(thingDef, ref missing);
                if (primaryResolved == null && !fallbackDef.NullOrEmpty())
                {
                    primaryResolved = Resolve(fallbackDef, ref missing);
                    // A missing primary is the fallback's whole purpose, so once
                    // the fallback lands there is nothing to report.
                    if (primaryResolved != null)
                        missing = null;
                }
            }

            return missing == null ? null : string.Join(", ", missing.ToArray());
        }

        // The ThingDef this line bills for the given weapon, or null when the
        // spec is inert (or has no def for that weapon's tier).
        public ThingDef Select(Thing weapon)
        {
            if (!Tiered)
                return primaryResolved;

            // Three-tier only when a low tier was named and resolved; otherwise
            // the industrial/spacer pair covers everything at or below
            // industrial, which is what the tech-level boundary calls for.
            return lowResolved != null
                ? CostRuleHelpers.SelectByTechLevel(
                    weapon, lowResolved, industrialResolved, spacerResolved)
                : CostRuleHelpers.SelectByTechLevel(
                    weapon, industrialResolved, spacerResolved);
        }

        public IEnumerable<string> ConfigErrors()
        {
            if (Tiered)
            {
                if (!thingDef.NullOrEmpty() || !fallbackDef.NullOrEmpty())
                    yield return "ingredient mixes the fixed form (thingDef/fallbackDef) "
                        + "with the tech-tiered form; the fixed fields are ignored";
                if (industrialDef.NullOrEmpty() || spacerDef.NullOrEmpty())
                    yield return "tech-tiered ingredient must set both industrialDef and "
                        + "spacerDef (lowDef is optional)";
            }
            else if (thingDef.NullOrEmpty())
            {
                yield return "ingredient sets neither thingDef nor the tech-tiered defNames";
            }

            if (count <= 0)
                yield return "ingredient count must be positive";
        }

        public override string ToString()
        {
            string name = Tiered
                ? (lowDef.NullOrEmpty() ? "" : lowDef + "/") + industrialDef + "/" + spacerDef
                : thingDef + (fallbackDef.NullOrEmpty() ? "" : " (fallback " + fallbackDef + ")");
            return count + "x " + name;
        }

        private static ThingDef Resolve(string defName, ref List<string> missing)
        {
            if (defName.NullOrEmpty())
                return null;

            ThingDef resolved = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (resolved == null)
            {
                if (missing == null)
                    missing = new List<string>();
                missing.Add(defName);
            }
            return resolved;
        }
    }

    // Appends the rule's ingredient lines on top of whatever the pipeline has
    // computed so far — the first additive worker. Existing entries are never
    // modified or removed; a line for a material already present just raises
    // that entry's count.
    //
    // With refundable set to false the worker contributes nothing on the removal
    // pipeline, so the player pays the surcharge when adding the trait and gets
    // none of it back when removing it. That asymmetry is the entire
    // "unrefundable" mechanism.
    public class AddIngredientsWorker : TraitCostRuleWorker
    {
        public override void OnStartup()
        {
            if (def.addIngredients.NullOrEmpty())
                return;

            foreach (TraitCostIngredient ingredient in def.addIngredients)
            {
                string missing = ingredient.ResolveDefs();
                if (missing == null || !Prefs.DevMode)
                    continue;

                Log.Message("[Unique Weapons Unbound] Cost rule " + def.defName
                    + ": ingredient " + ingredient + " found no loaded ThingDef for "
                    + missing + (ingredient.IsResolved
                        ? ". Remaining tiers still apply."
                        : ". The line is omitted; the rest of the rule still runs."));
            }
        }

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            if (isRemoval && !def.refundable)
                return;
            if (def.addIngredients.NullOrEmpty())
                return;

            foreach (TraitCostIngredient ingredient in def.addIngredients)
            {
                if (ingredient.count <= 0)
                    continue;

                ThingDef thing = ingredient.Select(weapon);
                if (thing != null)
                    CostRuleHelpers.AddOrMerge(costs, thing, ingredient.count);
            }
        }
    }
}
