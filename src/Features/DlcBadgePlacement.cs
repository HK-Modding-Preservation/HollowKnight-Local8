using System.Collections.Generic;
using System;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class DlcBadgePlacement
{
    // SFCore.DlcIconHelper's DLC row: five built-in slots, ten per row.
    // Offsets are relative to Hidden_Dreams_Logo in world space, as in SFCore.
    internal static Vector2 Offset(int index)
        => new Vector2(3.1f + (index % 10 - 5) * .83f, -.16f + (index / 10) * .83f);
    internal static bool OnCustomGrid(Vector2 offset)
    {
        int col = (int)Math.Round((offset.x + 1.05f) / .83f), row = (int)Math.Round((offset.y + .16f) / .83f);
        if (col < 0 || col >= 10 || row < 0 || row * 10 + col < 5) return false;
        Vector2 cell = Offset(row * 10 + col);
        return Math.Abs(cell.x - offset.x) < .25f && Math.Abs(cell.y - offset.y) < .25f;
    }
    internal static bool Overlaps(Rect one, Rect other, float gap = 0f)
        => one.xMin - gap < other.xMax && one.xMax + gap > other.xMin &&
           one.yMin - gap < other.yMax && one.yMax + gap > other.yMin;

    internal static bool Find(Rect anchor, Rect viewport, IList<Rect> occupied, out Rect result, int registeredIcons = 0)
    {
        if (anchor.width <= 0 || anchor.height <= 0 || viewport.width < anchor.width || viewport.height < anchor.height)
        { result = default(Rect); return false; }
        for (int index = 5 + Math.Max(0, registeredIcons); index < 200; index++)
        {
            Vector2 offset = Offset(index);
            var next = new Rect(anchor.x + offset.x, anchor.y + offset.y, anchor.width, anchor.height);
            if (next.xMin < viewport.xMin || next.xMax > viewport.xMax || next.yMin < viewport.yMin || next.yMax > viewport.yMax) continue;
            bool free = true;
            foreach (var other in occupied) if (Overlaps(next, other, .005f)) { free = false; break; }
            if (free) { result = next; return true; }
        }
        result = default(Rect); return false;
    }
}
