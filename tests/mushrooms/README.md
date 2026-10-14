# Mushroom ownership regression fixture

Run `python3 tests/mushrooms/run.py --dotnet /path/to/dotnet` with a .NET SDK. The runner uses temporary outputs and does not require or distribute game libraries.

The actual production MushroomBounceRouting source is compiled with controlled Unity, hero, collision, hook and PlayerContext doubles. Baseline examples reproduce a stale P1 nail cache and a singleton body callback. Tests cover eight actual contacting owners, all native directional outcomes, nested/ambient contexts, exception cleanup, foreign cache edits, blocked/unknown actors, unrelated/solo contacts and symmetric hook lifecycle.

This checks callback ownership and isolation. Native scene component layouts, Unity raycast/contact timing, physics, skin animation, audio and other mods require an in-game test. No game source is embedded in the fixture.
