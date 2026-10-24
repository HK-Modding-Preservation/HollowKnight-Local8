# New-save setup hint

Run `python3 tests/setup_hint/run.py --dotnet /path/to/dotnet`.

Executes the complete production CoopSetupHint, RescueHint, JoinHint,
Local8SaveData and localization loader. NewGameHook, Unity canvas/font/text
objects, clocks and modal/session inputs are controlled IO. Tests check old
save exclusion, native new-game duplicate events, per-save serialization,
solo and multiplayer display, configured key, all ten actual translations,
late native fonts/canvas, pause/modal/ending suppression, hint priority and
balanced event/label cleanup. This does not render a native Unity HUD.

The supplied Modding API's Mod.SceneChanged resets local settings when entering
Menu_Title. GameManager.LoadFirstScene invokes NewGameHook twice; the production
notice tolerates both and is armed only for a fresh local-save state.
