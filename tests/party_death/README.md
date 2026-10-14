# Party death logic checks

Requires Python 3 and a .NET SDK (8 or later). No game libraries are required for these controlled logic tests.

```sh
python3 tests/party_death/run.py --dotnet /path/to/dotnet
```

The runner compiles the actual `src/Features/PartyDeath.cs` and extracts the actual death and event hook bodies from `src/Core/Hooks.cs`. Its doubles provide controlled native objects and the current Modding API's known nested death-iterator shape. Temporary build products are kept outside the source tree and removed after the test.

These checks do not execute Unity, real native scene FSMs or other installed mods. Compile the complete mod with `build.py` against the matching Modding API references and perform the gameplay checks documented in `docs/ALPHA193.md`.
