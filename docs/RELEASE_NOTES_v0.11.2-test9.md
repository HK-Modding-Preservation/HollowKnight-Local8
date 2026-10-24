# v0.11.2-test9

- Soul-totem orbs, soul and collection flash follow the player who hit the totem,
  including delayed collection and reused orbs.
- Killing a lifeseed gives one blue mask to every eligible living player within
  12 world units of the bug. Repeated event delivery cannot duplicate it.
- Enemy alert ranges recover missing contacts. A nearby hero behind a wall no
  longer hides another visible hero from the native sight check.
- Completed room entries recover accidentally suspended gravity without
  cancelling native dash, spell or conveyor states.
- Persistent deep terrain embedding, including after a falling platform, sends
  the player to a verified safe point without another mask penalty.

Camera behavior and native enemy attack, proximity and armor rules are retained.
No UI strings added; the ten-language UI and prior fixes are retained.
Local unpublished DLL for Hollow Knight 1.5.78.11833 and matching Modding API.
Runtime 0.11.2-test9; assembly/file version 0.11.1.9.
