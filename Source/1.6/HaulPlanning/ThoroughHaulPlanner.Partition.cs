using System;
using System.Collections.Generic;
using UniqueWeaponsUnbound.HaulPlanning.Internal;

namespace UniqueWeaponsUnbound.HaulPlanning
{
    // Phase 4 — partition DP: split one support combo's nodes into feasible
    // trips at minimum total tour cost, lowbit-pinned so each partition is
    // enumerated exactly once.
    public partial class ThoroughHaulPlanner
    {
        // Scratch arrays reused across combos, sized once to the largest
        // combo — per-combo allocation would churn the GC at high combo
        // counts for no benefit.
        private sealed class PartitionBuffers
        {
            public readonly int[] PosMask;
            public readonly float[] Mass;
            public readonly float[] Byp;
            public readonly int[] F;
            public readonly int[] Parent;

            public PartitionBuffers(int maxNodes)
            {
                int size = 1 << Math.Max(maxNodes, 1);
                PosMask = new int[size];
                Mass = new float[size];
                Byp = new float[size];
                F = new int[size];
                Parent = new int[size];
            }
        }

        // f[mask] = cheapest trip partition of the node subset `mask`, with
        // the first trip pinned to contain lowbit(mask) so each partition is
        // enumerated exactly once. Subset sums (position mask, mass, best
        // bypass) are lowbit-incremental, so feasibility and tour lookup per
        // candidate trip are O(1).
        private static int SolvePartition(
            List<PlannedNode> nodes, SubsetTourTable tours, float budget,
            PartitionBuffers buf, out List<int> tripMasks)
        {
            int k = nodes.Count;
            int size = 1 << k;
            int[] posMaskOf = buf.PosMask;
            float[] massOf = buf.Mass;
            float[] bypOf = buf.Byp;
            int[] f = buf.F;
            int[] parent = buf.Parent;

            posMaskOf[0] = 0;
            massOf[0] = 0f;
            bypOf[0] = 0f;
            for (int mask = 1; mask < size; mask++)
            {
                int low = mask & -mask;
                int li = IndexOfLowBit(low);
                int rest = mask ^ low;
                posMaskOf[mask] = posMaskOf[rest] | (1 << nodes[li].PosIndex);
                massOf[mask] = massOf[rest] + nodes[li].Mass;
                bypOf[mask] = Math.Max(bypOf[rest], nodes[li].BestBypass);
            }

            long steps = 0;
            f[0] = 0;
            for (int mask = 1; mask < size; mask++)
            {
                int low = mask & -mask;
                int rest = mask ^ low;
                int best = Infeasible;
                int bestT = 0;
                int sub = rest;
                while (true)
                {
                    int t = sub | low;
                    steps++;
                    if (massOf[t] - bypOf[t] <= budget + HaulMath.MassEpsilon)
                    {
                        int c = f[mask ^ t];
                        if (c < Infeasible)
                        {
                            c += tours.TourCost(posMaskOf[t]);
                            if (c < best)
                            {
                                best = c;
                                bestT = t;
                            }
                        }
                    }
                    if (sub == 0) break;
                    sub = (sub - 1) & rest;
                }
                f[mask] = best;
                parent[mask] = bestT;
            }
            LastPlanPartitionSteps += steps;

            int full = size - 1;
            if (f[full] >= Infeasible)
            {
                tripMasks = null;
                return int.MaxValue;
            }
            tripMasks = new List<int>();
            for (int mask = full; mask != 0; mask ^= parent[mask])
            {
                if (parent[mask] == 0)
                {
                    tripMasks = null; // defensive: corrupt parent chain
                    return int.MaxValue;
                }
                tripMasks.Add(parent[mask]);
            }
            return f[full];
        }
    }
}
