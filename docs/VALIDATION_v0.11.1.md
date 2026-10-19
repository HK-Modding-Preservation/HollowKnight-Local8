# v0.11.1 validation

Built from the complete source with .NET SDK 8.0.408 and the user's matching
Modding API-patched Hollow Knight Managed references. No game/API assemblies are
included in the distribution. The compiler's existing warnings remain.

Controlled regression hosts:

| Suite | Scope |
| --- | --- |
| `primary_controller` | Actual Controls/PrimaryController and session input methods: migration, eight pads, binding isolation, first Start, late connection, device swaps, profiles/health preservation, saved keyboard bindings, reconnect and action disposal. |
| `menu_ui` | Actual F8 rendering/callbacks, including P1 controls and eight device rows at narrow/wide widths in all ten languages. |
| `localization` | Real text resolver and all ten translation columns. |
| `player_profiles` | Existing profile persistence, portable keyboard bindings and ownership/statistics regressions. |
| `native_charms` | Existing native charm-card/navigation/ownership rendering host. |

The installer contains one root entry: `HollowKnightLocal8.dll`, version
`0.11.1.0` with runtime mod version `0.11.1`. An independently extracted source
package rebuild is compared byte for byte with the installer DLL.

These hosts use explicit doubles for Unity and controller devices. They do not
test Windows drivers, real simultaneous controllers, hot-plug hardware, Steam
Input or native scene/menu timing. In-game verification remains necessary;
the controlled tests do not establish full hardware compatibility.

Inspection of the matching native `InControl.InputDevice` constructor confirms
that its GUID is initialized with `Guid.NewGuid()`. The regression host therefore
uses different GUIDs when reconnecting a device, and also covers two same-name
Unity controller ports and native location keys.
