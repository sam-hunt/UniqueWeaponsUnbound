using Verse;

namespace UniqueWeaponsUnbound
{
    public class UWU_Settings : ModSettings
    {
        public bool allowArchotechCustomization;
        public bool allowUltratechCustomization = true;
        public bool allowUncraftableCustomization = true;
        public bool enforceCanGenerateAlone;
        public float costMultiplier = 1f;
        public float refundRate = 0.5f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref allowArchotechCustomization, "allowArchotechCustomization");
            Scribe_Values.Look(ref allowUltratechCustomization, "allowUltratechCustomization", true);
            Scribe_Values.Look(ref allowUncraftableCustomization, "allowUncraftableCustomization", true);
            Scribe_Values.Look(ref enforceCanGenerateAlone, "enforceCanGenerateAlone");
            Scribe_Values.Look(ref costMultiplier, "costMultiplier", 1f);
            Scribe_Values.Look(ref refundRate, "refundRate", 0.5f);
        }
    }
}
