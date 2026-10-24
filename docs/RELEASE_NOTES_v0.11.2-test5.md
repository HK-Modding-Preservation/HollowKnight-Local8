# v0.11.2-test5 (unpublished)

- Place the existing F8 Players glyph on SFCore's DLC grid, adjacent to the
  built-in icons. Respect SFCore reservations (including Pale Court) and late
  sibling icons. The Team Cherry footer no longer pushes the badge far away.
- Keep other keyboard players' actual action sets and default layout identities
  when P1 changes devices. Keypad defaults follow the keyboard layout, rather
  than the player's number. Saved bindings remain owned by their player slot.
- Exchange occupied keyboard/controller devices without adding players or
  changing characters, health or profiles. Prepare both replacements before
  committing either; failed preparation preserves the working assignments.
- Keep unchanged controller action sets when native settings are refreshed;
  retain the missing P1 controller reservation during keyboard fallback.
  A reattached generic device's matching GUID takes priority over list order.
- Show each player's actual input device, highlight P1's current choice and
  distinguish identical controller names with numbers. Remove the misleading
  current-device Automatic button and clarify joins/swaps in all ten languages.
- Retain test4's pregame settings, brief hazard streaks, individual optional
  mask feedback and native controller-remap isolation.

DLL only. Runtime `0.11.2-test5`; assembly/file `0.11.1.5`.
No GitHub release, push or ModLinks request was made.

The supplied test4 log records P1 on XInput and P2 on keyboard, both connected
at P2's completed spawn. It contains no physical disconnect/reconnect or stag
round trip. The reported stag return is still unconfirmed in the native game.
Offline tests do not establish PS5/Steam Input hardware compatibility or native
Unity rendering; those require playtesting.
