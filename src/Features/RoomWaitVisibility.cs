using GlobalEnums;
using HutongGames.PlayMaker;

namespace KO.HollowKnight8;

// An ordinary door can finish its fade before BeginSceneTransition is called.
// Keep only that deferred transition visible; native death/hazard/story fades
// and other FSMs still run normally.
internal static class RoomWaitVisibility
{
    private static bool installed;
    private static PlayMakerFSM currentFade;
    private static bool specialFade;
    private static bool requestedIn;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.HutongGames.PlayMaker.Fsm.ProcessEvent += Event;
        On.CameraController.FadeOut += FadeOut;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.HutongGames.PlayMaker.Fsm.ProcessEvent -= Event;
        On.CameraController.FadeOut -= FadeOut;
        Reset();
    }

    internal static void Reset()
    {
        currentFade = null;
        specialFade = false;
        requestedIn = false;
    }

    private static bool Waiting(CoopSession s)
    {
        var gm = GameManager.instance;
        return TransitionVote.Pending && s != null && s.Active && s.Gameplay && !s.TeamWipe
            && gm && !gm.IsLoadingSceneTransition && !ScriptedParty.Active && !CoopEnding.Active
            && !ArenaGather.TransferActive && !BossSequenceController.IsInSequence
            && !StagMenuRouting.HasOwner && !DreamSequence.InStoryDream
            && !StagTravelRules.SharedRoute(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, null, null);
    }

    private static bool Hazard(CoopSession s)
    {
        foreach (PlayerSlot player in s.Players)
            if (player.Hazard || player.Down) return true;
        return false;
    }

    private static void Observe(PlayMakerFSM fade)
    {
        if (currentFade != fade)
        {
            currentFade = fade;
            specialFade = false;
            requestedIn = false;
        }
        if (fade.ActiveStateName == "Normal") specialFade = false;
    }

    internal static void Tick(CoopSession s)
    {
        if (!Waiting(s)) return;
        var cameras = GameCameras.instance;
        var fade = cameras ? cameras.cameraFadeFSM : null;
        if (!fade) return;
        Observe(fade);
        if (specialFade || Hazard(s) || fade.ActiveStateName == "Normal") return;
        string before = fade.ActiveStateName;
        if (fade.Fsm.GetState("Normal") != null)
        {
            fade.SetState("Normal");
            RestoreCamera();
            Diagnostics.Write("ROOM WAIT visible state=" + before);
        }
        else if (!requestedIn)
        {
            requestedIn = true;
            GameManager.instance.FadeSceneIn();
            RestoreCamera();
            Diagnostics.Write("ROOM WAIT fade in state=" + before);
        }
    }

    private static void RestoreCamera()
    {
        var gm = GameManager.instance;
        var camera = gm ? gm.cameraCtrl : null;
        if (camera && camera.mode == CameraController.CameraMode.FROZEN)
            camera.SetMode(CameraController.CameraMode.PREVIOUS);
    }

    private static bool TransitionFade(string name)
    {
        return name == "FADE OUT" || name == "FADE OUT INSTANT"
            || name == "FADE SCENE OUT" || name == "FADE SCENE OUT INSTANT";
    }

    private static void Event(On.HutongGames.PlayMaker.Fsm.orig_ProcessEvent orig, Fsm self, FsmEvent evt, FsmEventData data)
    {
        if (!TransitionVote.Pending)
        {
            orig(self, evt, data);
            return;
        }
        var s = Plugin.Self == null ? null : Plugin.Self.Session;
        var cameras = GameCameras.instance;
        var fade = cameras ? cameras.cameraFadeFSM : null;
        if (evt != null && fade && ReferenceEquals(self, fade.Fsm) && Waiting(s))
        {
            Observe(fade);
            if (evt.Name == "RESPAWN FADE" || evt.Name == "HAZARD FADE"
                || evt.Name == "START FADE" || evt.Name == "JUST FADE") specialFade = true;
            if (!specialFade && !Hazard(s) && TransitionFade(evt.Name)) return;
        }
        orig(self, evt, data);
    }

    private static void FadeOut(On.CameraController.orig_FadeOut orig, CameraController self, CameraFadeType type)
    {
        var gm = GameManager.instance;
        var s = Plugin.Self == null ? null : Plugin.Self.Session;
        if (gm && self == gm.cameraCtrl && type == CameraFadeType.LEVEL_TRANSITION
            && Waiting(s) && !specialFade && !Hazard(s)) return;
        orig(self, type);
    }
}
