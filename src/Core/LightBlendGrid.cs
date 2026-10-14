using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

internal struct LightWeightedVertex
{
    internal LightVertex Vertex;
    internal double Weight;

    internal LightWeightedVertex(LightVertex vertex, double weight)
    {
        Vertex = vertex;
        Weight = weight;
    }
}

// All glow sprites use the SAME screen-space triangles. Normalizing only at
// independently tessellated sprite vertices is insufficient: their different
// interpolations can still add up above one, or leave a visible diagonal seam.
// Clipping a native sprite into this field preserves its atlas UVs and depth.
internal sealed class LightBlendGrid
{
    private const float Min = -0.01f;
    private const float Span = 1.02f;
    private const int MaxPlayers = 8;
    private double[] weights = new double[0];
    private readonly double[] scratch = new double[MaxPlayers];
    private readonly LightVertex[] clipA = new LightVertex[24];
    private readonly LightVertex[] clipB = new LightVertex[24];
    private int columns, rows, count;
    private float stepX, stepY;

    internal int Columns => columns;
    internal int Rows => rows;

    internal void Prepare(LightPoint[] points, int playerCount, double viewWidth,
        double viewHeight, double feather, bool reducedDetail = false, bool smooth = false)
    {
        count = playerCount;
        double cell = Math.Max(smooth ? .55 : 1.0, feather * (smooth ? .25 : .5)) * (reducedDetail && !smooth ? 1.5 : 1.0);
        columns = Math.Max(smooth ? 32 : reducedDetail ? 12 : 16, Math.Min(smooth ? 64 : reducedDetail ? 32 : 48, (int)Math.Ceiling(viewWidth / cell)));
        rows = Math.Max(smooth ? 20 : reducedDetail ? 8 : 10, Math.Min(smooth ? 40 : reducedDetail ? 20 : 32, (int)Math.Ceiling(viewHeight / cell)));
        stepX = Span / columns;
        stepY = Span / rows;
        int required = (columns + 1) * (rows + 1) * MaxPlayers;
        if (weights.Length < required)
            weights = new double[required];
        for (int row = 0; row <= rows; row++)
        {
            for (int column = 0; column <= columns; column++)
            {
                double x = (Min + column * stepX) * viewWidth;
                double y = (Min + row * stepY) * viewHeight;
                LightBlendRules.Fill(points, count, x, y, feather, scratch);
                int offset = Offset(column, row);
                for (int player = 0; player < count; player++)
                    weights[offset + player] = scratch[player];
            }
        }
    }

    private int Offset(int column, int row)
    {
        return (row * (columns + 1) + column) * MaxPlayers;
    }

    internal void Tessellate(LightVertex a, LightVertex b, LightVertex c,
        int player, List<LightWeightedVertex> output)
    {
        int left = Cell(Math.Min(a.X, Math.Min(b.X, c.X)), stepX, columns);
        int right = Cell(Math.Max(a.X, Math.Max(b.X, c.X)), stepX, columns);
        int bottom = Cell(Math.Min(a.Y, Math.Min(b.Y, c.Y)), stepY, rows);
        int top = Cell(Math.Max(a.Y, Math.Max(b.Y, c.Y)), stepY, rows);
        for (int row = bottom; row <= top; row++)
        {
            float y0 = Min + row * stepY;
            float y1 = Min + (row + 1) * stepY;
            for (int column = left; column <= right; column++)
            {
                int offset = Offset(column, row) + player;
                double w00 = weights[offset];
                double w10 = weights[offset + MaxPlayers];
                double w01 = weights[offset + (columns + 1) * MaxPlayers];
                double w11 = weights[offset + (columns + 2) * MaxPlayers];
                // A zero field at all four corners stays zero throughout the
                // cell. Skip it before clipping, particularly in large rooms.
                if (w00 == 0.0 && w10 == 0.0 && w01 == 0.0 && w11 == 0.0)
                    continue;

                float x0 = Min + column * stepX;
                float x1 = Min + (column + 1) * stepX;
                clipA[0] = a;
                clipA[1] = b;
                clipA[2] = c;
                int n = LightGeometry.Clip(clipA, 3, clipB, 1f, 0f, x1);
                n = LightGeometry.Clip(clipB, n, clipA, -1f, 0f, -x0);
                n = LightGeometry.Clip(clipA, n, clipB, 0f, 1f, y1);
                n = LightGeometry.Clip(clipB, n, clipA, 0f, -1f, -y0);
                if (n < 3)
                    continue;

                // The shared diagonal runs from the lower left to upper right.
                float limit = stepX * y0 - stepY * x0;
                int lower = LightGeometry.Clip(clipA, n, clipB, -stepY, stepX, limit);
                Append(clipB, lower, output, x0, y0, false, w00, w10, w01, w11);
                int upper = LightGeometry.Clip(clipA, n, clipB, stepY, -stepX, -limit);
                Append(clipB, upper, output, x0, y0, true, w00, w10, w01, w11);
            }
        }
    }

    private static int Cell(float coordinate, float step, int extent)
    {
        return Math.Max(0, Math.Min(extent - 1, (int)Math.Floor((coordinate - Min) / step)));
    }

    private void Append(LightVertex[] polygon, int n, List<LightWeightedVertex> output,
        float x0, float y0, bool upper, double w00, double w10, double w01, double w11)
    {
        for (int i = 1; i < n - 1; i++)
        {
            LightVertex a = polygon[0], b = polygon[i], c = polygon[i + 1];
            double area = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            if (Math.Abs(area) < 1e-12)
                continue;
            double wa = Interpolate(a, x0, y0, upper, w00, w10, w01, w11);
            double wb = Interpolate(b, x0, y0, upper, w00, w10, w01, w11);
            double wc = Interpolate(c, x0, y0, upper, w00, w10, w01, w11);
            if (wa == 0.0 && wb == 0.0 && wc == 0.0)
                continue;
            output.Add(new LightWeightedVertex(a, wa));
            output.Add(new LightWeightedVertex(b, wb));
            output.Add(new LightWeightedVertex(c, wc));
        }
    }

    private double Interpolate(LightVertex vertex, float x0, float y0, bool upper,
        double w00, double w10, double w01, double w11)
    {
        double x = Math.Max(0.0, Math.Min(1.0, (vertex.X - x0) / stepX));
        double y = Math.Max(0.0, Math.Min(1.0, (vertex.Y - y0) / stepY));
        if (upper)
        {
            x = Math.Min(x, y); // Roundoff at the clipped diagonal.
            return w00 * (1.0 - y) + w11 * x + w01 * (y - x);
        }
        y = Math.Min(y, x);
        return w00 * (1.0 - x) + w10 * (x - y) + w11 * y;
    }
}
