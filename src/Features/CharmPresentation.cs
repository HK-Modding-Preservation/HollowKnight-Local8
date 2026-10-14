using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace KO.HollowKnight8;

// Royal charm 36 has dedicated localization keys and serialized scene sprites.
// Reading its generic icon/key exposes Custom Knight's unused placeholder.
internal static class CharmPresentation
{
    private static InvCharmBackboard royalBoard;
    private static PlayMakerFSM royalFsm;
    private static CharmDisplay royalDisplay;
    private static float nextProbe;
    private static float nextBoardProbe;

    private static int RoyalState => GameManager.instance && GameManager.instance.playerData != null
        ? GameManager.instance.playerData.GetInt("royalCharmState") : 0;

    internal static string Key(string prefix, int id)
    {
        if (id != 36) return prefix + id;
        int state = RoyalState;
        return prefix + "36_" + (state > 3 ? "C" : state == 3 ? "B" : "A");
    }

    private static PlayMakerFSM SpriteFsm(GameObject obj)
    {
        if (!obj) return null;
        foreach (var fsm in obj.GetComponentsInChildren<PlayMakerFSM>(true))
            if (fsm && fsm.Fsm.GetState("R Final") != null && fsm.Fsm.GetState("R Shade") != null) return fsm;
        return null;
    }

    private static void Probe()
    {
        // Prefer the catalog's explicit icon link, including siblings of the backboard.
        foreach (var board in Charms.NativeBoards)
            if (board && board.charmNum == 36)
            {
                if (royalBoard != board || (!royalFsm && Time.unscaledTime >= nextBoardProbe))
                {
                    nextBoardProbe = Time.unscaledTime + .5f;
                    royalBoard = board;
                    royalFsm = SpriteFsm(board.charmObject) ?? SpriteFsm(board.gameObject);
                    royalDisplay = board.charmObject ? board.charmObject.GetComponentInChildren<CharmDisplay>(true) : null;
                }
                if (royalFsm || royalDisplay) return;
                break;
            }
        if (Time.unscaledTime < nextProbe) return;
        nextProbe = Time.unscaledTime + .5f;
        // The F8 menu can be opened before the native pane has been probed.
        foreach (var display in Resources.FindObjectsOfTypeAll<CharmDisplay>())
            if (display && display.id == 36 && (display.whiteCharm || display.blackCharm))
            {
                royalDisplay = display;
                royalFsm = SpriteFsm(display.gameObject);
                if (display.gameObject.activeInHierarchy) break;
            }
    }

    internal static Sprite Sprite(int id)
    {
        if (id != 36) return CharmIconList.Instance ? CharmIconList.Instance.GetSprite(id) : null;
        Probe();
        int state = RoyalState;
        if (royalFsm)
        {
            var fsmState = royalFsm.Fsm.GetState(state > 3 ? "R Shade" : state == 3 ? "R Final" : state == 2 ? "R King" : "R Queen");
            if (fsmState != null)
                foreach (var action in fsmState.Actions)
                    if (action is SetSpriteRendererSprite set && set.sprite != null && set.sprite.Value is Sprite sprite && sprite) return sprite;
        }
        // CharmDisplay.Start uses these fields for the complete white/void variants.
        // Never fall back to spriteList[36], which is not the royal charm.
        return royalDisplay ? state > 3 ? royalDisplay.blackCharm : royalDisplay.whiteCharm : null;
    }

    internal static void Reset()
    {
        royalBoard = null; royalFsm = null; royalDisplay = null; nextProbe = nextBoardProbe = 0f;
    }
}
