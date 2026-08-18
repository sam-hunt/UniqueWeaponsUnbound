---
name: translate
description: Generate, update, or audit mod localization (Keyed + DefInjected) for a target language, grounded in vanilla RimWorld terminology. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Unique Weapons Unbound. English is
the source of truth; every other language derives from it.

**The family-wide process lives in the `l10n/` submodule — load these first,
and only these** (progressive disclosure; if `l10n/` is empty, run
`git submodule update --init`):

- `l10n/process.md` — non-negotiables, file/format conventions, terminology
  grounding method, and the generation / update / audit workflows. This is
  the workflow authority; follow it step by step.
- `l10n/languages/<Language>.md` — the target language's engine mechanics,
  style rules, and vanilla-grounded common vocabulary. Read ONLY the target
  language's file.
- `glossary/<Language>.md` (beside this file) — this mod's own coined-term
  table and worked examples for the target language. Read it in the same
  pass.
- `l10n/lessons.md` — cross-language lessons; read when generating a new
  language, skim otherwise.
- `l10n/workshop.md` — Steam Workshop description/title conventions;
  `.steamworkshop/README.md` names this mod's anchor term and title-coupling
  key (`UWU_SettingsCategory`).

**Where learnings land:** mod-independent findings (engine mechanics, a
language's grammar rule, corpus style facts) go in the `l10n/` submodule —
edit the canonical checkout at `~/dev/rimworld-l10n`, commit there, then bump
the pin here. Mod-specific findings (coined terms, phrasing decisions,
`RulePackDef`/`labelKeywords` worked examples) go in `glossary/<Language>.md`.

The glossaries are shared knowledge across the weapon-mod family (here,
`../UniqueMeleeWeapons`, `../PersonaWeaponsUnbound`): when a row or lesson is
added or corrected in one repo's glossary, mirror it into the siblings,
adjusting domain-specific rows. Add rows whenever a native review lands
corrections.

## This mod's translation surface

- English Keyed source: `1.6/Languages/English/Keyed/UWU_UI.xml`.
- DefInjected def-type folders: `JobDef`, `ResearchProjectDef`, and
  `UniqueWeaponsUnbound.TraitCostRuleDef` (namespace-qualified because the
  def class is this mod's own — a bare `TraitCostRuleDef` folder silently
  drops every entry in it). Enumerate the exact expected key set from
  `Scripts/expected-injections.json`, not from `1.6/Defs/` or the English
  DefInjected tree — either would miss inherited/vanilla-parent or
  C#-default fields the sidecar alone sees.
- No gated compat load root exists today (`UWU_Blood` is deliberately
  ungated instead — see `TraitCostRules.xml`), but the checker and StageMod
  both already support one if the Alpha Armoury rules or a Biotech-gated def
  ever need it.

## This mod's grounding domain

Domain DLC: **Odyssey** (plus Core; Ideology for relic/ideoligion strings).
Terms that MUST be grounded before use: weapon trait, unique weapon, relic,
ideoligion, charge weapons, quality tiers, workbench names (smithy, machining
table, fabrication bench), research project names, tech levels — the
vanilla-grounded answers live in `l10n/languages/<Language>.md`; this mod's
coined terms and domain-specific vocabulary (weapon traits, ammo types,
workbench/research naming, `labelKeywords`) live in `glossary/<Language>.md`.

**The weapon-trait word is per-DLC, not per-language — check the right
stat.** Weapon **trait** and pawn-personality **trait** are different words
in many official localizations, and the weapon word itself can differ
*between DLCs*. For THIS mod the authority is Odyssey's
`Stat_ThingUniqueWeaponTrait_Label` / `WeaponTraits` /
`StatsReport_WeaponTraits` (unique weapons), NOT Royalty's
`Stat_Thing_PersonaWeaponTrait_Label` (persona weapons — PersonaWeaponsUnbound's
domain) and NOT Core's pawn `<Traits>`. Always look the two weapon stats up
separately by defName; never resolve "trait" once per language. Record the
result in the target language's glossary either way — see
`l10n/lessons.md` for why this check cannot be skipped even between sibling
languages.

## `labelKeywords` (TraitCostRuleDef) — keep English, append localized

`TraitCostRuleDef.labelKeywords` is not display text: the cost pipeline
matches these tokens against weapon-trait labels (and defName tokens) to
pick thematic cost rules. It is `[TranslationCanChangeCount]` (hence
`PARITY_EXEMPT_FIELDS = {"labelKeywords"}` in the checker shim), so a
language may replace the whole list with a different entry count.
Convention, mandatory for every language pass:

- **Keep every English keyword, append localized ones.** English entries
  keep matching defName tokens — the language-invariant backbone — and
  localized entries catch localized trait labels. Never drop an English
  entry.
- Matching is exact-token: lowercase, split on spaces and hyphens only, no
  stemming. Carry the exact inflected surface forms that appear in
  trait-label position (gender/number variants where the language declines
  them). A label with no spaces or hyphens (typical zh/ja) is a single token
  that only a whole-label keyword can match — most zh/ja matching rides on
  defName tokens instead, so only add zh/ja keywords that can genuinely
  match (Latin tokens vanilla space-delimits, or short canonical whole-label
  forms). Korean labels split on spaces, so Korean word tokens work
  normally.
- Ground localized keywords in official vanilla terminology (the tars), same
  as any other term; UMW's shipped trait-label translations are a useful
  secondary corpus. Flag ungroundable inventions for native review.
- Inject as whole-list replacements
  (`<UWU_ToxSwap.labelKeywords><li>…</li>…</UWU_ToxSwap.labelKeywords>`) in
  `DefInjected/UniqueWeaponsUnbound.TraitCostRuleDef/`, with the usual
  `<!-- EN: … -->` comment carrying the English list.
- **Never inject `labelKeywords` for a `requireAllKeywords` rule**
  (`UWU_HeavyScrap`): `TraitCostRuleWorker.Matches` requires *every* entry in
  the list, so a whole-list replacement that appends localized tokens
  becomes unsatisfiable — no label carries the English words *and* their
  translations at once — and silently kills the English/defName match too.
  Label and description entries for such rules are unaffected and fine.
- Watch cross-language homographs both ways: a *new English* keyword can be
  a common word in your language (fr `arc` = bow) and a localized keyword
  can collide with unrelated English/defName tokens. Flag risky tokens
  rather than silently shipping them.

## Workflows

Follow `l10n/process.md`'s Initial generation / Update pass / Audit-only
workflows verbatim. This mod's specifics on top:

- The checker: `python3 Scripts/check-translations.py` (`--strict` for new
  languages). Sidecar regen: `python3
  Scripts/refresh-translation-expectations.py` (game must be closed; drives
  the deployed L10nProbe).
- No compat-root routing today (see the surface section above).
- The public roster (and credits) is CONTRIBUTING.md's localization table —
  update it in the same commit as any language addition or native review.
- Community translations are owned by their contributors: Russian is
  community-maintained (PR #6) — update stale/missing keys when asked, but
  do not rewrite a contributor's phrasing wholesale without the user's
  explicit direction.
