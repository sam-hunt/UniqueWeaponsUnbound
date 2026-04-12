using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Stateless game-rule predicates for determining whether weapons are
    /// customizable and what research is required.
    /// </summary>
    public static class CustomizationRules
    {
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

            if (WeaponRegistry.IsUniqueWeapon(def))
            {
                // Unique weapons are always in the customization system
                // regardless of whether a base def exists.
            }
            else
            {
                if (WeaponRegistry.GetUniqueVariant(def) == null)
                    return false;
            }

            if (UWU_Mod.Settings.requireCustomizationResearch)
            {
                // Don't surface any customization UI until the player has completed
                // UniqueSmithing, so we don't clutter menus for uninterested players.
                if (!UWU_ResearchDefOf.UniqueSmithing.IsFinished)
                    return false;

                ResearchProjectDef requiredResearch = GetRequiredResearch(def.techLevel);
                if (requiredResearch == null)
                    return false;

                if (!requiredResearch.IsFinished)
                    return "UWU_RequiresResearch".Translate(requiredResearch.label);
            }
            else
            {
                // Even without research requirements, tech-level gating still applies
                if (GetRequiredResearch(def.techLevel) == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Whether the base weapon's crafting prerequisites are met.
        /// Returns AcceptanceReport with the blocking research name, or false
        /// with no reason for uncraftable weapons without the mod setting.
        /// </summary>
        public static AcceptanceReport GetCraftabilityReport(ThingDef baseDef, ThingDef uniqueDef)
        {
            RecipeMakerProperties recipeMaker = baseDef?.recipeMaker ?? uniqueDef?.recipeMaker;
            if (recipeMaker == null)
                return UWU_Mod.Settings.allowUncraftableCustomization;

            if (UWU_Mod.Settings.requireRecipeResearch)
            {
                ResearchProjectDef recipeResearch = recipeMaker.researchPrerequisite;
                if (recipeResearch != null && !recipeResearch.IsFinished)
                    return "UWU_RequiresResearch".Translate(recipeResearch.label);
            }

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
        /// Returns the weapon's tech level if it participates in the customization system.
        /// Returns TechLevel.Undefined if the weapon has no customization path.
        /// </summary>
        public static TechLevel GetWeaponTechLevel(Thing weapon)
        {
            ThingDef def = weapon.def;

            if (WeaponRegistry.IsUniqueWeapon(def))
                return def.techLevel;

            if (WeaponRegistry.GetUniqueVariant(def) != null)
                return def.techLevel;

            return TechLevel.Undefined;
        }
    }
}
