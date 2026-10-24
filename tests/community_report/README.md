# Community report regressions

Run `python3 tests/community_report/run.py --dotnet /path/to/dotnet`.

Runs the production FuryRecovery, HazardFeedback and StagTravelRules modules,
the actual hazard coroutine hook/session method and the actual direct/gate
transition-voting methods. The old direct-voting method is retained only as
an explicit negative control from v0.11.1.

Checks cover individual Fury death/respawn cleanup, valid one-mask Fury on the
other player, idle-FSM orphan flashes, unrelated flashes, replacement heroes,
native spike/acid prefab selection and direction/scale, acid immunity, pit
fallback, duplicate suppression, fatal hazards, shared stag departure/arrival,
repeated trips, route-object preservation and ordinary room voting.

FSM actions, prefabs, game data, scene activation and voting eligibility use
controlled doubles. The negative control demonstrates unsafe deferred stag
departure; it does not reproduce the full reported station soft lock in Unity.
Actual station travel, visual/audio timing and physical controllers must be
checked in game.
