using System;
using System.Collections.Generic;
using System.Reflection;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.Rendering;

namespace KO.HollowKnight8;

// Mask rendering is independent of the healing FSM and its native white FX.
// Every camera must hide these plates; hiding them only in mainCamera leaves
// an auxiliary pass free to draw the solid Focus background again.
internal static class FocusBackdropGuard
{
    private sealed class Actor
    {
        internal HeroController Hero;
        internal PlayMakerFSM Spell;
        internal bool Focusing;
        internal bool Reported;
        internal float Until;
        internal readonly List<Renderer> Masks = new List<Renderer>();
        internal readonly List<Renderer> FocusMasks = new List<Renderer>();
        internal readonly List<Renderer> Foreground = new List<Renderer>();
    }
    private struct Saved { internal Renderer Renderer; internal bool Enabled; }
    private sealed class Pass
    {
        internal Camera Camera;
        internal bool Completed;
        internal bool Healing;
        internal CommandBuffer Composite;
        internal CameraEvent CompositeEvent;
        internal bool CompositeAttached;
        internal readonly List<Renderer> CompositePlanes = new List<Renderer>();
        internal NativeBackground.SavedCamera Background;
        internal readonly List<Saved> Saved = new List<Saved>(64);
        internal readonly HashSet<Renderer> Seen = new HashSet<Renderer>();
    }
    private static readonly Actor[] actors = new Actor[8];
    private static readonly List<Pass> passes = new List<Pass>();
    private static readonly Stack<Pass> pool = new Stack<Pass>();
    private static readonly HashSet<Camera> loggedCameras = new HashSet<Camera>();
    private static int frame = -1;

    internal static void Reset()
    {
        RestoreAll();
        while (pool.Count > 0)
        {
            Pass pass = pool.Pop();
            if (pass.Composite != null) pass.Composite.Release();
        }
        Array.Clear(actors, 0, actors.Length); loggedCameras.Clear(); frame = -1; NativeBackground.Reset();
    }
    private static void Restore(Pass pass)
    {
        DetachComposite(pass);
        NativeBackground.RestoreCamera(pass.Background); pass.Background = default(NativeBackground.SavedCamera);
        for (int i = pass.Saved.Count - 1; i >= 0; i--)
        {
            Saved saved = pass.Saved[i];
            if (saved.Renderer && saved.Renderer.enabled != saved.Enabled) saved.Renderer.enabled = saved.Enabled;
        }
        pass.Saved.Clear(); pass.Seen.Clear(); pass.Camera = null; pass.Completed = pass.Healing = false; pool.Push(pass);
    }
    private static void RestoreAll()
    {
        for (int i = passes.Count - 1; i >= 0; i--) Restore(passes[i]);
        passes.Clear();
    }
    internal static void AfterCamera(Camera camera)
    {
        for (int i = passes.Count - 1; i >= 0; i--)
        {
            if (passes[i].Camera != camera || passes[i].Completed) continue;
            passes[i].Completed = true;
            // Nested passes restore inner saved values first, then native values.
            while (passes.Count > 0 && passes[passes.Count - 1].Completed)
            {
                int last = passes.Count - 1; Pass pass = passes[last]; passes.RemoveAt(last); Restore(pass);
            }
            return;
        }
    }
    private static void Add(List<Renderer> list, Renderer renderer, HeroController hero)
    {
        // A BlurPlane composes the native rendered/blurred texture, even if
        // its sprite definition is black_solid or a Focus action references it.
        // Suppress real heal masks without hiding that background compositor.
        if (!renderer || renderer is ParticleSystemRenderer || renderer == hero.heroLight ||
            renderer.GetComponent<BlurPlane>() ||
            (GameManager.instance && renderer == GameManager.instance.heroLight) || list.Contains(renderer)) return;
        list.Add(renderer);
    }
    private static void MaskRoot(Actor actor, Component root)
    {
        if (!root || root.gameObject == actor.Hero.gameObject) return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) Add(actor.Masks, renderer, actor.Hero);
    }
    private static Actor Discover(HeroController hero)
    {
        Actor actor = new Actor { Hero = hero, Spell = hero.spellControl };
        Foreground(actor);
        MaskRoot(actor, hero.vignette); MaskRoot(actor, hero.vignetteFSM);
        // The native field works even if a skin renames the mask. This fallback
        // preserves existing extra-vignette handling, including mesh renderers.
        foreach (Transform child in hero.GetComponentsInChildren<Transform>(true))
            if (child.name.IndexOf("vignette", StringComparison.OrdinalIgnoreCase) >= 0) MaskRoot(actor, child);
        return actor;
    }
    private static void Foreground(Actor actor)
    {
        actor.Foreground.Clear();
        actor.Hero.GetComponentsInChildren<Renderer>(true, actor.Foreground);
    }
    private static bool Healing(HeroController hero)
    {
        if (CoopEnding.Active || EndingAbsorption.Active || !hero.spellControl) return false;
        string state = hero.spellControl.ActiveStateName;
        if (state == "Focus Start D") return false;
        return hero.cState.focusing || state == "Focus Start" || state == "Focus" ||
            state == "Focus Heal" || state == "Focus Heal 2";
    }
    private static bool HealingState(string state) => state == "Focus Start" || state == "Focus" ||
        state == "Focus Heal" || state == "Focus Heal 2" || state == "Focus End" || state == "Focus Cancel";
    private static bool SolidMask(Renderer renderer)
    {
        SpriteRenderer sprite = renderer as SpriteRenderer;
        if (sprite && sprite.sprite) return sprite.sprite.name == "black_solid";
        tk2dBaseSprite meshSprite = renderer.GetComponent<tk2dBaseSprite>();
        var definition = meshSprite ? meshSprite.GetCurrentSpriteDef() : null;
        return definition != null && definition.name == "black_solid";
    }
    private static void Target(Actor actor, GameObject target)
    {
        if (!target) return;
        // Direct ordinary-heal references can point to a detached black plate.
        // Do not match by a dark colour or a generic Focus name: that would
        // remove skins, charge lines, shells and legitimate story fades.
        foreach (Renderer renderer in target.GetComponents<Renderer>())
            if (SolidMask(renderer)) Add(actor.FocusMasks, renderer, actor.Hero);
    }
    private static void FocusTargets(Actor actor)
    {
        actor.FocusMasks.Clear(); PlayMakerFSM spell = actor.Hero.spellControl;
        if (!spell || spell.FsmStates == null) return;
        foreach (FsmState state in spell.FsmStates)
        {
            if (!HealingState(state.Name) || state.Actions == null) continue;
            foreach (FsmStateAction action in state.Actions)
            {
                if (action == null) continue;
                foreach (ReferenceField field in Hooks.GetReferenceAccesses(action.GetType()))
                {
                    object value = field.GetValue(action); GameObject target = value as GameObject;
                    if (value is Component component) target = component ? component.gameObject : null;
                    else if (value is FsmGameObject gameObject) target = gameObject.Value;
                    else if (value is FsmObject fsmObject)
                    {
                        target = fsmObject.Value as GameObject;
                        if (fsmObject.Value is Component c) target = c ? c.gameObject : null;
                    }
                    else if (value is FsmOwnerDefault owner && owner.OwnerOption != OwnerDefaultOption.UseOwner)
                        target = owner.GameObject == null ? null : owner.GameObject.Value;
                    Target(actor, target);
                }
            }
        }
    }
    private static void Hide(Pass pass, List<Renderer> masks, Camera camera)
    {
        int cullingMask = camera.cullingMask;
        foreach (Renderer renderer in masks)
        {
            Hide(pass, renderer, cullingMask);
        }
    }
    private static void Hide(Pass pass, Renderer renderer, Camera camera, bool capture = false)
    {
        Hide(pass, renderer, camera.cullingMask, capture);
    }
    private static void Hide(Pass pass, Renderer renderer, int cullingMask, bool capture = false)
    {
        if (!renderer || pass.Seen.Contains(renderer)) return;
        var obj = renderer.gameObject;
        if ((cullingMask & (1 << obj.layer)) == 0 || !obj.activeInHierarchy ||
            (!capture && renderer.GetComponent<BlurPlane>())) return;
        pass.Seen.Add(renderer);
        bool enabled = renderer.enabled;
        pass.Saved.Add(new Saved { Renderer = renderer, Enabled = enabled });
        if (enabled) renderer.enabled = false;
    }
    private static void Capture(Pass pass)
    {
        if (!pass.Background.Camera) return;
        // BlurPlane is a consumer of this pass's render texture. Never render
        // it into its own source, even if a skin/effect changes its layer.
        // Keep it visible in the scene camera, where native blur belongs.
        int cullingMask = pass.Camera.cullingMask;
        for (int i = 0; i < BlurPlane.BlurPlaneCount; i++)
        {
            var plane = BlurPlane.GetBlurPlane(i);
            if (plane) Hide(pass, plane.GetComponent<MeshRenderer>(), cullingMask, capture: true);
        }
        // Player bodies and Focus/lantern particles are foreground effects,
        // not distant scenery. Exclude them only from the background capture;
        // they keep their native rendering in the main camera.
        foreach (Actor actor in actors)
            if (actor != null && actor.Hero)
                foreach (Renderer renderer in actor.Foreground)
                    Hide(pass, renderer, cullingMask, capture: true);
    }
    private static void Reapply(Pass pass)
    {
        // tk2d flushes deferred sprite work in OnPreCull. Other camera
        // callbacks may also re-enable a mask after the delegate ran.
        foreach (Saved saved in pass.Saved)
            if (saved.Renderer && saved.Renderer.enabled) saved.Renderer.enabled = false;
    }
    private static void DetachComposite(Pass pass)
    {
        if (pass.CompositeAttached && pass.Camera)
            pass.Camera.RemoveCommandBuffer(pass.CompositeEvent, pass.Composite);
        pass.CompositeAttached = false;
        if (pass.Composite != null) pass.Composite.Clear();
        // If the native texture disappears between pre-cull and pre-render,
        // withdraw the bypass as one transaction, including its plane hiding.
        // Keep background-capture masks and other outer transactions intact.
        for (int i = pass.Saved.Count - 1; i >= 0; i--)
        {
            Saved saved = pass.Saved[i];
            if (!pass.CompositePlanes.Contains(saved.Renderer)) continue;
            if (saved.Renderer && saved.Renderer.enabled != saved.Enabled) saved.Renderer.enabled = saved.Enabled;
            pass.Saved.RemoveAt(i); pass.Seen.Remove(saved.Renderer);
        }
        pass.CompositePlanes.Clear();
    }
    private static void CompositeBackground(Pass pass)
    {
        // The alpha204 fix bypassed native BlurPlane meshes and rendered the
        // distant artwork directly, which made healing work but lost its blur.
        // Bypass those same meshes, now copying the already-blurred native
        // capture before foreground geometry. Main-camera effects still run
        // after composition; players and white Focus particles stay sharp.
        if (!pass.Healing || !NativeBackground.TryTexture(pass.Camera, out RenderTexture texture))
        {
            DetachComposite(pass);
            return;
        }
        CameraEvent stage = pass.Camera.actualRenderingPath == RenderingPath.DeferredShading ||
            pass.Camera.actualRenderingPath == RenderingPath.DeferredLighting
            ? CameraEvent.BeforeGBuffer : CameraEvent.BeforeForwardOpaque;
        if (pass.CompositeAttached && stage != pass.CompositeEvent) DetachComposite(pass);
        if (pass.Composite == null) pass.Composite = new CommandBuffer { name = "Local8 Focus blurred backdrop" };
        pass.Composite.Clear();
        pass.Composite.Blit(texture, BuiltinRenderTextureType.CameraTarget);
        // Blit changes render state; restore the camera target and its viewport
        // before opaque/transparent actors are submitted by Unity.
        pass.Composite.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        pass.Composite.SetViewport(pass.Camera.pixelRect);
        if (!pass.CompositeAttached)
        {
            pass.CompositeEvent = stage;
            pass.Camera.AddCommandBuffer(stage, pass.Composite);
            pass.CompositeAttached = true;
        }
        for (int i = 0; i < BlurPlane.BlurPlaneCount; i++)
        {
            var plane = BlurPlane.GetBlurPlane(i);
            var renderer = plane ? plane.GetComponent<MeshRenderer>() : null;
            if (renderer && !pass.Seen.Contains(renderer))
            {
                Hide(pass, renderer, pass.Camera, capture: true);
                if (pass.Seen.Contains(renderer)) pass.CompositePlanes.Add(renderer);
            }
        }
    }
    internal static void BeforeRender(Camera camera)
    {
        // tk2d may rebuild its matrix in OnPreCull after the camera delegate.
        // Synchronize only the native background pass. Healing must not widen
        // the main depth range or replace blurred artwork with sharp artwork.
        var runtime = Plugin.Self; var session = runtime == null ? null : runtime.Session;
        var gm = GameManager.instance;
        if (!camera || runtime == null || !runtime.Enabled.Value || session == null || !session.Active || session.TeamWipe ||
            !gm || !gm.IsGameplayScene() || gm.IsLoadingSceneTransition) { RestoreAll(); return; }
        try
        {
            for (int i = passes.Count - 1; i >= 0; i--)
                if (passes[i].Camera == camera && !passes[i].Completed)
                {
                    Pass pass = passes[i];
                    if (pass.Background.Camera) NativeBackground.Synchronize(camera);
                    Capture(pass); CompositeBackground(pass); Reapply(pass);
                    break;
                }
        }
        catch (Exception e) { RestoreAll(); Diagnostics.Throttled("FOCUS background render", e); }
    }
    internal static void BeforeCamera(Camera camera)
    {
        if (frame != Time.frameCount) { RestoreAll(); frame = Time.frameCount; }
        var runtime = Plugin.Self; CoopSession session = runtime == null ? null : runtime.Session;
        var gm = GameManager.instance;
        if (!camera || session == null || !runtime.Enabled.Value || !session.Active || session.TeamWipe ||
            !gm || !gm.IsGameplayScene() || gm.IsLoadingSceneTransition)
        {
            RestoreAll();
            return;
        }
        Pass pass = pool.Count == 0 ? new Pass() : pool.Pop(); pass.Camera = camera; passes.Add(pass);
        try
        {
            pass.Background = NativeBackground.BeginCamera(camera);
            bool healing = false;
            foreach (PlayerSlot player in session.Players)
            {
                if (player.Index < 0 || player.Index >= actors.Length || !player.Hero) continue;
                Actor actor = actors[player.Index];
                if (actor == null || actor.Hero != player.Hero || actor.Spell != player.Hero.spellControl)
                    actors[player.Index] = actor = Discover(player.Hero);
                bool focus = player.Alive && Healing(actor.Hero);
                if (focus && !actor.Focusing)
                {
                    Foreground(actor);
                    FocusTargets(actor);
                    if (!actor.Reported)
                    {
                        actor.Reported = true;
                        Diagnostics.Write("FOCUS BACKDROP P" + (player.Index + 1) + " state=" + actor.Spell.ActiveStateName +
                            " native_masks=" + actor.Masks.Count + " heal_solid_masks=" + actor.FocusMasks.Count);
                    }
                }
                actor.Focusing = focus;
                if (focus) actor.Until = Time.unscaledTime + .25f;
                Hide(pass, actor.Masks, camera);
                if (player.Alive && !CoopEnding.Active && !EndingAbsorption.Active &&
                    actor.Spell && actor.Spell.ActiveStateName != "Focus Start D" && Time.unscaledTime < actor.Until)
                {
                    healing = true; Hide(pass, actor.FocusMasks, camera);
                }
            }
            pass.Healing = healing;
            Capture(pass); CompositeBackground(pass);
            if (healing && pass.Saved.Count > 0 && loggedCameras.Add(camera))
                Diagnostics.Write("FOCUS BACKDROP camera=" + camera.name + " masks=" + pass.Saved.Count +
                    " capture=" + (bool)pass.Background.Camera + " " + NativeBackground.Describe() +
                    " blurred_copy=" + pass.CompositeAttached +
                    " native_background_blur=preserved native heal FX retained");
        }
        catch (Exception e) { AfterCamera(camera); Diagnostics.Throttled("FOCUS BACKDROP", e); }
    }
}
