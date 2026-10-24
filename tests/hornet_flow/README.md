# Greenpath Hornet flow

Run `python3 tests/hornet_flow/run.py --dotnet /path/to/dotnet --managed /path/to/Managed`.

Compiles full HornetEncounterFlow. Exact native FloatCompare.OnEnter/DoCompare
IL is transplanted from Assembly-CSharp. Controlled FSM/event/scene IO models
the published position decision, including its missing-event failure.

Covers all eight intro actors, ownership despite other contacts, native
completion, identity/reset/room guards, both corpse sides/equality, grace,
working native comparisons, modified actions/thresholds, pause and no direct
progression writes. interaction_lifecycle also executes full production hook
and control-recovery methods with this feature. Full native fight, animation,
cloak and gate delivery need an in-game trial.
