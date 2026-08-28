using RimWorld;
using UnityEngine;
using UniqueWeaponsUnbound.HaulPlanning;
using Verse;

namespace UniqueWeaponsUnbound
{
    public class UWU_Mod : Mod
    {
        // Setter is internal so the headless test suite can install a settings
        // instance; production assigns it exactly once, in the ctor below.
        public static UWU_Settings Settings { get; internal set; }

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
            listing.Label("UWU_SettingsProgression".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(12.0f);

            listing.CheckboxLabeled(
                "UWU_RestrictTraitsToDiscovered".Translate(),
                ref Settings.restrictTraitsToDiscovered,
                "UWU_RestrictTraitsToDiscoveredDesc".Translate());

            listing.Gap(18.0f);

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
            Settings.traitCostMultiplier = listing.Slider(Settings.traitCostMultiplier, 0f, 5f);
            Settings.traitCostMultiplier = Mathf.Round(Settings.traitCostMultiplier * 20f) / 20f;

            bool costsFree = Settings.traitCostMultiplier == 0f;
            string refundPct = (Settings.traitRefundRate * 100f).ToString("F0");
            string refundLabel = "UWU_TraitRefundRate".Translate(refundPct);
            if (Settings.traitRefundRate == 0.5f)
                refundLabel += "UWU_DefaultSuffix".Translate();
            if (costsFree)
            {
                DrawInertSlider(listing, refundLabel, Settings.traitRefundRate, 0f, 1f,
                    "UWU_RefundRateNoEffect".Translate());
            }
            else
            {
                listing.Label(refundLabel);
                Settings.traitRefundRate = listing.Slider(Settings.traitRefundRate, 0f, 1f);
                Settings.traitRefundRate = Mathf.Round(Settings.traitRefundRate * 20f) / 20f;
            }

            listing.Gap();

            string rarityCapText = Settings.rarityCostCap.ToString("0.##");
            string rarityCapLabel = "UWU_RarityCostCap".Translate(rarityCapText);
            if (Settings.rarityCostCap == 2f)
                rarityCapLabel += "UWU_DefaultSuffix".Translate();
            if (costsFree)
            {
                DrawInertSlider(listing, rarityCapLabel, Settings.rarityCostCap, 1f, 4f,
                    "UWU_CostSettingNoEffect".Translate());
            }
            else
            {
                listing.Label(rarityCapLabel, tooltip: "UWU_RarityCostCapDesc".Translate());
                Settings.rarityCostCap = listing.Slider(Settings.rarityCostCap, 1f, 4f);
                Settings.rarityCostCap = Mathf.Round(Settings.rarityCostCap * 4f) / 4f;
            }

            listing.Gap();

            string floorPct = (Settings.complexityFloorScale * 100f).ToString("F0");
            string floorLabel = "UWU_ComplexityFloorScale".Translate(floorPct);
            if (Settings.complexityFloorScale == 1f)
                floorLabel += "UWU_DefaultSuffix".Translate();
            if (costsFree)
            {
                DrawInertSlider(listing, floorLabel, Settings.complexityFloorScale, 0f, 2f,
                    "UWU_CostSettingNoEffect".Translate());
            }
            else
            {
                listing.Label(floorLabel, tooltip: "UWU_ComplexityFloorScaleDesc".Translate());
                Settings.complexityFloorScale = listing.Slider(Settings.complexityFloorScale, 0f, 2f);
                Settings.complexityFloorScale = Mathf.Round(Settings.complexityFloorScale * 20f) / 20f;
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
                "UWU_RequireCustomizationResearchDesc".Translate(
                    UWU_ResearchDefOf.UniqueSmithing.label,
                    UWU_ResearchDefOf.UniqueMachining.label,
                    UWU_ResearchDefOf.UniqueFabrication.label));

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_RequireRecipeResearch".Translate(),
                ref Settings.requireRecipeResearch,
                "UWU_RequireRecipeResearchDesc".Translate(UWU_ThingDefOf.Gun_ChargeRifle.label, UWU_ResearchDefOf.ChargedShot.label));

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

            if (Settings.allowArchotechCustomization)
            {
                // Visually forced on — Archotech implies Ultratech at runtime
                // (see CustomizationRules.GetRequiredResearch). Stored setting is
                // left untouched so toggling Archotech off restores prior intent.
                Color prevColor = GUI.color;
                Color prevContent = GUI.contentColor;
                GUI.color = new Color(0.4f, 0.4f, 0.4f);
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                bool forcedOn = true;
                listing.CheckboxLabeled(
                    "UWU_AllowUltratech".Translate(),
                    ref forcedOn,
                    "UWU_AllowUltratechImpliedDesc".Translate());
                GUI.contentColor = prevContent;
                GUI.color = prevColor;
            }
            else
            {
                listing.CheckboxLabeled(
                    "UWU_AllowUltratech".Translate(),
                    ref Settings.allowUltratechCustomization,
                    "UWU_AllowUltratechDesc".Translate(UWU_ResearchDefOf.UniqueFabrication.label));
            }

            listing.Gap();

            listing.CheckboxLabeled(
                "UWU_AllowArchotech".Translate(),
                ref Settings.allowArchotechCustomization,
                "UWU_AllowArchotechDesc".Translate(UWU_ResearchDefOf.UniqueFabrication.label));

            listing.Gap(18.0f);

            DrawSkillCheckSettings(listing);

            listing.Gap(24.0f);

            Text.Font = GameFont.Medium;
            listing.Label("UWU_SettingsHaulPlanner".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Sequential,
                "UWU_HaulPlannerSequential".Translate() + "UWU_VanillaSuffix".Translate(),
                "UWU_HaulPlannerSequentialDesc".Translate(),
                enabled: true);

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Sweep,
                "UWU_HaulPlannerSweep".Translate() + "UWU_DefaultSuffix".Translate(),
                "UWU_HaulPlannerSweepDesc".Translate(),
                enabled: true);

            DrawHaulPlannerOption(listing,
                HaulPlannerKind.Thorough,
                "UWU_HaulPlannerThorough".Translate() + "UWU_ExperimentalSuffix".Translate(),
                "UWU_HaulPlannerThoroughDesc".Translate(),
                enabled: true);

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

        // The optional skill prerequisite: two radio groups (who is checked,
        // what is demanded) and the flat-minimum slider. The second group and
        // the slider render inert while the subject is "no one"; the slider is
        // also inert unless the flat kind is in force. The weaponsmithing row
        // is inert without Vanilla Skills Expanded, and when it is nonetheless
        // the stored selection the flat row renders as active at the fallback
        // level — mirroring what SkillCheckRules.EffectiveKind enforces —
        // without touching the stored value, so installing VSE later restores
        // the player's intent (same non-mutating treatment as the
        // Archotech-implies-Ultratech checkbox above).
        private static void DrawSkillCheckSettings(Listing_Standard listing)
        {
            listing.Label("UWU_SkillCheckSubject".Translate(),
                tooltip: "UWU_SkillCheckSubjectDesc".Translate());
            listing.Gap(4f);

            DrawSubjectOption(listing, SkillCheckSubject.None,
                "UWU_SkillCheckSubjectNone".Translate() + "UWU_DefaultSuffix".Translate(),
                "UWU_SkillCheckSubjectNoneDesc".Translate());
            DrawSubjectOption(listing, SkillCheckSubject.CustomizingPawn,
                "UWU_SkillCheckSubjectPawn".Translate(),
                "UWU_SkillCheckSubjectPawnDesc".Translate());
            DrawSubjectOption(listing, SkillCheckSubject.BestOnMap,
                "UWU_SkillCheckSubjectMap".Translate(),
                "UWU_SkillCheckSubjectMapDesc".Translate());
            DrawSubjectOption(listing, SkillCheckSubject.BestAnywhere,
                "UWU_SkillCheckSubjectWorld".Translate(),
                "UWU_SkillCheckSubjectWorldDesc".Translate());

            listing.Gap(8f);

            bool enabled = SkillCheckRules.Enabled;
            string inertTip = "UWU_SkillCheckKindNoEffect".Translate();
            bool vseAvailable = VanillaSkillsExpandedIntegration.Available;
            bool fallback = SkillCheckRules.WeaponsmithFallbackActive;
            SkillCheckKind effective = SkillCheckRules.EffectiveKind(out int flatLevel);

            Color prevColor = GUI.color;
            if (!enabled)
                GUI.color = Color.gray;
            listing.Label("UWU_SkillCheckKind".Translate(),
                tooltip: enabled ? "UWU_SkillCheckKindDesc".Translate() : inertTip);
            GUI.color = prevColor;
            listing.Gap(4f);

            if (DrawRadioOption(listing,
                "UWU_SkillCheckKindRecipe".Translate() + "UWU_DefaultSuffix".Translate(),
                enabled ? "UWU_SkillCheckKindRecipeDesc".Translate(TechTierSummary()) : inertTip,
                active: effective == SkillCheckKind.RecipeOrTechTier,
                enabled: enabled))
            {
                Settings.skillCheckKind = SkillCheckKind.RecipeOrTechTier;
            }

            // The radio label doubles as the slider's value label (no
            // "(default)" suffix here — on a radio row it would read as the
            // default option rather than the default level).
            string flatLabel = "UWU_SkillCheckKindFlat".Translate(flatLevel);
            string flatTip = !enabled ? inertTip
                : fallback ? "UWU_SkillCheckWeaponsmithFallbackDesc".Translate(SkillCheckRules.WeaponsmithFallbackLevel)
                : "UWU_SkillCheckKindFlatDesc".Translate();
            if (DrawRadioOption(listing, flatLabel, flatTip,
                active: effective == SkillCheckKind.FlatMinimum,
                enabled: enabled))
            {
                Settings.skillCheckKind = SkillCheckKind.FlatMinimum;
            }

            // Slider indented under its radio row; live only when the flat
            // kind is the player's own selection.
            Rect sliderRect = listing.GetRect(22f);
            sliderRect.xMin += 24f;
            bool sliderLive = enabled && effective == SkillCheckKind.FlatMinimum && !fallback;
            if (sliderLive)
            {
                Settings.skillCheckMinimumLevel = Mathf.RoundToInt(Widgets.HorizontalSlider(
                    sliderRect, Settings.skillCheckMinimumLevel,
                    SkillCheckRules.MinFlatLevel, SkillCheckRules.MaxFlatLevel));
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.HorizontalSlider(sliderRect, flatLevel,
                    SkillCheckRules.MinFlatLevel, SkillCheckRules.MaxFlatLevel);
                GUI.color = prevColor;
                TooltipHandler.TipRegion(sliderRect,
                    !enabled || fallback ? flatTip : "UWU_SkillCheckFlatSliderNoEffect".Translate());
            }
            listing.Gap(8f);

            string weaponsmithTip = !enabled ? inertTip
                : vseAvailable ? "UWU_SkillCheckKindWeaponsmithDesc".Translate()
                : "UWU_SkillCheckWeaponsmithUnavailableDesc".Translate();
            if (DrawRadioOption(listing,
                "UWU_SkillCheckKindWeaponsmith".Translate(), weaponsmithTip,
                active: effective == SkillCheckKind.WeaponsmithExpertise,
                enabled: enabled && vseAvailable))
            {
                Settings.skillCheckKind = SkillCheckKind.WeaponsmithExpertise;
            }
        }

        private static void DrawSubjectOption(
            Listing_Standard listing, SkillCheckSubject subject, string label, string tooltip)
        {
            if (DrawRadioOption(listing, label, tooltip,
                active: Settings.skillCheckSubject == subject, enabled: true))
            {
                Settings.skillCheckSubject = subject;
            }
        }

        // The per-tier fallback minimums, for the recipe option's tooltip, read
        // from the rule table so the text can't drift from the behaviour.
        private static string TechTierSummary()
        {
            TechLevel[] tiers =
            {
                TechLevel.Neolithic, TechLevel.Medieval, TechLevel.Industrial,
                TechLevel.Spacer, TechLevel.Ultra, TechLevel.Archotech,
            };
            var parts = new string[tiers.Length];
            for (int i = 0; i < tiers.Length; i++)
            {
                parts[i] = "UWU_SkillCheckTierEntry".Translate(
                    tiers[i].ToStringHuman(),
                    SkillCheckRules.TechTierMinimumCraftingSkill(tiers[i]));
            }
            return string.Join(", ", parts);
        }

        // One radio row. Disabled rows render in vanilla's subtle grey and
        // ignore clicks (Widgets.RadioButtonLabeled still reports the click, so
        // the enabled check lives here). Returns true when an enabled row was
        // clicked.
        private static bool DrawRadioOption(
            Listing_Standard listing, string label, string tooltip, bool active, bool enabled)
        {
            bool clicked = listing.RadioButton(label, active, 0f, tooltip, null, disabled: !enabled);
            listing.Gap(4f);
            return clicked && enabled;
        }

        // A grayed, non-interactive slider row for a setting that currently has
        // no effect, with the explanation as a tooltip over the slider.
        private static void DrawInertSlider(
            Listing_Standard listing, string label, float value, float min, float max,
            string tooltip)
        {
            Color prevColor = GUI.color;
            GUI.color = Color.gray;
            listing.Label(label);
            Rect sliderRect = listing.GetRect(22f);
            Widgets.HorizontalSlider(sliderRect, value, min, max);
            TooltipHandler.TipRegion(sliderRect, tooltip);
            GUI.color = prevColor;
        }

        // Renders one row of the haul-planner radio group. Disabled options
        // render darkened and ignore clicks. Selecting an enabled option flips
        // Settings.haulPlannerKind to that value. The label is passed in fully
        // composed (including any "(default)" / "(vanilla)" suffix).
        private static void DrawHaulPlannerOption(
            Listing_Standard listing,
            HaulPlannerKind kind,
            string label,
            string tooltip,
            bool enabled)
        {
            bool active = Settings.haulPlannerKind == kind;

            if (enabled)
            {
                if (listing.RadioButton(label, active, tabIn: 0f, tooltip: tooltip))
                {
                    Settings.haulPlannerKind = kind;
                }
            }
            else
            {
                Color prevColor = GUI.color;
                Color prevContent = GUI.contentColor;
                // Compound the tint: GUI.color attenuates the whole control,
                // GUI.contentColor specifically attenuates text/icon glyphs.
                // Together they multiply (~0.4 * 0.5 = 0.2 effective), which
                // reads as visibly darker than plain Color.gray would.
                GUI.color = new Color(0.4f, 0.4f, 0.4f);
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                // Force-render as inactive even if Settings somehow points
                // here (e.g. via a save from a future build); the runtime
                // factory falls back to Sequential for unrecognized values
                // anyway, so showing it inactive here matches behavior.
                listing.RadioButton(label, active: false, tabIn: 0f, tooltip: tooltip);
                GUI.contentColor = prevContent;
                GUI.color = prevColor;
            }
            listing.Gap(8f);
        }
    }
}
