# Phase 2.1 — Trait Power Pricing (rarity heuristic + ultratech corrections)

**Status:** spec'd 2026-07-30 from owner review of Phases 1–2; **ratified in
full by the owner the same day** — the three flagged decisions (see "Ratified
decisions" at the end) plus a full-spec review. Ready to implement; nothing
implemented yet.
**Depends on:** Phase 1 (complexity helper, by-value conversions), Phase 2
(keyword lists + 8-language injections). **Feeds:** rides Steam update ① with
Phases 1–2; Phase 3 tunes AA on top of it.

## Objective

The pipeline prices `f(weapon recipe value, quality, theme)` and carries no
per-trait power term. After Phase 1's by-value conversions, low-complexity
melee weapons buy top-tier traits for trivial amounts (monomolecular = 1–4
advanced components; gold inlay's MV ×2 for 10–30 gold). Close the gap with a
conservative rarity multiplier grounded in `WeaponTraitDef.commonality`, a
complexity floor on the spacer conversion, and a thematic correction moving
`zeus` to the ultratech tier.

## Grounding (corpus survey, 2026-07-30)

`commonality` is a pure selection weight (`RandomElementByWeight` in
`CompUniqueWeapon.InitializeTraits`), XML default 0 (vanilla `ConfigErrors`
flags ≤ 0; every def in all three corpora sets it explicitly). How the corpora
use it:

- **Vanilla/Odyssey (42):** structural, not power — all mechanical traits 1.0
  (including `ChargeCapacitor`, the strongest); 0.5 reserved for
  ability/attachment traits and cosmetics (`GoldInlay`, `JadeInlay`,
  `Ornamental`, `Ugly`).
- **UMW (28):** deliberate power throttle — `Monomolecular`, `PlasmaCored`,
  `ZeusHeaded`, `Piledriver`, `Storied` demoted to 0.5 by authorial intent
  (code comments say so), alongside the cosmetics.
- **Alpha Armoury (89):** inconsistent — scales at the extremes
  (`RandomProjectiles` 0.05/+2000 MV, `DeadlifeLauncher` 0.1/+1000) but its
  strongest passives (teleports, `MimicCore`, `SharpshootersFocus`) sit at a
  flat 0.25/0.5 next to mild traits like `Undersized`.

**Signals rejected during this review** (record; do not re-propose):

- Trait MarketValue — already rejected by Design Constraint 2, and now also
  empirically: AA's strongest traits carry _no_ MV offset/factor at all.
- `abilityProps != null` component surcharge — vanilla's 0.5 "device" class
  conflates structural class with tech tier; UMW's Piledriver (earthshake) and
  Storied (rallying cry) are low-tech abilities where an advanced-component
  surcharge is thematically wrong, and other mods will miss the same way.

Because the heuristic only ever raises prices (multiplier ≥ 1) the worst
misfire is a bounded overprice on a structurally-rare-but-mild trait —
acceptable under Design Constraint 3 (overpricing is a feature).

## Work items

### 1. `RarityMultiplierWorker` + `UWU_RarityMultiplier` (foundation band)

New keywordless rule at **priority 250** (after `UWU_QualityMultiplier` 200,
before `UWU_NegativeDowngrade` 300), scaling every entry's count like the
quality worker does. Keywordless rules match unconditionally
(`TraitCostRuleWorker.Matches`, `Source/1.6/TraitCostRules/TraitCostRuleWorker.cs:19`).

- `multiplier = clamp(1 / trait.commonality, 1, RarityCapMax)` with
  `RarityCapMax = 2` (hardcoded constant beside `ComplexityWorkDivisor`, per
  the O1 precedent). Cap 2 ratified over cap 3 (owner, 2026-07-30): it halves
  the AA-wide blast radius (its dominant 0.25 tier lands at 2×, not 3×); the
  accepted tradeoff is that `RandomProjectiles`-class monsters stay
  underpriced until Phase 3's per-trait AA tuning.
- `commonality <= 0` → multiplier 1 (misconfigured def; vanilla already logs a
  config error — do not reward it with 1/0).
- **Exempt negative traits** via `TraitCostUtility.IsNegativeTrait`
  (`Source/1.6/Utilities/TraitCostUtility.cs:90`) — fixes vanilla `Ugly`
  (0.5), `Cumbersome`, UMW `Carbonized`/`BloodStained` (MV factor 0.8).
- Round with `CeilToInt` per entry (matches quality worker's behavior).

Deliberate properties, assert in tests:

- Runs _before_ the theme conversions, so by-value conversions inherit the
  scaled base naturally, and refunds stay symmetric for free (rarity applies
  inside `RunPipeline`, both directions).
- Complexity-derived signature counts (tox/incendiary third branch) and fixed
  additive surcharges (`UWU_Blood`'s 10 hemogen packs, priority 2200) are
  **not** scaled — theme owns those; rarity owns the base bill.
- `UWU_HeavyScrap` (priority 1500) replaces the whole list later, so heavy
  scrap traits stay 1 slag chunk regardless of rarity.

Known accepted misfires (document in the XML comment): AA `Oversized` /
`Undersized` (0.25, no MV data → not detected negative) stack rarity with
their cost factors; Phase 3's AA tuning is where that gets revisited.

### 2. Complexity floor on the spacer conversion

`ApplyConvertAllToSpacer` (`Source/1.6/Utilities/CostRuleHelpers.cs:288`)
currently bills `ceil(totalValue / 200)`, bottoming out at 1 component on
low-value melee weapons. Add a floor: **only when the incoming cost list
carries no `ComponentIndustrial`/`ComponentSpacer` entries** (the same
no-components condition as Phase 1's third branch), final count =
`max(byValue, ceil(GetWeaponComplexity(weapon)))` — helper at
`CostRuleHelpers.cs:207`; the function needs the weapon passed in (signature
change; both `ConvertToSpacerWorker` call sites are ours).

The no-components condition keeps ranged weapons out of blast radius: charge
rifles (costList carries ComponentSpacer) and industrial guns (components)
price exactly as today; only recipe-component-less weapons — melee, bows —
gain the floor.

### 3. Move `zeus` to the ultratech tier

Royalty's zeushammer is an ultratech persona weapon; a zeus-flavored trait
belongs with mono/plasma, not the industrial EMP split. Corpus-verified blast
radius: **`UMW_ZeusHeaded` is the only trait in all three corpora carrying a
`zeus` token** (vanilla and AA have none), so this is surgical.

- English XML: remove `zeus` from `UWU_EmpSplit` `labelKeywords`; append to
  `UWU_ChargeUnconditional`. (`shock`/`arc`/`thunder` stay industrial EMP.)
- All 8 language injections: move `zeus` **and its localized equivalents**
  (e.g. Korean `제우스`) between the same two lists. Convention unchanged:
  English prefix verbatim, localized appended, lowercase. Korean note: `펄스`
  must still not appear in either family (Phase 2 handoff trap). The pt-BR
  `carregador` caveat is about `UWU_ChargeCategoryGated`, not this rule — no
  interaction.
- Test pins: `Tests/1.6/TraitRuleCoverageTests.cs:149` (UMW row →
  `UWU_ChargeUnconditional`) and `:213` (`AssertRuleMatched` line). The
  localization spot-check tests don't pin zeus; re-run to confirm.
- Effect: zeus-headed prices as full spacer conversion (with items 1–2:
  3 advanced components on a masterwork steel warhammer, vs ~7 industrial
  components + 45 steel today).

Alternative considered (rejected): a dedicated `UWU_ZeusSplit` rule
keeping the 70% split shape but replacing into spacer components — more
surface (new def, new worker subclass, 8 new translated labels/descriptions)
for a marginally different number; not recommended.

### 4. Translations for the new rule

`UWU_RarityMultiplier` label + description in all 8 languages via the
translate skill. No `labelKeywords` (keywordless rule) — so no keyword
injections, and no HeavyScrap-style hazards. Description must be honest,
vanilla idiom: rarer traits cost proportionally more; negative traits
unaffected. Add the chosen renderings to the pending-native-review paragraphs
(also covers the Phase 2 follow-up already noted in the handoff).

## Reference outcomes (steel stuff unless noted; after items 1–3)

| Case                                           | Today                            | After                         |
| ---------------------------------------------- | -------------------------------- | ----------------------------- |
| Monomolecular, masterwork warhammer            | 2 ComponentSpacer                | 3 ComponentSpacer             |
| Monomolecular, masterwork plasteel longsword   | 5 ComponentSpacer                | 9 ComponentSpacer             |
| Monomolecular, normal knife                    | 1 ComponentSpacer                | 1 ComponentSpacer             |
| Zeus-headed, masterwork warhammer              | 7 ComponentIndustrial + 45 steel | 3 ComponentSpacer             |
| Gold inlay, masterwork warhammer               | 29 gold                          | 57 gold                       |
| Gold inlay, normal knife                       | 3 gold                           | 6 gold                        |
| Envenomed (commonality 1), any weapon          | unchanged                        | unchanged                     |
| Vanilla Ugly / UMW Carbonized (negative)       | unchanged                        | unchanged                     |
| AA Tesla on charge rifle (has ComponentSpacer) | unchanged                        | 2× rarity only (Tesla is 0.5) |

Verify these exactly in `Tests/1.6/TraitCostUtilityTests.cs` extensions.

## Out of scope

Canvas-value pricing for cosmetic traits (scaling ornamental/inlay off the
weapon's market value — deferred owner question); any AA-specific rules
(Phase 3); mod settings for the cap (O1 precedent: hardcoded first).

## Constraints

- Design Constraints 1–4 bind. Rarity is floor-only: never a discount.
- Determinism preserved: commonality is def data; no RNG/clock (V6).
- Comments plain `//`; log prefix `[Unique Weapons Unbound]`.
- Items 1–3 are live repricings for vanilla 0.5-tier ranged traits
  (inlays/launchers 2×) and most of AA (2×) — ratified (owner, 2026-07-30),
  same C3-style acceptance as Phase 1 items 1–5; changelog examples per the
  C3 mitigation.

## Verification

- Extend `Tests/1.6/TraitCostUtilityTests.cs`: clamp boundaries (c=0, c=0.05,
  c=0.5, c=1, c>1), negative-trait exemption, ordering (rarity before
  conversions; blood surcharge and complexity signature counts unscaled;
  HeavyScrap unaffected), spacer floor applies only on component-less costs,
  the reference-outcome table above.
- Update the two zeus pins in `TraitRuleCoverageTests.cs`; the AA drain
  assertion must stay green (no other rule starts firing).
- `check-translations.py` 0/0 across all 8 languages after item 4.
- Full suite via `./Scripts/test-windows.sh`; build + deploy green.

## Ratified decisions (owner, 2026-07-30)

1. **Rarity cap = 2** (conservative option). AA's 0.25 tier prices at 2×
   (e.g. ~14 industrial components for SharpshootersFocus on a masterwork
   assault rifle); revisit `RandomProjectiles`-class underpricing in Phase 3.
2. **Live repricing accepted**, riding update ① so melee prices are right
   from UMW's launch, with changelog before/after examples.
3. **Zeus mechanism: keyword move** (not the dedicated 70%-split rule).

Full-spec owner review completed 2026-07-30; implementation proceeds in a
clean session seeded with this spec.

## Suggested orchestration

Fable orchestrates and reviews every diff; no subagent commits. One Opus
implementer for items 1–2 + tests (C# is small; the test matrix is the bulk),
one for item 3's XML/test pins, translation passes per the Phase 2 pattern
(don't drop below Sonnet for CJK/Korean) for item 4. Sonnet scout first to
re-verify file:line anchors, which may have drifted.
