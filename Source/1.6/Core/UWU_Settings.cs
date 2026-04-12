using Verse;

namespace UniqueWeaponsUnbound
{
    public class UWU_Settings : ModSettings
    {
        public bool requireCustomizationResearch = true;
        public bool requireRecipeResearch = true;
        public bool requireAppropriateWorkbench = true;
        public bool allowArchotechCustomization;
        public bool allowUltratechCustomization = true;
        public bool allowUncraftableCustomization = true;
        public bool enforceCanGenerateAlone;
        public float traitCostMultiplier = 1f;
        public float traitRefundRate = 0.5f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref requireCustomizationResearch, "requireCustomizationResearch", true);
            Scribe_Values.Look(ref requireRecipeResearch, "requireRecipeResearch", true);
            Scribe_Values.Look(ref requireAppropriateWorkbench, "requireAppropriateWorkbench", true);
            Scribe_Values.Look(ref allowArchotechCustomization, "allowArchotechCustomization");
            Scribe_Values.Look(ref allowUltratechCustomization, "allowUltratechCustomization", true);
            Scribe_Values.Look(ref allowUncraftableCustomization, "allowUncraftableCustomization", true);
            Scribe_Values.Look(ref enforceCanGenerateAlone, "enforceCanGenerateAlone");
            Scribe_Values.Look(ref traitCostMultiplier, "traitCostMultiplier", 1f);
            Scribe_Values.Look(ref traitRefundRate, "traitRefundRate", 0.5f);
        }
    }
}
