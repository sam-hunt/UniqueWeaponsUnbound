using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // --- Texture tab ---

        private void DrawTextureTab(Rect rect)
        {
            float curY = rect.y;

            // Disabled: reverted to base
            if (IsRevertedToBase)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(rect,
                    "Select traits to customize texture"); // TODO: localize
                GUI.color = prevColor;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Pre-render all variant previews (cached, rebuilds on color/def change)
            EnsureTextureVariantPreviews();

            // Grid of clickable texture variant cells
            int cols = Mathf.Max(1,
                Mathf.FloorToInt(rect.width / (TextureCellSize + TextureCellGap)));

            int col = 0;
            float startX = rect.x;
            for (int i = 0; i < textureVariantCount; i++)
            {
                Rect cellRect = new Rect(
                    startX + col * (TextureCellSize + TextureCellGap),
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
        }
    }
}
