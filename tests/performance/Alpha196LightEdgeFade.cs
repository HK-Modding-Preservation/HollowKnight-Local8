// Exact alpha196 implementation, retained only as a regression/benchmark baseline.
using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

// The game's light sprite has a tight polygon, not a rectangular quad. Its
// texture can still carry alpha at that polygon's rim. Fade only those outside
// edges; internal triangulation and camera clipping must never become seams.
internal sealed class ReferenceLightEdgeFade
{
    private int[] starts = Array.Empty<int>();
    private int[] ends = Array.Empty<int>();
    private LightPoint[] points;
    private double feather = 1;

    internal int EdgeCount => starts.Length;

    internal void Topology(ushort[] triangles)
    {
        var uses = new Dictionary<long, int>();
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                int a = triangles[i + j], b = triangles[i + (j + 1) % 3];
                if (a == b) continue;
                long key = ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
                uses.TryGetValue(key, out int count);
                uses[key] = count + 1;
            }
        }
        var boundary = new List<long>();
        foreach (var pair in uses) if (pair.Value == 1) boundary.Add(pair.Key);
        starts = new int[boundary.Count];
        ends = new int[boundary.Count];
        for (int i = 0; i < boundary.Count; i++)
        {
            starts[i] = (int)(boundary[i] >> 32);
            ends[i] = (int)(uint)boundary[i];
        }
    }

    internal void Project(LightPoint[] projected)
    {
        points = projected;
        if (points.Length == 0) return;
        double minX = points[0].X, maxX = minX, minY = points[0].Y, maxY = minY;
        foreach (var p in points)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
        }
        feather = Math.Max(.7, Math.Min(2.5, Math.Min(maxX - minX, maxY - minY) * .12));
    }

    internal double At(double x, double y)
    {
        if (points == null || starts.Length == 0) return 1;
        double nearest = double.PositiveInfinity;
        for (int i = 0; i < starts.Length; i++)
        {
            LightPoint a = points[starts[i]], b = points[ends[i]];
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double length = dx * dx + dy * dy;
            double t = length > 1e-16 ? Math.Max(0, Math.Min(1, ((x - a.X) * dx + (y - a.Y) * dy) / length)) : 0;
            double vx = x - a.X - dx * t, vy = y - a.Y - dy * t;
            nearest = Math.Min(nearest, vx * vx + vy * vy);
        }
        double value = Math.Min(1, Math.Sqrt(nearest) / feather);
        return value * value * (3 - 2 * value);
    }
}
