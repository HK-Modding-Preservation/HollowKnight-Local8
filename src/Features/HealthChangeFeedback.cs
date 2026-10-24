using System;

namespace KO.HollowKnight8;

// One bounded transition per HUD owner. Reading health never changes gameplay.
internal sealed class HealthChangeFeedback
{
    internal const float Duration = .5f;
    internal int Health, Blue, Maximum;
    internal int PreviousHealth, PreviousBlue, PreviousMaximum;
    private bool initialized;
    private float changedAt = float.NegativeInfinity;

    internal void Observe(int health, int blue, int maximum, float now, bool enabled)
    {
        health = Math.Max(0, health); blue = Math.Max(0, blue); maximum = Math.Max(0, maximum);
        if (!initialized)
        {
            initialized = true;
            Health = PreviousHealth = health; Blue = PreviousBlue = blue; Maximum = PreviousMaximum = maximum;
            changedAt = float.NegativeInfinity;
            return;
        }
        if (Health != health || Blue != blue || Maximum != maximum)
        {
            PreviousHealth = Health; PreviousBlue = Blue; PreviousMaximum = Maximum;
            Health = health; Blue = blue; Maximum = maximum;
            changedAt = enabled ? now : float.NegativeInfinity;
        }
        if (!enabled) changedAt = float.NegativeInfinity;
    }

    internal float Fade(float now)
    {
        float t = Math.Max(0f, Math.Min(1f, (now - changedAt) / Duration));
        return 1f - t * t * (3f - 2f * t);
    }
    internal bool Filled(int mask) => mask >= 0 && (mask < Health || mask >= Maximum && mask < Maximum + Blue);
    internal bool WasFilled(int mask) => mask >= 0 && (mask < PreviousHealth || mask >= PreviousMaximum && mask < PreviousMaximum + PreviousBlue);
    internal bool Lost(int mask) => WasFilled(mask) && !Filled(mask);
    internal bool Gained(int mask) => !WasFilled(mask) && Filled(mask);
    internal int Count(float now) => Math.Max(Maximum + Blue, Fade(now) > 0f ? PreviousMaximum + PreviousBlue : 0);
}
