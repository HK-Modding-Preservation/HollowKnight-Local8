using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

// The game's light sprite has a tight polygon, not a rectangular quad. Its
// texture can still carry alpha at that polygon's rim. Fade only those outside
// edges; internal triangulation and camera clipping must never become seams.
internal sealed class LightEdgeFade
{
    private int[] starts = Array.Empty<int>();
    private int[] ends = Array.Empty<int>();
    private struct Edge
    {
        internal double X, Y, Dx, Dy, Length;
    }
    private Edge[] edges = Array.Empty<Edge>();
    private bool projected;
    private double feather = 1;
    private bool round;
    private double centerX,centerY,radiusX,radiusY,inscribed;

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
        edges = new Edge[boundary.Count];
        projected = false;
        for (int i = 0; i < boundary.Count; i++)
        {
            starts[i] = (int)(boundary[i] >> 32);
            ends[i] = (int)(uint)boundary[i];
        }
    }

    internal void Project(LightPoint[] projected,bool round = false)
    {
        this.projected = projected.Length > 0;
        if (!this.projected) return;
        // Segment geometry is identical for every output vertex this frame.
        // Cache it once, preserving the native polygon's outer edges only.
        for (int i = 0; i < edges.Length; i++)
        {
            LightPoint a = projected[starts[i]], b = projected[ends[i]];
            double dx = b.X - a.X, dy = b.Y - a.Y;
            edges[i] = new Edge { X = a.X, Y = a.Y, Dx = dx, Dy = dy, Length = dx * dx + dy * dy };
        }
        LightPoint[] points = projected;
        double minX = points[0].X, maxX = minX, minY = points[0].Y, maxY = minY;
        foreach (var p in points)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
        }
        feather = Math.Max(.7, Math.Min(2.5, Math.Min(maxX - minX, maxY - minY) * .12));
        this.round=round;
        centerX=(minX+maxX)*.5;centerY=(minY+maxY)*.5;
        radiusX=Math.Max(.001,(maxX-minX)*.5);radiusY=Math.Max(.001,(maxY-minY)*.5);
        inscribed=1;
        if(round)foreach(var edge in edges)
        {
            double ax=(edge.X-centerX)/radiusX,ay=(edge.Y-centerY)/radiusY;
            double dx=edge.Dx/radiusX,dy=edge.Dy/radiusY,length=dx*dx+dy*dy;
            double t=length>1e-16?Math.Max(0,Math.Min(1,-(ax*dx+ay*dy)/length)):0;
            inscribed=Math.Min(inscribed,Math.Sqrt((ax+dx*t)*(ax+dx*t)+(ay+dy*t)*(ay+dy*t)));
        }
        inscribed=Math.Max(.01,inscribed);
    }

    internal double At(double x, double y)
    {
        if (!projected || edges.Length == 0) return 1;
        if(round)
        {
            // Fit a soft ellipse inside the native tight sprite: its texture and
            // shader stay intact, but the polygon's straight rim is never visible.
            double dx=(x-centerX)/radiusX,dy=(y-centerY)/radiusY;
            double radius=Math.Sqrt(dx*dx+dy*dy)/inscribed;
            double t=Math.Max(0,Math.Min(1,(1-radius)/.28));
            return t*t*(3-2*t);
        }
        double nearest = double.PositiveInfinity;
        for (int i = 0; i < edges.Length; i++)
        {
            Edge edge = edges[i];
            double t = edge.Length > 1e-16 ? Math.Max(0, Math.Min(1, ((x - edge.X) * edge.Dx + (y - edge.Y) * edge.Dy) / edge.Length)) : 0;
            double vx = x - edge.X - edge.Dx * t, vy = y - edge.Y - edge.Dy * t;
            nearest = Math.Min(nearest, vx * vx + vy * vy);
        }
        if (nearest >= feather * feather) return 1;
        double value = Math.Min(1, Math.Sqrt(nearest) / feather);
        return value * value * (3 - 2 * value);
    }
}
