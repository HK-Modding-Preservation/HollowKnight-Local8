using System;using System.Linq;using UnityEngine;using UnityEngine.Audio;using KO.HollowKnight8;using HutongGames.PlayMaker.Actions;
internal static class Test {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 sealed class Fixture {
  internal CoopSession Session=new();internal AudioMixer Music=new(){name="Music"},Sfx=new(){name="SFX"};internal AudioManager Audio;internal MusicCue Cue;internal HealthManager Boss;internal AudioMixerSnapshot SilentMusic,NormalSfx;
  internal Fixture(int count=2){BossMusicGuard.Uninstall();UnityEngine.Object.All.Clear();ArenaGather.engagedBosses.Clear();ArenaGather.encounterTarget=null;PartySoul.IndividualDeathAudio=PvpArena.KeepsMusic=BossSceneController.IsBossScene=false;Diagnostics.Messages.Clear();Time.unscaledTime+=100;Application.isFocused=true;UnityEngine.SceneManagement.SceneManager.Room="Fungus1_04";
   Cue=new(){name="BossHornet",Snapshot=new(){name="Hornet music",audioMixer=Music}};var sources=new AudioSource[6];for(int i=0;i<6;i++){var go=new GameObject("Track "+i);sources[i]=go.Add<AudioSource>();sources[i].clip=new(){name="Hornet "+i};Cue.Channels[i]=new(){Clip=sources[i].clip};}
   Audio=new(sources){CurrentMusicCue=Cue};SilentMusic=new(){name="Silent",audioMixer=Music};NormalSfx=new(){name="Normal",audioMixer=Sfx};var noMusic=new MusicCue{name="No Music",OriginalMusicEventName="",Snapshot=new(){name="Music Off",audioMixer=Music}};
   GameManager.instance=new(){AudioManager=Audio,noMusicCue=noMusic,noMusicSnapshot=noMusic.Snapshot,playerData=Session.Data};Plugin.Self=new(){Session=Session};
   for(int i=0;i<count;i++)Session.Players.Add(new(){Index=i,Hero=new GameObject("Knight "+i).Add<HeroController>()});Boss=new GameObject("Hornet Boss 1").Add<HealthManager>();ArenaGather.engagedBosses.Add(Boss);BossMusicGuard.Install();BossMusicGuard.Install();BossMusicGuard.Tick();
  }
  internal void Tick(float dt=.05f){Time.unscaledTime+=dt;BossMusicGuard.Tick();}
  internal TransitionToAudioSnapshot Action(AudioMixerSnapshot s)=>new(){snapshot=new(){Value=s},transitionTime=new(){Value=1}};
  internal void Down(int index){Session.Players[index].Alive=false;}
  internal void NativeDeath(){PartySoul.IndividualDeathAudio=true;try{var mute=Action(SilentMusic);mute.OnEnter();var normal=Action(NormalSfx);normal.OnEnter();Check(mute.Finishes==1&&normal.Finishes==1,"native death action did not finish");}finally{PartySoul.IndividualDeathAudio=false;}}
 }
 static void Main(){
  foreach(int count in new[]{2,4,8})for(int actor=0;actor<count;actor++){
   var f=new Fixture(count);Check(On.AudioManager.Count==1&&On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Count==1,"unbalanced audio hook installation");f.Down(actor);f.NativeDeath();Check(f.SilentMusic.Transitions==0&&f.NormalSfx.Transitions==1,"individual death silenced music or suppressed native death SFX");
   // The native legacy Track 1 fade lowers volume over multiple frames. Keep
   // the healthy pre-death volume instead of recording its gradual decrease.
   f.Audio.Sources[0].volume=.7f;f.Tick();Check(f.Audio.Sources[0].volume==1,"partial native death fade was retained");f.Audio.Sources[0].volume=.15f;f.Tick();Check(f.Audio.Sources[0].volume==1,"healthy pre-death volume decayed with the fade");
   f.Audio.Sources[1].Stop();f.Audio.Sources[1].clip=null;f.Tick();Check(f.Audio.Sources[1].isPlaying&&f.Audio.Sources[1].clip==f.Cue.Channels[1].Clip&&f.Audio.Sources[1].timeSamples==1000,"death stop was not resumed at captured sample");
   Check(f.Music.Current==f.Cue.Snapshot,"native mixer-only mute did not restore active cue snapshot");
  }
  {var f=new Fixture(4);f.Down(1);f.SilentMusic.TransitionTo(1);Check(f.Audio.Sources.All(s=>s.isPlaying),"mixer-only native death fixture stopped sources");f.Tick();Check(f.Music.Current==f.Cue.Snapshot,"playing sources hid a muted music mixer");}
  {var f=new Fixture(4);PartySoul.IndividualDeathAudio=true;f.Audio.ApplyMusicSnapshot(f.SilentMusic,2,1);f.Audio.ApplyMusicSnapshot(f.NormalSfx,0,1);PartySoul.IndividualDeathAudio=false;Check(f.SilentMusic.Transitions==0&&f.NormalSfx.Transitions==1,"delayed manager snapshot escaped death ownership");}
  foreach(string guard in new[]{"no-coop","team-wipe","native-death","boss-dead","loading","other-room","normal-story"}){
   var f=new Fixture(2);f.Down(1);PartySoul.IndividualDeathAudio=true;
   switch(guard){case "no-coop":f.Session.Active=false;break;case "team-wipe":f.Session.TeamWipe=true;break;case "native-death":f.Session.AllowVanillaDeath=true;break;case "boss-dead":f.Boss.hp=0;PartySoul.IndividualDeathAudio=false;break;case "loading":GameManager.instance.IsLoadingSceneTransition=true;break;case "other-room":GameManager.instance.Gameplay=false;break;case "normal-story":PartySoul.IndividualDeathAudio=false;break;}
   f.Action(f.SilentMusic).OnEnter();Check(f.SilentMusic.Transitions==1,"legitimate native silence blocked: "+guard);PartySoul.IndividualDeathAudio=false;
  }
  {var f=new Fixture(2);f.Down(1);f.Tick();var phase=new AudioClip{name="new phase"};f.Audio.Sources[0].clip=phase;f.Audio.Sources[0].Stop();f.Tick();Check(!f.Audio.Sources[0].isPlaying&&f.Audio.Sources[0].clip==phase,"death recovery replaced a different phase clip");}
  {var f=new Fixture(2);f.Down(1);f.Tick();f.Tick(6);f.Audio.Sources[0].volume=.2f;f.Tick();Check(f.Audio.Sources[0].volume==.2f,"death repair continued beyond its bounded window");}
  {var f=new Fixture(2);f.Down(1);f.Tick();var next=new MusicCue{name="Next phase",Snapshot=new(){audioMixer=f.Music}};for(int i=0;i<6;i++)next.Channels[i]=new(){Clip=new(){name="next "+i}};BossMusicGuard.Cue((audio,cue,d,t,s)=>audio.NativeCue(cue),f.Audio,next,0,0,true);f.Tick();f.Audio.Sources[0].volume=.4f;f.Tick();Check(f.Audio.CurrentMusicCue==next&&f.Audio.Sources[0].clip==next.Channels[0].Clip&&f.Audio.Sources[0].volume==.4f,"native phase cue was overridden by old death repair");}
  {var f=new Fixture(2);var silent=GameManager.instance.noMusicCue;BossMusicGuard.Cue((audio,cue,d,t,s)=>audio.NativeCue(cue),f.Audio,silent,0,0,true);Check(f.Audio.CurrentMusicCue==f.Cue,"known live-boss no-music guard lost");f.Boss.hp=0;BossMusicGuard.Cue((audio,cue,d,t,s)=>audio.NativeCue(cue),f.Audio,silent,0,0,true);Check(f.Audio.CurrentMusicCue==silent,"defeated boss cannot finish native cue");}
  {var f=new Fixture(2);f.Down(1);f.Tick();GameManager.instance.IsLoadingSceneTransition=true;f.Tick();GameManager.instance.IsLoadingSceneTransition=false;UnityEngine.SceneManagement.SceneManager.Room="Town";f.Audio.Sources[0].volume=.2f;f.Tick();Check(f.Audio.Sources[0].volume==.2f,"scene return revived prior room's music volume");}
  {var f=new Fixture(2);f.Down(1);GameManager.instance.isPaused=true;f.Audio.Sources[0].volume=.5f;f.Tick();Check(f.Audio.Sources[0].volume==.5f,"pause triggered audio repair");GameManager.instance.isPaused=false;f.Tick();Check(f.Audio.Sources[0].volume==1,"death first observed after pause lost recovery");}
  {var f=new Fixture(2);ArenaGather.engagedBosses.Clear();f.Boss.hp=0;PvpArena.KeepsMusic=true;foreach(var p in f.Session.Players)p.Alive=false;f.Audio.Sources[0].Stop();f.Tick();Check(f.Audio.Sources[0].isPlaying,"PvP round continuity regressed");PvpArena.KeepsMusic=false;}
  BossMusicGuard.Uninstall();Check(On.AudioManager.Count==0&&On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Count==0,"audio teardown leaked hooks");
  Console.WriteLine("PASS "+checks+" production boss-music checks with exact native snapshot-action IL: 2/4/8 parties, every downed slot, distinct music/SFX mixers, gradual/stopped/mixer-only death mutes, native phases/endings/loading, bounded recovery and balanced hooks. Unity mixer/source IO remains controlled.");
 }
}
