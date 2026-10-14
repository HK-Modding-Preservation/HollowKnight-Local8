using System;
using System.Linq;
using System.Text.Json;
using UnityEngine;
using UnityEngine.Audio;
using KO.HollowKnight8;
using Action=HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot;
static class Test {
 static int checks;static void Check(bool yes,string why){checks++;if(!yes)throw new Exception(why);}
 static PlayerSlot P(int i)=>new PlayerSlot{Index=i};
 static CoopSession Session(){var r=new Local8Runtime();Plugin.Self=r;for(int i=0;i<8;i++){var p=P(i);p.Actions.Load(i==0?"W,A,S,D":"Keypad"+i);r.Session.Players.Add(p);}return r.Session;}
 static void Profiles(){
  var s=Session();var p=s.Players[1];p.Adventure=new PlayerAdventureStatistics{EnemiesKilled=4,DamageDealt=80};p.Charms.Initialize(s.Data,new[]{2,1,3});Local8Mod.Save.RoleIds[1]="mage";p.SkinId="custom";Plugin.Self.Panel=true;
  Check(PlayerProfiles.Save(p,"Raul"),"save failed");var saved=PlayerProfiles.Find(p.ProfileId);Check(saved.Name=="Raul"&&saved.RoleId=="mage"&&saved.Charms.SequenceEqual(new[]{2,1}),"saved wrong gear/role");
  Check(!PlayerProfiles.Load(s.Players[2],saved),"same profile in two live slots");
  saved.Charms=saved.Charms.Concat(new[]{41}).ToArray();
  PlayerProfiles.Detach(p);Check(p.ProfileId==null&&p.Adventure.EnemiesKilled==0&&saved.Adventure.EnemiesKilled==4,"guest reset erased profile");
  p.Actions.Load("I,J,K,L");var q=s.Players[2];q.InputBlocked=true;q.Vitals.Health=2;q.Vitals.Blue=0;q.Vitals.Soul=30;int geo=s.Data.geo;
  Check(PlayerProfiles.Load(q,saved),"load P2 profile in P3 failed");Check(q.ProfileId==saved.Id&&q.Adventure.EnemiesKilled==4&&Local8Mod.Save.RoleIds[2]=="mage"&&q.SkinId=="custom","identity lost on slot change");
  Check(q.Vitals.Health==2&&q.Vitals.Blue==0&&q.Vitals.Soul==30&&s.Data.geo==geo,"profile grants free health/soul/story money");
  Check(q.Actions.Save()=="Keypad1"&&KeypadInput.Repairs==1,"portable independent keyboard profile not applied");
  Check(saved.Charms.Contains(41)&&!q.Charms.IsEquipped(41),"unavailable custom charm preference erased or equipped illegally");
  Check(!ReferenceEquals(q.Adventure,saved.Adventure)&&!ReferenceEquals(q.Adventure,Local8Mod.Save.AdventureStats[2]),"shared mutable stats");
  q.Adventure.EnemiesKilled++;PlayerProfiles.Dirty();PlayerProfiles.Flush(s);
  var roundtrip=JsonSerializer.Deserialize<Local8Settings>(Modding.Mod.Json,new JsonSerializerOptions{IncludeFields=true});Check(roundtrip.PlayerProfiles[0].Adventure.EnemiesKilled==5&&roundtrip.PlayerProfiles[0].Id==saved.Id,"persistent roundtrip lost identity/stats");
  Check(!PlayerProfiles.Save(s.Players[3],"rAuL"),"ambiguous duplicate names");
  int[] old=saved.Charms.ToArray();PvpArena.Active=true;q.Charms.Initialize(s.Data,new[]{3});PlayerProfiles.Capture(s);Check(saved.Charms.SequenceEqual(old),"arena overrides contaminated saved equipment");PvpArena.Active=false;
  var z=s.Players[4];z.ProfileLoadedThisJoin=true;Time.unscaledTime=90;Check(!PlayerProfiles.CanLoad(z,s),"unrested late gear load allowed");Charms.Bench=true;Check(PlayerProfiles.CanLoad(z,s),"bench gear load denied");GameManager.instance.IsLoadingSceneTransition=true;Check(!PlayerProfiles.CanLoad(z,s),"load allowed during transition");GameManager.instance.IsLoadingSceneTransition=false;
  Check(!ProfileKeyboard.CanApply(z,"W",s),"stolen keyboard controls allowed");Check(!ProfileKeyboard.CanApply(z,"invalid",s),"invalid saved keyboard accepted");Check(ProfileKeyboard.CanApply(z,"Keypad9",s),"valid number pad profile denied");Check(HeroActions.Created-HeroActions.Destroyed==8,"validation leaks candidate actions");
  PlayerProfiles.Detach(q);q.Hero.ThrowCharm=true;var previous=q.Vitals.Copy();var loadout=q.Charms;Check(!PlayerProfiles.Load(q,saved),"provider error not handled");Check(q.ProfileId==null&&q.Vitals.Health==previous.Health&&ReferenceEquals(q.Charms,loadout)&&PlayerContext.Current==null&&!PlayerProfiles.Applying,"profile transaction did not roll back");
  Local8Mod.Settings.PlayerProfiles=new[]{saved,new PlayerProfileData{Id=saved.Id,Name="duplicate"},new PlayerProfileData{Id="bad",Name=" \n< ",RoleId="unknown",Charms=new[]{-1,2,2},Adventure=new PlayerAdventureStatistics{Deaths=-9,SecondsPlayed=double.NaN}},null};PlayerProfiles.Normalize();Check(PlayerProfiles.All.Length==1,"corrupt identity normalization");
  saved.Adventure.Deaths=-4;saved.Adventure.SecondsPlayed=double.PositiveInfinity;PlayerProfiles.Normalize();Check(saved.Adventure.Deaths==0&&saved.Adventure.SecondsPlayed==0,"corrupt stats not repaired");
  Check(AdventureStatisticsRules.Name(" <b>\nName\t ")=="bName","profile text injection sanitizer");
  Modding.Mod.Fail=true;PlayerProfiles.Dirty();Check(!PlayerProfiles.Flush(s),"failed persistence silently succeeded");Modding.Mod.Fail=false;Check(PlayerProfiles.Flush(s),"retry lost dirty data");
  // Simulate the next day: new roster, settings restored from persisted fields,
  // the same identity loaded into another numbered slot, with no P2 present.
  Local8Mod.Settings=JsonSerializer.Deserialize<Local8Settings>(Modding.Mod.Json,new JsonSerializerOptions{IncludeFields=true});
  s=Session();Local8Mod.Save=new LocalSave();Charms.Bench=true;PlayerProfiles.Normalize();saved=PlayerProfiles.All.Single();
  Check(PlayerProfiles.Load(s.Players[2],saved)&&s.Players[2].Adventure.EnemiesKilled==5&&Local8Mod.Save.RoleIds[2]=="mage","next-session profile did not survive in P3");
  MenuTheme.Paints.Clear();float row=0;AdventureStatsUI.Draw(s,690,ref row);
  var header=MenuTheme.Paints.Single(x=>x.Text=="P3  ·  Raul");
  Check(MenuTheme.Paints.Any(x=>x.Text=="5"&&x.Rect.y==header.Rect.y+68&&x.Rect.x==header.Rect.x),"Adventure panel does not show the saved profile's total in its new slot");
  var first=MenuTheme.Paints.Single(x=>x.Text=="P1");
  Check(MenuTheme.Paints.Any(x=>x.Text=="0"&&x.Rect.y==first.Rect.y+68&&x.Rect.x==first.Rect.x),"Loaded profile's panel statistics leaked to P1");
  Check(MenuTheme.Paints.Any(x=>x.Text=="stats.tip")&&row>0,"Adventure panel lost its caption or measured height");
  Plugin.Self.Panel=false;Time.unscaledDeltaTime=.1f;double played=s.Players[2].Adventure.SecondsPlayed;PlayerProfiles.Tick(Plugin.Self);Check(s.Players[2].Adventure.SecondsPlayed>played,"adventure time not recorded");
  Plugin.Self.Panel=true;played=s.Players[2].Adventure.SecondsPlayed;PlayerProfiles.Tick(Plugin.Self);Check(s.Players[2].Adventure.SecondsPlayed==played,"F8 menu counted as play time");
 }
 static HitInstance Hit(PlayerSlot p,int amount)=>new HitInstance{Source=p.Hero,DamageDealt=amount};
 static void Statistics(){
  var s=Session();AdventureStats.Install();AdventureStats.Install();var a=s.Players[0];var b=s.Players[1];var e=new HealthManager{hp=10};Time.unscaledTime=100;
  On.HealthManager.Strike(e,Hit(a,30),(e,h)=>{});Check(PlayerProfiles.Stats(a).DamageDealt==0,"blocked hit counted");
  On.HealthManager.Strike(e,Hit(a,3));On.HealthManager.Strike(e,Hit(b,40));Check(a.Adventure.DamageDealt==3&&b.Adventure.DamageDealt==7,"overkill or wrong owner damage");Check(a.Adventure.Assists==1&&b.Adventure.EnemiesKilled==1,"kill/assist attribution");
  On.HealthManager.Kill(e);Check(b.Adventure.EnemiesKilled==1,"duplicate death counted");e.hp=6;e.Dead=false;On.HealthManager.Strike(e,Hit(a,6));Check(a.Adventure.EnemiesKilled==1,"pooled enemy not reset");
  e=new HealthManager{hp=12};On.HealthManager.Strike(e,Hit(a,0),(enemy,h)=>{enemy.hp-=3;using(PlayerContext.Enter(b))On.HealthManager.Extra(enemy,4);});Check(a.Adventure.DamageDealt==12&&b.Adventure.DamageDealt==11,"nested damage counted twice or attributed to wrong actor");
  e=new HealthManager{hp=9};On.HealthManager.Strike(e,Hit(a,2));Time.unscaledTime+=31;On.HealthManager.Strike(e,Hit(b,7));Check(a.Adventure.Assists==1,"stale contribution counted as assist");
  e=new HealthManager{hp=2,Shade=true};On.HealthManager.Strike(e,Hit(a,2));Check(a.Adventure.EnemiesKilled==1,"own shade counted as adventure enemy");
  for(int i=0;i<8;i++){var enemy=new HealthManager{hp=2};long before=PlayerProfiles.Stats(s.Players[i]).DamageDealt;On.HealthManager.Strike(enemy,Hit(s.Players[i],3));Check(s.Players[i].Adventure.DamageDealt==before+2,"eight player source isolation");}
  b.Vitals.Health=2;On.HeroController.Heal(b.Hero,2);Check(b.Adventure.HealthRecovered==2&&b.Vitals.Health==4,"nested Hero/PlayerData healing doubled or attributed to P1");On.HeroController.Heal(b.Hero,999);Check(b.Adventure.HealthRecovered==3,"overheal counted");
  AdventureStats.Given(a,2);AdventureStats.Down(b);AdventureStats.Revive(a);Check(a.Adventure.HealingGiven==2&&a.Adventure.Revives==1&&b.Adventure.Deaths==1,"adventure healing/revive/death missing");
  long damage=a.Adventure.DamageDealt;PvpMatch.Running=true;On.HealthManager.Strike(new HealthManager{hp=3},Hit(a,3));AdventureStats.Down(a);Check(a.Adventure.DamageDealt==damage&&a.Adventure.Deaths==0,"PvP contaminated adventure");PvpMatch.Running=false;
  AdventureStats.ClearRoom();AdventureStats.Uninstall();
  for(int before=0;before<50;before++)for(int after=-100;after<70;after++)Check(AdventureStatisticsRules.Damage(before,after)==Math.Min(before,Math.Max(0,before-after)),"damage boundary");
  Check(AdventureStatisticsRules.Add(long.MaxValue,1)==long.MaxValue&&AdventureStatisticsRules.Add(-1,5)==5,"counter overflow");
 }
 static void Audio(){
  var s=Session();CombatEffects.Enabled=true;DamageAudioGuard.Install();DamageAudioGuard.Install();var hero=s.Players[1].Hero;hero.damageEffectFSM=new PlayMakerFSM();var fsm=hero.damageEffectFSM.Fsm;
  var hurt=new AudioMixerSnapshot{name="Hurt Lowpass"};var normal=new AudioMixerSnapshot{name="Normal"};GameManager.instance.actorSnapshotUnpaused=normal;
  Action Mix(AudioMixerSnapshot snapshot,HutongGames.PlayMaker.Fsm owner){var a=new Action{snapshot=new HutongGames.PlayMaker.FsmObject{Value=snapshot},Fsm=owner};On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Enter(a);return a;}
  int before=On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Calls;Check(Mix(hurt,fsm).Finished&&On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Calls==before,"damage mix was not filtered/FSM not finished");
  Mix(normal,fsm);Check(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Calls==++before,"normal recovery mix blocked");
  Mix(hurt,new HutongGames.PlayMaker.Fsm());Check(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Calls==++before,"unowned mixer blocked");
  using(DamageAudioGuard.Damage()){On.AudioManager.Mix(hurt);On.AudioManager.Mix(normal);}Check(On.AudioManager.Calls==1,"scoped hurt mix blocked normal music");
  foreach(int kind in Enumerable.Range(0,5)){GameManager.instance.isPaused=kind==0;GameManager.instance.IsLoadingSceneTransition=kind==1;CoopEnding.Active=kind==2;ScriptedParty.Active=kind==3;CombatEffects.Enabled=kind!=4;Mix(hurt,fsm);Check(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Calls==++before,"pause/loading/cutscene/disabled mix blocked");}
  GameManager.instance.isPaused=false;GameManager.instance.IsLoadingSceneTransition=false;CoopEnding.Active=ScriptedParty.Active=false;CombatEffects.Enabled=true;s.TeamWipe=true;Mix(hurt,fsm);Check(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Calls==++before,"real team death mix blocked");DamageAudioGuard.Uninstall();
 }
 static void Main(){Profiles();Statistics();Audio();Console.WriteLine("PASS "+checks+" production profile persistence, portable controls, HP-based statistics, ownership and damage-audio checks. Native Unity player visuals/audio are not simulated.");}
}
