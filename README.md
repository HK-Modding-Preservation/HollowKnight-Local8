# Hollow Knight 8-Player Co-op

**Public version: v0.11.0 · DLL version: 0.11.0 · ModLinks version: 0.11.0.0**

Local cooperative multiplayer for **2–8 players** inside one Hollow Knight game. Players share the world, enemies, bosses and story progression while keeping their own controls, health, SOUL, charms, optional roles, profiles and supported Custom Knight skins.

Target: **Hollow Knight 1.5.78.11833** with its matching **Hollow Knight Modding API**.

## What is new in v0.11.0

- Named player profiles and per-player adventure statistics, portable between slots and sessions.
- Spells-only PvP with configurable SOUL regeneration and safer duel entry from jumps, falls and benches.
- Redesigned F8 menus, translated hover help and expanded native/custom charm navigation, icons and descriptions.
- Automatic performance presets: **1–2 Normal, 3–4 Balanced, 5–8 Maximum**, with manual customization and Restore.
- Direct vanilla particle/blur controls, optional reduced hit effects and less repeated context/event/hierarchy work.
- World-interaction fixes for mushrooms, metal recoil, shade gates, slopes, conveyors, crystal dash and enemy sensors/targeting.
- Improved party death, healing/background rendering, boss-roar motion and interaction/menu control recovery.

See the [v0.11.0 release notes](docs/RELEASE_NOTES_v0.11.0.md), [changelog](CHANGELOG.md) and [features](docs/FEATURES.md).

## Installation

Install the matching Modding API, then extract **HollowKnightLocal8-v0.11.0.zip** into the existing mod folder under the game's `Mods` directory. Replace the old DLL; do not keep multiple versions installed. Existing settings and saves are retained.

The installation ZIP contains only **HollowKnightLocal8.dll**. All mod-owned artwork and translations are embedded. [Download v0.11.0](https://github.com/RaulSB-code/HollowKnight-Local8/releases/download/v0.11.0/HollowKnightLocal8-v0.11.0.zip).

For Lumafly, search **Hollow Knight 8-Player Co-op**. The catalog follows ModLinks; a newly published GitHub version appears there after its update request is merged. See [installation](docs/INSTALLATION.md).

## Playing

Load a save and press Start on another controller to join, or use **F8** to configure players and keyboard profiles. The panel key is configurable. Nine role choices, including no role, are available; later role/equipment changes respect bench rules.

**RB** is assigned to dream rescue instead of quick cast during co-op. The default keyboard rescue key is **1**, and bindings can be changed. Hold it to move as dream essence and rejoin another living player, subject to cooldown and safe landing.

Up to four keyboard profiles are supported. Physical keyboard rollover limits can affect simultaneous keys; the mod provides a warning and rebinding options. Supported vJoy/DirectInput devices are also available.

## Profiles, charms and performance

In **F8 → Players**, select the target player to create, save or load a named profile. Profiles retain identity, role, charm preferences, appearance, extra keyboard bindings and adventure totals. Loading equipment respects ownership, notches and the bench/initial-join window; it does not grant resources or story progress. One profile cannot be active in two slots.

Open the charm inventory near a bench to navigate each player's own catalog and equipped card. Moving up beyond the catalog's top row enters that player's equipped card; submit removes the selected equipped charm where permitted. Supported custom charms use their real IDs, icons and provider descriptions. Acquired Void Heart remains permanent.

In **F8 → Game → Performance**, use Automatic or select a preset. The recommendation follows joined player count. Changing an individual option selects Custom, preserving the manual choice. Vanilla particles, blur, bloom, decoration and damage effects can be adjusted separately; Restore recovers the previous graphics settings. These profiles reduce visual/CPU costs but do not guarantee a particular frame rate.

## Compatibility and testing

**Custom Knight** has dedicated integration for individual skins. **Enemy HP Bar** is useful for shared fights. Neither is required. Custom-charm providers and **Pale Court** have additional handling, but larger content mods and unusual scripted scenes remain experimental. See [compatibility](docs/COMPATIBILITY.md).

This release uses alpha227 gameplay and includes controlled tests of profiles, menus, contexts, charms, PvP, world ownership and lifecycle cleanup. The DLL is rebuilt from the complete source. Recent interaction/control fixes still need wider native gameplay coverage; eight simulated slots are not a test with eight physical controllers.

[Build instructions](docs/BUILDING.md) · [source notes](docs/SOURCE_NOTES.md) · [release verification](docs/VALIDATION_v0.11.0.md)

[Report bugs](https://github.com/RaulSB-code/HollowKnight-Local8/issues) with the version, game version, player count, other mods, scene, reproduction steps and relevant log.

Developed by **RaulSB-code**. Hollow Knight belongs to Team Cherry. This is an unofficial community mod and is not affiliated with Team Cherry. AI-assisted development is disclosed in the existing ModLinks tag and release notes.
