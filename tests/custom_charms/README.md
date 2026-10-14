# Custom charm compatibility checks

Run with a .NET SDK (8 or later):

```sh
python3 tests/custom_charms/run.py --dotnet /path/to/dotnet
```

This compiles the actual CharmGridRules, CharmCell, CharmExtensions, CharmLoadout and CoopRules files. Toggle, FirstOwned, EditorNeighbor and PvP snapshot copying are extracted from the current source, rather than copied into the test implementation. Controlled PlayerData/provider/Unity doubles let the tests execute without redistributing game DLLs. The legacy alpha193 navigation fixture is retained to check exact vanilla behaviour.

The suite covers randomized four-row navigation, fifth/sixth/partial/sparse rows, padding, registry and role-ID separation, ownership, bench/notch/overcharm constraints, independent equipment for eight players, actual toggling, provider-state restoration, custom save IDs and PvP snapshots. It does not simulate native scene FSMs, Unity rendering or third-party charm effects.


Alpha216 also executes production VoidHeartRules and the shared Charms.Toggle
body for P1–P8: acquired Void Heart cannot be manually toggled off; missing
legacy loadouts are repaired once; Kingsoul remains removable before state 4;
other charms/costs/provider isolation persist; native Pantheon charm binding,
no-charms PvP, pending clones and unacquired progression remain respected.
The combined fixture passes 162,723 checks, including 50 new Void Heart checks.
