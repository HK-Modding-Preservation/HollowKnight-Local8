using System;

namespace KO.HollowKnight8;

// Transient UI focus, keyed by the real charm ID, never a synthetic mod/role ID.
internal sealed class EquippedCharmCursor
{
    internal bool Active;
    internal int Selected, ReturnCharm = 1;
    private int index;

    internal bool Enter(int[] ids, int catalogCharm)
    {
        if (ids == null || ids.Length == 0) return false;
        ReturnCharm = catalogCharm;
        int at = Array.IndexOf(ids, catalogCharm);
        index = at < 0 ? 0 : at;
        Selected = ids[index]; Active = true;
        return true;
    }

    internal bool Sync(int[] ids)
    {
        if (!Active) return false;
        if (ids == null || ids.Length == 0) { Clear(); return false; }
        int at = Array.IndexOf(ids, Selected);
        index = at < 0 ? Math.Min(index, ids.Length - 1) : at;
        Selected = ids[index];
        return true;
    }

    internal void Move(int direction, int columns, int[] ids)
    {
        if (!Sync(ids)) return;
        columns = Math.Max(1, columns);
        if (direction == -1) index = Math.Max(0, index - 1);
        else if (direction == 1) index = Math.Min(ids.Length - 1, index + 1);
        else if (direction == 2 && index >= columns) index -= columns;
        else if (direction == 3)
        {
            int nextRow = (index / columns + 1) * columns;
            if (nextRow >= ids.Length) { Clear(); return; }
            index = Math.Min(ids.Length - 1, index + columns);
        }
        Selected = ids[index];
    }

    internal int Index(int[] ids)
    {
        if (!Active || ids == null || ids.Length == 0) return -1;
        int at = Array.IndexOf(ids, Selected);
        return at < 0 ? Math.Min(index, ids.Length - 1) : at;
    }
    internal void Clear() { Active = false; Selected = 0; index = 0; }
}
