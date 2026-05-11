using System.Reflection;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Optional integration with Alpha Armoury (packageId sarg.alphaarmoury).
    /// Alpha Armoury's <c>WeaponKit</c> item stores a single <see cref="WeaponTraitDef"/>
    /// in a public <c>trait</c> field; using the kit applies that trait to a compatible
    /// unique weapon. For progression-mode trait visibility we treat kits as
    /// player-discoverable sources alongside actual unique weapons.
    ///
    /// All access goes through reflection so this mod compiles and runs without
    /// Alpha Armoury installed. The sibling kit defs (Converter / Remover / TabulaRasa)
    /// don't carry a trait and are intentionally ignored — only the <c>WeaponKit</c>
    /// class is recognised here.
    /// </summary>
    internal static class AlphaArmouryIntegration
    {
        private static readonly System.Type WeaponKitType =
            GenTypes.GetTypeInAnyAssembly("AlphaArmoury.WeaponKit");

        private static readonly FieldInfo TraitField =
            WeaponKitType?.GetField("trait",
                BindingFlags.Public | BindingFlags.Instance);

        public static bool Available => WeaponKitType != null && TraitField != null;

        /// <summary>
        /// Returns true and emits the stored trait if <paramref name="thing"/> is an
        /// Alpha Armoury weapon kit carrying a non-null trait. Returns false for any
        /// non-kit thing, kits with a null trait, or when Alpha Armoury isn't loaded.
        /// </summary>
        public static bool TryGetKitTrait(Thing thing, out WeaponTraitDef trait)
        {
            trait = null;
            if (!Available || thing == null)
                return false;
            if (!WeaponKitType.IsInstanceOfType(thing))
                return false;
            trait = TraitField.GetValue(thing) as WeaponTraitDef;
            return trait != null;
        }
    }
}
