# Melee Trait Cost Pipeline — Findings and Recommendations

**Status:** verified, strategy set, formalized into implementation specs —
`Docs/Specs/PHASE1_PIPELINE_CAPABILITIES.md` (C# capabilities),
`PHASE2_VOCABULARY_LOCALIZATION.md` (generic XML + translations, ships with
Phase 1 in update ①), `PHASE3_ALPHA_ARMOURY_TUNING.md` (update ②). Nothing
implemented yet. _Post-implementation addendum (2026-07-30): Phases 1–2 are
committed; owner review found top-tier traits underpriced on low-complexity
melee weapons — `PHASE2_1_TRAIT_POWER_PRICING.md` specs a commonality-based
rarity multiplier (with the corpus survey grounding it), a spacer-conversion
complexity floor, and the zeus→ultratech keyword move. It also records two
newly rejected signals: trait MV (re-affirmed empirically) and
`abilityProps`-based surcharges. Phase 2.1 is now implemented and committed
(same day): the priority chain gains `UWU_RarityMultiplier` at 250, and
`ApplyConvertAllToSpacer` takes the weapon for its complexity floor._ Explored 2026-07-29 (Opus); findings re-verified first-hand and
extended 2026-07-29 (Fable) — see the **Verification addendum** below, which
supersedes the draft where they conflict.

**Trigger:** the companion mod **Unique Melee Weapons** (`../UniqueMeleeWeapons`,
packageId `shunter.uniquemeleeweapons`) is approaching release. Melee weapons are
single-stuff (no `costList`), so UWU's cost pipeline bills the same undifferentiated
pile of the weapon's own stuff for nearly every trait — a masterwork steel warhammer
costs 150 steel per trait, a plasteel one 150 plasteel, whatever the trait does.

The goal is to make trait costs more interesting and thematically appropriate
**without** compromising the pipeline's design intent (see Design Constraints below).

---

## Design Constraints (owner-stated, non-negotiable)

These came out of the session and bound any solution. A clean session should treat
them as given rather than re-deriving or re-litigating them.

1. **The pipeline must stay rule-based and generic.** It exists to adapt dynamically
   to mods neither of us has seen. A modded EMP weapon from any author should get
   component replacement automatically, as long as it uses idiomatic vanilla
   language. Per-mod or per-trait authored manifests defeat the point.

2. **Trait `MarketValue` is not a pricing signal.** UMW's MV ladder (+20 carbonized …
   +120 monomolecular) is tuned for _standalone_ play — keeping pawn+gear value
   aligned with vanilla's intent, because threat points and other mechanics are tuned
   around it. UWU already discards trait MV for vanilla/Odyssey weapons. Do not
   couple cost to it.

3. **Overpricing customization relative to the MV a trait adds is a feature.** UWU's
   appeal is end-game optimization that is _diminishing-returns by design_, given how
   much power traits lend for their relatively low MV increase. Cost/benefit ratios
   that look "regressive" are intentional.

4. **Trait costs price the physical fitting, not the ammo.** (Stated 2026-07-29
   while deciding the firefoam rule.) The fiction is that adding a trait installs
   the launcher/attachment/mechanism on the weapon; mirroring an ability's
   reload/consumption costs is a non-goal. Prefer the thematically legible
   ingredient (firefoam shells for a firefoam launcher) over the mechanically
   mirrored one (the chemfuel its ability actually reloads with).

---

## How the pipeline works today

Entry: `TraitCostUtility.RunPipeline` (`Source/1.6/Utilities/TraitCostUtility.cs:204`).
Rules are `TraitCostRuleDef` instances from `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`,
sorted by `priority`, each delegating to a `TraitCostRuleWorker`.

Matching is keyword-based: `TraitCostRuleWorker.Matches`
(`Source/1.6/TraitCostRules/TraitCostRuleWorker.cs:19`) tests a rule's
`labelKeywords` against a word set built from the trait label, optionally gated by
`weaponCategories`.

Effective cost for a stuffed weapon with no `costList`:

```
cost = costStuffCount × 0.5 × qualityMult      [QualityMultiplierWorker.cs:12, CostFraction]
```

`qualityMult`: Awful 0.7, Poor 0.85, Normal 1.0, Good 1.25, Excellent 1.5,
Masterwork 2.0, Legendary 2.5.

### Grounding numbers

Vanilla `costStuffCount` for the seven weapons UMW builds on:

| Weapon               | stuff | WorkToMake          | Normal cost | Masterwork cost |
| -------------------- | ----- | ------------------- | ----------- | --------------- |
| Knife                | 30    | 1800                | 15          | 30              |
| Mace / Gladius / Axe | 50    | 6000 / 12000 / 7000 | 25          | 50              |
| Spear                | 75    | 12000               | 38          | 75              |
| Longsword            | 100   | 18000               | 50          | 100             |
| Warhammer            | 150   | 18000               | 75          | 150             |

Material market values (verified against the local install):
Wood 1.2, Silver 1, Steel 1.9, Jade 5, Uranium 6, Neutroamine 6, Plasteel 9,
Gold 10, HerbalMedicine 10, Thrumbofur 14, Chemfuel 2.3, Bioferrite 0.75,
ComponentIndustrial 32, ComponentSpacer 200.

Melee accepts Metallic + Woody + Stony stuff, so the same masterwork warhammer trait
ranges **180 silver (wood) → 285 (steel) → 750 (jade) → 900 (uranium) → 1350
(plasteel) → 1500 (gold)**.

### Current UMW coverage

Of UMW's 28 traits, **24 match no rule at all** and produce the plain stuff pile.
The four that match:

- `gold inlay` → `UWU_MaterialOverride` → **150 gold** on a warhammer (see F4)
- `jade inlay` → `UWU_MaterialOverride` → 150 jade
- `ornamental` → `UWU_Ornamental` → half to silver by count
- `lightweight` → `UWU_Lightweight` **misses**, it is `weaponCategories`-gated to `Bow`

---

## Findings

The diagnosis that survived scrutiny: **UWU speaks the ranged dialect fluently and
the melee dialect not at all.** Three defects, all generic — they affect any mod's
melee weapons, not just UMW's.

### F1 — Keyword matching runs on the localized label (bug, live today)

`RunPipeline` builds its word set from `trait.label`
(`Source/1.6/Utilities/TraitCostUtility.cs:208`), and `WeaponTraitDef.label` is
`[MustTranslate]`. UMW ships `DefInjected/WeaponTraitDef/` for eight languages (all
but English) and Ludeon translates the Odyssey and Royalty traits in every official
language pack (verified inside the French/German tars), so **on any non-English
install every keyword rule stops matching** and all traits fall back to plain recipe
cost — for vanilla weapons too, not just UMW's.

There is already an in-repo precedent for the fix:
`CostRuleHelpers.GetMaterialOverride` (`Source/1.6/Utilities/CostRuleHelpers.cs:293`)
falls back to `TryMatchWords(SplitPascalCase(trait.defName))` at line 302. The
matcher never got the same treatment.

This is live in the shipped mod, independent of UMW.

### F2 — Every interesting transform pivots on something melee recipes lack

`CostRuleHelpers.ApplyComponentSwapOrSplit` (`:125`) looks for `ComponentIndustrial`
at `:131`. Melee weapons have no `costList`, so it always takes the fall-through
branch — `SplitBaseMaterials` (`:224`), which is hardcoded to `WoodLog`, `Steel`,
`Plasteel` at `:230`.

Consequence: on a **jade, gold, uranium, silver or stony** weapon, the split
recognises nothing, the converted count is zero, and the rule **silently no-ops**.
Affected rules: `UWU_ToxSwap`, `UWU_IncendiarySwap`, `UWU_EmpSplit`,
`UWU_Flarestriker`.

Related gap: the component lookup does not check `ComponentSpacer`, so charge
weapons (`Gun_ChargeRifle`, `Gun_ChargeLance`, `Gun_BeamRepeater`) also fall through
to the split branch.

### F3 — The keyword vocabulary is ranged-only

Current lists: `tox/toxic/paralytic/acid-injector`, `emp/flux/voltaic/sonic`,
`incendiary/blast/blasts/pitch-soaked/detonation/chemburster`,
`charge/charger/frequency/tesla`, `crypto/cryo/capacitor/ultracoils/thump/rail`,
`grip`, `inlay`, `comfort`, `ornamental`, `akimbo/oversized`, `heavy`+`scrap`,
`flare/flarestriker`, `lightweight/whisper`.

Nothing in the melee idiom matches — and that idiom is **vanilla language**. Royalty
ships **zeushammer**, **monosword**, **plasmasword**. A modded shock-mace naming
itself "zeus-headed" is using the game's own word and getting nothing.

Also note Odyssey defines **no melee `WeaponCategoryDef`s** (all 15 are ranged:
Ranged, BulletFiring, Bow, Rifle, Sighted, Scoped, Shotgun, BurstFire,
LowStoppingPower, Gun, PelletFiring, PulseCharge, Pistol, BeamWeapon, Attachable).
UMW defines its own six (`UMW_Melee`, `UMW_Bladed`, `UMW_Blunt`, `UMW_Pointed`,
`UMW_Guarded`, `UMW_Heavy`), which UWU cannot reference generically.

### F4 — `ApplyMaterialOverride` swaps 1:1 by count

`CostRuleHelpers.ApplyMaterialOverride` (`:307`) sums all raw-resource counts and
re-emits the total in the override material (`:321`). So `gold inlay` on a masterwork
warhammer bills **150 gold (1500 silver)** where steel would bill 285. Generic — hits
any mod's `<material> inlay/plated/trimmed` trait.

---

## Rejected approaches

Recorded so a clean session does not re-propose them.

- **Trait-MV-weighted costing** (scale cost by the trait's MarketValue offset).
  Rejected under Constraint 2 — the MV ladder serves threat-point alignment in
  standalone play, and Constraint 3 makes the MV/cost gap intentional.

- **Per-trait authored ingredient "kits"** (a `AddIngredientsWorker` plus ~28 UMW
  rule defs assigning each trait a signature material). Rejected under Constraint 1:
  it would work for UMW and do nothing for unseen mods. _Note:_ the underlying
  `AddIngredientsWorker` idea may still have standalone merit for the twelve
  `<!-- TODO -->` Alpha Armoury rules in `TraitCostRules.xml` (hemogen pack,
  psytrainer, go-juice, firefoam shell…), which are inherently mod-specific. That is
  a separate question from melee costing.

- **Shipping UWU rule defs from UMW** via
  `<li IfModActive="shunter.uniqueweaponsunbound">1.6-UWU</li>`. Mechanically
  verified to work (`Verse.LoadFolder.ShouldLoad`; attributes `IfModActive`,
  `IfModActiveAll`, `IfModNotActive` parsed in `ModContentPack`), but moot once
  per-trait authoring is off the table. Keep the note — the mechanism is useful if
  UMW ever needs to ship UWU-conditional content.

- **Psytrainer-based surcharges for the skip/invisibility rules** (1×
  `Psytrainer_Skip`/`Psytrainer_Invisibility`, the implied defs verified in V5).
  Cut by owner 2026-07-29: too niche for current risk appetite. Those rules use
  advanced components; the V5 mechanism facts stay recorded for reference only.

---

## Recommendations

Priority order. R1–R3 are defect fixes worth landing regardless of UMW's release.

### R1 — Union defName tokens into the match set

Fixes F1. In `RunPipeline`, build `labelWords` from **both** `trait.label` and
`SplitPascalCase(trait.defName)`. Everything below is English-only without it. Also
_improves_ generic mod coverage in English, since modders' defNames are
conventionally English even when labels are flavourful
(`VWE_ChargeRifle` → `charge`, `rifle`).

### R2 — Make `SplitBaseMaterials` stuff-agnostic

Fixes F2's second half. Split whatever raw stuff is present using the existing
`CostRuleHelpers.IsRawResource` (`:330`) rather than the hardcoded trio at `:230`.

**Hazard:** value-preserving conversion across a large tier gap yields nonsense — a
gold longsword with a tox trait would bill ~70 herbal medicine. R2 needs R3 to be
safe.

### R3 — Derive a stuff-independent complexity from `WorkToMake`

The headline. Not an invented constant — reverse-engineered from Ludeon's own
pricing. `WorkToMake / 6000` against actual vanilla component counts:

| Weapon             | cplx | comps |     | Weapon       | cplx | comps |
| ------------------ | ---- | ----- | --- | ------------ | ---- | ----- |
| Revolver           | 0.7  | 2     |     | ChainShotgun | 5.2  | 5     |
| Autopistol         | 0.8  | 2     |     | LMG          | 5.7  | 5     |
| MachinePistol      | 1.8  | 3     |     | AssaultRifle | 6.7  | 7     |
| BoltActionRifle    | 2.0  | 3     |     | HellcatRifle | 6.7  | 7     |
| PumpShotgun        | 2.0  | 3     |     | SniperRifle  | 7.5  | 8     |
| IncendiaryLauncher | 3.3  | 4     |     | Incinerator  | 8.0  | 6     |
| HeavySMG           | 4.0  | 4     |     | Minigun      | 10.0 | 20    |

Within ±1 for ten of eleven industrial guns (Minigun is the outlier — see O2).

Melee then reads: club 0.2, knife 0.3, ikwa / breach axe 0.8, mace 1.0, axe 1.2,
gladius / spear 2.0, longsword / warhammer 3.0 — a longsword is a bolt-action
rifle's worth of complexity.

**Change:** give `ApplyComponentSwapOrSplit` a third branch — no components present →
use complexity in place of component count, times the multiplier already carried by
the rule def (3× herbal medicine, 10× chemfuel). A tox trait then bills **9 herbal
medicine on any warhammer, wooden or gold**; 1 on a knife. Because complexity is
stuff-independent, the signature count stops tracking stuff value, which is what
makes R2 safe.

Also extend the component lookup to `ComponentSpacer` (F2's related gap).

Bonus: complexity is defined for uncraftable weapons like `Gun_BeamGraser`
(work 47000, no `costList`) where the recipe path yields nothing.

Resolve `WorkToMake` from the same def `BaseCostFromRecipeWorker` resolves — base
variant via `WeaponRegistry.GetBaseVariant`, falling back to `weapon.def`
(`Source/1.6/TraitCostRules/BaseCostFromRecipeWorker.cs:17-23`) — so the two stay
consistent.

### R4 — Extend the keyword vocabulary to the melee dialect

Fixes F3. Pure XML in `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`, anchored on
vanilla words so it generalizes:

| Add                                                     | To rule                   | Vanilla anchor          |
| ------------------------------------------------------- | ------------------------- | ----------------------- |
| `zeus`, `shock`, `arc`, `thunder`                       | `UWU_EmpSplit`            | zeushammer (Royalty)    |
| `mono`, `monomolecular`                                 | `UWU_ChargeUnconditional` | monosword (Royalty)     |
| `plasma`                                                | spacer conversion         | plasmasword (Royalty)   |
| `venom`, `envenomed`, `poisoned`, `opiated`, `sedative` | `UWU_ToxSwap`             | already has `paralytic` |
| `flaming`, `searing`, `burning`, `ember`                | `UWU_IncendiarySwap`      | —                       |
| `engraved`, `filigree`, `gilded`, `enameled`, `etched`  | `UWU_Ornamental`          | —                       |

Plus **one new generic rule — metal fittings → steel**, on keywords `serrated`,
`razored`, `honed`, `keen`, `barbed`, `studded`, `spiked`, `flanged`, `quilloned`,
`toothed`, `jagged`, `weighted`, `counterweight`. Swaps a fraction of the weapon's
stuff for plain steel, because a steel-studded wooden club is the normal object.
Highest-value single addition for melee texture; fires for anyone's serrated dagger.
Fitting material follows the weapon's tech level per O4: ≤ Industrial → steel,
≥ Spacer → plasteel.

Plus **a generic blood rule** (owner, 2026-07-29): repurpose the planned hemovoric
rule — gate it on **Biotech only**, not Alpha Armoury — adding 10× `HemogenPack`
(unrefundable) on keywords `blood`, `hemovoric`. `blood-stained` hyphen-splits to
`blood`, so it prices UMW's `blood-stained`, AA's `hemovoric`, and any mod's
blood-flavored trait with one rule. Without Biotech those traits stay plain.

**Exact-surface-form caveat (verified):** matching is exact token membership — no
stemming. `counterweighted` matches neither `weighted` nor `counterweight`; UMW's
actual labels are `counterweighted` and `head-weighted` (the latter hyphen-splits so
`weighted` catches it). Keyword lists must carry the exact inflected forms
(`counterweighted`, `spike` for "armor spike", etc.).

Expected effect on UMW (cross-checked against its actual 28 labels): ~19–21 traits
differentiate, with UWU never learning UMW exists. Still plain afterwards:
`bell-cast`, `dead-blow`, `piledriver`, `carbonized`, `storied`, `needle point`,
`armor spike` (and `blood-stained` when Biotech is inactive) — plus
`cumbersome`/`ugly`, which are negative and correctly take only the downgrade path.

### R5 — Convert `ApplyMaterialOverride` off 1:1-by-count

Fixes F4. _(Decided, owner 2026-07-29: convert by market value.)_ Re-emit the
raw-resource total as the override material at equal market value (ceil, min 1);
gold inlay on a masterwork steel warhammer bills ~29 gold instead of 150.

---

## Open considerations

Resolutions recorded 2026-07-29 from owner review; items without a resolution
marker remain genuinely open.

- **O1 — Where the complexity divisor lives.** _(resolved, owner 2026-07-29)_
  Hardcoded constant for now — it leaves the mod-setting option open for later.
- **O2 — Complexity underprices very high-component weapons.** _(resolved, owner)_
  Linear is a close-enough heuristic to start with; Minigun stays the accepted
  outlier.
- **O3 — Complexity is tier-blind.** _(ratified, owner)_ A complexity-derived bill
  selects its material by the weapon's tech level rather than always the rule's
  default.
- **O4 — Should the R4 fittings rule always yield steel?** _(decided, owner)_
  Simple tech-level boundary: weapon tech level ≤ Industrial → steel; ≥ Spacer →
  plasteel.
- **O5 — Ordering and redundancy between R2 and R3.** _(stays open, owner)_ Once
  complexity exists, is stuff-agnostic splitting still needed, or does it become
  dead weight? Revisit during implementation.
- **O6 — Refund symmetry.** _(resolved, owner)_ No special handling to start with:
  complexity-derived ingredients flow through `GetRemovalCost`/`GetTotalRefund` at
  `RefundRate` like any other cost line.
- **O7 — `NegativeDowngradeWorker` interaction.** _(expected resolved by O3,
  owner)_ Once the bill material tracks weapon tech level, the existing downgrade
  map (spacer→industrial, plasteel→steel, steel→wood) already applies to whatever
  material was selected. Verify at implementation.
- **O8 — `CostPruneWorker`'s 3-material cap** (`priority 9900`). _(owner: probably
  fine regardless)_ Stuff + 1–2 signature lines fits; confirm incidentally in
  tests.
- **O9 — Does R1 widen matching unsafely?** _(resolved — prefix-strip adopted,
  owner 2026-07-29)_ Owner's concern: defs aren't namespaced, so mods prefix
  defNames with identifiable acronyms (`VWE_`, `AArmoury_`, `UMW_`…), and an
  acronym token can collide with a keyword — more plausible now that R4 adds short
  words like `arc` and `mono` that read as mod acronyms. **Proposal:** when the
  defName contains an underscore, strip the first underscore-delimited segment
  (the prefix) before tokenizing; PascalCase-split the remainder, also splitting
  on non-letters (`SplitPascalCase` today splits neither underscores nor digits,
  `Source/1.6/Utilities/CostRuleHelpers.cs:358`). Vanilla trait defNames have no
  underscores and are untouched; meaningful tails (`VWE_ChargeRifle` → `charge`,
  `rifle`) survive. Residual risk is a thematically wrong cost, never a crash,
  bounded by explicit keyword lists. Optional debuggability: a dev-mode dump of
  each trait → matched rules, so keyword regressions are auditable at a glance.
- **O10 — Is `defNameKeywords` worth a separate def field?** _(resolved — see
  addendum, C1)_ No: keep the single `labelKeywords` list, match it against the
  union of label tokens and defName tokens, and let translations append localized
  keywords to the same list.
- **O11 — Multiplayer determinism.** _(resolved — see addendum, V6)_ Pipeline is
  RNG/clock-free, but three inputs are client-local ModSettings — the actual desync
  vector if MP support proceeds.

### Owner's additional considerations

Raised by the owner 2026-07-29; analysis grounded in the Verification addendum.

**C1 — Localization support beyond R1.** Three-layer strategy, cheapest first:

1. **R1 defName tokens are the language-invariant backbone.** They also cover CJK
   installs, where space-tokenizing a localized label can never match English
   keywords at all.
2. **Make `labelKeywords` translator-extensible.** DefInjected can already inject
   the field per-index with no code changes (V4); the one worthwhile code change is
   decorating it `[TranslationCanChangeCount]` so whole-list replacement may change
   the entry count. Convention for translators (document in the translate skill):
   _keep the English words, append your language's_. English entries keep matching
   defName tokens; localized entries match localized labels. No translator has
   touched these fields yet in any shipped language, so there is no compatibility
   burden.
3. **Ship our own keyword translations** for the eight non-English languages UWU
   already ships, generated via the translate skill against each language's official
   vanilla terminology (e.g. Korean 독성 for tox-family labels).

**C2 — Alpha Armoury tuning.** Needs the pipeline's first _additive_ worker
(`AddIngredientsWorker`): adds def-specified ingredient lines on top of the computed
cost. Resolve ingredient defNames via `GetNamedSilentFail` so rules are inert when
the source mod/DLC is absent (no hard dependency needed). "Unrefundable" maps onto
the existing `isRemoval` flag: the worker simply doesn't add its surcharge when
computing the removal/refund pipeline. This does not conflict with the rejected
per-trait-kit approach — AA tuning is inherently mod-specific balancing, which is
exactly where authored rules are appropriate (already noted under Rejected
approaches). Corrected premises for the twelve TODO rules live in V5 and in the
updated TODO comments in `TraitCostRules.xml`.

**Ingredient decisions (owner, 2026-07-29):** hellsphere → 1× `SignalChip`
(SilentFail-resolved, advanced-components fallback for non-Biotech hellsphere
traits from other mods); skip/invisibility → advanced components — the
psytrainer-based alternative was considered and **cut** (owner: too niche for
current risk appetite; see Rejected approaches); firefoam → 3× `Shell_Firefoam`
per Constraint 4 (price the fitting, not the ammo).

**C3 — Stability and rollout for ~50k subscribers.** The two verified facts that
decide this: melee is empty content in shipped UWU (V1), and no purchase ledger
exists (V2).

- The melee rework (R2+R3+R4) is not a repricing of anything players currently
  experience — it is launch content for UMW. Gating it protects an audience that
  does not exist yet.
- Changes existing players _will_ see: the F1 fix (non-English installs gain the
  thematic costs English players already had — a bug fix, not a rebalance), the
  ComponentSpacer lookup (charge-weapon traits reprice), R1 in English (wider
  matching on modded traits), R5 (inlay-style costs drop — downward, the pleasant
  direction), and AA rules (deliberate rebalance — the requested feature).
- "New games only" gating is not actually implementable for costs: nothing about
  cost is save state (V2), so any gate would be a global setting, not a per-save
  one.
- Refund asymmetry on update is unavoidable but bounded: refunds recompute from the
  new pipeline, and `RefundRate` 0.5 halves any discrepancy. A price-paid ledger
  (Scribe per-trait costs at purchase) would fix future repricings but adds save
  state and MP sync surface — deliberately deferred (ratified by owner
  2026-07-29); revisit at 1.7 if pipeline churn continues.

**Decision (ratified by owner 2026-07-29): option (1) — mainline in 1.6, no
setting gate, no postponement**, sequenced as two updates: (i) defect fixes + melee enablement + localization
(R1–R5), timed with UMW's release; (ii) AA tuning (`AddIngredientsWorker` + rules).
Work is cut capabilities-first into three specs under `Docs/Specs/`: Phase 1
(C# capabilities) + Phase 2 (generic XML + translations) form update ①; Phase 3
(AA rule XML) is update ②.
Mitigations: Steam changelog with before/after cost examples; the existing
`traitCostMultiplier` setting is the escape hatch for players who dislike new
prices. A default-off setting (option 2) would double the pipeline test surface to
protect nobody, and postponing to 1.7 (option 3) would undercut UMW's launch with
150-plasteel-per-trait costs.

**C4 — What the 1.7 boundary is actually for.** Reserve it for genuinely breaking
changes — a purchase ledger, or re-parameterizing the complexity divisor (O1) if
tuning experience demands it. Note RimWorld's versioned def folders (`1.6/` vs
`1.7/`) mean XML-level pipeline differences can ship per-game-version with zero
runtime gating; the C# can carry dormant capabilities keyed off def fields.

---

## Verification addendum (Fable, 2026-07-29)

F1, F2 and F4 re-verified first-hand in the source; F3 and the grounding tables
verified by independent inspection of UMW, Alpha Armoury and the local install.
The draft's diagnosis stands. Corrections and new load-bearing facts:

### Corrections to the draft

- UMW ships **eight** translated languages, not nine (English has no DefInjected).
- Keyword matching is **exact-token**: `SplitLabelWords` lowercases, splits on
  spaces plus hyphen parts, and membership is `HashSet.Contains`
  (`Source/1.6/Utilities/CostRuleHelpers.cs:100`). No stemming — see the
  exact-surface-form caveat added to R4.
- A rule's `weaponCategories` gate matches the **trait's** `weaponCategory` field,
  not the weapon (`Source/1.6/TraitCostRules/TraitCostRuleWorker.cs:23`). That is
  the precise mechanism of the `UWU_Lightweight` miss: UMW's lightweight trait
  carries a `UMW_*` category, not `Bow`.
- `Gun_ChargeRifle` is WorkToMake 45000 (cplx 7.5 — table value stands), costList
  Plasteel 50 + ComponentSpacer 2. `Gun_BeamGraser` is **Biotech** (mechanoid gun);
  Odyssey bolts a standalone `Make_BeamGraser` RecipeDef onto it, and
  `BaseCostFromRecipeWorker` reads only `costList`/`costStuffCount`
  (`Source/1.6/TraitCostRules/BaseCostFromRecipeWorker.cs:29-41`), so that recipe is
  invisible to the pipeline — separate small fix candidate.
- Vanilla melee weapons _do_ carry WeaponTraitDefs — Royalty's 19 BladeLink persona
  traits — but UWU excludes BladeLink explicitly, so the melee-is-uncovered
  conclusion stands for UWU's purposes.

### New load-bearing facts

- **V1 — Melee is empty content in shipped UWU.**
  `TraitValidationUtility.GetCompatibleTraits` intersects each trait's
  `weaponCategory` with the weapon's `CompProperties_UniqueWeapon` categories;
  vanilla+DLC define only ranged categories (plus BladeLink, excluded), so no melee
  weapon is offered any trait today. The melee rework has ~zero blast radius on
  existing saves.
- **V2 — No purchase ledger exists.** Refunds recompute from the _current_ pipeline
  at removal time (`GetRemovalCost`/`GetTotalRefund` → `RunPipeline`); the only
  Scribe'd cost data is the in-flight `CustomizationOp.cost`/`.refund` snapshot for
  queued jobs (`Source/1.6/Jobs/CustomizationSpec.cs:18`). Every cost change
  retroactively reprices refunds; conversely, per-save gating of costs has no
  mechanism to attach to.
- **V3 — Ludeon translates WeaponTraitDef labels in every official language pack**
  (verified in Odyssey/Royalty French and German tars). F1 is live for all
  non-English players on vanilla content, independent of UMW.
- **V4 — `labelKeywords` is DefInjected-injectable today.** Decompiled
  `Verse.DefInjectionPackage`: `[MustTranslate]` never gates injection (only
  `[NoTranslate]`/`[Unsaved]` block it); per-index injection
  (`RuleDefName.labelKeywords.0`) works now; whole-list replacement with a
  _different count_ requires `[TranslationCanChangeCount]` on the field.
- **V5 — Alpha Armoury grounding** (found in the local workshop cache; packageId
  `sarg.alphaarmoury`; requires Harmony + VEF + **Odyssey**; 1.6-only). 89
  WeaponTraitDefs, **all ranged-only** — its patch adds `AArmoury_*` categories to
  base-game ranged uniques exclusively, so AA needs none of the melee work.
  Corrected TODO premises: the mod is "Alpha Armoury" (not "Armory"); the
  hellsphere launcher's ability reloads on ComponentSpacer ×5; the firefoam
  launcher reloads on **Chemfuel ×15/charge (3 charges)**, not `Shell_Firefoam`;
  the deadlife launcher has 1 charge reloading `Shell_Deadlife` ×1; and AA's skip
  traits aren't Royalty-gated.
  **Owner corrections to the agent's misses (verified 2026-07-29):** `SignalChip`
  **does** exist — a Biotech XML ThingDef (MV 1000) dropped by the player-summoned
  Diabolus, the boss that wields the hellsphere cannon (the agent searched only
  Odyssey and AA). Psytrainers **also** exist, as **implied defs**:
  `ThingDefGenerator_Neurotrainer` generates one `Psytrainer_<AbilityDefName>` per
  psycast at load (`Psytrainer_Skip` MV 700, `Psytrainer_Invisibility` MV 850;
  MV = lerp(100→1000, level/6)) — invisible to def-file searches, resolvable via
  `DefDatabase` at startup. Each grants its _specific_ psycast; randomness is only
  in trader-stock/reward selection.
  Verified good: `HemogenPack`, `GoJuice`, `MealLavish`, `Chemfuel`,
  `Shell_Deadlife`, `MedicineHerbal/Industrial/Ultratech`, `ChunkSlagSteel`.
  **Live collision today:** `AArmoury_Oversized` ("oversized" — Mass ×2, melee
  damage ×1.5, nothing to do with dual-wielding) matches `UWU_Akimbo`'s `oversized`
  keyword and gets cost-doubled; confirms the rename TODO — split `oversized` into
  its own rule with its own multiplier. Also fires correctly already: tox/toxic,
  incendiary, emp/voltaic/flux/sonic, cryo, capacitor, detonation, chemburster,
  flare, and tesla (keyword _and_ `PulseCharge` category).
- **V6 — MP determinism.** The pipeline is RNG- and clock-free, and all caches are
  startup-built and immutable. The actual desync vector is that three inputs are
  client-local ModSettings: `traitCostMultiplier`, `traitRefundRate`,
  `useRecipeBaseCost`. Any MP support must sync or freeze these.

---

## Verification notes

Claims above were checked against the local install at `$RIMWORLD_PATH`
(`/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld`), per CLAUDE.md's
preference for the local install as source of truth:

- `WeaponTraitDef` field list (confirming `marketValueOffset` exists and is separate
  from `statOffsets`, and that both vanilla and UMW declare MarketValue via
  `statOffsets`) — `ilspycmd -t RimWorld.WeaponTraitDef`.
- `Verse.LoadFolder.ShouldLoad` and the `IfModActive` / `IfModActiveAll` /
  `IfModNotActive` attribute names — `ilspycmd` over `Assembly-CSharp.dll`.
- `costStuffCount`, `WorkToMake`, `costList` and market values — parsed from
  `$RIMWORLD_PATH/Data/**/*.xml`.

Tests live at `Tests/1.6/TraitCostUtilityTests.cs`; run via
`./Scripts/test-windows.sh` (WSL cannot host the net472 runner).
