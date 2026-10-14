# Performance profiles

Run `python3 tests/performance_presets/run.py --dotnet /path/to/dotnet`.

This compiles the real profile/configuration service, native graphics/save hook,
particle-copy scope, settings migration, performance policy and F8 controls.
Controlled Unity/native/IMGUI doubles cover roster changes, deaths, manual
overrides, per-property backups, restarts with applied or original preferences,
external video changes, native saves/exceptions/unload, missing/replaced native
settings, bounded particle refresh failures and zero-allocation steady updates.
Spawn cases model Unity's serialized copy and the supplied native reducer's
private high/low caches; seven guests must each receive Low once and recover
Normal. Failed copies and custom emitter curves retain the source values.

The menu is also executed with the real theme, atlas, localization, profile
callbacks and scrolling in `tests/menu_ui`. `tests/focus_performance` exercises
the real renderer budget at balanced, maximum and independent custom levels.

These are controlled .NET fixtures. The actual Unity player, GPU, frame times,
native font metrics, all boss attacks and combinations of mods require an
in-game trial. Metadata checks in `docs/diagnostics/alpha224/verify_artifact.py`
verify that the compiled build uses the provided native fields and refresh path.
