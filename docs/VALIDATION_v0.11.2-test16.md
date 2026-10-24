# v0.11.2-test16 progression review

Target: HK 1.5.78.11833, matching Modding API, net472, SDK 8.0.408.
Base: public v0.11.1 plus unpublished test1–test15. Nothing published.

## Result and limits

All 35 automated suites pass, including the new complete production
EndingAbsorption lifecycle suite (3,433 assertions). The prior test15 class
fails the new cancellation, post-climax timeout and cancelled-loader cases
after successful compilation. Its normal native-ending duration and actor
ownership cases pass, separating the reproduced regression from the fixture.

This review does not certify that every room, boss, ending or optional DLC
route is playable. No native Unity runtime, complete campaign save sequence,
rendered cutscene, real scene loader or controller hardware was executed.
The remaining campaign verification requires playing the routes in the game.

## Reproduced ending defect and correction

The old cosmetic timeout required both !climax and !handoff. Normal frames
set climax at 5.36 seconds, permanently disabling that timeout. Native focus
cancellation also had no companion cleanup. A cancelled absorption could
therefore leave living companions frozen and invisible indefinitely.

The exact captured P1 Spell Control returning through Regain Control now
releases only the captured companions. A missed cancellation callback has a
32-second scaled-time fallback even after the cosmetic climax. Native loading
or the cleared room name keeps its existing invisible handoff without a
timeout. If the loader returns to the same room, the bounded fallback applies
again. Scene/session/primary changes retain their existing reset behavior.

The native FSM reference was read from jakobhellermann/hk-fsms commit
194ebbdf54d7061aec0e704e6eda1ccc04e9fd1f: Spell Control_Knight and
Corpse_Boss Corpse. Focus Cancel D can return through Regain Control, while
the corpse's eight explicit waits total 19.45 seconds before loading, excluding
Focus Start D. Applying the old twelve-second limit unconditionally would
restore guests during a valid ending. The new normal-ending tests therefore
hold for 20.5 seconds and exercise the abandoned-sequence fallback separately.

## Coverage reviewed

The source review follows transition voting, exact door destinations, native
stag cinematic exclusions, scene entry/roster cleanup, crystal-dash transit,
arena gathering and Hornet's dialogue/departure, shared acquisitions, the
first spell chamber, Shade Cloak/Shriek and white acquisition rituals,
challenge animation cleanup, Dream Nail's intended first fall, party death,
safe recovery and the story Radiance climb checkpoint. Intentional native
story events and progression flags remain owned by the game. The Radiance
checkpoint's wait for a safe platform is preserved; it was reviewed but its
native ascent scene was not executed.

The existing suites continue to exercise complete production components and,
where documented in their individual READMEs, transplanted native method IL.
They cover room voting and fade restoration, native stag route exclusions,
per-actor interaction ownership/recovery, boss intro/gathering, enemy sensors,
ability identification, party death, body/terrain/hazard placement, falling
platform damage, SOUL/lifeseed rewards, controller mapping and isolation,
profiles, UI and all ten localization columns. Native physics, UI delivery,
coroutine timing and scene loading remain controlled boundaries.

The new suite executes the whole production EndingAbsorption class with
2/4/8 players at multiple frame rates. It checks both entry paths, native
primary isolation, the full native ending wait, immediate fade/loading,
cancelled loading, pause, disconnects, death/hazard ownership, replacement
bodies, session/scene changes, unsupported clips and partial setup failure.
Original callbacks, body flags, supported skin colors/property blocks and
emitter cleanup are checked on release.

## Artifact validation

Compilation, emitted assembly versions/resources and actual hook-to-ending
call paths are checked, followed by a clean public-base plus full-checkpoint
rebuild and byte comparison. Only EndingAbsorption and ReleaseInfo differ
from the test15 production sources. Camera framing, enemy/Baldur detection,
controllers, progression flags, other recovery methods and all embedded
artwork/translations are unchanged. There are no new dependencies or UI text.
