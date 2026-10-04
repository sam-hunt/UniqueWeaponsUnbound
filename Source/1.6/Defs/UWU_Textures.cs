using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Keeps the attribute so the first resolve happens on the main thread
    // during CallAll (vanilla's canonical asset-loading slot). Textures sit
    // behind getters that re-resolve rather than in readonly fields because an
    // in-process play-data reload (main-menu language switch) destroys every
    // mod-shipped texture (ModContentHolder.ClearDestroy) while the type
    // initializer never runs again; a destroyed Texture2D compares equal to
    // null through Unity's == overload, which is exactly the check below.
    // Only Customize is mod-shipped; the two vanilla Ideology icons persist
    // (Resources.Load fallback) but are routed the same way for uniformity.
    [StaticConstructorOnStartup]
    public static class UWU_Textures
    {
        private const string CustomizePath = "UI/UWU_Customize";

        // Vanilla overlay icons for color swatches (Ideology DLC)
        private const string FavoriteColorPath = "UI/Icons/ColorSelector/ColorFavourite";
        private const string IdeoColorPath = "UI/Icons/ColorSelector/ColorIdeology";

        private static Texture2D customize;
        private static Texture2D favoriteColor;
        private static Texture2D ideoColor;

        static UWU_Textures()
        {
            customize = ContentFinder<Texture2D>.Get(CustomizePath);
            favoriteColor = ContentFinder<Texture2D>.Get(FavoriteColorPath, false);
            ideoColor = ContentFinder<Texture2D>.Get(IdeoColorPath, false);
        }

        // Unity-overloaded ==, deliberately not ?? (see CLAUDE.md).
        public static Texture2D Customize
        {
            get
            {
                if (customize == null)
                    customize = ContentFinder<Texture2D>.Get(CustomizePath);
                return customize;
            }
        }

        public static Texture2D FavoriteColor
        {
            get
            {
                if (favoriteColor == null)
                    favoriteColor = ContentFinder<Texture2D>.Get(FavoriteColorPath, false);
                return favoriteColor;
            }
        }

        public static Texture2D IdeoColor
        {
            get
            {
                if (ideoColor == null)
                    ideoColor = ContentFinder<Texture2D>.Get(IdeoColorPath, false);
                return ideoColor;
            }
        }
    }
}
