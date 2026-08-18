# Spanish (Castellano) — Unique Weapons Unbound glossary

Machine-assisted generation, 2026-07-29; no native review yet. Family-shared
mechanics (Castilian vs. Latin American folder naming, the `de`/`a` +
`[X_definite]` contraction trap, gender-agreement restructuring,
`[RECIPIENT_possessive]` singular-only, `RulePackDef` parallel-gender-family
technique, and vanilla-grounded common vocabulary — including materials,
tech-level enum labels, relic/ideoligion, disarm, Structure, research tree,
and the button/quality-tier rows) live in the `l10n/` submodule at
`l10n/languages/Spanish.md` — this file holds only what is specific to
Unique Weapons Unbound.

## Weapon-trait word check (mandatory per repo — see SKILL.md)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | rasgo / rasgos (standalone: Rasgos del arma) | característica | Odyssey `WeaponTraits` = Rasgos, `Stat_ThingUniqueWeaponTrait_Label` = Rasgos, `StatsReport_WeaponTraits` = Rasgos del arma. **es splits like ja/ko do**: `Stat_Thing_PersonaWeaponTrait_Label` = *Características* is the Royalty **persona**-weapon word and belongs to PersonaWeaponsUnbound, not here. Core's pawn-trait `<Traits>` is also Rasgos, so es shares the pawn word (like de/ja, unlike ru) |
| unique weapon | arma única | arma exclusiva | Odyssey `UniqueWeapon` = Arma única |

## UWU domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| customize | personalizar / personalización | adaptar, configurar | Core `CustomizeIdeoligion` = Personalizar ideoligión; float menu follows `Equip` = Equipar {0}, i.e. infinitive + **bare** label |
| charge (weapons) | **carga(s)** root: fusil de cargas, lanza de cargas | impulso-, energía- | Core `Gun_ChargeRifle` = fusil de cargas, `Gun_ChargeLance` = lanza de cargas, `Bullet_ChargeRifle` = disparo de cargas. es keeps "charge" literal like zh-Hans, where de switched to Impuls- and ru to энерг- — never extrapolate between languages |
| pulse-charged munitions (ChargedShot) | municiones de pulsos | | Core research label |
| beam (weapons) | armas de rayos | | Core `BeamWeapons` research, `Gun_BeamRepeater` = repetidor de rayos. **But the DamageDef `Beam.label` = láser** — point each def at the right one |
| Smithing (the **skill**/WorkType) | Forja | herrería | `WorkTypeDef Smithing` = Forja while `ResearchProjectDef Smithing` = herrería — same defName, two defs, two words. Check the def type |
| tech level (the concept) | nivel tecnológico | nivel de tecnología | Core `TechLevelTooLow` (7 hits, 0 for the alternative) |
| "{0} quality or better" | `requiere calidad {0} o mejor` | | reshaped from Core `NormalQualityOrBetter` = calidad normal o mejor (pre-inflected, untemplatable) |
| herbal medicine | hierbas medicinales | medicina herbal | Core `MedicineHerbal` |
| bioferrite / thrumbofur / birdskin / steel slag chunk | bioferrita / piel de trumbo / pellejo de pájaro / escombro metálico | trumbofur, cuero de ave | Anomaly `Bioferrite`; Core `Leather_Thrumbo`, `Leather_Bird`, `ChunkSlagSteel` |
| burst speed / burst count / stopping power | Cadencia de tiro / Tiros por ráfaga / Potencia de parada | | Core `BurstShotFireRate`, `BurstShotCount`, `StoppingPower` |
| armor penetration / damage | Penetración de blindaje / Daño | | Core `ArmorPenetration`, `Damage` |
| ignores accuracy penalties | ignora las penalizaciones de precisión | | Odyssey `AimAssistance.description` — reuse verbatim |
| Traders will pay more / less for it. | Los comerciantes pagarán más por ella. / Los comerciantes pagarán menos por ella. | | Odyssey `GoldInlay` / `Ugly` descs — reuse verbatim (feminine `ella`, agreeing with *arma*) |
| inlay / grip / ornamental / lightweight / cumbersome / ugly | incrustación / empuñadura / ornamental / ligero / torpe / feo | | Odyssey trait labels. **es has a real noun for inlay** (incrustación de oro / de jade), unlike de which only has adjectives |
| tox / incendiary / EMP rounds | balas tóxicas / balas incendiarias / balas PEM | balas EMP | Odyssey `ToxRounds`, `IncendiaryRounds`, `EMPRounds` |
| EMP (the acronym) | **PEM** | EMP | `EMP.label` = PEM — es localizes the acronym itself. High-frequency trap |
| cut / stab (**DamageDef** label) | corte / apuñalamiento | herida, corte profundo | Core DamageDefs; the `labelNoun`s are `un corte` / `una puñalada`, a different field |
| stun / toxic buildup / flare | aturdir (state: aturdido) / acumulación tóxica / bengala | | Core `Stun`, `ToxicBuildup`; Anomaly `DisruptorFlare` = bengala disruptora |
| carrying capacity | capacidad de carga | | Core `CarryingCapacity` |
| ingredients / quality / effects / prerequisites | Ingredientes / Calidad / Efectos / Prerrequisitos | | Core Keyed |
| (none) / Free | (nada) / Gratis | Nada, Sin coste | Core `NoneBrackets`, `CommandCallRoyalAidFreeOption` |
| Warning / log / Progress | Advertencia / registro / Progreso | | Core `Warning`, `OpenLogOnWarnings` (= Abrir registro …), `Progress` |
| Miscellaneous | Varios | Misceláneo | 15 hits vs 0 |
| float menu / right-click / button / select | menú contextual / hacer clic derecho / botón / seleccionar | menú flotante | Core `AddBillSimpleMeal.text` uses all of these |
| appearance / colour | Apariencia / Color | | Core `Appearance`, `Color` |
| ability / cooldown | Habilidades / Enfriamiento | | Core Keyed |
| techprint | tecnoplano | tecnoimpresión | Core `TechprintLabel` |
| wielder | portador | usuario, empuñador | 62 hits across weapon-trait descs |
| sealed / fogged | sellado / en la niebla | | Odyssey `AncientSealedCrate` = caja sellada; niebla for map fog |
| scrap | chatarra | desechos | vanilla namer rules (planeta chatarra, señor de la chatarra) |

Two workbench nuances beyond the shared `l10n/languages/Spanish.md` row for
smithy/machining/fabrication/workbench: `AncientTableMachining` = mesa de
**mecanizado** — the ancient-ruin variant only, don't generalize from it —
and vanilla itself is inconsistent about the fabrication bench: the
blueprint label says mesa de fabricación while `Fabrication.description`
says mesas de ensamblado, but the *thing* label (mesa de ensamblaje) is
authoritative.

## Research `generalRules` — UWU's three research defs

**es research `generalRules` define a symbol English never does:
`subject_def`.** All 90 vanilla es research entries carry it — the "of the X"
form with article, gendered to the head noun: `subject->herrería` /
`subject_def->de la herrería`, `maquinado` / `del maquinado`, `mesas de
ensamblado…` / `de los bancos de ensamblado…`. This is the es analogue of
German's 13-symbol case paradigm, and exactly why the cross-language lesson
in `l10n/lessons.md` says to diff the target language's entry for the *same
def* before translating.

Two further es-specific facts about that field:

- **`subject_gerund` is an INFINITIVE in es, not a gerund** — vanilla writes
  `forjar`, `maquinar`, `construir equipos de maquinado`, `desarrollar proyectos de
  alta tecnología`. Never `forjando`.
- `subject_story` is a subject-less **third-person preterite** clause (`se estableció
  en una aldea medieval y dominó …`, `construyó en secreto un arsenal …`), which
  happens to match the English shape — unlike de (verb-final), ko (polite 했습니다)
  or ja (plain した).

`subject_def` currently has **no consumer**: the only rulepack referencing the
`subject` family is `Core/.../RulePacks_Book_Descriptions.xml`, whose es translation
is still entirely untranslated English and contains no `[subject_def]`, and there is
no `priority=-1` fallback for it anywhere. Supplying it is therefore zero-risk today
(extra symbols are legal, list injections replace the whole list, and the checker
skips list-valued entries) and correct if that rulepack is ever translated — so
UWU's three research defs ship it, matching vanilla es rather than being the only
defs missing it.

Mod-decided terms pending native review: the research trio **herrería / maquinado /
fabricación de armas únicas** (vanilla lowercase stem + "de armas únicas"), with
`subject_def` forms de la / del / de la respectively; haul planner modes
**Secuencial / Barrido / Exhaustivo**; haul plan **plan de transporte** and section
header **Transporte de ingredientes**; net refund/cost **Reembolso neto / Coste
neto** (vanilla es has **no** word for refund at all — `reembolso` and `devolución`
are both 0 hits, matching de/ko/zh); texture tab **Textura** (Material is taken —
it is `Stat_Stuff_Name`); vanilla-behavior suffix **(juego base)** and the matching
prose "en el juego base" (`juego base` is 0 hits in vanilla, but `vanilla` is 1, so
neither is established); progression header **Progreso**; gizmo button **botón de
orden**; "must disarm from hostile" **Hay que quitarle el arma a un hostil** (see
the `desarmar` landmine in `l10n/languages/Spanish.md`); **Flarestriker** and
**Akimbo** kept in Latin script (neither is a vanilla trait); flare launcher
**lanzabengalas**; weapon def **def del arma** (kept Latin, as ja, zh, ko and de do).
