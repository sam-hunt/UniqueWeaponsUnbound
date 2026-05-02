using System.Reflection;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Transforms a weapon Thing into a different ThingDef while preserving
    /// identity-bearing properties: quality, hitpoint percentage, texture
    /// override, and (when applicable) Ideology relic status. Used during
    /// customization at the 0↔1 trait boundary to swap between a weapon's
    /// base def and its unique counterpart.
    /// </summary>
    public static class WeaponDefConversion
    {
        // Ideology DLC: Precept_Relic.generatedRelic (private Thing).
        // Resolved once at startup; null if Ideology is not installed.
        private static readonly FieldInfo GeneratedRelicField =
            GenTypes.GetTypeInAnyAssembly("RimWorld.Precept_Relic")
                ?.GetField("generatedRelic", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Creates a new weapon Thing from targetDef, copying quality and hitpoints
        /// from oldWeapon. If targetDef has CompUniqueWeapon (base→unique conversion),
        /// clears the auto-generated traits/name/color from PostPostMake().
        /// Returns the new weapon. Does NOT destroy oldWeapon (caller's responsibility).
        /// </summary>
        public static Thing ConvertWeaponDef(Thing oldWeapon, ThingDef targetDef)
        {
            Thing newWeapon = ThingMaker.MakeThing(targetDef);

            // Copy quality
            if (oldWeapon.TryGetQuality(out QualityCategory quality))
            {
                CompQuality qualityComp = newWeapon.TryGetComp<CompQuality>();
                qualityComp?.SetQuality(quality, ArtGenerationContext.Colony);
            }

            // Copy hitpoints as a percentage of max
            if (oldWeapon.MaxHitPoints > 0 && newWeapon.MaxHitPoints > 0)
            {
                float hpPct = (float)oldWeapon.HitPoints / oldWeapon.MaxHitPoints;
                newWeapon.HitPoints = (int)(newWeapon.MaxHitPoints * hpPct);
                if (newWeapon.HitPoints < 1)
                    newWeapon.HitPoints = 1;
            }

            // Copy texture index
            newWeapon.overrideGraphicIndex = oldWeapon.overrideGraphicIndex;

            // If converting to unique, clear auto-generated traits/name/color from PostPostMake()
            CompUniqueWeapon uniqueComp = newWeapon.TryGetComp<CompUniqueWeapon>();
            if (uniqueComp != null)
            {
                uniqueComp.TraitsListForReading.Clear();
                if (WeaponModificationUtility.CompNameField != null)
                    WeaponModificationUtility.CompNameField.SetValue(uniqueComp, null);
                if (WeaponModificationUtility.CompColorField != null)
                    WeaponModificationUtility.CompColorField.SetValue(uniqueComp, null);
            }

            return newWeapon;
        }

        /// <summary>
        /// Transfers Ideology relic status from the old weapon to the new weapon.
        /// Must be called BEFORE destroying the old weapon — clears the old weapon's
        /// StyleSourcePrecept so that Thing.Destroy() does not fire Notify_ThingLost,
        /// which would trigger RelicDestroyed events, mood debuffs, and permanently
        /// orphan the relic precept.
        ///
        /// Updates both sides of the bidirectional reference:
        ///   Thing.StyleSourcePrecept → Precept_Relic (via CompStyleable)
        ///   Precept_Relic.generatedRelic → Thing (via reflection)
        ///
        /// No-op if Ideology is not active or the weapon is not a relic.
        /// </summary>
        public static void TransferRelicStatus(Thing oldWeapon, Thing newWeapon)
        {
            if (!ModsConfig.IdeologyActive)
                return;

            Precept_ThingStyle precept = oldWeapon.StyleSourcePrecept;
            if (precept == null)
                return;

            // Clear from old weapon BEFORE it gets destroyed to prevent
            // Precept_Relic.Notify_ThingLost from firing RelicDestroyed/RelicLost events.
            oldWeapon.StyleSourcePrecept = null;

            // Point the new weapon back at the precept.
            newWeapon.StyleSourcePrecept = precept;

            // Transfer the "ever seen by player" flag so the relic remains
            // recognized as player-possessed.
            if (oldWeapon is ThingWithComps oldTwc && newWeapon is ThingWithComps newTwc
                && oldTwc.compStyleable != null && newTwc.compStyleable != null)
            {
                newTwc.compStyleable.everSeenByPlayer = oldTwc.compStyleable.everSeenByPlayer;
            }

            // Update the Precept_Relic's private generatedRelic field to point
            // at the new weapon instance, keeping the precept→thing reference valid.
            if (GeneratedRelicField != null && precept is Precept_Relic)
            {
                GeneratedRelicField.SetValue(precept, newWeapon);
            }
        }
    }
}
