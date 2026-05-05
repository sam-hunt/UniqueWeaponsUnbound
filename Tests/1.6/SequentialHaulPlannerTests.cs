using System.Collections.Generic;
using System.Linq;
using UniqueWeaponsUnbound.HaulPlanning;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    /// <summary>
    /// Property tests for SequentialHaulPlanner. These guard the bedrock
    /// fallback path against regressions: every non-null plan must be
    /// VanillaCarryOnly with one CarryTracker pickup per trip — that shape
    /// is what the JobDriver's vanilla haul chain assumes, and a deviation
    /// breaks the no-abort guarantee for the default planner.
    /// </summary>
    public class SequentialHaulPlannerTests
    {
        private static readonly SequentialHaulPlanner Planner = new SequentialHaulPlanner();

        [Fact]
        public void EmptyDemand_ReturnsEmptyVanillaPlan()
        {
            var request = TestHelpers.MakeRequest();
            var plan = Planner.Plan(request);

            Assert.NotNull(plan);
            Assert.True(plan.IsValid);
            Assert.Empty(plan.Trips);
            Assert.Equal(HaulPlanExecutionStrategy.VanillaCarryOnly, plan.ExecutionStrategy);
        }

        [Fact]
        public void SingleSourceCoversDemand_OneTripOnePickup()
        {
            var steel = TestHelpers.MakeDef("TestSteel");
            var request = TestHelpers.MakeRequest(
                demand: new Dictionary<ThingDef, int> { [steel] = 50 },
                pool: new Dictionary<ThingDef, List<HaulCandidate>>
                {
                    [steel] = new List<HaulCandidate>
                    {
                        TestHelpers.MakeCandidate(steel, new IntVec3(5, 0, 5), 100, 1f),
                    },
                });

            var plan = Planner.Plan(request);

            Assert.NotNull(plan);
            Assert.Single(plan.Trips);
            Assert.Single(plan.Trips[0].Pickups);
            Assert.Equal(50, plan.Trips[0].Pickups[0].Count);
        }

        [Fact]
        public void MultipleSources_OneTripPerSource()
        {
            var steel = TestHelpers.MakeDef("TestSteel");
            var request = TestHelpers.MakeRequest(
                demand: new Dictionary<ThingDef, int> { [steel] = 80 },
                pool: new Dictionary<ThingDef, List<HaulCandidate>>
                {
                    [steel] = new List<HaulCandidate>
                    {
                        TestHelpers.MakeCandidate(steel, new IntVec3(5, 0, 5), 30, 1f),
                        TestHelpers.MakeCandidate(steel, new IntVec3(8, 0, 5), 30, 1f),
                        TestHelpers.MakeCandidate(steel, new IntVec3(11, 0, 5), 30, 1f),
                    },
                });

            var plan = Planner.Plan(request);

            Assert.NotNull(plan);
            Assert.Equal(3, plan.Trips.Count);
            Assert.All(plan.Trips, trip => Assert.Single(trip.Pickups));
            Assert.Equal(80, TestHelpers.AllPickups(plan).Sum(p => p.Count));
        }

        [Fact]
        public void DemandExceedsPool_ReturnsNull()
        {
            var steel = TestHelpers.MakeDef("TestSteel");
            var request = TestHelpers.MakeRequest(
                demand: new Dictionary<ThingDef, int> { [steel] = 200 },
                pool: new Dictionary<ThingDef, List<HaulCandidate>>
                {
                    [steel] = new List<HaulCandidate>
                    {
                        TestHelpers.MakeCandidate(steel, new IntVec3(5, 0, 5), 50, 1f),
                    },
                });

            var plan = Planner.Plan(request);
            Assert.Null(plan);
        }

        [Fact]
        public void AllPickupsAreCarryTrackerDestination()
        {
            var steel = TestHelpers.MakeDef("TestSteel");
            var components = TestHelpers.MakeDef("TestComponents");
            var request = TestHelpers.MakeRequest(
                demand: new Dictionary<ThingDef, int>
                {
                    [steel] = 50,
                    [components] = 10,
                },
                pool: new Dictionary<ThingDef, List<HaulCandidate>>
                {
                    [steel] = new List<HaulCandidate>
                    {
                        TestHelpers.MakeCandidate(steel, new IntVec3(5, 0, 5), 100, 1f),
                    },
                    [components] = new List<HaulCandidate>
                    {
                        TestHelpers.MakeCandidate(components, new IntVec3(7, 0, 5), 20, 0.6f),
                    },
                });

            var plan = Planner.Plan(request);

            Assert.NotNull(plan);
            Assert.All(TestHelpers.AllPickups(plan),
                p => Assert.Equal(PickupDestination.CarryTracker, p.Destination));
        }

        [Fact]
        public void TotalsExactlyMatchDemand()
        {
            var steel = TestHelpers.MakeDef("TestSteel");
            var plasteel = TestHelpers.MakeDef("TestPlasteel");
            var request = TestHelpers.MakeRequest(
                demand: new Dictionary<ThingDef, int>
                {
                    [steel] = 73,
                    [plasteel] = 41,
                },
                pool: new Dictionary<ThingDef, List<HaulCandidate>>
                {
                    [steel] = new List<HaulCandidate>
                    {
                        TestHelpers.MakeCandidate(steel, new IntVec3(5, 0, 5), 50, 1f),
                        TestHelpers.MakeCandidate(steel, new IntVec3(8, 0, 5), 50, 1f),
                    },
                    [plasteel] = new List<HaulCandidate>
                    {
                        TestHelpers.MakeCandidate(plasteel, new IntVec3(12, 0, 5), 100, 3f),
                    },
                });

            var plan = Planner.Plan(request);

            Assert.NotNull(plan);
            var totals = TestHelpers.TotalsByDef(plan);
            Assert.Equal(73, totals[steel]);
            Assert.Equal(41, totals[plasteel]);
        }

        [Fact]
        public void NearestSourceUsedFirst()
        {
            var steel = TestHelpers.MakeDef("TestSteel");
            var pawnPos = new IntVec3(0, 0, 0);
            // Two stacks; the second is closer to the pawn. Sequential sorts
            // by squared horizontal distance from the pawn (matches the
            // pre-refactor inline implementation), so the closer stack
            // becomes trip #1.
            var farStack = TestHelpers.MakeCandidate(steel, new IntVec3(50, 0, 50), 30, 1f);
            var nearStack = TestHelpers.MakeCandidate(steel, new IntVec3(2, 0, 2), 30, 1f);
            var request = TestHelpers.MakeRequest(
                pawnPos: pawnPos,
                demand: new Dictionary<ThingDef, int> { [steel] = 60 },
                pool: new Dictionary<ThingDef, List<HaulCandidate>>
                {
                    [steel] = new List<HaulCandidate> { farStack, nearStack },
                });

            var plan = Planner.Plan(request);

            Assert.NotNull(plan);
            Assert.Equal(2, plan.Trips.Count);
            Assert.Same(nearStack.Thing, plan.Trips[0].Pickups[0].Thing);
            Assert.Same(farStack.Thing, plan.Trips[1].Pickups[0].Thing);
        }

        [Fact]
        public void ExecutionStrategyAlwaysVanillaCarryOnly()
        {
            // Across every produced plan, the strategy must be VanillaCarryOnly —
            // the JobDriver's vanilla haul chain is what handles Sequential's
            // unconstrained-count pickups (volume splits via vanilla
            // putRemainderInQueue), and any other strategy routes through
            // the hybrid path and breaks the no-abort guarantee.
            var steel = TestHelpers.MakeDef("TestSteel");
            var emptyRequest = TestHelpers.MakeRequest();
            var fullRequest = TestHelpers.MakeRequest(
                demand: new Dictionary<ThingDef, int> { [steel] = 50 },
                pool: new Dictionary<ThingDef, List<HaulCandidate>>
                {
                    [steel] = new List<HaulCandidate>
                    {
                        TestHelpers.MakeCandidate(steel, new IntVec3(5, 0, 5), 100, 1f),
                    },
                });

            Assert.Equal(HaulPlanExecutionStrategy.VanillaCarryOnly,
                Planner.Plan(emptyRequest).ExecutionStrategy);
            Assert.Equal(HaulPlanExecutionStrategy.VanillaCarryOnly,
                Planner.Plan(fullRequest).ExecutionStrategy);
        }
    }
}
