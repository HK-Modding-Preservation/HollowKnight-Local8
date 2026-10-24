# v0.11.2-test12

- Cut vine platforms detect a descending underside crossing a player's head.
  Apply one native hazard hit with base damage 1 and recover the surviving
  victim beside a living teammate on validated ground. A fatal hit remains a
  normal down; a stationary platform, riding it or touching its side is safe.
- Pending recovery waits for a clear landing without charging more damage.
  If no living companion remains, use a validated previous safe point.
- Enemy sensing includes the native HeroBox as well as root body colliders.
  Owned nail/spell/summon objects do not become player bodies.
- A disabled sensor MonoBehaviour no longer suppresses its still-valid native
  trigger behavior. Actual collider disabling/object deactivation still matters.

Camera framing, enemy attack/armor rules, controls, resources and ten-language
UI assets are unchanged from test11. Native damage still respects game/mod
damage rules, blue masks, immunity and fatal-hit behavior. Runtime test12,
assembly/file 0.11.1.12. Unpublished DLL; real Unity playtesting remains needed.
