# World interaction regressions — alpha226

Run from the complete source tree:

```sh
python3 tests/world_interactions/run.py --managed /path/to/Managed --dotnet /path/to/dotnet
```

Requires the matching Modding API-patched Managed folder, its Mono.Cecil.dll,
and a .NET SDK. No game libraries or previous mod DLL are bundled or used as
production source.

The runner compiles the actual WorldContactRouting, SteepSlopeRouting,
ShadowGateRouting, PartySensorOccupancy, WalkerTargeting, EnemySightRouting, PhysicsGuard, Reflect
and ReferenceField sources with controlled dependencies and hook chains. It
then transplants 26 native method bodies from Assembly-CSharp.dll, preserving
their opcode sequence, locals, parameter references and branch mappings.
Unsupported exception regions or missing fixture members fail the transplant.
The native methods are separate from the hook wrappers, so nested native calls
also pass through production hooks where applicable.

The initial unhooked negative controls reproduce the old incorrect ownership.
Positive tests require the corrected outcomes. Tests cover 2/4/5/8 slots, P1
and guests, native branches/directions, simultaneous actors and colliders,
dying/reviving in place, hero replacement with cached ownership, context and
cached-field restoration after exceptions, another hook's field assignment,
scene reset from a wake event, inherited child trigger geometry, pre-existing
collision ignores, balanced install/uninstall, coherent native LOS and native
conveyor versus genuinely stuck gravity.

Sight regressions reproduce a native nearest-only query missing a visible
second hero behind an occluded first hero, and missing range-enter callbacks.
Native raycasts must retain wall occlusion and range restrictions while FSM
references agree with the visible target. Queries exclude waiting actors,
refresh missed enter/exit state through alert actions and reuse geometry/sight
within a frame. No attack transitions, enemy armor or thresholds are forced.
Review cases also require a real range enter/exit to invalidate only its
dependent sight caches in the same frame, and exclude hidden room voters from
the unchanged native Walker activation threshold. Unchanged repeated reads
still reuse both native raycasts and geometry.

The real native ShadowGate OnDestroy event-registration implementation is not
transplanted; its original-chain/lifetime callback is simulated. Component
discovery, colliders, Physics2D, effects, event dispatch and PlayerContext's
Unity-facing dependencies are controlled doubles. The unchanged production
context service is additionally exercised by the existing context_fps suite.

These checks do not run Unity physics, game scenes, physical controllers,
GPU rendering or a real game FPS benchmark. See docs/PRUEBAS_alpha226.md for
the required in-game pass.

Test11 models Bounds.Intersects in all three axes while collider distance ignores visual Z. The sensor sequence deliberately places alert/wake sensors at different depths from hero bodies; test10 fails this corrected fixture.

Test12 uses real native AlertRange/LOS/Wake method bodies for HeroBox-only contacts, per-actor LOS occupancy and disabled MonoBehaviour behavior. Current tests fail against frozen test11 source; nails do not count as player bodies.
