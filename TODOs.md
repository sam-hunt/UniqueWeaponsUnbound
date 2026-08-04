# TODOs

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

## Cleanup

- scan for and split out any oversize files if appropriate seams exist

- Run the `roslynator` CLI bulk fix for the RCS1146 (conditional access) warnings
  surfaced by the newly added analyzers; register the sweep commit in
  `.git-blame-ignore-revs`.
- Evaluate whether `Scripts/test-windows.sh` is still necessary or the suite can
  run natively with `dotnet test Tests/1.6/UniqueWeaponsUnbound.Tests.csproj` — the idiomatic
  pattern BetterTradersGuild uses (its CLAUDE.md warns the Windows-interop script
  corrupts shared `obj/` incremental state; ArchotechAndroidHardware verified
  native runs work and dropped the script, AAH 9bc240f). `DeployToModFolder` is
  already Release-gated here, so Debug `dotnet test` builds won't redeploy.
- Decide whether to standardize `generate_release_notes` in release.yml across
  the family (UMW uses false + manual changelog paste; UWU/PWU use true).
