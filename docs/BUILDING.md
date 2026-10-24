# Building Hollow Knight 8-Player Co-op v0.11.2

This is the complete source for v0.11.2, including the tested test1–test17 corrections. Runtime version is `0.11.2`; assembly/file/ModLinks version is `0.11.2.0`. No previous mod DLL is a build input.

## Canonical release build

Requirements: Python 3, **.NET SDK 8.0.408**, and the matching Modding API-patched **Hollow Knight 1.5.78.11833** `hollow_knight_Data/Managed` directory. Game/API assemblies are not redistributed.

```bash
python build.py --managed "YOUR_HOLLOW_KNIGHT_MANAGED_DIRECTORY"
```

Use `--dotnet` to select the SDK executable and `--output` for the DLL destination; the default is `artifacts/HollowKnightLocal8.dll`. `HK_MANAGED_DIR` can supply the reference directory. The build uses Roslyn directly with optimization, deterministic output, path mapping and no PDBs.

The reference directory must include the matching `Assembly-CSharp.dll`, `MMHOOK_Assembly-CSharp.dll`, `PlayMaker.dll`, `MMHOOK_PlayMaker.dll`, `mscorlib.dll`, Unity and framework references. Different compiler/reference versions can change the binary. An extracted source-package build is compared byte for byte with the release DLL.

## IDE project

`HollowKnightLocal8.csproj` targets net472. Copy `LocalBuildProperties_example.props` to `LocalBuildProperties.props`, set `HKManagedDir` and build Release. A .NET Framework 4.7.2 targeting pack may be needed for MSBuild. The canonical Python/Roslyn path uses the game references directly.

`src/Core/ReleaseInfo.cs` centralizes the public name/version; `Properties/AssemblyInfo.cs` consumes it. The existing assembly name, namespace, configuration/save keys and embedded-resource names are retained.

## Controlled tests

The `tests/` runners accept `--dotnet`; some also require `--managed` for reference-contract or native-IL checks. These hosts execute production methods with explicit doubles, not Unity gameplay. See [validation](VALIDATION_v0.11.2.md). Input checks include `primary_controller` and `controller_native`; 37 suites cover the documented controlled component paths.

Do not commit local reference paths, game libraries, `LocalBuildProperties.props`, `bin`, `obj`, `artifacts`, PDBs, response files or user logs.
