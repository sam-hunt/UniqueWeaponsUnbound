# TODOs

## Pre-release checks

- Verify the 231f55a reload fix in-game: switch language mid-session, then confirm customization still finds unique variants + workbenches, cost rows show the new language's materials and are payable, and bench-requirement messages use new-language labels (then switch back).

## Features

- Mod setting: quality cost influence slider (0–200% scale on the quality
  multiplier's effect on trait costs; deferred 2026-08-02 while the rarity cap
  and complexity floor sliders bed in)
- Add Alpha Armoury rules support (Phase 3 — workers landed, rules are XML-only)
- Mod option for gating customization on colony crafting skill recipe +2 or 12
- Mod setting for chance of upgrading enemy spawn weapons to unique
  - increasing chance of biocoding at higher tech/quality?
- Free Customize unique weapon relics on form/reform ideology
- Free Customize unique weapon dev mode gizmo
- Multiplayer support?
- Explore dynamically preserving arbitrary mod-added weapon properties across a
  base<->unique def conversion (today WeaponDefConversion hand-copies a fixed
  set: stuff, quality, hp%, texture, biocoding, art, relic status — anything
  else a mod attaches is dropped)
- Check Alpha Armoury and VWE for trait effects that could be patched to add tooltip effect strings

## Localization

- `UWU_Blood` could move to a `1.6/Mods/Biotech` compat load root now that the
  checker/StageMod support gated roots (2026-08-18 port from BTG); its
  DefInjected entries in all 8 languages would move with it. Currently
  deliberately ungated instead (see the comment in
  `1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml`).
- The Alpha Armoury TODO rules at the bottom of `TraitCostRules.xml` become
  translatable via a `1.6/Mods/AlphaArmoury`-style compat root once
  implemented (several are also `MayRequire Biotech`/`Royalty`/`Anomaly` on
  top of Alpha Armoury, which would need their own nested gating story).
- Scanned the repo for other commented-out/excluded DefInjected entries or
  skill notes mentioning gating exclusions (2026-08-18): found only the two
  items above (`UWU_Blood`'s ungating comment and the Alpha Armoury TODOs) —
  no other gating exclusions exist today.
- Dedupe glossary/Japanese.md's UMW melee-term block against UniqueMeleeWeapons' own glossary/Japanese.md (both hold a copy since the 2026-08-18 l10n consolidation)
