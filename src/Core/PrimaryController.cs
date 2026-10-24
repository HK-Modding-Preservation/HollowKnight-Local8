using System.Collections.Generic;
using InControl;

namespace KO.HollowKnight8;

/// <summary>Choose P1's device without reserving a mandatory keyboard slot.</summary>
internal static class PrimaryController
{
    internal const string Automatic = "Auto";

    internal static string Normalize(string preference, bool legacy)
    {
        if (string.IsNullOrEmpty(preference) || preference == "FirstGamepad" ||
            (legacy && preference == "Keyboard")) return Automatic;
        return preference;
    }

    internal static InputDevice Resolve(string preference, IEnumerable<InputDevice> devices, InputDevice active)
    {
        if (preference == "Keyboard") return InputDevice.Null;
        if (preference != Automatic) return Controls.Find(preference, devices);
        foreach (InputDevice device in devices)
        {
            if (device == null || device == InputDevice.Null || !device.IsAttached || device.Passive) continue;
            if (device == active) return device;
        }
        return InputDevice.Null;
    }
}
