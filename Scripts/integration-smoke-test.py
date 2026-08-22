#!/usr/bin/env python3
# Pre-release integration smoke test: boots the real game once with UWU plus
# every mod UWU integrates with, on a pinned minimal list where the baseline
# is a clean log, then classifies every Player.log error/warning by origin
# and fails on anything attributed to UWU or an integration seam. Thin shim
# over the shared engine in l10n/smoke/startup_smoke.py (see its header for
# mechanics and the BetterTradersGuild v1.1.0 CWTL incident this exists to
# catch).
#
# Run this before every release, with the game closed:
#   python3 Scripts/integration-smoke-test.py              # boot + scan
#   python3 Scripts/integration-smoke-test.py --no-launch  # rescan last log
#   python3 Scripts/integration-smoke-test.py --strict     # any error fails

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "smoke"))
import startup_smoke as engine  # noqa: E402

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

engine.PACKAGE_ID = "shunter.uniqueweaponsunbound"

# RATIONALE: base list is this repo's l10n CANONICAL_ACTIVE_MODS (the family
# boots together). VEF and Alpha Armoury are added because UWU's reflection
# integrations (VEFRecipeInheritanceIntegration, VEFWeaponTraitGraphicsIntegration,
# AlphaArmouryIntegration) only activate with them present; VEF loads before
# Alpha Armoury (its dep). UMW's trait-lines extension is read via reflection
# too. Probe last (auto-quit).
engine.SMOKE_ACTIVE_MODS = [
    "brrainz.harmony",
    "ludeon.rimworld",
    "ludeon.rimworld.royalty",
    "ludeon.rimworld.ideology",
    "ludeon.rimworld.biotech",
    "ludeon.rimworld.odyssey",
    "oskarpotocki.vanillafactionsexpanded.core",
    "sarg.alphaarmoury",
    "shunter.uniquemeleeweapons",
    "shunter.uniqueweaponsunbound",
    "shunter.personaweaponsunbound",
    "shunter.l10nprobe",
]

engine.OWN_PATTERNS = ["UniqueWeaponsUnbound", "UWU_"]

# Integration display name -> substrings (the other mod's namespaces and log
# prefixes). An error mentioning any of these gates the test: it means an
# integration seam regressed, even if the exception fires inside their code
# (the BTG/CWTL incident surfaced as an error inside CWTL's own cctor).
engine.INTEGRATION_PATTERNS = {
    "VEF": ["VEF."],
    "AlphaArmoury": ["AlphaArmoury"],
    "UMW": ["UniqueMeleeWeapons", "UMW_"],
    "PWU": ["PersonaWeaponsUnbound", "PWU_"],
}

raise SystemExit(engine.main())
