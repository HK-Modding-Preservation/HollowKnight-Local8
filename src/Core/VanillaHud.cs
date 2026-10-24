using System;
using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class VanillaHud
{
	private static readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();

	private static readonly List<Renderer> renderers = new List<Renderer>(160);

	private static int scanned = -1;
	private static GameObject canvas;
	private static readonly List<Transform> vitalRoots = new List<Transform>();
	private static float nextDiscovery;
	private static int canvasChildren;

	private static bool installed;
	[ThreadStatic] internal static GameObject SpawnSource;
	private static readonly Dictionary<GameObject, List<Renderer>> feedback = new Dictionary<GameObject, List<Renderer>>();

	// Native health FSMs can pool their bursts outside the HUD hierarchy. Hiding
	// the masks alone leaves those renderers at the original single-player HUD.
	internal static void Spawned(GameObject result, GameObject source)
	{
		if (!result) return;
		if (feedback.TryGetValue(result, out var previous))
		{
			foreach (var renderer in previous)
				if (renderer && hidden.TryGetValue(renderer, out bool visible))
				{
					renderer.forceRenderingOff = visible;
					hidden.Remove(renderer);
				}
			feedback.Remove(result);
		}
		var runtime = Plugin.Self;
		var game = GameManager.instance;
		var cameras = GameCameras.instance;
		if (runtime == null || !runtime.Enabled.Value || !runtime.HideOriginalHud.Value ||
			runtime.Session == null || !runtime.Session.Active || !game || !game.IsGameplayScene() ||
			game.IsLoadingSceneTransition || !cameras || !source) return;
		Transform root = cameras.hudCanvas && source.transform.IsChildOf(cameras.hudCanvas.transform) ? cameras.hudCanvas.transform :
			cameras.hudCamera && source.transform.IsChildOf(cameras.hudCamera.transform) ? cameras.hudCamera.transform : null;
		if (!root || !Vitals(source.transform, root)) return;
		// The first burst can precede the first camera callback for this canvas.
		if (canvas != cameras.hudCanvas) { Restore(); canvas = cameras.hudCanvas; }
		var owned = new List<Renderer>();
		result.GetComponentsInChildren(includeInactive: true, owned);
		foreach (var renderer in owned)
			if (renderer)
			{
				if (!hidden.ContainsKey(renderer)) hidden[renderer] = renderer.forceRenderingOff;
				renderer.forceRenderingOff = true;
			}
		feedback[result] = owned;
	}

	internal static void Install()
	{
		if (!installed)
		{
			installed = true;
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPreRender, new Camera.CameraCallback(BeforeCamera));
		}
	}

	internal static void Uninstall()
	{
		if (installed)
		{
			installed = false;
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Remove(Camera.onPreRender, new Camera.CameraCallback(BeforeCamera));
			Restore();
		}
	}

	internal static void Restore()
	{
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if ((bool)item.Key)
			{
				item.Key.forceRenderingOff = item.Value;
			}
		}
		hidden.Clear();
		feedback.Clear();
		renderers.Clear();
		vitalRoots.Clear(); canvas = null; nextDiscovery = 0f; canvasChildren = -1;
		scanned = -1;
	}

	private static Transform Vitals(Transform t, Transform root)
	{
		Transform found = null;
		while ((bool)t && t != root)
		{
			string name = t.name;
			if (name.IndexOf("health", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("soul", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("orb", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				found = t;
			}
			t = t.parent;
		}
		return found;
	}
	private static void Collect(Transform root)
	{
		renderers.Clear(); root.GetComponentsInChildren(includeInactive: true, renderers);
		foreach (Renderer renderer in renderers)
			if (renderer && !hidden.ContainsKey(renderer)) hidden[renderer] = renderer.forceRenderingOff;
	}

	private static void BeforeCamera(Camera camera)
	{
		PvpGeoHud.BeforeCamera(camera);
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		GameManager instance = GameManager.instance;
		if (self == null || !self.Enabled.Value || !self.HideOriginalHud.Value || coopSession == null || !coopSession.Active || !instance || !instance.IsGameplayScene() || instance.IsLoadingSceneTransition)
		{
			if (hidden.Count > 0)
			{
				Restore();
			}
			return;
		}
		GameCameras instance2 = GameCameras.instance;
		if (!instance2 || !instance2.hudCanvas)
		{
			return;
		}
		if (scanned != Time.frameCount)
		{
			if (canvas != instance2.hudCanvas) { Restore(); canvas = instance2.hudCanvas; }
			scanned = Time.frameCount;
			if (Time.unscaledTime >= nextDiscovery || canvasChildren != canvas.transform.childCount)
			{
				nextDiscovery = Time.unscaledTime + .5f; canvasChildren = canvas.transform.childCount;
				vitalRoots.Clear(); renderers.Clear();
				canvas.GetComponentsInChildren(includeInactive: true, renderers);
				foreach (Renderer renderer in renderers)
				{
					if (!renderer) continue;
					Transform root = Vitals(renderer.transform, canvas.transform);
					if (root && !vitalRoots.Contains(root)) vitalRoots.Add(root);
				}
			}
			// Still scan the actual health/soul roots every frame. Newly created
			// damage masks are hidden immediately, without walking all inventory,
			// journal, map and shop renderers on every camera pass.
			foreach (Transform root in vitalRoots) if (root) Collect(root);
		}
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if ((bool)item.Key)
			{
				item.Key.forceRenderingOff = true;
			}
		}
	}
}
