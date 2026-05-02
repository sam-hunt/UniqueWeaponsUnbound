using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Mutates a weapon Thing in place: adds/removes traits (with their ability
    /// wiring side-effects), sets cosmetic properties (name, color, texture),
    /// and spawns refunded resources. Def conversion (base↔unique) lives in
    /// <see cref="WeaponDefConversion"/>; ingredient gathering and reservation
    /// for a customization job lives in
    /// <see cref="HaulPlanning.IngredientReservation"/>.
    /// </summary>
    public static class WeaponModificationUtility
    {
        // Reflection fields for CompUniqueWeapon private members
        internal static readonly FieldInfo CompNameField = typeof(CompUniqueWeapon)
            .GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static readonly FieldInfo CompColorField = typeof(CompUniqueWeapon)
            .GetField("color", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static readonly FieldInfo IgnoreAccuracyField = typeof(CompUniqueWeapon)
            .GetField("ignoreAccuracyMaluses", BindingFlags.NonPublic | BindingFlags.Instance);

        // CompEquippableAbility caches its constructed Ability in this private field
        // and ScribeDeeps it across save/load. CompUniqueWeapon.Setup() never clears
        // it, so removing an ability-granting trait leaves the equipping pawn with
        // the old gizmo. We null it ourselves in RemoveTrait so the next access via
        // the lazy AbilityForReading getter rebuilds it from the current Props.
        internal static readonly FieldInfo EquippableAbilityField = typeof(CompEquippableAbility)
            .GetField("ability", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void AddTrait(Thing weapon, WeaponTraitDef trait)
        {
            CompUniqueWeapon comp = weapon.TryGetComp<CompUniqueWeapon>();
            if (comp == null)
            {
                Log.Error("[Unique Weapons Unbound] AddTrait: weapon has no CompUniqueWeapon.");
                return;
            }

            comp.AddTrait(trait);

            // Mirror RemoveTrait: invalidate the cached ignoreAccuracyMaluses so a
            // newly-added accuracy-malus-immunity trait actually takes effect on the
            // next shot. Vanilla Setup() never resets this cache, so without this
            // the weapon's accuracy behaviour reflects the trait list at first read
            // until the comp is recreated (def conversion or save/load).
            if (IgnoreAccuracyField != null)
                IgnoreAccuracyField.SetValue(comp, null);

            SetupAndPreserveCharges(comp, weapon);
        }

        public static void RemoveTrait(Thing weapon, WeaponTraitDef trait)
        {
            CompUniqueWeapon comp = weapon.TryGetComp<CompUniqueWeapon>();
            if (comp == null)
            {
                Log.Error("[Unique Weapons Unbound] RemoveTrait: weapon has no CompUniqueWeapon.");
                return;
            }

            comp.TraitsListForReading.Remove(trait);

            // Invalidate the cached ignoreAccuracyMaluses so it recalculates on next access
            if (IgnoreAccuracyField != null)
                IgnoreAccuracyField.SetValue(comp, null);

            // Vanilla CompUniqueWeapon.Setup() only assigns props for traits that
            // have abilityProps; it never clears them when an ability-granting trait
            // is removed, and never resets the cached Ability instance. Without this
            // scrub, an equipped pawn keeps the removed trait's gizmo (e.g. the
            // grenade launcher's launch+reload command) the next time the weapon is
            // re-equipped — even if the trait was swapped for one with different
            // abilityProps, because the lazy AbilityForReading getter only rebuilds
            // when its cache is null.
            if (trait.abilityProps != null)
                ResetEquippableAbilityState(weapon);

            SetupAndPreserveCharges(comp, weapon);
        }

        /// <summary>
        /// Public entry for the JobDriver's finalize toil. Behaviour-equivalent to
        /// <c>CompUniqueWeapon.Setup(false)</c> but preserves the equipped ability's
        /// remaining charges, since vanilla Setup unconditionally refills them via
        /// <c>Notify_PropsChanged</c>. Needed for cosmetics-only customizations
        /// (rename / recolour / texture) that never enter Add/RemoveTrait but still
        /// hit the finalize Setup, which would otherwise hand the player a free
        /// reload of an unchanged ability trait every time they confirmed the dialog.
        /// </summary>
        public static void RewireUniqueWeaponComps(Thing weapon)
        {
            CompUniqueWeapon comp = weapon.TryGetComp<CompUniqueWeapon>();
            if (comp == null)
                return;
            SetupAndPreserveCharges(comp, weapon);
        }

        /// <summary>
        /// Wrapper around <c>CompUniqueWeapon.Setup(false)</c> that preserves the
        /// equipped ability's <c>RemainingCharges</c> across the call. Vanilla Setup
        /// walks the trait list and, for every ability trait, calls
        /// <c>CompEquippableAbilityReloadable.Notify_PropsChanged()</c>, which forces
        /// <c>RemainingCharges = MaxCharges</c>. That's correct on PostPostMake and
        /// save load, but it also fires on every customization op — turning every
        /// dialog confirm into a free reload of any unchanged ability trait
        /// (skipping the steel/chemfuel/bioferrite Reload job). We snapshot the
        /// charges before, then restore them only if the same Ability instance
        /// survived; a different instance means the ability trait was added or
        /// swapped this op (player paid for the new trait), in which case fresh
        /// max charges is the right outcome.
        ///
        /// No-op for cooldown-only abilities (e.g. EMPPulser): they leave
        /// <c>maxCharges = 0</c>, so <c>UsesCharges</c> is false, the cooldown
        /// lives on <c>Ability.cooldownEndTick</c> (which Notify_PropsChanged
        /// doesn't touch), and the snapshot/restore round-trips zero.
        /// </summary>
        private static void SetupAndPreserveCharges(CompUniqueWeapon comp, Thing weapon)
        {
            CompEquippableAbilityReloadable abilityComp =
                weapon.TryGetComp<CompEquippableAbilityReloadable>();

            Ability priorAbility = null;
            int priorCharges = 0;
            if (abilityComp != null && EquippableAbilityField != null)
            {
                priorAbility = (Ability)EquippableAbilityField.GetValue(abilityComp);
                if (priorAbility != null)
                    priorCharges = priorAbility.RemainingCharges;
            }

            comp.Setup(false);

            if (priorAbility != null && abilityComp != null && EquippableAbilityField != null)
            {
                Ability currentAbility = (Ability)EquippableAbilityField.GetValue(abilityComp);
                if (ReferenceEquals(currentAbility, priorAbility))
                    abilityComp.RemainingCharges = priorCharges;
            }
        }

        /// <summary>
        /// Restores CompEquippableAbilityReloadable to its def-default state: drops
        /// the cached Ability and points <c>props</c> back at the empty stub from
        /// <c>weapon.def.comps</c>. A subsequent AddTrait → CompUniqueWeapon.Setup()
        /// will re-assign <c>props</c> from the new trait's abilityProps and the
        /// lazy getter will construct a fresh Ability from it.
        /// </summary>
        private static void ResetEquippableAbilityState(Thing weapon)
        {
            CompEquippableAbilityReloadable abilityComp =
                weapon.TryGetComp<CompEquippableAbilityReloadable>();
            if (abilityComp == null)
                return;

            EquippableAbilityField?.SetValue(abilityComp, null);

            foreach (CompProperties cp in weapon.def.comps)
            {
                if (cp is CompProperties_EquippableAbilityReloadable defaultProps)
                {
                    abilityComp.props = defaultProps;
                    break;
                }
            }
        }

        public static void SetName(Thing weapon, string name)
        {
            CompUniqueWeapon comp = weapon.TryGetComp<CompUniqueWeapon>();
            if (comp != null && CompNameField != null)
                CompNameField.SetValue(comp, name);

            // Keep CompArt.Title in sync for inspection dialogs
            CompArt artComp = weapon.TryGetComp<CompArt>();
            if (artComp != null && !string.IsNullOrEmpty(name))
                artComp.Title = name;
        }

        public static void SetColor(Thing weapon, ColorDef color)
        {
            CompUniqueWeapon comp = weapon.TryGetComp<CompUniqueWeapon>();
            if (comp != null && CompColorField != null)
            {
                CompColorField.SetValue(comp, color);
                weapon.Notify_ColorChanged();
            }
        }

        public static void SetTextureIndex(Thing weapon, int index)
        {
            weapon.overrideGraphicIndex = index;
        }

        /// <summary>
        /// Spawns resources near a position (e.g. the workbench).
        /// Used to refund resources when removing traits.
        /// </summary>
        public static void SpawnResourcesNear(
            Map map, IntVec3 center, List<ThingDefCountClass> resources)
        {
            if (resources == null || resources.Count == 0)
                return;

            foreach (ThingDefCountClass resource in resources)
            {
                if (resource.count <= 0)
                    continue;

                Thing thing = ThingMaker.MakeThing(resource.thingDef);
                thing.stackCount = resource.count;
                GenPlace.TryPlaceThing(thing, center, map, ThingPlaceMode.Near);
            }
        }

    }
}
