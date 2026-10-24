# v0.11.2-test14 validation

Target: HK 1.5.78.11833, matching Modding API, net472, SDK 8.0.408.
Base: public v0.11.1 plus unpublished test1–test13. No publication.

## Supplied evidence

Local8-20261024-081429-700.log identifies itself as test12. Hornet's BG CLOSE
is observed at 08:17:14.069Z; at 08:17:14.331Z the mod identifies Hornet Boss 1,
both gates and P1/P2 already inside, so neither needs gathering. ARENA ended
four seconds later ends only the gathering window, not native combat/gates.

CONTROL recovered P1 occurs at 08:17:07.269Z before combat. Automatic encounter
dialogue has no Up/Down prompt and was outside normal interaction control
ownership. A frozen-test13 regression fixture reproduces movement resetting
that dialogue hold. The screenshot shows Hornet wounded on the floor and
closed gates. The log lacks native corpse FSM states, so its exact final
stalled state cannot be established.

Published graph provenance: jakobhellermann/hk-fsms, commit
194ebbdf54d7061aec0e704e6eda1ccc04e9fd1f, files
static/pseudocode/hk/Encounter_Hornet Infected Knight Encounter.txt,
static/pseudocode/hk/Control_Corpse Hornet 1.txt and
static/pseudocode/hk/Control_Cloak Corpse.txt. The exported corpse Check Pos
contains a FloatCompare with no output events despite existing L/R transitions.
Exact FloatCompare.OnEnter/DoCompare IL from the supplied assembly reproduces
that missing-event stall in a controlled FSM. This supports a narrow fallback;
it does not prove that the user's corpse instance reached that exact state.

P2's movement at 08:16:00.627Z follows PLATFORM head hit P2 at 08:15:59.942Z;
its later movement at 08:18:11.637Z is a timed respawn after being downed.
The log does not record P1's terrain recovery requesting a group teleport.
That possible old-code relay is nevertheless reproduced and prevented.

## Implementation and checks

Hornet intro ownership is pinned at Point, routed through existing FSM/action
hooks and held only through Set Hero Active. Start Fight, identity loss and
scene reset release it. interaction_lifecycle executes full production
StateEnter/InteractionCall/CheckStuckControl with the new feature for all eight
owners: another contact cannot steal RegainControl, movement cannot reset the
speaker, and an unrelated orphan control lock can still recover.

Corpse recovery requires Greenpath, the exact native object/FSM, confirmed
hornet1Defeated, the same Check Pos for 0.5 scaled seconds, its original two
enabled action types, X Pos against constant 27.3, and both native L/R branches.
Other actions, thresholds, objects, Godhome, pause/loading and unconfirmed
defeat are excluded. The fallback sends a side event; it writes no progression
and does not bypass native departure, animations, HORNET LEAVE, cloak or gates.
Corpse states and fallback activation now have diagnostics.

Terrain recovery validates the explicit actor's collider and dynamic simulated
body. Eight-actor tests execute full production Recover/Commit under an
unrelated PlayerContext and check only the trapped actor moves/resets, all
vitals are preserved and context restores. Trigger/kinematic/unsimulated bodies
and legitimate Hornet dialogue are excluded. Local recovery invalidates only
that actor's group-warp observation. impact_fps runs actual WorldRouting
Begin/End/ObserveFrame through nested callbacks, replacement identities and
later genuine scripted moves.

All 33 suites pass. Frozen-test13 negative controls fail for three intended
functional reasons: trigger-body terrain teleport, local recovery relayed as a
group warp and movement interrupting Hornet dialogue. Build succeeds with only
existing warnings. Assembly wiring/resources/version and clean patch rebuild
are checked.

Six production files differ from test13: CoopSession, Hooks, ReleaseInfo,
WorldRouting, HornetEncounterFlow and TerrainEmbedRecovery. Camera,
enemy/Baldur, controller, reward, platform and resource files are byte-identical.
No UI text or dependency is added. Complete Unity scene/fight, animation,
cloak/gate delivery, physics and GPU behavior still require a gameplay trial.
