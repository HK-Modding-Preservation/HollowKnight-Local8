using System;
using UnityEngine;

namespace KO.HollowKnight8;

// Reuse native particle artwork in clean emitters. Never clone acquisition
// objects: they can carry lights, controllers, sound or shared-scene events.
internal sealed class AcquisitionParticles : IDisposable
{
    private GameObject root;
    private ParticleSystem particles;
    private readonly ParticleSystem.Particle[] motes = new ParticleSystem.Particle[72];
    private float emit;
    private static ParticleSystem dream;

    internal static ParticleSystem Dream(HeroController hero)
    {
        if (!dream)
        {
            int best = 39;
            foreach (var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>())
            {
                if (!Usable(ps)) continue;
                int score = NativeDreamFx.Score(ps);
                if (score > best) { best = score; dream = ps; }
            }
        }
        return dream ? dream : RoleAura.FindFocusParticles(hero);
    }

    internal static bool Usable(ParticleSystem ps)
        => ps && ps.GetComponent<ParticleSystemRenderer>() && ps.GetComponent<ParticleSystemRenderer>().sharedMaterial;

    internal static AcquisitionParticles Create(PlayerSlot p, ParticleSystem native, bool stream)
    {
        if (!p.Hero || !Usable(native)) return null;
        var fx = new AcquisitionParticles();
        try
        {
            fx.root = new GameObject("Local8 Shared Acquisition " + (stream ? "Essence" : "Burst") + " P" + (p.Index + 1));
            fx.root.layer = p.Hero.gameObject.layer;
            fx.particles = fx.root.AddComponent<ParticleSystem>();
            fx.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = fx.particles.main;
            main.playOnAwake = false; main.loop = true; main.maxParticles = 72;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, 1.1f);
            main.startSpeed = 0; main.gravityModifier = 0;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSize = stream ? new ParticleSystem.MinMaxCurve(.18f, .32f) : native.main.startSize;
            if (!stream)
            {
                float scale = Mathf.Max(Mathf.Abs(native.transform.lossyScale.x), Mathf.Abs(native.transform.lossyScale.y));
                main.startSizeMultiplier = Mathf.Clamp(native.main.startSizeMultiplier * scale, .1f, 1.3f);
                main.startRotation = native.main.startRotation;
            }
            main.startColor = stream ? new ParticleSystem.MinMaxGradient(Color.white) : native.main.startColor;
            var emission = fx.particles.emission; emission.enabled = false;
            var shape = fx.particles.shape; shape.enabled = false;
            var lifetime = fx.particles.colorOverLifetime; lifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.85f, .12f), new GradientAlphaKey(.7f, .7f), new GradientAlphaKey(0, 1) });
            lifetime.color = gradient;
            var renderer = fx.particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterials = native.GetComponent<ParticleSystemRenderer>().sharedMaterials;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var body = p.SkinRenderer ? p.SkinRenderer : p.Hero.GetComponent<Renderer>();
            if (body) { renderer.sortingLayerID = body.sortingLayerID; renderer.sortingOrder = body.sortingOrder + 1; }
            var sheet = native.textureSheetAnimation;
            if (sheet.enabled)
            {
                var copy = fx.particles.textureSheetAnimation;
                copy.enabled = true; copy.mode = sheet.mode; copy.animation = sheet.animation;
                if (sheet.mode == ParticleSystemAnimationMode.Sprites)
                    for (int i = 0; i < sheet.spriteCount; i++) copy.AddSprite(sheet.GetSprite(i));
                else { copy.numTilesX = sheet.numTilesX; copy.numTilesY = sheet.numTilesY; }
                copy.frameOverTime = sheet.frameOverTime; copy.startFrame = sheet.startFrame; copy.cycleCount = sheet.cycleCount;
            }
            fx.particles.Play();
            return fx;
        }
        catch { fx.Dispose(); throw; }
    }

    internal void Stream(Vector3 from, Vector3 target, float elapsed)
    {
        if (!particles) return;
        float dt = Mathf.Min(Mathf.Max(0, Time.unscaledDeltaTime), .06f);
        if (elapsed < .9f)
        {
            emit += dt * 25;
            int count = Mathf.Min(3, Mathf.FloorToInt(emit)); emit -= count;
            for (int i = 0; i < count; i++)
            {
                var ep = new ParticleSystem.EmitParams();
                ep.position = from + new Vector3(UnityEngine.Random.Range(-.18f, .18f), UnityEngine.Random.Range(-.15f, .3f), -.1f);
                ep.velocity = Vector3.zero; ep.startLifetime = AcquisitionRules.TravelSeconds;
                particles.Emit(ep, 1);
            }
        }
        int live = particles.GetParticles(motes);
        for (int i = 0; i < live; i++)
        {
            var mote = motes[i]; Vector3 delta = target - mote.position;
            float step = Mathf.Clamp01(dt / Mathf.Max(.04f, mote.remainingLifetime - .07f));
            Vector3 curl = new Vector3(-delta.y, delta.x, 0).normalized * Mathf.Sin((mote.startLifetime - mote.remainingLifetime) * 9 + i) * .09f * dt;
            mote.position = Vector3.Lerp(mote.position, target, step) + curl; mote.velocity = Vector3.zero;
            if (delta.sqrMagnitude < .035f) mote.remainingLifetime = Mathf.Min(.08f, mote.remainingLifetime);
            motes[i] = mote;
        }
        particles.SetParticles(motes, live);
    }

    internal void Burst(Vector3 at)
    {
        if (!particles) return;
        for (int i = 0; i < 24; i++)
        {
            float angle = UnityEngine.Random.Range(0, Mathf.PI * 2);
            var ep = new ParticleSystem.EmitParams();
            ep.position = at + new Vector3(Mathf.Cos(angle) * .55f, Mathf.Sin(angle) * .85f, -.12f);
            ep.velocity = new Vector3(Mathf.Cos(angle) * .32f, .35f + Mathf.Sin(angle) * .22f, 0);
            particles.Emit(ep, 1);
        }
    }

    public void Dispose()
    {
        if (root) UnityEngine.Object.Destroy(root);
        root = null; particles = null;
    }
    internal static void Reset() { dream = null; }
}
