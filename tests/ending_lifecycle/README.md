# Ending lifecycle regression

Run `python3 tests/ending_lifecycle/run.py --dotnet /absolute/path/to/dotnet`.
An optional `--source /path/to/older/EndingAbsorption.cs` executes the same
regressions against the prior production class as a negative control.

The complete production EndingAbsorption class executes with controlled
Unity/PlayMaker object, material, animation, clock, session and emitter IO.
No source methods are copied into the fixture. The primary, replacement
bodies and unrelated effect artwork are checked for unintended changes.
The fixture deliberately does not expose native progression, loading,
hero-control or damage APIs to a class responsible only for cosmetics.

Cases cover 2/4/8 players, 120/20/2 FPS, both supported native entry paths,
native cancellation before/after the cosmetic climax, missing callbacks,
the full native ending wait, immediate loading/blank-room handoff, cancelled
loading, pause, scene/session/primary invalidation, unavailable/replaced
guests, excluded recipients, missing clips and partial emitter setup.
Assertions verify individual body flags, original gravity/motion where the
captured living body still owns them, animation callbacks, supported skin
colors, property blocks, emitter cleanup and native-primary isolation.

Native FSM reference: jakobhellermann/hk-fsms commit
194ebbdf54d7061aec0e704e6eda1ccc04e9fd1f,
`static/pseudocode/hk/Spell Control_Knight.txt` and
`static/pseudocode/hk/Corpse_Boss Corpse.txt`. Focus Cancel D can return via
Regain Control before Dream End is set. The corpse's Suck Pause, Sucking,
Suck Flashing, Ramp Up, Chain Break, Break, Final Suck and Stop Shake waits
alone sum to 19.45 seconds, excluding Focus Start D. Therefore the normal
ending test holds beyond twelve seconds before native loading, and the
32-second recovery fallback is exercised separately after an abandoned
sequence. It never times out a native scene loader still in progress.

These are behavioral lifecycle regressions, not a rendered ending or complete
Hollow Knight playthrough. Native FSM assets, collision dynamics and Unity
scene-loading execution still require in-game validation.
