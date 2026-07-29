using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Replaces costs with the weapon's actual crafting recipe ingredients. Only
    // acts when the weapon has a craftable base def with recipe costs (or a
    // standalone make-recipe producing it); otherwise leaves the tech-level
    // fallback costs in place.
    public class BaseCostFromRecipeWorker : TraitCostRuleWorker
    {
        // Weapons made via a standalone RecipeDef rather than costList /
        // costStuffCount (e.g. Odyssey's Make_BeamGraser for the Biotech
        // Gun_BeamGraser). Built once at startup; first matching recipe per
        // product wins, following DefDatabase order.
        private Dictionary<ThingDef, RecipeDef> recipeByProduct;

        public override void OnStartup()
        {
            recipeByProduct = new Dictionary<ThingDef, RecipeDef>();
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs)
            {
                if (recipe.products == null || recipe.products.Count != 1)
                    continue;

                ThingDefCountClass product = recipe.products[0];
                if (product.count != 1 || product.thingDef == null || !product.thingDef.IsWeapon)
                    continue;

                if (!recipeByProduct.ContainsKey(product.thingDef))
                    recipeByProduct[product.thingDef] = recipe;
            }
        }

        public override void Apply(List<ThingDefCountClass> costs, Thing weapon, WeaponTraitDef trait, bool isRemoval)
        {
            if (UWU_Mod.Settings is { useRecipeBaseCost: false })
                return;

            ThingDef recipeDef = CostRuleHelpers.ResolveCostBasisDef(weapon);
            if (recipeDef == null)
                return;

            var recipeCosts = new List<ThingDefCountClass>();

            if (recipeDef.costList != null)
            {
                foreach (ThingDefCountClass entry in recipeDef.costList)
                    recipeCosts.Add(new ThingDefCountClass(entry.thingDef, entry.count));
            }

            if (recipeDef.costStuffCount > 0)
            {
                ThingDef stuff = weapon.Stuff
                    ?? GenStuff.DefaultStuffFor(recipeDef)
                    ?? ThingDefOf.Steel;
                recipeCosts.Add(new ThingDefCountClass(stuff, recipeDef.costStuffCount));
            }

            // No def-level costs: fall back to a standalone make-recipe. Only
            // fixed (single-def filter) ingredients translate to a cost line;
            // open category filters have no single def to bill and are skipped.
            if (recipeCosts.Count == 0
                && recipeByProduct != null
                && recipeByProduct.TryGetValue(recipeDef, out RecipeDef makeRecipe)
                && makeRecipe.ingredients != null)
            {
                foreach (IngredientCount ingredient in makeRecipe.ingredients)
                {
                    if (!ingredient.IsFixedIngredient)
                        continue;

                    int count = Mathf.CeilToInt(ingredient.GetBaseCount());
                    if (count > 0)
                        recipeCosts.Add(new ThingDefCountClass(ingredient.FixedIngredient, count));
                }
            }

            if (recipeCosts.Count > 0)
            {
                costs.Clear();
                costs.AddRange(recipeCosts);
            }
        }
    }
}
