# Traditional Chinese — Unique Weapons Unbound glossary

Grounded in this repo's 2026-08-26 machine-assisted generation pass, mined fresh
from the official zh-Hant Core + Odyssey + Ideology tars. **No native-speaker
review yet.** Family-shared engine mechanics, style/corpus rules (no Chinese
LanguageWorker of either script; 「」 quoting; full-width ：in terse templates;
ASCII parentheses set solid; ASCII spaces around Latin acronyms; the bare
JobDef `reportString`; the dash baseline; the zh-Hans/zh-Hant inversion table)
and vanilla-grounded common vocabulary live in the `l10n/` submodule at
`l10n/languages/ChineseTraditional.md` — read that file first; this one holds
only what is specific to Unique Weapons Unbound.

**Do not derive anything from `ChineseSimplified.md`.** Every term below was
re-grounded against the zh-Hant tars, and this mod's most-repeated words come
out inverted from zh-Hans: unique weapon is 獨特武器 (not 特化武器), charge
weapons take a 電能 root (not 电荷), plasteel is 塑鋼 (not 玻璃钢), and the
parenthetical suffixes are ASCII `()` (zh-Hans writes （）).

## Weapon-trait word check (mandatory per repo — see SKILL.md)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特質 (stats-report title 武器特質) | 特性 | Odyssey `Stat_ThingUniqueWeaponTrait_Label`=特質, `StatsReport_WeaponTraits`=武器特質. Odyssey Keyed `WeaponTraits`=特性 exists, but the stats slots are the nearer analog and UMW's zh-Hant pass already committed to 特質 for this same (Odyssey) domain |
| unique weapon | 獨特武器 | 特化武器 | Odyssey `UniqueWeapon` |

**zh-Hant splits the trait word by DLC and zh-Hans does not.** Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` is **特性**, so PersonaWeaponsUnbound uses
特性 throughout while this repo and UniqueMeleeWeapons use 特質. Never harmonize
the two: each is its own domain DLC's attested form.

## UWU domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| charge (weapons) | 電能 root (電能武器, 電能步槍) | 電荷, 充能- | Core `Gun_ChargeRifle`=電能步槍, `Gun_ChargeLance`=電能長槍 — **inverted vs zh-Hans's 电荷**. 充能 is reserved for the *act* of charging (Odyssey `ChargeCapacitor`=充能電容, `PulseCharger`=脈衝充能器) |
| pulse-charged munitions (`ChargedShot` research) | 高能電磁 | 電荷彈 | Core `ChargedShot.label` — bears no resemblance to the English; another resolve-the-hint-through-the-tar case |
| beam (weapons) | 光束 | | Odyssey `Gun_BeamRepeater`=光束連發槍, `FrequencyAmplifier.description` |
| burst speed / burst count / stopping power | 射速 / 連發次數 / 攔截力 | 抑止能力, 連射次數 | Core `BurstShotFireRate`, `BurstShotCount`, `StoppingPower` |
| accuracy penalty / ignores | 準度懲罰 / 忽略 | 命中率懲罰, 無視 | Odyssey `AimAssistance.description` uses 使武器忽略…準度懲罰 verbatim — this mod's `UWU_IgnoresAccuracyPenalties` describes exactly that trait's effect, so the vanilla wording is the nearest analog (PWU's zh-Hant renders it 無視命中率懲罰; harmonize on PWU's next pass) |
| inlay / grip / ornamental / lightweight | 鑲金・鑲玉 / 定制握把 / 裝飾性 / 輕巧 | 鑲嵌 as a bare label | Odyssey `GoldInlay`, `JadeInlay`, `CustomGrip`, `Ornamental`, `Lightweight` labels |
| toxic / incendiary / EMP | 毒性 / 燃燒 / EMP (ASCII-spaced) | 劇毒 | Odyssey `ToxRounds`=毒性彈藥, `IncendiaryRounds`=燃燒彈, `EMPRounds`=EMP 子彈 |
| flare | 閃焰彈 | 照明彈, 信號彈 | Anomaly `DisruptorFlare`=干擾閃焰彈 — **inverted vs zh-Hans's 照明弹** |
| bioferrite / hemogen pack / steel slag chunk | 生化鐵氧體 / 血原包 / 金屬碎片 | 活鐵, 鋼渣塊 | Anomaly `Bioferrite`, Biotech `HemogenPack`, Core `ChunkSlagSteel` |
| thrumbofur / birdskin / herbal medicine / chemfuel | 獨角獸皮 / 鳥皮 / 草藥 / 化合燃料 | | Core labels |
| components / advanced components | 零件 / 高級零件 | 零部件, 元件 | Core `ComponentIndustrial`, `ComponentSpacer` |
| workbench (generic) | 工作桌 | 工作台 | Every vanilla bench *description* says 工作桌 (`ElectricSmithy`, `FueledSmithy`, `TableMachining`, `FabricationBench`); 工作台 appears only in generic help prose, so the bench descriptions are the nearer analog. Matches PWU's zh-Hant |
| smithy / machining table / fabrication bench | 鍛造桌 / 機械加工桌 / 精密製作桌 | 精密裝配台 | Core `FueledSmithy`/`ElectricSmithy`, `TableMachining`, `FabricationBench` labels |
| tech levels | 原始部落 / 中世紀 / 工業 / 太空 / 高科技 / 遠古科技 | 石器時代, 極致科技, 超凡科技 | Core `TechLevel_*` |
| quality tiers / quality | 糟糕・劣質・普通・良好・傑出・大師・傳奇 / 品質 | | Core `QualityCategory_*`, `Quality` |
| market value | 市場價值 in cost prose (stat label `MarketValue`=基本價值, tooltip `MarketValueTip`=市場價格) | | Core Keyed — slot-dependent; the cost rules describe a valuation, so the tooltip form's root is the closer read |
| cost | 成本 | 花費 | Core `Difficulty_MaintenanceCostFactor_Label`=砲塔整備成本, `AddictionCost`=成癮消耗成本. PWU's zh-Hant uses 花費 (also vanilla-attested, in the "pay {0} silver" slot); this repo's strings are a cost *model* (base cost, multipliers, floors, caps), which is 成本's register |
| refund | 返還 | 退還 | Core `Flagstone*.description` 拆除石板路並不會返還資源 — an exact slot match. PWU's zh-Hant uses 退還; harmonize on its next pass |
| forbidden / reserved / unreachable | 禁用 / 預留 / 無法到達 | | Core `CommandForbid`, `Reserved`, `CannotReach` |
| haul / ingredients / stuff | 搬運 / 材料 / 素材 | | Core `HaulFromSource`, `Ingredients`=材料, `FabricationBench.description` 簡易素材 |
| research project / research tree | 研究項目 / 研究視窗 | 研究樹 | Core `NeedResearchProject`, `ClickToOpenResearchTab`=點擊打開研究視窗 — vanilla zh-Hant has no "tree" metaphor (PWU's zh-Hant coined 研究樹) |
| relic / ideoligion / reform an ideoligion | 聖物 / 理念 / 重組理念 | 改革理念 | Ideology `IdeoRelic`, `CustomizeIdeoligion`, `ReformIdeoligion` |
| customize / appearance | 自訂 / 外觀 | 客製化, 定制 | Core Keyed `Customize`, `Appearance` |
| Cancel / Confirm / Randomize / Reset / reset to defaults | 取消 / 確定 / 隨機生成 / 重置 / 恢復為預設值 | | Core Keyed |
| log / palette / colonist / menu / right-click | 日誌 / 調色盤 / 殖民者 / 選單 / 右鍵 | 記錄 (log) | Core `LogFileFolder`, `StartDevPaletteOn`, `Colonist`, `BillsTab.helpText` |
| Biotech (DLC name) | 「生機」 | Biotech | Core `SimulateNotOwningBiotech` — zh-Hant localizes DLC names in corner brackets (「皇權」/「漫遊」/「理念」/「異邪」/「生機」) |

## Mod-decided terms pending native review (2026-08-26)

Research trio **獨特武器鍛造 / 獨特武器機械加工 / 獨特武器精密製作** (Odyssey's
獨特武器 prefixed to Core's own `Smithing`=鍛造 / `Machining`=機械加工 /
`Fabrication`=精密製作, matching vanilla's terse research-label style); haul
planner modes **循序 / 集中 / 徹底** (reused verbatim from PWU's zh-Hant — no
vanilla term exists, so sibling consistency governs); net refund/cost
**淨返還 / 淨成本**; texture tab **貼圖** (0 hits in vanilla zh-Hant, so it is
free to coin; 紋理 is spent on `TextureCompression`=紋理壓縮 and 材料 on
`Stat_Stuff_Name`); vanilla-behavior suffix **(原版)**; "must disarm from
hostile" **必須從敵人身上繳械取得** (PWU's wording; vanilla only has
`DisarmedTime`); progression section header **進度** (41 corpus hits; PWU's 進程
has 0 and is ungrounded); gizmo button **指令按鈕**; weapon def **武器def**
(Latin kept solid, as ja and zh-Hans do); **稀有度** (rarity as an abstract
noun — the adjective 稀有 is vanilla-attested, the noun is not);
**太空科技特質最低成本** (the "Advanced trait minimum cost" label rendered with
太空科技 to match its Desc's "spacer-tech" rather than coining a separate term);
**貧鈾彈** (the Workshop description's depleted-uranium example — a modded trait
name, not a vanilla one); Workshop title / `UWU_SettingsCategory`
**獨特武器解放** (獨特武器 is the searchable Odyssey anchor the
`.steamworkshop/README.md` convention requires; 解放 renders "Unbound", matching
both the zh-Hans sibling and PWU's zh-Hant 羈絆武器解放).

**Ideology colour sections take bare 理念, not 「理念」.** `UWU_IdeologyColors`
sits in a row of three in-dialog headers beside 武器顏色 and 建築顏色, and the
swatches genuinely are the ideoligion's colours rather than a DLC citation, so
the corner brackets vanilla reserves for DLC *brand* names would be noise there.
The same reading carries into `UWU_EnableIdeoColors`/`Desc`. Contrast
`UWU_Blood.description`, which really does name the Biotech DLC and so keeps
「生機」.

**"Structure colors" is 建築顏色.** Ideology's Keyed `Structure`=結構 is the
ideoligion-structure slot, not this one; the palette is Core's `Structure_*`
ColorDefs, which paint buildings (`DesignatorPaintBuilding` 粉刷建築).

## `labelKeywords` — what was appended and why

Every English keyword is kept verbatim (they match the language-invariant
defName tokens); confirmed zh-Hant *whole labels* are appended, because a
zh-Hant label carries no ASCII space or hyphen and is therefore a single token.
The shipped additions, all exact labels from vanilla Odyssey or UMW's zh-Hant
tree: 淬毒・鴉片塗層・毒性彈藥・毒性彈丸 (ToxSwap), 燃燒彈・燃燒彈丸
(IncendiarySwap), 輕巧 (Lightweight), 閃焰彈 (Flarestriker),
單分子刃・等離子內芯・宙斯錘頭 (ChargeUnconditional), 裝飾性・琺瑯 (Ornamental),
穿甲尖刺・倒刺・配重・稜脊錘頭・頭重・針尖・劍格・剃刀刃・鋸齒刃・嵌釘錘頭
(MetalFittings), 染血 (Blood), 鑲金・鑲玉・鑲翡翠 (Inlay).

Deliberately not injected:

- **`UWU_HeavyScrap`** — sets `requireAllKeywords`, so any whole-list
  replacement becomes unsatisfiable and kills the English heavy+scrap match.
- **`UWU_EmpSplit`** — **vanilla zh-Hant writes EMP with ASCII spaces**
  (`EMPRounds`=EMP 子彈, `EMPPulser`=EMP 脈衝器, `EMPLauncher`= EMP 加農砲), so
  those labels already tokenize to `{emp, 子彈}` and the English `emp` keyword
  matches them token-for-token. Nothing to add. (This is the one place the
  zh-Hant acronym-spacing rule does real mechanical work.)
- **`UWU_ChargeCategoryGated`** — every vanilla charge trait's defName already
  carries `charge`/`charger`/`frequency`/`capacitor`.
- **Bare roots** 握把 and 鑲嵌 — the real labels are 定制握把 / 鑲金 / 鑲玉, so
  the roots match nothing on their own; 宙斯・單分子劍・等離子劍 are
  weapon-name roots no trait uses alone; 舒適 is extrapolated from the Comfort
  StatDef with no confirmed trait wording.

## Known limitation, not a translation bug

`UWU_MaterialOverride`'s auto-detection (`CostRuleHelpers.GetMaterialOverride`)
looks up `materialsByLabel`, keyed by the *localized* material label, using
tokens from the trait label and then the defName. In zh-Hant (as in zh-Hans, ja
and ko) 鑲金 is a single token that never equals 黃金, and the English defName
tokens never equal a localized material label either — so the fallback rule is
effectively inert in CJK languages. Its `description` is dev-facing
documentation (no code reads `TraitCostRuleDef.label`/`.description`), so the
translated example is illustrative; fixing the matching would be a C# change,
not a localization one.
