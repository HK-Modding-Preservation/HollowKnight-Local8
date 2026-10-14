# Menu UI checks

Run `python3 tests/menu_ui/run.py --dotnet /path/to/dotnet` from the source root.

The controlled IMGUI host executes the production theme, menus, keyboard binding widgets, respawn slider and arena UI. It checks all ten resource languages, five logical viewport sizes, actual menu callbacks, keyboard ownership/capture indices, add-versus-edit routing, disabled settings, native-skin restoration after exceptions, measured scrolling, the actual approved atlas, opaque button interiors, cached artwork/font discovery and complete texture teardown. Slider cases cover continuous rails at fractional coordinates and consistent partial-progress UVs; profile presentation cases cover empty/create/active/load/busy/bench/cancel/guest actions in ten languages at wide and narrow widths. Profile storage calls use controlled doubles here; `tests/player_profiles` separately executes the real persistence/statistics/audio services. `--preview-directory /path` optionally exports the actual draw calls and decoded menu textures for offline layout inspection.

The host is a fixture, not the Unity player. Text metrics are approximate; gameplay rendering, native font appearance and real controller behavior still require testing in Hollow Knight.

Foldout cases run in all ten languages at wide/narrow widths: closed/expanded abilities, localized Yes/No, conditional and positive regeneration, preserved arena preferences, frozen running-match controls, current-room duels, and visible indentation for health/SOUL and individual players. Profile storage remains separately covered by the real services and adventure panel in `tests/player_profiles`.

Alpha224 also executes the real performance controls/profile callbacks in ten languages at 200–690 logical pixels, checks translated button/caption fit, overlap and scroll extent, and exercises F8 preset/Automatic/manual/Restore actions. Native saving and per-property restoration run in the dedicated performance_presets suite.
