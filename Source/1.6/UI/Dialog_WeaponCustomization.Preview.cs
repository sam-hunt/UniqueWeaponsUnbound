using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    public partial class Dialog_WeaponCustomization
    {
        // Reflection: access protected Graphic_Collection.subGraphics for variant preview
        private static readonly FieldInfo SubGraphicsField = typeof(Graphic_Collection)
            .GetField("subGraphics", BindingFlags.Instance | BindingFlags.NonPublic);

        private const int PreviewRTSize = 256;
        private const int TextureGridRTSize = 128;

        // Cached preview render — rebuilt only when preview state changes
        private RenderTexture previewRT;
        private int cachedPreviewTextureIndex = -1;
        private ColorDef cachedPreviewColor;
        private ThingDef cachedPreviewDef;

        // Cached texture variant grid previews — rebuilt when color/def changes
        private RenderTexture[] textureVariantPreviews;
        private ColorDef cachedTextureGridColor;
        private ThingDef cachedTextureGridDef;

        // --- Left pane: weapon preview ---

        private void DrawWeaponPreview(Rect rect)
        {
            float curY = rect.y + 10f;

            // Weapon icon — reflects desired texture variant and effective color
            float iconSize = Mathf.Min(rect.width - 20f, rect.height * 0.4f);
            Rect iconRect = new Rect(
                rect.x + (rect.width - iconSize) / 2f,
                curY,
                iconSize,
                iconSize);
            DrawPreviewIcon(iconRect);

            curY = iconRect.yMax + 8f;

            // Name input field
            DrawNameRow(rect.x + 8f, ref curY, rect.width - 16f);

            Text.Anchor = TextAnchor.UpperLeft;
            curY += 20f;

            // Desired traits as clickable chips
            if (desiredTraits.Count > 0)
            {

                foreach (WeaponTraitDef trait in desiredTraits)
                {
                    float chipWidth = rect.width - 16f;
                    Rect chipRect = new Rect(rect.x + 8f, curY, chipWidth, TraitRowHeight);

                    // Chip background with hover highlight
                    bool hovered = Mouse.IsOver(chipRect);
                    Widgets.DrawBoxSolid(chipRect, hovered
                        ? new Color(0.3f, 0.3f, 0.3f, 0.5f)
                        : new Color(0.2f, 0.2f, 0.2f, 0.4f));

                    // Label
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Rect labelRect = new Rect(
                        chipRect.x + 4f, chipRect.y,
                        chipRect.width * 0.5f, chipRect.height);
                    Widgets.Label(labelRect, trait.LabelCap);

                    // Cost icons (right-aligned) — only for newly added traits
                    if (!originalTraits.Contains(trait))
                    {
                        List<ThingDefCountClass> chipCosts =
                            TraitCostUtility.GetAdditionCost(weapon, trait);
                        Rect chipCostRect = new Rect(
                            labelRect.xMax, chipRect.y,
                            chipRect.xMax - labelRect.xMax - 4f, chipRect.height);
                        DrawCostIcons(chipCostRect, chipCosts, rightAlign: true,
                            insufficientResources: insufficientResources);
                    }

                    Text.Anchor = TextAnchor.UpperLeft;

                    // Tooltip (same as traits tab)
                    string tooltip = BuildTraitTooltip(trait);
                    if (!string.IsNullOrEmpty(tooltip))
                        TooltipHandler.TipRegion(chipRect, tooltip);

                    // Click: switch to traits tab and scroll trait into view
                    if (Widgets.ButtonInvisible(chipRect))
                    {
                        activeTab = 0;
                        int traitIndex = compatibleTraits.IndexOf(trait);
                        if (traitIndex >= 0)
                            traitListScroll.y = traitIndex * (TraitRowHeight + TraitRowGap);
                    }

                    curY += TraitRowHeight + 2f;
                }
            }

            // Bottom-aligned cost and refund summary (always visible)
            {
                float costRowHeight = CostIconSize + 8f;
                float bottomPadding = 6f;

                bool hasSurplus = currentSurplus != null && currentSurplus.Count > 0;
                bool hasNetCost = currentNetCost != null && currentNetCost.Count > 0;

                // Stack from bottom: refund row, net cost row
                float bottomY = rect.yMax - bottomPadding;

                // Net refund row
                Rect refundArea = new Rect(
                    rect.x + 8f, bottomY - costRowHeight,
                    rect.width - 16f, costRowHeight);

                Text.Anchor = TextAnchor.MiddleLeft;
                if (!hasSurplus)
                    GUI.color = Color.gray;
                string refundLabel = "Net refund: "; // TODO: localize
                float refundLabelWidth = Text.CalcSize(refundLabel).x;
                Widgets.Label(
                    new Rect(refundArea.x, refundArea.y,
                        refundLabelWidth, refundArea.height),
                    refundLabel);

                if (hasSurplus)
                {
                    DrawCostIcons(
                        new Rect(refundArea.x + refundLabelWidth, refundArea.y,
                            refundArea.width - refundLabelWidth, refundArea.height),
                        currentSurplus,
                        greenQuantities: true);
                }
                else
                {
                    Widgets.Label(
                        new Rect(refundArea.x + refundLabelWidth, refundArea.y,
                            refundArea.width - refundLabelWidth, refundArea.height),
                        "None"); // TODO: localize
                    GUI.color = Color.white;
                }
                Text.Anchor = TextAnchor.UpperLeft;

                // Net cost row above refund
                Rect netCostArea = new Rect(
                    rect.x + 8f, refundArea.y - costRowHeight,
                    rect.width - 16f, costRowHeight);

                Text.Anchor = TextAnchor.MiddleLeft;
                if (hasNetCost)
                {
                    string costLabel = "Net cost: "; // TODO: localize
                    float labelWidth = Text.CalcSize(costLabel).x;
                    Widgets.Label(
                        new Rect(netCostArea.x, netCostArea.y, labelWidth, netCostArea.height),
                        costLabel);

                    DrawCostIcons(
                        new Rect(netCostArea.x + labelWidth, netCostArea.y,
                            netCostArea.width - labelWidth, netCostArea.height),
                        currentNetCost,
                        insufficientResources: insufficientResources);
                }
                else
                {
                    string costPrefix = "Net cost: "; // TODO: localize
                    float prefixWidth = Text.CalcSize(costPrefix).x;
                    Widgets.Label(
                        new Rect(netCostArea.x, netCostArea.y, prefixWidth, netCostArea.height),
                        costPrefix);
                    Color prevFreeColor = GUI.color;
                    GUI.color = new Color(0.4f, 0.8f, 0.4f);
                    Widgets.Label(
                        new Rect(netCostArea.x + prefixWidth, netCostArea.y,
                            netCostArea.width - prefixWidth, netCostArea.height),
                        "Free"); // TODO: localize
                    GUI.color = prevFreeColor;
                }
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawPreviewIcon(Rect rect)
        {
            ThingDef resultDef = ResultingDef;
            ColorDef effectiveColor = IsRevertedToBase ? null : EffectiveColor;

            bool needsRebuild = previewRT == null
                || cachedPreviewTextureIndex != desiredTextureIndex
                || cachedPreviewColor != effectiveColor
                || cachedPreviewDef != resultDef;

            // Rebuild during Layout to avoid disrupting Repaint's active rendering.
            // Graphics.Blit changes RenderTexture.active, which during Repaint would
            // redirect subsequent UI draws into our texture instead of the screen.
            if (needsRebuild && Event.current.type == EventType.Layout)
            {
                RebuildPreviewRT(resultDef, effectiveColor);
                cachedPreviewTextureIndex = desiredTextureIndex;
                cachedPreviewColor = effectiveColor;
                cachedPreviewDef = resultDef;
            }

            if (previewRT != null)
                GUI.DrawTexture(rect, previewRT, ScaleMode.ScaleToFit, true);
            else
                Widgets.ThingIcon(rect, resultDef);
        }

        private void RebuildPreviewRT(ThingDef resultDef, ColorDef colorDef)
        {
            DestroyPreviewRT();
            previewRT = BuildVariantPreview(resultDef, colorDef, desiredTextureIndex, PreviewRTSize);
        }

        /// <summary>
        /// Builds a RenderTexture preview for a specific texture variant of the weapon.
        /// Shared by the main preview icon and the texture variant grid.
        /// </summary>
        private RenderTexture BuildVariantPreview(
            ThingDef resultDef, ColorDef colorDef, int textureIndex, int rtSize)
        {
            Graphic graphic = resultDef.graphicData?.Graphic;
            if (graphic == null)
                return null;

            // Unwrap Graphic_RandomRotated to access underlying Graphic_Random
            if (graphic is Graphic_RandomRotated rotated)
                graphic = rotated.SubGraphic;

            // Select specific variant from Graphic_Random
            if (graphic is Graphic_Random random)
            {
                Graphic[] subs = SubGraphicsField?.GetValue(random) as Graphic[];
                if (subs != null && subs.Length > 0)
                    graphic = subs[textureIndex % subs.Length];
            }

            // Get colored version through the shader system — GetColoredVersion
            // re-initializes the graphic with the weapon's shader (CutoutComplex),
            // which loads the mask texture (_m suffix) so color is applied
            // only to masked regions (e.g. inlays, not the entire blade).
            if (colorDef != null)
            {
                Shader shader = resultDef.graphicData?.shaderType?.Shader
                    ?? ShaderDatabase.Cutout;
                graphic = graphic.GetColoredVersion(shader, colorDef.color, Color.white);
            }

            Material mat = graphic.MatSingle;
            Texture mainTex = mat?.mainTexture;
            if (mainTex == null)
                return null;

            RenderTexture rt = new RenderTexture(rtSize, rtSize, 0, RenderTextureFormat.ARGB32);

            // Save and restore RenderTexture.active around the entire operation.
            // Graphics.Blit sets it to the destination and does NOT restore it —
            // leaving it set would redirect all subsequent UI rendering into our texture.
            RenderTexture prev = RenderTexture.active;

            // Clear to transparent so clipped pixels (alpha < cutoff) stay transparent
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);

            // Blit through the material's shader — CutoutComplex reads the mask
            // texture to selectively apply the color, matching in-game rendering
            Graphics.Blit(mainTex, rt, mat);

            RenderTexture.active = prev;
            return rt;
        }

        private void EnsureTextureVariantPreviews()
        {
            ThingDef resultDef = ResultingDef;
            ColorDef effectiveColor = IsRevertedToBase ? null : EffectiveColor;

            bool needsRebuild = textureVariantPreviews == null
                || cachedTextureGridColor != effectiveColor
                || cachedTextureGridDef != resultDef;

            if (!needsRebuild || Event.current.type != EventType.Layout)
                return;

            DestroyTextureVariantPreviews();
            textureVariantPreviews = new RenderTexture[textureVariantCount];

            for (int i = 0; i < textureVariantCount; i++)
                textureVariantPreviews[i] = BuildVariantPreview(
                    resultDef, effectiveColor, i, TextureGridRTSize);

            cachedTextureGridColor = effectiveColor;
            cachedTextureGridDef = resultDef;
        }

        private void DestroyPreviewRT()
        {
            if (previewRT != null)
            {
                previewRT.Release();
                UnityEngine.Object.Destroy(previewRT);
                previewRT = null;
            }
        }

        private void DestroyTextureVariantPreviews()
        {
            if (textureVariantPreviews != null)
            {
                foreach (RenderTexture rt in textureVariantPreviews)
                {
                    if (rt != null)
                    {
                        rt.Release();
                        UnityEngine.Object.Destroy(rt);
                    }
                }
                textureVariantPreviews = null;
            }
        }

        public override void PreClose()
        {
            base.PreClose();
            DestroyPreviewRT();
            DestroyTextureVariantPreviews();
        }
    }
}
