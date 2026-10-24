# v0.11.2-test6 (unpublished)

- Protect complete native keyboard/controller reset operations, UI remapping,
  binding lookups and settings writes. Protect delayed controller UI setup and
  detach cleanup too, without retaining a changed input handler across yields.
  Native configuration stays on the original default-backed template; live
  player action sets and their selected devices stay isolated.
- Reject malformed/incomplete saved keyboard bindings and controller bindings
  in keyboard profiles. InControl can silently reset bindings when Load fails;
  explicitly validate the result and restore the previous working controls.
- Fix the extra-keyboard Reset button: prepare defaults without loading the old
  saved override, apply them to the existing action objects and persist them.
  Previously Reset could immediately load the old settings again.
- Keep test5's SFCore DLC layout, original F8 glyph, lobby/settings access,
  keyboard/controller swaps, optional health/hazard feedback and translations.
  No new user-facing strings were introduced.

Runtime `0.11.2-test6`; assembly/file version `0.11.1.6`. DLL only. No push,
GitHub release or ModLinks request was made.

Automated checks are not physical PS5/Steam Input or Unity gameplay tests. The
reported Crossroads → Dirtmouth → Crossroads stag return remains unconfirmed
by a native round trip; this build does not claim a newly proven stag fix.
