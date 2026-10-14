using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

internal static class CharmGridRules
{
    private sealed class Row
    {
        internal readonly List<CharmCell> Cells = new List<CharmCell>();
        internal double Y;
        internal bool Native;
        internal void Add(CharmCell c) { Y = (Y * Cells.Count + c.Y) / (Cells.Count + 1); Cells.Add(c); }
    }

    internal static int Next(int current, int dx, int dy, IEnumerable<CharmCell> cells)
    {
        List<CharmCell> native = new List<CharmCell>(), extra = new List<CharmCell>();
        HashSet<int> seen = new HashSet<int>();
        foreach (CharmCell c in cells)
        {
            if (c.Id < 1 || double.IsNaN(c.X) || double.IsNaN(c.Y) || double.IsInfinity(c.X) || double.IsInfinity(c.Y) || !seen.Add(c.Id)) continue;
            (c.Id <= 40 ? native : extra).Add(c);
        }
        Comparison<CharmCell> order = (a, b) => { int y = b.Y.CompareTo(a.Y); return y == 0 ? a.X.CompareTo(b.X) : y; };
        native.Sort(order);
        List<Row> rows = new List<Row>();
        double tolerance = 0.0001;
        if (native.Count == 40)
        {
            // Keep the original four staggered ten-column rows and their
            // column-based navigation exactly when no mods add rows.
            for (int i = 0; i < 4; i++)
            {
                Row row = new Row { Native = true };
                for (int j = 0; j < 10; j++) row.Add(native[i * 10 + j]);
                rows.Add(row);
            }
            double gap = double.MaxValue;
            for (int i = 1; i < rows.Count; i++) gap = Math.Min(gap, Math.Abs(rows[i - 1].Y - rows[i].Y));
            if (gap > 0 && gap < double.MaxValue) tolerance = gap * 0.45;
        }
        else { extra.AddRange(native); }
        extra.Sort(order);
        foreach (CharmCell c in extra)
        {
            Row nearest = null;
            double distance = tolerance;
            foreach (Row row in rows)
            {
                double delta = Math.Abs(row.Y - c.Y);
                if (delta <= distance) { nearest = row; distance = delta; }
            }
            if (nearest == null) { nearest = new Row(); rows.Add(nearest); }
            nearest.Native = false;
            nearest.Add(c);
        }
        rows.Sort((a, b) => b.Y.CompareTo(a.Y));
        int sourceRow = -1, sourceColumn = -1;
        for (int r = 0; r < rows.Count; r++)
        {
            rows[r].Cells.Sort((a, b) => a.X.CompareTo(b.X));
            int column = rows[r].Cells.FindIndex(c => c.Id == current);
            if (column >= 0) { sourceRow = r; sourceColumn = column; }
        }
        if (sourceRow < 0) return current;
        Row source = rows[sourceRow];
        if (dx != 0)
        {
            int step = Math.Sign(dx);
            for (int column = sourceColumn + step; column >= 0 && column < source.Cells.Count; column += step)
                if (source.Cells[column].Owned) return source.Cells[column].Id;
        }
        if (dy != 0)
        {
            int step = -Math.Sign(dy);
            for (int r = sourceRow + step; r >= 0 && r < rows.Count; r += step)
            {
                Row target = rows[r];
                int selected = -1;
                double best = double.MaxValue;
                for (int c = 0; c < target.Cells.Count; c++)
                {
                    if (!target.Cells[c].Owned) continue;
                    double distance = source.Native && target.Native ? Math.Abs(c - sourceColumn) : Math.Abs(target.Cells[c].X - source.Cells[sourceColumn].X);
                    if (distance < best) { best = distance; selected = c; }
                }
                if (selected >= 0) return target.Cells[selected].Id;
            }
        }
        return current;
    }
}
