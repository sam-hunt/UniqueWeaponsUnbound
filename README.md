# Unique Weapons Unbound

> A RimWorld mod for customizing unique weapons

[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-blue.svg)](https://rimworldgame.com/)
[![Odyssey DLC](https://img.shields.io/badge/DLC-Odyssey%20Required-orange.svg)](https://store.steampowered.com/app/2380740/RimWorld__Odyssey/)
[![Version](https://img.shields.io/badge/Version-0.1.0-brightgreen.svg)](https://github.com/sam-hunt/UniqueWeaponsUnbound/releases)
[![Development Status](https://img.shields.io/badge/Status-In%20Development-yellow.svg)](https://github.com/sam-hunt/UniqueWeaponsUnbound/releases)

![Preview](About/Preview.png)

## About

RimWorld's Odyssey DLC introduced unique weapons — special variants of existing weapons with 1-3 weapon traits that alter properties like accuracy, weight, name, color, or grant abilities. But once you find one, you're stuck with whatever traits it rolled.

This mod lets you take control:

- **Add traits** to weapons at crafting workbenches, converting normal weapons into unique variants
- **Remove traits** from existing unique weapons, reverting them if all traits are removed
- **Full trait validation** respecting vanilla rules — max count, category exclusions, sole-trait restrictions
- **Research-gated** progression from Neolithic through Spacer/Ultratech tiers
- **Resource costs** derived from weapon crafting recipes, with trait-specific overrides for inlays

## Features

### Workbench Customization

- **Tiered workbenches**: Smithy (Neolithic/Medieval), Machining Table (up to Industrial), Fabrication Bench (all tiers)
- **Styling Station-inspired dialog**: Live weapon preview, available traits list, cost summary
- **Job-based system**: Each trait change is a separate job — interrupt safely without losing resources

### Research Progression

- **Basic Weapon Customization** — Neolithic & Medieval weapons (requires Smithing)
- **Standard Weapon Customization** — Industrial weapons (requires Machining)
- **Advanced Weapon Customization** — Spacer & Ultratech weapons (requires Advanced Fabrication)

### Mod Compatibility

Designed for automatic compatibility with mods that add new unique weapons, weapon traits, or crafting recipes. No hard-coded def references — all detection is dynamic.

## Requirements

- **RimWorld 1.6** or later
- **Odyssey DLC** (required - depends on Odyssey's unique weapon system)
- **Harmony** (auto-download from Steam Workshop if you don't have it)

## Installation

### Steam Workshop (Recommended)

Subscribe on the Steam Workshop and it will auto-download.

### Manual Installation

1. Download the latest release from the [Releases](https://github.com/sam-hunt/UniqueWeaponsUnbound/releases) page
2. Extract the `UniqueWeaponsUnbound` folder to your RimWorld `Mods` directory:
   - **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
   - **Mac**: `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
   - **Linux**: `~/.steam/steam/steamapps/common/RimWorld/Mods/`
3. Enable the mod in RimWorld's mod menu
4. Restart RimWorld

## Compatibility

- **Safe to add** to existing saves.
- **Safe to remove** from saves (no persistent game state modifications).

## Contributing

Bug reports and feature requests welcome on [GitHub Issues](https://github.com/sam-hunt/UniqueWeaponsUnbound/issues).
Please attach any relevant logs/stack traces/mod lists etc.

For development setup, see [CLAUDE.md](CLAUDE.md).

## Credits

**Author**: Sam Hunt ([@sam-hunt](https://github.com/sam-hunt))

**Built With**:

- [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike - Runtime patching library
- RimWorld modding API, community examples

**Special Thanks**:

- [Ludeon Studios](https://ludeon.com) for RimWorld and modding API
- [The RimWorld modding community](https://steamcommunity.com/app/294100/workshop/) for inspiration and working examples
- [Claude Code](https://claude.com/claude-code) for wading through `monodis` output and breathing C#
