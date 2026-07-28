---
name: translate
description: Generate, update, or audit mod localization (Keyed + DefInjected) for a target language, grounded in vanilla RimWorld terminology. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Unique Weapons Unbound. English is
the source of truth; every other language derives from it.

## Non-negotiables

- **Run the checker first and last.** `python3 Scripts/check-translations.py`
  validates key sets, placeholders, DefInjected paths, staleness, and file
  hygiene deterministically. Never hand-derive anything it reports; never
  finish with it failing.
- **Community translations are owned by their contributors.** Update
  stale/missing keys in an existing language when asked, but do not rewrite a
  contributor's phrasing wholesale without the user's explicit direction.
  (Russian is community-maintained — PR #6.)
- **Machine-assisted output is a first pass.** PRs and commits containing
  generated translations must say so and invite native-speaker review.
- **Keep the public roster current.** CONTRIBUTING.md's localization table
  (Planned / Machine-assisted / Native, plus credit) must be updated in the
  same commit whenever a language is added or a native review lands. The
  target roster lives there — consult it before proposing new languages.

## File map and conventions

- English Keyed source: `1.6/Languages/English/Keyed/UWU_UI.xml`
- Target layout: `1.6/Languages/<Language>/Keyed/*.xml` and
  `1.6/Languages/<Language>/DefInjected/<DefTypeFolder>/*.xml`
- `<DefTypeFolder>` must be the def's resolvable type name: bare for vanilla
  types (`JobDef`, `ResearchProjectDef`), **namespace-qualified for this mod's
  own def classes** (`UniqueWeaponsUnbound.TraitCostRuleDef`) — a bare custom
  name silently drops every translation in the folder.
- **The type folder is load-bearing, not organizational** (decompile-verified,
  `Verse.LoadedLanguage`): RimWorld enumerates only the top-level directories
  under `DefInjected/` and resolves each directory *name* to the def type its
  files target. An `.xml` placed directly in `DefInjected/` is never loaded,
  and the checker likewise iterates only directories — a misplaced file fails
  silently on both sides, so never flatten the tree. *Inside* a type folder
  everything is free: file names are arbitrary and files are found recursively,
  so one bundled file per type vs one-def-per-file is pure preference — this
  repo bundles per type, since reviewers work in whole-language passes and
  entries are found by their defName-prefixed keys, not by file. (The loader
  even tolerates a pluralized folder name by retrying with the last character
  stripped — `ThingDefs` → `ThingDef` — but the checker does not; use exact
  type names.)
- DefInjected keys are `DefName.field` paths (`UniqueSmithing.description`,
  `UniqueSmithing.customUnlockTexts.0`). Translate `label`, `description`,
  `reportString`, `customUnlockTexts`, `generalRules.rulesStrings` — the
  checker warns on uncovered label/description.
- **EN comment convention (required):** every translated entry carries the
  current English source directly above it:
  `<!-- EN: Customize {0} -->` — this is how the checker detects staleness.
- Formatting: UTF-8 without BOM, LF endings, 2-space indent, final newline,
  root element `<LanguageData>`.
- Placeholders (`{0}`, `{1}`, named args) must match English exactly per key.
  Translator comments above placeholdered English keys explain what gets
  injected — injected values are lowercase def labels; phrase around them
  accordingly.

## Terminology grounding (do not skip)

Every game term must match the official localization, not a plausible
translation. Sources, in order:

1. Vanilla language data:
   `"$RIMWORLD_PATH"/Data/<Expansion>/Languages/<Language> (<Native>).tar`
   (read entries with `tar -xOf`). Check Core plus Odyssey (this mod's DLC),
   and Ideology for relic/ideoligion strings.
2. This file's glossary below (lessons already learned — apply them).
3. If a term appears nowhere official, flag it in the PR for native review
   rather than inventing silently.

Terms that MUST be grounded before use: weapon trait, unique weapon, relic,
ideoligion, charge weapons, quality tiers, workbench names (smithy, machining
table, fabrication bench), research project names, tech levels.

### Glossary — Russian (from PR #6 native review)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | свойство | черта | vanilla `WeaponTraits`=Свойства; черта = pawn personality traits |
| ideoligion | идеолигия | идеология | vanilla's coined portmanteau (`ReformIdeoligion`) |
| charge (weapons) | энерг- root | заряд- | vanilla `Gun_ChargeRifle`=энерговинтовка; заряд reads as ammo |
| fueled/electric smithy | топливная кузня / электрокузня | кузница | vanilla building labels |
| machining table | верстак | обрабатывающий стол | vanilla building label |
| fabrication bench | высокоточный станок | сборочный стол | vanilla building label |
| Cancel (button) | Отменить | Отмена | vanilla `Cancel`; buttons use infinitive verbs |
| job report strings | noun phrases (Настройка {0}) | finite verbs | matches inspect-pane convention |

Add rows here whenever a native review lands corrections.

### Glossary — Japanese (machine-assisted generation, 2026-07; no native review yet)

RimWorld's language folder is `Japanese` (tar: `Japanese (日本語).tar`).

Style rules discovered from the vanilla JP data (mandatory):

- Vanilla JP uses ASCII punctuation: `,` and `.` — never `、` or `。`.
- Descriptions/tooltips: polite です/ます form ending `.`; labels/buttons no period.
- Job report strings: continuous form 〜している / 〜中, no subject, no period.
- Thought (`ThoughtDef` stage) descriptions are plain first-person, no です/ます.
- Quote injected def labels and cross-referenced UI labels with 「」. Suffixes
  and parentheticals take no leading space and use ASCII parens.
- `traitAdjectives` are **attributive** forms ending in の / な / い / a verb
  (Odyssey ships 探知の, 正確な, 灼熱の). The JP namer concatenates with no
  space, so a bare noun reads broken.
- Name grammar: no spaces around [symbols]; "The X of Y" → `[Y]の[X]`; vanilla
  keeps `[RECIPIENT_possessive]` (unlike zh, which drops it).
- `stuffProps.stuffAdjective` is `〜製` (鉄製, プラスチール製, 木製, ヒスイ製),
  so `[stuff_adjective]の[noun]` composes cleanly — supply the の in our rules,
  matching vanilla's の-terminated trait adjectives.
- Battle-log entries end in plain past tense and JP `[skillAdv]` values are
  adverbials (巧みに, ゆっくりと), so `[skillAdvMaybe]` slots before the verb.
- `deathMessage` keeps vanilla's space after the pawn token: `{0}は 斬られて…`.
- DLC names stay in Latin script (Odyssey, Royalty), as does MOD.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器の特性) | 特性・特徴 | `WeaponTraits`=特性, and JP shares the pawn-trait word (unlike Russian). But the DLC domains still diverge: 特性・特徴 is Royalty's *persona*-weapon word (`Stat_Thing_PersonaWeaponTrait_Label`), so it belongs to PWU, not here |
| unique weapon | ユニークな武器 | | vanilla `UniqueWeapon`, Odyssey `*_Unique` labels |
| Pulse-charged munitions (ChargedShot research) | チャージライフル | パルス弾 | JP names the research after the rifle; disambiguate as 「チャージライフル」の研究 |
| fueled / electric smithy | 工作台 / 電動工作台 | 鍛冶場 | vanilla building labels |
| machining table | 精密工作機械 | | vanilla building label (also the Machining research name) |
| fabrication bench | コンポーネント工作台 | | vanilla building label |
| ultratech | 最先端の技術力 (noun) / 最先端技術級 (attributive) | ウルトラテック | vanilla `TechLevel_Ultra` |
| ideoligion | 思想 | イデオリギオン | JP does not coin a portmanteau; relic = レリック |
| Cancel / Confirm / Randomize / Reset / Reset to defaults | キャンセル / 了承 / ランダム / リセット / デフォルトに戻す | | vanilla Keyed buttons |
| monosword / plasmasword / zeushammer | モノソード / プラズマソード / ゼウスハンマー | | Royalty weapon labels |
| longsword / spear / mace / knife / gladius / axe / warhammer | ロングソード / スピア / メイス / ナイフ / グラディウス / 戦斧 / ウォーハンマー | | Core/Odyssey/Royalty labels (mostly katakana, not 長剣/槍) |
| plasteel / jade / wood (stuff adjectives) | プラスチール製 / ヒスイ製 / 木製 | 塑鋼, 翡翠 | Core `stuffProps.stuffAdjective` |
| mechanite / mechanoid | メカナイト / メカノイド | | Royalty, Odyssey descs |
| wielder / bearer | 使用者 / 持ち主 | | Odyssey `EMPPulser` desc |
| stun / EMP / stagger | スタン / EMP / よろめき | | `StunnedByEMP`, `StaggerDurationFactor` |
| armor penetration / bleed rate / move speed | アーマー貫通力 / 出血量 / 移動速度 | | Core Keyed + StatDefs |
| cut / stab (DamageDef) | 斬る / 刺す | 切創, 刺し傷 (those are the *hediff* labels) | Core DamageDefs vs HediffDefs differ |
| toxic buildup | 毒物が蓄積 | | Core `ToxicBuildup` |
| item stash / bandit camp / ancient mercenaries / sealed crate | 埋蔵品 / 盗賊の野営地 / 古代の傭兵 / 密封されたクレート | | Core sites, Odyssey quest + `AncientSealedCrate` |
| humanlike / ability / quest / cooldown / cells | 人型 / 能力 / クエスト / クールダウン / セル | | Core Keyed |
| quality tiers | 壊れかけ/低品質/標準品/良品/秀品/名品/幻の一品 | | Core `QualityCategory_*` |
| Traders will pay more/less for it. | 貿易商は高値で/低い価格でこれを買い取ります. | | Odyssey `GoldInlay`/`Ugly` descs — reuse verbatim |

The six Odyssey trait ports (`Lightweight`, `Cumbersome`, `Ornamental`,
`Ugly`, `GoldInlay`, `JadeInlay`) have official JP labels, adjectives and — for
four of them — descriptions that match our English word for word; copy them
rather than retranslating.

Mod-decided terms pending native review: research trio ユニーク武器の鍛冶 /
ユニーク武器の精密加工 / ユニーク武器の組立製造; haul planner modes 順次 / 巡回 /
徹底; net refund/cost 実質返却 / 実質コスト; haul plan 運搬計画.

From UniqueMeleeWeapons' 2026-07 JP pass, also pending native review: 受け流し
(parry, register-matched to `TextMote_Dodge` 回避), 戦士団 (warband, parallel to
vanilla 傭兵団), 頭目 (warlord), 鍔 / クロスガード (quillons / crossguard),
地響き (earthshake), 鼓舞の叫び (rallying cry), 士気高揚 (rallied), 由緒ある
(storied), 杭打ちヘッド (piledriver), アヘン塗布 (opiated), 琺瑯 (enameled),
無反発 (dead-blow, from the real tool term 無反発ハンマー), 稜付き (flanged),
鋲打ち (studded), 徹甲スパイク (armor spike), 先重心 (head-weighted), 素早い
(quickdraw — vanilla's 早撃ちの is ranged-specific and wrong on melee).

### Glossary — Simplified Chinese (machine-assisted generation, 2026-07; no native review yet)

Seeded from UniqueMeleeWeapons' 2026-07 zh-Hans run and extended by UWU's own
pass. RimWorld's language folder is `ChineseSimplified` (tar:
`ChineseSimplified (简体中文).tar`). Decompile-verified (`Verse.LoadedLanguage`):
the ctor derives `legacyFolderName` by cutting at the `(`, and `AllDirectories`
accepts either name, so a mod folder named `ChineseSimplified` loads — the same
holds for `Japanese`.

Style rules from the vanilla zh data (mandatory):

- Full-width punctuation in prose (，。、；：（）……); descriptions end with 。;
  labels and buttons carry no trailing period. Placeholders, digits and units
  stay ASCII. Vanilla labels use full-width parens: 锻造台（燃料）.
- **Two quote styles, both vanilla, split by what is being cited.** Injected
  placeholders take full-width curly quotes (`“{0}”`, 32 vanilla hits vs 5 for
  「」); literal UI, building and research names spelled out in prose take
  corner brackets, matching vanilla research descriptions (解锁建造「精密装配台」,
  研究「基础逆重科技」). Terse stat and job-report templates take no quotes at
  all (`品质: {0}`, `搬运TargetA。`).
- Only *named entities* get quotes. Common-noun labels stay bare, per vanilla
  `Equip`=装备{0} and `NormalQualityOrBetter`=品质需要为一般及以上。 — so
  research/ideoligion/trait names are quoted, weapon and material labels and
  quality tiers are not.
- Terse label templates use an **ASCII** `: ` separator (vanilla `QualityIs`=
  品质: {0}, `EffectsAtLevel`=效果: ); full-width ：only inside prose.
- Job report strings (`reportString` and anything returned from
  `JobDriver.GetReport`) are verb-first phrases that **do** end in 。 — vanilla
  writes 研究中。/ 清理TargetA。 This is the opposite of JP, which takes no
  period; do not carry the JP rule across.
- Vanilla zh files can contain untranslated English values — vanilla
  incompleteness is not style guidance. Some vanilla zh files carry a BOM;
  ours never do.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器特性) | — | Odyssey `WeaponTraits` / `StatsReport_WeaponTraits` |
| unique weapon | 特化武器 | 独特武器 | Odyssey `UniqueWeapon` |
| customize | 自定义 | 定制 | Core `CustomizeIdeoligion`=自定义文化; float-menu register matches `Equip`=装备{0} |
| charge (weapons) | 电荷 root | 能量- / 充能- | Core `Gun_ChargeRifle`=电荷步枪, `ChargedShot` research=电荷弹 — zh keeps "charge" literal where RU switched to an energy root; never extrapolate |
| beam (weapons) | 光束 | | Core/Odyssey `Beam`, `BeamBypassShields` |
| fueled / electric smithy | 锻造台（燃料） / 锻造台（电力） | 铁匠铺 | Core building labels |
| machining table | 机械加工台 | | Core `TableMachining`; `Machining` research=机械加工 |
| fabrication bench | 精密装配台 | | Core `FabricationBench`; `Fabrication` research=精密装配 |
| smithing (research) | 锻造 | | Core `Smithing` |
| ideoligion | 文化 (also 文化形态) | 意识形态 | Ideology Keyed (`ButtonShowAllIdeoligions`, `IdeoligionOf`) — a plain word like JP 思想, no portmanteau |
| relic | 圣物 (relic of X = X的圣物) | | Ideology `<Relic>`, `RelicOf` |
| tech levels | 石器时代/中世纪/工业时代/太空时代/极致时代/超凡时代 | | Core `TechLevel_*` |
| ultratech / archotech (attributive) | 极致科技 / 超凡科技 | 超科技 | `BodyPartsUltra`=极致科技; 超凡科技 recurs throughout Anomaly/Ideology prose |
| tech level (the gating concept) | 科技等级 | 科技水平 | Core `CantSendMilitaryAidInTime` uses 科技等级 for the mechanical sense |
| plasteel | 玻璃钢 | 塑钢 | Core `Plasteel` — counterintuitive, always check |
| wood / components / advanced components | 原木 / 零部件 / 高级零部件 | 木材, 元件 | Core `WoodLog`, `ComponentIndustrial`, `ComponentSpacer` |
| chemfuel / herbal medicine / silver | 化合燃料 / 草药 / 白银 | 化学燃料 | Core labels — 化合, not 化学 |
| bioferrite / thrumbofur / birdskin / steel slag chunk | 活铁 / 敲击兽皮 / 鸟皮 / 钢渣块 | 生物铁 | Anomaly `Bioferrite`; Core `Leather_Thrumbo`, `Leather_Bird`, `ChunkSlagSteel` |
| quality tiers | 极差/较差/一般/良好/极佳/大师级/传奇级 | | Core `QualityCategory_*` |
| burst speed / burst count / stopping power | 射速 / 连射次数 / 抑止能力 | | Core `BurstShotFireRate`, `BurstShotCount`, `StoppingPower` |
| accuracy penalty / ignores | 精度惩罚 / 无视 | | Odyssey `AimAssistance.description` uses both verbatim |
| inlay / grip / ornamental / lightweight | 镶嵌 / 握把 / 华丽 / 轻便 | | Odyssey `GoldInlay`, `CustomGrip`, `Ornamental`, `Lightweight` labels |
| toxic / incendiary / EMP | 剧毒 / 燃烧 / EMP | 毒性 | Odyssey `ToxRounds`, `IncendiaryRounds`, `EMPRounds` |
| flare | 照明弹 | 信号弹 | Anomaly `Apparel_DisruptorFlarePack` verb + chargeNoun |
| scrap / mod / log / save | 废料 / 模组 / 日志 / 存档 | | Core `CubeMaterialScrap`, `ScenPart_Error`, `OpenLogOnWarnings`, `SaveGameDataFolder` |
| caravan / quest / forbidden / cannot reach | 远行队 / 任务 / 已禁用 / 无法到达 | 商队 | Core `Caravan`, `Quest`, `ForbiddenLower`, `CannotReach` |
| haul / carrying capacity / market value | 搬运 / 携带能力 / 市场价值 | | Core `Haul.label`, `CarryingCapacity`, `MarketValue` |
| Cancel / Reset / Confirm / Randomize / Reset to defaults | 取消 / 重设 / 确定 / 随机 / 还原默认设置 | 重置为默认值 | Core Keyed buttons; 重置为默认值 is `ResetBinding`, keybinding-specific, while `RestoreToDefaultSettings`=还原默认设置 is the settings-page verb |
| "requires X quality or better" | 品质需要为{0}及以上 | | Core `NormalQualityOrBetter`=品质需要为一般及以上。 |

Mod-decided terms pending native review: research trio 特化武器锻造 / 特化武器机械加工 /
特化武器精密装配 (vanilla research name prefixed with 特化武器); haul planner modes
逐次 / 巡回 / 彻底; haul plan 搬运计划; net refund/cost 实际返还 / 实际成本
(no vanilla term for "refund" exists at all); texture tab 贴图 (材质 is taken —
it is `Stat_Stuff_Name`, i.e. *material*); vanilla-behavior suffix （原版）;
"must disarm from hostile" 必须从敌人手中缴获 (vanilla only has `DisarmedTime`=解除武装);
progression section header 进度; gizmo button 指令按钮; weapon def 武器def (kept
Latin, as JP does).

### Cross-language lessons

- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans “{0}”) — injected labels never inflect, and quoting
  sidesteps case and agreement problems. **But check how far vanilla actually
  carries it**: the case/agreement motive doesn't exist in zh-Hans, and vanilla
  zh leaves common-noun labels bare (`Equip`=装备{0}), so there quoting narrows
  to named entities (research, ideoligion, trait names) and terse
  stat/job-report templates take none.
- Job-report register does **not** transfer between languages: vanilla zh
  `reportString`s end in 。(研究中。), vanilla JP ones take no period. Check the
  target language's own `DefInjected/JobDef` before writing any.
- When an English string is reworded, refresh the EN comments in every
  language **in the same commit** — the checker reports the mismatch as STALE
  either way, but batching avoids churn.
- Coined vanilla terms (ideoligion) may be a portmanteau in one language
  (RU идеолигия) and a plain word in another (JP 思想, zh-Hans 文化) — always
  check, never extrapolate between languages.
- Mod-coined terms recur in def labels AND in Keyed settings prose that
  restates them. When generation is chunked across files or subagents,
  reconcile those terms across the whole language before committing (UMW's
  zh-Hans run needed an alignment pass for its ability/hediff/trait names).

## Workflows

### Initial generation (`/translate <Language>`)

1. Run the checker; confirm English itself is clean.
2. Enumerate English Keyed keys and DefInjected-translatable def fields
   (mirror the English/Russian file structure).
3. Extract the vanilla tar for the target language into the scratchpad;
   build a term list for the grounded terms above.
4. Translate via subagent(s) carrying: the glossary, the vanilla term list,
   the EN-comment requirement, placeholder rules, and formatting rules.
   Chunk by file section if the key count is large.
5. Run the checker (`--strict` for new languages); fix everything.
6. Review the diff yourself before committing. Commit message and PR text
   must state machine-assisted origin and invite native review.

### Update pass (`/translate update`)

1. Run the checker; it lists missing keys and stale entries per language.
2. Translate only that delta, refreshing each entry's EN comment.
3. Leave correct existing entries untouched. Re-run the checker.

### Audit only (`/translate check`)

Run the checker and report; change nothing.

## Optional in-game verification

RimWorld Dev Mode offers "Save translation report" and "clean up translation
files" (Verse.LanguageReportGenerator / TranslationFilesCleaner). These need a
running game with the mod loaded — useful as a final QA pass, not a substitute
for the checker.
