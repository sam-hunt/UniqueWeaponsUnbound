#!/usr/bin/env python3
# Validates mod translation files against the English source of truth and the
# mod's own Defs. Deterministic companion to the .claude/skills/translate flow:
# anything this script can prove is never re-derived by an agent.
#
# Checks per non-English language:
#   Keyed:       missing/extra keys, argument-placeholder mismatches, stale EN
#                comments (grammar constructs like {PAWN_gender ? a : b : c} are
#                language-specific and deliberately not compared - see below)
#   DefInjected: folder names resolvable as def types, defNames exist, field
#                paths structurally valid against def XML, stale EN comments,
#                uninjected label/description (warning)
#   All files:   well-formed XML, <LanguageData> root, UTF-8 no BOM, LF line
#                endings, no tabs, final newline (hygiene -> warnings)
#
# Staleness relies on the EN-comment convention: every translated entry carries
# the English source directly above it, e.g.
#   <!-- EN: Reset to defaults -->
#   <UMW_ResetToDefaults>...</UMW_ResetToDefaults>
# A missing EN comment is a warning; an EN comment that no longer matches the
# current English text is an error (the translation is stale).
#
# Exit code: 1 if any errors (or, with --strict, any warnings), else 0.

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

PLACEHOLDER_RE = re.compile(r"\{[^{}]*\}")
EN_COMMENT_RE = re.compile(r"^\s*EN:\s?(.*)$", re.DOTALL)

# A {...} span is one of two different things, and only one of them is an interface
# contract with the C# call site.
#
#   argument placeholder   {0}, {1}, {PAWN_labelShort}
#       Supplied by the caller. A language that drops or invents one is broken, so
#       these must match English exactly.
#
#   grammar construct      {PAWN_gender ? é : ée : é(e)}, {0_gender ? un : une : un(e)}
#       Resolved by GrammarResolverSimple from an argument's gender, and inflecting
#       languages need them where uninflected English does not. French Core writes
#       Cut.deathMessage as "{0} a été taillad{PAWN_gender ? é : ée : é(e)} à mort."
#       against an English "{0} has been cut to death." (HealthUtility passes both
#       pawn.LabelShortCap and pawn.Named("PAWN") precisely so this resolves.)
#       Comparing these across languages would forbid correct translations.
#
# So grammar constructs are excluded before comparing. This does not weaken the
# argument check: a construct references its subject as "PAWN_gender", never as a
# bare "{0}", so a translation that dropped a real argument still fails.
GRAMMAR_CONSTRUCT_RE = re.compile(r"\{[^{}]*\?[^{}]*\}")

# Fields whose entries legitimately vary per language (RimWorld's
# [TranslationCanChangeCount]-style matching tokens): exempt from the
# cross-language parity check, keyed on the final path segment.
# UWU: labelKeywords ([TranslationCanChangeCount]) are matching tokens whose
# count and presence legitimately differ per language — see SKILL.md's
# labelKeywords section (requireAllKeywords rules must NOT be injected; only
# keywords that can genuinely match should be added).
PARITY_EXEMPT_FIELDS = {"labelKeywords"}

# ThingDefs carrying this thingSetMakerTag are unique weapons whose tool
# labels and ability-comp strings are externally sourced (see
# EXTERNAL_INJECTIONS below); None disables that guard in repos that ship no
# such weapons. This repo ships none.
UNIQUE_WEAPON_TAG = None

# ---------------------------------------------------------------------------
# Externally-sourced translatable fields.
#
# The game's in-game translation report (Dev Mode > save translation report)
# walks the LIVE DefDatabase with reflection over [MustTranslate], so it can
# see translatable text a def-XML scan structurally cannot: fields inherited
# from VANILLA parents (ParentName targets living in RimWorld's Data/, not
# this repo) and C# field DEFAULTS never written in any XML at all. Such
# injections are enumerated here instead, each mapping the exact DefInjected
# key to its current English text (used for placeholder and EN-comment
# staleness checks, exactly as def XML text is for structural fields).
#
# This repo currently has NO such fields (all defs are the mod's own
# JobDef/ResearchProjectDef/TraitCostRuleDef with no vanilla parents or
# translatable comp defaults), so the manifest is empty. The guards in
# check_manifest_guards() are the tripwire that keeps it honest: content in a
# class known to carry externally-sourced text (a unique weapon by tag, an
# abilityProps trait, an InjuryBase hediff, a FactionDef) fails this script
# until its rows are added here, with the paths taken from a fresh in-game
# report. See UniqueMeleeWeapons' checker for a populated example.
EXTERNAL_INJECTIONS = {}


def check_manifest_guards(defs, report):
    # Force EXTERNAL_INJECTIONS maintenance from the def XML itself: content
    # whose class of def is known to carry externally-sourced translatable
    # text must have manifest rows before the repo passes.
    label = "[Scripts/check-translations.py EXTERNAL_INJECTIONS]"
    ext_thing = EXTERNAL_INJECTIONS.get("ThingDef", {})
    for def_name, elem in sorted(defs.get("ThingDef", {}).items()):
        tags = [li.text for li in elem.findall("thingSetMakerTags/li")]
        if UNIQUE_WEAPON_TAG is None or UNIQUE_WEAPON_TAG not in tags:
            continue
        if not any(k.startswith(f"{def_name}.tools.") for k in ext_thing):
            report.error(label, f"{def_name} is a unique weapon but has no "
                                f"tools.*.label manifest rows (its tools are "
                                f"inherited from the vanilla base def; get "
                                f"the paths from an in-game translation "
                                f"report)")
        for f in ("chargeNoun", "cooldownGerund"):
            key = f"{def_name}.comps.CompEquippableAbilityReloadable.{f}"
            if key not in ext_thing:
                report.error(label, f"{def_name} carries the ability comp "
                                    f"but has no {key} manifest row")
    ext_trait = EXTERNAL_INJECTIONS.get("WeaponTraitDef", {})
    for def_name, elem in sorted(defs.get("WeaponTraitDef", {}).items()):
        if elem.find("abilityProps") is None:
            continue
        for f in ("chargeNoun", "cooldownGerund"):
            key = f"{def_name}.abilityProps.{f}"
            if key not in ext_trait:
                report.error(label, f"{def_name} has abilityProps but no "
                                    f"{key} manifest row")
    ext_hediff = EXTERNAL_INJECTIONS.get("HediffDef", {})
    for def_name, elem in sorted(defs.get("HediffDef", {}).items()):
        if elem.get("ParentName") == "InjuryBase" \
                and f"{def_name}.labelNounPretty" not in ext_hediff:
            report.error(label, f"{def_name} is an injury (InjuryBase) but "
                                f"has no {def_name}.labelNounPretty manifest "
                                f"row")
    ext_faction = EXTERNAL_INJECTIONS.get("FactionDef", {})
    for def_name in sorted(defs.get("FactionDef", {})):
        if f"{def_name}.messageDefendersAttacking" not in ext_faction:
            report.error(label, f"{def_name} has no "
                                f"{def_name}.messageDefendersAttacking "
                                f"manifest row (inherited from FactionBase)")


def norm(text):
    return re.sub(r"\s+", " ", (text or "").strip())


def placeholders(text):
    return set(PLACEHOLDER_RE.findall(GRAMMAR_CONSTRUCT_RE.sub("", text or "")))


def parse_with_comments(path):
    # Returns (root, entries) where entries is [(key, text, en_comment)].
    # The EN comment for an entry is the nearest preceding EN: comment that
    # appears after the previous element (section headers are skipped).
    builder = ET.TreeBuilder(insert_comments=True)
    root = ET.parse(path, parser=ET.XMLParser(target=builder)).getroot()
    entries = []
    pending_en = None
    for node in root:
        if node.tag is ET.Comment:
            m = EN_COMMENT_RE.match(node.text or "")
            if m:
                pending_en = m.group(1)
        else:
            entries.append((node.tag, node, pending_en))
            pending_en = None
    return root, entries


def flatten_entry(elem):
    # A Keyed/DefInjected entry is either a single text value or a list of <li>.
    kids = list(elem)
    if kids:
        return [li.text or "" for li in kids]
    return elem.text or ""


class Report:
    def __init__(self):
        self.errors = []
        self.warnings = []

    def error(self, path, msg):
        self.errors.append(f"{path}: {msg}")

    def warn(self, path, msg):
        self.warnings.append(f"{path}: {msg}")


def check_hygiene(path, report):
    raw = path.read_bytes()
    if raw.startswith(b"\xef\xbb\xbf"):
        report.error(path, "UTF-8 BOM present")
    if b"\r" in raw:
        report.warn(path, "CRLF line endings (repo convention is LF)")
    if b"\t" in raw:
        report.warn(path, "tab indentation (repo convention is 2 spaces)")
    if raw and not raw.endswith(b"\n"):
        report.warn(path, "missing final newline")


def load_language_xml(path, report):
    check_hygiene(path, report)
    try:
        root, entries = parse_with_comments(path)
    except ET.ParseError as e:
        report.error(path, f"XML parse error: {e}")
        return None
    if root.tag != "LanguageData":
        report.error(path, f"root element is <{root.tag}>, expected <LanguageData>")
        return None
    return entries


def collect_keyed(lang_dir, report):
    # key -> (text, en_comment, path)
    keyed = {}
    for path in sorted((lang_dir / "Keyed").glob("**/*.xml")) if (lang_dir / "Keyed").is_dir() else []:
        entries = load_language_xml(path, report)
        for key, elem, en in entries or []:
            if key in keyed:
                report.error(path, f"duplicate key <{key}> (also in {keyed[key][2].name})")
            keyed[key] = (flatten_entry(elem), en, path)
    return keyed


def collect_defs(defs_dirs):
    # tag -> {defName -> element}; abstract parents kept under their Name attr.
    defs = {}
    parents = {}
    for defs_dir in defs_dirs:
        for path in sorted(defs_dir.glob("**/*.xml")):
            try:
                root = ET.parse(path).getroot()
            except ET.ParseError:
                continue
            if root.tag != "Defs":
                continue
            for elem in root:
                if elem.tag is ET.Comment:
                    continue
                name = elem.get("Name")
                if name is not None:
                    parents.setdefault(elem.tag, {})[name] = elem
                def_name = elem.findtext("defName")
                if def_name:
                    defs.setdefault(elem.tag, {})[def_name] = elem
    return defs, parents


def resolve_field(elem, segments, parents):
    # Structurally walk a DefInjected path (field names, li indices) through a
    # def element, following ParentName inheritance. Returns the matched
    # element, or None. A path may legitimately stop at a list field
    # (full-list translation), which is a match on the list element itself.
    if not segments:
        return elem
    head, rest = segments[0], segments[1:]
    if head.isdigit():
        kids = [k for k in elem if k.tag == "li"]
        idx = int(head)
        if idx < len(kids):
            return resolve_field(kids[idx], rest, parents)
        return None
    child = elem.find(head)
    if child is not None:
        return resolve_field(child, rest, parents)
    parent_name = elem.get("ParentName")
    pool = parents.get(elem.tag, {})
    while parent_name and parent_name in pool:
        parent = pool[parent_name]
        child = parent.find(head)
        if child is not None:
            return resolve_field(child, rest, parents)
        parent_name = parent.get("ParentName")
    return None


def expected_injections(defs):
    # The full set of DefInjected keys every language must carry:
    # label/description present in our own def XML, plus the
    # externally-sourced manifest. {def_type: {key: english_text}}
    expected = {t: dict(keys) for t, keys in EXTERNAL_INJECTIONS.items()
                if t in defs}
    for def_type, by_name in defs.items():
        for def_name, elem in by_name.items():
            for field in ("label", "description"):
                node = elem.find(field)
                if node is not None:
                    expected.setdefault(def_type, {})[f"{def_name}.{field}"] \
                        = node.text or ""
    return expected


def check_language(lang_dir, english_keyed, defs, parents, expected, report):
    # Returns {def_type: set(keys)} actually translated in this language,
    # for the cross-language parity check in main().
    lang = lang_dir.name

    # --- Keyed ---
    keyed = collect_keyed(lang_dir, report)
    label = f"[{lang}/Keyed]"
    for key in sorted(set(english_keyed) - set(keyed)):
        report.error(label, f"missing key <{key}>")
    for key, (_, _, path) in sorted(keyed.items()):
        if key not in english_keyed:
            report.error(path, f"unknown key <{key}> (not in English)")
            continue
        text, en, _ = keyed[key]
        en_text = english_keyed[key][0]
        if isinstance(text, str) and isinstance(en_text, str):
            if placeholders(text) != placeholders(en_text):
                report.error(path, f"<{key}> placeholders {sorted(placeholders(text))} "
                                   f"!= English {sorted(placeholders(en_text))}")
        if en is None:
            report.warn(path, f"<{key}> has no EN: comment")
        elif isinstance(en_text, str) and norm(en) != norm(en_text):
            report.error(path, f"<{key}> is STALE: EN comment does not match current "
                               f"English text")

    # --- DefInjected ---
    found = {}
    inj_root = lang_dir / "DefInjected"
    folders = sorted(p for p in inj_root.iterdir() if p.is_dir()) \
        if inj_root.is_dir() else []
    for folder in folders:
        def_type = folder.name
        if def_type not in defs:
            report.error(folder, f"folder does not match any def type in this mod's "
                                 f"Defs (expected one of: {', '.join(sorted(defs))})")
            continue
        external = EXTERNAL_INJECTIONS.get(def_type, {})
        for path in sorted(folder.glob("**/*.xml")):
            entries = load_language_xml(path, report)
            for key, elem, en in entries or []:
                segments = key.split(".")
                def_name = segments[0]
                if def_name not in defs[def_type]:
                    report.error(path, f"<{key}>: no {def_type} named {def_name}")
                    continue
                found.setdefault(def_type, set()).add(key)
                if key in external:
                    # Sourced from vanilla inheritance or C# defaults; the
                    # manifest supplies the English text (see the header of
                    # EXTERNAL_INJECTIONS).
                    en_text = external[key]
                else:
                    target = resolve_field(defs[def_type][def_name], segments[1:], parents)
                    if target is None:
                        report.error(path, f"<{key}>: field path does not exist on the "
                                           f"def (nor in EXTERNAL_INJECTIONS)")
                        continue
                    en_text = target.text if not list(target) else None
                text = flatten_entry(elem)
                if isinstance(text, str) and en_text is not None:
                    if placeholders(text) != placeholders(en_text):
                        report.error(path, f"<{key}> placeholders {sorted(placeholders(text))} "
                                           f"!= English {sorted(placeholders(en_text))}")
                if en is None:
                    report.warn(path, f"<{key}> has no EN: comment")
                elif en_text is not None and norm(en) != norm(en_text):
                    report.error(path, f"<{key}> is STALE: EN comment does not match "
                                       f"current English source")
    # Completeness: every expected key (label/description in our XML plus the
    # external manifest) must be translated. An entire def type with no
    # DefInjected folder lands here too — the old per-folder walk silently
    # skipped def types nobody had started translating (how sibling repo
    # UMW's WeaponCategoryDef labels shipped missing in all its languages).
    for def_type, keys in sorted(expected.items()):
        missing = set(keys) - found.get(def_type, set())
        for key in sorted(missing):
            report.error(f"[{lang}/DefInjected/{def_type}]",
                         f"missing <{key}> (EN: {keys[key]!r})")
    return found


def main():
    ap = argparse.ArgumentParser(description="Validate mod translation files.")
    ap.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent,
                    help="repo root (default: parent of Scripts/)")
    ap.add_argument("--strict", action="store_true", help="treat warnings as errors")
    args = ap.parse_args()

    lang_roots = sorted(args.root.glob("*/Languages")) + \
                 ([args.root / "Languages"] if (args.root / "Languages").is_dir() else [])
    defs_dirs = sorted(args.root.glob("*/Defs")) + \
                ([args.root / "Defs"] if (args.root / "Defs").is_dir() else [])
    if not lang_roots:
        print(f"No Languages/ directory found under {args.root}", file=sys.stderr)
        return 2

    report = Report()
    english_keyed = {}
    languages = []
    for lang_root in lang_roots:
        for lang_dir in sorted(p for p in lang_root.iterdir() if p.is_dir()):
            if lang_dir.name == "English":
                english_keyed.update(collect_keyed(lang_dir, report))
            else:
                languages.append(lang_dir)

    if not english_keyed:
        print("No English Keyed strings found; nothing to check against.", file=sys.stderr)
        return 2

    defs, parents = collect_defs(defs_dirs)
    check_manifest_guards(defs, report)
    expected = expected_injections(defs)
    found_by_lang = {}
    for lang_dir in languages:
        found_by_lang[lang_dir.name] = check_language(
            lang_dir, english_keyed, defs, parents, expected, report)

    # Cross-language parity: any DefInjected key one language translates
    # beyond the expected set (secondary fields, list translations, grammar
    # rules) must exist in every language — a key added in one pass and
    # forgotten in the others is exactly the drift this catches.
    union = {}
    for found in found_by_lang.values():
        for def_type, keys in found.items():
            union.setdefault(def_type, set()).update(keys)
    for lang, found in sorted(found_by_lang.items()):
        for def_type, keys in sorted(union.items()):
            extra = keys - found.get(def_type, set()) \
                - set(expected.get(def_type, {}))
            extra = {k for k in extra
                     if k.split(".")[-1] not in PARITY_EXEMPT_FIELDS}
            for key in sorted(extra):
                report.error(f"[{lang}/DefInjected/{def_type}]",
                             f"missing <{key}> (translated in other languages)")

    for line in report.errors:
        print(f"ERROR   {line}")
    for line in report.warnings:
        print(f"WARNING {line}")
    checked = ", ".join(sorted({l.name for l in languages})) or "none"
    print(f"\n{len(english_keyed)} English keys; languages checked: {checked}")
    print(f"{len(report.errors)} error(s), {len(report.warnings)} warning(s)")
    return 1 if report.errors or (args.strict and report.warnings) else 0


if __name__ == "__main__":
    sys.exit(main())
