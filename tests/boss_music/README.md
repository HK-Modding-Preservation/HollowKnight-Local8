# Boss music continuity

Run `python3 tests/boss_music/run.py --dotnet /path/to/dotnet --managed /path/to/Managed`.
Use `--source /path/to/old/BossMusicGuard.cs` for a negative regression run.

Executes the full production BossMusicGuard with controlled Unity audio mixers,
snapshots, sources, gameplay/party state and boss liveness. The exact native
TransitionToAudioSnapshot.OnEnter method IL is transplanted from the supplied
matching Assembly-CSharp.dll, retaining opcodes/branches/member signatures.
No native game assembly is distributed with the tests.

The native Hero Death Anim reference (hk-fsms commit
194ebbdf54d7061aec0e704e6eda1ccc04e9fd1f, hk/Hero Death Anim_Hero Death.txt)
uses both a Silent snapshot and a Normal snapshot, then fades Track 1 toward
zero. Tests therefore use distinct music/SFX mixers and a separate music
Silent snapshot instead of assuming it equals GameManager.noMusicSnapshot.

Cases cover every downed slot in 2/4/8 parties, partial and stopped-source
fades, mixer-only silence with still-playing sources, delayed manager snapshot
requests, native SFX passthrough, full defeat/native death, loading, phase cue
and clip changes, bounded recovery, pause and PvP round continuity. Native
Unity sound playback, the full death FSM assets and a live boss fight remain
unexecuted. Source/mixer IO is simulated; the actual snapshot action executes.
