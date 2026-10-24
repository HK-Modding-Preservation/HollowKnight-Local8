# v0.11.2-test15 validation

Target: HK 1.5.78.11833, matching Modding API, net472, SDK 8.0.408.
Base: public v0.11.1 plus unpublished test1–test14. Nothing published.

## Evidence

Local8-20261024-092905-644.log identifies itself as test14. At 09:37:31.809Z,
CameraLockArea B2 starts gathering with bounds centered at (51.3,37.9),
extents (12.8,7.5), while Hornet is at (19.6,28.6). P1 is at (60.2,32.4) and
P2 at (67.5,32.4). The camera region is a corridor outside the actual fight.
The resulting gather has zero gates, no anchor and repeated no-landing
failures. It expires before combat and marks Hornet's gather finished.

The same log confirms the native corpse progresses through Check Pos -> R ->
Jump -> Throw Start -> Throw -> Yank -> End at 09:38:44–45Z, followed by the
hasDash acquisition. No corpse fallback is activated. Those working states
and the previous ending repair are not changed by this patch.

Native assembly inspection identifies a separate lifeseed path: it is
ScuttlerControl (healthScuttler=true), not HealthManager. Accepted Hit starts
the native Heal coroutine, which later sends ADD BLUE HEALTH through
EventRegister, outside the previous pickup-FSM source scope. The earlier tests
covered legacy sourced FSM events and missed this native route.

## Implementation

The new scuttler hooks preserve the original Hit body, including activation
delay, living-state checks, accepted attacks, journal updates and initial
particles/audio. Only the Heal call inside that exact Hit/session scope is
replaced. Its accepted death immediately shares one mask per eligible actor,
centered on the bug in XY with an inclusive 12-unit radius. Each grant uses
the recipient's PlayerContext and existing HUD feedback. No unsupported hit,
orange scuttler or inactive co-op receives this sharing path. The delayed
global reward is absent, so scene unloading cannot award it twice or give P1
an additional mask. Native 1.2-second visual lifetime/screen-flash prefab are
retained. Token/identity and alive-state checks protect rearmed pooled bugs;
scene reset cleans pending dead visuals without disabling a rearmed bug.

CameraContact rejects a camera region when an identified boss lies outside
its XY bounds plus the small entry tolerance. Camera zoom/framing are untouched.
Hornet's exact native encounter calls the existing gather at Point and at
Leap Antic/Attacked, with the encounter's actual Boss variable. This re-arms
the window after a long conversation or an obsolete earlier window. Known
gate shapes can be used while their colliders are still open. The existing
supported-floor placement, actor identity, fade and cleanup stay in charge of
the actual move. The native dialogue owner/control handoff is preserved.

## Executed checks

All 34 suites pass. party_rewards adds exact native ScuttlerControl.Hit and
Heal method IL plus all eight Heal closure/iterator methods, alongside its
existing exact SoulOrb/Fling bodies. Native Hit/attack acceptance and the
old delayed P1-only event are exercised. Unity particle/audio/material/event
delivery, PlayerData IO and coroutine scheduling remain controlled boundaries.
Cases cover all hitters in 2/4/8 parties, each eligibility exclusion, radius,
repeated dead hits, legacy duplicate events, room unload, rearmed lifetimes,
cleanup, native initial burst/screen-flash and inactive-co-op passthrough.

arena_gather compiles the complete production ArenaGather, HornetEncounterFlow,
ArenaLanding, SpawnSafety and body/geometry rules. Full production IsSafe,
InRoom, FindSafePosition and Commit are extracted from the current session.
The supplied corridor geometry is reproduced, including P2 at 67.5 outside
the corridor bounds. Cases include every intro owner in 2/4/8 parties, open
native gate shapes, long dialogue, leaving/re-entering at gate closure, safe
floor, unchanged vitals, valid boss locks, completely unavailable landing and
scene cancellation. Rectangle physics and coroutine timing are simulated.

Three negative controls fail for the intended functional reasons: the frozen
test14 camera callback starts the wrong corridor encounter, frozen test14
Hornet flow does not gather at the first dialogue, and frozen test14 Lifeblood
does not share through the actual native ScuttlerControl.Hit path.

Compilation, emitted DLL wiring/resources/version and clean full-patch rebuild
are checked. Five production files differ from test14: ArenaGather, Lifeblood,
ReleaseInfo, HornetEncounterFlow and LifebloodScuttlers. Other enemy detection,
controllers, terrain recovery, camera rendering and assets are identical.
No new UI strings or dependencies. Native Unity fight/scene assets, animations,
full physics, rendering and hardware playtesting remain unexecuted.
