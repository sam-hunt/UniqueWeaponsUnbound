#!/usr/bin/env python3
# UniqueWeaponsUnbound's config shim over the shared sidecar-refresh engine
# (l10n/refresh/refresh_expectations.py — the rimworld-l10n submodule),
# which drives the L10nProbe dev mod (source at l10n/probe/; build/deploy it
# only from the canonical ~/dev/rimworld-l10n checkout). The engine holds all
# logic; this file holds only this repo's config and the rationale behind it.
# Usage is unchanged (game must be closed):
#   python3 Scripts/refresh-translation-expectations.py [--no-launch]
# If l10n/ is empty, run: git submodule update --init

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "refresh"))
import refresh_expectations as engine  # noqa: E402  (import after sys.path edit)

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

engine.PACKAGE_ID = "shunter.uniqueweaponsunbound"

# RATIONALE: Core, plus every DLC any family mod hard-requires or gates
# content behind via MayRequire — a def whose gate is absent never loads, so
# its keys drop out of the sidecar and its already-shipped translations turn
# illegal (UMW's Royalty-gated uniques are the live example). Only THIS
# repo's mod is needed for THIS sidecar's correctness — the family is
# designed independent and does not patch each other's defs — but the
# siblings (UniqueMeleeWeapons, PersonaWeaponsUnbound) ride along so one boot
# refreshes every dump and their refresh scripts can reuse it with
# --no-launch; keeping the three lists identical is that convenience, not a
# correctness rule. See the engine's header for the general membership rule,
# the lowercase-id warning, and the contamination-pinning rationale; order is
# load order, the probe last.
engine.CANONICAL_ACTIVE_MODS = [
    "brrainz.harmony",
    "ludeon.rimworld",
    "ludeon.rimworld.royalty",
    "ludeon.rimworld.ideology",
    "ludeon.rimworld.biotech",
    "ludeon.rimworld.odyssey",
    "shunter.uniquemeleeweapons",
    "shunter.uniqueweaponsunbound",
    "shunter.personaweaponsunbound",
    "shunter.l10nprobe",
]

raise SystemExit(engine.main())
