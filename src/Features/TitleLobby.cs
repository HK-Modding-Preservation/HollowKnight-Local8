using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class TitleLobby
{
    internal static bool Visible
    {
        get
        {
            var gm = GameManager.instance;
            return gm && !gm.IsLoadingSceneTransition && (gm.IsMenuScene() || gm.IsTitleScreenScene());
        }
    }
    internal static void Tick(Local8Runtime runtime)
    {
        LobbyMenuInput.Sync(runtime.Panel && Visible);
        if (!runtime.Enabled.Value || !Visible || !Application.isFocused || runtime.Session == null || runtime.Session.Primary != null) return;
        var roster = runtime.Lobby;
        roster.Refresh();
        if (MenuInput.Waiting || ActionKeys.Capturing != 0) return;
        var actions = GameManager.instance.inputHandler?.inputActions;
        // A connected/active pad alone never claims P1. The first join/confirm does.
        bool keyboard = KeypadInput.Pressed(KeyCode.Return) || KeypadInput.Pressed(KeyCode.Space) ||
            actions != null && actions.menuSubmit.WasPressed && actions.menuSubmit.LastInputType == BindingSourceType.KeyBindingSource;
        if (roster.Count == 0 && !keyboard && actions != null && actions.menuSubmit.WasPressed &&
            (actions.menuSubmit.LastInputType == BindingSourceType.DeviceBindingSource || actions.menuSubmit.LastInputType == BindingSourceType.UnknownDeviceBindingSource))
            roster.Join(actions.ActiveDevice);
        roster.Poll(keyboard);
    }
    internal static void Entry(Local8Runtime runtime, float width, float height)
    {
        if (!Visible || runtime.Panel) return;
        float w = Mathf.Min(310f, width - 36f), x = width - w - 18f;
        if (MenuTheme.Button(new Rect(x, height - 87f, w, 39f), UiLocalization.Get("lobby.settings"), MenuTheme.ButtonStyle))
            runtime.SetPanel(true);
        MenuTheme.Centered(new Rect(x, height - 44f, w, 25f),
            UiLocalization.Get("lobby.shortcut").Replace("{key}", MenuInput.Label), MenuTheme.Muted, 13);
    }
    internal static void Players(Local8Runtime runtime, float width, ref float row)
    {
        var roster = runtime.Lobby;
        MenuTheme.Section(UiLocalization.Get("lobby.title"), width, ref row, 0);
        row += MenuTheme.Paragraph(5, row, width - 10, UiLocalization.Get("lobby.join_hint"), MenuTheme.Paper, 15) + 12;
        MenuTheme.Section(UiLocalization.Get("input.p1_title"), width, ref row, 0);
        if (MenuTheme.Choice(new Rect(5, row, width - 10, 35),
            new GUIContent(UiLocalization.Get("input.keyboard"), UiLocalization.Get("input.keyboard_tip")),
            roster.Primary != null && Controls.IsKeyboard(roster.Primary.Device)))
            roster.AssignPrimary(InputDevice.Null);
        row += 43;
        foreach (var device in Controls.Devices())
        {
            if (MenuTheme.Choice(new Rect(5, row, width - 10, 35),
                new GUIContent(UiLocalization.Get("common.assign_p1") + "  ·  " + Controls.Label(device), UiLocalization.Get("input.assign_tip")),
                roster.Primary != null && roster.Primary.Device == device))
                roster.AssignPrimary(device);
            row += 43;
        }
        MenuTheme.Section(UiLocalization.Get("players.title"), width, ref row, 0);
        foreach (var member in roster.Members)
        {
            int index = roster.Members.IndexOf(member);
            var tint = CoopSession.Colors[index];
            MenuTheme.DeviceGlyph(new Rect(12, row + 8, 24, 24), Controls.IsKeyboard(member.Device), tint);
            float height = MenuTheme.Paragraph(48, row + 4, width - 58, "P" + (index + 1) + "  ·  " + member.Name +
                (member.Connected ? "" : UiLocalization.Get("players.disconnected")), tint, 15);
            row += Mathf.Max(47, height + 12);
        }
        bool enabled = GUI.enabled;
        GUI.enabled = enabled && roster.Count < 8 && !roster.Contains(InputDevice.Null);
        if (MenuTheme.Button(new Rect(5, row, width - 10, 38), UiLocalization.Get("lobby.keyboard"), MenuTheme.ButtonStyle))
            roster.Join(InputDevice.Null);
        GUI.enabled = enabled && roster.Count < 8;
        row += 46;
        foreach (var device in Controls.Devices())
        {
            if (roster.Contains(device)) continue;
            if (MenuTheme.Button(new Rect(5, row, width - 10, 38), UiLocalization.Get("action.join") + "  ·  " + Controls.Label(device), MenuTheme.ButtonStyle))
                roster.Join(device);
            row += 46;
        }
        GUI.enabled = enabled;
        row += MenuTheme.Paragraph(5, row, width - 10, UiLocalization.Get("lobby.ready_hint"), MenuTheme.Muted) + 10;
    }
}
