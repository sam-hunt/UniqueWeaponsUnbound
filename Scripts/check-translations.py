#!/usr/bin/env python3
# UniqueWeaponsUnbound's config shim over the shared translation checker
# (l10n/checker/check_translations.py — the rimworld-l10n submodule). The
# engine holds all logic; this file holds only this repo's config and the
# rationale behind it. Usage is unchanged:
#   python3 Scripts/check-translations.py [--strict] [--root PATH]
# If l10n/ is empty, run: git submodule update --init

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "checker"))
import check_translations as engine  # noqa: E402  (import after sys.path edit)

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

# labelKeywords ([TranslationCanChangeCount]) are matching tokens whose count
# and presence legitimately differ per language — see the translate skill's
# labelKeywords section (requireAllKeywords rules must NOT be injected; only
# keywords that can genuinely match should be added). The sidecar's
# fullListAllowed flag could eventually subsume it.
engine.PARITY_EXEMPT_FIELDS = {"labelKeywords"}

# RATIONALE: Odyssey is a hard dependency (About/About.xml's
# modDependencies — without it the mod does not load at all) plus
# MayRequire= usage in Defs/, of which this repo has none: UWU_Blood is
# deliberately ungated so its DefInjected entries always resolve (see
# 1.6/Defs/TraitCostRuleDefs/TraitCostRules.xml), and the Alpha Armoury rules
# are still TODOs. So Odyssey alone.
engine.REQUIRED_DLCS = {"Odyssey"}

# Empty here today; this repo's own UniqueWeaponsUnbound.TraitCostRuleDef
# needs NO alias — def types a mod itself defines dump under their
# namespace-qualified name, matching the XML tag.
engine.DEF_TYPE_ALIASES = {}

# This mod ships a real Keyed surface, so a missing Languages/ tree is a hard
# config error, not a legal state.
engine.ALLOW_NO_KEYED_SURFACE = False

# The localized Steam Workshop title lives in this Keyed key (the
# settings-window header); the checker enforces the title-coupling rule
# against each .steamworkshop/Description/<Language>.txt title line.
engine.WORKSHOP_TITLE_KEY = "UWU_SettingsCategory"

raise SystemExit(engine.main())
