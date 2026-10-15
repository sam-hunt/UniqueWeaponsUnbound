using Verse;

namespace UniqueWeaponsUnbound
{
    // Single place to ask "is mod X active?" from C#.
    //
    // Do NOT use ModsConfig.IsActive(string) for this: it is an exact match against the
    // ids stored in ModsConfig.xml, and Steam Workshop installs are stored with a
    // "_steam" postfix (vanillaexpanded.skills_steam), so it returns false for every
    // Workshop build of the mod and true only for local copies in Mods/. Until this
    // helper existed, the Vanilla Skills Expanded integration and the Alpha Armoury
    // drift warning silently stayed off for every Workshop subscriber.
    // The engine's own MayRequire, IfModActive and PatchOperation gates go through
    // ModLister's NoSuffix helpers, which is what this wraps, so C# detection agrees
    // with the XML gates.
    internal static class ModDetection
    {
        public static bool IsActive(string packageId)
        {
            return ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) != null;
        }
    }
}
