using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound
{
    public enum OpType
    {
        RemoveTrait,
        AddTrait,
        ApplyCosmetics,
    }

    public class CustomizationOp
    {
        public OpType type;
        public WeaponTraitDef trait;
        public List<ThingDefCountClass> cost;
        public List<ThingDefCountClass> refund;

        // Optional color change on this specific op (null = no color change at this step)
        public ColorDef colorToApply;

        // If true, clear the color to default (no tint). Used when removing a
        // forced-color trait with no remaining forced-color traits active.
        public bool clearColor;

        // Only for ApplyCosmetics ops
        public string nameToApply;
        public int? textureIndexToApply;
    }

    /// <summary>
    /// Data transfer object between Dialog_WeaponCustomization and JobDriver_CustomizeWeapon.
    /// Stored in a static registry keyed by pawn ID; retrieved (and removed) when the job starts.
    /// </summary>
    public class CustomizationSpec
    {
        /// <summary>
        /// Ordered operations: removals → cosmetics → additions.
        /// Each op carries its own per-op cost and optional cosmetic changes.
        /// </summary>
        public List<CustomizationOp> operations;

        /// <summary>
        /// The final ThingDef the weapon should have after all operations.
        /// Used for def conversion decisions (base↔unique).
        /// </summary>
        public ThingDef resultingDef;

        /// <summary>
        /// Aggregate net resource cost across all operations (addition costs minus
        /// expected refunds). Used for pre-flight ingredient reservation and hauling.
        /// </summary>
        public List<ThingDefCountClass> totalCost;

        /// <summary>
        /// Aggregate resource refund from all removal operations (raw costs aggregated
        /// then floored once per material). Initializes the job driver's virtual refund
        /// ledger, which offsets addition costs and spawns any surplus at job end.
        /// </summary>
        public List<ThingDefCountClass> totalRefund;

        /// <summary>
        /// The final color to apply after all operations complete.
        /// Set from EffectiveColor in the dialog. Applied in the finalize toil
        /// to ensure it persists through Setup() calls and def conversions.
        /// Null means no color change (e.g., reverting to base with no unique comp).
        /// </summary>
        public ColorDef finalColor;

        /// <summary>
        /// The desired texture variant index. Applied during base→unique conversion
        /// so the weapon immediately shows the correct texture rather than a stale
        /// or random one until the ApplyCosmetics op runs later in the work loop.
        /// </summary>
        public int? finalTextureIndex;

        private static readonly Dictionary<int, CustomizationSpec> pending =
            new Dictionary<int, CustomizationSpec>();

        public static void Store(Pawn pawn, CustomizationSpec spec)
        {
            pending[pawn.thingIDNumber] = spec;
        }

        /// <summary>
        /// Returns and removes the pending spec for this pawn (one-shot consumption).
        /// Returns null if no spec exists.
        /// </summary>
        public static CustomizationSpec Retrieve(Pawn pawn)
        {
            if (pending.TryGetValue(pawn.thingIDNumber, out CustomizationSpec spec))
            {
                pending.Remove(pawn.thingIDNumber);
                return spec;
            }
            return null;
        }

        public static void Clear(Pawn pawn)
        {
            pending.Remove(pawn.thingIDNumber);
        }

        public static bool Has(Pawn pawn)
        {
            return pending.ContainsKey(pawn.thingIDNumber);
        }

        /// <summary>
        /// Returns the pending spec without removing it. Used by TryMakePreToilReservations
        /// for pre-flight checks (RimWorld calls this multiple times before the job starts).
        /// </summary>
        public static CustomizationSpec Peek(Pawn pawn)
        {
            pending.TryGetValue(pawn.thingIDNumber, out CustomizationSpec spec);
            return spec;
        }
    }
}
