using System;
using System.Collections.Generic;
using System.Linq;
using GlobalEnums;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Hud
{
	private static GUIStyle text;

	private static GUIStyle title;

	internal static GUIStyle small;

	private static GUIStyle button;

	private static GUIStyle tag;

	private static Texture2D pixel;

	internal static int selected;

	private static int tab;

	private static Vector2 scroll;
	private static readonly float[] menuHeight = new float[5];
	private static int pvpPage;
	private static bool adventurePage;
	private static bool addingKeyboard;

	private static readonly int[] lastHealth = Enumerable.Repeat(-1, 8).ToArray();

	private static readonly Color[] palette = new Color[12]
	{
		new Color(0.82f, 0.92f, 1f),
		new Color(0.4f, 0.85f, 1f),
		new Color(1f, 0.55f, 0.45f),
		new Color(0.55f, 1f, 0.6f),
		new Color(0.95f, 0.65f, 1f),
		new Color(1f, 0.88f, 0.4f),
		new Color(0.55f, 0.62f, 1f),
		new Color(1f, 0.64f, 0.84f),
		Color.white,
		new Color(0.55f, 1f, 0.9f),
		new Color(1f, 0.35f, 0.35f),
		new Color(0.75f, 0.55f, 1f)
	};

	private static Font nativeFont;

	private static float nextFontSearch;

	private static string lastTip = "";

	private static float tipSince;

	private static void Styles()
	{
		if (text == null)
		{
			pixel = Texture2D.whiteTexture;
			text = new GUIStyle(GUI.skin.label)
			{
				fontSize = 15,
				normal = 
				{
					textColor = Color.white
				}
			};
			title = new GUIStyle(text)
			{
				fontSize = 20,
				fontStyle = FontStyle.Bold
			};
			small = new GUIStyle(text)
			{
				fontSize = 12
			};
			button = new GUIStyle(GUI.skin.button)
			{
				fontSize = 13,
				normal = 
				{
					textColor = Color.white
				}
			};
			tag = new GUIStyle(text)
			{
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold
			};
		}
	}

	private static void Box(Rect r, Color c)
	{
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(r, pixel);
		GUI.color = color;
	}

	internal static void Icon(Texture tex, Rect rect, Color tint)
	{
		Color color = GUI.color;
		GUI.color = tint;
		GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, alphaBlend: true);
		GUI.color = color;
	}

	internal static void Restore()
	{
		VanillaHud.Restore();
		RescueHint.Reset();
		for (int i = 0; i < 8; i++)
		{
			lastHealth[i] = -1;
		}
	}

	internal static void QueueRescueHint()
	{
		RescueHint.Queue();
	}

	internal static void ReleaseAssets()
	{
		PlayerProfilesUI.Reset();
		Restore();
		HudAssets.Release();
		RoomWaitArt.Release();
		MaskDisplay.Clear();
		SoulReserveBar.Release();
		MenuTheme.Release();
	}

	private static string Status(PlayerSlot p, Local8Runtime plugin)
	{
		if (p.Faulted)
		{
			return Charms.SanitizeLocalized("hud.recover_in", "LOCAL8", "RECUPERAR EN ") + MenuInput.Label;
		}
		if (!p.Hero || !p.Ready)
		{
			return Charms.SanitizeLocalized("hud.waiting", "LOCAL8", "ESPERANDO");
		}
		if (p.Down)
		{
			if (!PvpMatch.Running)
			{
				if (!plugin.TimedRespawn.Value)
				{
					return Charms.SanitizeLocalized("hud.down", "LOCAL8", "CAÍDO");
				}
				float remaining = TimedRespawnRecovery.Remaining(p, plugin.RespawnSeconds.Value);
				return remaining > 0f ? Mathf.CeilToInt(remaining) + "s" : Charms.SanitizeLocalized("hud.returning", "LOCAL8", "VOLVIENDO");
			}
			return Charms.SanitizeLocalized("hud.eliminated", "LOCAL8", "ELIMINADO");
		}
		if (!p.Connected)
		{
			return Charms.SanitizeLocalized("hud.no_device", "LOCAL8", "SIN DISPOSITIVO");
		}
		if (CoopEnding.NeedsFocus(p))
		{
			return UiLocalization.Get("hud.focus");
		}
		if (!p.Hazard)
		{
			return "";
		}
		return Charms.SanitizeLocalized("hud.returning", "LOCAL8", "VOLVIENDO");
	}

	internal static void Draw(Local8Runtime plugin)
	{
		Styles();
		CoopSession session = plugin.Session;
		float num = Mathf.Clamp((float)Screen.height / 900f, 0.65f, 1.6f);
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = Matrix4x4.Scale(new Vector3(num, num, 1f));
		float w = (float)Screen.width / num;
		float h = (float)Screen.height / num;
		try
		{
			GameManager instance = GameManager.instance;
			if (Event.current.type == EventType.Repaint && session.Active && session.Primary != null && (bool)instance && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition)
			{
				if (!plugin.BasicHud.Value)
				{
					HudAssets.Prepare();
				}
				foreach (PlayerSlot player in session.Players)
				{
					int num2 = player.Vitals.Health + player.Vitals.Blue;
					if (lastHealth[player.Index] >= 0 && num2 < lastHealth[player.Index])
					{
						player.HealthBeforeHit = lastHealth[player.Index];
						player.LastDamageAt = Time.unscaledTime;
						player.DamageFlashUntil = Time.unscaledTime + 0.4f;
					}
					lastHealth[player.Index] = num2;
					player.HudHealth.Observe(player.Vitals.Health, player.Vitals.Blue, player.CurrentMaxHealth,
						Time.unscaledTime, plugin.Performance.HudHealthEffects);
				}
				if (plugin.BasicHud.Value || !HudAssets.Ready)
				{
					DrawBasic(plugin, w);
				}
				else
				{
					DrawMasks(plugin, w);
				}
				if (plugin.ShowLabels.Value)
				{
					DrawLabels(session, num);
				}
				DrawRevival(session, num);
				DrawPvp(session, w, num);
			}
			if (!plugin.Panel && session.Active && session.Players.Count > 1 && (bool)instance && instance.IsGameplayScene())
			{
				NativeCharmCards(session, w, h, num);
				if (Charms.NativeMenuOpen) DrawTooltip(w, h);
			}
			if (!plugin.Panel && session.Active && (bool)instance && instance.IsGameplayScene())
			{
				TransitionBar(w, h);
				JoiningBar(session, w, h);
			}
			if (plugin.Panel)
			{
				DrawMenu(plugin, w, h);
			}
			else TitleLobby.Entry(plugin, w, h);
		}
		finally
		{
			GUI.matrix = matrix;
		}
		RoleUI.Draw(plugin);
		PvpRoundClock.Draw(plugin);
	}

	private static void DrawMenu(Local8Runtime plugin, float w, float h)
	{
		GUISkin previousSkin=GUI.skin;
		bool previousEnabled=GUI.enabled;Color previousColor=GUI.color;
		GUIStyle previousText=text,previousSmall=small,previousTitle=title,previousButton=button;
		try
		{
			GUI.skin=MenuTheme.Skin;
			text=MenuTheme.Label;small=MenuTheme.Small;title=MenuTheme.Heading;button=MenuTheme.ButtonStyle;
			if(Charms.Editing)CharmPanel(plugin,w,h);else Panel(plugin,w,h);
			DrawTooltip(w,h);
		}
		finally
		{
			GUI.skin=previousSkin;text=previousText;small=previousSmall;title=previousTitle;button=previousButton;
			GUI.enabled=previousEnabled;GUI.color=previousColor;
		}
	}

	private static void DrawBasic(Local8Runtime plugin, float w)
	{
		CoopSession session = plugin.Session;
		float num = Mathf.Min(210f * plugin.HudScale.Value, (w - 32f - (float)((session.Players.Count - 1) * 7)) / (float)session.Players.Count);
		float height = 57f * plugin.HudScale.Value;
		float num2 = num * (float)session.Players.Count + (float)(7 * (session.Players.Count - 1));
		float num3 = (w - num2) * 0.5f;
		foreach (PlayerSlot player in session.Players)
		{
			Rect r = new Rect(num3 + (float)player.Index * (num + 7f), 12f, num, height);
			Box(r, new Color(0.035f, 0.04f, 0.075f, 0.84f));
			Box(new Rect(r.x, r.y, 3f, r.height), player.Color);
			GUI.Label(new Rect(r.x + 8f, r.y + 2f, r.width - 12f, 20f), "P" + (player.Index + 1) + " " + Status(player, plugin), new GUIStyle(small)
			{
				fontStyle = FontStyle.Bold,
				normal = 
				{
					textColor = player.Color
				}
			});
			Color before = GUI.color;
			try
			{
				float fade = player.HudHealth.Fade(Time.unscaledTime);
				GUI.color = new Color(1f, 1f, 1f, 1f - .3f * fade);
				GUI.Label(new Rect(r.x + 8f, r.y + 22f, r.width - 12f, 20f), UiLocalization.Get("hud.health") + Mathf.Max(0, player.Vitals.Health) + ((player.Vitals.Blue > 0) ? (" +" + player.Vitals.Blue) : "") + " / " + player.CurrentMaxHealth, small);
			}
			finally { GUI.color = before; }
			Bar(new Rect(r.x + 8f, r.yMax - 6f, r.width - 16f, 3f), (float)player.Vitals.Soul / (float)Mathf.Max(1, player.Vitals.MaxSoul), player.Color);
			if (player.ReviveProgress > 0f)
			{
				Bar(new Rect(r.x + 8f, r.yMax - 12f, r.width - 16f, 3f), player.ReviveProgress, Color.white);
			}
		}
	}

	private static void Bar(Rect rect, float fraction, Color color)
	{
		Box(rect, new Color(0.12f, 0.14f, 0.19f, 0.85f));
		rect.width *= Mathf.Clamp01(fraction);
		Box(rect, color);
	}

	private static void DrawMasks(Local8Runtime plugin, float w)
	{
		CoopSession session = plugin.Session;
		float num = (float)CoopRules.HudScale(session.Players.Count, w, plugin.HudScale.Value);
		float num2 = 254f * num;
		float num3 = 8f;
		float num4 = (w - ((float)session.Players.Count * num2 + (float)(session.Players.Count - 1) * num3)) * 0.5f;
		foreach (PlayerSlot player in session.Players)
		{
			float num5 = num4 + (float)player.Index * (num2 + num3);
			float num6 = 12f;
			Color color = player.Color;
			HudAssets.Images images = HudAssets.For(player);
			Texture2D texture2D = ((images != null && (bool)images.Mask) ? images.Mask : HudAssets.Mask);
			Texture2D texture = ((images != null && (bool)images.Soul) ? images.Soul : HudAssets.Soul);
			GUIStyle style = new GUIStyle(small)
			{
				fontSize = Mathf.Max(10, Mathf.RoundToInt(13f * num)),
				fontStyle = FontStyle.Bold,
				normal = 
				{
					textColor = color
				}
			};
			GUI.Label(new Rect(num5 + 7f * num, num6, num2 - 14f * num, 20f), "P" + (player.Index + 1) + " " + Status(player, plugin), style);
			Rect rect = new Rect(num5 + 7f * num, num6 + 24f * num, 56f * num, 56f * num);
			SoulDisplay.Icon(texture, rect, new Color(0.22f, 0.25f, 0.32f), player);
			float num7 = Mathf.Clamp01((float)player.Vitals.Soul / (float)Mathf.Max(1, SoulDisplay.Capacity(player.Vitals)));
			if (num7 > 0f)
			{
				GUI.BeginGroup(new Rect(rect.x, rect.y + rect.height * (1f - num7), rect.width, rect.height * num7));
				try
				{
					SoulDisplay.Icon(texture, new Rect(0f, (0f - rect.height) * (1f - num7), rect.width, rect.height), color, player);
				}
				finally
				{
					GUI.EndGroup();
				}
			}
			if (images != null && (bool)images.Frame)
			{
				Icon(images.Frame, new Rect(rect.x - 5f * num, rect.y - 13f * num, rect.width + 15f * num, rect.height + 23f * num), color);
			}
			int currentMaxHealth = player.CurrentMaxHealth;
			int num8 = Mathf.Max(0, player.Vitals.Blue);
			int num9 = Mathf.Max(currentMaxHealth + num8, player.HudHealth.Count(Time.unscaledTime));
			int num10 = Mathf.Max(5, Mathf.Min(10, Mathf.CeilToInt((float)num9 / 2f)));
			int b = Mathf.CeilToInt((float)num9 / (float)num10);
			float num11 = Mathf.Min(29f * num, 58f * num / (float)Mathf.Max(2, b));
			float num12 = Mathf.Min(27f * num, 180f * num / (float)num10);
			for (int i = 0; i < num9; i++)
			{
				bool flag2 = i < player.Vitals.Health || i >= currentMaxHealth && i < currentMaxHealth + num8;
				Color color2 = ((!flag2) ? new Color(0.2f, 0.22f, 0.28f, 0.85f) : ((i >= currentMaxHealth) ? new Color(0.45f, 0.85f, 1f) : color));
				Texture2D texture2 = texture2D;
				if (images != null)
				{
					if (flag2 && i >= currentMaxHealth && (bool)images.Blue)
					{
						texture2 = images.Blue;
					}
					else if (!flag2 && (bool)images.Empty)
					{
						texture2 = images.Empty;
						color2 = color;
					}
				}
				float fade = player.HudHealth.Fade(Time.unscaledTime);
				bool gained = player.HudHealth.Gained(i) && fade > 0f;
				if (gained)
				{
					color2 = Color.Lerp(color2, Color.white, .45f * fade);
					color2.a *= 1f - .35f * fade;
				}
				Rect mask = new Rect(num5 + 67f * num + (float)(i % num10) * num12, num6 + 23f * num + (float)(i / num10) * num11, num12 - 2f * num, num11 - 2f * num);
				// Retired blue/max-health cells disappear with their fading full mask.
				if (i < currentMaxHealth + num8) MaskDisplay.Icon(texture2, mask, color2, player);
				if (fade > 0f && player.HudHealth.Lost(i))
				{
					bool blue = i >= player.HudHealth.PreviousMaximum;
					Texture2D previousMask = blue && images != null && images.Blue ? images.Blue : texture2D;
					Color ghost = blue ? new Color(.45f, .85f, 1f) : color;
					ghost.a = .85f * fade;
					mask.y -= (1f - fade) * 2f * num;
					MaskDisplay.Icon(previousMask, mask, ghost, player);
				}
				else if (gained)
				{
					float rim = 1.1f * num * fade;
					MaskDisplay.Icon(texture2, new Rect(mask.x - rim, mask.y - rim, mask.width + rim * 2, mask.height + rim * 2), new Color(1f, 1f, 1f, .18f * fade), player);
				}
			}
			if (player.Vitals.Reserve > 0)
			{
				float reserveCapacity = session.Data != null ? Mathf.Clamp01(session.Data.MPReserveMax / 99f) : 1f;
				SoulReserveBar.Draw(new Rect(rect.x, rect.yMax + 4f * num, rect.width, 3f * num),
					player.Vitals.Reserve / 99f, reserveCapacity, color);
			}
			if (player.ReviveProgress > 0f)
			{
				Bar(new Rect(num5 + 6f * num, num6 + 99f * num, num2 - 12f * num, 3f * num), player.ReviveProgress, Color.white);
			}
		}
	}

	private static void DrawLabels(CoopSession s, float scale)
	{
		if (!Camera.main)
		{
			return;
		}
		UIManager instance = UIManager.instance;
		if (!instance || instance.uiState != UIState.PLAYING || GameManager.instance.isPaused || s.Data.disablePause || Plugin.Self.Panel || ShopMenuRouting.MenuVisible || PickupCard.Owner != null || Charms.NativeMenuOpen || StagMenuRouting.HasOwner || ScriptedParty.Active || CoopEnding.Active)
		{
			return;
		}
		GameCameras instance2 = GameCameras.instance;
		if (!instance2)
		{
			return;
		}
		PlayMakerFSM cameraFadeFSM = instance2.cameraFadeFSM;
		if (!cameraFadeFSM || !(cameraFadeFSM.ActiveStateName == "Normal"))
		{
			return;
		}
		GUI.depth = 5000;
		Camera main = Camera.main;
		if (!main)
		{
			return;
		}
		foreach (PlayerSlot player in s.Players)
		{
			if ((bool)player.Hero && player.Alive && !TransitionVote.Holding(player))
			{
				Vector3 vector = main.WorldToScreenPoint(player.Hero.transform.position + Vector3.up * 1.9f);
				if (!(vector.z <= 0f))
				{
					Rect position = new Rect(vector.x / scale - 20f, ((float)Screen.height - vector.y) / scale - 12f, 40f, 22f);
					GUI.Label(new Rect(position.x + 1f, position.y + 1f, position.width, position.height), "P" + (player.Index + 1), new GUIStyle(tag)
					{
						normal = 
						{
							textColor = Color.black
						}
					});
					GUI.Label(position, "P" + (player.Index + 1), new GUIStyle(tag)
					{
						normal = 
						{
							textColor = player.Color
						}
					});
				}
			}
		}
		GUI.depth = -10000;
	}

	private static void DeviceRow(CoopSession s, InputDevice d, string name, float cw, ref float row)
	{
		InputDevice d2 = d;
		PlayerSlot playerSlot = s.Players.FirstOrDefault((PlayerSlot p) => p.Device == d2);
		bool compact = cw < 420f;
		MenuTheme.Text(new Rect(5f, row, compact ? cw - 10f : cw - 230f, compact ? 42f : 29f), name + ((playerSlot != null) ? (" - P" + (playerSlot.Index + 1)) : UiLocalization.Get("common.unassigned_suffix")), MenuTheme.Paper, 13);
		if (compact) row += 46f;
		float width = compact ? (cw - 15f) * .5f : 105f;
		float left = compact ? 5f : cw - 220f;
		float right = compact ? 10f + width : cw - 110f;
		if (d != InputDevice.Null && playerSlot == null && s.Players.Count < 8 && MenuTheme.Button(new Rect(left, row, width, 29f), UiLocalization.Get("action.join"), button))
		{
			s.Join(d2);
		}
		if (MenuTheme.Choice(new Rect(right, row, width, 29f), Help(UiLocalization.Get("common.assign_p1"), UiLocalization.Get("input.assign_tip")), s.Primary.Device == d2))
		{
			s.AssignPrimary(d2);
		}
		row += 35f;
	}

	private static void ChangeTab(int next)
	{
		if(tab==next)return;
		tab=next;scroll=Vector2.zero;ActionKeys.Capturing=0;MenuInput.Waiting=false;lastTip="";
	}

	private static void Panel(Local8Runtime plugin, float w, float h)
	{
		CoopSession session=plugin.Session;
		float width=MenuLayout.Width(w),height=MenuLayout.Height(h),x=(w-width)*.5f,y=(h-height)*.5f;
		Rect panel=new Rect(x,y,width,height);
		Box(new Rect(0,0,w,h),new Color(.005f,.009f,.019f,.27f));
		MenuTheme.Frame(panel);
		MenuTheme.Centered(new Rect(x+78,y+21,width-156,32),ReleaseInfo.Name,MenuTheme.Paper,23);
		MenuTheme.Centered(new Rect(x+78,y+56,width-156,20),ReleaseInfo.Version+"   ·   "+MenuInput.Label+UiLocalization.Get("panel.toggle_hint"),MenuTheme.Muted,12);
		MenuTheme.HeaderArt(new Rect(x+width*.5f-180,y+73,360,42));
		if(MenuTheme.IconButton(new Rect(x+width-65,y+24,34,34),MenuArt.Close,UiLocalization.Get("common.close")))plugin.SetPanel(false);
		string[] tabs={UiLocalization.Get("tab.players"),UiLocalization.Get("tab.game"),"PvP",UiLocalization.Get("tab.settings")};
		string[] tips={UiLocalization.Get("menu.players_tip"),UiLocalization.Get("menu.game_tip"),UiLocalization.Get("menu.pvp_tip"),UiLocalization.Get("menu.settings_tip")};
		bool wide=MenuLayout.Wide(width);float side=MenuLayout.Sidebar(width);
		float top=MenuLayout.BodyTop(width);
		if(wide)
		{
			Box(new Rect(x+16,y+MenuLayout.Header,side-12,height-MenuLayout.Header-MenuLayout.Footer),new Color(.023f,.028f,.042f,1));
			for(int i=0;i<tabs.Length;i++)if(MenuTheme.Nav(new Rect(x+24,y+MenuLayout.Header+14+i*65,side-28,58),tabs[i],tips[i],i,tab==i||(tab==4&&i==0)))ChangeTab(i);
			MenuTheme.Text(new Rect(x+30,y+height-MenuLayout.Footer-94,side-36,24),UiLocalization.Get("players.title"),MenuTheme.Muted,13);
			MenuTheme.Text(new Rect(x+30,y+height-MenuLayout.Footer-67,side-36,25),(session.Primary==null?plugin.Lobby.Count:session.Players.Count)+" / 8",MenuTheme.Paper,20);
			foreach(PlayerSlot p in session.Players)MenuTheme.Mark(new Rect(x+30+p.Index*18,y+height-MenuLayout.Footer-29,12,16),p.Color);
		}
		else
		{
			for(int i=0;i<tabs.Length;i++)if(MenuTheme.Choice(new Rect(x+24+i%2*(width-48)/2,y+MenuLayout.Header+i/2*43,(width-48)/2-5,39),new GUIContent(tabs[i],tips[i]),tab==i||(tab==4&&i==0)))ChangeTab(i);
		}
		InputDevice[] devices=tab==0?Controls.Devices().ToArray():Array.Empty<InputDevice>();
		Rect area=new Rect(x+side+24,y+top+8,width-side-46,height-top-MenuLayout.Footer-16);
		float visible=Mathf.Max(1,area.height),content=MenuLayout.ContentHeight(menuHeight[Mathf.Clamp(tab,0,4)],visible);
		scroll.y=MenuLayout.ClampScroll(scroll.y,content,visible);
		scroll=GUI.BeginScrollView(area,scroll,new Rect(0,0,area.width-17,content),false,false,GUIStyle.none,GUI.skin.verticalScrollbar);
		int drawnTab=tab;
		float row=0,cw=area.width-24;
		try
		{
			if(tab==0)PlayerOptions(plugin,session,devices,cw,ref row);
			else if(tab==1)GameOptions(plugin,cw,ref row);
			else if(tab==2)PvpOptions(plugin,session,cw,ref row);
			else AdvancedOptions(plugin,session,cw,ref row);
			menuHeight[Mathf.Clamp(drawnTab,0,4)]=row;
		}
		finally{GUI.EndScrollView();}
		float bottom=y+height-MenuLayout.Footer;
		MenuTheme.Divider(new Rect(x+width*.5f-192,bottom-11,384,26));
		float cell=(width-48)/4;
		if(MenuTheme.Choice(new Rect(x+27.5f,bottom+14,cell-7,34),Help(MenuInput.Key==KeyCode.F9?UiLocalization.Get("action.join"):UiLocalization.Get("action.join_f9"),UiLocalization.Get("menu.join_tip")),true))
		{
			if(session.Primary==null)plugin.Lobby.JoinFirstAvailable();else session.JoinFirstAvailable();
		}
		bool prior=GUI.enabled;
		try
		{
			GUI.enabled=prior&&session.Primary!=null;
			if(MenuTheme.Button(new Rect(x+27.5f+cell,bottom+14,cell-7,34),Help(UiLocalization.Get("action.gather"),UiLocalization.Get("menu.gather_tip")),button))session.GatherNow();
			if(MenuTheme.Button(new Rect(x+27.5f+cell*2,bottom+14,cell-7,34),Help(UiLocalization.Get("action.repair"),UiLocalization.Get("menu.repair_tip")),button))session.RepairPlayers();
			GUI.enabled=prior&&(session.Primary==null?plugin.Lobby.Count>0:session.Players.Count>1);
			if(MenuTheme.Button(new Rect(x+27.5f+cell*3,bottom+14,cell-7,34),Help(UiLocalization.Get("action.remove_last"),UiLocalization.Get("menu.remove_tip")),button))
			{
				if(session.Primary==null)plugin.Lobby.RemoveLast();else session.RemoveLast();
			}
		}
		finally{GUI.enabled=prior;}
		MenuTheme.Centered(new Rect(x+76,bottom+55,width-152,26),Time.unscaledTime<plugin.MessageUntil?plugin.Message:UiLocalization.Get("menu.hover_hint"),MenuTheme.Muted,13);
	}

	private static void PlayerOptions(Local8Runtime plugin,CoopSession s,InputDevice[] devices,float cw,ref float row)
	{
		if(s.Primary==null){TitleLobby.Players(plugin,cw,ref row);return;}
		MenuTheme.Section(UiLocalization.Get("input.p1_title"),cw,ref row,0);
		MenuTheme.Text(new Rect(5,row,cw-10,28),"P1: "+(Controls.IsKeyboard(s.Primary.Device)?UiLocalization.Get("input.keyboard"):Controls.Label(s.Primary.Device)),MenuTheme.Paper,15);row+=34;
		if(MenuTheme.Choice(new Rect(5,row,cw-10,35),Help(UiLocalization.Get("input.keyboard"),UiLocalization.Get("input.keyboard_tip")),Controls.IsKeyboard(s.Primary.Device)))s.AssignPrimary(InputDevice.Null);
		row+=43;
		MenuTheme.Text(new Rect(5,row,cw-10,60),UiLocalization.Get("input.p1_hint"),MenuTheme.Muted,13);row+=66;
		foreach(InputDevice device in devices)DeviceRow(s,device,Controls.Label(device),cw,ref row);
		row+=8;
		MenuTheme.Section(UiLocalization.Get("players.title"),cw,ref row);
		int columns=cw>=470?2:1;float cell=(cw-10)/columns;int n=0;
		foreach(PlayerSlot player in s.Players)
		{
			Rect card=new Rect(8.5f+n%columns*cell,row+n/columns*85,cell-7,76);
			if(MenuTheme.Card(card,Help("",UiLocalization.Get("menu.customize_tip")),selected==player.Index,player.Color))selected=player.Index;
			MenuTheme.DeviceGlyph(new Rect(card.x+12,card.y+26,24,24),Controls.IsKeyboard(player.Device),player.Color);
			MenuTheme.Text(new Rect(card.x+44,card.y+12,42,24),"P"+(player.Index+1),player.Color,20);
			MenuTheme.Text(new Rect(card.x+91,card.y+12,card.width-106,24),string.IsNullOrEmpty(player.ProfileId)?player.DeviceName:PlayerProfiles.Display(player),MenuTheme.Paper,14);
			string detail=player.Connected?(Controls.IsKeyboard(player.Device)?UiLocalization.Get("input.keyboard"):Controls.Label(player.Device)):UiLocalization.Get("players.disconnected").Trim();
			MenuTheme.Text(new Rect(card.x+44,card.y+41,card.width-59,20),detail,MenuTheme.Muted,12);n++;
		}
		row+=Mathf.CeilToInt(n/(float)columns)*85+4;
		selected=Mathf.Clamp(selected,0,s.Players.Count-1);PlayerSlot p=s.Players[selected];
		MenuTheme.Section(UiLocalization.Get("appearance.player")+(selected+1),cw,ref row);
		MenuTheme.Text(new Rect(5,row,120,26),UiLocalization.Get("common.color"),MenuTheme.Muted,14);
		float swatch=Mathf.Min(31,(cw-130)/palette.Length);
		for(int i=0;i<palette.Length;i++)
		{
			Rect r=new Rect(130+i*swatch,row,swatch-4,25);Box(r,palette[i]);
			if(MenuTheme.Button(r,Help("",UiLocalization.Get("common.color")),GUIStyle.none)){p.Color=palette[i];Local8Mod.Settings.Colors[selected]=ColorUtility.ToHtmlStringRGB(p.Color);}
			if(ColorUtility.ToHtmlStringRGB(p.Color)==ColorUtility.ToHtmlStringRGB(palette[i]))MenuArt.Nine(new Rect(r.x-3,r.y-3,r.width+6,r.height+6),MenuArt.Selection,Color.white);
		}
		row+=35;
		for(int i=0;i<3;i++)
		{
			float value=i==0?p.Color.r:i==1?p.Color.g:p.Color.b;
			Color channel=i==0?new Color(.95f,.48f,.50f):i==1?new Color(.48f,.86f,.59f):new Color(.47f,.65f,.97f);
            MenuTheme.Text(new Rect(5,row,26,24),i==0?"R":i==1?"G":"B",channel,13);
            float next=MenuTheme.Slider(new Rect(40,row+3,cw-103,24),value,0,1,channel);
			MenuTheme.Text(new Rect(cw-52,row,47,24),Mathf.RoundToInt(next*255).ToString(),MenuTheme.Muted,13);
			if(Mathf.Abs(next-value)>.001f){Color c=p.Color;if(i==0)c.r=next;else if(i==1)c.g=next;else c.b=next;p.Color=c;Local8Mod.Settings.Colors[selected]=ColorUtility.ToHtmlStringRGB(c);}
			row+=25;
		}
		row+=5;
		MenuTheme.Text(new Rect(5,row,105,30),UiLocalization.Get("common.skin"),MenuTheme.Muted,14);
		if(MenuTheme.IconButton(new Rect(115,row,36,30),MenuArt.Arrow,UiLocalization.Get("common.skin"),true))SkinBridge.Cycle(p,-1);
		MenuTheme.Text(new Rect(160,row,cw-210,30),SkinBridge.Display(p),MenuTheme.Paper,15);
		if(MenuTheme.IconButton(new Rect(cw-43,row,36,30),MenuArt.Arrow,UiLocalization.Get("common.skin")))SkinBridge.Cycle(p,1);
		row+=42;
		if(MenuTheme.Button(new Rect(5,row,cw*.49f-8,35),UiLocalization.Get("charms.player")+(selected+1),button))Charms.Open(p);
		if(p.Index>0&&Controls.IsKeyboard(p.Device)&&MenuTheme.Button(new Rect(cw*.5f,row,cw*.49f,35),UiLocalization.Get("menu.controls"),button)){addingKeyboard=false;ChangeTab(4);}
		row+=45;
		PlayerProfilesUI.Draw(s,p,cw,ref row);
		MenuTheme.Section(UiLocalization.Get("keyboard.section"),cw,ref row);
		if(MenuTheme.Button(new Rect(5,row,cw-10,34),UiLocalization.Get("keyboard.add"),button)){addingKeyboard=true;ChangeTab(4);}
		row+=43;
		MenuTheme.Section(UiLocalization.Get("menu.display"),cw,ref row);
		if(MenuTheme.Toggle(new Rect(5,row,cw-10,38),UiLocalization.Get("labels.prefix"),plugin.ShowLabels.Value))plugin.ShowLabels.Value=!plugin.ShowLabels.Value;
		row+=46;
		if(MenuTheme.Button(new Rect(5,row,cw-10,35),UiLocalization.Get("interface.prefix")+UiLocalization.Get(plugin.BasicHud.Value?"hud.basic":"hud.full"),button))plugin.BasicHud.Value=!plugin.BasicHud.Value;
		row+=42;
		MenuTheme.Text(new Rect(5,row,cw-240,34),UiLocalization.Get("menu.hud_scale"),MenuTheme.Muted,14);
		if(MenuTheme.Step(new Rect(cw-228,row,65,34),false))plugin.HudScale.Value=Mathf.Max(.7f,plugin.HudScale.Value-.1f);
		MenuTheme.Centered(new Rect(cw-153,row,70,34),Mathf.RoundToInt(plugin.HudScale.Value*100)+"%",MenuTheme.Paper,15);
		if(MenuTheme.Step(new Rect(cw-73,row,65,34),true))plugin.HudScale.Value=Mathf.Min(1.5f,plugin.HudScale.Value+.1f);
		row+=42;
	}

	private static void KeyboardOptions(Local8Runtime plugin,float cw,ref float row)
	{
		CoopSession s=plugin.Session;
		if(!addingKeyboard&&selected>=1&&selected<s.Players.Count&&Controls.IsKeyboard(s.Players[selected].Device)&&s.Players[selected].Actions!=null)
		{
			PlayerSlot p=s.Players[selected];
			MenuTheme.Section("P"+(p.Index+1)+"  ·  "+UiLocalization.Get("menu.controls"),cw,ref row,0);
			KeyboardWarning.Draw(cw,ref row);
			MenuTheme.Text(new Rect(5,row,cw-10,44),UiLocalization.Get("bind.help"),MenuTheme.Muted,14);row+=52;
			var a=p.Actions;
			PlayerAction[] actions={a.up,a.down,a.left,a.right,a.jump,a.attack,a.dash,a.cast,a.focus,a.dreamNail,a.superDash,a.quickMap,a.paneRight};
			string[] names={"up","down","left","right","jump","attack","dash","cast","focus","dreamnail","superdash","map","rescue"};
			int columns=cw>=470?2:1;float cell=(cw-10)/columns;
			for(int i=0;i<actions.Length;i++)
			{
				float x=5+(i%columns)*cell,top=row+(i/columns)*65;
				MenuTheme.Text(new Rect(x,top,cell-9,23),UiLocalization.Get("menu.bind."+names[i]),MenuTheme.Paper,14);
				string key=ActionKeys.Capturing==i+10?UiLocalization.Get("bind.capture_short"):actions[i].Bindings.Count>0?actions[i].Bindings[0].Name:"-";
				if(MenuTheme.Choice(new Rect(x,top+24,cell-9,33),Help(key,UiLocalization.Get("bind.capture")),ActionKeys.Capturing==i+10)){ActionKeys.Capturing=i+10;MenuInput.Waiting=false;}
			}
			row+=Mathf.CeilToInt(actions.Length/(float)columns)*65+8;
			if(MenuTheme.Button(new Rect(5,row,cw*.49f-7,34),UiLocalization.Get("common.reset"),button))
			{
				ActionKeys.Capturing=0;
				Controls.ResetKeyboard(p,s.OriginalActions);
			}
			if(MenuTheme.Button(new Rect(cw*.5f,row,cw*.49f,34),UiLocalization.Get("common.back"),button))ChangeTab(0);
			row+=43;return;
		}
		MenuTheme.Section(UiLocalization.Get("keyboard.new_title"),cw,ref row,0);
		KeyboardWarning.Draw(cw,ref row);
		int count=s.Players.Count(p=>Controls.IsKeyboard(p.Device));
		MenuTheme.Text(new Rect(5,row,cw-10,40),UiLocalization.Get("keyboard.new_intro"),MenuTheme.Paper,15);row+=48;
		MenuTheme.Text(new Rect(5,row,cw-10,54),UiLocalization.Get("bind.help"),MenuTheme.Muted,14);row+=62;
		MenuTheme.Text(new Rect(5,row,cw-10,38),UiLocalization.Get("keyboard.p1"),MenuTheme.Muted,14);row+=43;
		MenuTheme.Text(new Rect(5,row,cw-10,38),UiLocalization.Get("keyboard.controllers"),MenuTheme.Muted,14);row+=43;
		MenuTheme.Text(new Rect(5,row,cw-10,38),UiLocalization.Get("keyboard.mouse"),MenuTheme.Muted,14);row+=51;
		bool enabled=GUI.enabled;
		try
		{
			GUI.enabled=enabled&&count<4&&s.Players.Count<8;
			if(MenuTheme.Choice(new Rect(5,row,cw-10,38),new GUIContent(UiLocalization.Get("keyboard.add_now")),true))
			{
				if(s.CanJoin){s.Join(InputDevice.Null);selected=s.Players.Count-1;addingKeyboard=false;scroll=Vector2.zero;}
				else plugin.Notice(UiLocalization.Get("keyboard.wait"));
			}
		}
		finally{GUI.enabled=enabled;}
		row+=46;
		if(count>=4||s.Players.Count>=8){MenuTheme.Text(new Rect(5,row,cw-10,38),UiLocalization.Get(count>=4?"keyboard.limit":"group.full"),MenuTheme.Muted,14);row+=44;}
		if(MenuTheme.Button(new Rect(5,row,cw-10,34),UiLocalization.Get("common.back"),button))ChangeTab(0);
		row+=43;
	}

	private static void GameOptions(Local8Runtime plugin,float cw,ref float row)
	{
		if(tab==4){KeyboardOptions(plugin,cw,ref row);return;}
		MenuTheme.Section(UiLocalization.Get("difficulty.title"),cw,ref row,1);
		string[] levels={UiLocalization.Get("difficulty.easy"),UiLocalization.Get("difficulty.normal"),UiLocalization.Get("difficulty.hard"),UiLocalization.Get("difficulty.extreme")};
		float cell=(cw-10)/4;
		for(int i=0;i<4;i++)if(MenuTheme.Choice(new Rect(7.5f+i*cell,row,cell-5,38),Help(levels[i],UiLocalization.Get("difficulty.tip")),plugin.Difficulty.Value==i))plugin.Difficulty.Value=i;
		row+=43;
		MenuTheme.Section(UiLocalization.Get("camera.title"),cw,ref row);
		float half=(cw-15)/2;
		if(MenuTheme.Toggle(new Rect(5,row,half,44),UiLocalization.Get("camera.shared"),plugin.ZoomByPlayerCount.Value,UiLocalization.Get("camera.tip")))plugin.ZoomByPlayerCount.Value=!plugin.ZoomByPlayerCount.Value;
		if(MenuTheme.Toggle(new Rect(half+10,row,half,44),UiLocalization.Get("wait.label"),plugin.WaitForParty.Value,UiLocalization.Get("wait.tip")))plugin.WaitForParty.Value=!plugin.WaitForParty.Value;
		row+=49;
		MenuTheme.Text(new Rect(5,row,cw-170,34),UiLocalization.Get("camera.margin")+plugin.SideMargin.Value.ToString("0.0"),MenuTheme.Muted,14);
		if(MenuTheme.Step(new Rect(cw-148,row,65,34),false))plugin.SideMargin.Value=Mathf.Max(2.8f,plugin.SideMargin.Value-.5f);
		if(MenuTheme.Step(new Rect(cw-73,row,65,34),true))plugin.SideMargin.Value=Mathf.Min(10f,plugin.SideMargin.Value+.5f);
		row+=41;
		if(MenuTheme.Toggle(new Rect(5,row,half,44),UiLocalization.Get("darkness.label"),!plugin.GroupDarkness.Value,UiLocalization.Get("darkness.tip")))plugin.GroupDarkness.Value=!plugin.GroupDarkness.Value;
		if(MenuTheme.Toggle(new Rect(half+10,row,half,44),UiLocalization.Get("camera.player_lights"),plugin.PlayerLights.Value,UiLocalization.Get("camera.player_lights.tip")))plugin.PlayerLights.Value=!plugin.PlayerLights.Value;
		row+=47;
		if(MenuTheme.Toggle(new Rect(5,row,cw-10,38),UiLocalization.Get("camera.smooth_lights"),plugin.SmoothPlayerLights.Value,UiLocalization.Get("camera.smooth_lights.tip")))plugin.SmoothPlayerLights.Value=!plugin.SmoothPlayerLights.Value;
		row+=43;
		MenuTheme.Section(UiLocalization.Get("revive.title"),cw,ref row);
		if(MenuTheme.Toggle(new Rect(5,row,half,44),UiLocalization.Get("respawn.label"),plugin.TimedRespawn.Value,UiLocalization.Get("respawn.tip")))plugin.TimedRespawn.Value=!plugin.TimedRespawn.Value;
		if(MenuTheme.Toggle(new Rect(half+10,row,half,44),UiLocalization.Get("shade.label"),plugin.SpawnShades.Value,UiLocalization.Get("shade.tip")))plugin.SpawnShades.Value=!plugin.SpawnShades.Value;
		row+=48;
		RespawnSlider.Draw(plugin,cw,ref row);
		row+=2;
		PerformanceMenu.Draw(plugin,cw,ref row);
	}

	private static void TransitionBar(float w, float h)
	{
		if (TransitionVote.Pending)
		{
			float num = Mathf.Min(222f, w * 0.23f);
			float num2 = w - num - 23f;
			float num3 = h - 92f;
			RoomWaitArt.Draw(new Rect(num2 - 8f, num3 - 27f, num + 16f, 83f));
			GUI.Label(new Rect(num2, num3 - 8f, num, 22f), Charms.SanitizeLocalized("hud.waiting_bar", "LOCAL8", "ESPERANDO  ") + TransitionVote.Count, new GUIStyle(small)
			{
				font = RoomWaitArt.GameFont,
				alignment = TextAnchor.MiddleCenter,
				fontSize = 13,
				fontStyle = FontStyle.Bold,
				normal = 
				{
					textColor = new Color(0.94f, 0.94f, 0.9f)
				}
			});
			Box(new Rect(num2 + 28f, num3 + 23f, num - 56f, 2f), new Color(0.62f, 0.67f, 0.76f, 0.85f));
			Box(new Rect(num2 + 28f, num3 + 23f, (num - 56f) * TransitionVote.Progress, 2f), new Color(0.97f, 0.97f, 1f));
		}
	}

	private static void JoiningBar(CoopSession s, float w, float h)
	{
		PlayerSlot playerSlot = null;
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Index > 0 && player.JoinVisualUntil > Time.unscaledTime)
			{
				playerSlot = player;
				break;
			}
		}
		if (playerSlot != null && !TransitionVote.Pending)
		{
			float num = Mathf.Min(206f, w * 0.23f);
			float num2 = w - num - 23f;
			float num3 = h - 89f;
			RoomWaitArt.Draw(new Rect(num2 - 8f, num3 - 24f, num + 16f, 77f));
			GUI.Label(new Rect(num2, num3 - 7f, num, 24f), "P" + (playerSlot.Index + 1) + UiLocalization.Get("hud.joined"), new GUIStyle(small)
			{
				font = RoomWaitArt.GameFont,
				alignment = TextAnchor.MiddleCenter,
				fontSize = 14,
				fontStyle = FontStyle.Bold,
				normal = 
				{
					textColor = new Color(0.94f, 0.92f, 0.86f)
				}
			});
			Box(new Rect(num2 + 34f, num3 + 26f, num - 68f, 2f), new Color(playerSlot.Color.r, playerSlot.Color.g, playerSlot.Color.b, 0.78f));
		}
	}

	private static void AdvancedOptions(Local8Runtime plugin, CoopSession s, float cw, ref float row)
	{
		if (tab == 4)
		{
			GameOptions(plugin, cw, ref row);
			return;
		}
		MenuTheme.Section(UiLocalization.Get("advanced.menu"),cw,ref row);
		if (MenuTheme.Button(new Rect(5f, row, cw - 160f, 37f), Help(MenuInput.Waiting ? Charms.SanitizeLocalized("bind.capture_any", "LOCAL8", "Pulsa una tecla o botón... Esc para cancelar") : (Charms.SanitizeLocalized("advanced.open", "LOCAL8", "Abrir menú: ") + MenuInput.Label), Charms.SanitizeLocalized("bind.help", "LOCAL8", "Selecciona una acción y pulsa la tecla o botón que quieras usar.")), button))
		{
			MenuInput.Waiting = true;
		}
		if (MenuTheme.Button(new Rect(cw - 150f, row, 145f, 37f), Help(Charms.SanitizeLocalized("common.reset", "LOCAL8", "Restablecer"), Charms.SanitizeLocalized("advanced.reset_tip", "LOCAL8", "Restaura F8 como tecla del panel.")), button))
		{
			MenuInput.Set(KeyCode.F8);
		}
		row += 49f;
		MenuTheme.Section(UiLocalization.Get("rescue.title"),cw,ref row);
		if (MenuTheme.Button(new Rect(5f, row, cw * 0.61f - 8f, 33f), Help((ActionKeys.Capturing == 1) ? Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar") : (Charms.SanitizeLocalized("common.keyboard", "LOCAL8", "Teclado: ") + ActionKeys.RescueLabel), Charms.SanitizeLocalized("rescue.tip", "LOCAL8", "Mantén la tecla para viajar como partículas oníricas hacia un compañero. Recarga: 25 s.")), button))
		{
			ActionKeys.Capturing = 1;
			MenuInput.Waiting = false;
		}
		if (MenuTheme.Button(new Rect(cw * 0.61f, row, cw * 0.39f - 5f, 33f), Help(Charms.SanitizeLocalized("common.controller", "LOCAL8", "Mando: ") + ActionKeys.ButtonLabel, Charms.SanitizeLocalized("rescue.controller_tip", "LOCAL8", "Botón de rescate para jugadores con mando.")), button))
		{
			Local8Mod.Settings.RescueButton = (Local8Mod.Settings.RescueButton + 1) % 4;
		}
		row += 45f;
		MenuTheme.Section(UiLocalization.Get("duel.title"),cw,ref row);
		if (MenuTheme.Button(new Rect(5f, row, cw - 10f, 33f), Help((ActionKeys.Capturing == 2) ? Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar") : (Charms.SanitizeLocalized("duel.toggle", "LOCAL8", "Iniciar / detener PvP: ") + ActionKeys.PvpLabel), Charms.SanitizeLocalized("duel.tip", "LOCAL8", "Inicia o detiene las rondas de duelo.")), button))
		{
			ActionKeys.Capturing = 2;
			MenuInput.Waiting = false;
		}
		row += 48f;
		MenuTheme.Section(UiLocalization.Get("virtual.title"),cw,ref row);
		GUI.Label(new Rect(5f, row + 4f, cw - 210f, 30f), Charms.SanitizeLocalized("vjoy.label", "LOCAL8", "vJoy / DirectInput: ") + Controls.VirtualCount + Charms.SanitizeLocalized("vjoy.detected", "LOCAL8", " detectados") + (Controls.NativeActive ? "" : Charms.SanitizeLocalized("vjoy.inactive", "LOCAL8", " (lector inactivo)")), small);
		if (MenuTheme.Button(new Rect(cw - 205f, row, 200f, 30f), Charms.SanitizeLocalized("vjoy.search", "LOCAL8", "Buscar vJoy"), button))
		{
			Controls.EnableDirectInput(requested: true);
		}
		row += 54f;
		MenuTheme.Section(UiLocalization.Get("diagnostic.title"),cw,ref row);
		DrawLogButtons(5f, row, cw);
		row += 43f;
		if (!plugin.BasicHud.Value && !HudAssets.Ready)
		{
			GUI.Label(new Rect(5f, row, cw, 45f), Charms.SanitizeLocalized("hud.loading", "LOCAL8", "Cargando los gráficos del HUD..."), small);
		}
	}

	private static void PvpOptions(Local8Runtime plugin, CoopSession session, float cw, ref float row)
	{
		Local8Settings settings=Local8Mod.Settings;
		string[] pages={UiLocalization.Get("menu.combat"),UiLocalization.Get("pvp.teams"),UiLocalization.Get("pvp.round_settings"),PvpArena.Text("title")};
		float cell=(cw-10)/4;
		for(int i=0;i<pages.Length;i++)if(MenuTheme.Choice(new Rect(7.5f+i*cell,row,cell-5,52),new GUIContent(pages[i]),pvpPage==i)&&pvpPage!=i){pvpPage=i;scroll=Vector2.zero;}
		row+=67;
		if(pvpPage==0)
		{
		MenuTheme.Section(UiLocalization.Get("pvp.title"),cw,ref row);
		string[] array = new string[3]
		{
			Charms.SanitizeLocalized("pvp.coop", "LOCAL8", "Cooperativo"),
			Charms.SanitizeLocalized("pvp.friendly", "LOCAL8", "Fuego amigo"),
			Charms.SanitizeLocalized("pvp.rounds", "LOCAL8", "Duelos por rondas")
		};
		float num = (cw - 10f) / 3f;
		for (int i = 0; i < 3; i++)
		{
			if (MenuTheme.Choice(new Rect(5f + (float)i * num, row, num - 5f, 38f), Help(array[i], i switch
			{
				1 => Charms.SanitizeLocalized("pvp.friendly_tip", "LOCAL8", "Aventura cooperativa con daño entre jugadores."), 
				0 => Charms.SanitizeLocalized("pvp.coop_tip", "LOCAL8", "Aventura cooperativa sin daño entre jugadores."), 
				_ => Charms.SanitizeLocalized("pvp.rounds_tip", "LOCAL8", "Combates por rondas con marcador y reaparición."), 
			}), settings.PvpMode == i))
			{
				PvpMatch.SetMode(i);
			}
		}
		row += 45f;
		MenuTheme.Section(UiLocalization.Get("pvp.damage"),cw,ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.nail", "LOCAL8", "Aguijón"), ref settings.PvpNailDamage, 1, 5, 1, cw, ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.spells", "LOCAL8", "Hechizos y rayos"), ref settings.PvpSpellDamage, 1, 5, 1, cw, ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.nailarts", "LOCAL8", "Artes del aguijón"), ref settings.PvpArtDamage, 1, 5, 1, cw, ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.charms", "LOCAL8", "Amuletos / mascotas / Sombra Afilada"), ref settings.PvpCharmDamage, 1, 5, 1, cw, ref row);
		if (MenuTheme.Toggle(new Rect(5f,row,cw-10f,38f),UiLocalization.Get("pvp.parry"),settings.PvpParry,UiLocalization.Get("pvp.parry_tip")))
		{
			settings.PvpParry = !settings.PvpParry;
		}
		row += 44f;
		if (MenuTheme.Toggle(new Rect(5f,row,cw-10f,38f),UiLocalization.Get("pvp.charm_attacks"),settings.PvpCharmAttacks,UiLocalization.Get("pvp.charm_attacks_tip")))
		{
			settings.PvpCharmAttacks = !settings.PvpCharmAttacks;
		}
		row += 44f;
		if (MenuTheme.Toggle(new Rect(5f,row,cw-10f,38f),UiLocalization.Get("pvp.soul_hit"),settings.PvpSoulOnHit,UiLocalization.Get("pvp.soul_hit_tip")))
		{
			settings.PvpSoulOnHit = !settings.PvpSoulOnHit;
		}
		row += 43f;
		}
		else if(pvpPage==1)
		{
		if(MenuTheme.Choice(new Rect(5,row,cw*.5f-8,35),new GUIContent("PvP"),!adventurePage))adventurePage=false;
		if(MenuTheme.Choice(new Rect(cw*.5f,row,cw*.5f-5,35),new GUIContent(UiLocalization.Get("stats.adventure")),adventurePage))adventurePage=true;
		row+=43;
		if(adventurePage){AdventureStatsUI.Draw(session,cw,ref row);return;}
		MenuTheme.Section(UiLocalization.Get("pvp.teams"),cw,ref row);
		foreach (PlayerSlot player in session.Players)
		{
			int index = player.Index;
			GUI.Label(new Rect(5f, row + 4f, 40f, 25f), "P" + (index + 1), new GUIStyle(small)
			{
				normal = 
				{
					textColor = player.Color
				}
			});
			if (MenuTheme.Button(new Rect(50f, row, 110f, 30f), Help((settings.PvpTeams[index] == 0) ? Charms.SanitizeLocalized("pvp.free", "LOCAL8", "Libre") : (Charms.SanitizeLocalized("pvp.team", "LOCAL8", "Equipo ") + settings.PvpTeams[index]), Charms.SanitizeLocalized("pvp.free_tip", "LOCAL8", "Libre: todos contra todos. Los compañeros de equipo no se hacen daño.")), button))
			{
				PvpMatch.Stop(restorePosition: true);
				settings.PvpTeams[index] = (settings.PvpTeams[index] + 1) % 5;
				PvpCombat.Invalidate();
			}
			GUI.Label(new Rect(175f, row + 5f, cw - 180f, 25f), UiLocalization.Get("pvp.round_prefix") + PvpCombat.Wins[index] + Charms.SanitizeLocalized("pvp.kills", "LOCAL8", "   Bajas ") + PvpCombat.Kills[index] + Charms.SanitizeLocalized("pvp.deaths", "LOCAL8", "   Caídas ") + PvpCombat.Deaths[index] + Charms.SanitizeLocalized("pvp.parries", "LOCAL8", "   Parries ") + PvpCombat.Parries[index], small);
			row += 37f;
		}
		row += 8f;
		}
		else if(pvpPage==2)
		{
		MenuTheme.Section(UiLocalization.Get("pvp.round_settings"),cw,ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.start_soul", "LOCAL8", "Alma inicial"), ref settings.DuelSoul, 0, 99, 33, cw, ref row);
		PvpRoundClock.Number(Charms.SanitizeLocalized("pvp.round_time", "LOCAL8", "Tiempo por ronda (segundos)"), ref settings.DuelSeconds, 30, 600, 30, cw, ref row);
		GUI.Label(new Rect(8f, row, cw - 178f, 34f), Charms.SanitizeLocalized("pvp.bestof", "LOCAL8", "Mejor de: ") + ((settings.DuelBestOf == 0) ? Charms.SanitizeLocalized("pvp.unlimited", "LOCAL8", "sin límite") : settings.DuelBestOf.ToString()), MenuTheme.Label);
		if (MenuTheme.Step(new Rect(cw - 148f, row, 65f, 34f), false))
		{
			settings.DuelBestOf = PvpRules.SeriesLength(settings.DuelBestOf - 2);
		}
		if (MenuTheme.Step(new Rect(cw - 73f, row, 65f, 34f), true))
		{
			settings.DuelBestOf = PvpRules.SeriesLength((settings.DuelBestOf == 0) ? 1 : (settings.DuelBestOf + 2));
		}
		row += 41f;
		if (MenuTheme.Button(new Rect(5f, row, cw - 10f, 31f), Help(Charms.SanitizeLocalized("pvp.duel_key", "LOCAL8", "Tecla de duelo: ") + ((ActionKeys.Capturing == 2) ? Charms.SanitizeLocalized("bind.capture_short", "LOCAL8", "Pulsa una tecla...") : ActionKeys.PvpLabel), Charms.SanitizeLocalized("pvp.duel_key_tip", "LOCAL8", "Cambia la tecla que inicia o detiene el PvP.")), button))
		{
			ActionKeys.Capturing = 2;
			MenuInput.Waiting = false;
		}
		row += 39f;
		}
		else
		{
			PvpArenaUI.Draw(plugin,session,cw,ref row);return;
		}
		row+=12;
		if (MenuTheme.Button(new Rect(5f, row, cw * 0.48f, 34f), Help(PvpMatch.Running ? Charms.SanitizeLocalized("pvp.end_duel", "LOCAL8", "Terminar duelo") : Charms.SanitizeLocalized("pvp.start_rounds", "LOCAL8", "Iniciar rondas"), Charms.SanitizeLocalized("pvp.start_tip", "LOCAL8", "Empieza desde las posiciones actuales tras una cuenta atrás de 3 segundos.")), button))
		{
			if (PvpMatch.Running)
			{
				PvpMatch.Stop(restorePosition: true);
				plugin.Notice(Charms.SanitizeLocalized("pvp.ended", "LOCAL8", "Duelo terminado."));
			}
			else
			{
				PvpMatch.SetMode(2);
				PvpMatch.RequestStart();
			}
		}
		if (MenuTheme.Button(new Rect(cw * 0.51f, row, cw * 0.48f, 34f), Help(Charms.SanitizeLocalized("pvp.clear", "LOCAL8", "Borrar marcador"), Charms.SanitizeLocalized("pvp.clear_tip", "LOCAL8", "Reinicia rondas ganadas, bajas y parries.")), button))
		{
			PvpCombat.ResetScores();
		}
		row += 42f;
		if (!string.IsNullOrEmpty(PvpMatch.StartIssue))
		{
			GUI.Label(new Rect(5f, row, cw - 10f, 42f), PvpMatch.StartIssue, new GUIStyle(small)
			{
				wordWrap = true,
				normal = 
				{
					textColor = new Color(1f, 0.75f, 0.65f)
				}
			});
			row += 46f;
		}
	}

	private static void PvpNumber(string label, ref int value, int min, int max, int step, float cw, ref float row)
	{
		value=Mathf.Clamp(value,min,max);
		GUI.Label(new Rect(8f, row, cw - 178f, 34f), label + ": " + value, MenuTheme.Label);
		if (MenuTheme.Step(new Rect(cw - 148f, row, 65f, 34f), false))
		{
			value = Mathf.Max(min, value - step);
		}
		if (MenuTheme.Step(new Rect(cw - 73f, row, 65f, 34f), true))
		{
			value = Mathf.Min(max, value + step);
		}
		row += 41f;
	}

	private static void FindNativeFont()
	{
		if ((bool)nativeFont || !(Time.unscaledTime >= nextFontSearch))
		{
			return;
		}
		nextFontSearch = Time.unscaledTime + 5f;
		Font[] array = Resources.FindObjectsOfTypeAll<Font>();
		foreach (Font font in array)
		{
			if (font.name.IndexOf("Trajan", StringComparison.OrdinalIgnoreCase) >= 0 || font.name.IndexOf("Perpetua", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				nativeFont = font;
				break;
			}
		}
	}

	private static void DrawPvp(CoopSession s, float w, float scale)
	{
		GameManager instance = GameManager.instance;
		if (PvpMatch.Running && !Plugin.Self.Panel && (bool)instance && !instance.isPaused && s.Gameplay && PvpMatch.BlocksInput)
		{
			FindNativeFont();
			if ((bool)nativeFont)
			{
				GUIStyle gUIStyle = new GUIStyle(tag)
				{
					font = nativeFont,
					fontStyle = FontStyle.Normal,
					fontSize = 56,
					normal = 
					{
						textColor = new Color(0.94f, 0.91f, 0.85f)
					}
				};
				string announcement = PvpMatch.Announcement;
				GUI.Label(new Rect(1f, (float)Screen.height / scale * 0.32f + 1f, w, 100f), announcement, new GUIStyle(gUIStyle)
				{
					normal = 
					{
						textColor = new Color(0f, 0f, 0f, 0.8f)
					}
				});
				GUI.Label(new Rect(0f, (float)Screen.height / scale * 0.32f, w, 100f), announcement, gUIStyle);
			}
		}
	}

	private static GUIContent Help(string label, string tip)
	{
		return new GUIContent(label, tip);
	}

	private static void DrawTooltip(float w, float h)
	{
		if (Event.current.type == EventType.Repaint)
		{
			string text = GUI.tooltip ?? "";
			if (text != lastTip)
			{
				lastTip = text;
				tipSince = Time.unscaledTime;
			}
			if (text.Length != 0 && !(Time.unscaledTime - tipSince < 0.35f) && Event.current.type == EventType.Repaint)
			{
				float num = Mathf.Min(345f, w - 24f);
				GUIStyle gUIStyle = MenuTheme.TooltipStyle;
				float num2 = gUIStyle.CalcHeight(new GUIContent(text), num);
				Vector2 mousePosition = Event.current.mousePosition;
				Rect rect = new Rect(Mathf.Clamp(mousePosition.x + 16f, 8f, w - num - 8f), Mathf.Clamp(mousePosition.y + 20f, 8f, h - num2 - 8f), num, num2);
				Box(rect, MenuTheme.Ink);
				MenuArt.Nine(rect,MenuArt.Selection,new Color(.64f,.69f,.76f,.8f));
				GUI.Label(rect, text, gUIStyle);
			}
		}
	}

	private static void DrawLogButtons(float x, float y, float width)
	{
		if (MenuTheme.Button(new Rect(x, y, width * 0.48f, 30f), UiLocalization.Get("logs.open"), button))
		{
			Diagnostics.OpenFolder();
		}
		if (MenuTheme.Button(new Rect(x + width * 0.5f, y, width * 0.48f, 30f), UiLocalization.Get("logs.copy"), button))
		{
			Diagnostics.CopyFolder();
		}
	}

	private static void DrawRevival(CoopSession s, float scale)
	{
		Camera main = Camera.main;
		if (!main)
		{
			return;
		}
		foreach (PlayerSlot player in s.Players)
		{
			if (!player.Reviving || !player.Hero)
			{
				continue;
			}
			Vector3 vector = main.WorldToScreenPoint(player.Hero.transform.position + Vector3.up * 2f);
			if (!(vector.z <= 0f))
			{
				float num = vector.x / scale;
				float num2 = ((float)Screen.height - vector.y) / scale;
				Box(new Rect(num - 49f, num2 - 2f, 98f, 9f), new Color(0f, 0f, 0f, 0.7f));
				Bar(new Rect(num - 46f, num2, 92f, 5f), player.ReviveProgress, Color.Lerp(player.Color, Color.white, 0.4f));
				GUI.Label(new Rect(num - 60f, num2 - 23f, 120f, 20f), UiLocalization.Get("hud.reviving"), new GUIStyle(small)
				{
					alignment = TextAnchor.MiddleCenter,
					normal = 
					{
						textColor = player.Color
					}
				});
				HudAssets.Images images = HudAssets.For(player);
				Texture2D texture2D = ((images != null) ? images.Soul : HudAssets.Soul);
				if ((bool)texture2D)
				{
					float num3 = 16f + Mathf.Sin(Time.unscaledTime * 7f) * 2f;
					Icon(texture2D, new Rect(num - num3 / 2f, num2 - 45f, num3, num3), new Color(player.Color.r, player.Color.g, player.Color.b, 0.5f + 0.5f * player.ReviveProgress));
				}
			}
		}
	}

	private static void NativeCharmCards(CoopSession s, float w, float h, float scale)
	{
		if (!Charms.NativeMenuOpen)
		{
			RoleUI.DrawNative(s, w, h, scale);
			return;
		}
		if (s.Primary != null && PlayerContext.Current == null)
		{
			s.Primary.Charms.Read(s.Data);
		}
		int count = s.Players.Count;
		Rect r = CharmMenuLayout.Cards(w, h, count);
		CharmMenuBackdrop.Draw(r, h, new Color(0.035f, 0.027f, 0.03f, 1f));
		for (int i = 0; i < count; i++)
		{
			DrawCharmCard(s.Players[i], s.Data, CharmMenuLayout.Card(r, i, count));
		}
		DrawNativeCharmCursors(s, scale);
		RoleUI.DrawNative(s, w, h, scale);
	}

	private static void DrawNativeCharmCursors(CoopSession s, float scale)
	{
		GameCameras instance = GameCameras.instance;
		if (!instance || !instance.hudCamera)
		{
			return;
		}
		foreach (PlayerSlot player in s.Players)
		{
			if (!player.Alive || !player.Ready || RoleSystem.State(player).Equipped.Active)
			{
				continue;
			}
			int num = Charms.NativeCursor[player.Index];
			InvCharmBackboard invCharmBackboard = null;
			InvCharmBackboard[] nativeBoards = Charms.NativeBoards;
			foreach (InvCharmBackboard invCharmBackboard2 in nativeBoards)
			{
				if ((bool)invCharmBackboard2 && invCharmBackboard2.charmNum == num)
				{
					invCharmBackboard = invCharmBackboard2;
					break;
				}
			}
			if (!invCharmBackboard)
			{
				continue;
			}
			Vector3 vector = instance.hudCamera.WorldToScreenPoint(invCharmBackboard.transform.position);
			if (vector.z <= 0f)
			{
				continue;
			}
			float num2 = vector.x / scale;
			float num3 = ((float)Screen.height - vector.y) / scale;
			if (num2 < 20f || num2 > (float)Screen.width / scale - 20f || num3 < 20f || num3 > (float)Screen.height / scale - 20f)
			{
				continue;
			}
			int num4 = 0;
			foreach (PlayerSlot player2 in s.Players)
			{
				if (player2.Index < player.Index && !RoleSystem.State(player2).Equipped.Active && Charms.NativeCursor[player2.Index] == num)
				{
					num4++;
				}
			}
			float num5 = (float)(58 + num4 * 9) * 0.5f;
			Color color = player.Color;
			CharmCursorUI.Draw(new Rect(num2 - num5, num3 - num5, num5 * 2f, num5 * 2f), color);
			GUI.Label(new Rect(num2 - num5, num3 - num5 - 23f, 38f, 21f), "P" + (player.Index + 1), new GUIStyle(tag)
			{
				fontSize = 14,
				normal = 
				{
					textColor = color
				}
			});
		}
	}

	private static void DrawCharmCard(PlayerSlot p, PlayerData data, Rect r)
	{
		CharmCardUI.Draw(p, data, r);
		RoleUI.CardOverlay(p, data, r);
	}

	private static void CharmPanel(Local8Runtime plugin, float w, float h)
	{
		CoopSession session = plugin.Session;
		PlayerSlot playerSlot = session.Players.FirstOrDefault((PlayerSlot v) => v.Index == Charms.Selected);
		if (playerSlot == null)
		{
			Charms.Close();
			return;
		}
		float num = Mathf.Min(840f, w - 24f);
		float num2 = Mathf.Min(790f, h - 24f);
		float num3 = (w - num) / 2f;
		float num4 = (h - num2) / 2f;
		MenuTheme.Frame(new Rect(num3,num4,num,num2));
		MenuTheme.Centered(new Rect(num3+76,num4+18,num-152,32),UiLocalization.Get("charms.title")+(playerSlot.Index+1),playerSlot.Color,22);
		if (MenuTheme.Button(new Rect(num3 + num - 105f, num4 + 15f, 85f, 30f), UiLocalization.Get("common.back"), button))
		{
			Charms.Close();
		}
		float num5 = num3 + 20f;
		foreach (PlayerSlot player in session.Players)
		{
			if(MenuTheme.Card(new Rect(num5,num4+55f,55f,28f),new GUIContent("P"+(player.Index+1)),player.Index==Charms.Selected,player.Color))Charms.Select(player);
			num5 += 60f;
		}
		playerSlot=session.Players.First(v=>v.Index==Charms.Selected);
		MenuTheme.Text(new Rect(num3+20,num4+90,num-40,21),UiLocalization.Get("charms.notches_prefix")+playerSlot.Charms.Used+" / "+session.Data.charmSlots+(playerSlot.Charms.Overcharmed?UiLocalization.Get("charms.overcharmed_suffix"):""),MenuTheme.Muted,13);
		MenuTheme.Text(new Rect(num3+20,num4+113,num-40,27),Charms.CanEdit(playerSlot)?UiLocalization.Get("charms.editable"):UiLocalization.Get("charms.bench"),Charms.CanEdit(playerSlot)?MenuTheme.Muted:new Color(1,.76f,.64f),13);
		int[] charmIds = CharmExtensions.Ids();
		int charmRows = (charmIds.Length + 9) / 10;
		float num6 = Mathf.Min(72f, (num - 40f) / 10f);
		if (charmIds.Length > 40) num6 = Mathf.Min(num6, Mathf.Max(10f, (num2 - 300f) / charmRows));
		float num7 = num3 + (num - num6 * 10f) / 2f;
		float num8 = num4 + 142f;
		for (int charmIndex = 0; charmIndex < charmIds.Length; charmIndex++)
		{
			int i = charmIds[charmIndex];
			Rect rect = new Rect(num7 + (float)(charmIndex % 10) * num6, num8 + (float)(charmIndex / 10) * num6, num6 - 5f, num6 - 5f);
			bool @bool = session.Data.GetBool("gotCharm_" + i);
			bool flag = playerSlot.Charms.IsEquipped(i);
			Box(rect, (i == Charms.Cursor) ? new Color(0.38f, 0.42f, 0.55f, 0.9f) : (flag ? new Color(playerSlot.Color.r, playerSlot.Color.g, playerSlot.Color.b, 0.32f) : new Color(0.13f, 0.14f, 0.18f, 0.7f)));
			if(i==Charms.Cursor||flag)MenuArt.Nine(rect,MenuArt.Selection,i==Charms.Cursor?MenuTheme.Paper:playerSlot.Color);
			if (@bool)
			{
				Texture2D texture2D = Charms.Icon(i);
				if ((bool)texture2D)
				{
					Icon(texture2D, new Rect(rect.x + 5f, rect.y + 3f, rect.width - 10f, rect.height - 13f), flag ? Color.white : new Color(0.7f, 0.7f, 0.7f));
				}
				else
				{
					GUI.Label(rect, i.ToString(), tag);
				}
			}
			else
			{
				GUI.Label(rect, "?", tag);
			}
			if (MenuTheme.Button(rect, "", GUIStyle.none))
			{
				Charms.Cursor = i;
				if (@bool)
				{
					Charms.Toggle(playerSlot, i);
				}
			}
			if (Event.current.type == EventType.MouseMove && rect.Contains(Event.current.mousePosition))
			{
				Charms.Cursor = i;
			}
			if (flag)
			{
				GUI.Label(new Rect(rect.x + 3f, rect.yMax - 15f, rect.width - 6f, 16f), UiLocalization.Get("charms.equipped"), new GUIStyle(small)
				{
					fontSize = 9,
					alignment = TextAnchor.MiddleCenter
				});
			}
		}
		int cursor = Charms.Cursor;
		float num9 = num8 + charmRows * num6 + 12f;
		bool bool2 = session.Data.GetBool("gotCharm_" + cursor);
		GUI.Label(new Rect(num3 + 20f, num9, num - 40f, 30f), bool2 ? (Charms.Name(cursor) + " - " + session.Data.GetInt("charmCost_" + cursor) + UiLocalization.Get("charms.notches_suffix")) : UiLocalization.Get("charms.undiscovered"), text);
		if (bool2)
		{
			GUI.Label(new Rect(num3 + 20f, num9 + 34f, num - 40f, Mathf.Max(40f, num2 - (num9 - num4) - 105f)), Charms.Description(cursor), new GUIStyle(small)
			{
				wordWrap = true,
				richText = true
			});
		}
		GUI.Label(new Rect(num3 + 20f, num4 + num2 - 60f, num - 40f, 45f), (Time.unscaledTime < plugin.MessageUntil) ? plugin.Message : UiLocalization.Get("charms.controls"), new GUIStyle(small)
		{
			wordWrap = true
		});
	}
}
