# v0.11.2-test13 validation

Target: HK 1.5.78.11833 with matching Modding API, net472, SDK 8.0.408.
Base: public v0.11.1 plus unpublished test1–test12. No external publication.

## Supplied log evidence

Local8-20261024-075334-805.log identifies its runtime as test11. It contains
no DREAM rescue start record. P2 has one arena gather and two timed respawns;
these are intentional automatic actions, independent of the rescue button.
The anomalous repetition is P1's eleven TERRAIN embedded actor recovered
records, including several closely spaced recoveries. DREAM teleport trail
records are visual notifications and must not be counted as manual commands.
The log does not record the collider names responsible for each recovery, so
the exact geometry behind those eleven detections cannot be reconstructed.

## Totem reward and visual path

The native soul_totem FSM exports use a TAKE DAMAGE/nail-validation/Hit path.
Hit consumes its native value and flings 8–9 Soul Orb R objects in the standard
variant. The new hook scopes the actual native Fling action for soul-orb
prefabs only. Each spawned orb grants its original 2-SOUL unit via the specific
recipient's HeroController.AddMPCharge under that player's context. Native
capacity/reserve and other-mod handling remain in that game method. Nearby
recipients are snapshotted at the statue; current identity/eligibility is
rechecked during delivery. Outside/dead/down/waiting/disconnected/transitioning
or otherwise ineligible players are excluded. Radius is inclusive and uses XY.

Native Fling still owns RNG, spawn count/variation and Finish. Depletion,
hit validation, cooldown, strike flashes and infinite-totem mechanics are not
replaced. Shared orbs use the existing artwork with a bounded outward motion
and fading sprite. Their native homing/award/collection coroutine is bypassed,
including during scene unloading. Pooled sprite color is restored, generations
cannot cancel a reused orb, and scene reset recycles outstanding shared FX.
No new texture, menu text or required dependency is introduced.

The reward fixture executes exact native IL for FlingObjectsFromGlobalPool's
OnEnter plus nine SoulOrb methods (including native Update, FaceAngle and
ProjectileSquash) and all six native Zoom iterator methods. It tests 2/4/8
parties, every hitter, native min/max count, nearby equality, eligibility,
inclusive radius, distant P1, outward velocity/fade, absence of native P1
collection/flash, reset/cancel/pool reuse and non-totem/non-soul passthrough.
Native AddMPCharge and Unity rendering are explicit controlled IO boundaries.

Graph provenance: jakobhellermann/hk-fsms, commit
194ebbdf54d7061aec0e704e6eda1ccc04e9fd1f,
static/pseudocode/hk/soul_totem_Soul Totem 1.txt. Native method bodies were
inspected from the provided matching game assemblies.

## Automatic recovery filtering

The previous generic watchdog treated any persistent penetration over .12 as
an embedding. The revised predicate requires body-center containment and a
depth greater than max(.2, body-height/4), with active solid collider geometry.
It rejects effector geometry, ignored pairs/layers and protected recovery time.
It does not remove real deep-embedding recovery or change the dedicated falling
vine hazard. Recovery logs now identify the blocking object and layer.

Controlled rectangular-world regressions exercise foot overlap, effectors,
ignored pairs/layers, protection, real deep containment, missing-floor retry,
safe destinations and the complete existing Recover/Commit methods. They
cannot identify the actual collider behind an old log entry or reproduce all
native tilemap shapes/GPU output. A real multiplayer playthrough is still needed.

Enemy detector/Baldur source, camera framing/zoom, input source, falling-vine
logic and embedded resources are byte-identical to test12.

## Final verification

All 32 regression suites pass. Frozen test12 production fails the new nearby
totem-sharing requirement and the foot-overlap false-recovery fixture. The
expanded party suite passes 1,716 controlled checks; spawn safety passes 1,536.
These are overlapping assertions, not unique native gameplay scenarios.
A clean checkout rebuild and emitted-assembly verification accompany the DLL
as internal evidence. Native Unity/GPU playtesting remains unavailable.
