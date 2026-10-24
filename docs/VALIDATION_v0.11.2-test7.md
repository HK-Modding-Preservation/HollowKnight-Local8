# v0.11.2-test7 validation

Base: public v0.11.1 `989dbec8cc970250b15dbf18db50b55e355e0c72` plus test1–test6.
Target: Hollow Knight 1.5.78.11833 and matching Modding API; .NET SDK 8.0.408;
.NET Framework 4.7.2 mod assembly. All 29 repository suites pass.

## Supplied log and reproduced regression

`Local8-20261023-183408-540.log` runs test6 with P1 on XInput and P2 on keyboard.
At 18:40:28.541, a native door queues the first vote; its interaction remains
claimed until the scene transition at 18:40:33.543. Other attempts complete
early when the second actor joins. These are station doorway changes, not an
actual stag journey. No physical eight-controller run is present in the log.

The frozen test6 TransitionVote runs as a negative control. It retains the
black fade and native door ownership while waiting. Production changes clear
that deferred fade, suppress only ordinary transition fade requests on the
actual camera fade FSM and release only the entering actor's scene interaction.
Door fallback accepts the exact validated door/entry while its prompt is hidden;
range, actor eligibility, lock states and prerequisites still apply. Commit
restores normal native loading fade and the winning actor's context.

Pause timestamps now advance for each vote as well as the aggregate timer,
so removing a voter after pausing cannot reuse an old, expired timestamp.

## Coverage

- `room_wait`: 536 assertions. Complete production vote and visibility classes,
  extracted unchanged door-selection logic and targeted interaction release;
  2/4/8 actors, every first entrant, early completion, late/pre-existing fades,
  route identity, gates, locks, range, prerequisites, timeout, majority/ties,
  cancellation, pause/settings, disconnects and unrelated NPC ownership.
  Death/hazard/story fades, stag menu/cinematic, dream/boss/arena/scripted flow
  and foreign FSMs/controllers are checked outside ordinary wait recovery.
- `controller_native`: 230,760 assertions, including overlapping existing
  controller checks. Exact native mapping/reset/UI/default-layout/Load IL is
  retained. Added sustained 2/4/8-pad all-actor D-pad/stick/jump isolation,
  every-slot and simultaneous stable-port reconnect with reordered matching
  devices, full-party P1 swaps, keyboard/controller return and focus recovery.
- All existing menus, charms, interactions, effects, profiles, death, performance,
  title grid and ten-language localization suites pass. No UI strings were added.

## Limits

Unity rendering/physics, native scene loading, physical devices and callback
delivery use controlled boundaries. Tests do not prove visual behavior in a
running game, physical PS5 Bluetooth/cable/Steam Input compatibility or a real
Steam Remote Play session. The older Crossroads → Dirtmouth → Crossroads stag
softlock remains unconfirmed by a native round trip; its existing route guard
is retained. The inactive OptionsMenuScreen log error lacks a stack and has not
been assigned a speculative fix. No push, release or ModLinks mutation occurred.
