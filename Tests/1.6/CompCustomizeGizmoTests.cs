using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RimWorld;
using Verse;
using Xunit;

namespace UniqueWeaponsUnbound.Tests
{
    // Covers CompCustomizeGizmo.EnsureOn, the per-def injection step. Defs are
    // synthetic and stay out of the DefDatabase; WeaponRegistry is untouched.
    public class CompCustomizeGizmoTests
    {
        // ThingDef.IsWeapon reads category == Item, a non-empty verbs/tools
        // list and apparel == null; an uninitialized def satisfies the last.
        private static ThingDef MakeWeapon(string defName, List<CompProperties> comps)
        {
            var def = TestHelpers.MakeDef(defName, stackLimit: 1);
            def.category = ThingCategory.Item;
            def.tools = new List<Tool> { new Tool() };
            def.comps = comps;
            return def;
        }

        private static int CopyCount(ThingDef def)
        {
            return def.comps.Count(c => c.compClass == typeof(CompCustomizeGizmo));
        }

        [Fact]
        public void EnsureOn_AppendsOneCompAtEnd()
        {
            var existing = new CompProperties(typeof(CompQuality));
            var def = MakeWeapon("UWU_Test_Append", new List<CompProperties> { existing });
            Assert.True(def.IsWeapon);

            Assert.True(CompCustomizeGizmo.EnsureOn(def));

            Assert.Equal(2, def.comps.Count);
            Assert.Same(existing, def.comps[0]);
            Assert.Equal(typeof(CompCustomizeGizmo), def.comps[1].compClass);
        }

        [Fact]
        public void EnsureOn_IsIdempotent()
        {
            var def = MakeWeapon("UWU_Test_Idempotent", new List<CompProperties>());

            Assert.True(CompCustomizeGizmo.EnsureOn(def));
            Assert.False(CompCustomizeGizmo.EnsureOn(def));

            Assert.Equal(1, CopyCount(def));
        }

        [Fact]
        public void EnsureOn_DefsSharingOneCompsList_GetOneCopy()
        {
            var shared = new List<CompProperties>();
            var a = MakeWeapon("UWU_Test_SharedA", shared);
            var b = MakeWeapon("UWU_Test_SharedB", shared);

            Assert.True(CompCustomizeGizmo.EnsureOn(a));
            Assert.False(CompCustomizeGizmo.EnsureOn(b));

            Assert.Single(shared);
        }

        [Fact]
        public void EnsureOn_NullComps_CreatesListAndAdds()
        {
            var def = MakeWeapon("UWU_Test_NullComps", null);
            Assert.True(def.IsWeapon);

            Assert.True(CompCustomizeGizmo.EnsureOn(def));

            Assert.NotNull(def.comps);
            Assert.Equal(1, CopyCount(def));
        }

        [Fact]
        public void EnsureOn_NonWeapon_IsLeftUntouched()
        {
            // No verbs or tools, so IsWeapon is false; comps stays null.
            var def = TestHelpers.MakeDef("UWU_Test_NonWeapon", stackLimit: 1);
            def.category = ThingCategory.Item;
            Assert.False(def.IsWeapon);

            Assert.False(CompCustomizeGizmo.EnsureOn(def));
            Assert.Null(def.comps);

            def.comps = new List<CompProperties>();
            Assert.False(CompCustomizeGizmo.EnsureOn(def));
            Assert.Empty(def.comps);
        }

        [Fact]
        public void InjectedDef_InitializeComps_BuildsCompWithParent()
        {
            var def = MakeWeapon("UWU_Test_InitComps", new List<CompProperties>());
            CompCustomizeGizmo.EnsureOn(def);

            var thing = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            thing.def = def;
            thing.InitializeComps();

            var comp = Assert.Single(thing.AllComps.OfType<CompCustomizeGizmo>());
            Assert.Same(thing, comp.parent);
        }
    }
}
