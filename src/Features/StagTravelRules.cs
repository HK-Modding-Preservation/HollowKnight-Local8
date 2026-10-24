using System;

namespace KO.HollowKnight8;

internal static class StagTravelRules
{
    internal const string Cinematic = "Cinematic_Stag_travel";
    internal const string ArrivalGate = "door_stagExit";

    // A stag trip is one shared scripted journey. Deferring either leg for a
    // room vote lets the departure FSM continue while no load has begun.
    // Keep native SceneLoadInfo subclasses and their readiness callbacks intact.
    internal static bool SharedRoute(string from, string to, string gate)
    {
        return string.Equals(from, Cinematic, StringComparison.OrdinalIgnoreCase)
            || string.Equals(to, Cinematic, StringComparison.OrdinalIgnoreCase)
            || string.Equals(gate, ArrivalGate, StringComparison.OrdinalIgnoreCase);
    }
}
