using System;
using System.Collections.Generic;
using UniqueWeaponsUnbound.HaulPlanning.Internal;
using Verse;

namespace UniqueWeaponsUnbound.HaulPlanning
{
    // Phase 5 — emission: expand the winning partition's node masks back to
    // member-stack pickups, designate the carry tracker per trip, sequence
    // each trip with the shared Held-Karp solver, and emit the final plan.
    public partial class ThoroughHaulPlanner
    {
        private struct TripTake
        {
            public Thing Thing;
            public IntVec3 Position;
            public int Count;
            public float UnitMass;
            public int CtVol;
        }

        private static HaulPlan Emit(
            List<PlannedNode> nodes, List<int> tripMasks, IntVec3 wb)
        {
            var trips = new List<HaulTrip>(tripMasks.Count);
            foreach (int mask in tripMasks)
            {
                // Merge same-stack slices: virtual copies of one node landing
                // in the same trip re-join into a single pickup.
                var takes = new List<TripTake>();
                var indexOf = new Dictionary<Thing, int>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    if ((mask & (1 << i)) == 0)
                        continue;
                    PlannedNode pn = nodes[i];
                    foreach (MemberTake t in pn.Takes)
                    {
                        if (indexOf.TryGetValue(t.Thing, out int at))
                        {
                            TripTake merged = takes[at];
                            merged.Count += t.Count;
                            takes[at] = merged;
                        }
                        else
                        {
                            indexOf[t.Thing] = takes.Count;
                            takes.Add(new TripTake
                            {
                                Thing = t.Thing,
                                Position = t.Position,
                                Count = t.Count,
                                UnitMass = t.UnitMass,
                                CtVol = pn.Source.CtVol,
                            });
                        }
                    }
                }

                // Carry-tracker designation = argmax bypassed mass — exactly
                // what feasible() assumed (merging same-stack chunks only
                // increases the realizable bypass, never decreases it).
                // First-wins on ties for determinism.
                int ct = -1;
                float bestByp = -1f;
                for (int i = 0; i < takes.Count; i++)
                {
                    if (takes[i].CtVol <= 0)
                        continue;
                    float byp = Math.Min(takes[i].Count, takes[i].CtVol) * takes[i].UnitMass;
                    if (byp > bestByp)
                    {
                        bestByp = byp;
                        ct = i;
                    }
                }

                var pickups = new List<HaulPickup>(takes.Count + 1);
                var positions = new List<IntVec3>(takes.Count + 1);
                for (int i = 0; i < takes.Count; i++)
                {
                    TripTake t = takes[i];
                    if (i == ct)
                    {
                        int ctTake = Math.Min(t.Count, t.CtVol);
                        pickups.Add(new HaulPickup
                        {
                            Thing = t.Thing,
                            Count = ctTake,
                            Destination = PickupDestination.CarryTracker,
                        });
                        positions.Add(t.Position);
                        // Volume overflow rides as Inventory in the same trip
                        // (Sweep's split rule).
                        if (t.Count > ctTake)
                        {
                            pickups.Add(new HaulPickup
                            {
                                Thing = t.Thing,
                                Count = t.Count - ctTake,
                                Destination = PickupDestination.Inventory,
                            });
                            positions.Add(t.Position);
                        }
                    }
                    else
                    {
                        pickups.Add(new HaulPickup
                        {
                            Thing = t.Thing,
                            Count = t.Count,
                            Destination = PickupDestination.Inventory,
                        });
                        positions.Add(t.Position);
                    }
                }

                int[] order = HeldKarp.Order(positions, wb);
                var ordered = new List<HaulPickup>(pickups.Count);
                for (int s = 0; s < order.Length; s++)
                    ordered.Add(pickups[order[s]]);
                trips.Add(new HaulTrip { Pickups = ordered });
            }

            return new HaulPlan
            {
                Trips = trips,
                ExecutionStrategy = HaulPlanExecutionStrategy.UwuCarryInventoryHybrid,
            };
        }
    }
}
