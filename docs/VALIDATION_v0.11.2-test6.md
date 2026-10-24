# v0.11.2-test6 validation

Base: public v0.11.1 `989dbec8cc970250b15dbf18db50b55e355e0c72` plus test1–test5.
Target: Hollow Knight 1.5.78.11833 and matching Modding API. Compiler: .NET SDK
8.0.408; .NET Framework 4.7.2 mod assembly. All 28 repository suites pass.

## Reproduced failures and corrections

1. A full native controller reset clears filtered P1 button actions before the
   test5 MapControllerButtons guard runs. A native settings refresh need not
   restore them when the template signature is unchanged. The regression uses
   the actual outer reset IL with only the old inner guard as a negative control.
2. Native RemapUIButtons resets menu actions to their default-binding lists;
   filtered copies use regular bindings. The negative control loses submit and
   rescue bindings. Complete config operations now use the default-backed
   original action template, including nested callbacks and delayed UI setup.
3. InControl PlayerActionSet.Load catches malformed data and silently calls
   Reset. Old try/catch guards therefore did not prevent an empty keyboard set.
   The native exception handler reproduces this. Explicit result validation
   restores the previous working bindings and rejects invalid keyboard profiles.
4. The extra-keyboard Reset button disposed the old actions and reloaded the
   same saved override. Reset now prepares defaults without that override,
   loads them into the same action objects and saves them.

## New validation

- Exact native IL: MapControllerButtons, ResetDefaultControllerButtonBindings,
  ResetDefaultKeyBindings, RemapUIButtons, MapDefaultKeyboardLayout and Load,
  including Load's exception region. Method opcode/branch sequences are kept.
- Native Key enum values and deduplicated default/regular binding behavior in
  the input fixture. This removes duplicate bindings the older fixture allowed;
  raw assertion totals are not comparable to earlier fixture totals.
- Configuration lookup/save, delayed setup/resume/cancel/exception/dispose,
  scoped guest contexts, foreign handlers, double installation and full removal.
- Real KeyboardMemory and ProfileKeyboard methods execute under distinct host
  type names. Invalid data, mixed input types, valid restore, selected capture,
  duplicate-save suppression and default persistence are checked. Snapshot byte
  storage and disk writes are controlled; the full native binary deserializer
  and real Unity coroutine scheduling are not simulated.
- Existing eight-pad selection, swaps, shared-keyboard layout IDs, reconnect,
  references, 10 UI languages, five resolutions, SFCore grid, health effects,
  hazards, charm menus, profiles, interactions, death and performance checks pass.

Results: primary_controller 3,226 assertions; controller_native 62,501;
player_profiles 8,565; menu_ui 107,178; localization 14,306; title_badge 324.
These suites overlap and do not represent a count of unique gameplay scenarios.

## Release limits

No native Unity playthrough, physical PS5/cable/Bluetooth/Steam Input matrix,
real Remote Play session or actual Pale Court rendering run was available.
The reported Crossroads → Dirtmouth → Crossroads stag return remains
unconfirmed by a native round trip. Its test1 route/vote guard remains covered;
this audit does not establish a newly proven fix for that softlock.

The supplied test4 log's inactive OptionsMenuScreen coroutine message has no
stack; shutdown also logs missing GameManager. No speculative production change
was attributed to either message. No push, release or ModLinks update occurred.
