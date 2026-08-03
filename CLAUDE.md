# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Unique Weapons Unbound** is a RimWorld 1.6 mod that allows players to customize unique weapons. Requires the Harmony mod and the Odyssey DLC.

## Build Commands

```bash
# Build the mod (outputs to 1.6/Assemblies/ AND atomically redeploys to the RimWorld Mods folder)
dotnet build UniqueWeaponsUnbound.sln -c Release

# Build only the main project (also triggers the deploy)
dotnet build Source/1.6/UniqueWeaponsUnbound.csproj

# Override RimWorld install path
RIMWORLD_PATH="/path/to/RimWorld" dotnet build UniqueWeaponsUnbound.sln -c Release
# Or: dotnet build -p:RimWorldPath="/path/to/RimWorld"
```

The build system auto-detects the RimWorld installation path on Windows/Linux/Mac (including WSL targeting a Windows install). For CI builds without RimWorld installed, it falls back to the `Krafs.Rimworld.Ref` NuGet package. For local development and api inspection (monodis, ilspycmd etc), the local installation should be preferred as the source of truth.

### Deployment

Every local build auto-deploys into the RimWorld `Mods/` folder (when a local install is detected) — no separate clean/copy step. The `StageMod` target in `Source/1.6/UniqueWeaponsUnbound.csproj` is the **single source of truth** for what ships: it wipes the target dir and recopies a whitelist of runtime file types, so deleted/renamed files never linger. To change what ships, edit its `_ModFiles` ItemGroup. CI (`.github/workflows/release.yml`) and the local Stop hook both reuse this target, so the release zip can't drift from the local deploy.

A gitignored Stop hook (`.claude/hooks/sync-mod.sh`) rebuilds + redeploys after any turn that touched mod source/content.

**WSL Setup:** Requires `RIMWORLD_PATH` env var in `~/.bashrc` pointing to the Windows RimWorld install (e.g., `/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld`).

**Releases:** Push a tag matching `v*.*.*` to trigger the release workflow (`.github/workflows/release.yml`).

### Tests

xUnit suite under `Tests/1.6/` (a separate project, never shipped). Run with `./Scripts/test-windows.sh` — WSL can't host the net472 runner, so it shells out to the Windows `dotnet` CLI. CI builds but doesn't run it.

## Architecture

### Entry Point

`Source/1.6/Core/ModInitializer.cs` - Static constructor with `[StaticConstructorOnStartup]` auto-patches via Harmony attribute discovery. Harmony ID: `shunter.uniqueweaponsunbound`.

### Key Patterns

**Namespace Convention:** Use `*Patches` suffix for patch namespaces to avoid RimWorld type conflicts.

**Comments:** Plain `//` comments only — never XML doc comments (`///` with `<summary>` etc.). No tooling consumes the doc XML in this project, so the tag scaffolding is noise.

**Serialized Fields:** Use camelCase for fields serialized via `Scribe_Values.Look` to match save file XML element names (per .editorconfig). PascalCase for all other public members.

**Trait-effect-lines contract (`Utilities/TraitEffectLinesIntegration.cs`):** our trait tooltip's
"Effects" block is built from vanilla `WeaponTraitDef` fields, and for a **melee** trait nearly all of
them are inert — `damageDefOverride`, `extraDamages` and `equippedStatOffsets` are read only by the
projectile and bladelink paths, so such a trait would list a market value and nothing else. A
publisher mod (Unique Melee Weapons) supplies the missing lines by attaching a `DefModExtension`
whose **simple type name** is `TraitEffectLinesExtension`, exposing a public `List<string> lines`
of unstyled, already-localized text. We duck-type it, so neither assembly references the other.
**The type name and field name are the contract** — changing what we match on silently empties those
tooltips. Resolved once at startup via `VerifyReflection()` (called from `ModInitializer` beside the
other reflection checks) so a shape mismatch is reported during load, leaving draw time a plain
dictionary lookup. Covered by `TraitEffectLinesIntegrationTests`, which guards our reader only; a
rename on the *publisher's* side is invisible to it.

A publisher can also make an "inert" field live via its own patch, in which case both sides would
describe it: Unique Melee Weapons routes `equippedStatOffsets` to the wielder, so `BuildTraitTooltip`
prints that list **only when the publisher produced no lines** for the trait, letting the publisher's
own wielder-marked line stand alone. `statOffsets`/`statFactors` need no such guard, since vanilla
already displays those and publishers leave them to it.

**Trait stat rows render unfinalized.** Every `StatModifier` on a trait is a raw, pre-curve delta, so
print it with `stat.Worker.ValueToString(value, finalized: false, sense)` — what vanilla
`CompUniqueWeapon` does. `StatDef.ValueToString` defaults to `finalized: true` and applies
`toStringStyle`, which renders `MeleeHitChance`'s raw `-1` as `-100%` instead of `-1.0`.

**Logging:** Prefix mod-specific logs with the mod name — `Log.Message("[Unique Weapons Unbound] ...")`.

**Settings Triple Invariant:** see `Source/1.6/Core/CLAUDE.md`.

## Localization

English (Keyed files + def fields) is the source of truth; other languages derive from it via the
`/translate` skill (`.claude/skills/translate/SKILL.md` — grounding rules, glossary, workflows, and
the `labelKeywords` convention) and are validated deterministically by
`python3 Scripts/check-translations.py` (also a CI release gate). The DefInjected expected set is
the checked-in sidecar `Scripts/expected-injections.json`: a dump of every injection point the
*live* game sees for this mod — including any vanilla-inherited field or C#-default comp string
that never appears in this repo's XML — produced by
`Scripts/refresh-translation-expectations.py` driving the `../L10nProbe` dev mod (never shipped)
through the game's own walker. The checker refuses to run against stale expectations (any defName
in `Defs/` the sidecar has never seen, or label/description text that drifted), so new content
forces a regen and the regen sees everything the game sees; the release skill regenerates every
release, which also covers vanilla updates changing inherited text under unchanged defNames. Def
type folders are the loader-accepted names, so this mod's own def class appears as the
namespace-qualified `UniqueWeaponsUnbound.TraitCostRuleDef` in both the sidecar and
`DefInjected/`. The public language roster lives in CONTRIBUTING.md and must move in the same
commit as any language change.

For reading `Player.log` or disassembling the RimWorld API, use the `rimworld-logs` skill.

