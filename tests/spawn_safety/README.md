# Spawn and placement safety

Run `python3 tests/spawn_safety/run.py --dotnet /path/to/dotnet`.

Executes the full production SpawnSafety, DuelGround, ArenaLanding,
RecoveryRules and PartyRules classes in a controlled collision world. Complete
production finish-spawn iterator, parked-actor resume, placement selectors,
group warp, dream shrine gathering, final boss gathering and rescue restoration
methods are extracted without edits. The initial cloning validation guard runs
up to the actual native clone boundary; Instantiate itself is not simulated.

Frozen test7 methods reproduce an unchecked join fallback and accepting empty
air during recovery. Coverage includes full-body bounds and non-finite values,
disabled/offset/rotated/mirrored/scaled bodies, narrow walls, hazards at feet,
terrain layers, floor normals, query saturation, disconnected regions,
every player index, moving gates, native charm resize between initialization
frames, retry without activation, scene epochs, downed native arrival proxies,
per-actor group placements, arena support and rescue cancellation without a
valid return position. Failed rescue restoration preserves health and collider/
renderer flags while ordinary hazard recovery retries its landing.

The production EntryGravityRecovery and TerrainEmbedRecovery run for every
slot. Tests preserve jump momentum and intentional dash/cast/conveyor gravity
holds, exclude pause/story/bench/vote/death states, and distinguish ordinary,
shallow and transient contacts from persistent deep penetration while moving
sideways. A blocked saved point must fall back to a supported safe point.
Complete production Recover, Commit and checkpoint methods now execute in
these tests. Assertions cover unchanged health, blue masks, soul/reserve and
restored input, body, layer, arrival event, protection and safe-point state.
A missing valid landing must preserve position/vitals and retry after support
becomes available. Unity/platform simulation, event IO and encounter hooks
remain controlled doubles.

Physics queries use explicit rectangular-world doubles, with slab ray/box
casts. This verifies selection and lifecycle logic, not Unity tilemaps,
platform effectors, scene assets, rendering, actual mod interactions or gameplay.

Test12 adds complete production FallingPlatformHazard with controlled native damage/cut/land IO: falling underside versus on-top/side/stationary/rising contacts, every slot, one base-damage call, complete non-respawn Recover, fatal/blue/unsafe/retry and reset cases. This is not Unity physics or an actual platform playthrough.

Test13 distinguishes genuine deep body-center embedding from foot overlap, effector/ignored surfaces and protected recovery time. The same controlled foot fixture fails against frozen test12 watchdog code. Dedicated native falling-vine hazard tests remain unchanged.

Test14 additionally checks trigger/kinematic/unsimulated bodies, legitimate
Hornet dialogue and explicit actor isolation. Full production Recover/Commit
runs for each trapped slot under another actor context; all other positions,
vitals and reset counts must remain intact.
