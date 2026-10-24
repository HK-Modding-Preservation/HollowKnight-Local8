using System.Collections.Generic;
using System.Linq;
using InControl;

namespace KO.HollowKnight8;

// Menu reservations contain devices only; actors and action sets start in a save.
internal sealed class LobbyRoster
{
    internal sealed class Member
    {
        internal InputDevice Device;
        internal string Key;
        internal bool Connected => Device == InputDevice.Null || Device != null && Device.IsAttached;
        internal string Name => Controls.IsKeyboard(Device) ? UiLocalization.Get("input.keyboard") : Controls.Label(Device);
    }
    internal readonly List<Member> Members = new List<Member>();
    internal int Count => Members.Count;
    internal Member Primary => Members.Count == 0 ? null : Members[0];
    private bool applied;

    internal bool Contains(InputDevice device) => Members.Any(m => m.Device == device || m.Key == Controls.Key(device));
    internal void Poll(bool keyboardPressed)
    {
        if (Count == 0 && keyboardPressed) Join(InputDevice.Null);
        foreach (InputDevice device in Controls.Devices())
            if (Controls.JoinPressed(device) || Count == 0 && device.Action1.WasPressed) Join(device);
    }
    internal bool Join(InputDevice device)
    {
        if (applied || device == null || device != InputDevice.Null && (!device.IsAttached || device.Passive) ||
            Count >= 8 || Contains(device)) return false;
        Members.Add(new Member { Device = device, Key = Controls.Key(device) });
        return true;
    }
    internal bool AssignPrimary(InputDevice device)
    {
        if (applied || device == null || device != InputDevice.Null && (!device.IsAttached || device.Passive)) return false;
        if (Count == 0) return Join(device);
        string key = Controls.Key(device);
        Member other = Members.Skip(1).FirstOrDefault(m => m.Device == device || m.Key == key);
        if (other != null) { other.Device = Primary.Device; other.Key = Primary.Key; }
        Primary.Device = device; Primary.Key = key;
        return true;
    }
    internal void Refresh()
    {
        foreach (Member member in Members)
        {
            if (member.Connected) continue;
            InputDevice replacement = Controls.Find(member.Key);
            if (replacement != null && !Members.Any(m => m != member && m.Device == replacement)) member.Device = replacement;
        }
    }
    internal void JoinFirstAvailable()
    {
        InputDevice device = Controls.Devices().FirstOrDefault(d => !Contains(d));
        if (device != null) Join(device);
        else if (!Contains(InputDevice.Null)) Join(InputDevice.Null);
    }
    internal void Apply(CoopSession session)
    {
        if (applied || session.Primary == null || !session.CanJoin) return;
        // Reserve missing pads too: their slots stay blocked until reconnection.
        for (int i = 1; i < Members.Count; i++) session.Join(Members[i].Device);
        applied = true;
    }
    internal void RemoveLast() { if (!applied && Count > 0) Members.RemoveAt(Count - 1); }
    internal void Clear() { Members.Clear(); applied = false; }
}
