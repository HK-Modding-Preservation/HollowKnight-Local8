# v0.11.2-test8 validation

Base: public v0.11.1 `989dbec8cc970250b15dbf18db50b55e355e0c72` plus test1–test7.
Target: Hollow Knight 1.5.78.11833 and matching Modding API; SDK 8.0.408;
.NET Framework 4.7.2 mod assembly. All 30 repository suites pass.

## Reproduced placement failures

Frozen test7 methods reproduce two faults: JoinDestination returns an unchecked
coordinate after every safe option fails; final recovery accepts a body-clear
point without floor support. Joining also validated P1's geometry rather than
the arriving body, and finish-spawn did not revalidate native initialization.

The new join selector returns success/failure and never supplies an unchecked
fallback. Initial cloning and parked activation are deferred before native IO
when no landing is valid. Complete finish-spawn checks placement before native
reset/input restoration and after native initialization; an obstructed actor
is parked for retry without losing its downed state.

Body queries include offsets, mirrors, scale and rotation, reject non-finite
coordinates and require the entire body to lie within game room dimensions.
Camera framing bounds are not used as map bounds. The overlap query now sees
hazards close to the feet and thin obstacles at body edges. Floor support
recognizes enabled solid terrain layers 8 and 25 and rejects incomplete saturated
queries. Nearby placement aligns feet between different bodies; floor search
sweeps the body so an otherwise clear point across a wall is not selected.

Group/scripted teleport, dream-shrine and final-boss gathering validate each
actor's landing. Arena transfers require support as well as clearance. Rescue
restoration rechecks current/return positions, then validated saved points or
living actors. If no landing remains, it preserves collider/renderer flags and
health while normal recovery retries with the actor hidden and frozen.

## Evidence and regression checks

- `spawn_safety`: 718 assertions. Full production SpawnSafety/DuelGround/
  ArenaLanding/rule classes, complete finish-spawn iterator, parked activation,
  placement selectors and group/rescue methods. The initial cloning validation
  guard runs up to the controlled native Instantiate boundary.
- Rectangular-world ray/box casts exercise every slot, invalid bounds/NaN,
  different/disabled/mirrored/rotated bodies, floor layers/normals, pits,
  inaccessible regions, moving gates, native charm resize, initialization
  retries/scene epochs, downed P1 arrival proxies and saturated physics buffers.
- All 29 previous suites pass: 230,760 controller-native checks, 536 room-wait
  checks and existing keyboard, menus, profiles, ten-language localization,
  charms, effects, interactions, death, performance and title-icon regressions.
  Counts overlap and do not represent unique native gameplay scenarios.

## Limits

The collision world, native initialization boundaries and rendering are explicit
doubles. This does not verify every Unity tilemap, platform effector, third-party
mod asset, native cinematic or actual game session. The native P1 scene-entry
sequence remains under vanilla control; Local8 placement/recovery now validates
all player slots. There is no real controller/Steam Remote Play test or native
Crossroads → Dirtmouth → Crossroads stag round trip here. Existing stag/dream/
boss sequence guards remain unchanged. No GitHub or ModLinks mutation occurred.
