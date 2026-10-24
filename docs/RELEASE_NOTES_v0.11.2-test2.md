# v0.11.2-test2 (unpublished)

- Set up the group/settings before loading a save, using the bottom-right co-op button or F8. The button shows the configured panel shortcut.
- Join controllers with Start in the lobby, including while the panel is open. The first joined device belongs to P1; later controllers never claim keyboard P1.
- Keep the exact lobby device order through loading and reconnection. Eight controller slots remain available.
- Masks fade when lost and glow gently when gained, only on the affected player's HUD. Includes blue masks and a light transition for the basic HUD; removes the full-HUD shake/red flash.
- Performance has separate hazard and HUD health feedback switches, both on by default. They are saved and remain independent of the presets. Automatic profiles also count the pregame group.

Includes test1's controller remapping, Fury cleanup, stag-vote exclusion and
native hazard feedback changes. The stag round trip still needs confirmation
inside Hollow Knight.

Close the game and replace the existing DLL with the one in the test ZIP.
Runtime label: `0.11.2-test2`; assembly/file version: `0.11.1.2`, below the future
stable `0.11.2.0`. No GitHub release or ModLinks change has been submitted.
