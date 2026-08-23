using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Texture tab ---

        private void DrawTextureTab(Rect rect)
        {
            // Disabled: reverted to base
            if (IsRevertedToBase)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(rect,
                    "UWU_SelectTraitsForTexture".Translate());
                GUI.color = prevColor;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Pre-render all variant previews (cached, rebuilds on color/def change)
            EnsureTextureVariantPreviews();

            // Grid of clickable texture variant cells, scrollable when other mods
            // add more variants than fit in the tab area. One cell per UNIQUE
            // variant (deduped display list) — but every selection is written
            // as a full subGraphics[] index, the domain overrideGraphicIndex
            // indexes on the live weapon.
            float scrollWidth = rect.width - 16f;
            int cols = Mathf.Max(1,
                Mathf.FloorToInt(scrollWidth / (TextureCellSize + TextureCellGap)));

            int cellCount = uniqueVariantIndexes.Count;
            int rows = (cellCount + cols - 1) / cols;
            float innerHeight = rows > 0
                ? rows * TextureCellSize + Mathf.Max(0, rows - 1) * TextureCellGap
                : 0f;

            Rect innerRect = new Rect(0f, 0f, scrollWidth, innerHeight);
            Widgets.BeginScrollView(rect, ref textureTabScroll, innerRect);

            float curY = 0f;
            int col = 0;
            for (int k = 0; k < cellCount; k++)
            {
                int variantIndex = uniqueVariantIndexes[k];
                Rect cellRect = new Rect(
                    col * (TextureCellSize + TextureCellGap),
                    curY,
                    TextureCellSize,
                    TextureCellSize);

                // Cell background
                Widgets.DrawBoxSolid(cellRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));

                // Draw texture variant preview (array is cell-indexed)
                if (textureVariantPreviews != null
                    && k < textureVariantPreviews.Length
                    && textureVariantPreviews[k] != null)
                {
                    Rect previewRect = cellRect.ContractedBy(8f);
                    GUI.DrawTexture(previewRect, textureVariantPreviews[k],
                        ScaleMode.ScaleToFit, true);
                }

                // Selected highlight (no border on unselected)
                if (IsSelectedVariantCell(variantIndex))
                {
                    Widgets.DrawBox(cellRect, 2);
                    GUI.color = Color.white;
                    Widgets.DrawBox(cellRect, 3);
                }

                // Hover highlight
                if (Mouse.IsOver(cellRect))
                    Widgets.DrawHighlight(cellRect);

                // Click to select
                if (Widgets.ButtonInvisible(cellRect))
                    desiredTextureIndex = variantIndex;

                col++;
                if (col >= cols)
                {
                    col = 0;
                    curY += TextureCellSize + TextureCellGap;
                }
            }

            Widgets.EndScrollView();
        }
    }
}
