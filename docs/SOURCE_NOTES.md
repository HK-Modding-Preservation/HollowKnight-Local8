# Source notes

Public **v0.11.0** contains the complete alpha227-based production source. Only release version metadata changes from that private build. Existing namespace, public mod identity, embedded-resource IDs, configuration keys and save/profile identifiers are retained.

The DLL compiles directly from C# and embedded mod-owned assets; it does not require an older mod binary, a merger, or game files inside the source package. An extracted source-package build is checked against the release DLL using the same SDK and Managed references. The installation ZIP contains only the canonical DLL.

Recovered native coroutines retain explicit iterator state machines where needed. Their identifiers remain legal C# while preserving state, completion and disposal. Controlled tests and frozen mod-source comparisons are under `tests`; native IL used by world-interaction tests comes from the user's own Managed references at test time.

Private gameplay logs, user-specific diagnostics, local build paths, compiler output, game/Team Cherry libraries and SDK files are excluded. See [building](BUILDING.md) and [release validation](VALIDATION_v0.11.0.md).

No new license grant is introduced beyond the repository's existing terms.
