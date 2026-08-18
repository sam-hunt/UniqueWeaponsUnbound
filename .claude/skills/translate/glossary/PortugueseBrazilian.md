# Brazilian Portuguese — Unique Weapons Unbound glossary

Machine-assisted generation, 2026-07-29; no native review yet. Family-shared
mechanics (the near-inert `LanguageWorker_Portuguese`, mandatory-but-
unsupported contractions, gender hedging with literal `(a)`, casing-by-def-
type, and vanilla-grounded common vocabulary — including the crossed
Cancel/Confirm button pair, quality tiers, the EMP acronym, market value,
gold/silver inlay, and "Traders will pay more/less for it.") live in the
`l10n/` submodule at `l10n/languages/PortugueseBrazilian.md` — this file
holds only what is specific to Unique Weapons Unbound.

## Weapon-trait word check (mandatory per repo — see SKILL.md)

**pt-BR INVERTS the Spanish assignment — the single highest-value row in
this glossary.**

| English | Use | Never | Why |
|---|---|---|---|
| **trait (weapon)** | **Característica / Características** (standalone: Características da Arma) | **Traços** | Odyssey `WeaponTraits` = Características, `Stat_ThingUniqueWeaponTrait_Label` = Características, `Stat_ThingUniqueWeaponTrait_Desc` = "Características individuais desta arma.", `StatsReport_WeaponTraits` = Características da Arma. Meanwhile `Stat_Thing_PersonaWeaponTrait_Label` = **Traços** is Royalty's *persona*-weapon word and belongs to PersonaWeaponsUnbound. In es the two words are assigned the *other way round* (unique=Rasgos, persona=Características), so carrying the es row across gets both wrong at once. Core's pawn-trait `<Traits>` is also Traços, so pt-BR splits weapon-trait from pawn-trait the way ru does |
| unique weapon | arma única | arma exclusiva | Odyssey `UniqueWeapon` = Arma única |

## UWU domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| customize | personalizar / personalização | customizar, configurar | Core `CustomizeIdeoligion` = **Personalização da Ideologia** — note vanilla used a *noun* there; the float menu should still follow `Equip` = Equipar {0} and `ExtractRelic` = Extrair {0}, i.e. infinitive + **bare** label |
| charge (weapons) | **carga** root: rifle de carga, lança carga, tiro de carga | pulso-, energia- | Core `Gun_ChargeRifle` = rifle de carga, `Gun_ChargeLance` = lança carga, `Bullet_ChargeRifle` = tiro de carga. **pt-BR is consistent here**, unlike fr which scrambles the family — but the *research* still diverges (below) |
| pulse-charged munitions (ChargedShot) | munição de carga | munição de pulso | Core research `label` = munição de carga. Its own `generalRules` `subject` says *munição de pulso* — vanilla contradicts itself; the label is authoritative |
| beam (weapons) | **feixe** (armas de feixe) | laser, raio | Core `BeamWeapons` = Armas de Feixe, `Gun_BeamRepeater` = repetidor de feixe, **and** the DamageDef `Beam.label` = feixe. Consistent, unlike fr where the DamageDef broke ranks |
| fueled / electric smithy | **forja abastecida / forja elétrica** | ferraria, forjaria | Core building labels |
| machining table | **mesa de usinagem** | bancada de usinagem | Core `TableMachining`. Note `AncientTableMachining` = Bancada de Mecânica Ancestral — the ruin variant only; don't generalize |
| fabrication bench | **bancada de fabricação** | mesa de fabricação | Core `FabricationBench.label` |
| workbench (**generic**) | **bancada de trabalho** | estação de trabalho, oficina | attested generically in `DrugLab`/`BioferriteShaper`/`ToolCabinet` descriptions ("Uma bancada de trabalho..."). No collision — nothing is labelled exactly *bancada de trabalho*, and every specific bench is forja / mesa de usinagem / bancada de fabricação. `estação de trabalho` is taken: it is how `TableMachining.description` opens |
| smithing / machining / fabrication (research stems) | **metalurgia / usinagem / fabricação** (advanced: fabricação avançada) | forja for smithing | Core research labels. **Landmine:** `ResearchProjectDef Smithing` = *metalurgia*, but `WorkTypeDef Smithing.labelShort` = *forja* and `.pawnLabel` = Ferreiro, and the building is *forja abastecida* — three words in one family. Check the def type |
| **Crafting (the skill)** | **fabricação** | artesanato | Core `Crafting.label`. **This collides three ways**: the Crafting *skill*, the `Fabrication` *research*, and `FabricationBench` are all *fabricação* / *bancada de fabricação*. Disambiguate by context or name the bench explicitly |
| tech level (the concept) | nível de tecnologia | nível tecnológico | `TechLevelTooLow`, `CantSendMilitaryAidInTime` and `ResearchCostComparison` all use *nível de tecnologia* — pt-BR is unanimous, unlike fr where it was near-tied |
| tech levels (the **enum labels**) | neolítico / medieval / industrial / **espacial** / **ultra** / **arquotecnológico** | ultratech, arqueotec- | Core `TechLevel_*`. ultratech = plain **Ultra**; archotech = **Arquotecnológico**, i.e. pt-BR gives the enum label the *attributive* form other languages reserve for prose |
| ultratech / archotech (**attributive, in prose**) | ultratecnológico / arquotecnológico | ultratech | **Comment-stripped, bare `ultratech` is 0 hits** — every apparent occurrence is inside an `<!-- EN: -->` comment. Real translations write *ultratecnológico* (10) or the clipped *ultratec* (11, e.g. `Gun_BeamRepeater.description` = "Uma arma ultratec"); *arquotecnológic-* is 112. A raw grep here is actively misleading |
| "{0} quality or better" | `requer qualidade {0} ou melhor` | | reshaped from Core `NormalQualityOrBetter` = qualidade normal ou melhor (pre-inflected, untemplatable, and note vanilla gives it no terminal period) |
| plasteel | **plastiaço** | plastaço, plasteel | Core `Plasteel` — counterintuitive, always check (*plastaço* is 0 hits) |
| chemfuel | **combustível químico** | biocombustível, quimicombustível | Core `Chemfuel` — pt-BR is **literal** here, where es coined *biocombustible*, de *Sprit* and fr left it English. Never extrapolate this row between languages |
| herbal medicine | **medicina natural** | ervas medicinais, medicina herbal | Core `MedicineHerbal`. *ervas medicinais* does occur (4 hits) but is not the label |
| wood / gold / uranium / jade | madeira / ouro / urânio / jade | | Core labels (`WoodLog` = madeira) |
| components / advanced components | componente / componente avançado | peças | `ComponentIndustrial`, `ComponentSpacer` (both singular labels) |
| bioferrite / thrumbofur / birdskin / steel slag chunk | bioferrita / pele de trumbo / pele de pássaro / pedaço de escória de aço | couro de ave | Anomaly `Bioferrite`; Core `Leather_Thrumbo`, `Leather_Bird`, `ChunkSlagSteel` |
| burst speed / burst count / stopping power | Taxa de disparo / Contagem de tiros por disparo / Poder de parada | | Core `BurstShotFireRate`, `BurstShotCount`, `StoppingPower` |
| armor penetration / damage | Penetração de Armadura / Dano | | Core `ArmorPenetration`, `Damage` |
| ignores accuracy penalties | ignora penalidades de precisão | | Odyssey `AimAssistance.description` = "ignore a penalidade de precisão por mau tempo e fumaça" — reuse the noun phrase |
| grip / ornamental / lightweight / cumbersome / ugly | empunhadura / ornamental / leve / desajeitado / feia | | Odyssey trait labels. Note `Ugly` = *feia*, pre-agreed feminine to *arma* |
| tox / incendiary / EMP rounds | munição tóxica / munição incendiária / munição PEM | munição EMP | Odyssey `ToxRounds`, `IncendiaryRounds`, `EMPRounds` |
| cut / stab (**DamageDef** label) | corte / **facada** | punhalada | Core DamageDefs. The HediffDef labels happen to **agree** here (`Cut` = corte, `Stab` = facada; `labelNoun`s are *um corte* / *uma facada*), but `ToolCapacityDef Stab` = **punhalada** — so the three-way split lands on a different axis than in ko/fr. Point each def at the right one |
| stun / flare | atordoar (state: Atordoado) / sinalizador | | Core `StunLower`; Anomaly `DisruptorFlare` = sinalizador |
| relic | relíquia (relic of X = relíquia de X; reliquary = relicário) | | Ideology `RelicTip`, `ExtractRelic` |
| ideoligion | **ideologia** | ideoligião | Ideology `IdeoligionOf` = Ideologia de, `ReformIdeoligion` = Reformar ideologia. **pt-BR uses the plain word, no portmanteau** — with de/ja/zh/ko, against ru/es/fr. The split still doesn't follow language families |
| **disarm** | phrase with **desarmar** (É preciso desarmar um hostil) | | `DisarmedTime` = Desarmado, and *desarmado* is well attested for the weapon sense (Biotech `ArmedChildren`, `Melee.description`). **No collision**, unlike es where *desarmar* means deconstruct: pt-BR has `Deconstruct` = Descontrução (vanilla's own typo for Desconstrução) and `Uninstall` = Desinstalação |
| haul / carrying capacity | transportar / capacidade de transporte | carregar | Core `Haul.label`, `CarryingCapacity.label` |
| forbidden / cannot reach / reserved by | proibido / não pode alcançar / reservado por | | Core `ForbiddenLower`, `CannotReach`, `IsReservedBy` = {0} está reservado por {1}. |
| hostile / enemy / colonist / pawn (in prose) | Hostil / inimigo / colono | peão, personagem | Core `Hostile`, `Enemy`, `Colonist`; in prose *colono(s)* is 447 hits vs *personagem* 30 |
| Structure (architect category) | **estrutura** | estruturas | `Structure.label` is **singular** — as in fr, and unlike de and es which both pluralize it |
| ingredients / quality / effects / prerequisites / progress | Ingredientes / Qualidade / Efeitos / Pré-Requisitos / Progresso | | Core Keyed |
| Randomize | Aleatorizar | | Core `Randomize` |
| (none) / Free | (Nada) / Grátis | Nada, Sem custo | Core `NoneBrackets`, `CommandCallRoyalAidFreeOption` |
| Warning / log / colour / appearance | Aviso / Registro / Cor / Aparência | | Core `Warning`, `OpenLogOnWarnings`, `Color`, `Appearance` |
| Miscellaneous | **Diversos** | Outros, Miscelânea | `Architect_Misc.label` = Aba de Diversos and `PawnMisc.label` = Diversos are the UI-section uses, which is what our header is. `MiscRecordsCategory` = Outros is the records-tab category, a different role |
| float menu / right-click / button / select | menu de contexto / clicar com o botão direito / botão / selecionar | menu flutuante | *menu de contexto* 4 hits, *menu flutuante* 0; `AddBillSimpleMeal.text` uses "clicar com o botão direito", "no menu de contexto", "Clique no botão" |
| blue (colour label) | azul | | Core `Blue`, `Structure_Blue.label` |
| quest / cooldown / ability | Missão / Tempo de recarga / Habilidades | | Core Keyed (`Quest` tab is pluralized *Missões*; prose uses *missão*) |
| techprint | projeto técnico | tecnoimpressão | Core `TechprintLabel` |
| sealed / created at | selado / Criado em | | Odyssey `AncientSealedCrate` = Caixote Selado; Core `CreatedAt` |
| mechanite(s) | **mecanitos** | nanomáquinas, mecanitas | 46 comment-stripped hits vs 3 for *mecanitas*; *nanomáquinas* (10) renders the different English word "nanomachines". Same trap that shipped as a bug in PersonaWeaponsUnbound's ko pass |

## Research `generalRules` — UWU's three research defs

**pt-BR is the FIRST inflecting language checked that does NOT reshape research
`generalRules`.** It keeps the English 2-symbol set verbatim — `subject`,
`subject_story`, `subject_gerund`, nothing added — with **no article baked in** and a
**real gerund**: `Smithing` → `subject->metalurgia`, `subject_gerund->forjando`;
`Machining` → `usinagem` / `construindo equipamento de usinagem`; `AdvancedFabrication`
→ `fabricação de componentes avançados` / `fabricando componentes avançados`. Symbol
census across Core's ResearchProjectDef folder: `subject` 186, `subject_story` 460,
`subject_gerund` 202, and no other symbol except an unrelated `tv_content`. So UWU's
three defs ship the plain English shape, which is also the *only* language where doing
so needs no justification.

Why it's safe as well as faithful: the consumer,
`pt-BR/Core/DefInjected/RulePackDef/RulePacks_Book_Descriptions.xml`, is **unimplemented
— all three of its entries are the literal string `TODO`**, so there are zero live
`[subject]` references (107 apparent ones are all inside `<!-- EN: -->` comments), and
there is no `priority=-1` fallback anywhere. This is the es situation, not the fr one.

`subject_story` is a subject-less **third-person preterite** clause (`se assentou em um
vilarejo medieval e dominou a fabricação simples com metais`, `silenciosamente construiu
um arsenal de armas artesanais`), which matches the English shape — as es does, and
unlike de (verb-final) or ko (polite `했습니다`).

Note also `Odyssey/.../NamerUniqueWeapon` **hardcodes gendered articles** into its name
patterns (`O [weapon_type] da [badass_concept]`, `A [badass_concept] de [...]`) and its
`badass_adjective`s are masculine-default (sombrio, eterno, mortal). Our
`UWU_WeaponTypeFallback` feeds `[weapon_type]`, so *Arma* (feminine) can land after
vanilla's hardcoded `O`. That mismatch is vanilla's own pre-existing limitation — it
already breaks on *espada* — and is not fixable from a Keyed string; the fallback is
rarely hit, so use the correct word and leave it.

Mod-decided terms pending native review: the research trio **metalurgia / usinagem /
fabricação de armas únicas** (vanilla lowercase stem + "de armas únicas", with
`subject_gerund` forms *forjando / usinando / fabricando armas únicas*); haul planner
modes **Sequencial / Varredura / Minucioso**; haul plan **plano de transporte** and
section header **Transporte de ingredientes**; net refund/cost **Reembolso líquido /
Custo líquido** (vanilla pt-BR has **no** word for refund at all — *reembolso* and
*restituição* are both 0 hits, matching de/es/fr/ko/zh); texture tab **Textura**;
vanilla-behavior suffix **(jogo base)** and the matching prose "no jogo base" (*jogo
base* is 0 hits in vanilla and *vanilla* only appears in `Core.label`, so neither is
established); progression header **Progressão**; gizmo button **botão de comando** (0
vanilla hits); research tree **árvore de pesquisa** (0 vanilla hits — `ResearchScreen` =
Tela de pesquisa offers no tree); flare launcher **lançador de sinalizadores**;
**Flarestriker** and **Akimbo** kept in Latin script (neither is a vanilla trait);
weapon def **def da arma** (kept Latin, as ja, zh, ko, de, es and fr do).
