# v0.11.2-test7

- Fixed the room going black while waiting for the other players to enter.
- Other players can follow through the same door even after its native prompt
  has disappeared. Everyone entering ends the wait early; otherwise it keeps
  the five-second limit.
- Fixed the countdown expiring early if a voter disconnects after a long pause.

Retains test6's controller fixes and keyboard/controller selection. Expanded
automated coverage includes every slot's D-pad, stick, buttons, reconnects and
P1 device changes with full parties. These checks use simulated devices; a
physical-controller/Steam Remote Play session has not been run here.

No new UI strings. Existing translations, settings and artwork are retained.
Local unpublished DLL for Hollow Knight 1.5.78.11833 and its matching Modding
API. Runtime 0.11.2-test7; assembly/file version 0.11.1.7.
