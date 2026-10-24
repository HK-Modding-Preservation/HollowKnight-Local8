# Title badge lifecycle checks

Run `python3 tests/title_badge/run.py --dotnet YOUR_DOTNET`.
Optionally pass `--sfcore-source /path/to/DlcIconHelper.cs` to extract and run
SFCore's actual GetPositionOffsetForIndex method as the placement reference.
Without that argument, frozen expected slot positions remain available.

The production badge and placement helper run against an explicit object-tree
host. Checks cover 200 SFCore grid positions, five native slots, registered but
not yet rendered icons, Pale Court-style clones and late unregistered icons.
The oversized parent footer cannot reserve cells. Canvas/main-camera selection,
visibility, viewport exhaustion, no steady Sprite allocations and owned teardown
are covered. MenuArt supplies the same glyph; menu_ui decodes the actual atlas.
This host does not validate Unity rendering or Pale Court's serialized assets.
