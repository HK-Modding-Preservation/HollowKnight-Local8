# v0.11.2-test9 validation

Base: public v0.11.1 `989dbec8cc970250b15dbf18db50b55e355e0c72` plus test1–test8.
Target: Hollow Knight 1.5.78.11833 and matching Modding API; SDK 8.0.408;
.NET Framework 4.7.2 mod assembly. All 31 repository suites pass.

## Scope and native behavior

Camera implementation, presence rules and settings are unchanged. Elder Baldur
still closes when a living nearby hero satisfies its native proximity rule;
no enemy attack, armor, damage or state transition is forced.

SoulOrb.Start caches HeroController.instance, pooled OnEnable does not refresh
that target, and Zoom later awards soul and flashes the current singleton.
Production routing records the accepted totem nail hitter, binds each activation
to that actor and enters its context separately for every native iterator step.
The native orb's target, two-soul amount, flight, flash, collection delay and
recycling remain native. Concurrent orbs retain separate ownership; an old
coroutine cannot affect a newly pooled activation. A replaced/retired actor's
orphaned orb is cleaned up without rewarding P1 or the replacement. Removed
the old global three-second soul-redirection heuristic that could steal other
sources' soul while leaving the visual at P1.

Native PlayMaker OnTriggerEnter2D/Stay calls UpdateStateChanges before returning.
The contact actor is scoped to that callback; only a native state entry commits
the persistent hitter. Ignored cooldown contacts cannot steal delayed rewards.

Lifeseed health events give one blue mask to each eligible, living, ready and
present player within 12 world units of the source bug. Loading, rescue ghosts,
waiting voters, dead and disconnected guests are excluded. Delivery is consumed
once per bug lifetime, including broadcasts delivered in later frames; native
HealthManager.OnEnable permits a new pooled lifetime. Other blue-health pickups
retain their existing individual collection behavior.

Enemy native alert queries refresh geometry after missing enter/exit callbacks.
Sight checks retain native walls/ranges and select a visible eligible hero when
the nearest hero is occluded. FSM and Walker hero references share that target.
Geometry and sight are reused within a frame; no scene scans were added.

Completed entries watch briefly for accidentally zero gravity after native
entry/dash restoration. Repairs preserve velocity, nonzero/reverse gravity and
configured zero defaults, and defer to active casts, dashes, conveyors, recoil,
story scenes and other native control holds. Persistent deep solid penetration
is checked at ten Hz, distinguishing ordinary/shallow/transient contact. A
stuck actor recovers through validated saved points or safe living allies
without an additional health penalty. Paused/story time does not count toward
the persistence threshold.

## Evidence

- `party_rewards`: 314 checks. Transplants six exact native SoulOrb methods and
  all six Zoom iterator methods, preserving opcode/branch sequences and locals.
  The unhooked negative control reproduces P1 receiving another player's soul,
  target and flash. Covers all slots, concurrent flights, pooling without Start,
  old generations, ignored cooldown hits, exceptions, orphan cleanup, visual-only/dontRecycle paths,
  lifeseed eligibility/radius, delayed duplicates and ordinary pickups.
- `world_interactions`: exact native LOS/Walker/alert method IL with production
  routing; negative controls for nearest-only sight and missing contacts,
  occluded/visible actors, native range limits, every slot, waiting exclusion,
  named queries, same-frame reuse and existing contact/lifecycle regressions.
- `spawn_safety`: full production entry/terrain guards and previous placement
  code. Every slot, jump momentum, intentional motion holds, safe-point
  obstruction, shallow/transient/deep penetration, sideways motion, pause
  timing and unchanged health. Native Recover execution is a validated-placement
  boundary in the new motion fixture.
- All previous controller/keyboard, menu, room-wait, spawn, profile, charm,
  localization, death, performance, title-icon and interaction suites pass.
  The native controller suite retains its 230,760 checks. Counts overlap and
  are not unique gameplay scenarios.

## Limits

Unity physics, rendering, player-data IO and scene assets use explicit doubles.
Exact native method IL is exercised where specified, not a running Unity game.
The reported Baldur, Elder Baldur and Moss Knight scene FSMs are unavailable
here; their complete encounter behavior still requires an in-game playtest.
Likewise, falling-platform assets, room transitions, hardware controllers and
Steam Remote Play require a real game. No claim of universal compatibility or
zero bugs is made. No GitHub, release or ModLinks mutation occurred.
