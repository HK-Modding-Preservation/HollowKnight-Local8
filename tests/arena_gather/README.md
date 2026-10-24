# Native encounter gathering

Run `python3 tests/arena_gather/run.py --dotnet /path/to/dotnet`.

Compiles the complete production ArenaGather/HornetEncounterFlow and actual
safe-placement geometry, with controlled rectangle physics and an explicit
coroutine scheduler. Session IsSafe/InRoom/FindSafePosition/Commit are extracted
from current production source on every run.

Reproduces the supplied test14 corridor and split-party positions, then tests
all intro owners in 2/4/8 parties, open gate shapes, supported landing, preserved
vitals/control owner, long dialogue and another gathering at closure. Valid
boss locks, no available floor and scene cancellation are separate cases.
This does not execute Unity scene assets, dialogue, boss AI or GPU rendering.
