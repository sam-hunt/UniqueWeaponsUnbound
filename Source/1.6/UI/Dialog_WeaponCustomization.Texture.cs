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
            // add more variants than fit in the tab area.
            float scrollWidth = rect.width - 16f;
            int cols = Mathf.Max(1,
                Mathf.FloorToInt(scrollWidth / (TextureCellSize + TextureCellGap)));

            int rows = (textureVariantCount + cols - 1) / cols;
            float innerHeight = rows > 0
                ? rows * TextureCellSize + Mathf.Max(0, rows - 1) * TextureCellGap
                : 0f;

            Rect innerRect = new Rect(0f, 0f, scrollWidth, innerHeight);
            Widgets.BeginScrollView(rect, ref textureTabScroll, innerRect);

            float curY = 0f;
            int col = 0;
            for (int i = 0; i < textureVariantCount; i++)
            {
                Rect cellRect = new Rect(
                    col * (TextureCellSize + TextureCellGap),
                    curY,
                    TextureCellSize,
                    TextureCellSize);

                // Cell background
                Widgets.DrawBoxSolid(cellRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));

                // Draw texture variant preview
                if (textureVariantPreviews != null
                    && i < textureVariantPreviews.Length
                    && textureVariantPreviews[i] != null)
                {
                    Rect previewRect = cellRect.ContractedBy(8f);
                    GUI.DrawTexture(previewRect, textureVariantPreviews[i],
                        ScaleMode.ScaleToFit, true);
                }

                // Selected highlight (no border on unselected)
                if (i == desiredTextureIndex)
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
                    desiredTextureIndex = i;

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
