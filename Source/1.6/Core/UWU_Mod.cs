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
            Text.Font = GameFont.Medium;

            string costPct = (Settings.costMultiplier * 100f).ToString("F0");
            string costSuffix = Settings.costMultiplier == 1f ? " (default)" : "";
            listing.Label("Cost multiplier: " + costPct + "%" + costSuffix);
            Settings.costMultiplier = listing.Slider(Settings.costMultiplier, 0f, 3f);
            Settings.costMultiplier = Mathf.Round(Settings.costMultiplier * 20f) / 20f;

            string refundPct = (Settings.refundRate * 100f).ToString("F0");
            string refundSuffix = Settings.refundRate == 0.5f ? " (default)" : "";
            listing.Label("Refund rate: " + refundPct + "%" + refundSuffix);
            Settings.refundRate = listing.Slider(Settings.refundRate, 0f, 1f);
            Settings.refundRate = Mathf.Round(Settings.refundRate * 20f) / 20f;

            listing.Gap();

            listing.CheckboxLabeled(
                "Allow uncraftable weapon customization",
                ref Settings.allowUncraftableCustomization,
                "Allow customization of weapons that have no crafting recipe.");

            listing.Gap();

            listing.CheckboxLabeled(
                "Allow ultratech weapon customization",
                ref Settings.allowUltratechCustomization,
                "Allow customization of ultratech weapons, " +
                "requiring Advanced Weapon Customization research.");

            listing.Gap();

            listing.CheckboxLabeled(
                "Allow archotech weapon customization",
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
