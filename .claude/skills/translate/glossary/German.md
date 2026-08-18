# German — Unique Weapons Unbound glossary

Machine-assisted generation, 2026-07-28 (re-ran every preseeded row from
PersonaWeaponsUnbound against the de tars and confirmed them); no native
review yet. Family-shared mechanics (case vs. gender, the `lookup`/`decline`
mechanism, `PostProcessed`'s trailing-`'s` rewrite, stuff-naming inversion,
and vanilla-grounded common vocabulary — including materials like
Plastahl/Uran/Gold/Stahl/Holz, quality tiers, tech-level enum labels,
relic/ideoligion, and the Cancel/Reset/Confirm/None button rows) live in
the `l10n/` submodule at `l10n/languages/German.md` — this file holds only
what is specific to Unique Weapons Unbound.

## Weapon-trait word check (mandatory per repo — see SKILL.md)

**This row does not transfer from the Russian glossary.** German has no
split: Odyssey's `Stat_ThingUniqueWeaponTrait_Label`, Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` **and** Core's pawn-trait `<Traits>`
are all **Merkmale**. Where context doesn't make the weapon clear, use
vanilla's own `StatsReport_WeaponTraits` = **Waffenmerkmale**. Run the
lookup anyway — just expect it to agree.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | Merkmal / Merkmale (standalone: Waffenmerkmale) | Eigenschaft, Attribut | Odyssey `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits` |
| unique weapon | einzigartige Waffe | Unikat, besondere Waffe | Odyssey `UniqueWeapon` |

## UWU domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| charge (weapons) | **Impuls-** root: Impulsgewehr, Impulslanze, Impulsschuss | Ladungs-, Lade- | Core `Gun_ChargeRifle`, `Gun_ChargeLance`, `Bullet_ChargeRifle` — exactly parallel to the RU энерг-/заряд- row |
| fueled / electric smithy | Schmiede / elektr. Schmiede | Schmiedeofen, Esse | Core `FueledSmithy`, `ElectricSmithy` — vanilla abbreviates "elektr." |
| machining table | **Werkbank** | Maschinentisch, Drehbank | Core `TableMachining` — landmine: de reuses the generic word Werkbank for this *specific* bench, so avoid Werkbank for a generic "workbench" in any string that also names this one |
| fabrication bench | Fabrikationstisch | Fertigungstisch | Core `FabricationBench.label` |
| workbench (**generic**) | **Arbeitsstation** | Werkbank, Werktisch, Arbeitstisch | resolves the Werkbank landmine above: vanilla uses Arbeitsstation wherever English says "work tables"/"workstation" generically (`Armchair`, `Stool`, `DiningChair`, `Crafting.description`, `SubcoreEncoder`). Werktisch and Arbeitstisch are 0 hits. "created at" = `CreatedAt` = Produktionsstätte(n) |
| customize | anpassen / Anpassung | individualisieren, personalisieren | Core `CustomizeIdeoligion` = Ideologie anpassen — object + infinitive, so `{0} anpassen` for the float menu |
| smithing / machining / fabrication (research stems) | Schmieden / Maschinenbau / Fabrikation (advanced: Hightech-Fabrikation) | | Core `Smithing`, `Machining`, `Fabrication`, `AdvancedFabrication` |
| advanced components | Hightech-Bauteile | fortschrittliche Komponenten | Core `ComponentSpacer.label`; plain components are Bauteile (`ComponentIndustrial`, singular label) |
| chemfuel | **Sprit** | Chemtreibstoff, Chemikalientreibstoff | Core `Chemfuel.label` — counterintuitive, always check |
| herbal medicine / jade | Kräutermedizin / Jade | Heilkräuter | Core labels |
| bioferrite / thrumbofur / birdskin / steel slag chunk | Bioferrit / Thrumbofell / Vogelleder / Stahlschrott | | Anomaly `Bioferrite`; Core `Leather_Thrumbo`, `Leather_Bird`, `ChunkSlagSteel` |
| burst speed / burst count / stopping power | Feuerrate / Schüsse pro Feuerstoß / Mannstoppwirkung | | Core Keyed `BurstShotFireRate`, `BurstShotCount`, `StoppingPower` |
| ignores accuracy penalties | Genauigkeitsverlust ignorieren | Präzisionsstrafe | Odyssey `AimAssistance.description` — reuse "ignoriert den Genauigkeitsverlust" |
| beam (weapons) | Strahlen- root: Strahlenwaffen, Strahlen-Repeater | Laser-, Balken- | Core `BeamWeapons` research, `Gun_BeamRepeater`; the DamageDef `Beam.label` = Strahl |
| pulse-charged munitions (ChargedShot) | Impulsmunition | | Core research label — the same Impuls- root as the weapons |
| inlay / grip / ornamental / lightweight / cumbersome / ugly | vergoldet·jadeverziert / Spezialgriff / verziert / leicht / unhandlich / hässlich | | Odyssey trait labels — note de renders the two inlays as **adjectives**, not an "inlay" noun |
| tox / incendiary / EMP rounds | Giftmunition / Brandmunition / EMP-Munition | | Odyssey `ToxRounds`, `IncendiaryRounds`, `EMPRounds` |
| cut / stab (**DamageDef** label) | Schnitt / Stich | Schnittwunde, Stichwunde | Core DamageDefs; the **HediffDef** `labelNoun`s differ (`ein Schnitt`, `eine Stichwunde`) |
| haul / carrying capacity | Tragen / Tragekapazität | Transportieren | Core `Haul.label`, `CarryingCapacity` |
| ingredients | Zutaten | Bestandteile, Komponenten | Core `Stat_Recipe_Ingredients_Desc` (80 hits) |
| Structure (architect category) | **Strukturen** | Struktur | `Structure.label` (DesignationCategoryDef) is plural; the bare singular is the Keyed tab string |
| forbidden / cannot reach / reserved | verboten / nicht erreichbar / reserviert | | Core `ForbiddenLower`, `CannotReach`, `IsReservedBy` |
| Effects / Prerequisites | Effekte / Voraussetzungen | | Core `Effects`; `Prerequisites` = Voraussetzung(en), so use the plain plural for a section header |
| select / button / right-click / log | selektieren / Schaltfläche / rechtsklicken, Rechtsklick / Log | anklicken, Knopf | Core Keyed prose, `OpenLogOnWarnings` |
| float menu | Kontextmenü | Schwebemenü, Auswahlmenü | Core `AddBillSimpleMeal.text` |
| pawn (in prose) | Person / Personen | Pawn, Figur | Core `ConfirmForceDepartPawnsNotLeaving` and friends |
| fogged / sealed / save file / mod | im Nebel / versiegelt / Speicherdatei / Mod | | Core prose |
| "{0} quality or better" | `Qualität {0} oder besser` | | reshaped from Core `NormalQualityOrBetter` (pre-inflected, untemplatable) |
| ultratech / archotech (**attributive, in prose**) | Ultratech- / Archotech- (Ultratech-Waffe, Archotech-Struktur) | | vanilla prose does compound with these (`Ultratech-Signalstörer`, `Ultratech-Gerät`), so the "never Ultratech" ban applies only to the `TechLevel_*` enum labels, where it is plain **Ultra** |
| colour / appearance | Farbe / Erscheinung | Aussehen | Core `Color`, `Appearance` |
| Crafting (the skill) | Handwerk | Herstellung, Basteln | Core `Crafting.label` |
| bill / recipe (both) | Auftrag | Rezept, Rechnung | Core `TabBills`, `AddBill`, every `Stat_Recipe_*_Desc` — de collapses the two |
| colonist / research project | Kolonist / Forschungsprojekt | | Core `Colonist`, `NeedResearchBenchDesc` |
| wielder | Träger | Anwender | Royalty weapon-trait descs |
| techprint | Techplan / Techpläne | Techdruck, Blaupause | Core `TechprintLabel` |

## Research `generalRules` — UWU's three research defs

**Where case *is* solvable: research `generalRules`.** Unlike `.Translate()`
strings, rulepack injections go through the full resolver, and vanilla de
exploits that — every one of its 14 research defs expands the English
2-symbol `subject`/`subject_gerund` set into a **13-symbol case paradigm**,
with inline `|F|`/`|M|`/`|N|` gender and `|adj|` adjective-ending markers
(1704 `|adj|` occurrences across the de data). The symbol names are fixed:
`subject`, `subject_gender_nom_indef`, `subject_with_of{,_and_adj}`,
`subject_with_to{,_and_adj}`, `subject_with_the_dat{,_and_adj_dat}`,
`subject_with_the_acc{,_and_adj_acc}`, `subject_{dat,acc}_indef_zero`, plus
`subject_gerund{,_with_of,_with_to,_acc,_dat}`. The consumer is
`de/Core/DefInjected/RulePackDef/RulePacks_Book_Descriptions.xml` (book
descriptions cite research subjects).

Supplying the paradigm is optional but worth it: that rulepack ships a
`priority=-1` fallback for **every** symbol, so a mod def that defines only
`subject`/`subject_gerund` still resolves — except
`subject_gender_nom_indef(priority=-1)->|N|[subject]`, which silently defaults to
**neuter** and is simply wrong for a feminine or masculine subject noun. UWU's
three research defs therefore ship the full paradigm, copied from vanilla's own
per-gender pattern (feminine: `der/zur/die` + `|adj|en`/`|adj|e`; masculine:
`des …s/zum/dem/den` + `|adj|en`). Two authoring tricks make the declension
mechanical rather than a judgment call:

- Put the **head noun first** and keep an invariant prepositional tail, so only
  the article and head noun inflect: `Schmiedekunst für einzigartige Waffen` →
  `der Schmiedekunst für einzigartige Waffen`. Never a genitive tail that would
  itself need to agree.
- `<li>` list injections replace the whole list, so extra symbols beyond the
  English set are legal, and the checker skips placeholder/staleness comparison
  for list-valued entries (`en_text` is `None`) — it will not flag the longer list.

The `subject_story` register is a **subject-less, verb-final past-tense
subordinate clause** (`Gammastrahlenwaffen herstellte`, `sich in einem
mittelalterlichen Dörfchen niederließ`). Only 1 of the 14 vanilla defs uses
`[ANYPAWN_pronoun]`; don't rely on it. Note this differs from ko (polite
`했습니다`) and ja (plain `した`) for the same field — the per-language rule holds.

Also note `LanguageWorker_German.PostProcessThingLabelForRelic` truncates a
weapon label to its bare weapon noun via `EndsWith` against a hardcoded 26-noun
list (Horn, Lanze, Pulser, Werfer, Axt, Flinte, Bogen, Revolver, Gewehr,
Stoßzahn, Stab, Hammer, Schwert, Pistole, Dolch, Büchse, Kanone, Granaten,
Granate, Keule, Säbel, Messer, Rapier, Klinge, Sense, Speer), falling back to the
substring after the last space or hyphen. Relevant wherever this mod surfaces a
relic name from a weapon label; note Waffe is *not* on the list.

Mod-decided terms pending native review: the research trio **Schmieden /
Maschinenbau / Fabrikation einzigartiger Waffen** (vanilla stem + genitive; the
middle one is the weakest literally, since Maschinenbau means mechanical
engineering, but stem recognizability was judged worth more than precision) with
subject nouns **Schmiedekunst / Maschinenbau / Fabrikation für einzigartige
Waffen**; haul planner modes **Sequenziell / Sammelnd / Gründlich**; haul plan
**Transportplan** and section header **Zutatentransport** (reshaped from
`Haul.label` = Tragen, since Trageplan is not a recognizable German word); net
refund/cost **Nettoerstattung / Nettokosten** (vanilla de has no word for refund
at all); texture tab **Textur**; vanilla-behavior suffix **(Grundspiel)** and the
matching prose "im Grundspiel"; inlay as a **noun** **Einlegearbeit** (vanilla
only has the adjectives vergoldet/jadeverziert); **Flarestriker** and **Akimbo**
kept in Latin script (neither is a vanilla trait); flare launcher
**Fackelwerfer** (from `DisruptorFlare` = Disruptorfackel); progression header
**Fortschritt**; gizmo button **Befehlsschaltfläche**; research tree
**Forschungsbaum** (0 vanilla hits); weapon def **Waffen-Def** (kept Latin, as
ja, zh and ko do); spacer-tech trait **Merkmale der Raumfahrtstufe** (2026-08-02,
reuses the shipped "Waffen der Raumfahrtstufe" pattern from
`UniqueFabrication`'s customUnlockTexts rather than coining a compound).

Unrelated to German but worth remembering during any generation here: this
repo's `DefInjected/UniqueWeaponsUnbound.TraitCostRuleDef/` folder is
namespace-qualified because the def class is the mod's own. A bare
`TraitCostRuleDef` folder silently drops every entry in it.
