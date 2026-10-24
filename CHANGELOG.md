# Changelog

Public changes to Hollow Knight 8-Player Co-op.

## v0.11.2 - 2026-10-24

- Correct shared controller configuration resets, P1 movement/input isolation, controller/keyboard selection and lobby joining.
- Add title-screen co-op settings, a compatible DLC-row badge and a one-time translated setup-key hint for new saves.
- Repair party enemy sensors/Baldur proximity without overriding the native attack and armor rules.
- Preserve native stag/dream routes and restore visible room waiting, safe body placement, local stuck recovery and falling-platform hazard handling.
- Gather the party for Hornet's first dialogue and gate closure, and preserve native departure, dash acquisition and ending progression.
- Share SOUL totem and Lifeblood rewards with nearby eligible actors; handle deferred native lifeseed healing without duplicated grants.
- Add optional subtle hazard/HUD health feedback, recover stale Fury visuals and keep fight music through individual deaths.
- Preserve the final companion focus/dissolve/essence animation and restore captured companions after a cancelled or abandoned ending.
- Include 37 controlled regression suites, exact native-method checks where documented and deterministic release reconstruction. Native campaign/hardware validation remains separate.

## v0.11.1 - 2026-10-19

- Default P1 to Automatic controller selection instead of forcing keyboard. Prefer the active/first attached controller and allow 2–8 controller-only players.
- Migrate the old keyboard default once while retaining explicit saved controller identities; new manual Keyboard choices remain persistent.
- Promote P1 controls to the top of F8 and explain Automatic/Keyboard/Assign P1 in all ten languages, including compact controller rows.
- Assign an occupied controller to P1 by swapping control devices without moving characters, health or profiles; preserve saved extra-keyboard bindings.
- Rebuild P1 controller actions after reconnect, including a saved pad absent at initial load; do not steal another player's controller or leave the native input handler pointing at disposed actions.
- Use controller port/location keys for persistent assignment instead of InControl's random object GUID, with legacy name/ordinal migration.
- Claim a late first controller for solo P1 with Start; reserve subsequent controllers for P2–P8 and respect menu/loading guards.
- Add controlled production-input and UI regression coverage. Native hardware validation remains pending.

## v0.11.0 - 2026-10-14

- Add saved named player profiles and portable adventure statistics, with validated equipment/control loading and duplicate-identity protection.
- Add spells-only duels with individual 1–99 SOUL/second regeneration and safer jump/fall/bench entry and return.
- Redesign F8, profile controls, sliders and translated hover help; improve independent equipped-charm navigation, provider icons/descriptions, royal charm presentation and permanent Void Heart.
- Expand optional custom-charm registration/navigation/equipment/save support and experimental Pale Court clone/Bloom/Boon handling.
- Add automatic Normal/Balanced/Maximum performance presets for 1–2/3–4/5–8 players, manual customization, native particle/blur controls and original-setting restoration.
- Reduce repeated context/reflection/hierarchy/PlayMaker searches and optional damage particles/audio; clean orphaned native jump effects.
- Improve shared light presentation, healing/background compositing, cooperative acquisition effects and extra SOUL reserve bars.
- Correct world ownership for mushroom bounces, metal recoil, steep slopes, shade-gate collision exemptions and crystal-dash destructibles. Preserve vertical-conveyor gravity and coherent party sensors/Walker targets.
- Improve whole-party death authorization, enemy retargeting, familiar lifetime, native walking areas and crystal-dash cleanup/continuity.
- Correct shared-roar recoil/gravity/drift and improve interaction closing ownership, charm-panel visibility, Escape/F8 and stale-control recovery.
- Publish the complete alpha227-based source with 0.11.0/0.11.0.0 metadata, reproducible build instructions and validation limits.

## v0.10.0 - 2026-10-07

- Added nine persistent role choices, including no role, independent role selection and bench/charm-menu changes. Refined healer ally healing, mage SOUL sharing, Shaman damage risk and per-player summon bonuses.
- Added configurable trips to boss-free vanilla Godhome PvP arenas, including Markoth with a floor. Custom health/SOUL, jumps, dashes, abilities and match rules restore on return. Charms are enabled by default; Lifeblood and familiar PvP behavior have dedicated handling.
- Improved keyboard/controller isolation, physical numeric-keypad detection, saved extra-keyboard bindings, owned vibration, held jumps and stale menu-input recovery. Added a localized shared-keyboard limitation warning.
- Corrected reversed door destinations and strengthened room-entry/return recovery, safe landings, repeated bench saves and pickup/challenge completion. Added native bench boarding/dismounting and individual challenge draw sounds.
- Improved extra-player water/acid behavior, Isma surface handling, swim presentation and splash suppression. SOUL baths refill all valid occupants without requiring reentry; pooled SOUL spells retain their caster/facing and have a bounded lifetime.
- Refined smooth camera framing, visible-player anchoring, physical-player priority during dream rescue, contextual rescue prompts, bounded essence movement and smooth arrival. Tuned fixed Godhome views for compact and wide arenas.
- Protected ongoing boss/PvP music from local death effects, improved arena lighting/fade recovery, and prevented repeated boss-hit gathering. Added safer arena placements and return handling.
- Individual deaths preserve shared geo/SOUL capacity; whole-party defeat retains native Shade recovery and a visibly broken vessel. Refined recovery poses, full-screen fades and the final Radiance ascent checkpoint.
- Added stronger linked crystal-dash damage with restrained effects and room-to-room flight continuity; restored cooperative ending absorption and extra-player dark-ability rituals with dream essence and native acquisition routing.
- Expanded ten-language UI coverage, charm/role details, PvP countdown/results and finite-round timers. Unlimited round time is the default; timed rounds resolve by remaining health. Added a subtle Delicate Flower aura.
- Unified public branding and all DLL version metadata. The complete source now compiles directly without an older mod binary and reproduces the distributed DLL.

## v0.9.0 - 2026-10-02

First public release of Hollow Knight 8-Player Co-op.

Main features include:

- 2–8 local players in one shared Hollow Knight game.
- Shared enemies, bosses, world and progression.
- Independent health, Soul, controls, HUD and charms.
- Controller and multi-keyboard support.
- Adaptive cooperative camera and group transitions.
- Revive, respawn and rescue systems.
- Co-op, Friendly Fire and Round Duels.
- Difficulty scaling for multiple players.
- Custom Knight integration.
- Cooperative lighting and visibility options.
- Multi-language interface.
- Experimental compatibility work with larger content mods such as Pale Court.

The v0.9.0 release candidate is based on internal build 139.

## Versioning

Public tags use `v0.11.0`; Modding API/GetVersion uses `0.11.0`; assembly, file and ModLinks versions use `0.11.0.0`. Development alpha labels are separate from public-release metadata.
