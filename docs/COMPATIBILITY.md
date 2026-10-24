# Hollow Knight 8-Player Co-op compatibility

Hollow Knight 8-Player Co-op is being developed with compatibility with other Hollow Knight mods in mind.

Current target: **Hollow Knight 1.5.78.11833**, matching Modding API, candidate **v0.11.2**. Existing settings/save identifiers are preserved.

Compatibility can vary because larger mods may introduce custom rooms, enemies, bosses, interactions and scripted sequences that were not designed for multiple local players.

## Recommended

### Custom Knight

Recommended for Hollow Knight 8-Player Co-op.

The mod can detect Custom Knight and use installed body/HUD skins for individual players. Some attack sprites and particle effects can still follow Custom Knight's globally selected skin. This is a known cosmetic limitation; per-player effects are not claimed as fully supported.

### Enemy HP Bar

Recommended, especially with larger groups.

Every player attacks the same enemies and bosses, so visible enemy health bars make fights easier for the whole group to read.

## Experimental

### Pale Court

Pale Court is currently being tested with Hollow Knight 8-Player Co-op.

Parts of the mod already work, but support is still experimental and multiplayer-specific problems are expected. It should not yet be treated as fully compatible.

## Future compatibility

Other Hollow Knight mods will continue to be tested as Hollow Knight 8-Player Co-op continues development.

Mods that only make simple visual or interface changes are generally easier to support. Large content mods may require specific multiplayer handling.

## Custom charm providers in v0.11.0

The mod supports additional real inventory rows, sparse charm IDs, provider icons/descriptions and independent per-player equipment/preferences. It validates available provider APIs, preserves bench/notch/binding rules and keeps unavailable preferences for future loading. A provider introducing its own HeroController/FSM assumptions can still need dedicated multiplayer fixes.

Pale Court has extra safeguards for cloned actor initialization, actor-owned Bloom listeners, temporary Boon templates and known spell callbacks. These changes improve specific reproduced paths; they do not establish complete Pale Court compatibility.

## Validation limits

Two-to-four-player gameplay logs informed recent fixes. Automated hosts cover up to eight slots, but full native room/controller/GPU behavior and every mod combination have not been verified. The v0.11.2 component fixes have controlled-test coverage and await wider native gameplay reports.
