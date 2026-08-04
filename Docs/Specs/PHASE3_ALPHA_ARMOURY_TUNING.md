# Phase 3 — Alpha Armoury Tuning (XML)

**Status:** ready after Phases 1–2 land. Spec'd 2026-07-29.
**Depends on:** Phase 1 (`AddIngredientsWorker` with fallback/tech-tier/
refundable semantics, `CostFactorWorker`), Phase 2 (blood rule already covers
hemovoric; oversized already split from akimbo).
**Feeds:** Steam update ② — the deliberate AA rebalance, separate changelog.

## Objective

Cost-tune Alpha Armoury's exotic traits (packageId `sarg.alphaarmoury` — note
the spelling; requires Harmony + VEF + Odyssey; 89 traits, all ranged-only).
This is intentionally mod-specific balancing — the sanctioned exception to
Design Constraint 1 (see the research doc's Rejected approaches note). All
ingredient premises were verified 2026-07-29 against the local workshop copy
(research doc V5).

## Required reading (in order)

1. `Docs/Research/MELEE_TRAIT_COST_PIPELINE.md` — V5 (AA grounding + verified
   ingredient defNames), C2 (ingredient decisions), Constraint 4 (price the
   fitting, not the ammo).
2. The TODO block in `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml` — each
   TODO line is the authoritative per-rule premise, already corrected.
3. AA's trait labels (V5 table) — keyword choices below derive from them.

## Work items

One `TraitCostRuleDef` per line below, priorities in the thematic band, all via
Phase 1 workers. Ingredients resolve via `GetNamedSilentFail` (rule inert when
the item's source DLC/mod is absent). "Unrefundable" = the worker adds nothing
on the removal pipeline.

| Rule | Keywords (from AA labels) | Effect | Notes |
| --- | --- | --- | --- |
| hellsphere | `hellsphere` | +1× `SignalChip`, **refundable** | Fallback ingredient: `ComponentSpacer` (for non-Biotech hellsphere traits from other mods). SignalChip MV 1000, Diabolus drop — the boss wields the hellsphere cannon. |
| skip | `skip` | +N× `ComponentSpacer`, unrefundable | AA labels: "self-skip field", "skip field" (hyphen-split covers `skip`). NOT Royalty-gated. A psytrainer-based surcharge was considered and **cut** (owner, 2026-07-29) — do not re-propose. |
| invisibility | `invisibility` | +N× `ComponentSpacer`, unrefundable | Trait is Royalty-gated on AA's side; our rule needs no gate. Same psytrainer cut as skip. |
| adrenal | `adrenal` | +5× `GoJuice`, unrefundable | |
| chainsaw | `chainsaw` | +50× `Chemfuel`, unrefundable | AA label "attached chainsaw". |
| satiating | `satiating` | +5× `MealLavish`, unrefundable | |
| bayonet | `bayonet` | remove components | `ComponentRemovalWorker`, same shape as grip/inlay. AA label "attached bayonet". |
| undersized | `undersized` | 0.65× costs | `CostFactorWorker`. |
| healing/lifesteal | `healing`, `lifesteal` | +10× medicine by weapon tech level, unrefundable | Tech-tiered ingredient (herbal / industrial / ultratech) via the Phase 1 helper. AA labels "healing gun", "lifesteal". Check `healing` for false positives across corpora before shipping; fall back to `lifesteal` + a more specific healing form if needed. |
| firefoam | `firefoam` | +3× `Shell_Firefoam`, unrefundable | Decided 2026-07-29: shells, not the ability's chemfuel reload — Constraint 4. |
| deadlife | `deadlife` | +3× `Shell_Deadlife`, unrefundable | Anomaly item; SilentFail keeps it inert without Anomaly. |

Pick N for skip/invisibility during implementation (suggest 3–5× spacer
components; they are half-day-to-2h-cooldown teleport/stealth abilities —
err expensive per Design Constraint 3).

Also:

- Verify each keyword against the full AA label corpus and UMW's 28 labels for
  collisions (e.g. `skip`/`healing` are ordinary English words — the exact-token
  matcher plus these corpora checks is the guard; the Phase 1 dev-mode dump
  makes this fast).
- Decide `MayRequire` gating on the def nodes only if scouting shows it works
  cleanly on top-level defs in 1.6; SilentFail ingredient resolution already
  gives the required inert-when-absent behavior, so gating is cosmetic.
- Descriptions follow the existing rules' voice (vanilla idiom, honest); add
  translated labels/descriptions for the eight languages, and translated
  keywords per the Phase 2 convention (keep English, append localized).

## Out of scope

Rebalancing AA trait *stats* (not our mod's job); psytrainer-based surcharges
(considered and cut by owner 2026-07-29 — too niche; see the research doc's
Rejected approaches); melee anything (AA is ranged-only, verified V5).

## Verification

- Build + `dotnet test`; unit tests for the additive-worker rules
  can run against synthetic traits with AA-shaped labels (no AA dependency in
  tests — the test project must stay standalone).
- With AA present locally (workshop copy path in V5): dev-mode dump shows each
  of the twelve topics matching exactly its intended rule; the previously
  collision-only matches (tesla, cryo, tox launcher family, etc., listed in V5)
  are unchanged.
- Changelog draft for update ② with before/after cost examples — this update
  deliberately raises many AA trait costs; that's the feature (owner ratified,
  C3), and the changelog is the mitigation.

## Suggested orchestration

Fable orchestrates and reviews every diff; no subagent commits — commits happen
only as the owner directs. Sonnet scouts: refresh the AA label corpus from the
workshop copy (it may have updated since 2026-07-29 — re-verify the twelve
premises, especially reload costs) and test `MayRequire` on top-level defs.
Opus implementer: rule XML + tests in one session; translations can reuse the
Phase 2 per-language pattern (Sonnet acceptable here given the small volume,
except CJK/Korean which should stay Opus or use the repo glossaries strictly).
