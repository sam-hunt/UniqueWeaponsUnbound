using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Shared drawing helpers ---

        private void DrawCostIcons(
            Rect rect, List<ThingDefCountClass> costs, bool rightAlign = false,
            HashSet<ThingDef> insufficientResources = null, bool greenQuantities = false)
        {
            // TODO: Decide behavior for uncraftable weapons — TraitCostUtility returns
            // empty list. Currently shows nothing. May want "Free" label or a warning.
            if (costs == null || costs.Count == 0)
                return;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            float curX;
            if (rightAlign)
            {
                // Pre-calculate total width so we can start from the right edge
                float totalWidth = 0f;
                foreach (ThingDefCountClass cost in costs)
                {
                    totalWidth += CostIconSize + 1f;
                    totalWidth += Text.CalcSize("x" + cost.count).x + 6f;
                }
                if (totalWidth > 0f)
                    totalWidth -= 6f; // Remove trailing gap
                curX = Mathf.Max(rect.x, rect.xMax - totalWidth);
            }
            else
            {
                curX = rect.x;
            }

            foreach (ThingDefCountClass cost in costs)
            {
                if (curX + CostIconSize > rect.xMax)
                    break;

                // Material icon
                Rect iconRect = new Rect(curX, rect.y + (rect.height - CostIconSize) / 2f,
                    CostIconSize, CostIconSize);
                Widgets.ThingIcon(iconRect, cost.thingDef);
                curX += CostIconSize + 1f;

                // Count label — red when insufficient, green for refunds
                string countText = "x" + cost.count;
                float textWidth = Text.CalcSize(countText).x;
                Rect textRect = new Rect(curX, rect.y, textWidth, rect.height);
                bool isShort = insufficientResources != null
                    && insufficientResources.Contains(cost.thingDef);
                if (isShort)
                {
                    Color prevCostColor = GUI.color;
                    GUI.color = new Color(0.9f, 0.2f, 0.2f);
                    Widgets.Label(textRect, countText);
                    GUI.color = prevCostColor;
                }
                else if (greenQuantities)
                {
                    Color prevCostColor = GUI.color;
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    Widgets.Label(textRect, countText);
                    GUI.color = prevCostColor;
                }
                else
                {
                    Widgets.Label(textRect, countText);
                }
                curX += textWidth + 6f;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // --- Footer ---

        private void DrawFooter(Rect inRect)
        {
            float footerY = inRect.yMax - FooterHeight;
            float buttonY = footerY + (FooterHeight - ButtonSize.y) / 2f;

            // Cancel button (left-aligned)
            Rect cancelRect = new Rect(
                inRect.x,
                buttonY,
                ButtonSize.x,
                ButtonSize.y);
            if (Widgets.ButtonText(cancelRect, "UWU_Cancel".Translate()))
            {
                Close();
            }

            // Reset button (center-aligned)
            Rect resetRect = new Rect(
                inRect.x + (inRect.width - ButtonSize.x) / 2f,
                buttonY,
                ButtonSize.x,
                ButtonSize.y);
            if (HasChanges)
            {
                if (Widgets.ButtonText(resetRect, "UWU_Reset".Translate()))
                {
                    ResetToOriginal();
                }
            }
            else
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.ButtonText(resetRect, "UWU_Reset".Translate());
                GUI.color = prevColor;
            }

            // Confirm button (right-aligned)
            Rect confirmRect = new Rect(
                inRect.xMax - ButtonSize.x,
                buttonY,
                ButtonSize.x,
                ButtonSize.y);

            // Determine if confirm should be enabled
            bool canConfirm = HasChanges;
            bool insufficientForConfirm = false;
            if (canConfirm)
            {
                if (insufficientResources != null && insufficientResources.Count > 0)
                {
                    canConfirm = false;
                    insufficientForConfirm = true;
                }
            }

            if (canConfirm)
            {
                if (Widgets.ButtonText(confirmRect, "UWU_Confirm".Translate()))
                {
                    // Auto-generate name if result is unique but name is empty
                    if (ResultingDef == uniqueDef
                        && string.IsNullOrEmpty(desiredName)
                        && desiredTraits.Count > 0)
                    {
                        desiredName = GenerateWeaponName();
                    }

                    LogPlannedChanges();

                    // Build ordered operations list and spec — use net cost
                    // (total addition cost minus expected refunds) for hauling.
                    // The running job's wait-for-dialog toil will detect the
                    // stored spec on the next tick and advance to work phase.
                    var spec = BuildCustomizationSpec(currentNetCost);
                    CustomizationSpec.Store(pawn, spec);
                    Close();
                }
            }
            else
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.ButtonText(confirmRect, "UWU_Confirm".Translate());
                GUI.color = prevColor;

                if (insufficientForConfirm)
                {
                    // Red error text immediately left of the confirm button
                    string errorText = "Missing resources"; // TODO: localize
                    float errorWidth = Text.CalcSize(errorText).x;
                    Rect errorRect = new Rect(
                        confirmRect.x - errorWidth - 8f,
                        confirmRect.y,
                        errorWidth,
                        confirmRect.height);
                    Color prevColor2 = GUI.color;
                    GUI.color = new Color(0.9f, 0.2f, 0.2f);
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(errorRect, errorText);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = prevColor2;

                    TooltipHandler.TipRegion(confirmRect,
                        "Not enough materials on the map"); // TODO: localize
                }
            }
        }

        // --- Spec Building ---

        private CustomizationSpec BuildCustomizationSpec(List<ThingDefCountClass> totalCost)
        {
            var ops = new List<CustomizationOp>();
            List<WeaponTraitDef> removes = TraitsToRemove.ToList();
            List<WeaponTraitDef> adds = TraitsToAdd.ToList();

            bool hasForcedColorTraits =
                removes.Any(t => t.forcedColor != null) ||
                adds.Any(t => t.forcedColor != null);

            // 1. Removal ops
            var remainingOriginalTraits = new List<WeaponTraitDef>(originalTraits);
            foreach (WeaponTraitDef trait in removes)
            {
                var op = new CustomizationOp
                {
                    type = OpType.RemoveTrait,
                    trait = trait,
                    refund = UWU_Mod.Settings.refundFraction > 0f
                        ? TraitCostUtility.GetTraitCost(weapon, trait)
                        : null,
                };

                remainingOriginalTraits.Remove(trait);

                if (trait.forcedColor != null)
                {
                    // Revert to the next remaining forced color, or clear to default
                    // if no forced-color traits remain (weapon shows natural material).
                    ColorDef nextForced = null;
                    for (int i = remainingOriginalTraits.Count - 1; i >= 0; i--)
                    {
                        if (remainingOriginalTraits[i].forcedColor != null)
                        {
                            nextForced = remainingOriginalTraits[i].forcedColor;
                            break;
                        }
                    }
                    if (nextForced != null)
                        op.colorToApply = nextForced;
                    else
                        op.clearColor = true;
                }

                ops.Add(op);
            }

            // 2. Cosmetics op (only if result is unique)
            // If the weapon will be in base state after removals (all original
            // traits removed) and there are additions, cosmetics can't apply to
            // a base weapon. Merge them into the first AddTrait op instead,
            // which will convert base→unique and then apply cosmetics atomically.
            // Cosmetics can only apply to a unique weapon. We must defer them
            // onto the first AddTrait op if the weapon will be in base state when
            // the cosmetics step would run. Two cases:
            // (a) Weapon starts as unique but all original traits are removed → base
            // (b) Weapon starts as base (non-unique) → already in base state
            bool willBeBaseAfterRemovals = remainingOriginalTraits.Count == 0
                && WeaponCustomizationUtility.IsUniqueWeapon(weapon.def)
                && baseDef != null;
            bool startsAsBase = !WeaponCustomizationUtility.IsUniqueWeapon(weapon.def);
            bool deferCosmetics = (willBeBaseAfterRemovals || startsAsBase) && adds.Count > 0;

            string deferredName = null;
            int? deferredTexture = null;
            ColorDef deferredColor = null;

            if (ResultingDef == uniqueDef)
            {
                bool nameChanged = desiredName != originalName;
                bool texChanged = desiredTextureIndex != originalTextureIndex;
                bool colorChanged = EffectiveColor != originalColor;

                if (deferCosmetics)
                {
                    // Always save ALL desired cosmetics when deferring. The round-trip
                    // through base state (unique→base→unique) destroys the CompUniqueWeapon,
                    // so existing name/color are lost even if unchanged by the player.
                    // They must be re-applied after the first AddTrait converts back to unique.
                    deferredName = desiredName;
                    deferredTexture = desiredTextureIndex;
                    if (!hasForcedColorTraits)
                        deferredColor = desiredColor;
                }
                else if (nameChanged || texChanged || (colorChanged && !hasForcedColorTraits))
                {
                    var cosOp = new CustomizationOp
                    {
                        type = OpType.ApplyCosmetics,
                    };

                    if (nameChanged)
                        cosOp.nameToApply = desiredName;
                    if (texChanged)
                        cosOp.textureIndexToApply = desiredTextureIndex;
                    if (colorChanged && !hasForcedColorTraits)
                        cosOp.colorToApply = desiredColor;

                    ops.Add(cosOp);
                }
            }

            // 3. Addition ops
            bool firstAdd = true;
            foreach (WeaponTraitDef trait in adds)
            {
                var op = new CustomizationOp
                {
                    type = OpType.AddTrait,
                    trait = trait,
                    cost = TraitCostUtility.GetTraitCost(weapon, trait),
                };

                if (trait.forcedColor != null)
                    op.colorToApply = trait.forcedColor;

                // Merge deferred cosmetics into the first AddTrait op
                if (firstAdd && deferCosmetics)
                {
                    op.nameToApply = deferredName;
                    op.textureIndexToApply = deferredTexture;
                    if (deferredColor != null && op.colorToApply == null)
                        op.colorToApply = deferredColor;
                    firstAdd = false;
                }

                ops.Add(op);
            }

            return new CustomizationSpec
            {
                operations = ops,
                resultingDef = ResultingDef,
                totalCost = totalCost,
                totalRefund = currentTotalRefund,
                finalColor = ResultingDef == uniqueDef ? EffectiveColor : null,
                finalTextureIndex = ResultingDef == uniqueDef ? desiredTextureIndex : (int?)null,
            };
        }

        // --- Actions ---

        private void LogPlannedChanges()
        {
            string logMsg = "[Unique Weapons Unbound] Planned changes for "
                + weapon.LabelCap + ":\n";

            List<WeaponTraitDef> adds = TraitsToAdd.ToList();
            List<WeaponTraitDef> removes = TraitsToRemove.ToList();

            if (adds.Count > 0)
                logMsg += "  Add: " + string.Join(", ", adds.Select(t => t.LabelCap)) + "\n";
            if (removes.Count > 0)
                logMsg += "  Remove: " + string.Join(", ", removes.Select(t => t.LabelCap)) + "\n";

            logMsg += "  Result: " + ResultingDef.LabelCap + " with "
                + desiredTraits.Count + " trait(s)\n";

            // Name change
            if (desiredName != originalName)
            {
                if (string.IsNullOrEmpty(originalName))
                    logMsg += "  Name: \"" + desiredName + "\" (new)\n";
                else
                    logMsg += "  Name: \"" + originalName + "\" \u2192 \"" + desiredName + "\"\n";
            }

            // Texture change
            if (desiredTextureIndex != originalTextureIndex)
            {
                logMsg += "  Texture: variant " + (originalTextureIndex + 1)
                    + " \u2192 " + (desiredTextureIndex + 1)
                    + " (of " + textureVariantCount + ")\n";
            }

            // Color change
            ColorDef effectiveColor = EffectiveColor;
            if (effectiveColor != originalColor)
            {
                string origLabel = originalColor?.LabelCap ?? "none";
                string newLabel = effectiveColor?.LabelCap ?? "none";
                logMsg += "  Color: " + origLabel + " \u2192 " + newLabel;
                if (GetForcedColor() != null)
                    logMsg += " (forced by trait)";
                logMsg += "\n";
            }

            if (currentTotalRefund != null && currentTotalRefund.Count > 0)
            {
                logMsg += "  Refund: " + string.Join(", ",
                    currentTotalRefund.Select(c => c.thingDef.LabelCap + " x" + c.count)) + "\n";
            }

            if (currentNetCost != null && currentNetCost.Count > 0)
            {
                logMsg += "  Net cost: " + string.Join(", ",
                    currentNetCost.Select(c => c.thingDef.LabelCap + " x" + c.count));
            }
            else
            {
                logMsg += "  Net cost: Free";
            }

            Log.Message(logMsg);
        }

        public override void OnCancelKeyPressed()
        {
            Close();
        }
    }
}
