# Focus, native rendering and interaction regression fixtures

Run `python tests/focus_performance/run.py --dotnet YOUR_DOTNET` from the source root. Controlled fixtures compile the production Focus guard, lighting callbacks, background synchronization, render budget, vanilla HUD and hierarchy index unchanged.

Checks cover eight actors, destroyed direct/wrapped component references, white FX, native blur-plane visibility and unmodified main-camera clipping/projection during Focus, native/custom background views, nested/interrupted passes, story/loading/death exclusions, exact restoration, graphics toggles/quality changes, bounded native render textures, no steady texture reallocations and immediate hiding of newly created damage masks. Native blur compositors remain protected in the scene pass even with a black_solid sprite, direct/wrapped Focus references, initially disabled rendering or a component added after mask discovery. The HUD fixture compares renderer visits with a full-canvas scan; randomized routing compares the cached index with the previous full-root query.

Reported CPU time, allocations, renderer visits and pixel budgets are controlled measurements, not Unity FPS or visual validation. Real serialized FSM assets and gameplay still need testing.

Test3 also covers detached native HUD feedback, first-camera discovery, world
and journal exclusions, pooled reuse, original visibility flags and all disable/
loading gates. Extracted production pool/CreateObject hooks test source scope,
nested actions, exception restoration and preserved stored results. Spell and
weaverling services are passthrough doubles in this scope fixture.

Alpha206 additionally verifies the background capture itself: foreground Focus sprites/particles and blur meshes sampling that same render texture must be excluded from its source, while remaining visible in the main pass. Cover late sprite reactivation, newly registered compositors, actual PlayerLighting callback ordering, nested render state, excluded layers, originally disabled effects, invalid near/far ranges, first-Focus hierarchy refresh and no steady hierarchy/texture recreation. These checks supplement renderer-visibility assertions; they are not native GPU pixel tests.

Alpha207 verifies the accepted alpha204 compositor bypass with retained blur: schedule the native completed background texture before sharp foreground drawing, bypass the problematic native plane meshes only while healing, and restore both the command and plane state as one transaction. Controlled command-stream pixel values distinguish a blurred texture copy from sharp scenery or a black compositor. Exercise every healing player slot with one to eight players, perspective/orthographic cameras, performance on/off, foreign command buffers, repeated healing, late source replacement/loss, nested and interrupted passes, rendering-path changes, native subscriber order and reset/disposal. Use unchanged legacy fixtures to cover the no-valid-texture fallback. These are controlled buffer/callback tests, not native Unity/GPU screenshots.

Test11 removes extra background downsampling/pass overrides from NativeRenderBudget. Assertions now require native resolution/pass count and texture identity through normal, balanced, maximum and custom profiles, external edits and reset. Bloom/noise optimizations remain. These are controlled state tests, not a GPU shimmer reproduction.
