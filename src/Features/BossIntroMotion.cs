using GlobalEnums;
using UnityEngine;

namespace KO.HollowKnight8;

// A boss's native cutscene still needs HeroController.Update/FixedUpdate to
// finish recoil and restore gravity. Blocking input does not suspend physics.
internal static class BossIntroMotion
{
    internal static bool Runs(CoopSession session, PlayerSlot player)
    {
        var gm = GameManager.instance;
        return session != null && session.Active && player != null && player.Index > 0 &&
            gm && !gm.isPaused && gm.gameState == GameState.CUTSCENE &&
            !gm.IsLoadingSceneTransition && gm.HasFinishedEnteringScene && BossSceneController.IsBossScene;
    }
}
