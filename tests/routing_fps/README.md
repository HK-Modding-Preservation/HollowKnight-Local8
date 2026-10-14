# alpha221 routing regression checks

Run `python tests/routing_fps/run.py --dotnet YOUR_DOTNET`. The fixture extracts production event wrapper/call structs, RunFsm, source recognition and input recovery. Frozen alpha220 bodies provide the reference. Routing, original hook delegates and error/unwinding order are compared with controlled game services; production PlayerContext/extra-charm restoration is separately covered in context_fps.

Covers eight players, nested/malformed/unmatched events, every early guard, exceptions in each scope, clone callback ordering, replaced state arrays, hero/enemy ancestry changes, native pickup rooms and renamed/new pickups, Unicode conversion, disabled and newly created input modules, foreign bindings, bounded sampled timers/window resets, one-shot spawn timers, and mixed nail/art/charm/spell ownership with identical velocity/touched bits. Fixtures count work/allocations; they do not measure native Unity/Mono FPS.
