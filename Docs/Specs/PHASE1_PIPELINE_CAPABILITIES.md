# Phase 1 — Pipeline Capabilities (C#)

**Status:** implemented 2026-07-29, including both stretch items; suite green
(155 tests). One deviation: `SplitBaseMaterials` explicitly excludes
`ComponentIndustrial`/`ComponentSpacer` — vanilla components declare
`stuffProps` (texture tinting) so they pass `IsRawResource`, and without the
exclusion item 3 would have repriced flare-style value-splits on every
component recipe, a delta the rollout plan (C3) never ratified. Spec'd
2026-07-29.
**Depends on:** nothing. **Feeds:** Phase 2 (generic vocabulary XML), Phase 3 (Alpha
Armoury rule XML). Ships in Steam update ① together with Phase 2, timed with
Unique Melee Weapons' release.

## Objective

Land every C# capability the trait-cost rework needs, so Phases 2–3 are pure
XML/translation work. Two kinds of change:

- **Live defect fixes** (items 1–5): change shipped behavior immediately — these
  are the F1/F2/F4 fixes, ratified for 1.6 mainline.
- **Dormant capabilities** (items 6–8): new/generalized workers and def fields
  that no shipped XML uses yet; activated by Phase 2/3 XML.

## Required reading (in order)

1. `Docs/Research/MELEE_TRAIT_COST_PIPELINE.md` — full context. Design
   Constraints 1–4 are non-negotiable; the Verification addendum (V1–V6) and
   Open considerations (O1–O11) carry the ratified decisions this spec encodes.
2. `Source/1.6/Utilities/TraitCostUtility.cs` (`RunPipeline`),
   `Source/1.6/Utilities/CostRuleHelpers.cs`,
   `Source/1.6/TraitCostRules/*.cs`, `Source/1.6/Defs/TraitCostRuleDef.cs`.
3. `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml` — current rules and the
   TODO comments Phase 3 will implement (their required worker shapes constrain
   item 6).

## Work items

### 1. Matcher: union defName tokens into the match set (fixes F1; O9 as decided)

In `RunPipeline`, build the word set from **both** `trait.label` (via existing
`SplitLabelWords`) and the trait's `defName`, tokenized as follows (decided
2026-07-29):

- If the defName contains `_`, **strip the first underscore-delimited segment**
  (the mod-prefix acronym: `AArmoury_Oversized` → `Oversized`,
  `VWE_ChargeRifle` → `ChargeRifle`). DefNames without `_` are untouched
  (vanilla convention: `AimAssistance`).
- Split the remainder on PascalCase boundaries **and any non-letter character**
  (underscores, digits) — note `SplitPascalCase`
  (`CostRuleHelpers.cs:358`) currently splits on neither; extend it or add a new
  helper. Lowercase everything.
- Keep `CostRuleHelpers.GetMaterialOverride`'s defName fallback consistent with
  the new tokenizer (it currently uses raw `SplitPascalCase` + space-only
  `TryMatchWords`; unify so both paths see identical tokens).

Acceptance: a trait whose label is fully localized (simulate by using a label
with no English words) still matches its rules via defName tokens; a trait
defName prefixed with a keyword-colliding acronym (e.g. `ARC_HeavyBarrel`)
does **not** match the `arc` keyword.

### 2. Component lookup: recognize `ComponentSpacer` (fixes F2 gap)

`ApplyComponentSwapOrSplit` (`CostRuleHelpers.cs:131`) checks only
`ComponentIndustrial`. Extend: if no industrial entry, look for a
`ComponentSpacer` entry and swap that (count × multiplier), before falling
through to the split branch. Charge weapons (`Gun_ChargeRifle` costList:
Plasteel 50 + ComponentSpacer 2) then take the swap path.

### 3. Stuff-agnostic split (R2)

`SplitBaseMaterials` (`CostRuleHelpers.cs:230`) hardcodes WoodLog/Steel/Plasteel.
Split **any** entry passing `CostRuleHelpers.IsRawResource` instead. This is only
safe alongside item 4 (see R2's hazard note in the research doc: value-preserving
conversion across tier gaps yields nonsense counts).

### 4. Complexity-derived signature counts (R3; O1–O4 as decided)

Add a third branch to `ApplyComponentSwapOrSplit`: when no component entry
exists, compute `complexity = WorkToMake / 6000f` (divisor is a **hardcoded
constant** per O1) and bill `ceil(complexity × componentMultiplier)` of the
replacement material (minimum 1), instead of value-splitting the stuff.
Reference outcomes: tox (3× herbal) → 9 herbal on any warhammer, 1 on a knife.

- Resolve `WorkToMake` from the same def `BaseCostFromRecipeWorker` resolves
  (base variant via `WeaponRegistry.GetBaseVariant`, fallback `weapon.def`).
- **Tech-tier material selection (O3/O4, ratified):** where a bill's material
  has an industrial/spacer pair, select by the weapon's tech level —
  ≤ Industrial → the industrial-tier def, ≥ Spacer → the spacer-tier def.
  Concretely: component bills use `ComponentIndustrial`/`ComponentSpacer`;
  the Phase 2 fittings rule uses `Steel`/`Plasteel`. Implement as a small
  helper (e.g. `SelectByTechLevel(weapon, industrialDef, spacerDef)`); single-
  material replacements (herbal medicine, chemfuel, bioferrite) are unaffected.
- Linear fit accepted as-is (O2); Minigun outlier tolerated. No special refund
  handling (O6): complexity-derived lines flow through
  `GetRemovalCost`/`GetTotalRefund` at `RefundRate` like any other cost.
- Verify O7 in tests: `NegativeDowngradeWorker` should downgrade a
  tech-selected bill sensibly (spacer→industrial etc.) with no code change
  expected.

### 5. `ApplyMaterialOverride` converts by value (R5, decided 2026-07-29)

Replace the 1:1-by-count re-emit (`CostRuleHelpers.cs:321`): sum the **market
value** of raw-resource entries, re-emit
`ceil(totalValue / overrideMaterial.BaseMarketValue)` (minimum 1) of the
override material. Non-raw entries still pass through. Reference outcome: gold
inlay on a masterwork steel warhammer bills ~29 gold (was 150).

### 6. `AddIngredientsWorker` (new; dormant until Phases 2–3)

A worker that **adds** ingredient lines on top of the computed cost — the
pipeline's first additive worker. Required shape, derived from the Phase 2 blood
rule and the eleven Phase 3 TODO rules (read them before designing fields):

- Def-specified ingredient(s): defName **string** resolved once via
  `GetNamedSilentFail` — unresolvable ⇒ the rule is inert (no hard mod/DLC
  dependency; log once at startup in dev mode, not per-call).
- Optional **fallback ingredient** (hellsphere: `SignalChip`, fallback
  `ComponentSpacer`).
- Fixed count per ingredient.
- **`refundable` flag**: when false, the worker adds nothing on the
  `isRemoval: true` pipeline — that is the entire "unrefundable" mechanism.
- Optional **tech-tiered variant** (healing/lifesteal rule: 10× medicine where
  the medicine def follows the weapon's tech level — reuse the item 4 helper,
  extended to a three-tier herbal/industrial/ultratech pick if needed).

New def fields go on `TraitCostRuleDef` (plain public fields, XML-loaded; no
scribing, so PascalCase-vs-camelCase per .editorconfig for non-scribed fields).

### 7. Worker generalizations (dormant until Phases 2–3)

- **`CostFactorWorker`**: like `DoubleCostWorker` but with a def-specified
  factor (needed: akimbo 2.0, oversized ~1.5, undersized 0.65). Either
  generalize `DoubleCostWorker` (XML keeps working if the factor defaults to 2)
  or add alongside; implementer's choice.
- **Fittings-capable partial swap**: `PartialSwapWorker` currently swaps a
  def-implicit wood→birdskin fraction. Phase 2's fittings rule needs "swap a
  fraction of the weapon's *stuff entry* (whatever it is) to a tech-selected
  material (Steel/Plasteel per item 4's helper)". Generalize or add a sibling
  worker.

### 8. `labelKeywords` becomes translator-extensible (C1)

Decorate `TraitCostRuleDef.labelKeywords` with `[TranslationCanChangeCount]`
so language packs may whole-list-replace with a different entry count (verified
mechanism: V4 in the research doc). No matcher change needed beyond item 1 —
the convention (translators keep English words, append localized ones) is
documented in Phase 2.

### 9. Stretch (do only if the session has capacity)

- `BaseCostFromRecipeWorker` fallback to a standalone `RecipeDef` whose product
  is the weapon (covers `Gun_BeamGraser` + Odyssey's `Make_BeamGraser`); today
  only `costList`/`costStuffCount` are read.
- Dev-mode diagnostic: a dump (log or debug action) of every `WeaponTraitDef` →
  matched rules, for auditing keyword regressions (motivated by O9).

## Out of scope

Any XML keyword/rule changes (Phase 2/3), translations (Phase 2), any new mod
settings (O1 decided hardcoded), price-paid ledger (deferred, see C3), MP
settings sync (V6 — separate effort).

## Constraints

- Design Constraints 1–4 in the research doc bind all decisions.
- Determinism: no RNG/clock in the pipeline (V6). Caches built at startup only.
- Comments plain `//`; log prefix `[Unique Weapons Unbound]`; match existing
  code style.
- Items 1–5 change live costs; that is ratified (C3). Items 6–8 must be
  behavior-neutral until XML uses them — prove with a test that the shipped
  rule set produces identical costs before/after for a representative matrix,
  excluding the intended item 1–5 deltas.

## Verification

- Extend `Tests/1.6/TraitCostUtilityTests.cs`; run `./Scripts/test-windows.sh`
  (WSL cannot host the net472 runner). Cover: defName-only matching, prefix
  strip (incl. acronym-collision negative case), spacer-component swap,
  exotic-stuff split (jade/gold/stony no longer no-op), complexity counts
  (knife/warhammer/charge-rifle references above), tech-tier selection both
  sides of the boundary, by-value material override (29-gold reference),
  additive worker refundable/unrefundable asymmetry, negative-trait interaction
  (O7), prune cap not overflowed by stuff + signature + surcharge (O8).
- `dotnet build UniqueWeaponsUnbound.sln -c Release` clean; smoke-test in-game
  via the deployed build if feasible.

## Suggested orchestration

Fable orchestrates and reviews every diff; no subagent commits — commits happen
only as the owner directs. Sonnet scouts: re-verify the file:line anchors above
before editing (they may have drifted) and enumerate current worker/test
structure. Opus implementers: one for the matcher work (items 1, 8), one for
the cost-math work (items 2–5), one for new workers + tests (items 6–7);
tests may be written alongside each item or by a dedicated implementer.
