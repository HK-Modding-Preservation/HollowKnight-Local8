# v0.11.2-test11

- Enemy sensors use 2D geometry, so visual Z offsets cannot hide nearby heroes.
- Baldur's separate sleep detector is refreshed from the party before its
  native boolean decision. An exit cannot make it sleep while another player
  remains inside. The Closed-state FAR guard supports the actual BoolTest
  action and still permits opening once everybody leaves defense range.
- Performance profiles retain native background texture resolution and blur
  passes. The removed extra downsampling could make moving scenery shimmer.
  Native video preferences, bloom/noise and particle optimizations remain.

Camera framing/zoom, native enemy attack/armor actions, controls and translated
UI are unchanged. Runtime 0.11.2-test11, assembly/file version 0.11.1.11.
Unpublished DLL; real Unity encounters and moving GPU imagery remain untested.
