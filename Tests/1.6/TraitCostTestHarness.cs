using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using RimWorld;
using Verse;

namespace UniqueWeaponsUnbound.Tests
{
    // Shared fixture for the trait-cost pipeline tests.
    //
    // The pipeline reads three pieces of global game state that a headless test
    // run has none of: the DefDatabase (material caches, ingredient resolution),
    // ThingDefOf/StatDefOf statics, and Prefs. Bootstrap() populates all three
    // once per process with synthetic defs whose market values and work amounts
    // mirror the real ones, then calls the production initializers — so the
    // caches under test are built by shipped code, not reimplemented here.
    //
    // Prefs matters: Prefs.DevMode returns TRUE when Prefs.data is null, which
    // would route worker startup diagnostics into Verse.Log and from there into
    // Unity. Bootstrap installs a PrefsData with devMode off before anything
    // runs a startup hook.
    internal static class TraitCostTestHarness
    {
        private static readonly object BootstrapLock = new object();
        private static bool bootstrapped;

        // Held for the life of the process: Verse.Log routes into Unity
        // (StackTraceUtility.ExtractStackTrace throws "ECall methods must be
        // packaged into a system module" outside a Unity runtime), and merely
        // touching a DefOf class runs DefOfHelper.EnsureInitializedInCtor, which
        // warns. A LogLock raises Log's own logDisablers counter so every
        // Log.Message/Warning/Error returns before it reaches Unity.
        private static Log.LogLock logSuppression;

        // Materials. Market values are the vanilla ones (research doc's
        // grounding table, verified against the local install).
        internal static ThingDef WoodLog { get; private set; }
        internal static ThingDef Steel { get; private set; }
        internal static ThingDef Plasteel { get; private set; }
        internal static ThingDef Jade { get; private set; }
        internal static ThingDef Gold { get; private set; }
        internal static ThingDef Silver { get; private set; }
        internal static ThingDef Uranium { get; private set; }
        internal static ThingDef Birdskin { get; private set; }
        internal static ThingDef Thrumbofur { get; private set; }
        internal static ThingDef Bioferrite { get; private set; }
        internal static ThingDef ComponentIndustrial { get; private set; }
        internal static ThingDef ComponentSpacer { get; private set; }
        internal static ThingDef MedicineHerbal { get; private set; }
        internal static ThingDef MedicineIndustrial { get; private set; }
        internal static ThingDef MedicineUltratech { get; private set; }
        internal static ThingDef Chemfuel { get; private set; }
        internal static ThingDef ChunkSlagSteel { get; private set; }
        internal static ThingDef HemogenPack { get; private set; }
        internal static ThingDef SignalChip { get; private set; }

        // Nothing in vanilla is worth zero; this exists to exercise the
        // by-value conversions' "can't price it" fallbacks.
        internal static ThingDef ValuelessMaterial { get; private set; }

        private static readonly Dictionary<string, WeaponCategoryDef> Categories =
            new Dictionary<string, WeaponCategoryDef>();

        private static int nextThingId = 10000;

        // Idempotent; every test calls it first. Def equality is reference
        // equality and the DefDatabase is process-global, so the synthetic defs
        // must be registered exactly once (a duplicate defName makes
        // DefDatabase.Add rename it via Rand, which would also break
        // determinism).
        internal static void Bootstrap()
        {
            lock (BootstrapLock)
            {
                if (bootstrapped)
                    return;
                bootstrapped = true;

                logSuppression = Log.LockMessages();
                InstallPrefs();
                InstallStatDefs();
                InstallMaterials();
                CostRuleHelpers.Initialize();
            }
        }

        // Verse.Log's "dev mode only" branches would otherwise fire, because
        // Prefs.DevMode is true whenever Prefs.data is null.
        private static void InstallPrefs()
        {
            FieldInfo dataField = typeof(Prefs).GetField(
                "data", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (dataField == null)
                throw new InvalidOperationException(
                    "Verse.Prefs has no 'data' field any more; the harness cannot force devMode off.");

            dataField.SetValue(null, new PrefsData { devMode = false });
        }

        // StatDef is safe to construct directly (plain Def ctor). It must NOT
        // come from GetUninitializedObject: the field initializers supply
        // maxValue/roundToFiveOver, and zeroed ones would clamp every stat to 0.
        // supressDisabledError short-circuits the Prefs.DevMode disabled-stat
        // check at the top of StatWorker.GetValueUnfinalized.
        private static void InstallStatDefs()
        {
            StatDefOf.MarketValue = new StatDef
            {
                defName = "MarketValue",
                supressDisabledError = true,
            };
            StatDefOf.WorkToMake = new StatDef
            {
                defName = "WorkToMake",
                supressDisabledError = true,
            };
        }

        // Raw-resource status is decided by shipped code (CostRuleHelpers.
        // Initialize: def.IsStuff || in the ResourcesRaw category), so the
        // synthetic defs carry the same stuffProps/thingCategories the vanilla
        // ones do. Notably ComponentIndustrial really does declare stuffProps in
        // vanilla, so it counts as a raw resource; ComponentSpacer, chemfuel,
        // medicine and slag chunks do not.
        private static void InstallMaterials()
        {
            // ThingCategoryDef's ctor reaches BaseContent → Unity's resource
            // loader, same as ThingDef's, so it also has to be allocated
            // without running constructors. Only its identity and its (null)
            // parent matter here.
            var resourcesRaw =
                (ThingCategoryDef)FormatterServices.GetUninitializedObject(typeof(ThingCategoryDef));
            resourcesRaw.defName = "ResourcesRaw";
            DefDatabase<ThingCategoryDef>.Add(resourcesRaw);

            WoodLog = MakeMaterial("WoodLog", "wood", 1.2f, resourcesRaw, stuff: true);
            Steel = MakeMaterial("Steel", "steel", 1.9f, resourcesRaw, stuff: true);
            Plasteel = MakeMaterial("Plasteel", "plasteel", 9f, resourcesRaw, stuff: true);
            Jade = MakeMaterial("Jade", "jade", 5f, resourcesRaw, stuff: true);
            Gold = MakeMaterial("Gold", "gold", 10f, resourcesRaw, stuff: true);
            Silver = MakeMaterial("Silver", "silver", 1f, resourcesRaw, stuff: true);
            Uranium = MakeMaterial("Uranium", "uranium", 6f, resourcesRaw, stuff: true);
            Bioferrite = MakeMaterial("Bioferrite", "bioferrite", 0.75f, resourcesRaw, stuff: true);
            Birdskin = MakeMaterial("Leather_Bird", "bird leather", 1.8f, null, stuff: true);
            Thrumbofur = MakeMaterial("Leather_Thrumbo", "thrumbofur", 14f, null, stuff: true);
            ComponentIndustrial = MakeMaterial("ComponentIndustrial", "component", 32f, null, stuff: true);

            ComponentSpacer = MakeMaterial("ComponentSpacer", "advanced component", 200f, null, stuff: false);
            MedicineHerbal = MakeMaterial("MedicineHerbal", "herbal medicine", 10f, null, stuff: false);
            MedicineIndustrial = MakeMaterial("MedicineIndustrial", "medicine", 18f, null, stuff: false);
            MedicineUltratech = MakeMaterial("MedicineUltratech", "glitterworld medicine", 50f, null, stuff: false);
            Chemfuel = MakeMaterial("Chemfuel", "chemfuel", 2.3f, null, stuff: false);
            ChunkSlagSteel = MakeMaterial("ChunkSlagSteel", "steel slag chunk", 15f, null, stuff: false);
            HemogenPack = MakeMaterial("HemogenPack", "hemogen pack", 5f, null, stuff: false);
            SignalChip = MakeMaterial("SignalChip", "signal chip", 1000f, null, stuff: false);
            ValuelessMaterial = MakeMaterial("TestValuelessGoo", "valueless goo", 0f, null, stuff: false);

            // NegativeDowngradeWorker builds its downgrade map from these in
            // OnStartup (reached via UseRules → SetRules), falling back to a
            // lazy build on first Apply, so they must be in place before any
            // test installs rules or runs the pipeline.
            ThingDefOf.WoodLog = WoodLog;
            ThingDefOf.Steel = Steel;
            ThingDefOf.Plasteel = Plasteel;
            ThingDefOf.Silver = Silver;
            ThingDefOf.ComponentIndustrial = ComponentIndustrial;
            ThingDefOf.ComponentSpacer = ComponentSpacer;
            ThingDefOf.MedicineHerbal = MedicineHerbal;
            ThingDefOf.Chemfuel = Chemfuel;
        }

        private static ThingDef MakeMaterial(
            string defName, string label, float marketValue,
            ThingCategoryDef category, bool stuff)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = defName;
            def.label = label;
            def.stackLimit = 75;
            def.comps = new List<CompProperties>();
            def.BaseMarketValue = marketValue;
            if (stuff)
                def.stuffProps = new StuffProperties();
            if (category != null)
                def.thingCategories = new List<ThingCategoryDef> { category };
            DefDatabase<ThingDef>.Add(def);
            return def;
        }

        // A weapon def. comps must be a real list: WeaponRegistry.IsUniqueWeapon
        // goes through ThingDef.HasComp, which walks comps without a null check.
        // Weapon defs stay out of the DefDatabase — nothing resolves them by
        // name, and registering them would let a trait's defName tokens match
        // one as a material.
        internal static ThingDef MakeWeaponDef(
            string defName, TechLevel techLevel, float workToMake = 0f,
            List<ThingDefCountClass> costList = null, int costStuffCount = 0)
        {
            var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            def.defName = defName;
            def.label = defName;
            def.techLevel = techLevel;
            def.stackLimit = 1;
            def.comps = new List<CompProperties>();
            def.costList = costList;
            def.costStuffCount = costStuffCount;
            if (workToMake > 0f)
                def.SetStatBaseValue(StatDefOf.WorkToMake, workToMake);
            return def;
        }

        internal static Thing MakeWeapon(ThingDef def, ThingDef stuff = null)
        {
            var thing = (Thing)FormatterServices.GetUninitializedObject(typeof(Thing));
            thing.def = def;
            thing.stackCount = 1;
            thing.thingIDNumber = nextThingId++;
            if (stuff != null)
                thing.SetStuffDirect(stuff);
            return thing;
        }

        // A weapon carrying a quality, for QualityMultiplierWorker. Thing.
        // TryGetQuality reads ThingWithComps.compQuality directly, so the comp
        // only has to exist on the instance — no comp initialization needed.
        internal static Thing MakeWeaponWithQuality(
            ThingDef def, QualityCategory quality, ThingDef stuff = null)
        {
            var thing = (ThingWithComps)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            thing.def = def;
            thing.stackCount = 1;
            thing.thingIDNumber = nextThingId++;
            if (stuff != null)
                thing.SetStuffDirect(stuff);

            var comp = new CompQuality();
            FieldInfo qualityField = typeof(CompQuality).GetField(
                "qualityInt", BindingFlags.Instance | BindingFlags.NonPublic);
            if (qualityField == null)
                throw new InvalidOperationException(
                    "CompQuality has no 'qualityInt' field any more; the harness cannot set quality.");
            qualityField.SetValue(comp, quality);
            thing.compQuality = comp;

            return thing;
        }

        // One-liner for the common case: a weapon whose def nothing else needs.
        internal static Thing MakeWeapon(
            string defName, TechLevel techLevel, float workToMake = 0f,
            List<ThingDefCountClass> costList = null, int costStuffCount = 0,
            ThingDef stuff = null)
        {
            return MakeWeapon(
                MakeWeaponDef(defName, techLevel, workToMake, costList, costStuffCount), stuff);
        }

        // commonality defaults to 0, the XML default, which
        // RarityMultiplierWorker prices as common (1x) — so a test only sets it
        // when the rarity multiplier is what it is measuring.
        internal static WeaponTraitDef MakeTrait(
            string defName, string label = null,
            WeaponCategoryDef category = null, float marketValueOffset = 0f,
            float commonality = 0f)
        {
            return new WeaponTraitDef
            {
                defName = defName,
                label = label ?? defName,
                weaponCategory = category,
                marketValueOffset = marketValueOffset,
                commonality = commonality,
            };
        }

        // A trait whose MarketValue statFactor marks it negative — the UMW
        // Carbonized / vanilla Ugly shape, as opposed to the marketValueOffset
        // signal MakeTrait's own parameter covers.
        internal static WeaponTraitDef MakeMarketValueFactorTrait(
            string defName, string label, float marketValueFactor, float commonality)
        {
            WeaponTraitDef trait = MakeTrait(defName, label, commonality: commonality);
            trait.statFactors = new List<StatModifier>
            {
                new StatModifier { stat = StatDefOf.MarketValue, value = marketValueFactor },
            };
            return trait;
        }

        // WeaponCategoryDefs are interned by name so a rule's category list and
        // a trait's weaponCategory can be compared — Def equality is reference
        // equality, and TraitCostRuleWorker.Matches uses List.Contains.
        internal static WeaponCategoryDef Category(string defName)
        {
            if (!Categories.TryGetValue(defName, out WeaponCategoryDef cat))
            {
                cat = new WeaponCategoryDef { defName = defName };
                Categories[defName] = cat;
            }
            return cat;
        }

        internal static TraitCostRuleDef MakeRule(
            string defName, Type workerClass, int priority,
            IEnumerable<string> labelKeywords = null, bool requireAllKeywords = false,
            IEnumerable<WeaponCategoryDef> weaponCategories = null)
        {
            var rule = new TraitCostRuleDef
            {
                defName = defName,
                workerClass = workerClass,
                priority = priority,
                requireAllKeywords = requireAllKeywords,
            };
            if (labelKeywords != null)
                rule.labelKeywords = new List<string>(labelKeywords);
            if (weaponCategories != null)
                rule.weaponCategories = new List<WeaponCategoryDef>(weaponCategories);
            return rule;
        }

        internal static List<ThingDefCountClass> Costs(params (ThingDef def, int count)[] entries)
        {
            var list = new List<ThingDefCountClass>();
            foreach ((ThingDef def, int count) in entries)
                list.Add(new ThingDefCountClass(def, count));
            return list;
        }

        // Count billed for one material, 0 when the material is absent.
        internal static int CountOf(List<ThingDefCountClass> costs, ThingDef def)
        {
            int total = 0;
            foreach (ThingDefCountClass cost in costs)
            {
                if (cost.thingDef == def)
                    total += cost.count;
            }
            return total;
        }

        // "steel 80, component 6" — readable assertion failures.
        internal static string Describe(List<ThingDefCountClass> costs)
        {
            var parts = new List<string>();
            foreach (ThingDefCountClass cost in costs)
                parts.Add((cost.thingDef?.defName ?? "<null>") + " " + cost.count);
            return parts.Count == 0 ? "<empty>" : string.Join(", ", parts.ToArray());
        }

        // Installs a rule set for the pipeline entry points. All pipeline test
        // classes share one xunit collection, so this global swap is serialized.
        internal static void UseRules(params TraitCostRuleDef[] rules)
        {
            Bootstrap();
            TraitCostUtility.SetRules(rules);
        }

        internal static void UseRules(List<TraitCostRuleDef> rules)
        {
            Bootstrap();
            TraitCostUtility.SetRules(rules);
        }

        // Installs a mod-settings instance for tests that exercise the
        // settings-backed cost knobs, restoring the previous one on dispose.
        // UWU_Mod.Settings is process-global like the rule list, and all
        // pipeline test classes share one xunit collection, so the swap is
        // serialized the same way UseRules is.
        internal static IDisposable OverrideSettings(UWU_Settings settings)
        {
            return new SettingsScope(settings);
        }

        private sealed class SettingsScope : IDisposable
        {
            private readonly UWU_Settings previous;

            internal SettingsScope(UWU_Settings settings)
            {
                previous = UWU_Mod.Settings;
                UWU_Mod.Settings = settings;
            }

            public void Dispose()
            {
                UWU_Mod.Settings = previous;
            }
        }

        // Parses the shipped rule XML rather than mirroring it in code, so the
        // pipeline tests cannot drift from what actually ships. The file is
        // copied next to the test assembly by the test csproj.
        internal static List<TraitCostRuleDef> LoadShippedRules()
        {
            string path = Path.Combine(
                Path.GetDirectoryName(new Uri(typeof(TraitCostTestHarness).Assembly.CodeBase).LocalPath)
                    ?? ".",
                "TraitCostRules.xml");
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "The shipped trait cost rules were not copied next to the test assembly.", path);

            var doc = new XmlDocument();
            doc.Load(path);

            var rules = new List<TraitCostRuleDef>();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element
                    || node.Name != "UniqueWeaponsUnbound.TraitCostRuleDef")
                    continue;
                rules.Add(ParseRule(node));
            }

            if (rules.Count == 0)
                throw new InvalidOperationException("Parsed no rules out of TraitCostRules.xml.");

            return rules;
        }

        private static TraitCostRuleDef ParseRule(XmlNode node)
        {
            var rule = new TraitCostRuleDef();

            foreach (XmlNode field in node.ChildNodes)
            {
                if (field.NodeType != XmlNodeType.Element)
                    continue;

                string text = field.InnerText.Trim();
                switch (field.Name)
                {
                    case "defName":
                        rule.defName = text;
                        break;
                    case "label":
                        rule.label = text;
                        break;
                    case "description":
                        rule.description = text;
                        break;
                    case "workerClass":
                        rule.workerClass = Type.GetType(text + ", UniqueWeaponsUnbound", throwOnError: true);
                        break;
                    case "priority":
                        rule.priority = int.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    case "requireAllKeywords":
                        rule.requireAllKeywords = bool.Parse(text);
                        break;
                    case "refundable":
                        rule.refundable = bool.Parse(text);
                        break;
                    case "costFactor":
                        rule.costFactor = float.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    case "swapFraction":
                        rule.swapFraction = float.Parse(text, CultureInfo.InvariantCulture);
                        break;
                    case "fittingsIndustrialDef":
                        rule.fittingsIndustrialDef = text;
                        break;
                    case "fittingsSpacerDef":
                        rule.fittingsSpacerDef = text;
                        break;
                    case "labelKeywords":
                        rule.labelKeywords = ParseList(field);
                        break;
                    case "weaponCategories":
                        rule.weaponCategories = new List<WeaponCategoryDef>();
                        foreach (string catName in ParseList(field))
                            rule.weaponCategories.Add(Category(catName));
                        break;
                    case "addIngredients":
                        rule.addIngredients = ParseIngredients(field);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "TraitCostRules.xml field '" + field.Name
                            + "' is not understood by the test loader; teach it the new field.");
                }
            }

            return rule;
        }

        private static List<string> ParseList(XmlNode parent)
        {
            var items = new List<string>();
            foreach (XmlNode li in parent.ChildNodes)
            {
                if (li.NodeType == XmlNodeType.Element && li.Name == "li")
                    items.Add(li.InnerText.Trim());
            }
            return items;
        }

        private static List<TraitCostIngredient> ParseIngredients(XmlNode parent)
        {
            var ingredients = new List<TraitCostIngredient>();
            foreach (XmlNode li in parent.ChildNodes)
            {
                if (li.NodeType != XmlNodeType.Element || li.Name != "li")
                    continue;

                var ingredient = new TraitCostIngredient();
                foreach (XmlNode field in li.ChildNodes)
                {
                    if (field.NodeType != XmlNodeType.Element)
                        continue;

                    string text = field.InnerText.Trim();
                    switch (field.Name)
                    {
                        case "thingDef":
                            ingredient.thingDef = text;
                            break;
                        case "fallbackDef":
                            ingredient.fallbackDef = text;
                            break;
                        case "lowDef":
                            ingredient.lowDef = text;
                            break;
                        case "industrialDef":
                            ingredient.industrialDef = text;
                            break;
                        case "spacerDef":
                            ingredient.spacerDef = text;
                            break;
                        case "count":
                            ingredient.count = int.Parse(text, CultureInfo.InvariantCulture);
                            break;
                        default:
                            throw new InvalidOperationException(
                                "TraitCostRules.xml ingredient field '" + field.Name
                                + "' is not understood by the test loader.");
                    }
                }
                ingredients.Add(ingredient);
            }
            return ingredients;
        }
    }
}
