using System;

namespace KO.HollowKnight8;

// Layout is independent of game actors, camera state and input routing.
internal static class MenuLayout
{
    internal const float MaximumWidth = 980f;
    internal const float MaximumHeight = 790f;
    internal const float Header = 118f;
    internal const float Footer = 92f;
    internal static float Width(float viewport) => Math.Max(240f, Math.Min(MaximumWidth, viewport - 32f));
    internal static float Height(float viewport) => Math.Max(260f, Math.Min(MaximumHeight, viewport - 32f));
    internal static bool Wide(float width) => width >= 720f;
    internal static float Sidebar(float width) => Wide(width) ? 194f : 0f;
    internal static float BodyTop(float width) => Wide(width) ? Header : Header + 90f;
    internal static float ContentHeight(float measured, float visible) => Math.Max(visible, measured + 12f);
    internal static float ClampScroll(float y, float content, float visible) => Math.Max(0f, Math.Min(y, Math.Max(0f, content - visible)));
}
