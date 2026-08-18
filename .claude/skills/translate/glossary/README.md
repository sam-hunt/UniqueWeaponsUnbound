# Glossary — UWU-specific terminology

These per-language files (`Russian.md`, `Japanese.md`, `ChineseSimplified.md`,
`Korean.md`, `German.md`, `Spanish.md`, `French.md`, `PortugueseBrazilian.md`)
hold everything about a language's translation that is specific to Unique
Weapons Unbound: the weapon-trait-word-per-DLC lookup result, mod-coined
terms (haul planner modes, net refund/cost, texture tab, the localized
Workshop title, and the like), UWU's def-to-vanilla-template reuse notes,
`traitAdjectives`/namer-grammar decisions, and worked phrasing for UWU's own
`DefInjected` entries (e.g. the `RulePackDef` research `generalRules`
case/gender-paradigm reshaping forced by German/Spanish/French for UWU's
three research defs).

Family-shared, mod-independent findings — `LanguageWorker` mechanics, style
and corpus rules, vanilla-grounded common vocabulary (trader, settlement,
goodwill, quality tiers, and so on), and the *general* `RulePackDef`/
`traitAdjectives` grammar techniques (which part of speech a namer field
needs, how German/Spanish/French each solve name-grammar gender) — live
upstream in the `l10n/` submodule at `l10n/languages/<Language>.md` and
`l10n/lessons.md` (canonical checkout: `~/dev/rimworld-l10n`), since they
apply to any mod in the family that ships `RulePackDef`s or generates names,
not just this one.

**Shared with the weapon-mod siblings, but not upstreamed:** the weapon
domain vocabulary here (charge/beam weapons, tox/incendiary/EMP ammo,
inlay/grip/ornamental/lightweight/cumbersome/ugly traits, workbench/research
naming) recurs across `../UniqueMeleeWeapons` and `../PersonaWeaponsUnbound`
because they share the same Odyssey weapon-trait domain — but it stays in
each repo's own glossary rather than `l10n/` because it is specific to
*this family's* defs, not general RimWorld terminology. When a row or lesson
is added or corrected in one repo's glossary, mirror it into the siblings,
adjusting domain-specific rows (e.g. persona-weapon vs unique-weapon
vocabulary).

When a future translation pass coins a new UWU-specific term, record it
here. If a pass instead surfaces a correction to shared mechanics or
vocabulary, send that fix upstream to the l10n repo rather than duplicating
it here.

## Cross-language lesson specific to this mod's domain

**Building names, research names and skill names diverge — and sometimes
collide — inside one term family; never derive one member from another.**
es splits the smithy building (forja) from the Smithing research (herrería)
and again from the Smithing WorkTypeDef (Forja); fr is worse — the smithy is
*établi de forgeron*, the Smithing research *forge*, the machining table
*établi d'assemblage*, the Machining research *usinage*, the fabrication
bench *atelier de fabrication*, and the Fabrication research *assemblage de
composant*, so the words cross over between tiers. pt-BR shows the opposite
failure mode — **collision** rather than divergence: the Crafting skill,
the `Fabrication` research and `FabricationBench` are all *fabricação* /
*bancada de fabricação*. Look up every def individually by defName and
type, and check for collisions as well as splits, in both directions, for
every new language. Each language's own instance of this lesson (with the
concrete vocabulary) is recorded in that language's glossary file above.

`UWU_SettingsCategory` is each language's localized Workshop title and must
stay in sync with the title line (line 1) of
`.steamworkshop/Description/<Language>.txt` (see the CLAUDE.md localization
note). Until the initial Workshop translation pass runs (see TODOs.md) it
still holds the English brand for every language below, so the in-game
report lists it under "Keyed translations matching English (maybe ok)" —
expected for now, not a gap.
