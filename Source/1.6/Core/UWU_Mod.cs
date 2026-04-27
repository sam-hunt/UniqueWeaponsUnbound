using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    public class UWU_Mod : Mod
    {
        public static UWU_Settings Settings { get; private set; }

        private Vector2 settingsScroll;
        private float settingsHeight;

        public UWU_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<UWU_Settings>();
        }

        public override string SettingsCategory() => "UWU_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float buttonHeight = 30f;
            float buttonGap = 10f;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - buttonHeight - buttonGap);
            Rect buttonRect = new Rect(inRect.x, inRect.yMax - buttonHeight, 200f, buttonHeight);

            float innerWidth = viewRect.width - 16f;
            Rect innerRect = new Rect(0f, 0f, innerWidth, Mathf.Max(settingsHeight, viewRect.height));
            Widgets.BeginScrollView(viewRect, ref settingsScroll, innerRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(new Rect(0f, 0f, innerWidth - 8f, 99999f));
            GameFont prev = Text.Font;

            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("UWU_SettingsTraitCosts".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(12.0f);

            listing.CheckboxLabeled(
                "UWU_UseRecipeBaseCost".Translate(),
                ref Settings.useRecipeBaseCost,
                "UWU_UseRecipeBaseCostDesc".Translate());

            listing.Gap();

            string costPct = (Settings.traitCostMultiplier * 100f).ToString("F0");
            string costLabel = "UWU_TraitCostMultiplier".Translate(costPct);
            if (Settings.traitCostMultiplier == 1f)
                costLabel += "UWU_DefaultSuffix".Translate();
            listing.Label(costLabel);
            Settings.traitCostMultiplier = listing.Slider(Settings.traitCostMultiplier, 0f, 3f);
            Settings.traitCostMultiplier = Mathf.Round(Settings.traitCostMultiplier * 20f) / 20f;

            bool costsFree = Settings.traitCostMultiplier == 0f;
            string refundPct = (Settings.traitRefundRate * 100f).ToString("F0");
            string refundLabel = "UWU_TraitRefundRate".Translate(refundPct);
            if (Settings.traitRefundRate == 0.5f)
                refundLabel += "UWU_DefaultSuffix".Translate();
            if (costsFree)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                listing.Label(refundLabel);
                Rect sliderRect = listing.GetRect(22f);
                Widgets.HorizontalSlider(sliderRect, Settings.traitRefundRate, 0f, 1f);
                TooltipHandler.TipRegion(sliderRect,
                    "UWU_RefundRateNoEffect".Translate());
                GUI.color = prevColor;
            }
            else
            {
                listing.Label(refundLabel);
                Settings.traitRefundRate = listing.Slider(Settings.traitRefundRate, 0f, 1f);
                Settings.traitRefundRate = Mathf.Round(Settings.traitRefundRate * 20f) / 20f;
            }

            listing.Gap(18.0f);

            Text.Font = GameFont.Medium;
            listing.Label("UWU_SettingsPrerequisites".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            string qualityLabel = "UWU_MinimumQuality".Translate(Settings.minimumQuality.GetLabel());
            if (Settings.minimumQuality == QualityCategory.Awful)
                qualityLabel += "UWU_DefaultSuffix".Translate();
            else if (Settings.minimumQuality == QualityCategory.Normal)
                qualityLabel += "UWU_RecommendedSuffix".Translate();
            listing.Label(qualityLabel, tooltip: "UWU_MinimumQualityDesc".Translate());
            float qualityValue = (int)Settings.minimumQuality;
            qualityValue = listing.Slider(qualityValue, 0f, (int)QualityCategory.Legendary);
            Settings.minimumQuality = (QualityCategory)Mathf.RoundToInt(qualityValue);

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_AllowDefConversion".Translate(),
                ref Settings.allowDefConversion,
                "UWU_AllowDefConversionDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_RequireCustomizationResearch".Translate(),
                ref Settings.requireCustomizationResearch,
                "UWU_RequireCustomizationResearchDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_RequireRecipeResearch".Translate(),
                ref Settings.requireRecipeResearch,
                "UWU_RequireRecipeResearchDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_RequireWorkbench".Translate(),
                ref Settings.requireAppropriateWorkbench,
                "UWU_RequireWorkbenchDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_AllowUncraftable".Translate(),
                ref Settings.allowUncraftableCustomization,
                "UWU_AllowUncraftableDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_AllowUltratech".Translate(),
                ref Settings.allowUltratechCustomization,
                "UWU_AllowUltratechDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_AllowArchotech".Translate(),
                ref Settings.allowArchotechCustomization,
                "UWU_AllowArchotechDesc".Translate());

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("UWU_SettingsMiscellaneous".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "UWU_EnableGroundCustomization".Translate(),
                ref Settings.enableGroundCustomization,
                "UWU_EnableGroundCustomizationDesc".Translate());

            listing.Gap();

            if (ModsConfig.IdeologyActive)
            {
                listing.CheckboxLabeled(
                    "UWU_EnableIdeoColors".Translate(),
                    ref Settings.enableIdeologyColors,
                    "UWU_EnableIdeoColorsDesc".Translate());

                listing.Gap();
            }

            listing.CheckboxLabeled(
                "UWU_EnableStructureColors".Translate(),
                ref Settings.enableStructureColors,
                "UWU_EnableStructureColorsDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_EnforceMaxTraitLimit".Translate(),
                ref Settings.enforceMaxTraitLimit,
                "UWU_EnforceMaxTraitLimitDesc".Translate());

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_EnforceSoleTrait".Translate(),
                ref Settings.enforceCanGenerateAlone,
                "UWU_EnforceSoleTraitDesc".Translate());

            listing.Gap(60f);

            Text.Font = prev;
            settingsHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();

            if (Widgets.ButtonText(buttonRect, "UWU_ResetToDefaults".Translate()))
            {
                Settings.ResetToDefaults();
            }
        }
    }
}
