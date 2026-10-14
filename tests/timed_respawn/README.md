# Timed respawn regressions

Run `python3 tests/timed_respawn/run.py --dotnet /path/to/dotnet` with a .NET 8 SDK.

Compile the actual `TimedRespawnRecovery` implementation and actual extracted
death, hazard, room-rebuild dispatch, recovery dispatch and HUD status methods.
Exercise all player slots and rosters, scene loads before/after the deadline,
death at time zero, real subsequent deaths, oldest-survivor priority and safe
fallback, bounded retries, invalid anchors, all-party death, PvP, missing actors,
ordinary/fatal hazard overlap, Radiance checkpoint priority and shared HUD timing.

Unity physics and native `Recover` are controlled doubles. The separate compiled
DLL audit verifies that native recovery, collision/path checks, story/input/UI
guards and unrelated gameplay remain intact. This suite cannot prove safe
terrain or animation behavior in every native Unity scene.
