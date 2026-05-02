using System.Collections.Generic;
using Verse;

namespace UniqueWeaponsUnbound.HaulPlanning
{
    /// <summary>
    /// A planner's output: an ordered list of trips. Each trip is a single
    /// round-trip from the workbench, picking up one or more stacks before
    /// returning. Reservation and queue encoding happen downstream.
    /// </summary>
    public class HaulPlan
    {
        public List<HaulTrip> Trips;

        public bool IsValid => Trips != null;
    }

    /// <summary>
    /// One trip: a sequence of pickups in the order the pawn should visit them.
    /// All pickups in a trip happen before the pawn returns to the workbench.
    /// </summary>
    public class HaulTrip
    {
        public List<HaulPickup> Pickups;
    }

    /// <summary>
    /// A single pickup: take Count units from Thing. Count may be less than the
    /// stack's full size when the planner partial-takes a stack.
    /// </summary>
    public struct HaulPickup
    {
        public Thing Thing;
        public int Count;
    }
}
