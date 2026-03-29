using Verse;

namespace UniqueWeaponsUnbound
{
    public class UWU_Settings : ModSettings
    {
        public bool allowArchotechCustomization;
        public bool allowUltratechCustomization = true;
        public bool allowUncraftableCustomization = true;
        public bool enforceCanGenerateAlone;
        public float refundFraction = 0.5f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref allowArchotechCustomization, "allowArchotechCustomization");
            Scribe_Values.Look(ref allowUltratechCustomization, "allowUltratechCustomization", true);
            Scribe_Values.Look(ref allowUncraftableCustomization, "allowUncraftableCustomization", true);
            Scribe_Values.Look(ref enforceCanGenerateAlone, "enforceCanGenerateAlone");
            Scribe_Values.Look(ref refundFraction, "refundFraction", 0.5f);
        }
    }
}
