# Phase 2 overnight session — handoff (2026-07-30)

Owner review notes for the Phase 2 (vocabulary & localization) work done
autonomously overnight. Spec: `Docs/Specs/PHASE2_VOCABULARY_LOCALIZATION.md`.

## What shipped

### English rule XML (items 1–6) — done
`1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`:

- R4 keyword extensions applied exactly as specced (EMP += zeus/shock/arc/thunder,
  charge += mono/monomolecular/plasma, tox += venom/envenomed/poisoned/opiated/sedative,
  incendiary += flaming/searing/burning/ember, ornamental += engraved/filigree/gilded/enameled/etched).
- New `UWU_MetalFittings` (StuffFittingsSwapWorker, priority 2050, default 40%
  fraction, steel/plasteel by tech level per O4). Includes `needle` — see the
  accepted-collision note below.
- New `UWU_Blood` (AddIngredientsWorker, priority 2200, 10× HemogenPack,
  `refundable=false`, keywords blood/hemovoric). **Decision: no `MayRequire`
  gate on the def** — scout verified (ilspycmd, `LoadedModManager.ParseAndProcessXML`)
  that top-level def MayRequire works (vanilla even uses it on a WeaponTraitDef),
  but a gated-out def would leave 8 languages' DefInjected entries dangling,
  which bumps the translation-error count and emits a generic load warning for
  every non-Biotech player on those languages
  (`LoadedLanguage.InjectIntoData_AfterImpliedDefs` → aggregate `Log.Warning`).
  SilentFail resolution alone keeps the rule inert without Biotech with zero
  player-visible noise.
- `oversized` split out of `UWU_Akimbo` into new `UWU_Oversized`
  (CostFactorWorker at the default-recommended **1.5×**, priority 1850). Akimbo
  stays 2.0×. If a different factor reads better, it's one XML line.
- `UWU_Lightweight`: Bow gate **dropped** (spec's recommended resolution). The
  swap only touches wood costs so it self-gates; XML comment records the
  rationale. The harness test that pinned the old gate was updated to pin the
  gate's absence instead.
- Item 6 description hygiene: tox/incendiary descriptions no longer claim the
  stale "converts 70% of base materials by market value" fallback — Phase 1's
  actual behavior is an additive complexity-scaled signature count
  (`ApplyComponentSwapOrSplit` third branch). Incendiary now mentions spacer
  components counting as components (the worker folds them). MaterialOverride
  says "equal market value". EMP/Flarestriker descriptions were checked and are
  still accurate (they use the value-split path, which did not change shape —
  note the Phase 2 spec's parenthetical "EMP/flare now complexity-based" is
  imprecise; the code is the source of truth here).

### Coverage verification
- Deterministic matcher simulation over UMW's real 28 defName|label pairs:
  **21/28 thematic matches** (target ≥19). Still-plain set is exactly the
  expected one: bell-cast, dead-blow, piledriver, carbonized, storied +
  negatives cumbersome/ugly. gold/jade inlay hit UWU_Inlay via defName tokens.
- AA regression (scout re-extracted all 89 traits from the workshop copy):
  `AArmoury_Oversized` → UWU_Oversized only (mispricing fixed); all
  previously-correct families reconfirmed; **one new false positive, accepted
  deliberately**: `AArmoury_NeedleProjectiles` ("needle projectiles", a
  paralytic poison-dart ammo trait) matches UWU_MetalFittings via `needle`.
  Accepted because (a) dropping `needle` would orphan UMW's "needle point"
  (a genuine metal fitting), and (b) the hit is behaviorally inert: the
  fittings worker swaps a fraction of `weapon.Stuff`, and AA traits ride
  non-stuffed vanilla ranged uniques, so it no-ops. Pinned in tests as an
  accepted match. Revisit in Phase 3 if AA tuning wants needle→tox instead.

### Translate skill
`.claude/skills/translate/SKILL.md` gained the required
"`labelKeywords` — keep English, append localized" section (convention,
exact-token semantics, CJK whole-label caveat, homograph warnings).

### Tests
`Tests/1.6/TraitRuleCoverageTests.cs`: both corpora pinned verbatim through the
real matcher (`SplitLabelWords` ∪ `SplitDefNameWords` → `Worker.Matches`), with
exact per-rule match sets and a drain assertion so any *other* rule starting to
fire fails the suite. AA rows carry their real `weaponCategory`, so the
category-gated charge rule is genuinely exercised (`AArmoury_Tesla`).
`TraitRuleLocalizationTests.cs` spot-checks French and Korean shipped keyword
injections (see verification). Environment gotcha discovered: net472 tests must
avoid `Split(char)`-style netstandard-2.1-only overloads — the facade resolves
them at compile time but Mono's copy has no runtime implementation
(`MissingMethodException`); comment in the test file.

### Translations (item 7, 8 languages) — done, checker 0/0
All eight languages got labelKeywords whole-list injections (keep-English-
append-localized), translated labels/descriptions for the three new rules, and
refreshed entries for the four changed descriptions. Russian edits were kept
surgical (community file): only the four stale entries were rewritten. Every
injection was mechanically audited (by me, independent of the agents): English
prefix verbatim and in order, no duplicates, all-lowercase, and no
`UWU_HeavyScrap` injection anywhere.

**Load-bearing discovery (now documented in the translate skill):**
`UWU_HeavyScrap` (`requireAllKeywords`) must NEVER receive a keyword injection
in any language — `Matches` uses `All()` over the whole list, and a whole-list
replacement that appends tokens becomes unsatisfiable, silently killing the
English defName match too. Verified in `TraitCostRuleWorker.cs:28-30`; guarded
by a unit test and an XML comment in each language file.

Per-language decisions worth knowing (details in XML comments in each file):

- **pt-BR `carregador`** (charger/magazine homograph) is safe only because the
  charge rule is category-gated — if the gate is ever dropped, that token must
  go with it (XML comment records this next to the list).
- **Korean 펄스** excluded everywhere: vanilla uses it for both EMP and
  pulse-charge, so it would cross-fire the two rule families; 제우스 anchors
  EmpSplit instead.
- **Japanese Flarestriker got no injection** (フレア too generic); zh 照明弹 and
  ko 조명탄 shipped (specific pyrotechnic-round words).
- **ja `UWU_Inlay` label fixed 象嵌 → 象眼** to match Odyssey's official
  spelling (both are valid words; only one is Ludeon's).
- **RU ships ё and е spellings** of every ё-token (облегчённое/облегченное
  etc.) — Odyssey and UMW disagree on ё usage.
- Two deliberate RU terminology corrections inside already-stale entries:
  химтопливо → химическое топливо, птичью шкуру → кожу птицы (both match
  vanilla labels). They touch the community contributor's word choices —
  revert if you prefer his forms.
- "Metal fittings" has no vanilla anchor in any language; each translator's
  choice is flagged machine-assisted for native review (de Metallbeschläge,
  fr ferrures, es herrajes, zh 金属部件, ja 金具, ko 금속 부속물).
- **Biotech (DLC name) stays Latin in zh/ja/ko** Blood descriptions — vanilla
  ships no ExpansionDef translations there.

Scout findings worth keeping:

- **The defName backbone already covers everything**: every UMW defName is an
  English concept word present in the kept English keyword lists, so all
  intended matches succeed in all 8 languages via defName tokens alone.
  Localized keywords are defense-in-depth for label-only mods.
- **German**: fuses concepts into compounds (Giftmunition, blutbefleckt) — only
  complete compound words work as tokens. `UWU_HeavyScrap`'s
  `requireAllKeywords` (heavy AND scrap as separate tokens) is structurally
  unsatisfiable in idiomatic German; skipped there.
- **zh/ja**: no spaces/hyphens in labels (even `EMP弹`/`EMP弾` are fused), so
  only whole-label tokens can match — ~13/28 UMW traits catchable via label;
  HeavyScrap unmatchable. Korean has spaces, so real word tokens work (15/28,
  and uniquely can satisfy HeavyScrap via 무거운+고철).
- **Korean trap**: 펄스 means both EMP and pulse-charge in vanilla — must not be
  added to either rule family; 제우스 anchors EmpSplit instead.
- **Russian traps**: Odyssey "облегченное" vs UMW "облегчённое" (ё/е) — both
  forms needed; vanilla `OversizedRounds` literally translates as "heavy
  rounds" in RU and pt-BR, so it must NOT ground the oversized concept.
- **pt-BR gap**: "head-weighted" (cabeça pesada) has no safe token — every
  candidate collides with bell-cast/dead-blow/zeus-headed labels. defName
  backbone covers it; accepted.
- **French dormant homograph**: `arc` (English keyword, now shipped) is French
  for "bow". No current vanilla/UMW French trait label contains bare "arc" as a
  token, but third-party French labels could false-positive into the EMP rule.
  Bounded risk (thematically wrong cost, never a crash); noting for awareness.

## Verification status — all green
- [x] English XML loads; harness test updated for the dropped Bow gate
- [x] Corpus regression tests: UMW 28 rows + AA 89 rows, exact per-rule sets
- [x] `check-translations.py`: 0 errors / 0 warnings across all 8 languages
- [x] Localized spot-check tests (French + Korean): localized label matches
      through a translated keyword and provably not through English; a fully
      localized label with an English-concept defName matches via defName
      tokens alone; convention guards (English prefix, no HeavyScrap
      injection, no duplicate tokens) run per parsed language file
- [x] Final suite: **290 passed / 0 failed**; build + auto-deploy green

## Incident during testing (resolved, but eyeball if inclined)
The test subagent, proving its convention guards weren't vacuous, mutated the
*uncommitted* French language file and then reverted with `git checkout --`,
which restored HEAD and wiped the translator's work. It reconstructed the file
by reversing its three known mutations against the build-output copy. I then
verified the reconstruction independently: re-ran the full checker (0/0),
re-ran my own structural audit of all 13 French injections (English prefixes
verbatim, no dupes, lowercase, no HeavyScrap), and read the entire French diff
line by line — content, style and comments are coherent throughout. The file
was committed only after that. Residual risk is essentially the same as for
any machine-assisted file awaiting native review. (A first attempt to verify
against the auto-deployed Mods copy turned out to be circular — the test run
had already redeployed — which is why the verification above is manual.)

## Commits
1. `8e0d401` feat: Teach trait cost keywords the melee dialect — rule XML
   items 1–6, harness fix, corpus regression tests (UMW 28 / AA 89).
2. `b97ef16` feat: Localize trait cost keywords for eight languages — all
   8 language files, translate-skill convention section, localization
   spot-check tests + csproj glob.
3. (this file) docs: session handoff.

## Follow-ups for a clean session (not blocking release)
1. **Translate-skill glossary rows**: the new mod-decided terms above (fittings,
   oversized, hemogen surcharge renderings per language) belong in each
   glossary's "pending native review" paragraph in
   `.claude/skills/translate/SKILL.md`. Translators skipped this to avoid
   concurrent edits to the shared file; the terms are recorded in the language
   files' XML comments and in this handoff.
2. **CONTRIBUTING.md roster**: Russian is credited as Native (PR #6) but now
   carries machine-assisted entries added after that review (three new rules +
   four reworded descriptions). Your call whether the table needs a qualifier;
   nobody touched it.
3. **Phase 3 note**: if AA tuning wants `needle` → tox-family instead of
   fittings, the accepted `AArmoury_NeedleProjectiles` pin in
   `TraitRuleCoverageTests.cs` is the place that will argue back.

## Flagged for owner (decisions I made that you may want to revisit)
1. `UWU_Oversized` at 1.5× (spec default; flag-if-better invitation noted — AA's
   Oversized is Mass ×2 / melee dmg ×1.5, so 1.5× reads right to me).
2. Lightweight gate dropped (spec's recommended option).
3. `needle` kept on MetalFittings despite the inert AA NeedleProjectiles hit.
4. No MayRequire gate on UWU_Blood (translation-noise rationale above).
5. HeavyScrap left keyword-untranslatable in de/zh/ja (structural, documented).
