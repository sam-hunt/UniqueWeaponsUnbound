using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueWeaponsUnbound
{
    // Who the optional crafting-skill prerequisite is evaluated against.
    public enum SkillCheckSubject
    {
        // No skill check; any pawn can customize (default).
        None,
        // The pawn ordered to perform the customization.
        CustomizingPawn,
        // Any player-faction colonist on the weapon's map.
        BestOnMap,
        // Any player-faction colonist anywhere in the world (maps, caravans,
        // travelling transporters).
        BestAnywhere,
    }

    // What the crafting-skill prerequisite demands.
    public enum SkillCheckKind
    {
        // The weapon recipe's own skill requirements; uncraftable weapons fall
        // back to a per-tech-tier crafting minimum.
        RecipeOrTechTier,
        // A flat crafting minimum from the settings slider.
        FlatMinimum,
        // Vanilla Skills Expanded's weaponsmithing expertise. Without VSE this
        // resolves to FlatMinimum at WeaponsmithFallbackLevel.
        WeaponsmithExpertise,
    }

    // The optional skill prerequisite for customization: setting resolution
    // (including the VSE fallback), per-weapon requirement derivation, and the
    // pawn/colony evaluation the entry points consume as AcceptanceReports.
    //
    // Placement in the prerequisite chain: after the research/quality checks
    // (global settings the player controls) and before pathing and the
    // workbench search (the expensive, situational checks). With the subject
    // set to None every entry here returns immediately, so players who never
    // touch the setting see no behaviour or cost change.
    public static class SkillCheckRules
    {
        // Flat crafting minimum used when the weaponsmithing option is selected
        // but Vanilla Skills Expanded is not active. 15 is the skill level VSE
        // itself requires before a pawn can take an expertise.
        public const int WeaponsmithFallbackLevel = 15;

        public const int MinFlatLevel = 0;
        public const int MaxFlatLevel = 20;

        public static bool Enabled => UWU_Mod.Settings.skillCheckSubject != SkillCheckSubject.None;

        // Whether the stored WeaponsmithExpertise selection is currently being
        // substituted by the flat fallback because VSE is unavailable.
        public static bool WeaponsmithFallbackActive =>
            UWU_Mod.Settings.skillCheckKind == SkillCheckKind.WeaponsmithExpertise
            && !VanillaSkillsExpandedIntegration.Available;

        // The kind actually in force, after the VSE fallback. flatLevel is the
        // level the FlatMinimum kind would demand (the fallback level when
        // substituting, otherwise the slider value).
        public static SkillCheckKind EffectiveKind(out int flatLevel)
        {
            UWU_Settings settings = UWU_Mod.Settings;
            if (WeaponsmithFallbackActive)
            {
                flatLevel = WeaponsmithFallbackLevel;
                return SkillCheckKind.FlatMinimum;
            }
            flatLevel = Mathf.Clamp(settings.skillCheckMinimumLevel, MinFlatLevel, MaxFlatLevel);
            return settings.skillCheckKind;
        }

        // Fallback crafting minimum for weapons with no recipe, by tech tier.
        // Derived from a survey of every craftable vanilla weapon's recipe
        // requirement (Core + all DLC, 2026-08): Neolithic median 3 / max 6,
        // Medieval 3 / 5, Industrial 5 / 7, Spacer 8 / 9. Uncraftable weapons
        // at a tier are its exotic end (mechanoid guns, loot-only arms), so each
        // tier sits at the upper end of its craftable range; Ultra and Archotech
        // have no craftable vanilla weapons at all and continue the roughly
        // +3-per-tier trend (Archotech lands on VSE's expertise threshold).
        // Animal/Undefined fall in with Neolithic, matching the research gate's
        // tier fallthrough in CustomizationRules.GetRequiredResearch.
        public static int TechTierMinimumCraftingSkill(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.Medieval:
                    return 5;
                case TechLevel.Industrial:
                    return 7;
                case TechLevel.Spacer:
                    return 9;
                case TechLevel.Ultra:
                    return 12;
                case TechLevel.Archotech:
                    return 15;
                default: // Undefined, Animal, Neolithic
                    return 4;
            }
        }

        // What a pawn must satisfy to customize one weapon under the current
        // settings. Either a list of skill requirements (vanilla's own type, so
        // recipe entries are reused as-is) or the weaponsmithing expertise.
        public sealed class Requirement
        {
            public List<SkillRequirement> Skills = new List<SkillRequirement>();
            public bool RequiresWeaponsmithExpertise;

            public bool IsEmpty => !RequiresWeaponsmithExpertise && Skills.Count == 0;
        }

        // Builds the requirement for a weapon from its base/unique defs and tech
        // level. Never null; empty when nothing is demanded (a craftable weapon
        // whose recipe has no skill requirement, like a knife or a grenade —
        // vanilla lets anyone craft those, so anyone may customize them too).
        public static Requirement GetRequirement(ThingDef baseDef, ThingDef uniqueDef, TechLevel techLevel)
        {
            var requirement = new Requirement();
            switch (EffectiveKind(out int flatLevel))
            {
                case SkillCheckKind.WeaponsmithExpertise:
                    requirement.RequiresWeaponsmithExpertise = true;
                    break;

                case SkillCheckKind.FlatMinimum:
                    AddCrafting(requirement, flatLevel);
                    break;

                default: // RecipeOrTechTier
                    RecipeMakerProperties recipeMaker = baseDef?.recipeMaker ?? uniqueDef?.recipeMaker;
                    if (recipeMaker == null)
                    {
                        AddCrafting(requirement, TechTierMinimumCraftingSkill(techLevel));
                    }
                    else if (recipeMaker.skillRequirements != null)
                    {
                        foreach (SkillRequirement sr in recipeMaker.skillRequirements)
                        {
                            if (sr?.skill != null && sr.minLevel > 0)
                                requirement.Skills.Add(sr);
                        }
                    }
                    break;
            }
            return requirement;
        }

        private static void AddCrafting(Requirement requirement, int minLevel)
        {
            if (minLevel > 0)
            {
                requirement.Skills.Add(new SkillRequirement
                {
                    skill = SkillDefOf.Crafting,
                    minLevel = minLevel,
                });
            }
        }

        // Whether one pawn meets the requirement. Skill entries use vanilla's
        // SkillRequirement.PawnSatisfies, so player-controlled mechs with a
        // fixed skill level count exactly as they do for bills.
        public static bool PawnSatisfies(Pawn pawn, Requirement requirement)
        {
            if (pawn == null)
                return false;
            if (requirement.RequiresWeaponsmithExpertise
                && !VanillaSkillsExpandedIntegration.HasExpertise(
                    pawn, VanillaSkillsExpandedIntegration.WeaponsmithDefName))
            {
                return false;
            }
            for (int i = 0; i < requirement.Skills.Count; i++)
            {
                if (!requirement.Skills[i].PawnSatisfies(pawn))
                    return false;
            }
            return true;
        }

        // The prerequisite check the entry points call. pawn may be null for a
        // pawn-independent evaluation (the ground-weapon gizmo before a colonist
        // is picked); under CustomizingPawn that defers the check to the
        // targeter, while the colony-wide subjects are answered fully. weapon
        // supplies the map for BestOnMap (MapHeld, so an equipped or carried
        // weapon resolves to its holder's map).
        public static AcceptanceReport GetReport(Pawn pawn, Thing weapon, ThingDef baseDef, ThingDef uniqueDef)
        {
            SkillCheckSubject subject = UWU_Mod.Settings.skillCheckSubject;
            if (subject == SkillCheckSubject.None)
                return true;

            TechLevel techLevel = CustomizationRules.GetWeaponTechLevel(weapon);
            Requirement requirement = GetRequirement(baseDef, uniqueDef, techLevel);
            if (requirement.IsEmpty)
                return true;

            switch (subject)
            {
                case SkillCheckSubject.CustomizingPawn:
                    if (pawn == null || PawnSatisfies(pawn, requirement))
                        return true;
                    return PawnShortfall(pawn, requirement);

                case SkillCheckSubject.BestOnMap:
                {
                    Map map = weapon.MapHeld ?? pawn?.MapHeld;
                    if (map != null && AnyColonistSatisfies(
                        map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer), requirement))
                    {
                        return true;
                    }
                    return ColonyShortfall(requirement, onMap: true);
                }

                default: // BestAnywhere
                    if (AnyColonistSatisfies(
                        PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists, requirement))
                    {
                        return true;
                    }
                    return ColonyShortfall(requirement, onMap: false);
            }
        }

        private static bool AnyColonistSatisfies(List<Pawn> pawns, Requirement requirement)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p.IsColonist && PawnSatisfies(p, requirement))
                    return true;
            }
            return false;
        }

        // Rejection text for a specific pawn: vanilla's SkillTooLow ("need
        // crafting level 15, have 10") for the first unmet skill, or the
        // expertise line.
        private static string PawnShortfall(Pawn pawn, Requirement requirement)
        {
            if (requirement.RequiresWeaponsmithExpertise
                && !VanillaSkillsExpandedIntegration.HasExpertise(
                    pawn, VanillaSkillsExpandedIntegration.WeaponsmithDefName))
            {
                return "UWU_RequiresExpertise".Translate(WeaponsmithLabel());
            }
            for (int i = 0; i < requirement.Skills.Count; i++)
            {
                SkillRequirement sr = requirement.Skills[i];
                if (!sr.PawnSatisfies(pawn))
                    return "SkillTooLow".Translate(sr.skill.label, SkillLevel(pawn, sr.skill), sr.minLevel);
            }
            return "UWU_RequiresExpertise".Translate(WeaponsmithLabel());
        }

        // Rejection text when no colonist qualifies. Names the expertise, or the
        // highest-level skill entry (the binding constraint in practice).
        private static string ColonyShortfall(Requirement requirement, bool onMap)
        {
            if (requirement.RequiresWeaponsmithExpertise)
            {
                return (onMap ? "UWU_NoColonistOnMapWithExpertise" : "UWU_NoColonistWithExpertise")
                    .Translate(WeaponsmithLabel());
            }
            SkillRequirement binding = requirement.Skills[0];
            for (int i = 1; i < requirement.Skills.Count; i++)
            {
                if (requirement.Skills[i].minLevel > binding.minLevel)
                    binding = requirement.Skills[i];
            }
            return (onMap ? "UWU_NoColonistOnMapWithSkill" : "UWU_NoColonistWithSkill")
                .Translate(binding.skill.label, binding.minLevel);
        }

        // Mouse-attached line for the ground-weapon targeter under the
        // CustomizingPawn subject: the hovered pawn's level against the
        // requirement (or their expertise status), so the player can pick a
        // qualified colonist without guessing. Empty when nothing applies.
        // failing reports whether the pawn would be rejected, for colouring.
        public static string GetTargeterTip(Pawn pawn, Requirement requirement, out bool failing)
        {
            failing = false;
            if (pawn == null || requirement?.IsEmpty != false)
                return null;

            if (requirement.RequiresWeaponsmithExpertise)
            {
                bool has = VanillaSkillsExpandedIntegration.HasExpertise(
                    pawn, VanillaSkillsExpandedIntegration.WeaponsmithDefName);
                failing = !has;
                return (has ? "UWU_TargeterHasExpertise" : "UWU_TargeterLacksExpertise")
                    .Translate(WeaponsmithLabel());
            }

            // Show every skill entry; the first unmet one decides the colour.
            string tip = null;
            for (int i = 0; i < requirement.Skills.Count; i++)
            {
                SkillRequirement sr = requirement.Skills[i];
                if (!sr.PawnSatisfies(pawn))
                    failing = true;
                string line = "UWU_TargeterSkill".Translate(
                    sr.skill.LabelCap, SkillLevel(pawn, sr.skill), sr.minLevel);
                tip = tip == null ? line : tip + "\n" + line;
            }
            return tip;
        }

        // The pawn's displayed level in a skill: the skill record's level, or a
        // player mech's fixed level, or 0 when the pawn has neither.
        private static int SkillLevel(Pawn pawn, SkillDef skill)
        {
            SkillRecord record = pawn.skills?.GetSkill(skill);
            if (record != null)
                return record.Level;
            if (pawn.IsColonyMechPlayerControlled)
                return pawn.RaceProps.mechFixedSkillLevel;
            return 0;
        }

        private static string WeaponsmithLabel()
        {
            return VanillaSkillsExpandedIntegration.ExpertiseLabel(
                VanillaSkillsExpandedIntegration.WeaponsmithDefName,
                "UWU_WeaponsmithingFallbackLabel".Translate());
        }
    }
}
