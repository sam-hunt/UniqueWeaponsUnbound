# Phase 2 — Generic Vocabulary & Localization (XML + translations)

**Status:** ready after Phase 1 lands. Spec'd 2026-07-29.
**Depends on:** Phase 1 (matcher union, tech-tier helper, `AddIngredientsWorker`,
`CostFactorWorker`, fittings-capable swap, `[TranslationCanChangeCount]`).
**Feeds:** ships in Steam update ① together with Phase 1, timed with Unique
Melee Weapons' release. Phase 3 follows separately.

## Objective

Teach the pipeline the melee dialect and make keyword matching work in every
shipped language — all in XML and translation files, using Phase 1 capabilities.
Everything here must stay **generic** (Design Constraint 1): keywords anchor on
vanilla language, never on a specific mod's identity.

## Required reading (in order)

1. `Docs/Research/MELEE_TRAIT_COST_PIPELINE.md` — especially R4 (with the
   exact-surface-form caveat), C1 (localization strategy), Constraint 4, and
   the UMW coverage cross-check.
2. `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml` — current rules; the blood
   rule TODO; the akimbo/oversized TODO.
3. `1.6/Languages/*/DefInjected/UniqueWeaponsUnbound.TraitCostRuleDef/` —
   existing translated labels/descriptions (8 languages; none touch
   `labelKeywords` yet).

## Work items

### 1. R4 keyword extensions (existing rules)

Add to `labelKeywords` (matching is exact-token — carry exact inflected surface
forms, no stemming):

| Rule | Add |
| --- | --- |
| `UWU_EmpSplit` | `zeus`, `shock`, `arc`, `thunder` |
| `UWU_ChargeUnconditional` | `mono`, `monomolecular`, `plasma` |
| `UWU_ToxSwap` | `venom`, `envenomed`, `poisoned`, `opiated`, `sedative` |
| `UWU_IncendiarySwap` | `flaming`, `searing`, `burning`, `ember` |
| `UWU_Ornamental` | `engraved`, `filigree`, `gilded`, `enameled`, `etched` |

Sanity-check each addition against the trait-label corpora (UMW's 28 labels are
listed in the research doc's inventory; Alpha Armoury's 89 were surveyed in V5)
for unintended collisions before finalizing.

### 2. New rule: metal fittings

Keywords: `serrated`, `razored`, `honed`, `keen`, `barbed`, `studded`, `spike`,
`spiked`, `flanged`, `quilloned`, `toothed`, `jagged`, `weighted`,
`counterweight`, `counterweighted`, `needle`. Swaps a fraction of the weapon's
stuff for a tech-selected fitting material (O4, decided: weapon tech level
≤ Industrial → steel, ≥ Spacer → plasteel) via the Phase 1 fittings-capable
worker. Priority in the thematic band (1000–2100).

### 3. New rule: blood (Biotech-gated)

Keywords `blood`, `hemovoric` (covers UMW's `blood-stained` via hyphen-split,
AA's `hemovoric`, and any mod's blood trait). `AddIngredientsWorker`: 10×
`HemogenPack`, **unrefundable**. Inert without Biotech via SilentFail
resolution; decide whether to also `MayRequire`-gate the def itself (scout the
mechanism; SilentFail alone is sufficient behavior-wise).

### 4. Split `oversized` out of `UWU_Akimbo`

Remove the `oversized` keyword from `UWU_Akimbo` (which stays 2.0× for
`akimbo`); add a `UWU_Oversized` rule on `oversized` using `CostFactorWorker`
at **1.5×** (default; flag to owner if a different factor reads better —
Alpha Armoury's Oversized is Mass ×2 / melee damage ×1.5). Fixes the live
mispricing on AA's `AArmoury_Oversized`.

### 5. `UWU_Lightweight` category gate

Currently `weaponCategories`-gated to `Bow`, so UMW's `lightweight` misses (the
gate matches the *trait's* category, and UWU cannot reference `UMW_*` defs
generically). Recommendation: drop the gate — the worker swaps wood→birdskin,
which no-ops on non-wood weapons anyway. Flag in the session summary if a
different resolution is chosen.

### 6. Rule label/description hygiene

Any rule whose behavior changed in Phase 1 (EMP/tox/incendiary/flare now
complexity-based on component-less weapons; material override by value;
inlay/grip unchanged) gets its `description` re-checked against actual
behavior. Player-facing language: vanilla idiom, direct and honest, never
overclaim.

### 7. Keyword translations for the eight shipped languages

Using the `translate` skill, per language (ChineseSimplified, French, German,
Japanese, Korean, PortugueseBrazilian, Russian, Spanish):

- Add whole-list `labelKeywords` injections under
  `DefInjected/UniqueWeaponsUnbound.TraitCostRuleDef/` following the
  convention: **keep every English keyword, append localized ones**. English
  entries keep matching defName tokens; localized entries match localized
  labels.
- Ground localized keywords in **official vanilla terminology**: scouts extract
  how Ludeon's language packs translate the anchor words (charge, EMP, toxic,
  incendiary, zeushammer, monosword, plasmasword, serrated…) from the
  `Data/<DLC>/Languages/<lang>.tar` DefInjected/WeaponTraitDef files, rather
  than inventing translations. Lowercase; remember matching splits on spaces
  and hyphens only (CJK labels rely primarily on the defName backbone — for
  CJK, still add localized keywords where they can match, e.g. when Ludeon's
  label contains a Latin token or the language uses spaces).
- Update the translated label/description files for rules added/changed in
  items 1–5, and re-check translation freshness for existing entries.
- Document the keep-English-append-localized convention in the translate
  skill's guidance so future language passes follow it.

## Out of scope

Alpha Armoury-specific rules (Phase 3) — except the blood rule, which is
generic and lives here. Any C# (should all exist from Phase 1; if a gap is
found, stop and report rather than hacking around it).

## Verification

- Build + `./Scripts/test-windows.sh`. Add/extend a coverage test or use the
  Phase 1 dev-mode dump: **≥19 of UMW's 28 traits** must match at least one
  thematic rule (expected still-plain: `bell-cast`, `dead-blow`, `piledriver`,
  `carbonized`, `storied` — and `blood-stained` without Biotech; negatives
  `cumbersome`/`ugly` correctly take only the downgrade path).
- Collision regression: AA's 89 labels against the new keyword set — no new
  false positives beyond the intended matches (V5 lists the currently-correct
  ones); `AArmoury_Oversized` now hits `UWU_Oversized` (1.5×), not akimbo.
- Spot-check two languages in-game (or via unit test with injected labels):
  a localized trait label matches through translated keywords, and through
  defName tokens alone when keywords are left untranslated.

## Suggested orchestration

Fable orchestrates and reviews every diff; no subagent commits — commits happen
only as the owner directs. Sonnet scouts: extract vanilla terminology from the
language tars (one scout per 2–3 languages) and re-verify the UMW/AA label
corpora. Opus implementers: one for rule XML (items 1–6), one per 2–3 languages
for translations (item 7) — translation quality matters; don't drop below
Sonnet for CJK/Korean, and follow the existing glossaries in the repo.
