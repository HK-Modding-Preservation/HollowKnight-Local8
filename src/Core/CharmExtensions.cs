using System;
using System.Collections.Generic;
using Modding;
using UnityEngine;

namespace KO.HollowKnight8;

/// <summary>Optional charms use their provider's standard PlayerData hooks.
/// No dependency on SFCore or a particular charm mod is required.</summary>
internal static class CharmExtensions
{
    internal sealed class Entry
    {
        internal readonly int Id;
        internal readonly string Equipped, Owned;
        internal bool Valid, Value;
        internal Entry(int id) { Id = id; Equipped = "equippedCharm_" + id; Owned = "gotCharm_" + id; }
    }

    private static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
    private static Entry[] snapshot = new Entry[0];
    private static Sprite[] sprites;
    private static PlayerData cachedData;
    private static int cachedFrame = -1;
    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        ModHooks.SetPlayerBoolHook += Changed;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        ModHooks.SetPlayerBoolHook -= Changed;
        Reset();
    }

    private static bool Changed(string name, bool value)
    {
        // Native/provider changes must be visible immediately, including a
        // toggle or a nested context change in the same Unity frame.
        if (name != null && name.StartsWith("equippedCharm_", StringComparison.Ordinal))
        {
            int id;
            Entry entry;
            if (int.TryParse(name.Substring(14), out id) && entries.TryGetValue(id, out entry)) entry.Valid = false;
        }
        return value;
    }

    private static void ValidateFrame(PlayerData data)
    {
        if (ReferenceEquals(cachedData, data) && cachedFrame == Time.frameCount) return;
        cachedData = data;
        cachedFrame = Time.frameCount;
        foreach (Entry entry in snapshot) entry.Valid = false;
    }

    internal static bool Equipped(PlayerData data, Entry entry)
    {
        ValidateFrame(data);
        if (!entry.Valid)
        {
            entry.Value = data.GetBool(entry.Equipped);
            entry.Valid = true;
        }
        return entry.Value;
    }

    internal static void Equip(PlayerData data, Entry entry, bool value)
    {
        if (Equipped(data, entry) == value) return;
        data.SetBool(entry.Equipped, value);
        // Read the final provider result: another hook can reject/change the
        // request. Never assume that our requested value is the stored value.
        entry.Valid = false;
        Equipped(data, entry);
    }

    internal static Entry[] Entries { get { Discover(); return snapshot; } }
    internal static bool Contains(int id) { return id > 0 && (id <= 40 || entries.ContainsKey(id)); }
    internal static void Register(int id)
    {
        if (id <= 40 || entries.ContainsKey(id)) return;
        entries.Add(id, new Entry(id));
        snapshot = new Entry[entries.Count];
        entries.Values.CopyTo(snapshot, 0);
        Array.Sort(snapshot, (a, b) => a.Id.CompareTo(b.Id));
    }

    internal static void Discover()
    {
        CharmIconList icons = CharmIconList.Instance;
        if (!icons || icons.spriteList == null || ReferenceEquals(sprites, icons.spriteList)) return;
        sprites = icons.spriteList;
        // SFCore extends the native, ID-indexed sprite list. Empty padding is
        // not a registered charm and must never be sent to another mod's hooks.
        for (int id = 41; id < sprites.Length; id++) if (sprites[id]) Register(id);
    }

    internal static bool Owned(PlayerData data, int id)
    {
        if (data == null || id <= 0) return false;
        return data.GetBool("gotCharm_" + id);
    }

    internal static int[] Ids()
    {
        Entry[] extra = Entries;
        int[] ids = new int[40 + extra.Length];
        for (int i = 0; i < 40; i++) ids[i] = i + 1;
        for (int i = 0; i < extra.Length; i++) ids[40 + i] = extra[i].Id;
        return ids;
    }

    internal static bool IsRole(int cursor) { return cursor < 0; }
    internal static int Role(int index) { return -1 - index; }

    internal static void Reset() { entries.Clear(); snapshot = new Entry[0]; sprites = null; cachedData = null; cachedFrame = -1; }
}
