# Performance regression checks

Run from the source root with a .NET SDK:

```bash
python tests/performance/run.py --dotnet dotnet
```

The harness compiles the actual production reflection cache, frame buffers, light geometry, light normalization and quality setting. It extracts the actual summon ownership query and joining collision loop into controlled game doubles. Alpha196's own reflection and rim-feather implementations are retained here as reference baselines, outside the production build.

Checks cover independent/reordered/removed actors, exact array lengths, mutation-safe snapshots, rescue exclusions, the one-new-actor-per-frame budget, reflection fallbacks and same-named types from different providers, live summon pooling/ownership/room discovery, every collision pair, light-rim parity, grid continuity, normalized brightness with 2–8 players and switching decorative detail.

Allocation and search counts compare the relevant previous implementation with the updated production code after warming caches. The representative rim-math benchmark is plain C# on this test machine. None of these measurements are Unity FPS, whole-room rendering, native clone timing or a claim about the user's computer.
