using System;
using Modding;
using TMPro;
using UnityEngine;

namespace KO.HollowKnight8;

// The native new-game event arms this save only. Loading an existing save
// never turns the introduction back on, including saves predating this field.
internal static class CoopSetupHint
{
    private static bool installed, visible;
    private static float elapsed, nextSearch;
    private static Local8SaveData save;
    private static TextMeshProUGUI label;
    private static TMP_FontAsset font;
    internal static bool Pending => visible || Local8Mod.Save != null && Local8Mod.Save.CoopSetupHintState == 1;

    internal static void Install() { if (installed) return; installed = true; ModHooks.NewGameHook += NewGame; }
    internal static void Uninstall() { if (installed) ModHooks.NewGameHook -= NewGame; installed = false; Reset(); }
    private static void NewGame()
    {
        if (Local8Mod.Save != null && Local8Mod.Save.CoopSetupHintState == 0)
            Local8Mod.Save.CoopSetupHintState = 1;
    }
    internal static void Reset()
    {
        if (label) UnityEngine.Object.Destroy(label.gameObject);
        label = null; font = null; save = null; visible = false; elapsed = nextSearch = 0;
    }
    private static bool Create()
    {
        var ui = UIManager.instance;
        if (!ui || !ui.UICanvas || !ui.UICanvas.gameObject.activeInHierarchy) return false;
        if (!font && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 1f;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (candidate && (candidate.name.IndexOf("Trajan", StringComparison.OrdinalIgnoreCase) >= 0 || candidate.name.IndexOf("Perpetua", StringComparison.OrdinalIgnoreCase) >= 0)) { font = candidate; break; }
            if (!font) foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
                if (text && text.font && text.gameObject.scene.IsValid()) { font = text.font; break; }
        }
        if (!font) return false;
        if (!label)
        {
            var go = new GameObject("Local8 Co-op Setup Hint", typeof(RectTransform));
            go.transform.SetParent(ui.UICanvas.transform, false);
            label = go.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = 25f;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; label.enableWordWrapping = false;
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.05f, .12f); rect.anchorMax = new Vector2(.95f, .18f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        return true;
    }
    internal static void Tick(Local8Runtime runtime)
    {
        if (save != Local8Mod.Save) { Reset(); save = Local8Mod.Save; }
        if (label) label.enabled = false;
        if (!Pending || runtime == null || !runtime.Enabled.Value) return;
        var s = runtime.Session; var gm = GameManager.instance; var ui = UIManager.instance;
        if (s == null || !s.Active || !s.Gameplay || !s.CanJoin || s.Primary == null || !s.Primary.Ready || !s.Primary.Alive ||
            !gm || gm.isPaused || gm.IsLoadingSceneTransition || runtime.Panel || Charms.NativeMenuOpen || ScriptedParty.Active || CoopEnding.Active ||
            PickupCard.Owner != null || ShopMenuRouting.Buyer != null || StagMenuRouting.HasOwner || InteractionRouter.ActivePlayer != null ||
            !ui || ui.uiState != GlobalEnums.UIState.PLAYING || !Create()) return;
        if (!visible)
        {
            visible = true; save.CoopSetupHintState = 2;
            Diagnostics.Write("COOP setup hint shown once for new save");
        }
        elapsed += Mathf.Max(0, Time.unscaledDeltaTime);
        if (elapsed >= 15f) { Reset(); save = Local8Mod.Save; return; }
        label.text = string.Format(UiLocalization.Get("coop_setup_hint"), MenuInput.Display(MenuInput.Key));
        label.color = new Color(.95f, .93f, .86f, Mathf.Clamp01(Mathf.Min(elapsed / .5f, (15f - elapsed) / 2f)));
        label.enabled = true;
    }
}
