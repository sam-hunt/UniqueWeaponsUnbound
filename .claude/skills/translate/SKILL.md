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

### Glossary — Korean (machine-assisted generation, 2026-07; no native review yet)

RimWorld's language folder is `Korean` (tar: `Korean (한국어).tar`).

**Josa auto-resolution is load-bearing — this is Korean's whole game.**
Decompile-verified (`Verse.LanguageWorker_Korean`): `PostProcessed` runs
`ReplaceJosa` *after* placeholder substitution, so a particle written as a paired
token is rewritten to agree with whatever label actually got injected. Never
hardcode an inflecting particle after a `{0}`.

- The **only** recognized tokens are `(이)가 (와)과 (을)를 (은)는 (아)야 (이)어
  (으)로 (이)`. Spelling is exact: the paren holds the post-**consonant** form for
  every token **except `(와)과`**, where it holds the post-**vowel** form. `(과)와`
  does not match the regex and renders literally as garbage.
- Only these five distinctions inflect: 은/는, 이/가, 을/를, 와/과, 으로/로.
  `에`, `에서`, `의` are invariant — write them bare after a placeholder.
- `FindLastChar` deliberately skips a preceding `"`, `'` or `)` to reach the real
  final character, so `"{0}"(을)를` resolves correctly. Curly `“ ”` and corner
  `「 」` are **not** skipped — a josa after one silently fails to resolve and the
  raw `(은)는` shows on screen. Vanilla never places a josa after a curly quote.
- Latin-script tails are treated as having a final consonant only for
  `b c k l m n p q t`, so `Odyssey` → `y` → vowel-form particle.
- **That same list has no digits, so a josa resolving off a number is always
  wrong.** `AlphabetEndPattern` is consulted for any non-Korean char, so a digit
  yields the vowel-form particle unconditionally — correct for 2/4/5/9
  (이·사·오·구) but wrong for 1(일) 3(삼) 6(육) 7(칠) 8(팔) 0(영), whose readings
  carry batchim. **Phrase around it** rather than marking it: this mod's
  `UWU_CouldNotStartReservationConflict` says `{1} x{2} 예약에 실패했습니다`, not
  `x{2}(을)를 예약하지 못했습니다`. Highest-risk spot for a settings-heavy mod
  where counts and costs are injected constantly. A bare *invariant* particle
  after a number is fine (`0으로 설정하면`) — the author knows the digit, and no
  marker means the worker never touches it.
- Consequence: Korean needs **no defensive quoting** of injected labels at all —
  josa solves what quoting solves elsewhere. Vanilla quotes with ASCII `"` (24
  hits) and reserves it for zone/bill/setting names, matching `Equip`={0} 착용
  leaving plain labels bare.

Style rules from the vanilla KO data (mandatory):

- ASCII punctuation only — full-width `、` and `。` are **0 hits**. Use `.` and `,`.
- Descriptions/tooltips: polite 합니다/입니다, ending `.`; labels and buttons take
  no trailing period.
- Job report strings (`reportString`): `~ 중`, **no** period (`Research`=연구 중,
  `BuildSnowman`=눈사람 만드는 중). Same as JP, opposite of zh-Hans.
- Research `generalRules` `subject_story`: polite past **했습니다** — *not* the
  plain 했다 that JP uses for the same field. Check per language, never carry over.
- Register splits by def type, so don't pick one voice for a whole language:
  `ThoughtDef` stage descriptions are casual first-person (`-어`, `-지`, `-거야`;
  vanilla `이제 거의 깼어.`), battle-log `rulesStrings` end in the nominalized
  `-함.`/`-임.` (`Combat_Dodge`: `… [implement](을)를 [skillAdvMaybe] 피함.`), and
  everything else is polite `-습니다.`.
- **KO drops `[RECIPIENT_possessive]`, unlike JP.** The Japanese section above
  records that JP *keeps* it; do not carry that across. Vanilla ko's combat
  rulePacks contain 12 textual occurrences, all inside `<!-- EN: -->` comments
  and **none** in Korean values, because Korean omits possessive pronouns. A ko
  battle-log pack should drop the symbol rather than render 그의.
- Name grammar: KO **uses spaces** where JP and zh concatenate. The ko
  `NamerUniqueWeapon` composes `[weapon_adjective] [weapon_noun]`, drops English
  "The", and links with `의` (`[badass_concept]의 [weapon_type]`). So ko trait
  adjectives may be attributive verb forms (가벼운, 저주받은) *or* bare noun
  modifiers (황금, 신속, 특제) — JP's "must end in の/な/い" rule must not be
  ported. Materials compose bare: `[stuff_adjective] [weapon_noun]` → 강철 장검.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 특성 (stats title 무기 특성) | 개성 | Odyssey `WeaponTraits`; 개성 is Royalty's *persona*-weapon word (`Stat_Thing_PersonaWeaponTrait_Label`) |
| unique weapon | 고유 무기 | | Odyssey `UniqueWeapon` |
| customize | 개조 | 사용자 지정 | 개조 is the physical-modification sense (Core 신체 개조, 개조된); 사용자 지정 is the software-UI sense and wrong for a workbench operation |
| tech level (the concept) | 기술 수준 | 기술 등급 | `CantSendMilitaryAidInTime`; 기술 등급 means **skill** level (`BestSkillInfoLevel`=기술 등급 {0}) — the trap is the mirror image of zh-Hans, which went the other way |
| tech levels | 원시 / 중세 / 산업 / 우주 / 미래 / 초월 | | `TechLevel_*` — note ultratech=**미래**, archotech=**초월**, so neither transliterates |
| quality tiers | 끔찍/빈약/평범/상급/완벽/걸작/전설적 | | `QualityCategory_*` |
| "X quality or better" | {0} 품질 이상 | | `NormalQualityOrBetter`=평범 품질 이상 |
| fueled / electric smithy | 단조 작업대 / 전기 단조 작업대 | 대장간 | Core building labels |
| machining table | 기계 작업대 | | Core `TableMachining` (research `Machining`=기계 가공) |
| fabrication bench | 조립 작업대 | | Core `FabricationBench` (research `Fabrication`=정밀 조립) |
| smithing (research) | 단조 | | Core `Smithing` |
| pulse-charged munitions (ChargedShot) | 충전 탄환 | | Core research label; the charge *concept* is 펄스 충전 (`ChargeCapacitor.description`) |
| beam (weapons) | 광선 무기 | 빔 | Core `Beam` research |
| ideoligion / relic | 사상 / 유물 (relic of X = X의 유물) | 이념 | Ideology `ReformIdeoligion`, `Relic`, `RelicOf` — a plain word, no portmanteau |
| plasteel / steel / wood | 플라스틸 / 강철 / 목재 | | Core labels |
| components / advanced components | 부품 / 고급 부품 | | `ComponentIndustrial`, `ComponentSpacer` |
| chemfuel / herbal medicine / silver | 화학연료 / 생약 / 은 | 약초 | Core labels |
| bioferrite / thrumbofur / birdskin / steel slag chunk | 생체강 / 트럼보 모피 / 새 가죽 / 고철 덩어리 | | Anomaly `Bioferrite`; Core `Leather_Thrumbo`, `Leather_Bird`, `ChunkSlagSteel` |
| jade (inlay) | 옥 상감 | 비취옥 상감 | `JadeInlay`=옥 상감 even though `Jade.label`=비취옥 |
| inlay / grip / ornamental / lightweight / cumbersome / ugly | 상감 / 손잡이 / 장식용 / 경량 / 불편 / 난잡한 외형 | | Odyssey trait labels |
| tox / incendiary / EMP rounds | 독성 탄환 / 소이탄 / 펄스 탄환 | | Odyssey `ToxRounds`, `IncendiaryRounds`, `EMPRounds` |
| flare | 조명탄 | | Anomaly `Apparel_DisruptorFlarePack` |
| ignores accuracy penalties | 명중률 감소 무시 | | `AimAssistance.description` — reuse verbatim |
| Traders will pay more / less for it. | 상인들이 더 높은 값을 쳐줍니다. / 상인들은 더 적은 돈을 쳐줍니다. | | `GoldInlay` / `Ugly` descs — reuse verbatim |
| market value / carrying capacity / haul | 시장 가치 / 운반 수량 / 운반 | | Core `MarketValue`, `CarryingCapacity`, `Haul.label` |
| reserved by / forbidden / cannot reach | 예약됨 / 상호작용 금지됨 / 갈 수 없음 | | `IsReservedBy`, `ForbiddenLower`, `CannotReach` |
| Structure (architect category) | 구조물 | 구조 | `Structure.label`=구조물; bare 구조 is the Keyed tab string |
| Cancel / Reset / Confirm / Randomize / Reset to defaults | 취소 / 초기화 / 확인 / 섞기 / 기본값 복원 | 기본값으로 재설정 | Core Keyed; `ResetBinding`=기본값으로 재설정 is keybinding-specific, `RestoreToDefaultSettings`=기본값 복원 is the settings verb |
| Prerequisites / Default / colonist / log | 전제 조건 / 기본값 / 정착민 / 기록 | | Core Keyed, `OpenLogOnWarnings` |
| melee weapon names | 장검 / 창 / 철퇴 / 단검 / 검(gladius) / 도끼 / 전투망치 / 단분자검 / 플라즈마검 / 제우스망치 | | `MeleeWeapon_*` — mostly native words, not katakana-style transliterations |
| mechanite(s) | 기계입자 | 나노머신, 메카나이트 | Core, 36/36 occurrences (근섬유질 기계입자, 부활 기계입자) across 7 files. 나노머신 renders the *different* English word "nanomachines" (Royalty glands); Royalty's monosword desc paraphrases to 나노 기술 and is not a term source. Grounding on Royalty+Biotech alone misses this — it shipped as a bug in PWU and was corrected 2026-07-28 |
| point (of a weapon) | 칼끝 (spear: 끝) | 첨단 | `MeleeWeapon_LongSword.tools.point.label`; 첨단 means "cutting-edge" in every vanilla ko occurrence (첨단 기술, 최첨단 금속 검), so it reads as tech level, not geometry. 최첨단 for "ultratech" in prose is correct and unrelated |
| edge (of a weapon) | 칼날 | | `MeleeWeapon_LongSword.tools.edge.label` |
| cut / stab (**DamageDef** label) | 잘림 / 찔림 | 베임 for cut | Core DamageDefs. The **HediffDef** labels differ: `Cut`=베임, `Stab`=찔림, `Stab.labelNoun`=찔린 상처. Point each def at the right one |
| toxic \<damage\> (DamageDef label) | `찔림 (독성)` shape | | Core `ScratchToxic`=찢김 (독성), `ToxicBite`=물림 (독성) |

The Odyssey trait ports (`Lightweight`, `Cumbersome`, `Ornamental`, `Ugly`,
`GoldInlay`, `JadeInlay`) all have official KO labels, adjectives and
descriptions matching our English; copy them rather than retranslating.

Mod-decided terms pending native review: 개조 (customize) and the research trio
고유 무기 단조 / 고유 무기 기계 가공 / 고유 무기 정밀 조립 (vanilla research name
prefixed with 고유 무기); haul planner modes 순차 / 순회 / 철저; haul plan 운반 계획;
net refund/cost 실질 반환 / 실질 비용 (Korean vanilla has **no** word for refund at
all — 환급 and 환불 are both 0 hits); texture tab 외형 (질감 and 텍스처 are both
rare in vanilla); 연사 속도 / 연사 횟수 / 저지력 (burst speed/count, stopping power —
these KO stat labels are untranslated in vanilla); 아킴보 (akimbo — not an Odyssey
trait); vanilla-behavior suffix (바닐라); progression header 진행; gizmo button
지시 버튼; weapon def 무기 def (kept Latin, as JP and zh do).

### Glossary — German (preseeded from PersonaWeaponsUnbound's 2026-07-28 generation; NOT yet generated here)

Nothing German has been generated in this repo. The rows below were grounded
against the de Core/Royalty/Ideology/Odyssey tars during PWU's run and are
reusable as-is; the bench, charge-weapon and research-stem rows were ground for
this file. Language folder is `German` (tar: `German (Deutsch).tar`).

Style rules from the vanilla de data (mandatory):

- **ASCII single quotes** for cited def labels and UI labels — vanilla writes
  `Forschungsprojekt '{0}'`. Core+Royalty Keyed ship 140 single-quoted
  placeholders and **zero** German `„…"`. Never use `„ "`, `» «`, or curly
  quotes. Pawn names are not quoted.
- **En dash `–`, never em dash `—`** (20 vs 0). The English source uses `—`, so
  every dash needs converting; `<!-- EN: -->` comments keep the English verbatim.
- Ellipsis is ASCII `...` (74 in Core Keyed, `…` zero).
- Descriptions end with `.`; labels and buttons take none. Settings prose is
  informal **du** with imperatives, never Sie.
- `JobDef.reportString` is third-person **with** a terminal period (Core
  `ApplyTechprint` = `wendet TargetB an.`) — unlike ja/ko, which take none. This
  repo's `UWU_CustomizeWeapon_JobDef` should follow suit.
- Research labels are lowercase noun phrases (Hightech-Fabrikation, Maschinenbau,
  Schmieden) or verb-final phrases (Bier brauen, Maschinenpersona überreden).
  This repo's three tiers build on those vanilla stems.

**The RU glossary's anchor row does not transfer.** This file's Russian section
exists largely for свойство-not-черта, because RU uses a different word for pawn
traits. German has no split: Odyssey's `Stat_ThingUniqueWeaponTrait_Label`,
Royalty's `Stat_Thing_PersonaWeaponTrait_Label` **and** Core's pawn-trait
`<Traits>` are all **Merkmale**. Where context doesn't make the weapon clear, use
vanilla's own `StatsReport_WeaponTraits` = **Waffenmerkmale**. Run the lookup
anyway — just expect it to agree.

**Case is the German landmine, not gender** (decompile-verified:
`Verse.GrammarResolverSimple`, `LanguageWorker_German`, `LanguageWordInfo`).
`"key".Translate(args)` reaches `GrammarResolverSimple`, not the rulepack
resolver. Its `obj is string` branch *does* support `{0_gender ? m : f : n}`,
`{0_definite}`, `{0_indefinite}`, `{0_plural}` on a plain string, resolving gender
from the word itself via `WordInfo/Gender/{Male,Female,Neuter,Other}.txt` (~2450
nouns in Core). But it implements **no `lookup` function**, so
`{lookup: {0}; decline; N}` — the only route to the 2457-row `decline.txt` case
forms — silently fails there, and de's article helpers are nominative-only. Gender
is solvable; case is not.

Two live instances in this repo, both plain-string injections (the only two
`.Named()` calls here feed *vanilla* keys, so every UWU-owned key is affected):

- `UWU_RequireRecipeResearchDesc` — "customizing a {0} would require {1}", with
  `{0}` = `Gun_ChargeRifle.label` = **Impulsgewehr** (neuter, and present in the
  Gender tables). The English indefinite article sits where German wants a
  genitive ("die Anpassung eines Impulsgewehrs"), which `decline` cannot supply on
  this path — restructure instead, e.g. `Beispiel: für '{0}' wäre '{1}'
  erforderlich.`
- `UWU_RelicIdeoColorTip` — "{0}'s ideoligion color". The literal `'s` is
  rewritten at runtime by `LanguageWorker_German.PostProcessed` (trailing `'s` →
  `s`, or a bare `'` after s/ß/z/x/ce). German wants a restructure anyway
  (`Ideologiefarbe von {0}`), but never carry the `'s` across, and never write
  `'{0}'s` — a closing ASCII single quote followed by lowercase `s` is silently
  mangled. The checker cannot see this.

Also note `LanguageWorker_German.PostProcessThingLabelForRelic` truncates a
weapon label to its bare weapon noun via `EndsWith` against a hardcoded 26-noun
list (Horn, Lanze, Pulser, Werfer, Axt, Flinte, Bogen, Revolver, Gewehr,
Stoßzahn, Stab, Hammer, Schwert, Pistole, Dolch, Büchse, Kanone, Granaten,
Granate, Keule, Säbel, Messer, Rapier, Klinge, Sense, Speer), falling back to the
substring after the last space or hyphen. Relevant wherever this mod surfaces a
relic name from a weapon label; note Waffe is *not* on the list.

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | Merkmal / Merkmale (standalone: Waffenmerkmale) | Eigenschaft, Attribut | Odyssey `Stat_ThingUniqueWeaponTrait_Label`, `StatsReport_WeaponTraits` |
| unique weapon | einzigartige Waffe | Unikat, besondere Waffe | Odyssey `UniqueWeapon` |
| charge (weapons) | **Impuls-** root: Impulsgewehr, Impulslanze, Impulsschuss | Ladungs-, Lade- | Core `Gun_ChargeRifle`, `Gun_ChargeLance`, `Bullet_ChargeRifle` — exactly parallel to the RU энерг-/заряд- row |
| fueled / electric smithy | Schmiede / elektr. Schmiede | Schmiedeofen, Esse | Core `FueledSmithy`, `ElectricSmithy` — vanilla abbreviates "elektr." |
| machining table | **Werkbank** | Maschinentisch, Drehbank | Core `TableMachining` — landmine: de reuses the generic word Werkbank for this *specific* bench, so avoid Werkbank for a generic "workbench" in any string that also names this one |
| fabrication bench | Fabrikationstisch | Fertigungstisch | Core `FabricationBench.label` |
| smithing / machining / fabrication (research stems) | Schmieden / Maschinenbau / Fabrikation (advanced: Hightech-Fabrikation) | | Core `Smithing`, `Machining`, `Fabrication`, `AdvancedFabrication` |
| advanced components | Hightech-Bauteile | fortschrittliche Komponenten | Core `ComponentSpacer.label`; plain components are Bauteile |
| plasteel / uranium / gold / steel | Plastahl / Uran / Gold / Stahl | Plasteel | Core labels — Plastahl is translated |
| quality / tiers | Qualität / übel·schlecht·normal·gut·exzellent·meisterlich·legendär | | Core `Quality`, `QualityCategory_*` |
| "{0} quality or better" | `Qualität {0} oder besser` | | reshaped from Core `NormalQualityOrBetter` (pre-inflected, untemplatable) |
| tech levels | neolithisch / mittelalterlich / industriell / Raumfahrt / Ultra / Archotech | Weltraum, Ultratech | Core `TechLevel_*`; "tech level" = Techstufe |
| relic | Reliquie | Relikt | Ideology `Relic`, `RelicOf` (reliquary = Reliquienschrein) |
| ideoligion / reform | Ideologie / Ideologie reformieren | Ideoligion | Ideology `IdeoligionOf`, `ReformIdeoligion` — de uses the plain word, no portmanteau |
| colour / appearance | Farbe / Erscheinung | Aussehen | Core `Color`, `Appearance` |
| Crafting (the skill) | Handwerk | Herstellung, Basteln | Core `Crafting.label` |
| bill / recipe (both) | Auftrag | Rezept, Rechnung | Core `TabBills`, `AddBill`, every `Stat_Recipe_*_Desc` — de collapses the two |
| Cancel / Reset / Confirm / Randomize | Abbrechen / Zurücksetzen / Bestätigen / Zufällig | | Core buttons |
| Reset to defaults | Auf Standard zurücksetzen | | Core `ResetBinding`; `Default` = Standard |
| None | Nichts | Keine | Core `None` |
| colonist / research project | Kolonist / Forschungsprojekt | | Core `Colonist`, `NeedResearchBenchDesc` |
| wielder | Träger | Anwender | Royalty weapon-trait descs |
| techprint | Techplan / Techpläne | Techdruck, Blaupause | Core `TechprintLabel` |

Unrelated to German but worth remembering during any generation here: this repo's
`DefInjected/UniqueWeaponsUnbound.TraitCostRuleDef/` folder is namespace-qualified
because the def class is the mod's own. A bare `TraitCostRuleDef` folder silently
drops every entry in it.

### Cross-language lessons

- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans “{0}”) — injected labels never inflect, and quoting
  sidesteps case and agreement problems. **But check how far vanilla actually
  carries it**: the case/agreement motive doesn't exist in zh-Hans, and vanilla
  zh leaves common-noun labels bare (`Equip`=装备{0}), so there quoting narrows
  to named entities (research, ideoligion, trait names) and terse
  stat/job-report templates take none.
- Job-report register does **not** transfer between languages: vanilla zh
  `reportString`s end in 。(研究中。), vanilla JP ones take no period, vanilla de
  ones take a period (`wendet TargetB an.`). Check the target language's own
  `DefInjected/JobDef` before writing any.
- **Know which resolver your strings actually reach** (decompile-verified).
  `"key".Translate(args)` goes to `Verse.GrammarResolverSimple`, *not* the full
  rulepack `GrammarResolver`, and the two support different things. On a plain
  `string` arg `GrammarResolverSimple` gives you `{N_gender ? … : … : …}`,
  `{N_definite}`, `{N_indefinite}`, `{N_plural}` and the pronoun family — gender
  is looked up from the word itself via `LanguageWordInfo`, so no `NamedArgument`
  metadata is needed. It implements **no `lookup` function at all**, so
  `{lookup: {0}; decline; N}` and every case form it would produce are
  unavailable there. For inflecting languages that means gender is usually
  solvable and **case is not**: restructure so nothing has to agree with the
  injected label (drop the article, or move the head noun in front of the
  placeholder). See the German glossary for worked rewrites.
- **A gender lookup that misses defaults to masculine** (`ResolveGender`'s
  `defaultGender`), and mod-coined nouns are never in the vanilla Gender tables —
  so `{N_gender ? …}` on this mod's own weapon labels is a silent coin-flip, not
  a fix. Reserve it for vanilla nouns in nominative slots.
- **Check for a `LanguageWorker_<Language>` before generating** — it post-
  processes every finished string and can impose requirements or rewrites the
  vanilla data never reveals. German's rewrites a trailing `'s`, so a closing
  ASCII single quote followed by lowercase `s` is mangled; Korean's resolves josa
  markers. Decompile with
  `ilspycmd "$RIMWORLD_PATH/RimWorldWin64_Data/Managed/Assembly-CSharp.dll" -t
  "Verse.LanguageWorker_<Language>"`.
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
- **Check for a `LanguageWorker_<Language>` before writing anything.** Some
  languages solve agreement at runtime instead of in the string, which changes
  what correct authoring even looks like — Korean's worker rewrites paired josa
  tokens (`{0}(을)를`) after substitution, so quoting to dodge inflection is
  unnecessary there and hardcoding a particle is an outright bug. Decompile the
  worker rather than inferring the convention from grep counts alone.
- A defensive habit that is right in one language can be actively harmful in
  another: corner brackets are correct in JP, but in KO they break josa
  resolution because the worker only skips `"`, `'` and `)` when looking back
  for the preceding character.

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
