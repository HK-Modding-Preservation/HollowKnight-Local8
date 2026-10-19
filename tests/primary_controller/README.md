# P1 controller regression host

Run `python tests/primary_controller/run.py --dotnet /path/to/dotnet`.

Executes the complete production `Controls` and `PrimaryController` classes and
extracts the actual initialization, join, reassignment and reconnect methods
from `CoopSession`. The host provides explicit native API doubles and a minimal
unrelated player-spawn boundary; it does not reimplement assignment decisions.

Covers legacy/default preference migration, explicit keyboard persistence,
active and same-name controllers, eight separate controller slots, P1 Start
without a duplicate P2, late connection, menu/loading guards, occupied-device
swaps without profile/health changes, native binding isolation, saved keyboard
bindings, missing saved devices, reconnecting P1 without stealing P2, the four
keyboard limit and action-set disposal. This is not a physical-controller or
native Unity test; those still require the game.
