using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    [StaticConstructorOnStartup]
    public static class UWU_Textures
    {
        public static readonly Texture2D Customize =
            ContentFinder<Texture2D>.Get("UI/UWU_Customize");
    }
}
