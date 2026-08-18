# Japanese — Unique Weapons Unbound glossary

Machine-assisted generation, 2026-07; no native review yet. Family-shared
mechanics (no `LanguageWorker_Japanese`, ASCII punctuation, quote-mark
slotting, register-by-def-type, `traitAdjectives`/name-grammar rules,
battle-log grammar, `stuffProps.stuffAdjective`, and vanilla-grounded common
vocabulary) live in the `l10n/` submodule at `l10n/languages/Japanese.md` —
this file holds only what is specific to Unique Weapons Unbound.

## Weapon-trait word check (mandatory per repo — see SKILL.md)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 特性 (stats-entry title 武器の特性) | 特性・特徴 | `WeaponTraits`=特性, and JP shares the pawn-trait word (unlike Russian). But the DLC domains still diverge: 特性・特徴 is Royalty's *persona*-weapon word (`Stat_Thing_PersonaWeaponTrait_Label`), so it belongs to PersonaWeaponsUnbound, not here |

## UWU domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| unique weapon | ユニークな武器 | | vanilla `UniqueWeapon`, Odyssey `*_Unique` labels |
| Pulse-charged munitions (ChargedShot research) | チャージライフル | パルス弾 | JP names the research after the rifle; disambiguate as 「チャージライフル」の研究 |
| fueled / electric smithy | 工作台 / 電動工作台 | 鍛冶場 | vanilla building labels |
| machining table | 精密工作機械 | | vanilla building label (also the Machining research name) |
| fabrication bench | コンポーネント工作台 | | vanilla building label |
| ultratech | 最先端の技術力 (noun) / 最先端技術級 (attributive) | ウルトラテック | vanilla `TechLevel_Ultra` |
| ideoligion | 思想 | イデオリギオン | JP does not coin a portmanteau; relic = レリック |
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

The six Odyssey trait ports (`Lightweight`, `Cumbersome`, `Ornamental`,
`Ugly`, `GoldInlay`, `JadeInlay`) have official JP labels, adjectives and —
for four of them — descriptions that match our English word for word; copy
them rather than retranslating.

Mod-decided terms pending native review: research trio ユニーク武器の鍛冶 /
ユニーク武器の精密加工 / ユニーク武器の組立製造; haul planner modes 順次 / 巡回 /
徹底; net refund/cost 実質返却 / 実質コスト; haul plan 運搬計画.

From UniqueMeleeWeapons' 2026-07 JP pass (kept here for the family-shared
glossary; UMW's own defs), also pending native review: 受け流し
(parry, register-matched to `TextMote_Dodge` 回避), 戦士団 (warband, parallel to
vanilla 傭兵団), 頭目 (warlord), 鍔 / クロスガード (quillons / crossguard),
地響き (earthshake), 鼓舞の叫び (rallying cry), 士気高揚 (rallied), 由緒ある
(storied), 杭打ちヘッド (piledriver), アヘン塗布 (opiated), 琺瑯 (enameled),
無反発 (dead-blow, from the real tool term 無反発ハンマー), 稜付き (flanged),
鋲打ち (studded), 徹甲スパイク (armor spike), 先重心 (head-weighted), 素早い
(quickdraw — vanilla's 早撃ちの is ranged-specific and wrong on melee).
