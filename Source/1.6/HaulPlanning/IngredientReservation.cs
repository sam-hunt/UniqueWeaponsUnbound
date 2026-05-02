using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace UniqueWeaponsUnbound.HaulPlanning
{
    /// <summary>
    /// Bridge between the customization dialog and the IHaulPlanner pipeline.
    /// Counts ingredient availability for the dialog's per-row indicators,
    /// builds the candidate pool a planner will draw from, and commits the
    /// resulting plan to the job's target/count queues with reservations
    /// already held.
    ///
    /// Designed to be callable while the dialog's forcePause is active, so
    /// reservations lock in before any other pawn AI runs.
    /// </summary>
    public static class IngredientReservation
    {
        /// <summary>
        /// Counts available units of a material on the map that the given
        /// pawn can access.
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

        /// <summary>
        /// Builds a candidate pool, dispatches to the configured IHaulPlanner,
        /// reserves the chosen stacks against <paramref name="job"/>, and
        /// populates job.targetQueueA / countQueue for the haul phase. Atomic:
        /// if planning fails or any reservation can't be acquired, releases
        /// anything it reserved and returns false without mutating the job's
        /// queues.
        /// </summary>
        public static bool TryReserveIngredientsForJob(
            Pawn pawn, Job job, List<ThingDefCountClass> totalCost)
        {
            if (totalCost == null || totalCost.Count == 0)
                return true;

            // Demand: collapse possible duplicate ThingDef entries by summing.
            var demand = new Dictionary<ThingDef, int>();
            foreach (ThingDefCountClass cost in totalCost)
            {
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                    continue;
                demand.TryGetValue(cost.thingDef, out int existing);
                demand[cost.thingDef] = existing + cost.count;
            }
            if (demand.Count == 0)
                return true;

            // Resolve planner from settings. Stub planners throw on Plan(),
            // which is caught below and surfaced as a planning failure rather
            // than a hard exception escaping into the dialog.
            HaulPlannerKind kind = UWU_Mod.Settings.haulPlannerKind;

            // Low-spare-capacity fallback: Sequential uses Toils_Haul.StartCarryThing,
            // which loads the carry tracker — bound by Pawn_CarryTracker.MaxStackSpaceEver
            // (StatDefOf.CarryingCapacity / VolumePerUnit, capped at stackLimit).
            // That's a *volume* limit, not a mass limit; mass-encumbrance only
            // affects walking speed afterward. Batched planners load into the
            // pawn's inventory, which IS mass-bound via MassUtility checks.
            //
            // When spare mass capacity is low relative to demand, a batched plan
            // can produce dramatically more trips than Sequential, because
            // Sequential gets to over-carry via the carry tracker quirk while
            // batched is forced to make many small inventory trips. Fall back
            // to Sequential when the lower bound on batched trip count exceeds
            // Sequential's lower bound (one trip per def).
            //
            // The "right" long-term fix is a hybrid: batched planners use the
            // carry tracker for the first pickup of each trip and inventory for
            // the rest. See SweepHaulPlanner / OptimalHaulPlanner spec comments.
            if (kind != HaulPlannerKind.Sequential)
            {
                float spareCapacity = MassUtility.Capacity(pawn) - MassUtility.GearMass(pawn);
                float totalDemandMass = 0f;
                foreach (KeyValuePair<ThingDef, int> entry in demand)
                    totalDemandMass += entry.Value * entry.Key.GetStatValueAbstract(StatDefOf.Mass);

                bool overEncumbered = spareCapacity <= 0f;
                bool batchedWouldLose = !overEncumbered
                    && Mathf.CeilToInt(totalDemandMass / spareCapacity) > demand.Count;
                if (overEncumbered || batchedWouldLose)
                    kind = HaulPlannerKind.Sequential;
            }

            IHaulPlanner planner = HaulPlannerFactory.Get(kind);

            Dictionary<ThingDef, List<HaulCandidate>> pool = BuildHaulPool(
                pawn, demand, planner.CandidatePoolMultiplier, planner.CandidatePoolCap);

            IntVec3 workbenchPos = job.GetTarget(TargetIndex.C).Cell;

            var request = new HaulPlanRequest
            {
                PawnPosition = pawn.Position,
                WorkbenchPosition = workbenchPos,
                CapacityKg = MassUtility.Capacity(pawn),
                CurrentEncumbranceKg = MassUtility.GearMass(pawn),
                Demand = demand,
                Pool = pool,
            };

            HaulPlan plan;
            try
            {
                plan = planner.Plan(request);
            }
            catch (System.NotImplementedException)
            {
                // Stub planner selected but not yet implemented. Fail planning
                // cleanly so the dialog footer's catch-all surfaces the
                // standard "couldn't reserve ingredients" path.
                Log.Warning($"[Unique Weapons Unbound] Selected haul planner "
                    + $"({kind}) is not implemented. "
                    + $"Switch to Sequential in mod settings.");
                return false;
            }

            if (plan == null || !plan.IsValid)
                return false;

            // Reserve every chosen stack atomically. CanReserve was already
            // checked when we built the pool, but under forcePause that's
            // still the live state — Reserve should succeed. If it doesn't
            // (defensive), release anything we held and bail.
            var reserved = new List<Thing>();
            var queueA = new List<LocalTargetInfo>();
            var countQueue = new List<int>();

            foreach (HaulTrip trip in plan.Trips)
            {
                if (trip?.Pickups == null)
                    continue;
                foreach (HaulPickup pickup in trip.Pickups)
                {
                    if (pickup.Thing == null || pickup.Count <= 0)
                        continue;
                    if (!pawn.Reserve(pickup.Thing, job, 1, -1, null, errorOnFailed: false))
                    {
                        foreach (Thing t in reserved)
                            pawn.Map.reservationManager.Release(t, pawn, job);
                        return false;
                    }
                    reserved.Add(pickup.Thing);
                    queueA.Add(pickup.Thing);
                    countQueue.Add(pickup.Count);
                }
            }

            job.targetQueueA = queueA;
            job.countQueue = countQueue;
            return true;
        }

        /// <summary>
        /// Determines whether a pawn can use the given thing as an ingredient.
        /// Checks: spawned, not flagged forbidden by the player faction, not
        /// forbidden to the pawn (allowed-area), reservable by the pawn, and
        /// reachable. The faction-level forbidden check is explicit because
        /// IsForbidden(pawn) short-circuits to false for drafted/slave/host-faction
        /// pawns via CaresAboutForbidden, which would otherwise allow forbidden
        /// stacks to be hauled. Customization is a direct player order, so the
        /// player's red-X must always be respected regardless of pawn state.
        /// </summary>
        private static bool CanPawnUseIngredient(Thing thing, Pawn pawn)
        {
            return thing.Spawned
                && !thing.IsForbidden(Faction.OfPlayer)
                && !thing.IsForbidden(pawn)
                && pawn.CanReserve(thing)
                && pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly);
        }

        /// <summary>
        /// Gathers candidate stacks per demanded def for the planner. Each
        /// candidate has already passed CanPawnUseIngredient. The pool is
        /// sized to <paramref name="multiplier"/> times demand, capped per def
        /// at <paramref name="capPerDef"/>; richer pools give planners more
        /// sourcing flexibility at the cost of build time.
        ///
        /// Sort key for "which stacks to keep when capping" is squared
        /// distance from the pawn, matching the existing one-stack-per-trip
        /// behavior. Planners that prefer a workbench-anchored sort can
        /// re-sort the pool internally without losing candidates.
        /// </summary>
        private static Dictionary<ThingDef, List<HaulCandidate>> BuildHaulPool(
            Pawn pawn, Dictionary<ThingDef, int> demand, float multiplier, int capPerDef)
        {
            var pool = new Dictionary<ThingDef, List<HaulCandidate>>();
            IntVec3 origin = pawn.Position;

            foreach (KeyValuePair<ThingDef, int> entry in demand)
            {
                ThingDef def = entry.Key;
                int needed = entry.Value;
                int targetCount = Mathf.CeilToInt(needed * multiplier);

                List<Thing> stacks = pawn.Map.listerThings.ThingsOfDef(def)
                    .Where(t => CanPawnUseIngredient(t, pawn))
                    .OrderBy(t => (t.Position - origin).LengthHorizontalSquared)
                    .ToList();

                var candidates = new List<HaulCandidate>(stacks.Count);
                int cumulative = 0;
                foreach (Thing stack in stacks)
                {
                    if (candidates.Count >= capPerDef)
                        break;
                    candidates.Add(new HaulCandidate
                    {
                        Thing = stack,
                        Position = stack.Position,
                        AvailableCount = stack.stackCount,
                        MassPerUnit = stack.GetStatValue(StatDefOf.Mass),
                    });
                    cumulative += stack.stackCount;
                    // Stop once we have enough cumulative count to cover the
                    // pool target. The last candidate may overshoot; that's
                    // fine — the planner can partial-take it.
                    if (cumulative >= targetCount)
                        break;
                }

                pool[def] = candidates;
            }

            return pool;
        }
    }
}
