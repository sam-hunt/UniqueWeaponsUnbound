using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Color tab ---

        private void DrawColorTab(Rect rect)
        {
            float curY = rect.y;

            // Disabled: reverted to base
            if (IsRevertedToBase)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(rect,
                    "Select traits to customize color"); // TODO: localize
                GUI.color = prevColor;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Forced by trait
            ColorDef forced = GetForcedColor();
            if (forced != null)
            {
                // Find the trait name that forces the color (last one wins)
                string forcingTraitName = "";
                foreach (WeaponTraitDef trait in desiredTraits)
                {
                    if (trait.forcedColor != null)
                        forcingTraitName = trait.LabelCap;
                }

                // Vertically center the forced-color display within the tab
                float blockHeight = 20f + 8f + 60f + 4f + 20f; // msg + gap + swatch + gap + label
                float blockY = rect.y + (rect.height - blockHeight) / 2f;

                Text.Anchor = TextAnchor.MiddleCenter;
                Rect msgRect = new Rect(rect.x, blockY, rect.width, 20f);
                GUI.color = new Color(0.8f, 0.8f, 0.5f);
                Widgets.Label(msgRect,
                    "Color determined by " + forcingTraitName); // TODO: localize
                GUI.color = Color.white;

                // Show forced color swatch
                Rect forcedSwatchRect = new Rect(
                    rect.x + (rect.width - 60f) / 2f,
                    msgRect.yMax + 8f,
                    60f, 60f);
                Widgets.DrawBoxSolid(forcedSwatchRect, forced.color);
                Widgets.DrawBox(forcedSwatchRect);

                Text.Anchor = TextAnchor.UpperCenter;
                Rect forcedLabelRect = new Rect(rect.x, forcedSwatchRect.yMax + 4f, rect.width, 20f);
                Widgets.Label(forcedLabelRect, forced.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Normal: clickable swatch grid
            int cols = Mathf.Max(1,
                Mathf.FloorToInt(rect.width / (ColorSwatchSize + ColorSwatchGap)));

            int col = 0;
            float startX = rect.x;
            foreach (ColorDef colorDef in availableWeaponColors)
            {
                Rect swatchRect = new Rect(
                    startX + col * (ColorSwatchSize + ColorSwatchGap),
                    curY,
                    ColorSwatchSize,
                    ColorSwatchSize);

                // Draw the swatch
                Widgets.DrawBoxSolid(swatchRect, colorDef.color);

                // Selected highlight (no border on unselected)
                if (colorDef == desiredColor)
                {
                    Widgets.DrawBox(swatchRect, 2);
                    GUI.color = Color.white;
                    Widgets.DrawBox(swatchRect, 3);
                }

                // Hover highlight
                if (Mouse.IsOver(swatchRect))
                {
                    Widgets.DrawHighlight(swatchRect);
                    TooltipHandler.TipRegion(swatchRect, colorDef.LabelCap);
                }

                // Click to select
                if (Widgets.ButtonInvisible(swatchRect))
                    desiredColor = colorDef;

                col++;
                if (col >= cols)
                {
                    col = 0;
                    curY += ColorSwatchSize + ColorSwatchGap;
                }
            }
        }
    }
}
