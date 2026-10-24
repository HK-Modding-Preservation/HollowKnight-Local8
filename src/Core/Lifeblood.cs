using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Lifeblood
{
	private sealed class Pickup
	{
		internal PlayerSlot Player;

		internal float When;
	}

	private sealed class HitRecord
	{
		internal PlayerSlot Player;

		internal Vector3 Position;

		internal float When;
	}

	private sealed class Scope : IDisposable
	{
		private readonly GameObject before;

		internal Scope(GameObject value)
		{
			before = source;
			source = value;
		}

		public void Dispose()
		{
			source = before;
		}
	}

	private static readonly Dictionary<int, PlayerSlot> killers = new Dictionary<int, PlayerSlot>();

	private static readonly Dictionary<int, float> awarded = new Dictionary<int, float>();
	private static readonly HashSet<int> shared = new HashSet<int>();
	internal const float ShareRadius = 12f;
	private static bool installed;

	internal static void Install()
	{
		if (installed) return;
		installed = true;
		On.HealthManager.OnEnable += Enabled;
		LifebloodScuttlers.Install();
	}

	internal static void Uninstall()
	{
		if (!installed) return;
		installed = false;
		On.HealthManager.OnEnable -= Enabled;
		LifebloodScuttlers.Uninstall();
		Reset();
	}

	private static void Enabled(On.HealthManager.orig_OnEnable orig, HealthManager self)
	{
		if (IsBug(self.gameObject))
		{
			int id = self.gameObject.GetInstanceID();
			shared.Remove(id);
			awarded.Remove(id);
			killers.Remove(id);
			collectors.Remove(id);
		}
		orig(self);
	}

	private static bool IsBug(GameObject go)
	{
		if (!go) return false;
		string n = go.name;
		return n.IndexOf("health scuttler", StringComparison.OrdinalIgnoreCase) >= 0
			|| n.IndexOf("healthscuttler", StringComparison.OrdinalIgnoreCase) >= 0
			|| n.IndexOf("life seed", StringComparison.OrdinalIgnoreCase) >= 0
			|| n.IndexOf("lifeseed", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static readonly Dictionary<int, Pickup> collectors = new Dictionary<int, Pickup>();

	private static readonly Dictionary<Fsm, bool> pickupFsms = new Dictionary<Fsm, bool>();

	private static Pickup lastTouched;

	private static readonly List<HitRecord> recent = new List<HitRecord>();

	[ThreadStatic]
	private static GameObject source;

	private static bool IsSeed(GameObject go)
	{
		if (!go)
		{
			return false;
		}
		string name = go.name;
		if (name.IndexOf("health scuttler", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("healthscuttler", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("life seed", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("lifeseed", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("lifeblood", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("blue health", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return name.IndexOf("bluehealth", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static bool IsPickup(Fsm f)
	{
		if (f == null)
		{
			return false;
		}
		if (pickupFsms.TryGetValue(f, out var value))
		{
			return value;
		}
		string text = (f.GameObject ? f.GameObject.name : "");
		string text2 = f.Name ?? "";
		value = (bool)f.GameObject && (IsSeed(f.GameObject) || text2.IndexOf("blue health", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("lifeblood", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("health orb", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("health pickup", StringComparison.OrdinalIgnoreCase) >= 0);
		pickupFsms[f] = value;
		return value;
	}

	private static GameObject Root(GameObject go)
	{
		HealthManager healthManager = (go ? go.GetComponentInParent<HealthManager>() : null);
		if (!healthManager)
		{
			return go;
		}
		return healthManager.gameObject;
	}

	internal static void Hit(HealthManager hm, PlayerSlot p)
	{
		if ((bool)hm && p != null && IsSeed(hm.gameObject))
		{
			killers[hm.gameObject.GetInstanceID()] = p;
			recent.Add(new HitRecord
			{
				Player = p,
				Position = hm.transform.position,
				When = Time.unscaledTime
			});
			if (recent.Count > 24)
			{
				recent.RemoveAt(0);
			}
			Diagnostics.Write("LIFEBLOOD hit by P" + (p.Index + 1));
		}
	}

	private static PlayerSlot Killer(GameObject root)
	{
		if (killers.TryGetValue(root.GetInstanceID(), out var value) && value.Alive)
		{
			return value;
		}
		for (int num = recent.Count - 1; num >= 0; num--)
		{
			HitRecord hitRecord = recent[num];
			if (Time.unscaledTime - hitRecord.When > 12f)
			{
				break;
			}
			if (hitRecord.Player.Alive && Vector2.Distance(root.transform.position, hitRecord.Position) < 6f)
			{
				killers[root.GetInstanceID()] = hitRecord.Player;
				return hitRecord.Player;
			}
		}
		return Plugin.Self.Session.Nearest(root.transform.position);
	}

	internal static PlayerSlot Track(Fsm f, Collider2D other)
	{
		if (!IsPickup(f) || !other)
		{
			return null;
		}
		PlayerSlot playerSlot = Plugin.Self.Session?.Resolve(other);
		if (playerSlot == null || !playerSlot.Alive)
		{
			return null;
		}
		int instanceID = f.GameObject.GetInstanceID();
		if (!collectors.TryGetValue(instanceID, out var value) || value.Player != playerSlot || Time.unscaledTime - value.When > 1f)
		{
			Diagnostics.Write("LIFEBLOOD contact P" + (playerSlot.Index + 1) + " object=" + f.GameObject.name + " fsm=" + f.Name);
		}
		collectors[instanceID] = new Pickup
		{
			Player = playerSlot,
			When = Time.unscaledTime
		};
		lastTouched = collectors[instanceID];
		return playerSlot;
	}

	private static PlayerSlot Collector(GameObject root)
	{
		if ((bool)root && collectors.TryGetValue(root.GetInstanceID(), out var value) && value.Player.Alive && Time.unscaledTime - value.When < 3f)
		{
			return value.Player;
		}
		return null;
	}

	internal static PlayerSlot RecentCollector()
	{
		if (lastTouched == null || !lastTouched.Player.Alive || !(Time.unscaledTime - lastTouched.When < 0.8f))
		{
			return null;
		}
		return lastTouched.Player;
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		if (!IsPickup(f))
		{
			return null;
		}
		GameObject root = Root(f.GameObject);
		return Collector(f.GameObject) ?? Collector(root) ?? Killer(root);
	}

	internal static IDisposable Enter(Fsm f)
	{
		if (!IsPickup(f))
		{
			return null;
		}
		GameObject gameObject = Root(f.GameObject);
		if (!(gameObject != source))
		{
			return null;
		}
		return new Scope(gameObject);
	}

	internal static bool Award(FsmEvent evt)
	{
		if (!source || evt == null || evt.Name != "ADD BLUE HEALTH")
		{
			return false;
		}
		int instanceID = source.GetInstanceID();
		CoopSession session = Plugin.Self.Session;
		if (IsBug(source))
		{
			// A broadcast may visit every hero FSM over several frames. Consume
			// once for this bug's lifetime; OnEnable resets a pooled bug.
			if (!shared.Add(instanceID)) return true;
			Share(session, source);
			return true;
		}
		if (awarded.TryGetValue(instanceID, out var value) && Time.unscaledTime - value < 0.25f)
		{
			return true;
		}
		PlayerSlot playerSlot = Collector(source) ?? Killer(source);
		if (playerSlot == null)
		{
			return false;
		}
		awarded[instanceID] = Time.unscaledTime;
		Give(session, playerSlot);
		return true;
	}

	internal static void NativeBug(CoopSession session, GameObject bug)
	{
		// Called only when native ScuttlerControl.Hit starts its Heal routine.
		// This is a new accepted lifetime, including a rearmed pooled scuttler.
		shared.Add(bug.GetInstanceID());
		using (new Scope(bug)) Share(session, bug);
	}

	private static void Share(CoopSession session, GameObject bug)
	{
		Vector3 at = bug.transform.position;
		int count = 0;
		foreach (var p in session.Players)
		{
			if (!WorldContactRouting.Eligible(p) || p.Vitals.Health <= 0 || TransitionVote.Holding(p)
				|| Vector2.Distance(at, p.Hero.transform.position) > ShareRadius) continue;
			Give(session, p);
			count++;
		}
		Diagnostics.Write("LIFEBLOOD shared source=" + bug.name + " nearby=" + count + " radius=" + ShareRadius);
	}

	private static void Give(CoopSession session, PlayerSlot playerSlot)
	{
		using (PlayerContext.Enter(playerSlot))
		{
			session.Data.healthBlue++;
		}
		playerSlot.HealFlashUntil = Time.unscaledTime + 0.45f;
		Diagnostics.Write("LIFEBLOOD awarded P" + (playerSlot.Index + 1) + " source=" + source.name);
	}

	internal static void Reset()
	{
		LifebloodScuttlers.Reset();
		killers.Clear();
		awarded.Clear();
		shared.Clear();
		collectors.Clear();
		pickupFsms.Clear();
		recent.Clear();
		source = null;
		lastTouched = null;
	}
}
