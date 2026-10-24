# Controller join regression

Run `python3 tests/controller_native/run.py --managed /path/to/Managed --dotnet /path/to/dotnet`.

This extends the existing production Controls/PrimaryController/session tests.
The runner imports the game's enum values and transplants the exact
`InputHandler.MapControllerButtons` method body from the supplied matching
`Assembly-CSharp.dll`. Its opcode and branch sequence is retained. The
unhooked negative control reproduces the reported loss of controller directions
while buttons still work. The production isolation hook then prevents it.

Coverage includes eight simulated devices, native controller layouts, repeated
active-device changes, D-pad and stick directions/releases, explicit keyboard
P1, per-player binding isolation, scoped guest input, exceptions, foreign input
handlers and balanced hook installation. The original primary-controller
regressions for swaps, ports, saved bindings and reconnect also run.

InControl binding storage and values, Unity objects, settings IO and callback
delivery use explicit doubles. This is not a PS5 hardware, Bluetooth, Steam
Input or Unity gameplay test. No game binaries are included.

The real periodic RefreshBindings method is exercised after native remap
callbacks, including keyboard-only changes that must preserve held controller
action sets. Production keypad defaults and all primary-controller scenarios
run in this host too.

Test6 adds exact native IL for ResetDefaultControllerButtonBindings,
ResetDefaultKeyBindings, RemapUIButtons, MapDefaultKeyboardLayout and the
exception region of PlayerActionSet.Load. Negative controls demonstrate the
outer reset failure even when the test5 inner mapping guard is active, and the
silent clearing of keyboard controls after malformed base64. Fixtures now use
native Key enum values and binding deduplication matching PlayerAction.

Production config hooks, iterator resume/dispose/exception handling, real
KeyboardMemory and ProfileKeyboard methods (under distinct host type names),
reset persistence and action-reference stability are exercised. Binding snapshot
bytes and settings writes are controlled; the full native binary deserializer,
Unity coroutines and hardware callbacks are not simulated.

# Extended input matrix (test7)

`MatrixTest.cs` adds sustained 2/4/8-player checks with identical virtual-pad
names and distinct stable ports. Every actor's D-pad, stick and jump are read
against every other assigned action set, including press/release. It exercises
native reset/remap bodies, temporarily held actors, every-slot and simultaneous
reconnect with reordered enumeration, repeated full-roster P1 swaps, P1's
keyboard/controller return and focus-loss recovery. These remain simulated
devices; they do not establish physical PS5 or Steam Remote Play compatibility.
