# Korean — Unique Weapons Unbound glossary

Machine-assisted generation, 2026-07 (extended through 2026-07-30); no
native review yet. Family-shared mechanics (the josa-marker rules, register
splits, quoting conventions, and vanilla-grounded common vocabulary —
including the Cancel/Reset/Confirm/Randomize button row and "Traders will
pay more/less for it.") live in the `l10n/` submodule at
`l10n/languages/Korean.md` — this file holds only what is specific to
Unique Weapons Unbound.

## Weapon-trait word check (mandatory per repo — see SKILL.md)

| English | Use | Never | Why |
|---|---|---|---|
| trait (weapon) | 특성 (stats title 무기 특성) | 개성 | Odyssey `WeaponTraits`; 개성 is Royalty's *persona*-weapon word (`Stat_Thing_PersonaWeaponTrait_Label`) |
| unique weapon | 고유 무기 | | Odyssey `UniqueWeapon` |

## UWU domain vocabulary

| English | Use | Never | Why |
|---|---|---|---|
| customize | 개조 | 사용자 지정 | 개조 is the physical-modification sense (Core 신체 개조, 개조된); 사용자 지정 is the software-UI sense and wrong for a workbench operation |
| tech level (the concept) | 기술 수준 | 기술 등급 | `CantSendMilitaryAidInTime`; 기술 등급 means **skill** level (`BestSkillInfoLevel`=기술 등급 {0}) — the trap is the mirror image of zh-Hans, which went the other way |
| tech levels | 원시 / 중세 / 산업 / 우주 / 미래 / 초월 | | `TechLevel_*` — note ultratech=**미래**, archotech=**초월**, so neither transliterates |
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
| chemfuel / herbal medicine | 화학연료 / 생약 | 약초 | Core labels |
| bioferrite / thrumbofur / birdskin / steel slag chunk | 생체강 / 트럼보 모피 / 새 가죽 / 고철 덩어리 | | Anomaly `Bioferrite`; Core `Leather_Thrumbo`, `Leather_Bird`, `ChunkSlagSteel` |
| jade (inlay) | 옥 상감 | 비취옥 상감 | `JadeInlay`=옥 상감 even though `Jade.label`=비취옥 |
| inlay / grip / ornamental / lightweight / cumbersome / ugly | 상감 / 손잡이 / 장식용 / 경량 / 불편 / 난잡한 외형 | | Odyssey trait labels |
| tox / incendiary / EMP rounds | 독성 탄환 / 소이탄 / 펄스 탄환 | | Odyssey `ToxRounds`, `IncendiaryRounds`, `EMPRounds` |
| flare | 조명탄 | | Anomaly `Apparel_DisruptorFlarePack` |
| ignores accuracy penalties | 명중률 감소 무시 | | `AimAssistance.description` — reuse verbatim |
| haul / carrying capacity | 운반 / 운반 수량 | | Core `Haul.label`, `CarryingCapacity` |
| reserved by / forbidden / cannot reach | 예약됨 / 상호작용 금지됨 / 갈 수 없음 | | `IsReservedBy`, `ForbiddenLower`, `CannotReach` |
| Structure (architect category) | 구조물 | 구조 | `Structure.label`=구조물; bare 구조 is the Keyed tab string |
| Prerequisites / colonist / log | 전제 조건 / 정착민 / 기록 | | Core Keyed, `OpenLogOnWarnings` |
| melee weapon names | 장검 / 창 / 철퇴 / 단검 / 검(gladius) / 도끼 / 전투망치 / 단분자검 / 플라즈마검 / 제우스망치 | | `MeleeWeapon_*` — mostly native words, not katakana-style transliterations |
| mechanite(s) | 기계입자 | 나노머신, 메카나이트 | Core, 36/36 occurrences (근섬유질 기계입자, 부활 기계입자) across 7 files. 나노머신 renders the *different* English word "nanomachines" (Royalty glands); Royalty's monosword desc paraphrases to 나노 기술 and is not a term source. Grounding on Royalty+Biotech alone misses this — it shipped as a bug in PersonaWeaponsUnbound and was corrected 2026-07-28 |
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
지시 버튼; weapon def 무기 def (kept Latin, as JP and zh do); 고급 특성 (advanced
trait, 2026-08-02 — mirrors the English label's shorthand for spacer-tech traits
via the established 고급 부품; no vanilla precedent for the fixed phrase).
