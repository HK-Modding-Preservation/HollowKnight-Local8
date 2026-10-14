# Portable profiles, adventure stats and reduced damage audio

`python3 tests/player_profiles/run.py --dotnet /path/to/dotnet` executes the production services, real charm loadout/rules and vitals on controlled native-data/hook doubles. Checks persistence serialization, moving a named player between slots and sessions, one active identity, unavailable/custom charm preferences, notch/ownership validation, no free vital/story grants, transactional rollback, conflicting/invalid keypad bindings, blocked/overkill/nested damage, pooled enemy deaths, recent assists, eight-owner isolation, healing deduplication, PvP exclusion, play-time gates, and damage snapshot exclusions/recovery.

The host does not load Unity/Mono detours, render screenshots of the actual game, emulate an audio mixer or replace physical-controller gameplay verification.
