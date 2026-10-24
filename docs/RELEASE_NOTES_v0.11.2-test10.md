# v0.11.2-test10

- Enemy sight reacts to an alert-range enter/exit even when an earlier query
  already ran during the same frame.
- Hidden players waiting at a room exit no longer satisfy a Walker's native
  activation condition or become its movement target.
- Enemy sight subscriptions are cleared when contact routing is uninstalled.

Retains test9 soul-totem ownership/visuals, nearby lifeseed sharing, entry
gravity and terrain recovery. Camera code, native enemy attack/defense rules,
controller/keyboard code and the ten-language UI are unchanged.

Regression tests now execute complete production Recover/Commit/checkpoint
methods, including preservation of masks/soul, restored body/input and retry
when no safe landing exists. Real Unity encounters and hardware remain outside
the available test environment. Local unpublished DLL; runtime 0.11.2-test10,
assembly/file version 0.11.1.10.
