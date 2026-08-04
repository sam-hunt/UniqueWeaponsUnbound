using System;
using System.Collections.Generic;
using UniqueWeaponsUnbound.HaulPlanning.Internal;
using Verse;

namespace UniqueWeaponsUnbound.HaulPlanning
{
    // Phase 3 — support enumeration: per def, enumerate minimal covers of the
    // demand over its nodes, canonicalize counts nearest-first, and pre-split
    // oversized takes into virtual copies that each fit a solo trip.
    public partial class ThoroughHaulPlanner
    {
        // One node carrying a concrete canonical take — possibly one chunk of
        // a virtual-copy split. The unit of the partition DP.
        private sealed class PlannedNode
        {
            public Node Source;
            public List<MemberTake> Takes;
            public float Mass;       // sum of take * unitMass
            public float BestBypass; // max over takes of min(take, ctVol) * unitMass
            public int PosIndex;
        }

        private struct MemberTake
        {
            public Thing Thing;
            public IntVec3 Position;
            public int Count;
            public float UnitMass;
        }

        private sealed class Support
        {
            public List<PlannedNode> Nodes;
        }

        // Enumerates minimal covers of the def's demand over its nodes,
        // canonicalizes counts, and pre-splits oversized takes into virtual
        // copies. Returns null with copyGuard=true when a node would need
        // more than MaxVirtualCopiesPerNode chunks, and null with
        // copyGuard=false when a single unit can't ride any trip (genuinely
        // unfittable — same silent null as Sweep's bin-pack).
        private static List<Support> EnumerateSupports(
            DefNodes d, float budget, out bool copyGuard)
        {
            copyGuard = false;
            List<Node> nodes = d.Nodes;
            int k = nodes.Count;
            int size = 1 << k;

            // Aggregate availability per subset (lowbit-incremental), then
            // keep MINIMAL covers: drop any one member and the rest no longer
            // covers demand.
            var availOf = new int[size];
            for (int mask = 1; mask < size; mask++)
            {
                int low = mask & -mask;
                availOf[mask] = availOf[mask ^ low] + nodes[IndexOfLowBit(low)].Available;
            }

            var covers = new List<CoverKey>();
            for (int mask = 1; mask < size; mask++)
            {
                if (availOf[mask] < d.Demand)
                    continue;
                bool minimal = true;
                int sumDist = 0;
                int count = 0;
                for (int i = 0; i < k; i++)
                {
                    if ((mask & (1 << i)) == 0) continue;
                    if (availOf[mask] - nodes[i].Available >= d.Demand)
                    {
                        minimal = false;
                        break;
                    }
                    sumDist += nodes[i].DistWB;
                    count++;
                }
                if (minimal)
                    covers.Add(new CoverKey { Mask = mask, SumDist = sumDist, Count = count });
            }

            // Nearest-biased cap; the mask tiebreak makes the order total, so
            // capping is deterministic.
            covers.Sort((a, b) =>
            {
                int c = a.SumDist.CompareTo(b.SumDist);
                if (c != 0) return c;
                c = a.Count.CompareTo(b.Count);
                if (c != 0) return c;
                return a.Mask.CompareTo(b.Mask);
            });

            int keep = Math.Min(covers.Count, MaxCoversPerDef);
            var supports = new List<Support>(keep);
            for (int i = 0; i < keep; i++)
            {
                Support s = BuildSupport(d, covers[i].Mask, budget, out copyGuard);
                if (s == null)
                    return null;
                supports.Add(s);
            }
            return supports;
        }

        private struct CoverKey
        {
            public int Mask;
            public int SumDist;
            public int Count;
        }

        // Canonical counts for one minimal cover: iterate the cover's nodes
        // nearest-first; each takes its full availability until the marginal
        // (farthest) node takes the remainder. Minimality guarantees every
        // member ends up with a positive take. The same nearest-first rule
        // canonicalizes member-stack takes inside each node.
        private static Support BuildSupport(
            DefNodes d, int mask, float budget, out bool copyGuard)
        {
            copyGuard = false;
            var planned = new List<PlannedNode>();
            int remaining = d.Demand;
            for (int i = 0; i < d.Nodes.Count && remaining > 0; i++)
            {
                if ((mask & (1 << i)) == 0)
                    continue;
                Node node = d.Nodes[i];
                int nodeTake = Math.Min(remaining, node.Available);
                remaining -= nodeTake;

                var takes = new List<MemberTake>();
                int left = nodeTake;
                for (int j = 0; j < node.Members.Count && left > 0; j++)
                {
                    Member m = node.Members[j];
                    int take = Math.Min(left, m.Available);
                    takes.Add(new MemberTake
                    {
                        Thing = m.Thing,
                        Position = m.Position,
                        Count = take,
                        UnitMass = m.UnitMass,
                    });
                    left -= take;
                }

                List<PlannedNode> chunks = ChunkTake(node, takes, budget, out bool unfittable);
                if (chunks == null)
                {
                    copyGuard = !unfittable;
                    return null;
                }
                planned.AddRange(chunks);
            }
            return new Support { Nodes = planned };
        }

        // Pre-split into virtual copies: greedy nearest-first maximal chunks,
        // each feasible as a solo trip. Returns null when the take needs more
        // than MaxVirtualCopiesPerNode chunks (unfittable=false: copy guard)
        // or when even a single unit can't ride any trip (unfittable=true).
        private static List<PlannedNode> ChunkTake(
            Node node, List<MemberTake> takes, float budget, out bool unfittable)
        {
            unfittable = false;
            var chunks = new List<PlannedNode>();
            var cur = new List<MemberTake>();
            float curMass = 0f;
            float curByp = 0f;

            foreach (MemberTake take in takes)
            {
                int remaining = take.Count;
                while (remaining > 0)
                {
                    int x = MaxAddable(curMass, curByp, take.UnitMass, node.CtVol,
                        remaining, budget);
                    if (x <= 0)
                    {
                        if (cur.Count == 0)
                        {
                            unfittable = true;
                            return null;
                        }
                        // Closing this chunk leaves at least one more, so the
                        // total would exceed the copy guard.
                        if (chunks.Count == MaxVirtualCopiesPerNode - 1)
                            return null;
                        chunks.Add(MakePlannedNode(node, cur, curMass, curByp));
                        cur = new List<MemberTake>();
                        curMass = 0f;
                        curByp = 0f;
                        continue;
                    }
                    cur.Add(new MemberTake
                    {
                        Thing = take.Thing,
                        Position = take.Position,
                        Count = x,
                        UnitMass = take.UnitMass,
                    });
                    curMass += x * take.UnitMass;
                    curByp = Math.Max(curByp, Math.Min(x, node.CtVol) * take.UnitMass);
                    remaining -= x;
                }
            }
            if (cur.Count > 0)
                chunks.Add(MakePlannedNode(node, cur, curMass, curByp));
            return chunks;
        }

        private static PlannedNode MakePlannedNode(
            Node node, List<MemberTake> takes, float mass, float bestBypass)
        {
            return new PlannedNode
            {
                Source = node,
                Takes = takes,
                Mass = mass,
                BestBypass = bestBypass,
                PosIndex = node.PosIndex,
            };
        }

        // Largest count of one member addable to a chunk while the chunk
        // stays feasible as a solo trip. invMass(x) = mass + x*u -
        // max(byp, min(x, ctVol)*u) is monotonically non-decreasing in x
        // (the bypass grows at most as fast as the mass), so binary search
        // on the feasibility predicate is exact.
        private static int MaxAddable(
            float mass, float byp, float u, int ctVol, int limit, float budget)
        {
            if (u <= 0f)
                return limit; // massless: no capacity pressure
            if (FitsSolo(mass, byp, u, ctVol, limit, budget))
                return limit;
            if (!FitsSolo(mass, byp, u, ctVol, 1, budget))
                return 0;
            int lo = 1, hi = limit; // invariant: Fits(lo), !Fits(hi)
            while (hi - lo > 1)
            {
                int mid = lo + (hi - lo) / 2;
                if (FitsSolo(mass, byp, u, ctVol, mid, budget)) lo = mid;
                else hi = mid;
            }
            return lo;
        }

        private static bool FitsSolo(
            float mass, float byp, float u, int ctVol, int x, float budget)
        {
            float bypass = Math.Max(byp, Math.Min(x, ctVol) * u);
            return mass + x * u - bypass <= budget + HaulMath.MassEpsilon;
        }
    }
}
