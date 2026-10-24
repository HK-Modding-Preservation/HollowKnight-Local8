# v0.11.2-test15

- Killing a lifeseed gives one blue mask to every eligible living player
  within 12 units of the bug. The current game's ScuttlerControl death path
  now reaches sharing; it no longer sends its delayed reward only to P1.
- The first Hornet encounter gathers the party when dialogue begins and
  checks again when her native gate-closing sequence begins. A corridor
  camera trigger can no longer consume that encounter's gathering window.

Runtime 0.11.2-test15, assembly/file 0.11.1.15. Unpublished. The working Hornet
departure/ability/gate path, camera framing, enemies and controllers remain
unchanged. Automated validation passes; a complete Unity playthrough is still
required.
