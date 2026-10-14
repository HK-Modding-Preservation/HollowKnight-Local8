# v0.11.0 validation

Release source: alpha227 production code, with only `ReleaseInfo.cs` changed to `0.11.0` / `0.11.0.0`. All ten embedded resources match alpha227. Namespace, assembly name, save/settings keys and profile identities remain the same.

The DLL is compiled with the canonical Python/Roslyn path, **SDK 8.0.408**, and the provided **Hollow Knight 1.5.78.11833** Modding API-patched Managed references. The install ZIP has exactly one entry, `HollowKnightLocal8.dll`, whose bytes match the built DLL. Its SHA-256 is recorded in `release/SHA256SUMS.txt` and the Lumafly manifest.

## Controlled checks

Alpha227's unchanged production code passed interaction-control lifecycle, native charm menus, Esc/PvP/input recovery, contexts/references, event routing, world interactions, mushrooms, impact/motion, custom charms and performance-profile suites. The interaction suite reproduces the earlier borrowed inventory/bench ownership failure and checks owner retention, native bank return, replaced/dead/disconnected bodies, nested/failed callbacks and repeated menu exits.

The public tree additionally passes production named-profile/statistics checks, spell-only duel rules, the ten-language resolver and production menu callbacks/layouts across five viewport sizes. The emitted-DLL audit verifies public metadata, preserved resources/services, native hook contracts and balanced lifecycle integration.

The extracted public source package is compiled again and compared byte for byte with the delivered DLL. The ModLinks update is validated against the upstream XML schema and changes only this mod's Version, Link URL and Link SHA256.

## Limits

These automated hosts execute production code and, for world interactions, transplanted native IL with Unity/input/body doubles. They are not native Unity gameplay, a GPU benchmark, a run with eight physical controllers, or proof of every third-party scene/provider. Earlier two-to-four-player gameplay logs informed development; the final alpha227 interaction changes have controlled-test coverage and await wider native reports. No fixed FPS or complete Pale Court/provider compatibility is promised.

Report bugs with the public version, game version, player count, scene, other mods, reproduction steps and log. Private gameplay logs and user-specific diagnostics are not included in this public tree.
