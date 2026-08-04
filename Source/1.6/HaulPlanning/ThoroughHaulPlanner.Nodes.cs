using System;
using System.Collections.Generic;
using UniqueWeaponsUnbound.HaulPlanning.Internal;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound.HaulPlanning
{
    // Phase 1 — node construction: bucket the grouped candidate pool into
    // per-def sourcing nodes (one per storage group, span-guarded), sorted
    // nearest-first, and index their unique representative positions for the
    // TSP table.
    public partial class ThoroughHaulPlanner
    {
        private sealed class DefNodes
        {
            public int Demand;
            public List<Node> Nodes;
        }

        // One sourcing decision: a def's stacks within one storage group
        // (possibly bisected by the span guard), or a lone stack.
        private sealed class Node
        {
            public int CtVol;
            public List<Member> Members; // sorted nearest-to-workbench
            public int Available;
            public IntVec3 RepPos;       // nearest member's cell
            public int DistWB;
            public int PosIndex;         // into the unique-position table
        }

        private struct Member
        {
            public Thing Thing;
            public IntVec3 Position;
            public int Available;
            public float UnitMass;
        }

        private static List<DefNodes> BuildNodes(HaulPlanRequest request, IntVec3 wb)
        {
            // Sort demanded defs by name so Dictionary iteration order (an
            // implementation detail) can never change the plan.
            var defOrder = new List<ThingDef>(request.Demand.Keys);
            defOrder.Sort((a, b) => string.CompareOrdinal(a.defName, b.defName));

            var result = new List<DefNodes>();
            foreach (ThingDef def in defOrder)
            {
                int needed = request.Demand[def];
                if (needed <= 0)
                    continue;

                if (!request.Pool.TryGetValue(def, out List<HaulCandidate> candidates)
                    || candidates == null
                    || candidates.Count == 0)
                {
                    return null;
                }

                int ctVol = HaulMath.MaxStackSpaceEver(def, request.CapacityKg);

                // Bucket members by GroupId in pool order; negative ids are
                // singleton groups.
                var memberGroups = new List<List<Member>>();
                var groupIndexOf = new Dictionary<int, int>();
                int available = 0;
                foreach (HaulCandidate c in candidates)
                {
                    if (c.Thing == null || c.AvailableCount <= 0)
                        continue;
                    var member = new Member
                    {
                        Thing = c.Thing,
                        Position = c.Position,
                        Available = c.AvailableCount,
                        UnitMass = Mathf.Max(c.MassPerUnit, 0f),
                    };
                    if (c.GroupId < 0)
                    {
                        memberGroups.Add(new List<Member> { member });
                    }
                    else if (groupIndexOf.TryGetValue(c.GroupId, out int at))
                    {
                        memberGroups[at].Add(member);
                    }
                    else
                    {
                        groupIndexOf[c.GroupId] = memberGroups.Count;
                        memberGroups.Add(new List<Member> { member });
                    }
                    available += c.AvailableCount;
                }
                if (available < needed)
                    return null;

                var nodes = new List<Node>();
                foreach (List<Member> members in memberGroups)
                    AddNodesSplitBySpan(members, ctVol, wb, nodes);

                // Nearest-first node order — the canonical-count rule and the
                // cover bias both key off it.
                nodes.Sort((a, b) =>
                {
                    int c = a.DistWB.CompareTo(b.DistWB);
                    if (c != 0) return c;
                    c = a.RepPos.x.CompareTo(b.RepPos.x);
                    if (c != 0) return c;
                    return a.RepPos.z.CompareTo(b.RepPos.z);
                });

                result.Add(new DefNodes { Demand = needed, Nodes = nodes });
            }
            return result;
        }

        // Span guard: a single SlotGroup can sprawl (snaking stockpiles,
        // zones split across rooms), and one representative position would
        // misprice it. Bisect along the wider axis until each part's bounding
        // box fits MaxGroupSpan on both axes. Both halves are always
        // non-empty: span > MaxGroupSpan >= 1 puts the midpoint strictly
        // between the extremes.
        private static void AddNodesSplitBySpan(
            List<Member> members, int ctVol, IntVec3 wb, List<Node> nodes)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;
            foreach (Member m in members)
            {
                minX = Math.Min(minX, m.Position.x);
                maxX = Math.Max(maxX, m.Position.x);
                minZ = Math.Min(minZ, m.Position.z);
                maxZ = Math.Max(maxZ, m.Position.z);
            }

            if (maxX - minX <= MaxGroupSpan && maxZ - minZ <= MaxGroupSpan)
            {
                nodes.Add(MakeNode(members, ctVol, wb));
                return;
            }

            bool splitX = maxX - minX >= maxZ - minZ;
            int mid = splitX ? (minX + maxX) / 2 : (minZ + maxZ) / 2;
            var lo = new List<Member>();
            var hi = new List<Member>();
            foreach (Member m in members)
            {
                int coord = splitX ? m.Position.x : m.Position.z;
                (coord <= mid ? lo : hi).Add(m);
            }
            AddNodesSplitBySpan(lo, ctVol, wb, nodes);
            AddNodesSplitBySpan(hi, ctVol, wb, nodes);
        }

        private static Node MakeNode(List<Member> members, int ctVol, IntVec3 wb)
        {
            members.Sort((a, b) =>
            {
                int c = HaulMath.ManhattanDist(a.Position, wb)
                    .CompareTo(HaulMath.ManhattanDist(b.Position, wb));
                if (c != 0) return c;
                c = a.Position.x.CompareTo(b.Position.x);
                if (c != 0) return c;
                return a.Position.z.CompareTo(b.Position.z);
            });

            int available = 0;
            foreach (Member m in members)
                available += m.Available;

            return new Node
            {
                CtVol = ctVol,
                Members = members,
                Available = available,
                RepPos = members[0].Position,
                DistWB = HaulMath.ManhattanDist(members[0].Position, wb),
            };
        }

        private static List<IntVec3> IndexPositions(List<DefNodes> defs)
        {
            var positions = new List<IntVec3>();
            var indexOf = new Dictionary<IntVec3, int>();
            foreach (DefNodes d in defs)
            {
                foreach (Node n in d.Nodes)
                {
                    if (!indexOf.TryGetValue(n.RepPos, out int at))
                    {
                        at = positions.Count;
                        indexOf[n.RepPos] = at;
                        positions.Add(n.RepPos);
                    }
                    n.PosIndex = at;
                }
            }
            return positions;
        }
    }
}
