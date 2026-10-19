# Hollow Knight 8-Player Co-op v0.11.1

P1 can now use a controller automatically: **2–8 players can play entirely on controllers, with no mandatory keyboard player.**

- Connect controllers before loading a save. P1 uses the active controller or the first connected one; Start on the remaining controllers joins P2–P8.
- P1 controls are at the top of **F8 → Players & controls**. Choose **Automatic**, **Keyboard**, or **Assign P1** beside a specific controller. The choice is saved.
- Assigning an occupied controller exchanges control devices while characters, profiles, health and progression keep their slots.
- If no controller was connected at load, the first controller's Start assigns it to solo P1; subsequent controllers join normally.
- Reconnecting P1 restores controller bindings without taking another player's controller. A saved controller missing at load can reconnect after temporary keyboard fallback.
- Remember controllers using their available port/location, rather than InControl's per-object random GUID. Older saved assignments are migrated where their name/ordinal can be resolved.
- The old forced-keyboard default migrates to Automatic once. Keyboard remains available and a new explicit Keyboard selection is retained.
- P1 control descriptions/help are available in all ten supported languages, with compact controller rows for narrow menus.

Target: **Hollow Knight 1.5.78.11833** with its matching **Modding API**. Replace the old DLL with `HollowKnightLocal8.dll` from **HollowKnightLocal8-v0.11.1.zip**. The ZIP contains just that DLL; artwork and translations are embedded. Existing saves and profiles are retained.

Compilation and controlled production-code input, menu, language, profile and charm regressions passed. Input tests cover eight simulated devices, swapping, saved keyboard bindings and reconnection. **Physical controllers and native Unity gameplay have not been tested for this patch.** The source-package rebuild matches the release DLL byte for byte.

The Lumafly catalog follows ModLinks; v0.11.1 appears there after the update request is merged.

(AI-assisted)
