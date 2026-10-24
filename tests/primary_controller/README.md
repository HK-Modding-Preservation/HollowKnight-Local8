# P1 controller regression host

Run `python tests/primary_controller/run.py --dotnet /path/to/dotnet`.

Executes the complete production `Controls` and `PrimaryController` classes and
executes the production lobby/health-feedback state and extracts the actual initialization, join, reassignment and reconnect methods
from `CoopSession`. The host provides explicit native API doubles and a minimal
unrelated player-spawn boundary; it does not reimplement assignment decisions.

Covers legacy/default preference migration, explicit keyboard persistence,
active and same-name controllers, eight separate controller slots, P1 Start
without a duplicate P2, keyboard P1 not stolen by a late pad, pregame join order,
idempotent handoff, capture/loading guards, occupied-device
swaps without profile/health changes, native binding isolation, saved keyboard
bindings, missing saved devices, reconnecting P1 without stealing P2, the four
keyboard limit and action-set disposal. This is not a physical-controller or
native Unity test; those still require the game. Health cases cover isolated
half-second mask transitions, blue masks, healing, respawn and disable/re-enable.

The host also executes the actual keypad Defaults/Bind methods. Scenarios cover
late keyboard players after several controllers, four stable layout identities,
80 occupied-device exchanges, unchanged guest action references, current-device
no-ops, actual input-handler references in guest contexts, failed preparation,
missing-pad reservation through native settings changes and duplicate-name GUID
reordering. Periodic RefreshBindings is extracted from production as well.

Test6 covers rejected malformed/mixed keyboard data, Reset without restoring
the stale saved override, persistent defaults after device switches and keeping
the same action objects. The native suite additionally executes the real Load
failure handler and production KeyboardMemory save/capture/restore methods.
