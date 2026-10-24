# v0.11.2-test2 validation

Based on public v0.11.1 commit `989dbec8cc970250b15dbf18db50b55e355e0c72`
and the unpublished test1 corrections. Build uses .NET SDK 8.0.408 and the
user's Modding API/game references for Hollow Knight 1.5.78.11833.

## Input and lobby

`controller_native` executes the real native MapControllerButtons method body
through the actual isolation hook. The old reset removes copied P1 directional
bindings in its negative control. The protected template handles every native
gamepad layout with eight simulated pads, d-pad/stick press/release, keyboard
P1, foreign handlers and exception restoration. Isolation also remains active
for filtered P1 input before P2 joins.

The shared primary-controller tests execute actual Controls, PrimaryController,
LobbyRoster, HealthChangeFeedback and extracted CoopSession initialization,
join, swap and reconnect methods. They cover keyboard entry with idle pads,
exact first-joined pad ownership, duplicate Start, eight slots, idempotent
handoff, late connections, matching port reconnection, missing reservations,
capture/loading guards, keyboard memory and balanced action-set disposal.

`menu_ui` also executes the actual title lobby polling, entry button and panel
with zero actors. Tests cover native submit ownership, Start with the panel
open, capture/focus/loading/disabled guards, menu callbacks, pregame settings,
translated shortcut help and actor-only actions disabled before a save.
Native input modules are suspended under the overlay, including late modules,
and their previous states are restored after the close-input edge expires.

## Feedback and settings

Health transitions are actor-local, finish in 0.5 seconds and do not touch
health or gameplay input. Tests cover blue-mask loss, several masks lost at
once, healing, respawn, first draw suppression and immediate disable without
replaying old damage on re-enable. The basic HUD fades its health text; the
mask HUD only animates changed cells using the existing mask textures.

`community_report` checks disabled hazard effects without changing hazard
recovery or death, in addition to test1's Fury and stag-route cases.
`performance_presets` checks enabled defaults for new and old settings,
serialization, independent saved preferences across every preset/Restore,
and automatic recommendations from the lobby count. Its steady host updates
still allocate zero bytes and perform no repeated native refreshes or logs.

`localization`, `timed_respawn` and `usability` pass their existing language,
recovery, death/scene/HUD, pause, input and gameplay safety checks.
The menu host checks ten languages at five viewport sizes. Offline draw-call
previews use the real embedded UI atlas and approximate font rendering.

## Limits

These are controlled hosts, with native IL specifically where stated. No
physical PS5 controllers, Steam Input, native Unity scenes, real HUD effect
timing, actual game fonts or in-game frame rate were tested. Please check
keyboard/controller P1, two or more PS5 pads, lobby-to-save handoff, health/blue
mask changes, both feedback switches and the earlier Fury/stag report in game.
