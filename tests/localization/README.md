# Ten-language resource dispatch

Run `python tests/localization/run.py --dotnet YOUR_DOTNET`. The fixture extracts the complete production `Charms.SanitizeLocalized` method and compiles it with the unchanged `RoleText`/`UiLocalization` classes and actual embedded translation resources. Every UI key is checked against all ten columns, including language changes during a session, legacy fallback, native-text isolation and role descriptions. Game libraries are not redistributed.
