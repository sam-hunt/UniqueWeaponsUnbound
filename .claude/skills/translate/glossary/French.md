# French — Unique Weapons Unbound glossary

Machine-assisted generation, 2026-07-29; no native review yet. Family-shared
mechanics (`LanguageWorker_French`'s five elision regexes, the `de le`→`de`
bug, h-initial noun hazard, quote-mark slotting, dash/guillemet
tree-wide-vs-Keyed-only counting, `[X_possessive]` being structurally wrong
in French, and vanilla-grounded common vocabulary — including
trader/settlement/gravship terms, market value, quality tiers, and the
Cancel/Reset/Confirm button row) live in the `l10n/` submodule at
`l10n/languages/French.md` — this file holds only what is specific to
Unique Weapons Unbound.

## Weapon-trait word check (mandatory per repo — see SKILL.md)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | trait / traits (standalone: Traits d'arme) | caractéristique | Odyssey `WeaponTraits` = Traits d'arme, `StatsReport_WeaponTraits` = Traits d'armes (note vanilla's own singular/plural inconsistency). **fr does not split persona vs unique**: `Stat_Thing_PersonaWeaponTrait_Label` and `Stat_ThingUniqueWeaponTrait_Label` are both plain *Traits*, as in de. No collision with pawn traits either — the bio-tab header `<Traits>` is *Éléments marquants :* |
| unique weapon | arme unique | | Odyssey `UniqueWeapon` = Arme unique |

## UWU domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| customize | personnaliser / personnalisation | adapter, configurer | Core `CustomizeIdeoligion` = Personnalisez votre idéoligion; float menu follows `Equip` = Équiper {0} and `ExtractRelic` = Extraire {0}, i.e. infinitive + **bare** label |
| **charge (weapons)** | **à impulsion** (fusil à impulsion) | — | **Landmine: fr is internally inconsistent.** `Gun_ChargeRifle` = fusil **à impulsion** (like de's Impuls-), but `Gun_ChargeLance` = lance-**charges** and the `ChargedShot` research = tir **chargé** (like es/zh keeping it literal). Anchor on the rifle and look up every charge-family def individually — no single root covers them |
| pulse-charged munitions (ChargedShot) | tir chargé | | Core research label |
| beam (weapons) | à faisceau (armes à faisceau) | laser, rayon | Core `BeamWeapons`, `Gun_BeamRepeater` = répétiteur à faisceau |
| **fueled / electric smithy** | **établi de forgeron à combustible / électrique** | forge, frague | Core building labels. The `Smithing` **research** is *forge*, so building and research diverge — as in es |
| **machining table** | **établi d'assemblage** | établi d'usinage | Core `TableMachining`. **Landmine:** the `Machining` research is *usinage*, and the building's word *assemblage* also shows up in the `Fabrication` research name (below). The three-way scramble is worse than in any other language checked — never reuse a stem, look up each def |
| **fabrication bench** | **atelier de fabrication** | | Core `FabricationBench`. Meanwhile the `Fabrication` **research** is *assemblage de composant* and `AdvancedFabrication` is *fabrication avancée* |
| workbench (**generic**) | **établi** | plan de travail, poste de travail | 131 hits vs 3 and 1. `établi` is also the head noun of nearly every specific bench (établi de forgeron / d'assemblage / de brassage / de boucher / de sculpture / de tailleur de pierre / de tissage), **but nothing is labelled exactly `établi`** — so unlike German's Werkbank, there is no collision and the generic use is safe and idiomatic |
| smithing / machining / fabrication (research stems) | forge / usinage / fabrication (advanced: fabrication avancée) | | Core research labels |
| tech level (the concept) | niveau technique | | `TechLevelTooLow` uses it twice for the mechanical gating sense; `ResearchCostComparison` says *niveau technologique*. Near-tied (2 vs 3) — either is defensible, this repo uses the gating one |
| tech levels (the **enum labels**) | néolithique / médiéval / industriel / spatial / ultra / archotech | ultratech, spatial-era | Core `TechLevel_*`; ultratech is plain **ultra** and archotech stays **archotech** |
| "{0} quality or better" | `nécessite la qualité "{0}" ou mieux` | | reshaped from `NormalQualityOrBetter` = qualité normale ou mieux, which is pre-inflected feminine and untemplatable; quote the placeholder so it doesn't have to agree |
| plasteel | **plastacier** | plastier, plastacié | Core `Plasteel` |
| chemfuel | **chemfuel** (untranslated) | biocarburant, carburant chimique | Core `Chemfuel` — fr leaves it in English, unlike de (Sprit) and es (biocombustible) |
| steel / wood / gold / uranium / jade | acier / bois / or / uranium / jade | | Core labels (`WoodLog` = bois) |
| components / advanced components | composant / composant avancé | pièces | `ComponentIndustrial`, `ComponentSpacer` (both singular labels) |
| herbal medicine | herbe médicinale | médecine à base de plantes | Core `MedicineHerbal` |
| bioferrite / thrumbofur / birdskin / steel slag chunk | bioferrite / fourrure de thrumbo / cuir d'oiseau / débris d'acier | | Anomaly `Bioferrite`; Core `Leather_Thrumbo`, `Leather_Bird`, `ChunkSlagSteel` |
| burst speed / burst count / stopping power | Cadence de tir / Nombre de tirs par rafale / Puissance d'arrêt | | Core `BurstShotFireRate`, `BurstShotCount`, `StoppingPower` |
| armor penetration | Pénétration d'armure | | Core `ArmorPenetration` |
| ignores accuracy penalties | ignore les pénalités de précision | | Odyssey `AimAssistance.description` — reuse verbatim |
| Traders will pay more / less for it. | Les commerçants en paieront un prix plus élevé. / Les commerçants en paieront moins cher. | | Odyssey `GoldInlay` / `Ugly` descs — reuse verbatim |
| inlay / grip / ornamental / lightweight / cumbersome / ugly | incrusté d'or·de jade / poignée personnalisée / ornemental / léger / encombrant / laid | | Odyssey trait labels — note fr renders the inlays as **participial adjectives**, like de; the noun is *incrustation* |
| tox / incendiary / EMP rounds | munitions toxiques / munitions incendiaires / munitions IEM | munitions EMP | Odyssey `ToxRounds`, `IncendiaryRounds`, `EMPRounds` |
| **EMP (the acronym)** | **IEM** | EMP | `EMP.label` = IEM, `Bullet_Shell_EMP` = obus à impulsion électromagnétique. fr localizes the acronym, as es does with PEM — ja, zh, ko and de all keep EMP |
| cut / stab (**DamageDef** label) | taillade / **blessure par lame** | perforation for stab | Core DamageDefs. The **HediffDef** `Stab.label` is *perforation* and `Stab.labelNoun` *un coup de lame* — Cut happens to match (taillade) but Stab does not; point each def at the right one |
| stun / toxic buildup / flare | étourdissement (state: étourdi) / accumulation toxique / fusée | | Core `Stun`, `StunLower`, `ToxicBuildup`; Anomaly `DisruptorFlare` = fusée disruptive |
| relic | relique (relic of X = relique de X) | | Ideology `IdeoRelic`, `ExtractRelic` |
| ideoligion | **idéoligion** | idéologie | Ideology `IdeoligionOf` = Idéoligion de, `ReformIdeoligion` = Réformer l'idéoligion. **fr coins the portmanteau**, like ru and es — unlike de/ja/zh/ko |
| **disarm** | phrase with **prendre à** (pris à un hostile) | désarmer (as the verb of the weapon) | `DisarmedTime` = Désarmé is the only weapon sense, and `désarm*` is 3 hits total. Unlike es, there is **no** collision — Deconstruct = démanteler, Uninstall = désinstaller — but French `désarmer` takes the *person* as its object, so it reads wrong with a weapon subject |
| haul / carrying capacity | transport / capacité de transport | acheminer | Core `Haul.label`, `CarryingCapacity` |
| forbidden / cannot reach / reserved by | interdit / impossible d'atteindre / réservé par | | Core `ForbiddenLower`, `CannotReach`, `IsReservedBy` |
| hostile / enemy / colonist | Hostile / ennemi / colon | personnage for pawn | Core `Hostile`, `Enemy`, `Colonist`; in prose *colon* (181) beats *personnage* (70) |
| Structure (architect category) | Structure | | `Structure.label` — singular here, unlike de and es which both pluralize |
| ingredients / quality / effects / prerequisites | Ingrédients / Qualité / Effets / Prérequis | | Core Keyed |
| Randomize | Aléatoire | | Core buttons |
| (none) | (Rien) | | Core `NoneBrackets`. `None` is **feminine** — reinflect it when it follows a masculine noun of your own (this repo's refund line uses *Aucun*) |
| Warning / log / Progress | Avertissement / journal / Progression | | Core `Warning`, `OpenLogOnWarnings`, `Progress` |
| Miscellaneous | Divers | Autres | `MiscRecordsCategory` |
| float menu / right-click / button / select | menu contextuel / clic droit / bouton / sélectionner | menu flottant | *menu contextuel* 4 hits, *menu flottant* 0; Core prose uses "Faites un clic droit" |
| appearance / colour | Apparence / Couleur | | Core `Appearance`, `Color` |
| quest / save | Quête / sauvegarde | | Core Keyed |
| techprint | schéma technique | | Core `TechprintLabel` |
| wielder | porteur | utilisateur | weapon-trait descs |
| Crafting (the skill) | Artisanat | | Core `Crafting.label` |
| sealed / fogged | scellé / brouillard | | Odyssey `AncientSealedCrate` = caisse scellée; *brouillard* for map fog |
| melee weapon names | épée longue / lance / masse / couteau / glaive / hache / massue / épée mono-moléculaire / épée plasmique | | `MeleeWeapon_*` — native words, not transliterations |

`Reset to defaults` = **Réinitialiser les valeurs par défaut** (matches the
`l10n/` row) disambiguates the same way de's `ResetBinding` phrasing does:
`RestoreToDefaultSettings` = *Utiliser les paramètres par défaut* reads as
applying the defaults, not resetting them, so the reset phrasing is the only
one that says what our button does.

## Research `generalRules` — UWU's three research defs

**fr research `generalRules` keep the English symbol set but BAKE THE DEFINITE ARTICLE
into `subject` and `subject_gerund`** — `Smithing` → `subject->le forgeage`,
`Machining` → `l'usinage`, `Fabrication` → `l'atelier hi-tech de fabrication`,
`AdvancedFabrication` → `la fabrication de composants avancés`. This is fr's answer to
the problem es solved by *adding* a `subject_def` symbol, and it means the article is
**mandatory, not optional**: unlike es, the consumer is live. fr's translated
`Core/.../RulePacks_Book_Descriptions.xml` carries 111 live `[subject]` references
written to supply no article of their own (`sur [subject]`, `axé sur [subject]`,
`pour [subject_primary_gerund]`), so an unarticled subject renders ungrammatically.
UWU's three defs therefore ship articled subjects. Two further notes:

- **`subject_gerund` is an articled noun phrase in fr, not a gerund** — vanilla writes
  `le forgeage`, `la construction d'équipements d'usinage`. Never `forgeant`.
- `subject_story` is a subject-less **passé composé** clause (`a construit les forges
  dans un monde avancé...`, `s'est installé dans un hameau médiéval et a appris à...`),
  defaulting to masculine agreement. Shape matches en and es; differs from de
  (verb-final) and ko (polite `했습니다`).
- That rulepack also invents **gender-variant symbols** (`image_typesMasculine`,
  `notable_adjectiveFemininePlural`, `schematic_synonymPlural`) — fr handles agreement
  by duplicating rules per gender rather than with `|adj|` markers as de does.

Mod-decided terms pending native review: the research trio **forge / usinage /
fabrication d'armes uniques**, with subjects **le forgeage / l'usinage / la fabrication
d'armes uniques** (note the label uses *forge* but the subject uses vanilla's own
subject word *forgeage*; the third tier borrows *fabrication* from
`FabricationBench`/`AdvancedFabrication` rather than the `Fabrication` research's
unrecognizable *assemblage de composant*); haul planner modes **Séquentiel / Balayage /
Approfondi**; haul plan **plan de transport** and section header **Transport des
ingrédients**; net refund/cost **Remboursement net / Coût net** (vanilla fr has **no**
word for refund, matching de/es/ko/zh); texture tab **Texture**; vanilla-behavior
suffix **(jeu de base)** and the matching prose "dans le jeu de base" (*jeu de base* is
0 hits in vanilla, and *vanilla* appears only in `Core.label` = "Core/Vanilla", so
neither is established); progression header **Progression**; gizmo button **bouton de
commande**; research tree **arbre de recherche** (`ResearchScreen` = Écran de recherche
offers no tree); flare launcher **lance-fusées**; **Flarestriker** and **Akimbo** kept
in Latin script; weapon def **def** (kept Latin, as ja, zh, ko, de and es do).
