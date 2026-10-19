# Hollow Knight 8-Player Co-op

**Public version: v0.11.1 · DLL version: 0.11.1 · ModLinks version: 0.11.1.0**

Local cooperative multiplayer for **2–8 players** inside one Hollow Knight game. Players share the world, enemies, bosses and story progression while keeping their own controls, health, SOUL, charms, optional roles, profiles and supported Custom Knight skins.

Target: **Hollow Knight 1.5.78.11833** with its matching **Hollow Knight Modding API**.

## What is new in v0.11.1

- P1 automatically uses the active or first connected controller. **2–8 controllers can play without a mandatory keyboard player.**
- P1 control selection is now at the top of **F8 → Players & controls**: Automatic, Keyboard, or Assign P1 beside a controller.
- An occupied controller can be assigned to P1 by exchanging control devices; characters, profiles and progression keep their slots.
- Reconnecting P1 restores its controller bindings without taking another player's controller.
- The old forced-keyboard default migrates to Automatic once. Choosing Keyboard afterwards is saved normally. Existing specific-controller preferences are retained.

See the [v0.11.1 release notes](docs/RELEASE_NOTES_v0.11.1.md) and [validation](docs/VALIDATION_v0.11.1.md).

## Previous major update: v0.11.0

- Named player profiles and per-player adventure statistics, portable between slots and sessions.
- Spells-only PvP with configurable SOUL regeneration and safer duel entry from jumps, falls and benches.
- Redesigned F8 menus, translated hover help and expanded native/custom charm navigation, icons and descriptions.
- Automatic performance presets: **1–2 Normal, 3–4 Balanced, 5–8 Maximum**, with manual customization and Restore.
- Direct vanilla particle/blur controls, optional reduced hit effects and less repeated context/event/hierarchy work.
- World-interaction fixes for mushrooms, metal recoil, shade gates, slopes, conveyors, crystal dash and enemy sensors/targeting.
- Improved party death, healing/background rendering, boss-roar motion and interaction/menu control recovery.

See the [v0.11.0 release notes](docs/RELEASE_NOTES_v0.11.0.md), [changelog](CHANGELOG.md) and [features](docs/FEATURES.md).

## Installation

Install the matching Modding API, then extract **HollowKnightLocal8-v0.11.1.zip** into the existing mod folder under the game's `Mods` directory. Replace the old DLL; do not keep multiple versions installed. Existing settings and saves are retained.

The installation ZIP contains only **HollowKnightLocal8.dll**. All mod-owned artwork and translations are embedded. [Download v0.11.1](https://github.com/RaulSB-code/HollowKnight-Local8/releases/download/v0.11.1/HollowKnightLocal8-v0.11.1.zip).

For Lumafly, search **Hollow Knight 8-Player Co-op**. The catalog follows ModLinks; a newly published GitHub version appears there after its update request is merged. See [installation](docs/INSTALLATION.md).

## Playing

Connect the controllers, then load a save. Automatic mode gives P1 the active controller, or the first connected one. Press Start on the other controllers to join as P2–P8. If no controller was connected when the save loaded, the first controller's Start assigns it to P1 while P1 is still alone; later controllers join normally.

For keyboard P1 plus controller guests, select **F8 → Players & controls → Keyboard** first. To choose a particular controller, use **Assign P1** beside it. If already occupied, control devices are exchanged while player slots keep their identity. The panel key is configurable. Nine role choices, including no role, are available; later role/equipment changes respect bench rules.

**RB** is assigned to dream rescue instead of quick cast during co-op. The default keyboard rescue key is **1**, and bindings can be changed. Hold it to move as dream essence and rejoin another living player, subject to cooldown and safe landing.

Up to four keyboard profiles are supported. Physical keyboard rollover limits can affect simultaneous keys; the mod provides a warning and rebinding options. Supported vJoy/DirectInput devices are also available.

## Profiles, charms and performance

In **F8 → Players**, select the target player to create, save or load a named profile. Profiles retain identity, role, charm preferences, appearance, extra keyboard bindings and adventure totals. Loading equipment respects ownership, notches and the bench/initial-join window; it does not grant resources or story progress. One profile cannot be active in two slots.

Open the charm inventory near a bench to navigate each player's own catalog and equipped card. Moving up beyond the catalog's top row enters that player's equipped card; submit removes the selected equipped charm where permitted. Supported custom charms use their real IDs, icons and provider descriptions. Acquired Void Heart remains permanent.

In **F8 → Game → Performance**, use Automatic or select a preset. The recommendation follows joined player count. Changing an individual option selects Custom, preserving the manual choice. Vanilla particles, blur, bloom, decoration and damage effects can be adjusted separately; Restore recovers the previous graphics settings. These profiles reduce visual/CPU costs but do not guarantee a particular frame rate.

## Compatibility and testing

**Custom Knight** has dedicated integration for individual skins. **Enemy HP Bar** is useful for shared fights. Neither is required. Custom-charm providers and **Pale Court** have additional handling, but larger content mods and unusual scripted scenes remain experimental. See [compatibility](docs/COMPATIBILITY.md).

This release builds on v0.11.0/alpha227 gameplay with the P1 input corrections above. Controlled tests cover P1 selection, eight controller slots, swaps, keyboard bindings and reconnects, plus existing profile/menu/charm regressions. The DLL is rebuilt from the complete source. Physical controllers and native Unity gameplay still need in-game confirmation; eight simulated slots are not an eight-controller hardware test.

[Build instructions](docs/BUILDING.md) · [source notes](docs/SOURCE_NOTES.md) · [release verification](docs/VALIDATION_v0.11.1.md)

[Report bugs](https://github.com/RaulSB-code/HollowKnight-Local8/issues) with the version, game version, player count, other mods, scene, reproduction steps and relevant log.

Developed by **RaulSB-code**. Hollow Knight belongs to Team Cherry. This is an unofficial community mod and is not affiliated with Team Cherry. AI-assisted development is disclosed in the existing ModLinks tag and release notes.
