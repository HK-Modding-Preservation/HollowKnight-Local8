# v0.11.2-test1

- Keep P1's controller directions when another controller joins or becomes active.
- Clear stale Fury effects on the affected player after co-op death/respawn.
- Keep shared stag journeys out of room-exit voting.
- Restore the native spike/acid impact effects for individual hazard recovery.

Test build for the community report, based on v0.11.1. Close the game and replace
the installed mod's DLL with `HollowKnightLocal8.dll` from the test ZIP.

Please check two PS5 controllers, Fury after death/respawn, and the stag round
trip between Forgotten Crossroads and Dirtmouth. Automated tests passed; real
controllers and Unity scene/visual timing remain untested here.

The runtime label is `0.11.2-test1`; assembly/file version is `0.11.1.1` so a
future public `0.11.2.0` remains a newer installer version.
