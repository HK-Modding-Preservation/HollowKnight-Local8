# v0.11.2-test17 validation

Target: HK 1.5.78.11833, matching Modding API, net472, SDK 8.0.408.
Base: public v0.11.1 plus unpublished test1–test16. Nothing published.

## Evidence and scope

The supplied Local8-20261024-124149-582.log identifies itself as test15. It
shows the new-game route Opening_Sequence -> Tutorial_01. At 12:46:19.720Z the
native music cue changes from Greenpath to BossHornet. P2 goes down at
12:46:36.655Z while P1 continues fighting. Further individual and eventual
full-party deaths follow. The log has no LIFEBLOOD sharing messages, so it
does not establish which exact native reward callback ran or what P1's blue
health was. The supplied screenshot shows blue masks on P2, without matching
extra masks on P1. No exact runtime cause is inferred from absent diagnostics.

The previous lifeseed class required Heal to run within a thread-local Hit
scope. A regression fixture now invokes that native callback after Hit
returns; the previous class then misses P1 and nearby guests. The replacement
captures the lifeseed lifetime, session and scene before native Hit instead.
Native activation, accepted attack types, journal updates and initial death
effects retain their original code. The private Heal call on that dead
lifeseed shares once for the captured lifetime, even after Hit unwinds.
Repeated Heal entries cannot grant again. Native delayed global healing is
still replaced to avoid an additional P1-only reward. Pool/reset identity
protects another lifetime. Radius and recipient eligibility are unchanged.

The native Hero Death Anim reference (hk-fsms commit
194ebbdf54d7061aec0e704e6eda1ccc04e9fd1f) uses a separate Silent snapshot and
a gradual Track 1 fade. The music guard previously recognized only the
manager's no-music snapshot, repaired only an exactly zero source volume,
and did not reassert its cue snapshot when sources remained playing.
Individual-death music snapshots now match the actual cue's mixer by object
reference; death SFX mixers continue normally. The bounded recovery preserves
the healthy source volume through a gradual fade and restores the cue's own
snapshot when a participant drops. Native full defeat, loading, phase/cue
replacement and other-story snapshots retain their existing behavior.

## One-time new-save notice

NewGameHook arms a field in Local8SaveData only for a fresh save. The actual
Modding API resets local data on Menu_Title, and its LoadFirstScene can invoke
the new-game event twice; the notice is idempotent. Existing saves default
to the unarmed state. Display marks the save as shown and serialization
retains that state. Loading the save, changing rooms or resetting temporary
UI cannot re-arm it.

The label uses the rescue hint's native Trajan/Perpetua selection, 25-point
centered text, bottom HUD anchors, warm color and 15-second fade. It displays
the configured panel key (F8 by default). The new text exists in ES, EN, FR,
DE, IT, PT, RU, ZH, JA and KO. Native pause, loading, dialogues, pickups and
other modal UI defer display. Existing rescue/join hints wait their turn.

## Executed checks

All 37 suites pass. setup_hint executes the complete new hint, original rescue
and join hints, real save class and localization loader (63 assertions).
boss_music executes the complete production guard with the exact native
TransitionToAudioSnapshot.OnEnter IL (118 assertions), covering every downed
slot in 2/4/8 parties, independent music/SFX mixers, gradual/stopped/mixer-only
silence, manager requests, full defeat, loading, phase changes and teardown.

party_rewards now executes the full production PlayerContext in addition to
the native ScuttlerControl.Hit/Heal, SoulOrb and Fling method/iterator IL.
Its 1,947 checks include deferred Heal for every hitter in 2/4/8 parties,
nearby P1/guest grants, duplicate suppression, pooling and existing SOUL
rewards. Unity singleton/input, PlayerData field IO and coroutine scheduling
remain controlled. Both previous production classes compile but fail their
new functional negative controls for the intended reward/music reasons.

EndingAbsorption and EndingEssenceFx are byte-identical source files to
test16. The existing 3,433 ending lifecycle assertions pass, including guest
focus, progressive transparency, cosmetic-stream updates, native scene-load
handoff and cancelled-ending recovery. Native rendering was not executed.
Camera framing, enemy/Baldur detection and controller code are unchanged.

The final DLL is checked for version/resources and actual hook call paths,
then rebuilt from the public base plus the complete source checkpoint and
compared byte for byte. No new mandatory dependency is introduced. Native
Unity gameplay/audio/rendering, physical controllers and full campaign
completion remain unexecuted; automated component tests do not certify them.
