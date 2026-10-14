using UnityEngine;

namespace KO.HollowKnight8;

internal static class CharmMenuVisibility
{
    internal static bool Visible(GameObject pane, Camera camera, InvCharmBackboard[] boards, out Vector3 anchor)
    {
        anchor = Vector3.zero;
        if (!pane || !pane.activeInHierarchy || !camera) return false;
        int count = 0;
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        if (boards != null) foreach (var board in boards)
        {
            if (!board || !board.gameObject.activeInHierarchy || board.charmNum < 1 || board.charmNum > 40) continue;
            Vector3 at = camera.WorldToViewportPoint(board.transform.position);
            if (at.z <= 0f || float.IsNaN(at.x) || float.IsNaN(at.y) || float.IsInfinity(at.x) || float.IsInfinity(at.y)) continue;
            minX = Mathf.Min(minX, at.x); maxX = Mathf.Max(maxX, at.x);
            minY = Mathf.Min(minY, at.y); maxY = Mathf.Max(maxY, at.y); count++;
        }
        // A pane's pivot is not its visible center. Prefer its actual native
        // catalog, also when the inventory changes pages or its parent moves.
        if (count >= 2 && (maxX - minX > .05f || maxY - minY > .05f))
            anchor = new Vector3((minX + maxX) * .5f, (minY + maxY) * .5f, 1f);
        else anchor = camera.WorldToViewportPoint(pane.transform.position);
        return anchor.z > 0f && anchor.x >= .05f && anchor.x <= .95f && anchor.y >= .05f && anchor.y <= .95f;
    }
}
