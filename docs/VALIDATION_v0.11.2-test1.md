# v0.11.2-test1 validation

Base commit: `989dbec8cc970250b15dbf18db50b55e355e0c72` (public v0.11.1).
The unchanged base rebuilt byte-for-byte identically to the published DLL using
.NET SDK 8.0.408 and the user's matching Modding API-patched Managed references.

## Controller diagnosis

The native `InputHandler.MapControllerButtons` starts with
`inputActions.Reset()`. Filtered co-op sets copied regular bindings, while
native movement defaults remain on the original action set. A controller
activation could therefore reset P1's pad directions to keyboard directions,
while the rest of the method restored its buttons. The new hook temporarily
routes this native remapping to the original template and restores the current
actor's set in `finally`.

The new `controller_native` host executes that native method's actual IL and
reproduces the old symptom before enabling the production fix. It also runs
the original `primary_controller` suite. Devices and settings IO are simulated.

## Other changes

`FuryRecovery` runs the affected actor's native Deactivate state when its Fury
condition is no longer valid, and cancels only lingering red Fury loops.
`HazardFeedback` spawns the actor's native spike/acid effect without starting
the vanilla whole-party hazard return. Stag cinematic and arrival-gate routes
bypass room voting and keep their original load-info objects/callbacks.

`community_report` checks those production modules and extracted hooks. Native
Unity FSM assets, scene loading, rendering and hardware are not executed.
The stag change addresses an unsafe deferred-load path, but the reported
Forgotten Crossroads/Dirtmouth lock needs an in-game verification.

## Passed suites

- controller_native (includes primary_controller)
- community_report
- timed_respawn
- menu_ui
- context_fps
- party_death
- native_charms
- usability
- world_interactions

Compilation succeeds with the existing source warnings. Game/API assemblies,
native inspection files, test output and local reference paths are not included
in the installer. The installer contains only `HollowKnightLocal8.dll`.
