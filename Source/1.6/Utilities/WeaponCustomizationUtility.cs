using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Central utility for determining which weapons are customizable.
    /// Caches base↔unique weapon pair mappings at startup and provides
    /// runtime checks for research, craftability, and customizability.
    /// </summary>
    public static class WeaponCustomizationUtility
    {
        private static Dictionary<ThingDef, ThingDef> baseToUnique;
        private static Dictionary<ThingDef, ThingDef> uniqueToBase;

        private static HashSet<ThingDef> smithyDefs;
        private static HashSet<ThingDef> machiningDefs;
        private static HashSet<ThingDef> fabricationDefs;
        private static HashSet<ThingDef> weaponWorkbenchDefs;
        private static string smithyLabel;
        private static string machiningLabel;
        private static string fabricationLabel;

        /// <summary>
        /// Builds the base↔unique weapon pair cache. Must be called during
        /// StaticConstructorOnStartup (after all defs are loaded).
        /// </summary>
        public static void Initialize()
        {
            baseToUnique = new Dictionary<ThingDef, ThingDef>();
            uniqueToBase = new Dictionary<ThingDef, ThingDef>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (!def.HasComp(typeof(CompUniqueWeapon)))
                    continue;

                ThingDef baseDef = FindBaseWeapon(def);
                if (baseDef != null)
                {
                    uniqueToBase[def] = baseDef;
                    baseToUnique[baseDef] = def;
                }
                else
                {
                    Log.Warning($"[Unique Weapons Unbound] Unique weapon {def.defName} has no detectable base weapon.");
                }
            }

            Log.Message($"[Unique Weapons Unbound] Cached {uniqueToBase.Count} base/unique weapon pairs.");

            // Initialize workbench tier sets from vanilla anchors
            smithyDefs = ResolveDefSet("FueledSmithy", "ElectricSmithy");
            machiningDefs = ResolveDefSet("TableMachining");
            fabricationDefs = ResolveDefSet("FabricationBench");

            // Resolve display labels from vanilla anchors only (before expanding with modded benches)
            smithyLabel = ResolveWorkbenchLabel(smithyDefs);
            machiningLabel = ResolveWorkbenchLabel(machiningDefs);
            fabricationLabel = ResolveWorkbenchLabel(fabricationDefs);

            // Expand tier sets with benches that inherit recipes from vanilla anchors via VEF
            ExpandTiersFromVEF();

            // Build the set of all workbench defs that have at least one weapon recipe
            InitializeWeaponWorkbenches();
        }

        /// <summary>
        /// Detects the base weapon for a unique weapon def.
        /// Primary: descriptionHyperlinks. Fallback: naming convention.
        /// </summary>
        private static ThingDef FindBaseWeapon(ThingDef uniqueDef)
        {
            // Primary: descriptionHyperlinks — works for modded weapons that may not follow naming conventions
            if (uniqueDef.descriptionHyperlinks != null)
            {
                foreach (DefHyperlink link in uniqueDef.descriptionHyperlinks)
                {
                    if (link.def is ThingDef linked && linked.IsWeapon && !linked.HasComp(typeof(CompUniqueWeapon)))
                        return linked;
                }
            }

            // Fallback: naming convention ({BaseDefName}_Unique)
            if (uniqueDef.defName.EndsWith("_Unique"))
            {
                string baseName = uniqueDef.defName.Substring(0, uniqueDef.defName.Length - "_Unique".Length);
                return DefDatabase<ThingDef>.GetNamedSilentFail(baseName);
            }

            return null;
        }

        /// <summary>
        /// Returns the unique variant for a base weapon def, or null if none exists.
        /// </summary>
        public static ThingDef GetUniqueVariant(ThingDef baseDef)
        {
            return baseToUnique.TryGetValue(baseDef, out ThingDef unique) ? unique : null;
        }

        /// <summary>
        /// Returns the base weapon for a unique weapon def, or null if not found.
        /// </summary>
        public static ThingDef GetBaseVariant(ThingDef uniqueDef)
        {
            return uniqueToBase.TryGetValue(uniqueDef, out ThingDef baseDef) ? baseDef : null;
        }

        /// <summary>
        /// Whether the def is a unique weapon (has CompUniqueWeapon).
        /// </summary>
        public static bool IsUniqueWeapon(ThingDef def)
        {
            return def.HasComp(typeof(CompUniqueWeapon));
        }

        /// <summary>
        /// Whether this weapon has a customization path and the player has unlocked
        /// the required customization research. Does not check craftability (recipe
        /// research) — call <see cref="GetCraftabilityReport"/> separately so callers
        /// can insert context-dependent checks (e.g. workbench tier) in between.
        /// Returns AcceptanceReport with a rejection reason if not customizable.
        /// Returns false with no reason when the option should be hidden entirely.
        /// </summary>
        public static AcceptanceReport IsCustomizable(Thing weapon)
        {
            ThingDef def = weapon.def;

            ThingDef baseDef;
            if (IsUniqueWeapon(def))
            {
                baseDef = GetBaseVariant(def);
                if (baseDef == null)
                    return false;
            }
            else
            {
                if (GetUniqueVariant(def) == null)
                    return false;
                baseDef = def;
            }

            // Don't surface any customization UI until the player has completed
            // UniqueSmithing, so we don't clutter menus for uninterested players.
            if (!UWU_ResearchDefOf.UniqueSmithing.IsFinished)
                return false;

            ResearchProjectDef requiredResearch = GetRequiredResearch(baseDef.techLevel);
            if (requiredResearch == null)
                return false;

            if (!requiredResearch.IsFinished)
                return "UWU_RequiresResearch".Translate(requiredResearch.label);

            return true;
        }

        /// <summary>
        /// Whether the base weapon's crafting prerequisites are met.
        /// Returns AcceptanceReport with the blocking research name, or false
        /// with no reason for uncraftable weapons without the mod setting.
        /// </summary>
        public static AcceptanceReport GetCraftabilityReport(ThingDef baseDef)
        {
            if (baseDef.recipeMaker == null)
                return UWU_Mod.Settings.allowUncraftableCustomization;

            ResearchProjectDef recipeResearch = baseDef.recipeMaker.researchPrerequisite;
            if (recipeResearch != null && !recipeResearch.IsFinished)
                return "UWU_RequiresResearch".Translate(recipeResearch.label);

            return true;
        }

        /// <summary>
        /// Returns the required research project for customizing weapons of the given tech level,
        /// or null if the tech level is not customizable (e.g. Archotech without mod setting).
        /// </summary>
        public static ResearchProjectDef GetRequiredResearch(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.Neolithic:
                case TechLevel.Medieval:
                    return UWU_ResearchDefOf.UniqueSmithing;

                case TechLevel.Industrial:
                    return UWU_ResearchDefOf.UniqueMachining;

                case TechLevel.Spacer:
                    return UWU_ResearchDefOf.UniqueFabrication;

                case TechLevel.Ultra:
                    if (UWU_Mod.Settings.allowUltratechCustomization)
                        return UWU_ResearchDefOf.UniqueFabrication;
                    return null;

                case TechLevel.Archotech:
                    if (UWU_Mod.Settings.allowArchotechCustomization)
                        return UWU_ResearchDefOf.UniqueFabrication;
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Whether the player has completed the required research for the given tech level.
        /// </summary>
        public static bool HasRequiredResearch(TechLevel techLevel)
        {
            ResearchProjectDef required = GetRequiredResearch(techLevel);
            return required != null && required.IsFinished;
        }

        /// <summary>
        /// Whether a base weapon's crafting recipe exists and its prerequisite research is done.
        /// </summary>
        public static bool IsBaseCraftable(ThingDef baseDef)
        {
            if (baseDef.recipeMaker == null)
            {
                return UWU_Mod.Settings.allowUncraftableCustomization;
            }

            ResearchProjectDef recipeResearch = baseDef.recipeMaker.researchPrerequisite;
            return recipeResearch == null || recipeResearch.IsFinished;
        }

        /// <summary>
        /// Resolves a display label for a set of workbench defNames by finding the
        /// common suffix of their labels. For a single def, returns its label directly.
        /// This handles cases like "fueled smithy" / "electric smithy" → "smithy".
        /// </summary>
        private static string ResolveWorkbenchLabel(HashSet<ThingDef> defs)
        {
            List<string> labels = new List<string>();
            foreach (ThingDef def in defs)
            {
                labels.Add(def.label);
            }

            if (labels.Count == 0)
                return "?";
            if (labels.Count == 1)
                return labels[0];

            // Find the longest common suffix across all labels.
            string reference = labels[0];
            int commonLength = reference.Length;
            for (int i = 1; i < labels.Count; i++)
            {
                string other = labels[i];
                int matchLen = 0;
                int ri = reference.Length - 1;
                int oi = other.Length - 1;
                while (ri >= 0 && oi >= 0 && reference[ri] == other[oi])
                {
                    matchLen++;
                    ri--;
                    oi--;
                }
                commonLength = Math.Min(commonLength, matchLen);
            }

            if (commonLength > 0)
            {
                string suffix = reference.Substring(reference.Length - commonLength).TrimStart();
                if (suffix.Length > 0)
                    return suffix;
            }

            return labels[0];
        }

        /// <summary>
        /// Returns the tech level relevant for customization checks.
        /// For unique weapons, returns the base weapon's tech level.
        /// For base weapons with a unique variant, returns the weapon's own tech level.
        /// Returns TechLevel.Undefined if the weapon has no customization path.
        /// </summary>
        public static TechLevel GetWeaponTechLevel(Thing weapon)
        {
            ThingDef def = weapon.def;

            if (IsUniqueWeapon(def))
            {
                ThingDef baseDef = GetBaseVariant(def);
                return baseDef?.techLevel ?? TechLevel.Undefined;
            }

            if (GetUniqueVariant(def) != null)
                return def.techLevel;

            return TechLevel.Undefined;
        }

        /// <summary>
        /// Resolves the base and unique ThingDefs for a weapon, regardless of
        /// whether the weapon is currently in its base or unique form.
        /// </summary>
        public static void ResolveWeaponDefs(Thing weapon, out ThingDef baseDef, out ThingDef uniqueDef)
        {
            if (IsUniqueWeapon(weapon.def))
            {
                uniqueDef = weapon.def;
                baseDef = GetBaseVariant(weapon.def);
            }
            else
            {
                baseDef = weapon.def;
                uniqueDef = GetUniqueVariant(weapon.def);
            }
        }

        /// <summary>
        /// Result of searching for a valid workbench to customize a weapon at.
        /// Either contains a workbench or the highest-priority rejection reason.
        /// </summary>
        public struct WorkbenchSearchResult
        {
            public Building_WorkTable Workbench;
            public AcceptanceReport BestRejection;
            public bool Found => Workbench != null;
        }

        /// <summary>
        /// Finds the closest valid colonist workbench for customizing the specified weapon.
        /// Pawn-specific overload: checks reachability via the pawn's pathfinder and
        /// forbidden status relative to the pawn.
        /// </summary>
        public static WorkbenchSearchResult FindBestWorkbench(
            Pawn pawn, ThingDef baseDef, ThingDef uniqueDef, TechLevel weaponTechLevel,
            IntVec3 distanceOrigin)
        {
            return FindBestWorkbenchCore(pawn.Map, baseDef, uniqueDef, weaponTechLevel,
                distanceOrigin, workbench =>
                {
                    if (!pawn.CanReach(workbench, PathEndMode.InteractionCell, Danger.Deadly))
                        return "NoPath".Translate();
                    if (workbench.IsForbidden(pawn))
                        return "ForbiddenLower".Translate();
                    return true;
                });
        }

        /// <summary>
        /// Finds the closest valid colonist workbench for customizing the specified weapon.
        /// Pawn-independent overload: checks generic reachability from a map position and
        /// forbidden status relative to the player faction. Used for gizmo enabled/disabled
        /// state where no specific pawn is known yet.
        /// </summary>
        public static WorkbenchSearchResult FindBestWorkbench(
            Map map, ThingDef baseDef, ThingDef uniqueDef, TechLevel weaponTechLevel,
            IntVec3 distanceOrigin)
        {
            return FindBestWorkbenchCore(map, baseDef, uniqueDef, weaponTechLevel,
                distanceOrigin, workbench =>
                {
                    if (!map.reachability.CanReach(distanceOrigin, workbench,
                            PathEndMode.InteractionCell,
                            TraverseParms.For(TraverseMode.PassDoors)))
                        return "NoPath".Translate();
                    if (workbench.IsForbidden(Faction.OfPlayer))
                        return "ForbiddenLower".Translate();
                    return true;
                });
        }

        /// <summary>
        /// Common core for workbench search. Iterates colonist workbenches, applies tier
        /// and operational checks, then delegates reachability/forbidden checks to the
        /// caller-provided predicate. Returns the closest valid workbench or the
        /// highest-priority rejection reason.
        /// </summary>
        private static WorkbenchSearchResult FindBestWorkbenchCore(
            Map map, ThingDef baseDef, ThingDef uniqueDef, TechLevel weaponTechLevel,
            IntVec3 distanceOrigin, Func<Building_WorkTable, AcceptanceReport> accessCheck)
        {
            Building_WorkTable bestWorkbench = null;
            float bestDistSq = float.MaxValue;
            int bestRejectionPriority = -1;
            AcceptanceReport bestRejection = false;

            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (!(building is Building_WorkTable workbench))
                    continue;
                if (!weaponWorkbenchDefs.Contains(workbench.def))
                    continue;

                // Tier check (priority 4)
                AcceptanceReport tierReport = CanCustomizeAtWorkbench(
                    baseDef, uniqueDef, weaponTechLevel, workbench);
                if (!tierReport.Accepted)
                {
                    if (bestRejectionPriority < 4)
                    {
                        bestRejectionPriority = 4;
                        bestRejection = tierReport;
                    }
                    continue;
                }

                // Operational check (priority 3)
                AcceptanceReport opReport = GetWorkbenchOperationalReport(workbench);
                if (!opReport.Accepted)
                {
                    if (bestRejectionPriority < 3)
                    {
                        bestRejectionPriority = 3;
                        bestRejection = opReport;
                    }
                    continue;
                }

                // Caller-provided access check (reachability + forbidden)
                AcceptanceReport accessReport = accessCheck(workbench);
                if (!accessReport.Accepted)
                {
                    // Determine priority from the rejection reason
                    int priority = accessReport.Reason == "ForbiddenLower".Translate() ? 1 : 2;
                    if (bestRejectionPriority < priority)
                    {
                        bestRejectionPriority = priority;
                        bestRejection = accessReport;
                    }
                    continue;
                }

                // Valid candidate — track closest
                float distSq = (distanceOrigin - workbench.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestWorkbench = workbench;
                }
            }

            var result = new WorkbenchSearchResult();
            if (bestWorkbench != null)
            {
                result.Workbench = bestWorkbench;
            }
            else
            {
                result.BestRejection = bestRejectionPriority >= 0
                    ? bestRejection
                    : "UWU_NoSuitableWorkbench".Translate();
            }
            return result;
        }

        /// <summary>
        /// Whether the workbench has at least one recipe that produces a weapon,
        /// making it eligible to show the customization float menu option.
        /// </summary>
        public static bool IsCustomizationWorkbench(Building_WorkTable workbench)
        {
            return weaponWorkbenchDefs.Contains(workbench.def);
        }

        /// <summary>
        /// Whether the given workbench supports customizing the specified weapon.
        /// First checks if the bench has a recipe for the weapon (base or unique def).
        /// Falls back to tech-level tier matching (vanilla anchors + VEF inheritance).
        /// Returns AcceptanceReport with the required workbench name when the tier is too low.
        /// </summary>
        public static AcceptanceReport CanCustomizeAtWorkbench(
            ThingDef baseDef, ThingDef uniqueDef, TechLevel weaponTechLevel,
            Building_WorkTable workbench)
        {
            // Layer 2: Direct recipe match — if this bench can craft this weapon, allow customization
            if (WorkbenchHasRecipeFor(workbench.def, baseDef, uniqueDef))
                return true;

            // Layer 3: Tech-level tier fallback
            ThingDef benchDef = workbench.def;
            bool isMachiningOrHigher = machiningDefs.Contains(benchDef) || fabricationDefs.Contains(benchDef);
            bool isFabrication = fabricationDefs.Contains(benchDef);

            switch (weaponTechLevel)
            {
                case TechLevel.Neolithic:
                case TechLevel.Medieval:
                    return true;

                case TechLevel.Industrial:
                    if (isMachiningOrHigher)
                        return true;
                    return "UWU_RequiresWorkbench".Translate(machiningLabel);

                case TechLevel.Spacer:
                case TechLevel.Ultra:
                case TechLevel.Archotech:
                    if (isFabrication)
                        return true;
                    return "UWU_RequiresWorkbench".Translate(fabricationLabel);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether the workbench is operational (powered and/or fueled as required).
        /// Returns AcceptanceReport with a rejection reason if not operational.
        /// </summary>
        public static AcceptanceReport GetWorkbenchOperationalReport(Building_WorkTable workbench)
        {
            CompPowerTrader power = workbench.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
                return "NoPower".Translate();

            CompRefuelable fuel = workbench.TryGetComp<CompRefuelable>();
            if (fuel != null && !fuel.HasFuel)
                return "NoFuel".Translate();

            return true;
        }

        /// <summary>
        /// Resolves an array of defNames into a set of ThingDefs, silently skipping any that don't exist.
        /// </summary>
        private static HashSet<ThingDef> ResolveDefSet(params string[] defNames)
        {
            var set = new HashSet<ThingDef>();
            foreach (string name in defNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def != null)
                    set.Add(def);
            }
            return set;
        }

        /// <summary>
        /// Expands workbench tier sets by walking VEF's RecipeInheritanceExtension.
        /// Benches that inherit recipes from a vanilla anchor are classified at the
        /// highest tier of their inheritance sources.
        /// </summary>
        private static void ExpandTiersFromVEF()
        {
            Type extensionType = GenTypes.GetTypeInAnyAssembly("VEF.Buildings.RecipeInheritanceExtension");
            if (extensionType == null)
                return;

            FieldInfo field = extensionType.GetField("inheritRecipesFrom",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                return;

            int added = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.modExtensions == null)
                    continue;
                if (smithyDefs.Contains(def) || machiningDefs.Contains(def) || fabricationDefs.Contains(def))
                    continue;

                foreach (DefModExtension ext in def.modExtensions)
                {
                    if (!extensionType.IsInstanceOfType(ext))
                        continue;

                    if (!(field.GetValue(ext) is List<ThingDef> inheritFrom))
                        continue;

                    // Classify at highest inherited tier
                    HashSet<ThingDef> bestTier = null;
                    foreach (ThingDef source in inheritFrom)
                    {
                        if (fabricationDefs.Contains(source))
                        {
                            bestTier = fabricationDefs;
                            break;
                        }
                        if (machiningDefs.Contains(source))
                            bestTier = machiningDefs;
                        else if (smithyDefs.Contains(source) && bestTier == null)
                            bestTier = smithyDefs;
                    }

                    if (bestTier != null)
                    {
                        bestTier.Add(def);
                        added++;
                    }
                    break;
                }
            }

            if (added > 0)
                Log.Message($"[Unique Weapons Unbound] Classified {added} modded workbench(es) via VEF recipe inheritance.");
        }

        /// <summary>
        /// Builds the set of all Building_WorkTable defs that have at least one recipe
        /// producing a weapon. Used as the visibility gate for the customization float menu.
        /// </summary>
        private static void InitializeWeaponWorkbenches()
        {
            weaponWorkbenchDefs = new HashSet<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (!typeof(Building_WorkTable).IsAssignableFrom(def.thingClass))
                    continue;

                List<RecipeDef> recipes = def.AllRecipes;
                if (recipes == null)
                    continue;

                foreach (RecipeDef recipe in recipes)
                {
                    if (recipe.ProducedThingDef != null && recipe.ProducedThingDef.IsWeapon)
                    {
                        weaponWorkbenchDefs.Add(def);
                        break;
                    }
                }
            }

            // Include tier-classified benches regardless of whether their inherited
            // recipes are visible yet (VEF may not have processed them due to load order)
            weaponWorkbenchDefs.UnionWith(smithyDefs);
            weaponWorkbenchDefs.UnionWith(machiningDefs);
            weaponWorkbenchDefs.UnionWith(fabricationDefs);

            Log.Message($"[Unique Weapons Unbound] Found {weaponWorkbenchDefs.Count} workbench(es) with weapon recipes.");
        }

        /// <summary>
        /// Whether the workbench def has a recipe that produces the given base or unique weapon def.
        /// </summary>
        private static bool WorkbenchHasRecipeFor(ThingDef benchDef, ThingDef baseDef, ThingDef uniqueDef)
        {
            List<RecipeDef> recipes = benchDef.AllRecipes;
            if (recipes == null)
                return false;

            foreach (RecipeDef recipe in recipes)
            {
                ThingDef produced = recipe.ProducedThingDef;
                if (produced != null && (produced == baseDef || produced == uniqueDef))
                    return true;
            }
            return false;
        }
    }
}
