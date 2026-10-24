# v0.11.2-test12 validation

Target: HK 1.5.78.11833 and matching Modding API, net472, SDK 8.0.408.
Base: public v0.11.1 plus test1–test11; no external publication.

## Enemy regression evidence

The current native-method fixture executed against frozen test11 production
source fails with: party routing discarded native HeroBox/disabled-behavior
detection. The unhooked native AlertRange/LOS bodies recognize the same contact.

Two invented restrictions were removed: rebuilding occupancy from only root
colliders excluded HeroBox-only contacts from per-actor LOS; testing sensor
MonoBehaviour.enabled ignored Unity's trigger behavior on disabled behaviours.
WorldContactRouting now recognizes root/HeroBox bodies explicitly. The shared
cached collider selection serves both party sensors and Baldur. Nail/spell/
summon colliders are excluded. Inactive objects, disabled colliders, missing
actors and existing player eligibility rules still apply.

Official Unity trigger reference inspected:
https://docs.unity3d.com/es/2020.1/ScriptReference/MonoBehaviour.OnTriggerEnter2D.html

The world suite executes 26 exact native methods, covering root-outside /
HeroBox-inside sensing for every actor in 2/4/8-player parties, disabled and
enabled sensor behaviours, native wake/LOS callbacks, sweeps, final exit and
nail-only contact. Existing Baldur native action/party-range tests remain.

## Falling vine platforms

Inspected native VinePlatformCut.Cut, VinePlatform.Land and collision behavior
from the matching game DLL. Cut makes the body dynamic; Land stops it. The
feature hooks those boundaries and tracks only cut native vine platforms.
An underside sweep handles fast descent and physics pushing the hero below
the platform before the session update. The pre-Land check handles callback
ordering where native landing precedes that update.

Each hero is charged at most once per fall via HeroController.TakeDamage with
base amount 1 and native spike hazard type 2. Existing damage hooks own blue
health, charm/overcharm and other-mod rules, immunity, feedback and fatal hits.
Survivors enter the existing hazard recovery. Pending recovery runs before
ordinary session recovery, chooses a verified companion landing and extends
the hold when no safe landing exists. Only a party without an eligible living
companion can use its own verified safe point. Recovery uses respawn:false,
preserving remaining health and SOUL. Scene resets invalidate pending work.

The spawn fixture compiles full production FallingPlatformHazard, SpawnSafety
and extracted complete Recover/Commit/checkpoint methods. Checks cover all
slots in 2/4/8 parties, repeated cut callbacks, pre-Land/update ordering,
one-charge behavior, native base-damage call ownership, blue/fatal handling,
safe companion placement, missing-floor retry, no-living-companion fallback,
stationary/side/on-top/rising cases, fast descent, pause/story/bench/vote/
protection guards and synchronous reset during damage. Native damage and
cut/land IO are explicit controlled doubles, not a native Unity playthrough.

## Scope and limits

Existing terrain penetration recovery remains a separate no-extra-damage
fallback for unrelated terrain. Camera framing/zoom, controller code and
embedded assets remain byte-identical to test11. There is no running Unity
game or hardware/controller/GPU playthrough in this environment. Automated
results cannot establish that every enemy prefab/mod combination is correct.

## Final automated pass

All 32 suites pass, including controller isolation, party rewards, room waiting,
ten-language localization and new native-method sensor / full recovery cases.
Counts are overlapping controlled checks, not unique gameplay scenarios.
A clean-checkout build and emitted-assembly lifecycle verification accompany
the DLL as internal evidence.
