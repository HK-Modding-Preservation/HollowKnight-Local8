# v0.11.2-test11 validation

Base: public v0.11.1 plus unpublished test1–test10. Target: HK 1.5.78.11833,
matching Modding API, net472, SDK 8.0.408.

## Reproduced detection failures

Native Bounds.Intersects checks X/Y/Z; Physics2D distance ignores rendering Z.
Both production sensor implementations used the 3D bounds test as a prefilter.
Previous fixtures incorrectly modeled Bounds.Intersects as XY-only, so their
533 Baldur checks could not expose this problem. Correcting the fixture makes
test10 fail both Baldur occupancy and native-method world sensor tests.
Production now retains an XY prefilter plus collider-distance geometry without
moving any transform or altering a collider.

The previous Baldur follow-up also focused on distance comparisons rather than
the actual action graph. Four published game FSM exports show separate Alert
Range New, Attack Range and Unalert Range sensors, using BoolTest decisions.
The hero_detect_bool child writes Unalert Range on individual contacts. A
native BoolTest negative control reproduces premature UNALERT after one exit.
Production refreshes only the existing local proximity booleans from their own
sensor geometry before native decisions. FAR guarding includes the bool path.
No attack event, invulnerability action, animation or forced state is added.

## Evidence and limitations

The 869-check Baldur suite executes exact native IL for BoolTest.OnEnter and
OnUpdate, CheckAlertRangeByName.OnEnter/OnUpdate/Apply and AlertRange.Find.
The Mono.Cecil transplant checks opcodes, branches and locals. Decision
sequences follow the exported Dormant/Idle/Closed graph for 2/4/8 actors and
each possible last nearby actor: wake, close, another actor exits, last actor
exits, open, sleep beyond the outer range and wake again. Sensor/body Z values
differ explicitly. Physics, animation and full event dispatch remain controlled
dependencies; no actual scene or native Unity encounter is executed.

The world suite retains 26 exact native method bodies with correct 3D bounds
and separate 2D contact semantics. Its existing sensor scenario now separates
sensor/body Z. Prior native LOS/Walker, rewards, input, placement and transition
regressions remain applicable.

The old render budget halved or quartered background texture height and
overrode its blur passes. This can cause sampling shimmer; the still images
do not prove that it explains the user's entire visual issue. The budget no
longer writes those properties or reallocates that texture. Controlled tests
cover normal/balanced/maximum/custom modes, native edits, texture identity,
healing composition and restoration. Native GPU imagery is unavailable here.
Preserving native background quality costs more GPU work than the removed
optimization; other performance controls remain.

Camera framing/zoom, input and embedded resources are unchanged from test10.
No publication or external repository mutation occurred. Passing automated
checks does not guarantee a bug-free playthrough.

## Graph provenance

Inspected through GitHub: jakobhellermann/hk-fsms at commit
194ebbdf54d7061aec0e704e6eda1ccc04e9fd1f:

- static/pseudocode/hk/Blocker Control_Blocker_Battle Scene__Enemies.txt
  (Crossroads_11_alt, the route shown in the user's image).
- static/pseudocode/hk/Blocker Control_Blocker_Battle Scene.txt
  (Crossroads_ShamanTemple).
- static/pseudocode/hk/Blocker Control_Blocker 1.txt and Blocker 2.txt
  (Fungus1_28).
- static/pseudocode/hk/FSM_Unalert Range_Blocker 2.txt
  (shared hero_detect_bool child).

Native action bodies and Bounds/LightBlurredBackground/LightBlur/BlurManager
were also inspected directly from the provided matching game assemblies.
