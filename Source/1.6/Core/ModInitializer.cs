using HarmonyLib;
using Verse;

namespace UniqueWeaponsUnbound
{
    [StaticConstructorOnStartup]
    public static class UniqueWeaponsUnboundMod
    {
        static UniqueWeaponsUnboundMod()
        {
            var harmony = new Harmony("shunter.uniqueweaponsunbound");
            harmony.PatchAll();

            WeaponRegistry.Initialize();
            WorkbenchUtility.Initialize();
            TraitCostUtility.Initialize();

            Log.Message("[Unique Weapons Unbound] Initialized with " +
                harmony.GetPatchedMethods().EnumerableCount() + " patches.");
        }
    }
}
