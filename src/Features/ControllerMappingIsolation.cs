using System;
using System.Collections;
using GlobalEnums;
using InControl;

namespace KO.HollowKnight8;

// Native configuration belongs to the original default-backed action set.
// Clearing/resetting filtered player actions destroys their regular bindings;
// delayed UI setup can also add another pad's bindings to a keyboard player.
internal static class ControllerMappingIsolation
{
    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.InputHandler.MapControllerButtons += Remap;
        On.InputHandler.SetActiveGamepadType += Activate;
        On.InputHandler.ResetDefaultKeyBindings += ResetKeys;
        On.InputHandler.ResetDefaultControllerButtonBindings += ResetButtons;
        On.InputHandler.RemapUIButtons += ResetUi;
        On.InputHandler.SendKeyBindingsToGameSettings += SaveKeys;
        On.InputHandler.SendButtonBindingsToGameSettings += SaveButtons;
        On.InputHandler.SetupGamepadUIInputActions += SetupUi;
        On.InputHandler.RemoveGamepadUIInputActions += RemoveUi;
        On.InputHandler.ActionButtonToPlayerAction += Action;
        On.InputHandler.GetKeyBindingForAction += Key;
        On.InputHandler.GetButtonBindingForAction += Button;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.InputHandler.MapControllerButtons -= Remap;
        On.InputHandler.SetActiveGamepadType -= Activate;
        On.InputHandler.ResetDefaultKeyBindings -= ResetKeys;
        On.InputHandler.ResetDefaultControllerButtonBindings -= ResetButtons;
        On.InputHandler.RemapUIButtons -= ResetUi;
        On.InputHandler.SendKeyBindingsToGameSettings -= SaveKeys;
        On.InputHandler.SendButtonBindingsToGameSettings -= SaveButtons;
        On.InputHandler.SetupGamepadUIInputActions -= SetupUi;
        On.InputHandler.RemoveGamepadUIInputActions -= RemoveUi;
        On.InputHandler.ActionButtonToPlayerAction -= Action;
        On.InputHandler.GetKeyBindingForAction -= Key;
        On.InputHandler.GetButtonBindingForAction -= Button;
    }

    private readonly struct Scope : IDisposable
    {
        private readonly InputHandler input;
        private readonly HeroActions previous;
        private readonly HeroActions template;

        internal Scope(InputHandler input)
        {
            this.input = input;
            previous = input.inputActions;
            var session = Plugin.Self?.Session;
            var game = GameManager.instance;
            template = session?.Primary != null && game && game.inputHandler == input ? session.OriginalActions : null;
            if (template != null) input.inputActions = template;
        }
        internal PlayerAction Map(PlayerAction action)
            => template != null && action != null && previous != null && previous.Actions.Contains(action)
                ? template.GetPlayerActionByName(action.Name) ?? action : action;
        public void Dispose() { input.inputActions = previous; }
    }

    private static void Remap(On.InputHandler.orig_MapControllerButtons orig, InputHandler input, GamepadType type)
    { using (new Scope(input)) orig(input, type); }
    private static void Activate(On.InputHandler.orig_SetActiveGamepadType orig, InputHandler input, InputDevice device)
    { using (new Scope(input)) orig(input, device); }
    private static void ResetKeys(On.InputHandler.orig_ResetDefaultKeyBindings orig, InputHandler input)
    { using (new Scope(input)) orig(input); }
    private static void ResetButtons(On.InputHandler.orig_ResetDefaultControllerButtonBindings orig, InputHandler input)
    { using (new Scope(input)) orig(input); }
    private static void ResetUi(On.InputHandler.orig_RemapUIButtons orig, InputHandler input)
    { using (new Scope(input)) orig(input); }
    private static void SaveKeys(On.InputHandler.orig_SendKeyBindingsToGameSettings orig, InputHandler input)
    { using (new Scope(input)) orig(input); }
    private static void SaveButtons(On.InputHandler.orig_SendButtonBindingsToGameSettings orig, InputHandler input)
    { using (new Scope(input)) orig(input); }
    private static void RemoveUi(On.InputHandler.orig_RemoveGamepadUIInputActions orig, InputHandler input)
    { using (new Scope(input)) orig(input); }
    private static PlayerAction Action(On.InputHandler.orig_ActionButtonToPlayerAction orig, InputHandler input, HeroActionButton button)
    { using (new Scope(input)) return orig(input, button); }
    private static InputHandler.KeyOrMouseBinding Key(On.InputHandler.orig_GetKeyBindingForAction orig, InputHandler input, PlayerAction action)
    { using (var scope = new Scope(input)) return orig(input, scope.Map(action)); }
    private static InputControlType Button(On.InputHandler.orig_GetButtonBindingForAction orig, InputHandler input, PlayerAction action)
    { using (var scope = new Scope(input)) return orig(input, scope.Map(action)); }

    private static IEnumerator SetupUi(On.InputHandler.orig_SetupGamepadUIInputActions orig, InputHandler input)
    {
        IEnumerator body;
        using (new Scope(input)) body = orig(input);
        return Steps(input, body);
    }
    private static IEnumerator Steps(InputHandler input, IEnumerator body)
    {
        if (body == null) yield break;
        try
        {
            while (true)
            {
                bool next; object current;
                // Never retain a changed global input handler across a yield.
                using (new Scope(input))
                {
                    next = body.MoveNext();
                    current = next ? body.Current : null;
                }
                if (!next) yield break;
                yield return current;
            }
        }
        finally
        {
            using (new Scope(input)) (body as IDisposable)?.Dispose();
        }
    }
}
