using UnityEngine;

namespace KO.HollowKnight8;

internal static class LightProjection
{
    // Recomputed for each mesh rebuild, never retained across moving cameras.
    // All players and triangles use the same projection of a source vertex.
    internal static void Prepare(Camera camera, Transform source, Vector2[] vertices,
        Vector2[] uv, bool flipX, bool flipY, LightVertex[] projected)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 local = vertices[i];
            if (flipX) local.x = -local.x;
            if (flipY) local.y = -local.y;
            Vector3 screen = camera.WorldToViewportPoint(source.TransformPoint(local));
            projected[i] = new LightVertex(screen.x, screen.y, screen.z, uv[i].x, uv[i].y);
        }
    }
}
