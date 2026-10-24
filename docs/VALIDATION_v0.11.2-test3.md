# v0.11.2-test3 validation

Based on public v0.11.1 commit `989dbec8cc970250b15dbf18db50b55e355e0c72`
with unpublished test1/test2 corrections retained. Build target: Hollow Knight
1.5.78.11833 and matching Modding API, using .NET SDK 8.0.408.

## This update

`focus_performance` compiles the actual VanillaHud observer and extracts the
actual Hooks pool/CreateObject action bodies. Controlled callbacks verify
source scope during activation, nested source changes and exception restoration,
detached HUD burst suppression before the first camera callback, journal/world
exclusions, stored CreateObject results, pool reuse, initially hidden renderer
flags, destroyed renderers, loading and disable/restoration. Existing Focus,
background-composition, native render-budget and hierarchy regressions also run.

`menu_ui` executes the unchanged pregame panel/button in all ten languages,
including feedback switches and first-device ownership. New placement cases
cover five native icons, a later mod occupying the selected cell, randomized
obstacles, three scales, crowded rows, 4:3 and full/tiny viewports. They check
bounds, collision avoidance and preservation of native/foreign rectangles.
The existing Players texture is reused; embedded artwork is unchanged.

`title_badge` executes the real badge lifecycle against a controlled object
tree, with an incompatible gameplay HUD camera to catch wrong projection.
It checks the title canvas camera/main-camera fallback, exact shared F8 texture,
late icons, reversed Graphic corners, enable/menu visibility, crowded viewport,
no steady Sprite recreation and owned-resource teardown.

The full build compiles TitleCoopBadge against the real UIManager, Unity UI,
SpriteRenderer and canvas camera APIs. The badge owns only its clone and Sprite;
all native/foreign objects and the shared atlas texture keep their ownership.

## Prior checks and limits

Test2's controller-native, performance-presets, community-report, native-charms,
timed-respawn, usability and localization checks passed before this update;
see that build's validation for their scope. This update adds no user-facing
text requiring translation.

These are offline controlled hosts, not a Unity gameplay session. Native DLC
layout with Pale Court/SFCore, actual particle timing, physical controllers and
the reported stag round trip still need checking in Hollow Knight. The supplied
log is alpha227, so it cannot confirm a failure in the newer lobby code.
