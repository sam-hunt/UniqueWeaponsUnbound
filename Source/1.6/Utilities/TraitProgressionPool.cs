using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace UniqueWeaponsUnbound
{
    /// <summary>
    /// Snapshot of which weapon traits exist on player-discoverable unique weapons,
    /// classified by whether any source weapon is held by a non-hostile actor.
    ///
    /// Sources counted:
    ///   - Ground weapons on any loaded map, on tiles not currently fogged.
    ///   - Weapons equipped/inventoried/carried by spawned pawns on those maps.
    ///   - Weapons equipped/inventoried/carried by pawns in player-faction caravans.
    /// Weapons inside non-spawned ThingHolders (caskets, ancient containers) are
    /// excluded automatically because they don't appear in <see cref="ListerThings"/>.
    ///
    /// Built once at dialog construction; treat as immutable thereafter.
    /// </summary>
    public sealed class TraitProgressionPool
    {
        // Per-trait counts. nonHostile = sources NOT held by a hostile pawn
        // (ground items, friendly/neutral pawns, dead pawns, player caravans).
        private readonly Dictionary<WeaponTraitDef, int> nonHostileCount;
        private readonly Dictionary<WeaponTraitDef, int> hostileCount;

        private TraitProgressionPool(
            Dictionary<WeaponTraitDef, int> nonHostile,
            Dictionary<WeaponTraitDef, int> hostile)
        {
            nonHostileCount = nonHostile;
            hostileCount = hostile;
        }

        /// <summary>
        /// True if at least one player-discoverable weapon (anywhere) carries this trait.
        /// Traits failing this should be hidden entirely from the customization dialog.
        /// </summary>
        public bool IsVisible(WeaponTraitDef trait)
            => nonHostileCount.ContainsKey(trait) || hostileCount.ContainsKey(trait);

        /// <summary>
        /// True if at least one source for this trait is NOT held by a hostile pawn.
        /// Traits visible but failing this should appear in the dialog as disabled
        /// with a "only on hostile" rejection reason.
        /// </summary>
        public bool HasNonHostileSource(WeaponTraitDef trait)
            => nonHostileCount.TryGetValue(trait, out int n) && n > 0;

        /// <summary>
        /// True when removing the given weapon's contribution would leave no other
        /// non-hostile source for this trait. Used to flag "last copy" traits in
        /// the LHS chip list with a yellow warning.
        /// </summary>
        public bool IsLastNonHostileSource(WeaponTraitDef trait, IList<WeaponTraitDef> weaponTraits)
        {
            if (!nonHostileCount.TryGetValue(trait, out int total))
                return false;
            int contribution = (weaponTraits != null && weaponTraits.Contains(trait)) ? 1 : 0;
            return total - contribution <= 0;
        }

        /// <summary>
        /// Builds the pool by scanning all loaded maps and player-faction caravans.
        /// Errors during scan are logged and the partially-built pool is returned.
        /// </summary>
        public static TraitProgressionPool Build()
        {
            var nonHostile = new Dictionary<WeaponTraitDef, int>();
            var hostile = new Dictionary<WeaponTraitDef, int>();

            List<Map> maps = Find.Maps;
            if (maps != null)
            {
                foreach (Map map in maps)
                {
                    try { ScanMap(map, nonHostile, hostile); }
                    catch (Exception ex)
                    {
                        Log.Error("[Unique Weapons Unbound] Progression scan failed for map "
                            + map + ": " + ex);
                    }
                }
            }

            WorldObjectsHolder worldObjects = Find.WorldObjects;
            if (worldObjects != null)
            {
                foreach (Caravan caravan in worldObjects.Caravans)
                {
                    if (!caravan.IsPlayerControlled)
                        continue;
                    foreach (Pawn p in caravan.PawnsListForReading)
                    {
                        try { ScanPawn(p, isHostile: false, nonHostile, hostile); }
                        catch (Exception ex)
                        {
                            Log.Error("[Unique Weapons Unbound] Progression scan failed for caravan pawn "
                                + p + ": " + ex);
                        }
                    }
                }
            }

            return new TraitProgressionPool(nonHostile, hostile);
        }

        private static void ScanMap(
            Map map,
            Dictionary<WeaponTraitDef, int> nonHostile,
            Dictionary<WeaponTraitDef, int> hostile)
        {
            if (map?.listerThings == null)
                return;

            FogGrid fog = map.fogGrid;

            // Ground/loose unique weapons. ListerThings excludes things held in
            // non-spawned ThingOwners (pawn equipment/inventory, casket contents),
            // so this naturally implements the "not in unknown container" rule.
            foreach (Thing t in map.listerThings.AllThings)
            {
                if (t is Pawn)
                    continue;
                if (!WeaponRegistry.IsUniqueWeapon(t.def))
                    continue;
                IntVec3 pos = t.PositionHeld;
                if (fog != null && fog.IsFogged(pos))
                    continue;
                AddWeaponTraits(t, nonHostile);
            }

            // Pawn-held weapons. A pawn standing in a fogged cell shouldn't reveal
            // its kit, so the fog check applies symmetrically here.
            if (map.mapPawns != null)
            {
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    if (fog != null && fog.IsFogged(p.Position))
                        continue;
                    bool isHostile = p.HostileTo(Faction.OfPlayer);
                    ScanPawn(p, isHostile, nonHostile, hostile);
                }
            }
        }

        private static void ScanPawn(
            Pawn p, bool isHostile,
            Dictionary<WeaponTraitDef, int> nonHostile,
            Dictionary<WeaponTraitDef, int> hostile)
        {
            Dictionary<WeaponTraitDef, int> bucket = isHostile ? hostile : nonHostile;

            ThingWithComps eq = p.equipment?.Primary;
            if (eq != null && WeaponRegistry.IsUniqueWeapon(eq.def))
                AddWeaponTraits(eq, bucket);

            if (p.inventory?.innerContainer != null)
            {
                foreach (Thing item in p.inventory.innerContainer)
                {
                    if (WeaponRegistry.IsUniqueWeapon(item.def))
                        AddWeaponTraits(item, bucket);
                }
            }

            Thing carried = p.carryTracker?.CarriedThing;
            if (carried != null && WeaponRegistry.IsUniqueWeapon(carried.def))
                AddWeaponTraits(carried, bucket);
        }

        private static void AddWeaponTraits(Thing weapon, Dictionary<WeaponTraitDef, int> dest)
        {
            CompUniqueWeapon comp = weapon.TryGetComp<CompUniqueWeapon>();
            List<WeaponTraitDef> traits = comp?.TraitsListForReading;
            if (traits == null)
                return;
            for (int i = 0; i < traits.Count; i++)
            {
                WeaponTraitDef trait = traits[i];
                if (trait == null)
                    continue;
                if (dest.TryGetValue(trait, out int n))
                    dest[trait] = n + 1;
                else
                    dest[trait] = 1;
            }
        }
    }
}
