# Hollow Knight 8-Player Co-op v0.11.0

A substantial update from v0.10.0, with saved player profiles, expanded charm support, a redesigned F8 menu and automatic performance presets. Supports **2–8 local players** in one Hollow Knight game.

## New features

- **Named player profiles:** save role, charm preferences, color, Custom Knight skin, extra keyboard bindings and adventure totals; load the same profile in another slot on a later session. Duplicate active profiles are prevented.
- **Adventure statistics:** per-player enemy HP damage, kills, recent assists, healing, healer output, revives, deaths and active play time. PvP scores remain separate.
- **Spells-only duels:** optional spell-only combat with independent SOUL regeneration from 1–99 points per second. Normal movement, dash and healing remain available, and adventure state is restored when the match ends.
- **Automatic performance presets in F8:** Normal for 1–2 players, Balanced for 3–4 and Maximum for 5–8. Manual presets, custom controls and Restore are also available.
- **Native graphics controls:** adjust vanilla particles and blur directly from the mod, plus bloom, decorative detail and reduced damage effects. Temporary presets preserve the user's original graphics preferences for restoration.
- **Redesigned F8 interface:** dark panels, game-style typography, clearer player/profile controls, grouped options, improved sliders and hover help in all ten supported languages.

## Charms, presentation and compatibility

- Expanded support for supported mods' custom charms: additional inventory rows, sparse charm IDs, icons, descriptions, independent equipment and saved loadouts. Provider compatibility remains experimental.
- Independent navigation and removal in each player's equipped-charm card; P1-equipped catalog icons retain their color. Descriptions adapt to available space, prioritize gameplay effects and offer full text on hover.
- Correct native royal-charm appearance and permanent Void Heart equipment after acquisition.
- Restyled extra SOUL reserve bars, smoother shared lights, cooperative acquisition effects and safer healing/background rendering.
- Improved experimental Pale Court handling, including clone initialization, Bloom ownership and Boon spell callbacks. Pale Court is not a required dependency or a guarantee of full compatibility.

## Fixes and optimization

- Reduce repeated player-context, reflection, hierarchy and PlayMaker event-recipient work while preserving event order and player ownership.
- Reduce optional damage particles and audio mixes; clean orphaned jump effects after player/body loss.
- Correct retained recoil/gravity and horizontal drift around shared boss roars.
- Correct guest ownership for Fungal Wastes mushroom bounces, metal recoil, steep slopes and crystal-dash destructibles.
- Shade gates apply collision exemptions to the dashing player individually; party alert/wake sensors and Walker targeting account for living players. Native vertical-conveyor gravity is respected.
- Improve party-death authorization, enemy retargeting, pooled Weaversong cleanup, walking areas and crystal-dash transitions/effects.
- Improve Escape/F8 recovery and distinguish real NPC/shop interactions from inventory and bench control. Keep the closing interaction's owner through native callbacks and improve stale-control recovery.
- Duels can start when players are jumping, falling or sitting, using validated safe positions and restoring their adventure state on exit.

## Installation

Target: **Hollow Knight 1.5.78.11833** with its matching **Modding API**.

Download **HollowKnightLocal8-v0.11.0.zip**. It contains only `HollowKnightLocal8.dll`; all mod-owned art and translations are embedded. Replace the old DLL in the existing mod folder and do not keep multiple versions installed. Existing settings, saves and player identities are retained.

For Lumafly, search **Hollow Knight 8-Player Co-op**. Its catalog update becomes available after the ModLinks maintainers merge the update request.

## Validation and reporting

This release is based on development alpha227. Compilation, controlled regression tests and clean source-package rebuild verification supplement gameplay checks. The latest interaction/control changes have controlled-test coverage; not every native scene, controller combination or third-party mod has been checked in-game. Eight simulated player slots are not an eight-controller performance benchmark, and presets do not guarantee a specific FPS.

Please report problems with the version, player count, scene, other mods, reproduction steps and relevant log. Keep your game save backed up as you would when updating mods.

Source and technical build instructions are included in the repository. No game or Modding API assemblies are redistributed.

(AI-assisted)
