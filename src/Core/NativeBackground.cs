using System;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

// The vanilla background camera copies its lens only when it is created.
// Co-op changes the tk2d lens afterwards. Keep the background's framing in
// sync, but retain its own depth range, target texture and layer exclusions.
internal static class NativeBackground
{
    internal struct SavedCamera
    {
        internal Camera Camera;
        internal bool Orthographic;
        internal float Fov, Size, Near, Far;
        internal Matrix4x4 Projection, View;
        internal bool AutomaticView;
    }
    private static readonly FieldInfo Background = Reflect.Field(typeof(LightBlurredBackground), "backgroundCamera");
    private static readonly FieldInfo Scene = Reflect.Field(typeof(LightBlurredBackground), "sceneCamera");
    private static readonly FieldInfo Distant = Reflect.Field(typeof(LightBlurredBackground), "distantFarClipPlane");
    private static readonly FieldInfo Epsilon = Reflect.Field(typeof(LightBlurredBackground), "clipEpsilon");
    private static GameCameras owner;
    private static LightBlurredBackground controller;

    internal static LightBlurredBackground Controller
    {
        get
        {
            var cameras = GameCameras.instance;
            if (owner != cameras || (!controller && cameras))
            {
                owner = cameras;
                controller = cameras ? cameras.GetComponent<LightBlurredBackground>() : null;
            }
            return controller;
        }
    }
    internal static void Reset() { owner = null; controller = null; }
    internal static string Describe()
    {
        var blur = Controller;
        var scene = blur && Scene != null ? Scene.GetValue(blur) as Camera : null;
        var capture = blur && Background != null ? Background.GetValue(blur) as Camera : null;
        var plane = BlurPlane.BlurPlaneCount > 0 ? BlurPlane.GetBlurPlane(0) : null;
        return "scene_far=" + (scene ? scene.farClipPlane.ToString("G5") : "none") +
            " capture_depth=" + (capture ? capture.nearClipPlane.ToString("G5") + ".." + capture.farClipPlane.ToString("G5") : "none") +
            " planes=" + BlurPlane.BlurPlaneCount + " first=" + (plane ? plane.name + "@" + plane.PlaneZ.ToString("G5") : "none");
    }
    internal static bool TrySplit(Camera camera, out float far)
    {
        far = 0f; var blur = Controller;
        if (!blur || !blur.enabled || !blur.gameObject.activeInHierarchy || !camera || Scene == null || Distant == null ||
            (Scene.GetValue(blur) as Camera) != camera || BlurPlane.BlurPlaneCount == 0) return false;
        object value = Distant.GetValue(blur);
        if (!(value is float distance) || distance <= camera.nearClipPlane || float.IsNaN(distance) || float.IsInfinity(distance)) return false;
        far = distance;
        return true;
    }
    internal static bool TryTexture(Camera camera, out RenderTexture texture)
    {
        texture = null;
        if (!TrySplit(camera, out float far) || Background == null) return false;
        var capture = Background.GetValue(Controller) as Camera;
        if (!capture || !capture.enabled || !capture.gameObject.activeInHierarchy || capture.depth >= camera.depth)
            return false;
        var source = capture.targetTexture;
        // Respect disabled native blur, interrupted/unsupported textures and
        // other mods' camera targets. Never sample the output we are drawing to.
        if (!source || source == camera.targetTexture || source.width <= 0 || source.height <= 0 || !source.IsCreated())
            return false;
        texture = source;
        return true;
    }
    internal static Matrix4x4 Depth(Matrix4x4 lens, bool orthographic, float near, float far)
    {
        // Preserve tk2d's X/Y scale, offsets and viewport. Only the standard
        // view-space depth row changes when a pass uses another clipping range.
        float range = far - near;
        if (range <= .0001f) return lens;
        lens.m22 = orthographic ? -2f / range : -(far + near) / range;
        lens.m23 = orthographic ? -(far + near) / range : -2f * far * near / range;
        return lens;
    }
    internal static SavedCamera BeginCamera(Camera camera)
    {
        SavedCamera saved = default(SavedCamera);
        var blur = Controller;
        if (!camera || !blur || !blur.enabled || Background == null || (Background.GetValue(blur) as Camera) != camera) return saved;
        saved.Camera = camera; saved.Orthographic = camera.orthographic;
        saved.Fov = camera.fieldOfView; saved.Size = camera.orthographicSize;
        saved.Near = camera.nearClipPlane; saved.Far = camera.farClipPlane;
        saved.Projection = camera.projectionMatrix; saved.View = camera.worldToCameraMatrix;
        // Read the camera's actual automatic view instead of assuming that
        // another mod (or an outer nested pass) has no custom view matrix.
        camera.ResetWorldToCameraMatrix();
        saved.AutomaticView = saved.View.Equals(camera.worldToCameraMatrix);
        if (!saved.AutomaticView) camera.worldToCameraMatrix = saved.View;
        try { Synchronize(camera); }
        catch { RestoreCamera(saved); throw; }
        return saved;
    }
    internal static void RestoreCamera(SavedCamera saved)
    {
        Camera camera = saved.Camera;
        if (!camera) return;
        camera.orthographic = saved.Orthographic; camera.fieldOfView = saved.Fov; camera.orthographicSize = saved.Size;
        camera.nearClipPlane = saved.Near; camera.farClipPlane = saved.Far;
        camera.projectionMatrix = saved.Projection;
        if (saved.AutomaticView) camera.ResetWorldToCameraMatrix();
        else camera.worldToCameraMatrix = saved.View;
    }
    internal static void Synchronize(Camera camera)
    {
        var blur = Controller;
        if (!camera || !blur || !blur.enabled || Background == null || Scene == null || Distant == null ||
            (Background.GetValue(blur) as Camera) != camera) return;
        Camera main = Scene.GetValue(blur) as Camera;
        if (!main) return;
        float far = (float)Distant.GetValue(blur);
        float near = main.farClipPlane;
        if (!Valid(near, far))
        {
            // A temporary scene-camera far value must not empty the native
            // background capture. Recover its split from the actual native
            // plane in the view space that this pass is about to use.
            var plane = BlurPlane.ClosestBlurPlane;
            if (!plane) return;
            float epsilon = Epsilon == null ? 0f : (float)Epsilon.GetValue(blur);
            near = -main.worldToCameraMatrix.MultiplyPoint3x4(plane.transform.position).z + epsilon;
            if (!Valid(near, far)) return;
        }
        camera.orthographic = main.orthographic;
        camera.fieldOfView = main.fieldOfView;
        camera.orthographicSize = main.orthographicSize;
        camera.nearClipPlane = near;
        camera.farClipPlane = far;
        camera.worldToCameraMatrix = main.worldToCameraMatrix;
        camera.projectionMatrix = Depth(main.projectionMatrix, main.orthographic, near, far);
    }
    private static bool Valid(float near, float far) => near > 0f && far > near + .0001f &&
        !float.IsNaN(near) && !float.IsInfinity(near) && !float.IsNaN(far) && !float.IsInfinity(far);
}
