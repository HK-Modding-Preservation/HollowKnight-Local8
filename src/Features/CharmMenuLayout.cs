using UnityEngine;

namespace KO.HollowKnight8;

// The native grid keeps its position. Large parties use the unused width above
// it; their role panel starts below that strip instead of overlapping the cards.
internal static class CharmMenuLayout
{
    internal static Rect Cards(float width, float height, int players) =>
        new Rect(width * .104f, height * .197f,
            width * (players > 4 ? .823f : .478f), height * .294f);

    internal static Rect Card(Rect area, int position, int players)
    {
        int count = Mathf.Clamp(players, 1, 8);
        const float gap = 4f;
        float width = Mathf.Max(1f, (area.width - gap * (count + 1)) / count);
        return new Rect(area.x + gap + position * (width + gap), area.y + gap,
            width, Mathf.Max(1f, area.height - gap * 2f));
    }

    internal static Rect Roles(float width, float height, int players)
    {
        if (players <= 4) return new Rect(width * .610f, height * .192f, width * .317f, height * .637f);
        float top = Cards(width, height, players).yMax + height * .022f;
        return new Rect(width * .610f, top, width * .317f, height * .930f - top);
    }

    internal static int IconColumns(Rect card) =>
        Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(1f, card.width - 16f) / 36f), 1, 10);

    internal static bool Intersects(Rect a, Rect b) =>
        a.x < b.xMax && a.xMax > b.x && a.y < b.yMax && a.yMax > b.y;

    internal static Rect Feather(Rect rect, float height)
    {
        float edge = 12f * height / 900f;
        return new Rect(rect.x - edge, rect.y - edge, rect.width + edge * 2f, rect.height + edge * 2f);
    }

    internal static bool Replaced(Rect bounds, float width, float height, int players)
    {
        // Large pane ornaments/backgrounds are not native descriptions. The
        // actual selectable charm-grid hierarchy is separately protected.
        if (bounds.width >= width * .38f) return false;
        Rect description = new Rect(width * .600f, height * .170f, width * .340f, height * .680f);
        return Intersects(bounds, Feather(Cards(width, height, players), height))
            || Intersects(bounds, Feather(Roles(width, height, players), height))
            || Intersects(bounds, description);
    }
}
