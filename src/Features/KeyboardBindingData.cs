using System;
using InControl;

namespace KO.HollowKnight8;

// InControl.Load catches malformed data internally and calls Reset(), which
// empties our regular (non-default) bindings. Validate the result explicitly.
internal static class KeyboardBindingData
{
    internal static bool Valid(HeroActions actions)
    {
        if (actions == null) return false;
        foreach (var action in new[] { actions.left, actions.right, actions.up, actions.down, actions.jump, actions.attack })
            if (action == null || action.Bindings.Count == 0) return false;
        foreach (var action in actions.Actions)
            foreach (var binding in action.Bindings)
                if (!(binding is KeyBindingSource) && !(binding is MouseBindingSource)) return false;
        return true;
    }
    internal static bool TryLoad(HeroActions actions, string data)
    {
        if (actions == null || string.IsNullOrEmpty(data)) return false;
        string previous = actions.Save();
        try
        {
            actions.Load(data);
            if (!Valid(actions)) throw new FormatException("Keyboard bindings are incomplete or contain controller bindings");
            actions.ClearInputState();
            return true;
        }
        catch (Exception error)
        {
            actions.Load(previous);
            Diagnostics.Throttled("KEYBOARD rejected saved bindings; previous controls restored", error);
            return false;
        }
    }
}
