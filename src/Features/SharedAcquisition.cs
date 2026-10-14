using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Scenes = UnityEngine.SceneManagement.SceneManager;

namespace KO.HollowKnight8;

// The collector retains the one native transaction, card, save write and
// scene transition. Streams/short poses stay cosmetic; eligible white soul
// acquisitions delegate their bounded follower suspension to the ritual.
internal static class SharedAcquisition
{
    private sealed class Identity { internal string Key; internal int Value; internal GameObject Pickup; }
    private sealed class Actor
    {
        internal PlayerSlot Player; internal HeroController Hero;
        internal AcquisitionParticles Stream, Burst;
        internal bool Arrived, Posing; internal float PoseUntil;
        internal bool WhiteRitual;
        internal tk2dSpriteAnimator Animator; internal tk2dSpriteAnimationClip Pose;
        internal Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip> Completed;
        internal Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int> Triggered;
    }
    private sealed class Transfer
    {
        internal PlayerSlot Collector; internal HeroController Hero; internal string Id;
        internal float At; internal int Scene;
        internal readonly List<Actor> Actors = new List<Actor>();
    }
    private static readonly List<Transfer> transfers = new List<Transfer>();
    private static readonly Dictionary<Fsm, Identity> sources = new Dictionary<Fsm, Identity>();
    private static readonly Dictionary<Fsm, PlayerSlot> touching = new Dictionary<Fsm, PlayerSlot>();
    private sealed class Miss { internal FsmState[] States; internal float Until; }
    private static readonly Dictionary<Fsm, Miss> misses = new Dictionary<Fsm, Miss>();
    private static readonly Dictionary<string, int> levels = new Dictionary<string, int>();
    private static readonly AcquisitionLatch latch = new AcquisitionLatch();
    private static readonly List<SpellGetOrb> orbs = new List<SpellGetOrb>();
    private static PlayerSlot pending;
    private static Fsm pendingSource;
    private static GameObject pendingPickup;
    private static string pendingKey;
    private static float pendingAt;
    private static CoopSession session;
    private static PlayerData data;
    internal static bool Replaying;
    internal static Fsm AnimationSource;

    // These are the actual vanilla pickup objects, not the NPC which spawns
    // one. In particular, the shaman's soul delegates progress to a different
    // FSM (Check Fall); its contact FSM need not write fireballLevel at all.
    private static Identity NativePickup(GameObject go)
    {
        string scene = Scenes.GetActiveScene().name;
        string key = scene == "Crossroads_ShamanTemple" ? "fireballLevel" :
            scene == "Ruins1_24" ? "quakeLevel" : scene == "Room_Fungus_Shaman" ? "screamLevel" :
            scene == "Waterways_13" ? "hasAcidArmour" : null;
        if (key == null || !go || go.GetComponentInParent<HeroController>() || go.GetComponentInParent<HealthManager>()) return null;
        for (var at = go.transform; at; at = at.parent)
        {
            string name = at.name;
            if (name != null && name.EndsWith("(Clone)", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 7).TrimEnd();
            if (key == "fireballLevel" && name == "Vengeful Spirit" ||
                key == "quakeLevel" && (name == "Quake Item" || name == "Quake Pickup") ||
                key == "screamLevel" && name == "Scream Item" || key == "hasAcidArmour" && name == "Shiny Item Acid")
                return new Identity { Key = key, Value = 1, Pickup = at.gameObject };
        }
        return null;
    }

    private static bool Magical(string clip) => clip != null && clip.StartsWith("Collect Magical", StringComparison.Ordinal);

    private static Identity AnimationKind(Fsm f, string clip)
    {
        string key = ClipKey(clip);
        if (key != null) return new Identity { Key = key, Value = 1 };
        if (!Magical(clip)) return null;
        var native = NativePickup(f?.GameObject);
        if (native != null) return native;
        var known = Kind(f);
        if (known != null) return known;
        // Native initial-spell acquisition uses Collect Magical, not a clip
        // named Fireball Get. The actual animated actor is required by callers.
        // These rooms contain the initial spell; shadow upgrade rooms differ.
        string scene = Scenes.GetActiveScene().name;
        key = scene == "Crossroads_ShamanTemple" ? "fireballLevel" :
            scene == "Ruins1_24" ? "quakeLevel" : scene == "Room_Fungus_Shaman" ? "screamLevel" : null;
        return key == null ? null : new Identity { Key = key, Value = 1 };
    }

    private static Identity Kind(Fsm f)
    {
        if (f == null || !f.GameObject) return null;
        if (sources.TryGetValue(f, out var known)) return known;
        // Normal hero Spell Control contains unused get branches too.
        if (f.GameObject.GetComponentInParent<HeroController>()) return null;
        var native = NativePickup(f.GameObject);
        if (native != null) { sources[f] = native; misses.Remove(f); return native; }
        if (misses.TryGetValue(f, out var miss) && miss.States == f.States && Time.unscaledTime < miss.Until) return null;
        if (f.States != null) foreach (var state in f.States)
        {
            if (state == null || state.Actions == null) continue;
            foreach (var action in state.Actions)
            {
                Identity found = null;
                if (action is SetPlayerDataBool flag && flag.boolName != null && flag.value != null && flag.value.Value && AcquisitionRules.Flag(flag.boolName.Value))
                    found = new Identity { Key = flag.boolName.Value, Value = 1 };
                if (action is SetPlayerDataInt level && level.intName != null && level.value != null && AcquisitionRules.Level(level.intName.Value))
                    found = new Identity { Key = level.intName.Value, Value = level.value.Value };
                if (found != null && (AcquisitionRules.Flag(found.Key) || found.Value >= 1 && found.Value <= 2)) { sources[f] = found; misses.Remove(f); return found; }
            }
        }
        if (f.States != null && f.States.Length > 0) misses[f] = new Miss { States = f.States, Until = Time.unscaledTime + .5f };
        return null;
    }

    internal static void ObserveCall(Fsm f, PlayerSlot p, string method)
    {
        if ((method != "RelinquishControl" && method != "StopAnimationControl") || !Valid(p)) return;
        try
        {
            var s = Current;
            if (s == null || !s.Active || s.Data == null) return;
            EnsureSession(s);
            if (Kind(f) == null) return;
            Remember(f, p, Kind(f));
            // This call precedes the native method, not the later progress
            // write or Get clip. NPC dialogue alone is not an acquisition.
            Early(f, p, Kind(f), Starts(f?.ActiveState, Kind(f)));
        }
        catch (Exception e) { Diagnostics.Throttled("ACQUISITION source observation", e); }
    }

    private static bool Valid(PlayerSlot p) => p != null && p.Hero && p.Alive && p.Ready && !p.Retiring && !p.ArenaTransfer && !TransitionVote.Holding(p);
    private static CoopSession Current => Plugin.Self ? Plugin.Self.Session : null;
    private static int Read(PlayerData pd, string key) => AcquisitionRules.Level(key) ? pd.GetInt(key) : (pd.GetBool(key) ? 1 : 0);

    private static void Remember(Fsm f, PlayerSlot p, Identity kind)
    {
        if (!Valid(p) || kind == null) return;
        if (pending != p || pendingKey != kind.Key) pendingPickup = null;
        if (kind.Pickup) pendingPickup = kind.Pickup;
        pending = p; pendingSource = f; pendingKey = kind.Key; pendingAt = Time.unscaledTime;
    }

    private static bool WorldSource(Fsm f) => f != null && f.GameObject &&
        !f.GameObject.GetComponentInParent<HeroController>() && !f.GameObject.GetComponentInParent<HealthManager>() &&
        !f.GameObject.GetComponentInParent<DamageHero>() && !InteractionRouter.IsUi(f) && !InteractionRouter.ScreenList(f);

    private static bool PromptSource(Fsm f)
    {
        // IsInteraction also includes LoadScene/BeginSceneTransition, which
        // automatic pickups may legitimately contain. Only actual prompt
        // listeners require a confirmed input/acquisition branch.
        if (f?.States == null) return false;
        foreach (var state in f.States) if (state?.Actions != null) foreach (var action in state.Actions)
            if (action != null && (action.GetType().Name == "ListenForUp" || action.GetType().Name == "ListenForDown")) return true;
        return false;
    }

    // Automatic soul pickups start in their collider callback, before vanilla
    // processes the contact. An NPC's prompt radius only records its owner;
    // the actual acquisition branch must start before its followers do.
    internal static void Contact(Fsm f, PlayerSlot p, bool exit)
    {
        if (Replaying || !WorldSource(f) || !Valid(p)) return;
        try
        {
            var s = Current; if (s == null || !s.Active || s.Data == null) return;
            EnsureSession(s);
            if (exit) { if (touching.TryGetValue(f, out var old) && old == p) touching.Remove(f); return; }
            var kind = Kind(f);
            if (kind == null || !SoulAcquisitionRitual.White(kind.Key, kind.Value) || !AcquisitionRules.Gained(kind.Key, Read(s.Data, kind.Key), kind.Value)) return;
            if (transfers.Exists(t => t.Id == AcquisitionRules.Id(kind.Key, kind.Value))) return;
            touching[f] = p;
            if (!PromptSource(f)) { Remember(f, p, kind); Early(f, p, kind, true, "pickup contact"); }
        }
        catch (Exception e) { Diagnostics.Throttled("ACQUISITION contact", e); }
    }

    // Some pickups relay their collision through TriggerEnterEvent rather
    // than PlayMaker's OnTriggerEnter2D. Observe that same native callback.
    internal static void TriggerContact(GameObject trigger, PlayerSlot p, bool exit)
    {
        if (Replaying || !Valid(p) || NativePickup(trigger) == null) return;
        foreach (var script in trigger.GetComponentsInParent<PlayMakerFSM>(true))
            if (script && NativePickup(script.gameObject) != null) Contact(script.Fsm, p, exit);
    }

    internal static PlayerSlot Route(PlayerSlot routed, Fsm f)
    {
        if (f == null || pendingSource != f && !touching.ContainsKey(f) && !pendingPickup) return routed;
        if (!WorldSource(f)) return routed;
        var kind = Kind(f);
        if (kind == null || !SoulAcquisitionRitual.White(kind.Key, kind.Value)) return routed;
        // The physical pickup and its later Check Fall/card FSM are siblings.
        // Their native ownership outlives the short cosmetic follower lease.
        // Bind only that same pickup, never the NPC or other actors' FSMs.
        var s = Current;
        if (s?.Data != null && Valid(pending) && kind.Pickup && kind.Pickup == pendingPickup && kind.Key == pendingKey && Time.unscaledTime - pendingAt < 35 &&
            (Read(s.Data, kind.Key) < kind.Value || pending.Hero.controlReqlinquished)) return pending;
        if (Valid(pending) && pendingSource == f && Time.unscaledTime - pendingAt < SoulAcquisitionRitual.Seconds) return pending;
        return touching.TryGetValue(f, out var p) && Valid(p) ? p : routed;
    }

    private static Identity Written(FsmStateAction action)
    {
        if (action is SetPlayerDataInt level && level.intName != null && level.value != null && AcquisitionRules.Level(level.intName.Value))
            return new Identity { Key = level.intName.Value, Value = level.value.Value };
        if (action is SetPlayerDataBool flag && flag.boolName != null && flag.value != null && flag.value.Value && flag.boolName.Value == "hasAcidArmour")
            return new Identity { Key = flag.boolName.Value, Value = 1 };
        return null;
    }

    private static string ClipKey(string name) => name == "Fireball Get" || name == "Fireball 1 Get" ? "fireballLevel" :
        name == "Quake Get" || name == "Quake 1 Get" ? "quakeLevel" :
        name == "Scream Get" || name == "Scream 1 Get" ? "screamLevel" : null;

    private static bool Starts(FsmState state, Identity kind)
    {
        if (state?.Actions == null || kind == null) return false;
        bool controls = false;
        foreach (var action in state.Actions)
        {
            var written = Written(action);
            if (written != null && written.Key == kind.Key && written.Value == kind.Value) return true;
            if (action is CallMethodProper call && call.methodName != null &&
                (call.methodName.Value == "RelinquishControl" || call.methodName.Value == "StopAnimationControl")) controls = true;
            if (action != null && action.GetType().Name.StartsWith("Tk2dPlayAnimation", StringComparison.Ordinal) &&
                AnimationKind(state.Fsm, Reflect.Get<FsmString>(action, "clipName", null)?.Value)?.Key == kind.Key) return true;
        }
        if (!controls) return false;
        if (WorldSource(state.Fsm) && !PromptSource(state.Fsm)) return true;
        string name = (state.Name ?? "").ToLowerInvariant();
        return !name.Contains("?") && (name.Contains("collect") || name.Contains("absorb") || name.Contains("get spell") || name.Contains("get ability"));
    }

    // Only actions in the entered state count for actor-owned Spell Control.
    // Scanning its other Get branches would turn ordinary casts into rituals.
    internal static void ObserveState(FsmState state, PlayerSlot routed)
    {
        if (Replaying || state == null) return;
        try
        {
            var kind = Kind(state.Fsm);
            bool animationProof = false;
            if (state.Actions != null) foreach (var action in state.Actions)
            {
                var candidate = Written(action);
                if (kind == null && candidate != null && SoulAcquisitionRitual.White(candidate.Key, candidate.Value)) kind = candidate;
                var animated = action != null && action.GetType().Name.StartsWith("Tk2dPlayAnimation", StringComparison.Ordinal) ?
                    AnimationKind(state.Fsm, Reflect.Get<FsmString>(action, "clipName", null)?.Value) : null;
                if (animated != null) { kind = animated; animationProof = true; break; }
            }
            if (kind == null || !Starts(state, kind)) return;
            var p = WorldSource(state.Fsm) ? Route(routed, state.Fsm) ?? Collector(state.Fsm, kind.Key) : Current?.Resolve(state.Fsm) ?? routed;
            Early(state.Fsm, p, kind, true, "native state " + state.Name, animationProof);
        }
        catch (Exception e) { Diagnostics.Throttled("ACQUISITION state entry", e); }
    }

    internal static void BeforeEvent(Fsm f, FsmEvent evt, FsmEventData eventData)
    {
        if (Replaying || evt == null) return;
        try
        {
            // Kind already defers non-pickup action rescans for half a second.
            // In generic rooms, a still-valid miss cannot produce an accepted
            // acquisition, regardless of WorldSource's current ancestry. The
            // four native pickup rooms must keep their live name/parent check
            // ahead of the miss, so newly spawned/renamed pickups stay visible.
            Miss miss;
            if (f != null && misses.TryGetValue(f, out miss) && miss.States == f.States && Time.unscaledTime < miss.Until)
            {
                string room = Scenes.GetActiveScene().name;
                if (room != "Crossroads_ShamanTemple" && room != "Ruins1_24" && room != "Room_Fungus_Shaman" && room != "Waterways_13") return;
            }
            if (!WorldSource(f)) return;
            var kind = Kind(f); if (kind == null || f.ActiveState?.Transitions == null) return;
            foreach (var transition in f.ActiveState.Transitions)
            {
                if (transition == null || transition.EventName != evt.Name) continue;
                FsmState target = null;
                if (f.States != null) foreach (var state in f.States) if (state != null && state.Name == transition.ToState) { target = state; break; }
                if (!Starts(target, kind)) return;
                var s = Current;
                var p = eventData?.SentByFsm != null ? s?.Resolve(eventData.SentByFsm) : null;
                p = Valid(p) ? p : Route(PlayerContext.Current, f) ?? Collector(f, kind.Key);
                Early(f, p, kind, true); return;
            }
        }
        catch (Exception e) { Diagnostics.Throttled("ACQUISITION accepted event", e); }
    }

    internal static void ControlTaken(HeroController hero)
    {
        if (Replaying || !hero) return;
        try
        {
            var f = WorldRouting.Current; var kind = Kind(f);
            Early(f, Route(Current?.Resolve(hero), f), kind, Starts(f?.ActiveState, kind));
        }
        catch (Exception e) { Diagnostics.Throttled("ACQUISITION native control", e); }
    }

    private static void Early(Fsm f, PlayerSlot p, Identity kind, bool proven, string signal = "native control/event", bool animationProof = false)
    {
        var s = Current; var gm = GameManager.instance;
        if (s == null || !s.Active || s.Data == null || !Valid(p) || kind == null || !gm || !gm.HasFinishedEnteringScene ||
            !SoulAcquisitionRitual.White(kind.Key, kind.Value) || (!AcquisitionRules.Gained(kind.Key, Read(s.Data, kind.Key), kind.Value) && !animationProof)) return;
        if (!proven && (!WorldSource(f) || PromptSource(f))) return;
        EnsureSession(s); Remember(f, p, kind);
        Begin(s, kind.Key, kind.Value, p, f, null, signal);
    }

    private static PlayerSlot Collector(Fsm f, string key)
    {
        // A recorded native pickup wins over an unrelated ambient P1 context.
        if (Valid(pending) && Time.unscaledTime - pendingAt < 35 && pendingKey == key &&
            (f == null || f == pendingSource || Kind(f)?.Key == key || Current?.Resolve(f) == pending)) return pending;
        if (f != null)
        {
            if (touching.TryGetValue(f, out var contact) && Valid(contact)) return contact;
            var p = PickupCard.Resolve(f) ?? WorldRouting.Resolve(f) ?? InteractionRouter.Resolve(f);
            if (Valid(p)) return p;
        }
        if (Valid(PlayerContext.Current)) return PlayerContext.Current;
        if (f != null && f.GameObject) return Current?.Nearest(f.GameObject.transform.position);
        return null;
    }

    internal static PlayerSlot ResolveCollector(string key) => Collector(WorldRouting.Current, key);

    internal static void NativeAction(Fsm f, string key, int value)
    {
        if (!AcquisitionRules.Flag(key) && !AcquisitionRules.Level(key)) return;
        try
        {
            var s = Current;
            if (s == null || !s.Active || s.Data == null || !AcquisitionRules.Gained(key, Read(s.Data, key), value)) return;
            Begin(s, key, value, Collector(f, key), f, null);
        }
        catch (Exception e) { ClearTransfers(); Diagnostics.Throttled("ACQUISITION native observation", e); }
    }

    internal static void Changed(CoopSession s, PlayerData pd, string key, int oldValue, int value)
    {
        if (s == null || !s.Active || pd != s.Data || !AcquisitionRules.Gained(key, oldValue, value)) return;
        try
        {
            EnsureSession(s);
            levels[key] = value;
            Begin(s, key, value, Collector(WorldRouting.Current, key), WorldRouting.Current, null);
        }
        catch (Exception e) { ClearTransfers(); Diagnostics.Throttled("ACQUISITION data observation", e); }
    }

    internal static void AnimationStarting(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
    {
        if (Replaying || !animator || clip == null) return;
        try { ObserveAnimation(animator, clip); }
        catch (Exception e) { ClearTransfers(); Diagnostics.Throttled("ACQUISITION animation observation", e); }
    }

    // Observe the serialized animation action itself before native Play. This
    // supplies its own FSM/target even when an outer world update has a stale
    // ambient source or an animator's Play overload is inlined by Mono.
    internal static void AnimationAction(FsmStateAction action)
    {
        if (Replaying || action?.Fsm == null) return;
        string name = Reflect.Get<FsmString>(action, "clipName", null)?.Value;
        if (AnimationKind(action.Fsm, name) == null) return;
        try
        {
            var target = action.Fsm.GetOwnerDefaultTarget(Reflect.Get<FsmOwnerDefault>(action, "gameObject", null));
            var animator = target ? target.GetComponent<tk2dSpriteAnimator>() : null;
            if (animator) ObserveAnimation(animator, name, action.Fsm, "native animation action");
        }
        catch (Exception ex) { Diagnostics.Throttled("ACQUISITION animation action", ex); }
    }

    private static void ObserveAnimation(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
        => ObserveAnimation(animator, clip.name, AnimationSource ?? WorldRouting.Current, "native Play");

    private static void ObserveAnimation(tk2dSpriteAnimator animator, string clip, Fsm f, string signal)
    {
        // A native action, damage or movement can interrupt a cosmetic pose.
        foreach (var t in transfers) foreach (var a in t.Actors)
            if (a.Posing && a.Animator == animator) ReleasePose(a);
        if (!(Magical(clip) || clip != null && clip.EndsWith(" Get", StringComparison.Ordinal))) return;
        var s = Current; var kind = AnimationKind(f, clip) ?? Kind(f);
        var actor = s == null ? null : s.Resolve(animator);
        // Hero Spell Control has many ordinary cast states: recognise only
        // an actual acquisition clip. The P2/P8 animator is stronger evidence
        // than an ambient singleton or the NPC who spawned the spell orbs.
        if (kind == null || s == null || s.Data == null || !Valid(actor)) return;
        EnsureSession(s);
        Remember(f, actor, kind);
        // An orb may have captured vanilla's P1 singleton before the real
        // extra actor starts its Get clip. Correct that short-lived visual
        // transaction; never write the progress flag or replay the native event.
        if (Valid(actor)) for (int i = transfers.Count - 1; i >= 0; i--)
        {
            var t = transfers[i];
            if (t.Id != AcquisitionRules.Id(kind.Key, kind.Value) || t.Collector == actor || Time.unscaledTime - t.At > AcquisitionRules.EffectSeconds) continue;
            Finish(t); transfers.RemoveAt(i); SoulAcquisitionRitual.Release(); latch.Forget(kind.Key, kind.Value);
            Diagnostics.Write("ACQUISITION corrected native collector=P" + (actor.Index + 1) + " key=" + kind.Key);
            Begin(s, kind.Key, kind.Value, actor, f, null, signal + " corrected collector"); return;
        }
        // The real acquisition animation is proof even if this state wrote
        // the level first. One latch still prevents all later phases/replays.
        if (!AcquisitionRules.Gained(kind.Key, Read(s.Data, kind.Key), kind.Value) && AnimationKind(f, clip) == null) return;
        Begin(s, kind.Key, kind.Value, actor, f, null, signal + " clip=" + clip);
    }

    internal static void SpellOrb(SpellGetOrb orb)
    {
        var s = Current;
        if (!orb || !orb.trackToHero || s == null || s.Data == null || s.Data.fireballLevel != 0 || Scenes.GetActiveScene().name != "Crossroads_ShamanTemple") return;
        EnsureSession(s);
        if (!orbs.Contains(orb) && orbs.Count < 64) orbs.Add(orb);
    }

    private static ParticleSystem BurstSource(Fsm f, HeroController collector)
    {
        if (f != null && f.GameObject)
        {
            foreach (var ps in f.GameObject.GetComponentsInChildren<ParticleSystem>(true))
                if (AcquisitionParticles.Usable(ps)) return ps;
            if (f.States != null) foreach (var state in f.States)
                if (state?.Actions != null) foreach (var action in state.Actions)
                    if (action is SetParticleEmissionRate rate)
                    {
                        var go = f.GetOwnerDefaultTarget(rate.gameObject);
                        var ps = go ? go.GetComponent<ParticleSystem>() : null;
                        if (AcquisitionParticles.Usable(ps)) return ps;
                    }
        }
        return RoleAura.FindFocusParticles(collector) ?? AcquisitionParticles.Dream(collector);
    }

    private static void Begin(CoopSession s, string key, int value, PlayerSlot collector, Fsm f, ParticleSystem burst, string signal = "native progress fallback")
    {
        var gm = GameManager.instance;
        if (s == null || !s.Active || s.Players.Count < 2 || s.TeamWipe || PvpMatch.Running || CoopEnding.Active || !gm || gm.IsLoadingSceneTransition || !Valid(collector)) return;
        EnsureSession(s);
        if (!latch.First(key, value)) return;
        Diagnostics.Write("ACQUISITION start " + key + ":" + value + " collector=P" + (collector.Index + 1) +
            " signal=" + signal + " source=" + (f?.GameObject ? f.GameObject.name : "none") + " fsm=" + f?.Name + " state=" + f?.ActiveStateName);
        ReleasePoses();
        if (SoulAcquisitionRitual.White(key, value)) SoulAcquisitionRitual.Begin(s, collector, key, value);
        var t = new Transfer { Collector = collector, Hero = collector.Hero, At = Time.unscaledTime, Scene = Scenes.GetActiveScene().handle, Id = AcquisitionRules.Id(key, value) };
        transfers.Add(t);
        try
        {
            var nativeDream = AcquisitionParticles.Dream(collector.Hero);
            var nativeBurst = burst ? burst : BurstSource(f, collector.Hero);
            foreach (var p in s.Players)
            {
                if (p == collector || !Valid(p)) continue;
                var a = new Actor { Player = p, Hero = p.Hero, Animator = p.Hero.GetComponent<tk2dSpriteAnimator>(), WhiteRitual = SoulAcquisitionRitual.White(key, value) };
                t.Actors.Add(a);
                a.Stream = AcquisitionParticles.Create(p, nativeDream, true);
                a.Burst = AcquisitionParticles.Create(p, nativeBurst, false);
                if (a.WhiteRitual)
                {
                    // Local absorption is simultaneous with native pickup;
                    // the decorative travelling stream may arrive afterwards.
                    a.Arrived = true; a.Burst?.Burst(a.Hero.transform.position + Vector3.up * .3f);
                    var flash = a.Hero.GetComponent<SpriteFlash>(); if (flash) flash.flashFocusGet();
                }
            }
            Diagnostics.Write("ACQUISITION shared " + t.Id + " collector=P" + (collector.Index + 1) + " recipients=" + t.Actors.Count + (SoulAcquisitionRitual.White(key, value) ? "; bounded white ritual" : "; cosmetic only"));
        }
        catch (Exception e) { Finish(t); transfers.Remove(t); SoulAcquisitionRitual.Release(); Diagnostics.Throttled("ACQUISITION setup", e); }
    }

    private static bool Free(Actor a)
    {
        var p = a.Player; var h = a.Hero;
        if (GameManager.instance && GameManager.instance.isPaused) return false;
        if (!Valid(p) || p.Hero != h || !h || h.controlReqlinquished || !h.acceptingInput || p.InputBlocked || p.Reviving || p.Hazard || EmergencyWarp.Active(p) || ScriptedParty.Holds(p) || ChallengeSequence.Holds(p) || Charms.NativeMenuOpen || InteractionRouter.ActivePlayer == p) return false;
        var c = h.cState;
        if (!c.onGround || c.swimming || c.attacking || c.focusing || c.dashing || c.superDashing || c.recoiling || c.transitioning) return false;
        var anim = h.GetComponent<HeroAnimationController>();
        if (anim && !anim.controlEnabled) return false;
        var input = p.Actions;
        return input == null || Mathf.Abs(input.moveVector.X) < .15f && Mathf.Abs(input.moveVector.Y) < .15f && !input.jump.IsPressed && !input.attack.IsPressed && !input.dash.IsPressed && !input.cast.IsPressed;
    }

    private static void Pose(Actor a, HeroController collector)
    {
        if (!a.Animator || !Free(a)) return;
        // Several inventory flags can be granted together. Only one cosmetic
        // pose may own an animator's callback snapshot at a time.
        foreach (var t in transfers) foreach (var other in t.Actors)
            if (other != a && other.Posing && other.Animator == a.Animator) ReleasePose(other);
        var source = collector ? collector.GetComponent<tk2dSpriteAnimator>() : null;
        var name = source && source.CurrentClip != null ? source.CurrentClip.name : null;
        if (name != null && (name.StartsWith("Collect Magical", StringComparison.Ordinal) || name.EndsWith(" Get", StringComparison.Ordinal))) a.Pose = a.Animator.GetClipByName(name);
        if (!Usable(a.Pose)) foreach (string fallback in new[] { "Collect Magical 2", "Collect Magical", "Focus Get", "Focus" })
        { a.Pose = a.Animator.GetClipByName(fallback); if (Usable(a.Pose)) break; }
        if (!Usable(a.Pose)) return;
        a.Completed = a.Animator.AnimationCompleted; a.Triggered = a.Animator.AnimationEventTriggered;
        a.Animator.AnimationCompleted = null; a.Animator.AnimationEventTriggered = null;
        a.Posing = true; a.PoseUntil = Time.unscaledTime + AcquisitionRules.PoseSeconds;
        try { Replaying = true; using (PlayerContext.Enter(a.Player)) a.Animator.Play(a.Pose); }
        finally { Replaying = false; }
    }

    internal static bool Posing(PlayerSlot p)
    {
        foreach (var t in transfers) foreach (var a in t.Actors) if (a.Player == p && a.Posing)
        {
            if (Time.unscaledTime < a.PoseUntil && Free(a)) return true;
            ReleasePose(a);
        }
        return false;
    }

    private static void ReleasePose(Actor a)
    {
        if (!a.Posing) return;
        a.Posing = false;
        if (!a.Animator) return;
        // Do not restore native completion callbacks while our get clip is
        // still running: its later completion could advance a foreign FSM.
        bool wasReplaying = Replaying;
        try
        {
            if (a.Animator.CurrentClip == a.Pose && a.Animator.AnimationCompleted == null && a.Animator.AnimationEventTriggered == null)
            {
                Replaying = true;
                a.Animator.Stop();
                if (a.Hero && a.Player.Hero == a.Hero && a.Player.Alive && Usable(a.Animator.GetClipByName("Idle")))
                    using (PlayerContext.Enter(a.Player)) a.Animator.Play("Idle");
            }
        }
        catch (Exception e) { Diagnostics.Throttled("ACQUISITION pose release", e); }
        finally
        {
            Replaying = wasReplaying;
            // Restore even if an incomplete Custom Knight clip rejected Idle.
            // Preserve newer native callbacks installed by an interruption.
            if (a.Animator)
            {
                if (a.Animator.AnimationCompleted == null) a.Animator.AnimationCompleted = a.Completed;
                if (a.Animator.AnimationEventTriggered == null) a.Animator.AnimationEventTriggered = a.Triggered;
            }
        }
    }

    private static bool Usable(tk2dSpriteAnimationClip clip) => clip != null && clip.frames != null && clip.frames.Length > 0 && clip.fps > 0;

    internal static void ReleasePoses()
    {
        foreach (var t in transfers) foreach (var a in t.Actors) ReleasePose(a);
    }

    internal static void Tick(CoopSession s)
    {
        try
        {
            if (s == null || !s.Active || s.Data == null) { Reset(); return; }
            EnsureSession(s);
            // A read-only fallback for native acquisition clips already
            // running before a detour is reached; never start from a replica.
            if (GameManager.instance && GameManager.instance.HasFinishedEnteringScene && !Replaying)
                foreach (var p in s.Players) if (Valid(p) && p.Hero.controlReqlinquished && !SoulAcquisitionRitual.Holds(p) && !Posing(p))
                {
                    var animator = p.Hero.GetComponent<tk2dSpriteAnimator>();
                    if (animator && AnimationKind(null, animator.CurrentClip?.name) != null)
                        ObserveAnimation(animator, animator.CurrentClip.name, null, "active native clip fallback");
                }
            var gm = GameManager.instance;
            if (!gm || gm.IsLoadingSceneTransition) { ClearTransfers(); return; }
            for (int orbIndex = 0; orbIndex < orbs.Count; orbIndex++)
            {
                var orb = orbs[orbIndex];
                if (orb && orb.orbGetObject && orb.orbGetObject.activeInHierarchy)
                {
                    // Orb.hero can capture the ambient P1 singleton. Use a
                    // proven native pickup owner, never that fallback target.
                    var p = Collector(WorldRouting.Current, "fireballLevel");
                    var ps = orb.orbGetObject.GetComponentInChildren<ParticleSystem>(true);
                    Begin(s, "fireballLevel", 1, p, WorldRouting.Current, ps ? ps : orb.ptZoom);
                    orbs.Clear(); break;
                }
            }
            // Some native acquisition scripts write fields directly. Poll only
            // eleven flags and three levels, never scan all room objects/frame.
            foreach (string flag in AcquisitionRules.Flags) Poll(s, flag);
            foreach (string key in AcquisitionRules.Levels) Poll(s, key);
            for (int i = transfers.Count - 1; i >= 0; i--)
            {
                var t = transfers[i]; float elapsed = Time.unscaledTime - t.At;
                if (t.Scene != Scenes.GetActiveScene().handle || !Valid(t.Collector) || t.Collector.Hero != t.Hero || elapsed > AcquisitionRules.EffectSeconds)
                { Finish(t); transfers.RemoveAt(i); continue; }
                foreach (var a in t.Actors)
                {
                    if (!Valid(a.Player) || a.Player.Hero != a.Hero) { Dispose(a); continue; }
                    Vector3 target = a.Hero.transform.position + Vector3.up * .3f;
                    a.Stream?.Stream(t.Hero.transform.position + Vector3.up * .35f, target, elapsed);
                    if (!a.Arrived && elapsed >= AcquisitionRules.TravelSeconds)
                    {
                        a.Arrived = true; a.Burst?.Burst(target);
                        var flash = a.Hero.GetComponent<SpriteFlash>(); if (flash) flash.flashFocusGet();
                        if (!a.WhiteRitual) Pose(a, t.Hero);
                    }
                    if (a.Posing && (Time.unscaledTime >= a.PoseUntil || !Free(a))) ReleasePose(a);
                }
            }
        }
        catch (Exception e) { ClearTransfers(); Diagnostics.Throttled("ACQUISITION visual recovery", e); }
    }

    private static void Poll(CoopSession s, string key)
    {
        int value = Read(s.Data, key); levels.TryGetValue(key, out int old);
        levels[key] = value;
        if (AcquisitionRules.Gained(key, old, value)) Begin(s, key, value, Collector(WorldRouting.Current, key), WorldRouting.Current, null);
    }

    private static void EnsureSession(CoopSession s)
    {
        if (session == s && data == s.Data) return;
        // Preserve the just-observed collector when a native hook precedes the
        // first runtime Tick after entering a scene.
        var actor = pending; var source = pendingSource; var pickup = pendingPickup; var animation = AnimationSource; string rememberedKey = pendingKey; float at = pendingAt;
        Reset(); session = s; data = s.Data;
        pending = actor; pendingSource = source; pendingPickup = pickup; AnimationSource = animation; pendingKey = rememberedKey; pendingAt = at;
        foreach (string flag in AcquisitionRules.Flags) levels[flag] = Read(data, flag);
        foreach (string key in AcquisitionRules.Levels) levels[key] = Read(data, key);
    }

    private static void Dispose(Actor a)
    {
        ReleasePose(a); a.Stream?.Dispose(); a.Burst?.Dispose(); a.Stream = a.Burst = null;
    }
    private static void Finish(Transfer t) { foreach (var a in t.Actors) Dispose(a); t.Actors.Clear(); }
    private static void ClearTransfers() { foreach (var t in transfers) Finish(t); transfers.Clear(); orbs.Clear(); }
    internal static void Reset()
    {
        ClearTransfers(); sources.Clear(); touching.Clear(); misses.Clear(); levels.Clear(); latch.Clear();
        SoulAcquisitionRitual.Release();
        pending = null; pendingSource = null; pendingPickup = null; pendingKey = null; pendingAt = 0; session = null; data = null; Replaying = false; AnimationSource = null;
        AcquisitionParticles.Reset();
    }
}
