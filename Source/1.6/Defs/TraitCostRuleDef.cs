using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Defines a trait cost rule that participates in the cost calculation
    // pipeline. Rules are executed in priority order (lower first). Each rule
    // has a worker class that performs the actual cost transformation.
    public class TraitCostRuleDef : Def
    {
        public Type workerClass;
        public int priority;
        // Language packs may replace this list wholesale with a different entry
        // count (translators keep the English words and append localized ones).
        [TranslationCanChangeCount]
        public List<string> labelKeywords;
        public bool requireAllKeywords;
        public List<WeaponCategoryDef> weaponCategories;

        // Ingredient lines AddIngredientsWorker appends on top of the computed
        // cost. Each entry names its ThingDef by defName, so a rule can point at
        // a modded or DLC item without making that content a hard dependency:
        // an entry nothing matches is simply left out.
        public List<TraitCostIngredient> addIngredients;

        // Whether an additive rule's surcharge comes back when the trait is
        // removed. When false, AddIngredientsWorker adds nothing to the removal
        // pipeline, so the player pays it on addition and is refunded none of
        // it — the whole "unrefundable" mechanism.
        public bool refundable = true;

        // Factor CostFactorWorker applies to every cost. Defaults to 2 so
        // DoubleCostWorker rules need not state it.
        public float costFactor = 2f;

        // Fraction of the source material the partial-swap workers move to their
        // replacement, 1:1 by count. Defaults to the 40% the shipped
        // lightweight-bow rule expects.
        public float swapFraction = 0.4f;

        // Materials StuffFittingsSwapWorker swaps a weapon's stuff for, picked
        // by the weapon's tech level: industrial and below take the first,
        // spacer and above the second. Unset, they resolve to steel and
        // plasteel.
        public string fittingsIndustrialDef;
        public string fittingsSpacerDef;

        [Unsaved(false)]
        private TraitCostRuleWorker workerInt;

        public TraitCostRuleWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (TraitCostRuleWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string err in base.ConfigErrors())
                yield return err;
            if (workerClass == null)
                yield return "workerClass is null";
            else if (!typeof(TraitCostRuleWorker).IsAssignableFrom(workerClass))
                yield return $"workerClass {workerClass} must extend TraitCostRuleWorker";

            if (addIngredients != null)
            {
                foreach (TraitCostIngredient ingredient in addIngredients)
                {
                    foreach (string err in ingredient.ConfigErrors())
                        yield return err;
                }
            }
        }
    }
}
