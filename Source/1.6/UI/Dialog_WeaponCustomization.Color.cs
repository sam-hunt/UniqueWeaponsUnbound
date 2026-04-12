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

            // Normal: scrollable swatch grids
            float scrollWidth = rect.width - 16f;
            int cols = Mathf.Max(1,
                Mathf.FloorToInt(scrollWidth / (ColorSwatchSize + ColorSwatchGap)));

            float innerHeight = MeasureColorSections(cols);
            Rect innerRect = new Rect(0f, 0f, scrollWidth, innerHeight);

            Widgets.BeginScrollView(rect, ref colorTabScroll, innerRect);

            float curY = 0f;
            float startX = 0f;

            // --- Weapon colors ---
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(startX, curY, scrollWidth, SectionHeaderHeight),
                "Weapon colors"); // TODO: localize
            curY += SectionHeaderHeight;

            DrawColorGrid(ref curY, startX, cols, availableWeaponColors, false);

            // --- Ideology colors (Ideo + Misc palette, when DLC active) ---
            if (UWU_Mod.Settings.enableIdeologyColors
                && availableIdeoColors != null && availableIdeoColors.Count > 0)
            {
                curY += 10f;
                Widgets.Label(new Rect(startX, curY, scrollWidth, SectionHeaderHeight),
                    "Ideology colors"); // TODO: localize
                curY += SectionHeaderHeight;

                DrawColorGrid(ref curY, startX, cols, availableIdeoColors, true);
            }

            // --- Structure colors ---
            if (UWU_Mod.Settings.enableStructureColors && availableStructureColors.Count > 0)
            {
                curY += 10f;
                Widgets.Label(new Rect(startX, curY, scrollWidth, SectionHeaderHeight),
                    "Structure colors"); // TODO: localize
                curY += SectionHeaderHeight;

                DrawColorGrid(ref curY, startX, cols, availableStructureColors, false);
            }

            Widgets.EndScrollView();
        }

        private void DrawColorGrid(
            ref float curY, float startX, int cols,
            System.Collections.Generic.List<ColorDef> colors, bool showIdeoOverlays)
        {
            int col = 0;
            foreach (ColorDef colorDef in colors)
            {
                Rect swatchRect = new Rect(
                    startX + col * (ColorSwatchSize + ColorSwatchGap),
                    curY,
                    ColorSwatchSize,
                    ColorSwatchSize);

                Widgets.DrawBoxSolid(swatchRect, colorDef.color);

                if (showIdeoOverlays)
                    DrawIdeoColorOverlay(swatchRect, colorDef);

                if (colorDef == desiredColor)
                {
                    Widgets.DrawBox(swatchRect, 2);
                    GUI.color = Color.white;
                    Widgets.DrawBox(swatchRect, 3);
                }

                if (Mouse.IsOver(swatchRect))
                {
                    Widgets.DrawHighlight(swatchRect);
                    TooltipHandler.TipRegion(swatchRect, colorDef.LabelCap);
                }

                if (Widgets.ButtonInvisible(swatchRect))
                    desiredColor = colorDef;

                col++;
                if (col >= cols)
                {
                    col = 0;
                    curY += ColorSwatchSize + ColorSwatchGap;
                }
            }

            if (col > 0)
                curY += ColorSwatchSize + ColorSwatchGap;
        }

        private float MeasureColorSections(int cols)
        {
            float height = 0f;

            // Weapon colors
            height += SectionHeaderHeight;
            height += GridHeight(availableWeaponColors.Count, cols);

            // Ideology colors
            if (UWU_Mod.Settings.enableIdeologyColors
                && availableIdeoColors != null && availableIdeoColors.Count > 0)
            {
                height += 10f + SectionHeaderHeight;
                height += GridHeight(availableIdeoColors.Count, cols);
            }

            // Structure colors
            if (UWU_Mod.Settings.enableStructureColors && availableStructureColors.Count > 0)
            {
                height += 10f + SectionHeaderHeight;
                height += GridHeight(availableStructureColors.Count, cols);
            }

            return height;
        }

        private static float GridHeight(int count, int cols)
        {
            if (count <= 0)
                return 0f;
            int rows = (count + cols - 1) / cols;
            return rows * (ColorSwatchSize + ColorSwatchGap);
        }

        private void DrawIdeoColorOverlay(Rect swatchRect, ColorDef colorDef)
        {
            Texture2D overlayTex = null;
            string tooltipKey = null;

            // Favorite color takes priority (matching vanilla styling station order)
            if (pawn.story?.favoriteColor != null
                && GenColor.IndistinguishableFrom(colorDef.color, pawn.story.favoriteColor.color))
            {
                overlayTex = UWU_Textures.FavoriteColor;
                tooltipKey = "FavoriteColorPickerTip";
            }
            else if (pawn.Ideo?.colorDef != null && !Find.IdeoManager.classicMode
                && GenColor.IndistinguishableFrom(colorDef.color, pawn.Ideo.colorDef.color))
            {
                overlayTex = UWU_Textures.IdeoColor;
                tooltipKey = "IdeoColorPickerTip";
            }

            if (overlayTex != null)
            {
                Rect iconRect = GenUI.ContractedBy(swatchRect, 4f);

                // Shadow pass
                GUI.color = ColorExtension.ToTransparent(Color.black, 0.2f);
                GUI.DrawTexture(
                    new Rect(iconRect.x + 2f, iconRect.y + 2f, iconRect.width, iconRect.height),
                    overlayTex);

                // Main pass
                GUI.color = ColorExtension.ToTransparent(Color.white, 0.8f);
                GUI.DrawTexture(iconRect, overlayTex);

                GUI.color = Color.white;
            }

            if (tooltipKey != null && Mouse.IsOver(swatchRect))
            {
                TooltipHandler.TipRegion(swatchRect,
                    tooltipKey.Translate(pawn.Named("PAWN")));
            }
        }
    }
}
