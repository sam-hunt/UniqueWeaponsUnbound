using RimWorld;
using UniqueWeaponsUnbound.HaulPlanning;
using Verse;

namespace UniqueWeaponsUnbound
{
    public class UWU_Settings : ModSettings
    {
        public QualityCategory minimumQuality = QualityCategory.Awful;
        public bool requireCustomizationResearch = true;
        public bool requireRecipeResearch = true;
        public bool requireAppropriateWorkbench = true;
        public bool allowArchotechCustomization;
        public bool allowUltratechCustomization = true;
        public bool allowUncraftableCustomization = true;
        public bool allowDefConversion = true;
        public bool enforceMaxTraitLimit = true;
        public bool enforceCanGenerateAlone;
        public bool enableIdeologyColors = true;
        public bool enableStructureColors = true;
        public bool enableGroundCustomization = true;
        public bool useRecipeBaseCost = true;
        public float traitCostMultiplier = 1f;
        public float traitRefundRate = 0.5f;
        public HaulPlannerKind haulPlannerKind = HaulPlannerKind.Sweep;

        public void ResetToDefaults()
        {
            minimumQuality = QualityCategory.Awful;
            requireCustomizationResearch = true;
            requireRecipeResearch = true;
            requireAppropriateWorkbench = true;
            allowArchotechCustomization = false;
            allowUltratechCustomization = true;
            allowUncraftableCustomization = true;
            allowDefConversion = true;
            enforceMaxTraitLimit = true;
            enforceCanGenerateAlone = false;
            enableIdeologyColors = true;
            enableStructureColors = true;
            enableGroundCustomization = true;
            useRecipeBaseCost = true;
            traitCostMultiplier = 1f;
            traitRefundRate = 0.5f;
            haulPlannerKind = HaulPlannerKind.Sweep;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref useRecipeBaseCost, "useRecipeBaseCost", true);
            Scribe_Values.Look(ref minimumQuality, "minimumQuality", QualityCategory.Awful);
            Scribe_Values.Look(ref requireCustomizationResearch, "requireCustomizationResearch", true);
            Scribe_Values.Look(ref requireRecipeResearch, "requireRecipeResearch", true);
            Scribe_Values.Look(ref requireAppropriateWorkbench, "requireAppropriateWorkbench", true);
            Scribe_Values.Look(ref allowArchotechCustomization, "allowArchotechCustomization");
            Scribe_Values.Look(ref allowUltratechCustomization, "allowUltratechCustomization", true);
            Scribe_Values.Look(ref allowUncraftableCustomization, "allowUncraftableCustomization", true);
            Scribe_Values.Look(ref allowDefConversion, "allowDefConversion", true);
            Scribe_Values.Look(ref enforceMaxTraitLimit, "enforceMaxTraitLimit", true);
            Scribe_Values.Look(ref enforceCanGenerateAlone, "enforceCanGenerateAlone");
            Scribe_Values.Look(ref enableIdeologyColors, "enableIdeologyColors", true);
            Scribe_Values.Look(ref enableStructureColors, "enableStructureColors", true);
            Scribe_Values.Look(ref enableGroundCustomization, "enableGroundCustomization", true);
            Scribe_Values.Look(ref traitCostMultiplier, "traitCostMultiplier", 1f);
            Scribe_Values.Look(ref traitRefundRate, "traitRefundRate", 0.5f);
            Scribe_Values.Look(ref haulPlannerKind, "haulPlannerKind", HaulPlannerKind.Sweep);
        }
    }
}
