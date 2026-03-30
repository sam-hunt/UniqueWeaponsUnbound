using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Traits tab ---

        private void DrawTraitsTab(Rect rect)
        {
            float curY = rect.y;

            // Scrollable trait list
            float traitListHeight = rect.yMax - curY;
            Rect traitListOuterRect = new Rect(rect.x, curY, rect.width, traitListHeight);

            float totalTraitHeight = compatibleTraits.Count * (TraitRowHeight + TraitRowGap);

            Rect traitListInnerRect = new Rect(0f, 0f,
                traitListOuterRect.width - 16f, totalTraitHeight);

            Widgets.BeginScrollView(traitListOuterRect, ref traitListScroll, traitListInnerRect);

            float scrollY = 0f;
            foreach (WeaponTraitDef trait in compatibleTraits)
            {
                DrawTraitRow(traitListInnerRect.x, ref scrollY,
                    traitListInnerRect.width, trait);
            }

            Widgets.EndScrollView();
        }

        private void DrawTraitRow(float x, ref float curY, float width, WeaponTraitDef trait)
        {
            Rect rowRect = new Rect(x, curY, width, TraitRowHeight);
            string rejection = TraitValidationUtility.GetRejectionReason(desiredTraits, trait);
            bool isSelected = desiredTraits.Contains(trait);
            bool isDisabled = rejection != null;
            bool isClickable = isSelected || !isDisabled;

            // Highlight on hover for clickable traits
            if (isClickable && Mouse.IsOver(rowRect))
                Widgets.DrawHighlight(rowRect);

            // Selected highlight
            if (isSelected)
                Widgets.DrawBoxSolid(rowRect, new Color(0.35f, 0.35f, 0.35f, 0.4f));

            Color prevColor = GUI.color;
            if (isDisabled && !isSelected)
                GUI.color = new Color(0.5f, 0.5f, 0.5f);

            // Trait label
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(rowRect.x + 4f, rowRect.y,
                rowRect.width * 0.35f, rowRect.height);
            Widgets.Label(labelRect, trait.LabelCap);

            // Cost icons (right-aligned):
            // - Selected original: preview of removal (refund green / neg cost hypothetical red)
            // - Unselected original: committed removal (refund green / neg cost red if short)
            // - Selected new: committed addition cost, red if insufficient
            // - Unselected new: hypothetical addition cost, red if hypothetically unaffordable
            Rect costRect = new Rect(rowRect.x + rowRect.width * 0.7f, rowRect.y,
                rowRect.width * 0.3f - 4f, rowRect.height);
            bool isOriginal = originalTraits.Contains(trait);
            if (isSelected && isOriginal)
            {
                // Preview: what removal would cost/refund if toggled
                List<ThingDefCountClass> removalResult =
                    TraitCostUtility.GetRemovalCost(weapon, trait);
                if (TraitCostUtility.IsNegativeTrait(trait))
                {
                    DrawCostIcons(costRect, removalResult, rightAlign: true,
                        insufficientResources: GetHypotheticalInsufficient(removalResult));
                }
                else
                {
                    DrawCostIcons(costRect, removalResult, rightAlign: true,
                        greenQuantities: true);
                }
            }
            else if (!isSelected && isOriginal)
            {
                // Committed removal — show actual cost or refund
                List<ThingDefCountClass> removalResult =
                    TraitCostUtility.GetRemovalCost(weapon, trait);
                if (TraitCostUtility.IsNegativeTrait(trait))
                {
                    DrawCostIcons(costRect, removalResult, rightAlign: true,
                        insufficientResources: insufficientResources);
                }
                else if (UWU_Mod.Settings.refundFraction > 0f)
                {
                    DrawCostIcons(costRect, removalResult, rightAlign: true,
                        greenQuantities: true);
                }
            }
            else if (isSelected)
            {
                List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(weapon, trait);
                DrawCostIcons(costRect, costs, rightAlign: true,
                    insufficientResources: insufficientResources);
            }
            else
            {
                List<ThingDefCountClass> costs = TraitCostUtility.GetAdditionCost(weapon, trait);
                DrawCostIcons(costRect, costs, rightAlign: true,
                    insufficientResources: GetHypotheticalInsufficient(costs));
            }

            // Rejection reason (centered in the middle zone between label and costs)
            if (isDisabled && !isSelected)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.7f, 0.4f, 0.4f);
                Rect rejRect = new Rect(labelRect.xMax, rowRect.y,
                    costRect.x - labelRect.xMax, rowRect.height);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rejRect, rejection);
                Text.Font = GameFont.Small;
            }

            GUI.color = prevColor;
            Text.Anchor = TextAnchor.UpperLeft;

            // Tooltip with trait description and stat effects
            if (Mouse.IsOver(rowRect))
            {
                string tooltip = BuildTraitTooltip(trait);
                if (!string.IsNullOrEmpty(tooltip))
                    TooltipHandler.TipRegion(rowRect, tooltip);
            }

            // Click to add or remove
            if (isSelected)
            {
                if (Widgets.ButtonInvisible(rowRect)
                    && TraitValidationUtility.CanRemoveTrait(desiredTraits, trait))
                {
                    desiredTraits.Remove(trait);
                    OnTraitsChanged();
                }
            }
            else if (!isDisabled && Widgets.ButtonInvisible(rowRect))
            {
                desiredTraits.Add(trait);
                OnTraitsChanged();
            }

            curY += TraitRowHeight + TraitRowGap;
        }

        private string BuildTraitTooltip(WeaponTraitDef trait)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(trait.description))
                parts.Add(trait.description);

            var effectLines = new List<string>();

            if (trait.statOffsets != null)
            {
                foreach (StatModifier mod in trait.statOffsets)
                    effectLines.Add("  " + mod.stat.LabelCap + ": "
                        + mod.stat.ValueToString(mod.value, ToStringNumberSense.Offset));
            }

            if (trait.statFactors != null)
            {
                foreach (StatModifier mod in trait.statFactors)
                    effectLines.Add("  " + mod.stat.LabelCap + ": "
                        + mod.stat.ValueToString(mod.value, ToStringNumberSense.Factor));
            }

            if (trait.equippedStatOffsets != null)
            {
                foreach (StatModifier mod in trait.equippedStatOffsets)
                    effectLines.Add("  " + mod.stat.LabelCap + ": "
                        + mod.stat.ValueToString(mod.value, ToStringNumberSense.Offset));
            }

            if (trait.damageDefOverride != null)
                effectLines.Add("  " + "Damage type" + ": " + trait.damageDefOverride.LabelCap);

            if (trait.extraDamages != null)
            {
                foreach (ExtraDamage dmg in trait.extraDamages)
                    effectLines.Add("  " + "Extra damage" + ": "
                        + dmg.amount + " " + dmg.def.LabelCap);
            }

            if (trait.burstShotSpeedMultiplier != 1f)
                effectLines.Add("  " + "Burst speed" + ": x"
                    + trait.burstShotSpeedMultiplier.ToString("0.##"));

            if (trait.burstShotCountMultiplier != 1f)
                effectLines.Add("  " + "Burst count" + ": x"
                    + trait.burstShotCountMultiplier.ToString("0.##"));

            if (trait.additionalStoppingPower > 0f)
                effectLines.Add("  " + "Stopping power" + ": +"
                    + trait.additionalStoppingPower.ToString("0.##"));

            if (trait.ignoresAccuracyMaluses)
                effectLines.Add("  " + "Ignores accuracy penalties");

            if (effectLines.Count > 0)
                parts.Add("Effects:" + "\n" + string.Join("\n", effectLines));

            return string.Join("\n\n", parts);
        }
    }
}
