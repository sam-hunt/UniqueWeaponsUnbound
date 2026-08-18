# Simplified Chinese — Unique Weapons Unbound glossary

Machine-assisted generation, 2026-07 (seeded from UniqueMeleeWeapons' pass);
no native review yet. Family-shared mechanics (`LanguageWorker_ChineseSimplified`
imposes no authoring requirements, full-width punctuation, the two-style
quoting split, job-report register, and vanilla-grounded common vocabulary —
including customize, the fueled/electric smithy through fabrication-bench
research family, ideoligion/relic, tech levels, plasteel and other
materials, and the Cancel/Reset button row) live in the `l10n/` submodule at
`l10n/languages/ChineseSimplified.md` — that file's "Excluded from this
reference" section names exactly what stays here. This file holds only what
is specific to Unique Weapons Unbound.

## Weapon-trait word check (mandatory per repo — see SKILL.md)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器特性) | — | Odyssey `WeaponTraits` / `StatsReport_WeaponTraits` |
| unique weapon | 特化武器 | 独特武器 | Odyssey `UniqueWeapon` |

## UWU domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| charge (weapons) | 电荷 root | 能量- / 充能- | Core `Gun_ChargeRifle`=电荷步枪, `ChargedShot` research=电荷弹 — zh keeps "charge" literal where RU switched to an energy root; never extrapolate |
| beam (weapons) | 光束 | | Core/Odyssey `Beam`, `BeamBypassShields` |
| burst speed / burst count / stopping power | 射速 / 连射次数 / 抑止能力 | | Core `BurstShotFireRate`, `BurstShotCount`, `StoppingPower` |
| accuracy penalty / ignores | 精度惩罚 / 无视 | | Odyssey `AimAssistance.description` uses both verbatim |
| inlay / grip / ornamental / lightweight | 镶嵌 / 握把 / 华丽 / 轻便 | | Odyssey `GoldInlay`, `CustomGrip`, `Ornamental`, `Lightweight` labels |
| toxic / incendiary / EMP | 剧毒 / 燃烧 / EMP | 毒性 | Odyssey `ToxRounds`, `IncendiaryRounds`, `EMPRounds` |
| flare | 照明弹 | 信号弹 | Anomaly `Apparel_DisruptorFlarePack` verb + chargeNoun |

Mod-decided terms pending native review: research trio 特化武器锻造 / 特化武器机械加工 /
特化武器精密装配 (vanilla research name prefixed with 特化武器); haul planner modes
逐次 / 巡回 / 彻底; haul plan 搬运计划; net refund/cost 实际返还 / 实际成本
(no vanilla term for "refund" exists at all); texture tab 贴图 (材质 is taken —
it is `Stat_Stuff_Name`, i.e. *material*); vanilla-behavior suffix （原版）;
"must disarm from hostile" 必须从敌人手中缴获 (vanilla only has `DisarmedTime`=解除武装);
progression section header 进度; gizmo button 指令按钮; weapon def 武器def (kept
Latin, as JP does); 稀有度 (rarity as a noun, 2026-08-02 — the adjective 稀有 is
vanilla-attested, the abstract noun is not); 太空时代特性最低成本 (the "Advanced
trait minimum cost" label rendered with 太空时代 to match its Desc rather than
coining a separate "advanced trait" term).

Unrelated to zh but worth remembering during any generation here: this
repo's `DefInjected/UniqueWeaponsUnbound.TraitCostRuleDef/` folder is
namespace-qualified because the def class is the mod's own. A bare
`TraitCostRuleDef` folder silently drops every entry in it.
