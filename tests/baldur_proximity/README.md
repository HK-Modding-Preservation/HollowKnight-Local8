# Elder Baldur whole-party proximity

Run `python3 tests/baldur_proximity/run.py --dotnet /path/to/dotnet --managed /path/to/Managed`.

Compiles full production ElderBaldurRange and ColliderGeometry unchanged.
Transplants exact native BoolTest.OnEnter/OnUpdate, named range action
OnEnter/OnUpdate/Apply and AlertRange.Find IL. Event dispatch and rectangle
geometry are controlled. Bounds has native 3D semantics; Physics2D ignores Z.

The negative control measures only a departing hero and produces FAR while
another hero remains nearby. Production hooks must instead refresh the whole
party's distance or X-distance and block the premature FAR event. Direct and
named range queries must both report occupancy while any eligible hero remains.

Cases cover 2/4/8 players, every possible last nearby actor, sequential exits,
stale one-shot values, final exit and return, distinct horizontal/Euclidean
metrics, reversed hero queries, unowned/owned non-hero objects, other FSMs,
non-Closed states, dead enemies, and invalid actors. Hook installation/removal
is checked as well. This verifies the existing party rule rather than changing
attack/armor mechanics or replacing it with single-player targeting.

Test11 follows published-graph Dormant/Idle/Closed decisions with distinct
alert, defense and sleep ranges. The stale child-exit negative control produces
UNALERT; production must stay awake while another actor remains inside.
Every slot must wake, close, remain closed as another leaves, open with all
outside defense range, sleep with all outside unalert, then wake again.
No actual Unity scene, full attack animation or GPU output is executed.
