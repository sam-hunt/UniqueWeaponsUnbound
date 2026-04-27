# Changelog

All notable changes to Unique Weapons Unbound will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2026-04-28

### Added

- Minimum weapon quality threshold gate for customization

### Fixed

- Customization now aborts cleanly when placed ingredients are lost mid-job
- Ingredient stacks that fail to reserve mid-job are now skipped instead of stalling
- Unreachable materials are now reported as availability failures
- Customization job failures are now surfaced to the player
- When the weapon is lost simultaneously with another failure, the weapon-loss message takes priority

## [1.0.1] - 2026-04-21

### Added

- Setting to disable trait count limit
- Search bar in customize dialog traits tab
- Settings for recipe base cost and ground menu toggles

### Fixed

- Texture variant grid now scrolls when overflowing the tab area

## [1.0.0] - 2026-04-15

### Added

- Customize unique weapons at workbenches — add, remove, and swap traits
- Three customization entry points: equipped, inventory, and ground items
- Weapon gizmo for ground-item customization
- Asymmetric cost pipeline with tech-level fallback and data-driven trait cost rules
- Negative trait economics and thematic cost rules
- Global cost multiplier setting and configurable refund rate
- Research toggle and settings to bypass workbench tech-level and crafting research requirements
- Mod settings panel with sections and smart controls
- Multi-palette color picker with Ideology DLC support
- Relic Ideology color overlay and stacking tooltips
- Relic weapon name locking in customization dialog
- Support for unique weapons without a base weapon variant
- Full localization support — all UI strings extracted into keyed files

[1.0.2]: https://github.com/sam-hunt/UniqueWeaponsUnbound/releases/tag/v1.0.2
[1.0.1]: https://github.com/sam-hunt/UniqueWeaponsUnbound/releases/tag/v1.0.1
[1.0.0]: https://github.com/sam-hunt/UniqueWeaponsUnbound/releases/tag/v1.0.0
