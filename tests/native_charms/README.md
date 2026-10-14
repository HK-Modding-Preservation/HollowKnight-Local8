# Native charm-menu regressions

Run `python3 tests/native_charms/run.py --dotnet /path/to/dotnet`.
Optional `--preview-directory` records actual production draw commands and
feather alpha samples. Preview charm art is placeholder art.

Executes production CharmCardUI, RoleUI, visibility/diagnostics, cursor drawing,
EquippedCharmCursor, CharmCatalogTint and extracted native HUD/ProbeNative/
NativeActive/RoleSystem.NativeInput/Prime/MoveDue/return-focus methods. Uses the
real CharmGridRules and extracted Charms.Neighbor with native/mod board shapes.
Routing tests use a controlled removal endpoint to record the exact owner/ID;
the separate custom_charms suite runs the unchanged real Charms.Toggle backend,
provider/context/persistence behavior and Void Heart restrictions.

12,758 checks pass: previous layout/focus/suppression/feather regressions plus
independent P1–P8 equipped focus and removal, held-submit protection, slot removal,
provider updates, empty cards, actual columns/partial rows, native/mod fifth-row
navigation, sparse IDs up to 39001, role transitions, duplicate cursor avoidance,
actual icon corner placement and camera-scoped tint/opacity/restoration. Known
flash/effect layers keep their authored color/alpha; explicit sibling icon links
and provider failure/exit/missed post-render/shutdown are covered.

`--source /path/to/alpha216/source` fails 63 updated input/render requirements.
Older source comparisons borrow missing helper types/methods from this fixture
while keeping the old production method bodies unchanged. The direct new-helper
tint checks do not establish the old build's in-game tint behavior; the supplied
screenshot and code review establish that rendering requirement.

This fixture does not execute Unity, serialized game FSMs, GPU, real controllers
or every third-party mod. Native checks remain required.
