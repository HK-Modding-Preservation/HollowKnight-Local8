using System;
using System.Reflection;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace KO.HollowKnight8;

// Reduce real full-screen rendering work. Do not alter the camera viewport,
// colour grading, brightness, healing particles, collision or input cadence.
internal static class NativeRenderBudget
{
    private static readonly FieldInfo BloomResolution = Reflect.Field(typeof(BloomOptimized), "resolution");
    private static readonly FieldInfo BlurTexture = Reflect.Field(typeof(LightBlurredBackground), "renderTexture");
    private static readonly MethodInfo ResizeBlur = Reflect.Method(typeof(LightBlurredBackground), "OnCameraAspectChanged");
    private static Camera camera;
    private static BloomOptimized bloom;
    private static FastNoise noise;
    private static LightBlurredBackground blur;
    private static int originalIterations, appliedIterations, originalHeight, appliedHeight, originalPasses, appliedPasses;
    private static object originalResolution;
    private static bool originalNoise;
    private static bool originalBloomEnabled, appliedBloomEnabled, bloomTouched;
    private static ShaderQualities quality;
    private static int frame = -1;

    internal static void Reset()
    {
        if (bloom)
        {
            if (bloomTouched && bloom.enabled == appliedBloomEnabled) bloom.enabled = originalBloomEnabled;
            if (bloom.blurIterations == appliedIterations) bloom.blurIterations = originalIterations;
            if (BloomResolution != null && Equals(BloomResolution.GetValue(bloom), BloomOptimized.Resolution.Low))
                BloomResolution.SetValue(bloom, originalResolution);
        }
        if (noise && !noise.enabled) noise.enabled = originalNoise;
        if (blur)
        {
            if (blur.PassGroupCount == appliedPasses) blur.PassGroupCount = originalPasses;
            if (blur.RenderTextureHeight == appliedHeight)
            {
                blur.RenderTextureHeight = originalHeight;
                RefreshTexture(blur);
            }
        }
        camera = null; bloom = null; noise = null; blur = null; originalResolution = null; frame = -1; bloomTouched = false;
    }
    private static void RefreshTexture(LightBlurredBackground background)
    {
        if (!background || !background.enabled || !background.gameObject.activeInHierarchy ||
            BlurTexture == null || ResizeBlur == null) return;
        var texture = BlurTexture.GetValue(background) as RenderTexture;
        if (texture && texture.height != background.RenderTextureHeight)
            ResizeBlur.Invoke(background, new object[] { ForceCameraAspect.CurrentViewportAspect });
    }
    internal static void BeforeCamera(Camera rendering)
    {
        var runtime = Plugin.Self; var session = runtime == null ? null : runtime.Session;
        var gm = GameManager.instance; var cameras = GameCameras.instance;
        bool active = PerformanceTuning.Enabled && runtime.Enabled.Value && session != null && session.Active &&
            gm && gm.IsGameplayScene() && !gm.IsLoadingSceneTransition && cameras && cameras.mainCamera;
        if (!active)
        {
            if (camera || blur || bloom || noise) Reset();
            return;
        }
        try
        {
            if (camera != cameras.mainCamera)
            {
                Reset(); camera = cameras.mainCamera;
                bloom = camera.GetComponent<BloomOptimized>(); noise = camera.GetComponent<FastNoise>();
                if (bloom)
                {
                    originalIterations = bloom.blurIterations;
                    originalBloomEnabled = bloom.enabled;
                    originalResolution = BloomResolution == null ? null : BloomResolution.GetValue(bloom);
                }
                if (noise) originalNoise = noise.enabled;
                Diagnostics.Write("PERF graphics detail=" + PerformanceTuning.Detail + " bloom=" + PerformanceTuning.Bloom);
            }
            var background = NativeBackground.Controller;
            if (blur != background)
            {
                // Camera/component replacement also occurs after graphics-quality changes.
                if (blur)
                {
                    if (blur.PassGroupCount == appliedPasses) blur.PassGroupCount = originalPasses;
                    if (blur.RenderTextureHeight == appliedHeight) { blur.RenderTextureHeight = originalHeight; RefreshTexture(blur); }
                }
                blur = background;
                if (blur)
                {
                    originalHeight = blur.RenderTextureHeight; originalPasses = blur.PassGroupCount;
                    quality = gm.gameSettings.shaderQuality;
                }
                frame = -1;
            }
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            int detail = PerformanceTuning.Detail, bloomLevel = PerformanceTuning.Bloom;
            if (bloom && bloomLevel > 0)
            {
                if (bloomTouched && bloom.enabled != appliedBloomEnabled) originalBloomEnabled = bloom.enabled;
                appliedBloomEnabled = bloomLevel == 2 ? false : originalBloomEnabled;
                bloomTouched = true; bloom.enabled = appliedBloomEnabled;
                if (bloomLevel == 1)
                {
                    if (bloom.blurIterations != appliedIterations && bloom.blurIterations != originalIterations)
                        originalIterations = bloom.blurIterations;
                    appliedIterations = Math.Min(1, originalIterations);
                    bloom.blurIterations = appliedIterations;
                    if (BloomResolution != null)
                    {
                        var value = BloomResolution.GetValue(bloom);
                        if (!Equals(value, BloomOptimized.Resolution.Low)) originalResolution = value;
                        BloomResolution.SetValue(bloom, BloomOptimized.Resolution.Low);
                    }
                }
            }
            if (noise && detail > 0) { if (noise.enabled) originalNoise = true; noise.enabled = false; }
            if (blur && detail > 0)
            {
                if (quality != gm.gameSettings.shaderQuality)
                {
                    quality = gm.gameSettings.shaderQuality;
                    originalPasses = quality == ShaderQualities.Low ? 1 : 2;
                }
                else if (blur.PassGroupCount != appliedPasses && blur.PassGroupCount != originalPasses)
                    originalPasses = blur.PassGroupCount;
                if (blur.RenderTextureHeight != appliedHeight && blur.RenderTextureHeight != originalHeight)
                    originalHeight = blur.RenderTextureHeight;
                appliedPasses = Math.Min(1, originalPasses);
                appliedHeight = Math.Min(originalHeight, Math.Max(128, originalHeight / (detail == 2 ? 4 : 2)));
                blur.PassGroupCount = appliedPasses; blur.RenderTextureHeight = appliedHeight;
                RefreshTexture(blur);
            }
        }
        catch (Exception e) { Reset(); Diagnostics.Throttled("PERF graphics", e); }
    }
}
