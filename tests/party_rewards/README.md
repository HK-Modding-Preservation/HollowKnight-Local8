# Party reward regressions

Run `python3 tests/party_rewards/run.py --managed /path/to/Managed --dotnet /path/to/dotnet`.

Compiles complete production SoulOrbRouting, SoulTotemSharing and Lifeblood with
controlled Unity and health-asset callbacks. The complete production
PlayerContext also executes, including its P1 fast path and nested guests;
native singleton/input objects and PlayerData reads/writes remain controlled.

Transplants the exact native SoulOrb Awake, Start, OnEnable, OnDisable, Zoom
and FireAtTarget method bodies and all six methods of its Zoom iterator from
the supplied matching Assembly-CSharp.dll. Opcode/branch sequences and locals
are preserved; unsupported exception regions and missing members fail. No
native game code or references are distributed with the test source.

An unhooked negative control reproduces P1 receiving another player's totem
target, soul and flash. Production hooks must route the unchanged native flight,
two-soul award, flash, collection delay and recycling to the hitter. Tests cover
all eight slots, simultaneous flights, delayed collection, pooling without Start,
old coroutine generations, replaced actors, scene cleanup, exceptions,
visual-only orbs, dontRecycle and balanced hook installation/removal.
Accepted state entries retain the hitter across ignored cooldown contacts;
scoped contacts are discarded after native callbacks and scene reset.

Lifeseed checks cover nearby party eligibility, inclusive 12-unit radius centered
on the bug, distant killers, repeated broadcast delivery across frames, distinct
bugs and pooled lifetime reset. Ordinary blue pickups keep their single collector.

This executes native coroutine IL against simulated Unity services. It does not
run the game's soul-totem/lifeseed FSM assets, rendering, scene unloading or an
actual multiplayer playthrough.

Test13 uses the native totem Fling action for sharing. Its exact OnEnter IL and
the native SoulOrb Update/FaceAngle/ProjectileSquash IL also execute. Every
nearby actor receives each spawned orb's native 2-unit SOUL grant, while the
visual uses outward motion/fade and bypasses native homing/collection.
The earlier per-owner fixtures remain for non-shared orb lifetimes. Their
manually created orbs do not model the new totem Fling scope. Source counts,
eligibility/radius, pool/reset/cancel behavior and unrelated flings are checked.

Test15 adds exact ScuttlerControl.Hit and Heal IL with all closure/iterator
methods. It reproduces the native delayed P1-only event, then checks shared
actual native deaths, attack acceptance/delay, all nearby parties, duplicate
events, unloading, rearmed pooling and retained initial burst/screen flash.
Unity audio/rendering/event delivery and PlayerData IO remain controlled.

Test17 also enters the native Heal callback after Hit has returned, with every
hitter in 2/4/8-player parties. A captured lifeseed lifetime/session/scene must
still share one mask with P1 and each valid nearby guest, and repeated Heal
entry must not duplicate the reward. `--scuttler-source /path/to/old.cs` runs
the same native-IL fixtures against the previous class as a negative control.
