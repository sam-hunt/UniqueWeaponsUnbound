# Spec: Dedupe texture variants in the customization dialog

Status: implemented 2026-08-24 (display-list dedupe in `Dialog_WeaponCustomization` via
`TextureVariantDeduper`, plus the once-per-def load-order warning)
Origin: UMW Steam Workshop comment, 2026-08-24 (Russian): "all textures have doubles —
in the appearance selection every variant shows up in 2 copies". Triaged in UMW session;
awaiting the player's mod list / Player.log / screenshots.

## Problem

`Dialog_WeaponCustomization`'s texture tab shows every variant N times when the mod that
owns the weapon's textures is loaded N times (classic case: local `Mods/` copy alongside
the Steam Workshop subscription, both enabled).

Mechanism (verified by decompile of RimWorld 1.6 `Assembly-CSharp.dll`):

- `ContentFinder<Texture2D>.GetAllInFolder` iterates `LoadedModManager.RunningModsListForReading`
  — every active mod — and yields matches from each mod's own `ModContentHolder` with **no
  cross-mod dedup**. (Direct `ContentFinder.Get` lookups DO effectively dedupe — first hit
  wins — which is why the weapons themselves render normally and the doubling is visible
  only in variant enumeration.)
- Vanilla `Graphic_Collection.Init` builds `subGraphics[]` from that enumeration (it filters
  `_m` masks, groups by name-prefix-before-underscore, but never dedupes identical names
  across mods). A double-loaded mod therefore yields a doubled `subGraphics[]`.
- Our dialog faithfully mirrors that array: `GetTextureVariantCount()` returns
  `Graphic_Random.SubGraphicsCount` (`Dialog_WeaponCustomization.cs:566`), and each grid
  cell's preview is built from `SubGraphicAtIndex(i).MatSingle.mainTexture`
  (`Dialog_WeaponCustomization.Preview.cs:463`, `BuildVariantPreview`).

## Why fix it here (UWU), not elsewhere

- **Not a Harmony patch on `ContentFinder.GetAllInFolder`:** that method serves every mod's
  folder enumeration for every content type; a global dedupe changes behaviour and adds cost
  far outside our blast radius.
- **Not per-mod graphic subclasses** (e.g. UMW's `Graphic_RandomComplex.Init` override):
  that fixes only that one mod. UWU's picker surfaces the symptom for **any** unique-weapon
  mod a player double-installs (UMW, VE unique ranged, future family mods), and the report
  lands on whichever mod's weapon the player happened to open — usually misattributed.
  Deduping in the dialog covers the whole class once.
- Display-layer only, plain C#, no patching, runs once per dialog open. The underlying
  doubled `subGraphics[]` is left alone — harmless for rendering, since duplicates are
  pixel-identical and vanilla picks one entry per thing.

## Constraint that shapes the design

Variant selection is applied via vanilla `Thing.overrideGraphicIndex`, which indexes the
**full** `subGraphics[]` array (`Graphic_Random.SubGraphicFor` respects it). The initial
selection is snapshotted as
`weapon.overrideGraphicIndex ?? (weapon.thingIDNumber % Mathf.Max(1, textureVariantCount))`
(`Dialog_WeaponCustomization.cs:294-297`). Therefore:

- **Dedupe the display list only. Never change the index domain.** Every visible cell must
  map to a real index into the full array, and `overrideGraphicIndex` must always be written
  as a full-array index.
- The `thingIDNumber % count` fallback must keep using the FULL count, or the computed
  "current" index would disagree with what vanilla actually rendered on the map.

## Design

1. In the constructor (or `EnsureTextureVariantPreviews`), after resolving the
   `Graphic_Random` (unwrap `Graphic_RandomRotated` first, as `GetTextureVariantCount`
   already does), build `List<int> uniqueVariantIndexes`: iterate `0..SubGraphicsCount`,
   key each index by `SubGraphicAtIndex(i).MatSingle.mainTexture.name`, keep the FIRST
   index per key (first-wins matches direct-lookup semantics; duplicates from a double
   load share the filename because both copies are the same file). Guard nulls
   (`MatSingle`/`mainTexture` may be `BaseContent.BadGraphic` fallbacks) — treat a null
   key as always-unique rather than throwing.
2. Grid geometry, the preview array, and the draw loop
   (`Dialog_WeaponCustomization.Texture.cs:34-63`, `Preview.cs:507-513`) iterate
   `uniqueVariantIndexes.Count` cells; cell k renders/holds
   `BuildVariantPreview(topLevel, uniqueVariantIndexes[k], ...)`.
3. Selection: clicking cell k sets `desiredTextureIndex = uniqueVariantIndexes[k]` (a
   full-array index, as today). Highlighting the current selection compares by texture
   name, not raw index, so a weapon whose `thingIDNumber`-derived or previously-saved
   index points at a duplicate copy still highlights the right cell.
4. Diagnostic (the real payoff): when `uniqueVariantIndexes.Count < SubGraphicsCount`,
   `Log.Warning` once per def:
   `[Unique Weapons Unbound] <defName>: N texture variants but only M unique - the mod
   that owns these textures appears to be loaded more than once (local copy + Workshop
   subscription?).`
   This converts silent player confusion into a self-diagnosable log line.

## Non-goals

- Fixing the doubled `subGraphics[]` for map rendering (invisible; duplicates identical).
- Detecting double-installed mods in general (a true double load also duplicates defs and
  errors loudly at startup; that signal already exists).
- Any behaviour change on healthy installs: with no duplicates,
  `uniqueVariantIndexes == [0..count)` and the dialog is bitwise-identical to today.

## Testing

- Healthy install: variant grid unchanged (count, order, previews, selection persistence).
- Repro: copy any unique-weapon mod (UMW is convenient) into local `Mods/` while also
  subscribed; enable both. Before: every cell twice. After: N unique cells, warning logged,
  selecting each cell renders the expected variant on the equipped weapon, and reopening
  the dialog highlights the chosen cell.
- Edge: weapon with a single variant (count 1), non-random graphic (count fallback 1),
  and a def whose graphic resolved to `BadGraphic` — no throw, no dedupe misfire.
