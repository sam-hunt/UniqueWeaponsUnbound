using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

// Ideology types — only available when DLC is active, accessed via reflection
// RimWorld.Precept_Relic : Precept_ThingStyle
//   private Thing generatedRelic — the Thing instance that IS the relic

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Encapsulates all weapon mutation logic: trait addition/removal, name/color/texture
    /// changes, def conversion (base↔unique), and resource consumption.
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

        // Ideology DLC: Precept_Relic.generatedRelic (private Thing)
        // Resolved once at startup; null if Ideology is not installed.
        private static readonly FieldInfo GeneratedRelicField =
            GenTypes.GetTypeInAnyAssembly("RimWorld.Precept_Relic")
                ?.GetField("generatedRelic", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void AddTrait(Thing weapon, WeaponTraitDef trait)
        {
            CompUniqueWeapon comp = weapon.TryGetComp<CompUniqueWeapon>();
            if (comp == null)
            {
                Log.Error("[Unique Weapons Unbound] AddTrait: weapon has no CompUniqueWeapon.");
                return;
            }

            comp.AddTrait(trait);
            comp.Setup(false);
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

            comp.Setup(false);
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
                if (CompNameField != null)
                    CompNameField.SetValue(uniqueComp, null);
                if (CompColorField != null)
                    CompColorField.SetValue(uniqueComp, null);
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

        /// <summary>
        /// Consumes resources from stacks near a position (within radius cells).
        /// Used by the work loop to consume from hauled stacks at the workbench.
        /// Returns false if insufficient resources found nearby.
        /// </summary>
        public static bool ConsumeResourcesNear(
            Map map, IntVec3 center, List<ThingDefCountClass> costs, int radius = 3)
        {
            if (costs == null || costs.Count == 0)
                return true;

            foreach (ThingDefCountClass cost in costs)
            {
                int remaining = cost.count;

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!cell.InBounds(map) || remaining <= 0)
                        continue;

                    List<Thing> things = cell.GetThingList(map);
                    for (int i = things.Count - 1; i >= 0 && remaining > 0; i--)
                    {
                        Thing stack = things[i];
                        if (stack.def == cost.thingDef && stack.Spawned)
                        {
                            int take = UnityEngine.Mathf.Min(remaining, stack.stackCount);
                            remaining -= take;

                            if (take >= stack.stackCount)
                                stack.Destroy();
                            else
                                stack.SplitOff(take).Destroy();
                        }
                    }
                }

                if (remaining > 0)
                {
                    Log.Warning($"[Unique Weapons Unbound] Could not consume all " +
                        $"{cost.thingDef.LabelCap} near {center}: needed {cost.count}, short by {remaining}.");
                    return false;
                }
            }

            return true;
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

        /// <summary>
        /// Determines whether a pawn can use the given thing as an ingredient.
        /// Checks: spawned, not forbidden to the pawn (considers allowed area),
        /// and reservable by the pawn (not reserved by another pawn).
        /// This is the single source of truth for ingredient accessibility — used by
        /// both the dialog and the job driver.
        /// </summary>
        public static bool CanPawnUseIngredient(Thing thing, Pawn pawn)
        {
            return thing.Spawned && !thing.IsForbidden(pawn) && pawn.CanReserve(thing);
        }

        /// <summary>
        /// Counts available units of a material on the map that the given pawn can access.
        /// </summary>
        public static int CountAvailable(Map map, ThingDef thingDef, Pawn pawn)
        {
            int count = 0;
            foreach (Thing stack in map.listerThings.ThingsOfDef(thingDef))
            {
                if (CanPawnUseIngredient(stack, pawn))
                    count += stack.stackCount;
            }
            return count;
        }
    }
}
