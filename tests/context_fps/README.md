# Context and effect-reference regressions

`python tests/context_fps/run.py --dotnet /absolute/path/to/dotnet`

Executes the actual production PlayerContext, ReferenceField, CharmLoadout,
CharmExtensions, Vitals, FsmTextCache, ImpactMetrics and extracted reference
binding methods. Compares nested context traces and binding results against the
previous reflection access, using eight controlled heroes and a custom charm.

Checks immediate same-frame changes, global/input restoration, exceptions,
private/inherited/static/thread-static/volatile/readonly fields, reflection
fallback, Unicode/rename/destroyed-name parity, and sparse impact counters.

These doubles do not run Unity, Mono or the actual game. The printed .NET 8
microbenchmark measures field operations only; it is not a measured FPS gain.
