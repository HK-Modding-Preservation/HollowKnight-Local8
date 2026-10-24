# v0.11.2-test8

- Players wait for a safe landing instead of appearing at an unchecked fallback.
- Spawn checks use each player's full body, including collider offsets and size
  changes. Positions inside terrain, beyond room bounds or without support are
  rejected and retried.
- Rescue returns, group teleports and arena landings also validate placement.

Retains the previous controller, keyboard, menu, HUD and room-wait fixes.
All 30 automated suites pass, including a new controlled geometry/lifecycle
suite. Actual Unity scenes, physical controllers and Steam Remote Play remain
outside the available test environment.

No UI strings added; the existing ten-language UI is retained. Local unpublished
DLL for Hollow Knight 1.5.78.11833 and its matching Modding API. Runtime
0.11.2-test8; assembly/file version 0.11.1.8.
