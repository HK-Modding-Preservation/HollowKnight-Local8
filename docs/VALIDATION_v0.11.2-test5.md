# v0.11.2-test5 validation

Base: public v0.11.1 `989dbec8cc970250b15dbf18db50b55e355e0c72` plus the
unpublished test1–test4 fixes. Target references: Hollow Knight 1.5.78.11833
and its matching Modding API; compiler .NET SDK 8.0.408.

All 28 repository suites pass. Test outcomes remain controlled-host results,
not measurements or certification of physical controllers or Unity gameplay.

## New input checks

- 3,221 primary-controller assertions execute production Controls,
  PrimaryController, LobbyRoster and extracted session methods. They cover
  occupied/unoccupied swaps, repeated selections, eight gamepads, same-name
  ports, reconnect, saved/default keyboard controls and four stable layout IDs.
- Preparation failures preserve both assignments and release incomplete action
  sets. Guest-context input-handler references follow the replacement actions.
  Changing P1 never rebuilds an unrelated keyboard player's action set.
- The actual KeypadInput Defaults/Bind methods run in the host: a keyboard
  player joining after several pads receives its selected layout rather than
  being forced onto the numeric keypad by its player number.
- A native binding refresh retains the absent P1 pad's reservation during
  temporary keyboard fallback, and ignores keyboard-only changes for unchanged
  gamepad action sets. Reused same-name objects resolve by GUID before ordinal.
- The native-remapping suite executes the exact MapControllerButtons IL body
  imported from the game. Its negative control reproduces lost directions;
  production isolation preserves D-pad/stick press/release across every native
  layout and eight simulated pads, including subsequent periodic refreshes.
  That host reports 74,811 assertions including the primary-controller cases.

## Title and UI checks

- 324 badge assertions use the actual SFCore GetPositionOffsetForIndex source
  as the grid reference. All 200 positions match. Cases cover five reserved
  native slots, registered/unrendered sprites, Pale Court-style clones, later
  unregistered siblings, oversized parent bounds, exhausted space, camera
  fallback, visibility and owned cleanup. No foreign icon/list is mutated.
- The implementation reads SFCore reservations optionally through reflection;
  it does not register another sprite in SFCore's append-only collection or
  introduce a mandatory SFCore assembly dependency.
- 107,178 menu checks cover ten languages and five resolutions. Actual device
  selection is highlighted; device ownership remains visible on player cards.
  The confusing Automatic action is absent from current-device selection.
- All ten language columns include the clarified join and keyboard-swap help.
  The localization resolver suite reports 14,306 checks.

Primary reference sources:

- https://github.com/SFGrenade/SFCore/blob/master/src/DlcIconHelper.cs
- https://github.com/PaleCourt/PaleCourt/blob/master/FiveKnights.cs

## Evidence and limits

The supplied test4 log records P1 = XInput Controller and P2 = keyboard at
spawn-ready, both connected. It contains no real disconnect/reconnect or scene
travel. The screenshot is from test3 and shows the earlier excessive badge gap.
An inactive OptionsMenuScreen coroutine error has no stack; shutdown also logs
missing GameManager. Neither is assigned a speculative controller fix.

No native Unity session, physical PS5/Bluetooth/cable/Steam Input matrix,
Pale Court rendering session or real streaming session was available. The
Crossroads → Dirtmouth → Crossroads stag softlock remains unconfirmed by an
actual round trip. The DLL cannot promise universal third-party compatibility.
No GitHub push/release, ModLinks request or external publication was performed.
