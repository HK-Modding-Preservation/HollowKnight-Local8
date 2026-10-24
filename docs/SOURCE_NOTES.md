# Source notes

Version **v0.11.2** contains the complete production source with test1–test17 controller, AI, transition, reward, safe-placement, hint and audio corrections. Existing namespace, public mod identity, embedded-resource IDs and save/profile identifiers are retained. `PrimaryInputVersion` records the one-time migration from the forced-keyboard default to Automatic; the existing `PrimaryDevice` setting stores Automatic, Keyboard or a particular controller key.

The DLL compiles directly from C# and embedded mod-owned assets; it does not require an older mod binary, a merger, or game files inside the source package. An extracted source-package build is checked against the release DLL using the same SDK and Managed references. The installation ZIP contains only the canonical DLL.

Recovered native coroutines retain explicit iterator state machines where needed. Their identifiers remain legal C# while preserving state, completion and disposal. Controlled tests and frozen mod-source comparisons are under `tests`; native IL used by world-interaction tests comes from the user's own Managed references at test time.

Private gameplay logs, user-specific diagnostics, local build paths, compiler output, game/Team Cherry libraries and SDK files are excluded. See [building](BUILDING.md) and [release validation](VALIDATION_v0.11.2.md).

No new license grant is introduced beyond the repository's existing terms.
