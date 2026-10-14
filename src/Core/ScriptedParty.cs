using System;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class ScriptedParty
{
	private sealed class Guest
	{
		internal PlayerSlot Player;

		internal HeroController Hero;

		internal tk2dSpriteAnimator Animator;

		internal string Clip;

		internal Vector3 Offset;

		internal Rigidbody2D Body;

		internal bool Simulated;

		internal Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip> Completed;

		internal Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int> Triggered;
	}

	private static readonly List<Guest> guests = new List<Guest>();

	private static string scene;

	private static float started;

	private static float unlocked = -1f;

	private static float playableAt = -1f;

	private static int observedFireball = -1;

	private static bool attempted;

	private static Vector3 lastPrimary;

	private static bool active;

	private static GameObject card;

	private static bool cardSeen;

	private static PlayerSlot nativeActor;

	internal static bool Active => ShadeCloakRitual.Combined(active || SoulAcquisitionRitual.Active);

	internal static bool TracksChamber => !active && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Crossroads_ShamanTemple";

	internal static bool Holds(PlayerSlot p)
	{
		if (SoulAcquisitionRitual.Holds(p)) return true;
		if (!active || p == null)
		{
			return ShadeCloakRitual.Holds(native: false, p);
		}
		bool native;
		foreach (Guest guest in guests)
		{
			if (guest.Player != p || !(guest.Hero == p.Hero))
			{
				continue;
			}
			native = true;
			goto IL_0082;
		}
		return ShadeCloakRitual.Holds(native: false, p);
		IL_0082:
		return ShadeCloakRitual.Holds(native, p);
	}

	private static bool FirstSpell(CoopSession s)
	{
		if (s != null && s.Active && s.Players.Count > 1 && s.Primary != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Crossroads_ShamanTemple")
		{
			return s.Data != null;
		}
		return false;
	}

	internal static void NativeMove(CoopSession s, PlayerSlot p, Vector3 from, Vector3 to)
	{
		if (FirstSpell(s) && p != null && p.Alive && p == Anchor(s) && p.Hero.controlReqlinquished && s.Data.fireballLevel <= 1 && s.InRoom(from) && s.InRoom(to) && !(Vector2.Distance(from, to) < 12f))
		{
			Begin(s, "native chamber arrival", p);
		}
	}

	internal static void SpellOrb(SpellGetOrb orb)
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (!orb || !orb.trackToHero || !FirstSpell(coopSession))
		{
			return;
		}
		// Orbs/ascent are absorption, not a chamber transfer. Followers stay
		// in place for the bounded white ritual. NativeMove alone gathers the
		// party when vanilla really moves its collector into the locked room.
	}

	internal static void Card(Fsm source, GameObject created)
	{
		if (!active || !created) return;
		card = created;
		cardSeen = true;
	}

	private static PlayerSlot Anchor(CoopSession s)
	{
		if (active && nativeActor != null && nativeActor.Hero) return nativeActor;
		PlayerSlot candidate = SharedAcquisition.ResolveCollector("fireballLevel");
		if (candidate != null && candidate.Alive && candidate.Ready) return candidate;
		if (s != null && s.Primary != null && s.Primary.Alive) return s.Primary;
		if (s != null) foreach (PlayerSlot p in s.Players) if (p.Alive && p.Ready) return p;
		return null;
	}

	private static void Begin(CoopSession s, string reason)
	{
		Begin(s, reason, null);
	}

	private static void Begin(CoopSession s, string reason, PlayerSlot collector)
	{
		if (active || attempted || !FirstSpell(s))
		{
			return;
		}
		collector = collector ?? Anchor(s);
		if (collector == null || !collector.Alive || !collector.Ready) return;
		SoulAcquisitionRitual.Release();
		SharedAcquisition.ReleasePoses();
		nativeActor = collector;
		attempted = true;
		active = true;
		scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		started = Time.unscaledTime;
		unlocked = ((s.Data.fireballLevel > 0) ? started : (-1f));
		playableAt = -1f;
		card = null;
		cardSeen = false;
		lastPrimary = collector.Hero.transform.position;
		foreach (PlayerSlot player in s.Players)
		{
			if (player == collector || !player.Alive || !player.Ready || !player.Hero)
			{
				continue;
			}
			Guest guest = new Guest
			{
				Player = player,
				Hero = player.Hero,
				Animator = player.Hero.GetComponent<tk2dSpriteAnimator>(),
				Offset = new Vector3((guests.Count % 2 == 0 ? -1 : 1) * (1.4f + .4f * (guests.Count / 2)), 0f, (float)CoopRules.PlayerDepth(player.Index) - (float)CoopRules.PlayerDepth(collector.Index)),
				Body = player.Hero.GetComponent<Rigidbody2D>()
			};
			guests.Add(guest);
			if (guest.Body) guest.Simulated = guest.Body.simulated;
			if (guest.Animator)
			{
				guest.Completed = guest.Animator.AnimationCompleted;
				guest.Triggered = guest.Animator.AnimationEventTriggered;
				guest.Animator.AnimationCompleted = null;
				guest.Animator.AnimationEventTriggered = null;
			}
			using (PlayerContext.Enter(player))
			{
				Revival.End(player);
				player.Hero.RelinquishControl();
				player.Hero.StopAnimationControl();
				player.Hero.SetDamageMode(DamageMode.NO_DAMAGE);
				Rigidbody2D component = player.Hero.GetComponent<Rigidbody2D>();
				if ((bool)component)
				{
					component.velocity = Vector2.zero;
					component.simulated = false;
				}
			}
			Vector3 vector = collector.Hero.transform.position + guest.Offset;
			if (s.InRoom(vector))
			{
				Vector3 position = player.Hero.transform.position;
				NativeDreamFx.TeleportTrail(player, position, vector);
				player.Hero.transform.position = vector;
			}
			SpriteFlash component2 = player.Hero.GetComponent<SpriteFlash>();
			if ((bool)component2)
			{
				component2.flashFocusGet();
			}
		}
		if (guests.Count == 0)
		{
			active = false;
		}
		else
		{
			Diagnostics.Write("SCRIPT first spell started " + reason + " collector=P" + (collector.Index + 1) + " guests=" + guests.Count);
		}
	}

	internal static void Tick(CoopSession s)
	{
		ShadeCloakRitual.Tick(s);
		PlayerSlot collector = s == null ? null : Anchor(s);
		if (s == null || !s.Active || collector == null || !collector.Hero || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Crossroads_ShamanTemple")
		{
			if (active)
			{
				End(restore: false);
			}
		}
		else
		{
			if (!active && !FirstSpell(s))
			{
				return;
			}
			int fireballLevel = s.Data.fireballLevel;
			if (observedFireball < 0)
			{
				observedFireball = fireballLevel;
			}
			if (!active)
			{
				// A level increase is not proof of a teleport. Do not gather
				// followers just because P1 gained the shared ability flag.
				observedFireball = fireballLevel;
				return;
			}
			observedFireball = fireballLevel;
			if (!collector.Alive)
			{
				End(restore: true);
				return;
			}
			HeroController hero = collector.Hero;
			if (unlocked < 0f && s.Data.fireballLevel > 0)
			{
				unlocked = Time.unscaledTime;
				Diagnostics.Write("SCRIPT first spell unlocked; waiting for native wake");
			}
			Vector3 position2 = hero.transform.position;
			if (Vector2.Distance(lastPrimary, position2) > 6f)
			{
				Vector3 vector = lastPrimary;
				string text = vector.ToString();
				vector = position2;
				Diagnostics.Write("SCRIPT first spell native move " + text + " -> " + vector.ToString());
			}
			lastPrimary = position2;
			tk2dSpriteAnimator component = hero.GetComponent<tk2dSpriteAnimator>();
			string text2 = (((bool)component && component.CurrentClip != null) ? component.CurrentClip.name : null);
			bool flag = s.InRoom(position2);
			foreach (Guest guest in guests)
			{
				if ((bool)guest.Hero && !(guest.Hero != guest.Player.Hero))
				{
					if (flag)
					{
						guest.Hero.transform.position = position2 + guest.Offset;
					}
					if (text2 != null && text2 != guest.Clip && (bool)guest.Animator && guest.Animator.GetClipByName(text2) != null)
					{
						guest.Clip = text2;
						try
						{
							SharedAcquisition.Replaying = true;
							guest.Animator.Play(text2);
						}
						finally { SharedAcquisition.Replaying = false; }
					}
				}
			}
			UIManager instance = UIManager.instance;
			bool visibleCard = card && card.activeInHierarchy;
			bool flag2 = s.Gameplay && (bool)instance && instance.uiState == UIState.PLAYING && !Plugin.Self.Panel && !visibleCard;
			if (!flag2 && s.Gameplay && unlocked >= 0f && Time.unscaledTime - unlocked > 18f && cardSeen && !visibleCard && !Plugin.Self.Panel)
			{
				flag2 = true;
				Diagnostics.Write("SCRIPT first spell recovering stuck UI state=" + (instance ? instance.uiState.ToString() : "missing"));
			}
			if (flag2)
			{
				if (playableAt < 0f)
				{
					playableAt = Time.unscaledTime;
				}
			}
			else
			{
				playableAt = -1f;
			}
			if (unlocked < 0f)
			{
				if (Time.unscaledTime - started > 40f && playableAt > 0f)
				{
					End(restore: true);
				}
			}
			else
			{
				if (!flag || playableAt < 0f || Time.unscaledTime - unlocked < 1f || Time.unscaledTime - playableAt < 0.6f)
				{
					return;
				}
				tk2dSprite component2 = hero.GetComponent<tk2dSprite>();
				Renderer component3 = hero.GetComponent<Renderer>();
				bool num = (bool)component3 && component3.enabled && (!component2 || component2.color.a > 0.05f);
				HeroAnimationController component4 = hero.GetComponent<HeroAnimationController>();
				if (!num || hero.controlReqlinquished || !hero.acceptingInput || ((bool)component4 && !component4.controlEnabled))
				{
					if (!(hero == collector.Hero) || visibleCard || Time.unscaledTime - playableAt < 3f || (InteractionRouter.ActivePlayer != null && Time.unscaledTime - playableAt < 12f))
					{
						return;
					}
					using (PlayerContext.Enter(collector))
					{
						ActorRecovery.Reset(collector);
						CoopSession.RestoreLivingVisuals(collector);
					}
					Diagnostics.Write("SCRIPT first spell recovered native P" + (collector.Index + 1) + " after missing wake");
				}
				End(restore: true);
			}
		}
	}

	private static void End(bool restore)
	{
		if (!active)
		{
			return;
		}
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		foreach (Guest guest in guests)
		{
			if (guest.Animator)
			{
				if (guest.Animator.AnimationCompleted == null) guest.Animator.AnimationCompleted = guest.Completed;
				if (guest.Animator.AnimationEventTriggered == null) guest.Animator.AnimationEventTriggered = guest.Triggered;
			}
			if (guest.Body) guest.Body.simulated = guest.Simulated;
			PlayerSlot player = guest.Player;
			if (restore && (bool)guest.Hero && !(player.Hero != guest.Hero) && !player.Down && !player.Hazard)
			{
				Vector3 vector = nativeActor != null && nativeActor.Hero ? nativeActor.Hero.transform.position : guest.Hero.transform.position;
				if (coopSession != null && coopSession.FindSafePosition(vector + guest.Offset, player, out var result))
				{
					guest.Hero.transform.position = result;
				}
				using (PlayerContext.Enter(player))
				{
					ActorRecovery.Reset(player);
					CoopSession.RestoreLivingVisuals(player);
				}
				player.ProtectionUntil = Time.time + 1.5f;
				player.SafePoint = guest.Hero.transform.position;
				player.HasSafePoint = coopSession?.IsSafe(player.SafePoint, player) ?? false;
				string text = (player.Index + 1).ToString();
				Vector3 safePoint = player.SafePoint;
				Diagnostics.Write("SCRIPT first spell wake P" + text + " at=" + safePoint.ToString());
			}
		}
		guests.Clear();
		active = false;
		scene = null;
		started = 0f;
		unlocked = (playableAt = -1f);
		card = null;
		cardSeen = false;
		nativeActor = null;
	}

	internal static void SafetyTick(CoopSession s)
	{
		if (!active) return;
		bool local = s != null && s.Active && nativeActor != null && nativeActor.Hero && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == scene;
		if (!local) End(restore: false);
		else if (Time.unscaledTime - started >= 45f)
		{
			// Only release our guest choreography. The native collector/card
			// continues untouched, even if a callback or session Tick was missed.
			Diagnostics.Write("SCRIPT first spell guest safety release");
			End(restore: true);
		}
	}

	internal static void Reset()
	{
		End(restore: false);
		observedFireball = -1;
		attempted = false;
	}
}
