# v0.11.2-test10 validation

Base: public v0.11.1 `989dbec8cc970250b15dbf18db50b55e355e0c72` plus test1–test9.
Target: Hollow Knight 1.5.78.11833, matching Modding API, SDK 8.0.408 and
.NET Framework 4.7.2. This is an unpublished follow-up to test9 verification.

## Reproduced review failures

New tests executed with exact native Walker/LOS/AlertRange method IL exposed
two additional cases not covered by the test9 suite:

1. The same-frame sight cache could restore `canSeeHero = true` after an actual
   AlertRange exit had already cleared native occupancy. An empty cached sight
   could likewise delay a real entry. Contacts now invalidate only the sight
   records subscribed to that specific range. Unchanged repeated reads still
   reuse geometry and native raycasts.
2. WalkerTargeting's separate activation/movement selector accepted a hidden
   room voter, although generic sight routing already excluded waiting actors.
   It now applies the same exclusion before invoking native Walker logic.
   Native X-distance, camera-distance, startInactive and ambush conditions are
   retained; no attack or armor event is forced.

Frozen failure logs are retained with this build's internal validation evidence.
The expanded native-method fixture passes for 2/4/8 players and every slot,
including same-frame exit/re-entry, unchanged read reuse, hidden waiting actors
and a present actor satisfying the original activation condition.

## Expanded recovery evidence

The spawn fixture now extracts the complete production Recover, Commit,
GameCheckpoint and AwayFromHazard methods unchanged, replacing the previous
validated-placement mock for Recover. Body/input, renderer/collider state and
native event IO remain controlled dependencies. All eight actor slots execute
the real recovery flow. Assertions require health, blue masks, soul and reserve
to stay unchanged for non-respawn recovery, with restored body/input, layer,
native arrival notification, protection and a verified safe point.

Additional cases remove all valid landing destinations: the actor must retain
its position and vitals, then recover safely when support becomes available.
The previous shallow/transient/deep-contact, sideways movement, moving gate,
initialization, pause/story/bench/death guards and entry-gravity checks remain.

## Other retained evidence and limits

The full 31-suite regression run includes 314 party-reward checks using the
exact native SoulOrb iterator/initialization IL, 230,760 controller checks,
room-wait, ten-language UI and prior interaction/performance coverage. Test
counts overlap and do not describe unique in-game scenarios.

Camera implementation, Elder Baldur proximity/armor logic, controls and UI
translations are byte-identical at source level to test9. A matching clean
rebuild and emitted-assembly hook checks accompany the delivered DLL.

There is no running Unity game in this environment. The native Baldur/Moss
Knight FSM assets, falling platforms, Crossroads–Dirtmouth stag round trip,
physical controllers and Steam Remote Play remain in-game verification items.
Passing these checks is not a guarantee of zero bugs or universal compatibility.
No GitHub, release or ModLinks mutation occurred.

## Follow-up: the Elder Baldur party rule

A dedicated `baldur_proximity` suite now compiles the complete unchanged
ElderBaldurRange implementation. A single-hero negative control produces the
premature FAR that the user described. Production distance/X-distance queries,
comparison refresh, direct/named ranges and the Closed-state FAR guard must
keep the enemy closed while any eligible player remains nearby. Cases cover
2/4/8 actors with every possible final nearby player, sequential exits, all-far
opening and return, distinct metrics and ineligible actors.

Action/event/geometry IO is controlled, and actual encounter FSM assets remain
unexecuted. This follow-up changes tests/documentation only. The delivered
test10 DLL and its production sources remain unchanged.
