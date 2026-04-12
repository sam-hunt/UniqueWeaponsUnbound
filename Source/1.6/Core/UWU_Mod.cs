using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    public class UWU_Mod : Mod
    {
        public static UWU_Settings Settings { get; private set; }

        public UWU_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<UWU_Settings>();
        }

        public override string SettingsCategory() => "Unique Weapons Unbound";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            GameFont prev = Text.Font;

            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Trait costs");
            Text.Font = GameFont.Small;

            string costPct = (Settings.traitCostMultiplier * 100f).ToString("F0");
            string costSuffix = Settings.traitCostMultiplier == 1f ? " (default)" : "";
            listing.Label("Trait cost multiplier: " + costPct + "%" + costSuffix);
            Settings.traitCostMultiplier = listing.Slider(Settings.traitCostMultiplier, 0f, 3f);
            Settings.traitCostMultiplier = Mathf.Round(Settings.traitCostMultiplier * 20f) / 20f;

            bool costsFree = Settings.traitCostMultiplier == 0f;
            string refundPct = (Settings.traitRefundRate * 100f).ToString("F0");
            string refundSuffix = Settings.traitRefundRate == 0.5f ? " (default)" : "";
            if (costsFree)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.gray;
                listing.Label("Trait refund rate: " + refundPct + "%" + refundSuffix);
                Rect sliderRect = listing.GetRect(22f);
                Widgets.HorizontalSlider(sliderRect, Settings.traitRefundRate, 0f, 1f);
                TooltipHandler.TipRegion(sliderRect,
                    "Trait refund rate has no effect while trait cost multiplier is 0%.");
                GUI.color = prevColor;
            }
            else
            {
                listing.Label("Trait refund rate: " + refundPct + "%" + refundSuffix);
                Settings.traitRefundRate = listing.Slider(Settings.traitRefundRate, 0f, 1f);
                Settings.traitRefundRate = Mathf.Round(Settings.traitRefundRate * 20f) / 20f;
            }

            listing.Gap();

            Text.Font = GameFont.Medium;
            listing.Label("Prerequisites");
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled(
                "Require weapon crafting research",
                ref Settings.requireRecipeResearch,
                "Require the completion of any research that would enable crafting " +
                "the weapon, in addition to the unique smithing/machining/fabrication " +
                "research. For example, customizing a charge rifle would also require " +
                "Pulse-charged munitions.");

            listing.Gap();

            listing.CheckboxLabeled(
                "Require appropriate workbench",
                ref Settings.requireAppropriateWorkbench,
                "Require that the workbench matches the weapon's tech level. " +
                "When disabled, any workbench that can craft weapons is sufficient " +
                "for customizing all weapons.");

            listing.Gap();

            listing.CheckboxLabeled(
                "Allow customizing uncraftable weapons",
                ref Settings.allowUncraftableCustomization,
                "Allow customization of weapons that have no crafting recipe.");

            listing.Gap();

            listing.CheckboxLabeled(
                "Allow customizing ultratech-level weapons",
                ref Settings.allowUltratechCustomization,
                "Allow customization of ultratech weapons, " +
                "requiring Advanced Weapon Customization research.");

            listing.Gap();

            listing.CheckboxLabeled(
                "Allow customizing archotech-level weapons",
                ref Settings.allowArchotechCustomization,
                "Allow customization of archotech weapons, " +
                "requiring Advanced Weapon Customization research.");

            listing.Gap();

            listing.CheckboxLabeled(
                "Enforce sole-trait restrictions",
                ref Settings.enforceCanGenerateAlone,
                "Some traits cannot be the only trait on a weapon during vanilla generation. " +
                "Enable this to enforce the same restriction during customization.");

            Text.Font = prev;
            listing.End();
        }
    }
}
