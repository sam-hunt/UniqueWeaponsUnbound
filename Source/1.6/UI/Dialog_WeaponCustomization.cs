using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace UniqueWeaponsUnbound
{
    // Dialog layout (950x750):
    //
    // +------------------------------------------------------------------+
    // |  Customize [Weapon Name]                                         |
    // +---------------------+--------------------------------------------+
    // |                     |  Name  [___________________] [Random]      |
    // |   [Weapon Icon]     |                                            |
    // |                     |  [ Traits ][ Texture ][ Color * ]          |
    // |   "Weapon Name"     |  ┌──────────────────────────────────┐     |
    // |   "Will upgrade.."  |  │ ☑ Lightweight       ×4 steel     │     |
    // |   Color: [#] Gold   |  │ ☐ Gold Inlay        ×50 gold     │     |
    // |                     |  │   Conflicts with Silver Inlay    │     |
    // |   Traits:           |  │ ☐ Charge Capacitor  ×3 comp     │     |
    // |   • Lightweight  [x]|  │ ☐ Pulse Charger     ×3 comp     │     |
    // |   • Gold Inlay   [x]|  │ ...                               │     |
    // |                     |  └──────────────────────────────────┘     |
    // | Cost: [steel]×4     |                                            |
    // +---------------------+--------------------------------------------+
    // |  [Cancel]                  [Reset]                  [Confirm]    |
    // +------------------------------------------------------------------+
    //
    // Left pane (35%): Preview.cs — weapon icon, name, status, color swatch, trait list with [x]
    // Right pane (65%): Controls.cs — name field, texture nav, tab bar, tab content
    //   Traits tab: Traits.cs — scrollable checkbox list with per-trait costs and rejection reasons
    //   Texture tab: Texture.cs — clickable texture variant grid
    //   Color tab: Color.cs — clickable color swatch grid
    // Footer: Footer.cs — Cancel/Reset/Confirm buttons (ideology styling station layout)

    public partial class Dialog_WeaponCustomization : Window
    {
        // Reflection: delegate to WeaponModificationUtility which owns the FieldInfo statics
        private static FieldInfo CompNameField => WeaponModificationUtility.CompNameField;
        private static FieldInfo CompColorField => WeaponModificationUtility.CompColorField;

        // Immutable state — set in constructor, never modified
        private readonly Pawn pawn;
        private readonly Thing weapon;
        private readonly Building_WorkTable workbench;
        private readonly ThingDef uniqueDef;
        private readonly ThingDef baseDef; // null if unique weapon has no detected base
        private readonly List<WeaponTraitDef> originalTraits;
        private readonly List<WeaponTraitDef> compatibleTraits;
        private readonly string originalName;
        private readonly int originalTextureIndex;
        private readonly ColorDef originalColor;
        private readonly int textureVariantCount;
        private readonly List<ColorDef> availableWeaponColors;
        private readonly ColorDef initialDesiredColor;

        // Desired state — mutated by user interaction
        private readonly List<WeaponTraitDef> desiredTraits;
        private string desiredName;
        private int desiredTextureIndex;
        private ColorDef desiredColor;

        // UI state
        private Vector2 traitListScroll;
        private int activeTab; // 0 = Traits, 1 = Texture, 2 = Color
        private bool nameLocked;
        private bool hideNegativeTraits;
        private string lastAutoName;

        // Affordability state — recomputed each frame in DoWindowContents
        private HashSet<ThingDef> insufficientResources;
        private Dictionary<ThingDef, int> committedResources;
        private Dictionary<ThingDef, int> availableResources;
        private List<ThingDefCountClass> currentNetCost;
        private List<ThingDefCountClass> currentSurplus;
        private List<ThingDefCountClass> currentTotalRefund;
        private Dictionary<ThingDef, int> surplusBalance;

        // Layout constants
        private static readonly Vector2 ButtonSize = new Vector2(120f, 40f);
        private const float LeftPanePct = 0.35f;
        private const float TitleHeight = 40f;
        private const float GapBelowTitle = 10f;
        private const float FooterHeight = 50f;
        private const float PaneGap = 4f;
        private const float TraitRowHeight = 34f;
        private const float TraitRowGap = 2f;
        private const float SectionHeaderHeight = 30f;
        private const float RemoveButtonSize = 20f;
        private const float CostIconSize = 24f;
        private const float ControlRowHeight = 30f;
        private const float ControlRowGap = 4f;
        private const float NameFieldHeight = 35f;
        private const float ArrowButtonWidth = 28f;
        private const float RandomButtonWidth = 85f;
        private const float TabBarHeight = 32f;
        private const float ColorSwatchSize = 36f;
        private const float ColorSwatchGap = 10f;
        private const float TextureCellSize = 152f;
        private const float TextureCellGap = 12f;
        private const float ColorIndicatorSize = 16f;
        private const float ControlLabelWidth = 60f;

        public override Vector2 InitialSize => new Vector2(950f, 750f);

        public Dialog_WeaponCustomization(
            Pawn pawn, Thing weapon, Building_WorkTable workbench)
        {
            this.pawn = pawn;
            this.weapon = weapon;
            this.workbench = workbench;

            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = true;

            // Determine unique/base defs
            if (WeaponCustomizationUtility.IsUniqueWeapon(weapon.def))
            {
                uniqueDef = weapon.def;
                baseDef = WeaponCustomizationUtility.GetBaseVariant(weapon.def);
            }
            else
            {
                baseDef = weapon.def;
                uniqueDef = WeaponCustomizationUtility.GetUniqueVariant(weapon.def);
            }

            // Snapshot original traits
            CompUniqueWeapon uniqueComp = weapon.TryGetComp<CompUniqueWeapon>();
            if (uniqueComp != null && uniqueComp.TraitsListForReading != null)
                originalTraits = new List<WeaponTraitDef>(uniqueComp.TraitsListForReading);
            else
                originalTraits = new List<WeaponTraitDef>();
            // Note: non-unique weapons start with empty originalTraits — user can only add.

            desiredTraits = new List<WeaponTraitDef>(originalTraits);

            // Cache the full compatible trait list for this weapon type
            compatibleTraits = TraitValidationUtility.GetCompatibleTraits(uniqueDef);

            // Default to hiding negative traits unless the weapon already has one
            hideNegativeTraits = !originalTraits.Any(t => TraitCostUtility.IsNegativeTrait(t));

            // Snapshot original name via reflection
            if (uniqueComp != null && CompNameField != null)
                originalName = (string)CompNameField.GetValue(uniqueComp) ?? "";
            else
                originalName = "";
            desiredName = originalName;
            nameLocked = !string.IsNullOrEmpty(originalName);

            // Snapshot original color via reflection and build available colors list
            if (uniqueComp != null && CompColorField != null)
                originalColor = (ColorDef)CompColorField.GetValue(uniqueComp);
            else
                originalColor = null;

            availableWeaponColors = new List<ColorDef>();
            foreach (ColorDef colorDef in DefDatabase<ColorDef>.AllDefs)
            {
                if (colorDef.colorType == ColorType.Weapon && colorDef.randomlyPickable)
                    availableWeaponColors.Add(colorDef);
            }
            availableWeaponColors.SortBy(c => c.label);

            desiredColor = originalColor;
            if (desiredColor == null && availableWeaponColors.Count > 0)
                desiredColor = availableWeaponColors.RandomElement();
            initialDesiredColor = desiredColor;

            // Snapshot original texture index
            textureVariantCount = GetTextureVariantCount();
            originalTextureIndex = weapon.overrideGraphicIndex
                ?? (weapon.thingIDNumber % Mathf.Max(1, textureVariantCount));
            desiredTextureIndex = originalTextureIndex;
        }

        // --- Computed properties ---

        private ThingDef ResultingDef
        {
            get
            {
                if (desiredTraits.Count > 0)
                    return uniqueDef;
                // No desired traits — revert to base if one exists
                if (baseDef != null)
                    return baseDef;
                // Unique weapon with no detected base — keep unique def with zero traits.
                // This handles edge cases where a unique weapon has no base weapon mapping.
                return uniqueDef;
            }
        }

        /// <summary>
        /// True when weapon will revert to its non-unique base def (no traits, base exists).
        /// Name/texture/color controls are disabled in this state.
        /// </summary>
        private bool IsRevertedToBase => desiredTraits.Count == 0 && baseDef != null;

        /// <summary>
        /// The effective display color: forced color from traits takes priority,
        /// otherwise the player's manual choice.
        /// </summary>
        private ColorDef EffectiveColor => GetForcedColor() ?? desiredColor;

        /// <summary>
        /// Returns the forced color from the last desired trait with forcedColor != null,
        /// or null if no trait forces a color. Mirrors vanilla iteration order (last wins).
        /// </summary>
        private ColorDef GetForcedColor()
        {
            ColorDef forced = null;
            foreach (WeaponTraitDef trait in desiredTraits)
            {
                if (trait.forcedColor != null)
                    forced = trait.forcedColor;
            }
            return forced;
        }

        private bool HasChanges
        {
            get
            {
                if (desiredTraits.Count != originalTraits.Count)
                    return true;
                for (int i = 0; i < desiredTraits.Count; i++)
                {
                    if (desiredTraits[i] != originalTraits[i])
                        return true;
                }
                if (desiredName != originalName)
                    return true;
                if (desiredTextureIndex != originalTextureIndex)
                    return true;
                if (EffectiveColor != originalColor)
                    return true;
                return false;
            }
        }

        private IEnumerable<WeaponTraitDef> TraitsToAdd =>
            desiredTraits.Where(t => !originalTraits.Contains(t));

        private IEnumerable<WeaponTraitDef> TraitsToRemove =>
            originalTraits.Where(t => !desiredTraits.Contains(t));

        // --- Actions ---

        private void ResetToOriginal()
        {
            desiredTraits.Clear();
            desiredTraits.AddRange(originalTraits);
            desiredName = originalName;
            nameLocked = !string.IsNullOrEmpty(originalName);
            lastAutoName = null;
            desiredTextureIndex = originalTextureIndex;
            desiredColor = initialDesiredColor;
            traitListScroll = Vector2.zero;
        }

        private void OnTraitsChanged()
        {
            // If the last forced-color trait was removed and desiredColor is still
            // the inherited forced color (not manually changed by the player),
            // pick a random fallback so the preview updates.
            if (GetForcedColor() == null && desiredColor == originalColor
                && originalTraits.Any(t => t.forcedColor == originalColor
                    && !desiredTraits.Contains(t)))
            {
                if (availableWeaponColors.Count > 0)
                    desiredColor = availableWeaponColors.RandomElement();
            }

            if (!nameLocked && desiredTraits.Count > 0 && !IsRevertedToBase)
            {
                desiredName = GenerateWeaponName();
                lastAutoName = desiredName;
            }
        }

        // --- Helpers ---

        /// <summary>
        /// Returns the available count for a material on the map, with per-frame caching.
        /// </summary>
        private int GetAvailableCount(ThingDef thingDef)
        {
            if (availableResources.TryGetValue(thingDef, out int count))
                return count;
            count = WeaponModificationUtility.CountAvailable(pawn.Map, thingDef, pawn);
            availableResources[thingDef] = count;
            return count;
        }

        /// <summary>
        /// Returns the set of insufficient materials if this trait's cost were added
        /// on top of the currently committed resources. Accounts for unused refund
        /// surplus that can offset the hypothetical trait's cost. Returns null if
        /// fully affordable.
        /// </summary>
        private HashSet<ThingDef> GetHypotheticalInsufficient(List<ThingDefCountClass> traitCosts)
        {
            if (traitCosts == null || traitCosts.Count == 0)
                return null;

            HashSet<ThingDef> result = null;
            foreach (ThingDefCountClass cost in traitCosts)
            {
                committedResources.TryGetValue(cost.thingDef, out int committed);
                int needed = committed + cost.count;

                // Subtract unused refund surplus — these resources will be
                // available via the virtual ledger even though they aren't on the map
                if (surplusBalance.TryGetValue(cost.thingDef, out int surplus))
                    needed = Mathf.Max(0, needed - surplus);

                if (GetAvailableCount(cost.thingDef) < needed)
                {
                    if (result == null)
                        result = new HashSet<ThingDef>();
                    result.Add(cost.thingDef);
                }
            }
            return result;
        }

        /// <summary>
        /// Resolves the unique weapon def's graphic and returns the number of texture variants.
        /// Unwraps Graphic_RandomRotated if needed to access the underlying Graphic_Random.
        /// Returns 1 if the graphic is not a random-variant type.
        /// </summary>
        private int GetTextureVariantCount()
        {
            Graphic graphic = uniqueDef?.graphicData?.Graphic;
            if (graphic == null)
                return 1;

            if (graphic is Graphic_RandomRotated rotated)
                graphic = rotated.SubGraphic;

            if (graphic is Graphic_Random random)
                return random.SubGraphicsCount;

            return 1;
        }

        /// <summary>
        /// Generates a random weapon name using vanilla's grammar system
        /// (NameGenerator + RulePackDefOf.NamerUniqueWeapon), matching the same
        /// code path used by CompUniqueWeapon.PostPostMake() for initial generation.
        /// </summary>
        private string GenerateWeaponName()
        {
            // Collect adjectives from all desired traits
            var adjectives = new List<string>();
            foreach (WeaponTraitDef trait in desiredTraits)
            {
                if (trait.traitAdjectives != null && trait.traitAdjectives.Count > 0)
                    adjectives.AddRange(trait.traitAdjectives);
            }

            // Get weapon type label from CompProperties_UniqueWeapon.namerLabels
            CompProperties_UniqueWeapon props = uniqueDef?.comps?
                .OfType<CompProperties_UniqueWeapon>()
                .FirstOrDefault();
            string weaponType = (props?.namerLabels != null && props.namerLabels.Count > 0)
                ? props.namerLabels.RandomElement()
                : "Weapon";

            string colorLabel = EffectiveColor?.label ?? "blue";

            GrammarRequest request = default;
            request.Includes.Add(RulePackDefOf.NamerUniqueWeapon);
            request.Rules.Add(new Rule_String("weapon_type", weaponType));
            request.Rules.Add(new Rule_String("color", colorLabel));
            if (adjectives.Count > 0)
                request.Rules.Add(new Rule_String("trait_adjective", adjectives.RandomElement()));

            // Add the customizing pawn's name data for ANYPAWN_* grammar rules,
            // enabling possessive name patterns like "Kira's Gold Rifle"
            foreach (Rule rule in TaleData_Pawn.GenerateFrom(pawn).GetRules("ANYPAWN"))
                request.Rules.Add(rule);

            return NameGenerator.GenerateName(request, null, false, "r_weapon_name").StripTags();
        }

        /// <summary>
        /// Computes net cost and net surplus by subtracting refunds from costs per-material.
        /// Net cost contains materials where addition costs exceed refunds.
        /// Net surplus contains materials where refunds exceed addition costs (or appear
        /// only in refunds). These are what the player actually receives back.
        /// </summary>
        private static void ComputeNetCostAndSurplus(
            List<ThingDefCountClass> costs, List<ThingDefCountClass> refunds,
            out List<ThingDefCountClass> netCost, out List<ThingDefCountClass> netSurplus)
        {
            if (refunds == null || refunds.Count == 0)
            {
                netCost = costs;
                netSurplus = new List<ThingDefCountClass>();
                return;
            }

            var net = new Dictionary<ThingDef, int>();
            foreach (ThingDefCountClass cost in costs)
                net[cost.thingDef] = cost.count;
            foreach (ThingDefCountClass refund in refunds)
            {
                if (net.ContainsKey(refund.thingDef))
                    net[refund.thingDef] -= refund.count;
                else
                    net[refund.thingDef] = -refund.count;
            }

            netCost = new List<ThingDefCountClass>();
            netSurplus = new List<ThingDefCountClass>();
            foreach (KeyValuePair<ThingDef, int> kv in net)
            {
                if (kv.Value > 0)
                    netCost.Add(new ThingDefCountClass(kv.Key, kv.Value));
                else if (kv.Value < 0)
                    netSurplus.Add(new ThingDefCountClass(kv.Key, -kv.Value));
            }
        }

        // --- Main drawing ---

        public override void DoWindowContents(Rect inRect)
        {
            // Compute affordability state for cost coloring across all draw calls
            List<ThingDefCountClass> frameCost =
                TraitCostUtility.GetTotalCost(weapon, TraitsToAdd, TraitsToRemove);

            currentTotalRefund = UWU_Mod.Settings.refundFraction > 0f
                ? TraitCostUtility.GetTotalRefund(weapon, TraitsToRemove)
                : null;
            ComputeNetCostAndSurplus(frameCost, currentTotalRefund,
                out currentNetCost, out currentSurplus);

            surplusBalance = new Dictionary<ThingDef, int>();
            if (currentSurplus != null)
            {
                foreach (ThingDefCountClass surplus in currentSurplus)
                    surplusBalance[surplus.thingDef] = surplus.count;
            }

            committedResources = new Dictionary<ThingDef, int>();
            availableResources = new Dictionary<ThingDef, int>();
            insufficientResources = null;
            foreach (ThingDefCountClass cost in currentNetCost)
            {
                committedResources[cost.thingDef] = cost.count;
                int available = GetAvailableCount(cost.thingDef);
                if (available < cost.count)
                {
                    if (insufficientResources == null)
                        insufficientResources = new HashSet<ThingDef>();
                    insufficientResources.Add(cost.thingDef);
                }
            }

            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, TitleHeight);
            string titleLabel = "Customize " + weapon.LabelShortCap; // TODO: localize
            Widgets.Label(titleRect, titleLabel);
            Text.Font = GameFont.Small;

            // Content area between title and footer
            float contentTop = inRect.y + TitleHeight + GapBelowTitle;
            float contentHeight = inRect.height - TitleHeight - GapBelowTitle - FooterHeight;
            Rect contentRect = new Rect(inRect.x, contentTop, inRect.width, contentHeight);

            // Left pane: weapon preview
            Rect leftPane = new Rect(
                contentRect.x,
                contentRect.y,
                contentRect.width * LeftPanePct,
                contentRect.height);
            DrawWeaponPreview(leftPane);

            // Right pane: controls
            Rect rightPane = new Rect(
                leftPane.xMax + PaneGap,
                contentRect.y,
                contentRect.width - leftPane.width - PaneGap,
                contentRect.height);
            DrawControlsPanel(rightPane);

            // Footer
            DrawFooter(inRect);
        }
    }
}
