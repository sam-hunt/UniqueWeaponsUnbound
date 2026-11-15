using System.Collections.Generic;
using RimWorld;

namespace UniqueWeaponsUnbound
{
    // The customization dialog's rules for trait colors (WeaponTraitDef.
    // forcedColor), kept pure — no settings reads, no Rand, no dialog state —
    // so the suite can pin them (TraitColorRulesTests).
    //
    // Vanilla reads forcedColor exactly once, in CompUniqueWeapon.PostPostMake,
    // to stamp the comp's color field at generation; ForceColor() then returns
    // that field with no trait scan (decompile-verified on 1.6 and 1.7: no
    // other reader exists). So after generation the "trait forces the color"
    // rule is enforced only by this mod's dialog, and the enforceTraitColors
    // setting can relax it without any patch.
    //
    // Two modes, one invariant:
    //   - Enforced (default): the last staged trait with a forcedColor decides
    //     the weapon's color, mirroring vanilla generation.
    //   - Not enforced: staging such a trait selects its color but the player
    //     may then pick any other; the trait's color is offered as a swatch
    //     for as long as the trait is staged.
    //   - Either way a trait-only color is obtainable only through its trait:
    //     un-staging the last trait that carries the selected color strands
    //     the selection, and the dialog replaces it.
    public static class TraitColorRules
    {
        // The color the staged traits force on the weapon, or null when none
        // does or forcing is off. Last wins, mirroring vanilla's iteration in
        // CompUniqueWeapon.PostPostMake.
        public static ColorDef ForcedColor(List<WeaponTraitDef> traits, bool enforce)
        {
            if (!enforce)
                return null;

            ColorDef forced = null;
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].forcedColor != null)
                    forced = traits[i].forcedColor;
            }
            return forced;
        }

        // Fills into with the distinct forced colors of the staged traits, in
        // trait order. Normally zero or one entry (vanilla's color traits all
        // share the Color exclusion tag), but a modded trait can carry a
        // forcedColor without that tag, so this is a list.
        public static void StagedTraitColors(List<WeaponTraitDef> traits, List<ColorDef> into)
        {
            into.Clear();
            for (int i = 0; i < traits.Count; i++)
            {
                ColorDef color = traits[i].forcedColor;
                if (color != null && !into.Contains(color))
                    into.Add(color);
            }
        }

        // Whether any of the traits carries the color as its forcedColor.
        public static bool IsCarried(List<WeaponTraitDef> traits, ColorDef color)
        {
            if (color == null)
                return false;
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].forcedColor == color)
                    return true;
            }
            return false;
        }

        // Whether un-staging removed leaves the selected color with no right
        // to stay selected: it was that trait's color, no trait still staged
        // carries it, and no regular palette offers it. remaining is the
        // staged list after the removal. A color a palette also offers is
        // not trait-only, so it survives its trait.
        public static bool RemovalStrandsSelection(
            WeaponTraitDef removed, List<WeaponTraitDef> remaining,
            ColorDef selected, bool selectedOfferedByPalette)
        {
            return selected != null
                && removed.forcedColor == selected
                && !selectedOfferedByPalette
                && !IsCarried(remaining, selected);
        }
    }
}
