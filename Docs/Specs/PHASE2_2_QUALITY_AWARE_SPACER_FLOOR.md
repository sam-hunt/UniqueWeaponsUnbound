# Phase 2.2 — Quality-aware spacer complexity floor

**Status:** spec'd 2026-07-30 from owner review of live Phase 2.1 numbers;
option A ratified by the owner the same day. The rarity term in the floor is
Fable's recommended default (see "Open sub-choice" below), applied pending any
owner veto. **Implemented 2026-07-30** in the same session.
**Depends on:** Phase 2.1 (rarity multiplier, spacer complexity floor).
**Feeds:** rides Steam update ① with Phases 1–2.1.

## Objective

Phase 2.1's complexity floor (`ApplyConvertAllToSpacer`,
`Source/1.6/Utilities/CostRuleHelpers.cs`) is `ceil(WorkToMake / 6000)`,
computed from the def alone — *after* the quality (priority 200) and rarity
(priority 250) rules have already multiplied the cost list the conversion
discards. On cheap-stuff melee the floor always binds, so both multipliers are
erased: monomolecular on a steel longsword bills 3 advanced components at
every quality from awful to legendary, and zeus-headed on a masterwork steel
warhammer bills the same 3 as on a normal one. Premium-on-premium melee
customization prices flat, while a *weaker* good plasteel longsword bills 6
(owner observation, 2026-07-30; full investigation in the session record).

Fix: the floor rides the same multipliers as the bill it floors, clamped so it
can never drop below the current floor (floor-only, never a discount):

```
floor = max( ceil(complexity),
             ceil(complexity × CostFraction × qualityMult × rarityMult) )
```

where `complexity = WorkToMake / 6000` (existing helper), `CostFraction` is
the pipeline's 0.5 (currently private in `QualityMultiplierWorker` — expose
it; do not duplicate the literal), `qualityMult` is
`QualityMultiplierWorker.GetQualityMultiplier(weapon)` and `rarityMult` is
`RarityMultiplierWorker.GetRarityMultiplier(trait)` (which already exempts
negative traits and misconfigured commonality ≤ 0).

## Rejected alternatives (recorded, do not re-propose without new evidence)

- **B — steepen the quality curve to vanilla's MarketValue factors**
  (legendary 5, masterwork 2.5): fixes the material-vs-quality ratio
  everywhere but reprices every high-quality weapon including ranged
  (capacitor on a legendary charge rifle 6 → 11, gold axe 7 → 13). Owner
  chose A over this blast radius.
- **C — price the canvas off the weapon's MarketValue stat**: the already-
  deferred canvas-value owner question; structural, Phase 3/4 territory.
- Doing nothing about the gold-axe end: gold's SharpDamageMultiplier is 0.75,
  so its 7-component bill is the by-design luxury surcharge (Design
  Constraint 3), not part of the defect. Deliberately untouched here.

## Work items

### 1. Scale the floor in `ApplyConvertAllToSpacer`

- Signature gains the trait: `ApplyConvertAllToSpacer(costs, weapon, trait)`.
  Both call sites are ours (`ConvertToSpacerWorker.Apply`, which already
  receives the trait, serves both charge rules).
- Floor formula as above, applied only on the existing no-components
  condition (unchanged): recipes carrying Component/ComponentSpacer lines
  never see the floor, so all ranged pricing is untouched.
- Make `QualityMultiplierWorker.CostFraction` public (or equivalent) so the
  0.5 is written once.

### 2. Tests (`Tests/1.6/TraitCostUtilityTests.cs`)

Update the two invalidated pins (names + derivation comments):

- `Reference_MonomolecularOnAMasterworkSteelWarhammer…` 3 → 6.
- `Reference_ZeusHeadedOnAMasterworkSteelWarhammer…` 3 → 6.

Re-derive comments where the floor value changed but the outcome didn't
(masterwork plasteel longsword: floor now 6, by-value 9 still wins).

New pins:

- The motivating case: monomolecular on a legendary steel longsword bills 8;
  on a good plasteel longsword 6 — the stronger weapon now costs more.
- Quality ladder is monotonic on a floor-bound weapon (steel longsword +
  monomolecular): normal 3, good 4, excellent 5, masterwork 6, legendary 8.
- Clamp never discounts: a commonality-1 charge-keyword trait on a normal
  steel longsword still bills 3 (scaled term would be 2), and at awful
  quality still 3 (scaled term 2.1 → 3 after clamp… assert the outcome, 3).
- By-value still wins where it should: legendary gold axe (stuff 50, work
  7000) stays 7; masterwork plasteel longsword stays 9.
- Component-line recipes never floor: Tesla/charge-rifle (5) and
  assault-rifle (1) pins stay green untouched.
- Refund symmetry: removing monomolecular from the masterwork steel
  warhammer refunds floor(6 × 0.5) = 3.

## Reference outcomes (steel stuff unless noted)

| Case                                            | Today | After |
| ----------------------------------------------- | ----- | ----- |
| Monomolecular, masterwork warhammer              | 3 CS  | 6 CS  |
| Zeus-headed, masterwork warhammer                | 3 CS  | 6 CS  |
| Monomolecular, legendary longsword               | 3 CS  | 8 CS  |
| Monomolecular, good **plasteel** longsword       | 6 CS  | 6 CS  |
| Monomolecular, normal longsword                  | 3 CS  | 3 CS  |
| Monomolecular, normal knife                      | 1 CS  | 1 CS  |
| Monomolecular, masterwork **plasteel** longsword | 9 CS  | 9 CS  |
| Monomolecular, legendary **gold** axe            | 7 CS  | 7 CS  |
| ChargeCapacitor, legendary charge rifle          | 6 CS  | 6 CS  |
| AA Tesla, charge rifle                           | 5 CS  | 5 CS  |

(CS = ComponentSpacer.)

## Open sub-choice (applied default, owner may veto)

Rarity in the floor: quality-only would move the legendary steel longsword to
4 instead of 8. Included because the floor stands in for the *base bill*,
which Phase 2.1 says rarity owns — the "complexity signature counts are not
scaled" carve-out was about theme surcharges (tox/incendiary/blood), and
those stay unscaled. Vetoing means dropping `rarityMult` from the formula and
re-deriving the two 6s to 4s and the 8 to 4.

## Out of scope

- Rule `description` texts for the two charge rules (they omit the floor
  detail — a pre-existing simplification since Phase 2.1; the dialog always
  shows the actual computed bill, and touching descriptions means an
  8-language translation pass for no behavior).
- Quality-curve steepening (rejected option B) and canvas-value pricing
  (option C) — separate owner questions.
- No XML and no translations: no new defs, labels, or keywords.

## Constraints

- Design Constraints 1–4 bind. Floor-only: the outer `max` guarantees no
  case prices below today's floor.
- Determinism preserved: quality and commonality are def/thing data.
- Live repricing of floor-bound melee at good+ quality rides update ① with
  the rest — same C3-style acceptance as Phases 1–2.1; changelog
  before/after examples per the C3 mitigation.
- Comments plain `//`; log prefix `[Unique Weapons Unbound]`.

## Verification

- Extend/update `Tests/1.6/TraitCostUtilityTests.cs` per item 2; full suite
  via `./Scripts/test-windows.sh`; build + deploy green.
- No coverage-matrix or translation-checker impact expected
  (`check-translations.py` untouched).
