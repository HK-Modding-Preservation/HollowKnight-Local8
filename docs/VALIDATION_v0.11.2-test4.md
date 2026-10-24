# v0.11.2-test4 validation

Base: public v0.11.1 `989dbec8cc970250b15dbf18db50b55e355e0c72`, retaining
the unpublished test1–test3 changes. Build: .NET SDK 8.0.408, matching Modding API
and Hollow Knight 1.5.78.11833 references. The log supplied in this request
confirms test3 was loaded and the pregame panel was visible.

All 28 repository test suites passed after updating three stale fixture
contracts: event_search's particle-metrics/Fsm fields, optimized_fps's current
inventory/bench request guards, and pvp_spells's settings mode constant. Tests
keep the production methods; no production behavior was removed to pass them.

## Added checks

- Hazard feedback uses the owner's Take Hit prefab for every player slot.
  Factories for spike/acid animated-body prefabs fail if called, including
  swallowed-exception checks. Missing-prefab fallback, acid immunity, duplicate
  hazards, fatal hazards and the saved Performance switch remain covered.
- Pregame P1 selection preserves the count and slot objects, swaps occupied
  devices, rejects invalid/applied reservations and survives loading a save.
  Reselecting the current in-game device preserves the exact action set.
- Eight identical-name virtual XInput ports: keyboard host, late guest joins,
  occupied/unoccupied P1 pad selection, switching back to keyboard, port
  reconnection and focus loss/return. This is the consumer input contract used
  by streaming gamepads, not an actual Steam/Parsec session.
- Existing native controller-remapping IL tests cover d-pad/stick bindings
  on eight simulated pads and every native layout. Menu callbacks, all ten
  language resources, HUD rendering guards, charms, profiles, death/recovery,
  interactions, spells, lighting and performance regressions also pass.

## Steam and game limits

Official documentation:

- https://partner.steamgames.com/doc/features/remoteplay
- https://partner.steamgames.com/doc/api/ISteamRemotePlay
- https://store.steampowered.com/app/367520/Hollow_Knight/

Valve enables Together through the application's Steamworks settings. The
official ShowRemotePlayTogetherUI API reports failure when the game is not
configured for Together. Hollow Knight currently lists Remote Play for devices,
without Together. The mod does not modify Steam/client app identity or claim
to enable the unavailable official invitation. Remote keyboard/mouse normally
shares host input; independently exposed pads keep their own device binding.

Physical controllers, real streaming, serialized prefab appearance and arbitrary
third-party mods still need in-game testing. Passing offline checks does not
establish universal compatibility. The reported stag return remains unconfirmed
by a native round trip. The supplied log's one inactive OptionsMenuScreen
coroutine error has no stack and is not assigned a speculative fix.
