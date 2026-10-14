# World interaction regressions — alpha226

Run from the complete source tree:

```sh
python3 tests/world_interactions/run.py --managed /path/to/Managed --dotnet /path/to/dotnet
```

Requires the matching Modding API-patched Managed folder, its Mono.Cecil.dll,
and a .NET SDK. No game libraries or previous mod DLL are bundled or used as
production source.

The runner compiles the actual WorldContactRouting, SteepSlopeRouting,
ShadowGateRouting, PartySensorOccupancy, WalkerTargeting, PhysicsGuard, Reflect
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

The real native ShadowGate OnDestroy event-registration implementation is not
transplanted; its original-chain/lifetime callback is simulated. Component
discovery, colliders, Physics2D, effects, event dispatch and PlayerContext's
Unity-facing dependencies are controlled doubles. The unchanged production
context service is additionally exercised by the existing context_fps suite.

These checks do not run Unity physics, game scenes, physical controllers,
GPU rendering or a real game FPS benchmark. See docs/PRUEBAS_alpha226.md for
the required in-game pass.
