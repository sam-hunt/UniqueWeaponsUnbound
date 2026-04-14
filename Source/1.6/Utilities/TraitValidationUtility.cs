using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Validates trait combinations and provides filtered trait lists for the
    /// weapon customization dialog. Operates purely on defs — no initialization needed.
    /// </summary>
    public static class TraitValidationUtility
    {
        public const int MaxTraits = 3;

        /// <summary>
        /// Returns all weapon traits compatible with the given unique weapon def's
        /// categories, excluding BladeLink (Royalty persona) traits.
        /// This is the full list shown in the UI — individual traits may still be
        /// disabled based on the current desired trait selection.
        /// </summary>
        public static List<WeaponTraitDef> GetCompatibleTraits(ThingDef uniqueDef)
        {
            List<WeaponCategoryDef> categories = GetWeaponCategories(uniqueDef);
            if (categories == null || categories.Count == 0)
                return new List<WeaponTraitDef>();

            var result = new List<WeaponTraitDef>();
            foreach (WeaponTraitDef trait in DefDatabase<WeaponTraitDef>.AllDefs)
            {
                // Exclude Royalty bladelink/persona traits
                if (IsBladeLink(trait))
                    continue;

                if (categories.Contains(trait.weaponCategory))
                    result.Add(trait);
            }

            return result;
        }

        /// <summary>
        /// Returns null if the candidate trait can be added to the desired trait set,
        /// or a human-readable rejection reason if it cannot.
        /// </summary>
        public static string GetRejectionReason(
            List<WeaponTraitDef> desiredTraits, WeaponTraitDef candidate)
        {
            if (desiredTraits.Contains(candidate))
                return "UWU_AlreadyApplied".Translate();

            if (desiredTraits.Count >= MaxTraits)
                return "UWU_MaxTraitsReached".Translate();

            foreach (WeaponTraitDef existing in desiredTraits)
            {
                if (TraitsOverlap(candidate, existing))
                    return "UWU_ConflictsWith".Translate(existing.LabelCap);
            }

            if (UWU_Mod.Settings.enforceCanGenerateAlone
                && desiredTraits.Count == 0 && !candidate.canGenerateAlone)
                return "UWU_CannotBeOnlyTrait".Translate();

            return null;
        }

        /// <summary>
        /// Whether the given trait can be removed from the desired set without
        /// leaving an invalid configuration. Returns false if removal would leave
        /// a single trait that has canGenerateAlone=false.
        /// </summary>
        public static bool CanRemoveTrait(
            List<WeaponTraitDef> desiredTraits, WeaponTraitDef toRemove)
        {
            if (!desiredTraits.Contains(toRemove))
                return false;

            if (UWU_Mod.Settings.enforceCanGenerateAlone && desiredTraits.Count == 2)
            {
                WeaponTraitDef remaining = desiredTraits[0] == toRemove
                    ? desiredTraits[1]
                    : desiredTraits[0];
                if (!remaining.canGenerateAlone)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the reason a trait cannot be removed, or null if removal is allowed.
        /// </summary>
        public static string GetRemovalRejectionReason(
            List<WeaponTraitDef> desiredTraits, WeaponTraitDef toRemove)
        {
            if (UWU_Mod.Settings.enforceCanGenerateAlone
                && !CanRemoveTrait(desiredTraits, toRemove) && desiredTraits.Count == 2)
            {
                WeaponTraitDef remaining = desiredTraits[0] == toRemove
                    ? desiredTraits[1]
                    : desiredTraits[0];
                if (!remaining.canGenerateAlone)
                    return "UWU_TraitCannotBeOnlyTrait".Translate(remaining.LabelCap);
            }
            return null;
        }

        /// <summary>
        /// Whether two traits overlap — same def or shared exclusion tags.
        /// Mirrors the vanilla WeaponTraitDef.Overlaps() logic.
        /// </summary>
        public static bool TraitsOverlap(WeaponTraitDef a, WeaponTraitDef b)
        {
            if (a == b)
                return true;

            if (a.exclusionTags.NullOrEmpty() || b.exclusionTags.NullOrEmpty())
                return false;

            return a.exclusionTags.Any(tag => b.exclusionTags.Contains(tag));
        }

        /// <summary>
        /// Extracts the accepted weapon categories from a unique weapon def's
        /// CompProperties_UniqueWeapon.
        /// </summary>
        public static List<WeaponCategoryDef> GetWeaponCategories(ThingDef uniqueDef)
        {
            if (uniqueDef?.comps == null)
                return null;

            CompProperties_UniqueWeapon props = uniqueDef.comps
                .OfType<CompProperties_UniqueWeapon>()
                .FirstOrDefault();

            return props?.weaponCategories;
        }

        private static bool IsBladeLink(WeaponTraitDef trait)
        {
            return trait.weaponCategory != null
                && trait.weaponCategory.defName == "BladeLink";
        }
    }
}
