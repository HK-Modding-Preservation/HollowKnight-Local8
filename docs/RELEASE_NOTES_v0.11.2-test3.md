# v0.11.2-test3 (unpublished)

- Keep the bottom-right title configuration button, F8 shortcut help and all
  settings available before loading a save. Controllers can join the lobby;
  P1 keeps the keyboard or exact controller used to join first.
- Hide detached healing/health bursts emitted by the original HUD while the
  co-op HUD replaces it. Each affected player's own masks retain the gentle
  half-second glow/fade; this remains optional in Performance, enabled by
  default. Character Focus particles remain separate.
- Reuse the existing F8 Players two-knight icon in the title's native DLC group.
  Search for a free cell around visible menu/native/mod icons and continue
  upward when a row is full. Only the new badge moves; it hides if no cell fits.
  No SFCore dependency or new artwork is added.

The supplied 23 October log and screenshots identify `0.10.0-alpha227`, which
does not contain the title/lobby additions. Close the game, replace the actual
loaded mod DLL and check that F8 now displays `0.11.2-test3`.

Includes test1/test2's controller, Fury, stag-vote and hazard changes. The log
does not exercise stag travel; the Crossroads → Dirtmouth → Crossroads softlock
still needs a game reproduction. An inactive OptionsMenuScreen coroutine error
has no stack in the supplied log and is not attributed to this patch.

Runtime label: `0.11.2-test3`; assembly/file version: `0.11.1.3`, below the future
stable `0.11.2.0`. No GitHub release or ModLinks change has been submitted.
