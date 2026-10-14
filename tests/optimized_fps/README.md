# Optimization correctness fixtures

Run `python tests/optimized_fps/run.py --dotnet /path/to/dotnet`.

Compiles the production ownership resolver, hierarchy registration and cache,
the production interaction selection/request, and the light vertex projector.
The baseline resolver body is retained unchanged. Controlled fixtures compare
answers and actor side effects while reassigning pooled objects, adding a late
hero slot, cloning, destroying/replacing actors, advancing frames and exceeding
the cache bound. Randomized interaction selections compare owner, timestamps
and stag notification with the former condition order. Projection checks
compare coordinates/UVs across moving transforms/cameras, flips and 1–8 players.

API-call counters establish avoided repeated queries and unique-vertex
projection. These are meaningful controlled comparisons, not Unity FPS results.
The separate Focus and charm fixtures cover native visual transactions and
custom loadout behavior with controlled doubles.
