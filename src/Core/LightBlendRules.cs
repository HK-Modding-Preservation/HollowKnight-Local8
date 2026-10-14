using System;

namespace KO.HollowKnight8;

internal static class LightBlendRules
{
    // Share one native glow's intensity at every sample, including P1. Do not
    // suppress a player's whole light merely because another player is close:
    // the ownership changes smoothly across the overlapping part of the glows.
    internal static void Fill(LightPoint[] points, int count, double x, double y,
        double feather, double[] weights)
    {
        if (count == 1)
        {
            weights[0] = 1.0;
            return;
        }

        double nearest = double.MaxValue;
        for (int i = 0; i < count; i++)
        {
            double dx = x - points[i].X;
            double dy = y - points[i].Y;
            weights[i] = dx * dx + dy * dy;
            nearest = Math.Min(nearest, weights[i]);
        }

        double width = Math.Max(0.001, feather);
        double band = width * (width + 2.0 * Math.Sqrt(nearest));
        double total = 0.0;
        for (int i = 0; i < count; i++)
        {
            double t = Math.Max(0.0, 1.0 - (weights[i] - nearest) / band);
            weights[i] = t * t * (3.0 - 2.0 * t);
            total += weights[i];
        }
        for (int i = 0; i < count; i++)
            weights[i] /= total;
    }

    internal static double Weight(LightPoint[] points, int count, int index,
        double x, double y, double feather, double[] scratch)
    {
        Fill(points, count, x, y, feather, scratch);
        return scratch[index];
    }
}
