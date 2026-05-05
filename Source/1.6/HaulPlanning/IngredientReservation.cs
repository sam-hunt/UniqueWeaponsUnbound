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

            // The customization JobDriver owns the parallel destination/trip-
            // boundary lists, populated below alongside job.targetQueueA. If
            // the running driver isn't ours something is badly wrong upstream.
            var driver = pawn.jobs?.curDriver as JobDriver_CustomizeWeapon;
            if (driver == null)
            {
                Log.Error("[Unique Weapons Unbound] No customize-weapon driver "
                    + "active during ingredient reservation.");
                return false;
            }

            HaulPlannerKind kind = UWU_Mod.Settings.haulPlannerKind;
            IntVec3 workbenchPos = job.GetTarget(TargetIndex.C).Cell;

            HaulPlan plan = AttemptPlan(pawn, demand, workbenchPos, kind);

            // Silent fallback to Sequential when the configured planner can't
            // satisfy demand. Common cause: Sweep's pool cap (6 candidates
            // per def) excludes stacks Sequential's no-cap pool would include.
            // Sequential is also the bedrock fallback for stub planners
            // (Optimal) that throw NotImplementedException — AttemptPlan
            // returns null in that case and we retry here.
            if (plan == null && kind != HaulPlannerKind.Sequential)
                plan = AttemptPlan(pawn, demand, workbenchPos, HaulPlannerKind.Sequential);

            if (plan == null || !plan.IsValid)
                return false;

            return CommitPlanAtomic(pawn, job, driver, plan);
        }

        /// <summary>
        /// Builds the pool, request, and runs the configured planner. Returns
        /// null on failure (planner returned null, or stub planner threw
        /// NotImplementedException).
        /// </summary>
        private static HaulPlan AttemptPlan(
            Pawn pawn, Dictionary<ThingDef, int> demand, IntVec3 workbenchPos, HaulPlannerKind kind)
        {
            IHaulPlanner planner = HaulPlannerFactory.Get(kind);

            Dictionary<ThingDef, List<HaulCandidate>> pool = BuildHaulPool(
                pawn, demand, planner.CandidatePoolMultiplier, planner.CandidatePoolCap);

            var request = new HaulPlanRequest
            {
                PawnPosition = pawn.Position,
                WorkbenchPosition = workbenchPos,
                CapacityKg = MassUtility.Capacity(pawn),
                CurrentEncumbranceKg = MassUtility.GearMass(pawn),
                Demand = demand,
                Pool = pool,
            };

            try
            {
                return planner.Plan(request);
            }
            catch (System.NotImplementedException)
            {
                Log.Warning($"[Unique Weapons Unbound] Selected haul planner "
                    + $"({kind}) is not implemented; falling back to Sequential.");
                return null;
            }
        }

        /// <summary>
        /// Reserves every unique Thing in the plan atomically and commits the
        /// plan to the job's haul queues plus the driver's parallel destination
        /// and trip-boundary lists. Same Thing may appear in multiple pickups
        /// (split across CarryTracker + Inventory or across trips); we reserve
        /// it once, and the driver re-reserves before subsequent pickups since
        /// vanilla's Toils_Haul.StartCarryThing auto-releases when the per-
        /// pickup count is satisfied.
        /// </summary>
        private static bool CommitPlanAtomic(
            Pawn pawn, Job job, JobDriver_CustomizeWeapon driver, HaulPlan plan)
        {
            var reservedThings = new HashSet<Thing>();
            var queueA = new List<LocalTargetInfo>();
            var countQueue = new List<int>();
            var destinations = new List<int>();
            var lastInTripFlags = new List<bool>();

            foreach (HaulTrip trip in plan.Trips)
            {
                if (trip?.Pickups == null || trip.Pickups.Count == 0)
                    continue;

                int lastValidIdx = -1;
                for (int i = trip.Pickups.Count - 1; i >= 0; i--)
                {
                    HaulPickup p = trip.Pickups[i];
                    if (p.Thing != null && p.Count > 0) { lastValidIdx = i; break; }
                }
                if (lastValidIdx < 0)
                    continue;

                for (int i = 0; i < trip.Pickups.Count; i++)
                {
                    HaulPickup pickup = trip.Pickups[i];
                    if (pickup.Thing == null || pickup.Count <= 0)
                        continue;

                    if (reservedThings.Add(pickup.Thing))
                    {
                        if (!pawn.Reserve(pickup.Thing, job, 1, -1, null, errorOnFailed: false))
                        {
                            foreach (Thing t in reservedThings)
                                pawn.Map.reservationManager.Release(t, pawn, job);
                            return false;
                        }
                    }

                    queueA.Add(pickup.Thing);
                    countQueue.Add(pickup.Count);
                    destinations.Add((int)pickup.Destination);
                    lastInTripFlags.Add(i == lastValidIdx);
                }
            }

            job.targetQueueA = queueA;
            job.countQueue = countQueue;
            driver.SetHaulPickupMetadata(plan.ExecutionStrategy, destinations, lastInTripFlags);
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
