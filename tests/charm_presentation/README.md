# Royal charm presentation and icon cache regressions

Run `python3 tests/charm_presentation/run.py --dotnet /path/to/dotnet`.

Executes the production CharmPresentation helper and the exact extracted
Charms.Name, Description, Icon and Reset methods with controlled Unity,
PlayerData, Language and PlayMaker objects. Checks both white-fragment sides,
Kingsoul/Void Heart progression, native FSM sprite actions, dedicated language
keys, Custom Knight generic-key avoidance, F8 scene-source fallback, missing or
destroyed scenes, late source availability, changed skins/alternate/provider
sprites, cache reuse and destruction, and localization/extraction failures.

87 checks pass. This fixture does not execute the game's serialized FSMs,
Custom Knight itself, Unity/GPU or native progression. Source/reference
inspection and in-game checks are complementary.
