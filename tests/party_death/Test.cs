using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KO.HollowKnight8;
using UnityEngine;
using HutongGames.PlayMaker;
class Program {
 static int checks,heroFactories,heroDeaths,before,after,saves,nativeBodies,managerFactories,finallyCalls;
 static bool resetAtEnd;
 static string world;
 static IEnumerator scheduledManager;
 static CoopSession s;
 static void Check(bool x,string label){checks++;if(!x)throw new Exception(label);}
 static CoopSession Party(int n){PartyDeath.Reset();PvpMatch.Running=false;CoopEnding.Active=false;DreamSequence.InStoryDream=false;PlayerContext.Current=null;Diagnostics.Messages.Clear();heroFactories=heroDeaths=before=after=saves=nativeBodies=managerFactories=finallyCalls=0;scheduledManager=null;world="normal";resetAtEnd=false;s=new CoopSession();for(int i=0;i<n;i++){var g=new GameObject("P"+(i+1));var h=g.Add<HeroController>();h.heroDeathPrefab=new GameObject("Death P"+(i+1));s.Players.Add(new PlayerSlot{Index=i,Hero=h});}Plugin.Self=new Plugin{Session=s};return s;}
 static void Down(PlayerSlot p){p.Down=true;p.Vitals.Health=p.Vitals.Blue=0;}
 static Fsm Enemy(string name="Mage")=>new(){Name=name,GameObject=new GameObject(name)};
 static Fsm Body(PlayerSlot p)=>new(){Name="Hero",GameObject=p.Hero.gameObject};
 static Fsm DeathBody(PlayerSlot p){var child=new GameObject("Death child");child.transform.parent=p.Hero.heroDeathPrefab.transform;return new(){Name="Death Control",GameObject=child};}
 static bool Block(Fsm f,string evt="HERO DEATH",PlayerSlot source=null)=>PartyDeath.BlockEvent(f,new(evt),source==null?null:new(){SentByGameObject=source.Hero.gameObject});
 static void Confirm(){Check(PartyDeath.TryBegin(s),"All participant knockouts create proof");s.TeamWipe=true;foreach(var p in s.Players.Skip(1))p.Retiring=true;s.Primary.Down=false;}
 static IEnumerator NativeHeroFactory(HeroController h){heroFactories++;return NativeHero(h);}
 static IEnumerator NativeHero(HeroController h){Check(PlayerContext.Current==s.Primary,"Native Hero.Die uses primary context");heroDeaths++;if(world=="dream"||world=="godhome")yield break;yield return null;scheduledManager=DeathHooks.Manager(ManagerFactory,new(),.8f);}
 static IEnumerator ManagerFactory(GameManager gm,float wait){managerFactories++;return ApiManager();}
 // This matches the actual patched Modding API: before, yield native iterator,
 // after. Dispose has no After hook and the wrapper has no finally callback.
 static IEnumerator ApiManager(){before++;Check(PlayerContext.Current==s.Primary,"BeforePlayerDead remains primary during native death");yield return NativeManager();after++;}
 static IEnumerator NativeManager(){saves++;for(int i=0;i<3;i++){Check(PlayerContext.Current==s.Primary,"Native manager step keeps primary context");yield return new Wait();}nativeBodies++;if(resetAtEnd){PartyDeath.Reset();s.TeamWipe=false;}}
 sealed class Wait{}
 static void Drain(IEnumerator r,int limit=100){if(r==null)return;int moves=0;while(r.MoveNext()){Check(++moves<limit,"No iterator loop");var c=r.Current;Check(ReferenceEquals(c,r.Current),"Current yields the same child instance");if(c is IEnumerator nested)Drain(nested,limit);} (r as IDisposable)?.Dispose();(r as IDisposable)?.Dispose();Check(!r.MoveNext(),"Finished iterator stays finished");}
 static IEnumerator CountBody(){try{yield return null;nativeBodies++;}finally{finallyCalls++;}}
 static IEnumerator TailWrapper(){yield return ApiManager();nativeBodies++;}
 static IEnumerator ThrowBody(){try{yield return null;throw new InvalidOperationException("native error");}finally{finallyCalls++;}}
 static IEnumerable<int[]> Orders(int n){var basic=Enumerable.Range(0,n).ToArray();yield return basic;yield return basic.Reverse().ToArray();for(int i=0;i<n;i++)yield return basic.Skip(i).Concat(basic.Take(i)).ToArray();}
 static void Main(){
  // Every distinct living/dead pair, including primary being down first. An
  // enemy notification cannot replace an actual all-participants snapshot.
  for(int n=2;n<=8;n++)for(int dead=0;dead<n;dead++)for(int living=0;living<n;living++){
   if(dead==living)continue;Party(n);var p=s.Players[dead];Down(p);
   Check(!PartyDeath.TryBegin(s),"One death is not a group death");
   foreach(var evt in new[]{"HERO DEATH","HERO DEAD","PLAYER DEATH","PLAYER DEAD"}){
    Check(Block(Enemy(),evt,p),"Individual notification is hidden from world enemies");
    Check(Block(Body(s.Players[living]),evt,p),"Living hero cannot consume someone else's death");
    Check(!Block(Body(p),evt,p),"Downed hero keeps its own local death cleanup");
    Check(!Block(DeathBody(p),evt,p),"Individual death prefab can complete local cleanup");
    Check(Block(Enemy(),evt),"Missing current context cannot leak global death");
   }
   PlayerContext.Current=s.Players[living];
   Check(!Block(Body(p),"HERO DEATH",p),"Actual source wins over unrelated current context");
   Drain(DeathHooks.Manager(ManagerFactory,new(),0));Check(before==0&&after==0&&saves==0,"Individual manager death cannot reach API or save");
   PlayerContext.Current=null;
  }
  // Every roster size and player being first/last, simultaneous orders, normal
  // and dream deaths. Wipe clears P1.Down only after preserving proof.
  for(int n=1;n<=8;n++)foreach(var order in Orders(n))foreach(var zone in new[]{"normal","dream","godhome"}){
   Party(n);world=zone;
   for(int step=0;step<n;step++){
    int i=order[step];Drain(DeathHooks.Hero(NativeHeroFactory,s.Players[i].Hero));
    Check(heroDeaths==0,"Local knockout does not fire original OnDeath");
    if(step<n-1)Check(!PartyDeath.TryBegin(s),"Cannot commit before last casualty");
   }
   Confirm();Check(!PartyDeath.TryBegin(s),"A committed defeat cannot be recommitted");
   Check(PartyDeath.NativeHero(s.Primary,s.Primary.Hero),"Primary remains authorised after restoration");
   foreach(var p in s.Players.Skip(1))Check(!PartyDeath.NativeHero(p,p.Hero),"Retired extra cannot run native global death");
   var r1=DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero);var r2=DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero);
   Check(heroFactories==0,"Native factory is deferred until first MoveNext");
   Drain(r1);Drain(r2);Check(heroDeaths==1&&heroFactories==1,"Original OnDeath occurs once per committed defeat");
   Check(!Block(Enemy(),"HERO DEAD"),"World receives native group defeat notification");
   if(zone=="normal"){
    var duplicate=DeathHooks.Manager(ManagerFactory,new(),0);Drain(scheduledManager);Drain(duplicate);
    Check(before==1&&after==1&&saves==1,"Original API before/after and save execute once");
   }else Check(before==0&&after==0&&scheduledManager==null,"Dream/Godhome retain their native death-prefab return route");
  }
  // Being in a recovery state, without a body, or temporarily invisible must
  // never look like genuine death. Neither blue health nor a stale Down flag.
  Party(2);Down(s.Primary);var extra=s.Players[1];extra.Hazard=true;extra.Hero.gameObject.activeInHierarchy=false;
  Check(!PartyDeath.TryBegin(s),"Hidden hazard recovery is not death");extra.Hero=null;extra.Ready=false;extra.SpawnPending=true;
  Check(!PartyDeath.TryBegin(s),"Pending/missing actor blocks global death");extra.Retiring=true;
  Check(!PartyDeath.TryBegin(s),"Connected retiring/recovering actor is not assumed dead");
  Down(extra);extra.Vitals.Blue=1;Check(!PartyDeath.TryBegin(s),"Remaining blue health blocks defeat");extra.Vitals.Blue=0;extra.Vitals.Health=1;
  Check(!PartyDeath.TryBegin(s),"Down with positive health cannot authorise defeat");
  Party(2);Down(s.Primary);s.Players[1].Retiring=true;s.Players[1].Connected=false;
  Check(PartyDeath.TryBegin(s),"Abandoned disconnected retiring slot is not a combatant");
  Party(2);foreach(var p in s.Players)Down(p);PvpMatch.Running=true;
  Check(!PartyDeath.TryBegin(s),"PvP knockout cannot activate vanilla death");Drain(DeathHooks.Manager(ManagerFactory,new(),0));Check(before==0,"PvP death cannot notify API");
  Party(2);foreach(var p in s.Players)Down(p);s.Active=false;Check(!PartyDeath.TryBegin(s),"Inactive co-op cannot claim death");
  int calls=0;Drain(DeathHooks.Hero(h=>{calls++;return CountBody();},s.Primary.Hero));Check(calls==1,"Inactive/single-player original path is untouched");
  Drain(DeathHooks.Manager((g,w)=>{calls++;return CountBody();},new(),0));Check(calls==2,"Inactive manager original path is untouched");
  // Preserve enemy kills, scripted dream returns, hazards and endings. Story
  // exception is limited to known narrative collection, never all dream zones.
  Party(2);foreach(var evt in new[]{"DEATH","ENEMY DEATH","HAZARD DEATH","HAZARD RELOAD","DREAM RETURN","DREAM END","FINISHED","HERO DAMAGED","HERO LANDED"})
   Check(!Block(Enemy(),evt),"Unrelated/narrative event preserved: "+evt);
  DreamSequence.InStoryDream=true;Check(!Block(Enemy()),"Initial Dream Nail story can use native narrative notification");
  s.Data.hasDreamNail=true;Check(Block(Enemy()),"Story scene after acquisition not a blanket bypass");s.Data.hasDreamNail=false;Down(s.Primary);
  Check(Block(Enemy(),"HERO DEAD",s.Primary),"Even in story, actual individual death cannot leak");
  DreamSequence.InStoryDream=false;CoopEnding.Active=true;Check(!Block(Enemy(),"HERO DEAD",s.Players[1]),"Nonfatal ending animation preserved");
  Check(Block(Enemy(),"HERO DEAD",s.Primary),"Ending cannot turn actual local death into group defeat");CoopEnding.Active=false;
  s.TeamWipe=s.AllowVanillaDeath=true;Check(Block(Enemy(),"HERO DEAD",s.Primary),"Flags without proof cannot authorise global death");
  Drain(DeathHooks.Manager(ManagerFactory,new(),0));Check(before==0,"Fake AllowVanillaDeath flag cannot expose API");
  // Death-prefab ownership without OwnerTag, source supplied by FSM, summons
  // with owner tags, and bounded logging under repeated per-frame notifications.
  Party(2);Down(s.Primary);var summoned=new GameObject("Owned summon");s.Owners[summoned]=s.Primary;
  Check(Block(new(){Name="summon",GameObject=summoned},"HERO DEAD",s.Primary),"Owned summon is not a hero body");
  PlayerContext.Current=s.Players[1];Check(!PartyDeath.BlockEvent(Body(s.Primary),new("HERO DEAD"),new(){SentByFsm=DeathBody(s.Primary)}),"Death prefab source overrides current and resolves without tag");
  PlayerContext.Current=null;var mage=Enemy();int logStart=Diagnostics.Messages.Count;for(int i=0;i<1000;i++)Check(Block(mage),"Repeated world event suppressed");
  Check(Diagnostics.Messages.Count==logStart+1,"Death diagnostic is once per recipient/event/scene");
  int events=0;PlayerContext.Current=s.Players[1];
  DeathHooks.Event((f,e,d)=>events++,Body(s.Primary),new("HERO DEAD"),new(){SentByGameObject=s.Primary.Hero.gameObject});
  Check(events==1,"Full FsmEvent pipeline uses actual source instead of old current-context death filter");
  DeathHooks.Event((f,e,d)=>events++,Body(s.Primary),new("HERO DAMAGED"),new(){SentByGameObject=s.Primary.Hero.gameObject});
  Check(events==1,"Other local-event filters retain original isolation");
  DeathHooks.Event((f,e,d)=>events++,Enemy(),new("HERO DEAD"),null);Check(events==1,"Full pipeline blocks world death before observers");
  DeathHooks.Event((f,e,d)=>events++,Enemy(),new("DEATH"),null);Check(events==2,"Full pipeline retains real enemy DEATH");PlayerContext.Current=null;
  Check(!PartyDeath.BlockEvent(mage,null,null),"Null event harmless");Check(PartyDeath.BlockEvent(null,new("HERO DEAD"),null),"Unknown recipient cannot leak global death");
  // API completion must survive the scene reset which the native child causes.
  Party(2);foreach(var p in s.Players)Down(p);Confirm();Drain(DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero));resetAtEnd=true;
  Drain(scheduledManager);Check(before==1&&after==1&&saves==1,"Native scene change retains exactly one original API After callback");
  Check(!PartyDeath.NativeHero(s.Primary,s.Primary.Hero),"Native authorisation expires after scene reset");
  // Nested mod wrappers around the actual API shape also finish their own tail.
  Party(2);foreach(var p in s.Players)Down(p);Confirm();world="dream";Drain(DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero));resetAtEnd=true;
  Drain(PartyDeath.ManagerRoutine(()=>TailWrapper()));Check(before==1&&after==1&&nativeBodies==2,"Nested API/mod completion tail survives native reset");
  // Iterator captured before an external transition or a later defeat must
  // never be made valid by the new proof. Nor can a late old hero resume.
  Party(2);foreach(var p in s.Players)Down(p);Confirm();world="dream";var oldHero=DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero);
  PartyDeath.Reset();s.TeamWipe=false;foreach(var p in s.Players){p.Retiring=false;Down(p);}Confirm();Drain(oldHero);Check(heroDeaths==0,"Old unstarted hero cannot borrow later defeat proof");
  Drain(DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero));var oldManager=DeathHooks.Manager(ManagerFactory,new(),0);
  PartyDeath.Reset();s.TeamWipe=false;foreach(var p in s.Players){p.Retiring=false;Down(p);}Confirm();Drain(oldManager);
  Check(before==0&&managerFactories==0,"Old manager factory remains suppressed after replacement proof");
  // Scene change in a native factory: don't execute its stale iterator body.
  Party(2);foreach(var p in s.Players)Down(p);Confirm();
  Drain(DeathHooks.Hero(h=>{PartyDeath.Reset();return CountBody();},s.Primary.Hero));Check(nativeBodies==0,"Factory-triggered external transition blocks stale body");
  // If transition interrupts the actual save wait, do not run more native
  // actions; Dispose itself must not invent an After callback.
  Party(2);foreach(var p in s.Players)Down(p);Confirm();Drain(DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero));
  Check(scheduledManager.MoveNext(),"API wrapper starts");var child=(IEnumerator)scheduledManager.Current;
  Check(child.MoveNext(),"Native save wait starts");PartyDeath.Reset();s.TeamWipe=false;Drain(child);Drain(scheduledManager);
  Check(before==1&&after==0&&nativeBodies==0,"Aborted external transition cannot resume native death or fabricate After");
  // Context isolation, manager deduplication and propagated native exceptions.
  Party(3);foreach(var p in s.Players)Down(p);Confirm();PlayerContext.Current=s.Players[1];
  Drain(DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero));Check(PlayerContext.Current==s.Players[1],"Primary death restores caller context");
  Drain(DeathHooks.Manager(ManagerFactory,new(),0));Check(before==0,"Extra context cannot start global manager death");PlayerContext.Current=null;Drain(scheduledManager);
  Check(before==1&&after==1,"Primary-started manager remains valid after outer extra context closes");
  Party(2);foreach(var p in s.Players)Down(p);Confirm();world="dream";Drain(DeathHooks.Hero(NativeHeroFactory,s.Primary.Hero));
  var broken=PartyDeath.ManagerRoutine(()=>ThrowBody());Check(broken.MoveNext(),"Native failing routine begins");bool threw=false;
  try{broken.MoveNext();}catch(InvalidOperationException){threw=true;}Check(threw&&finallyCalls==1&&PlayerContext.Current==null,"Native exception propagated and context/finally restored once");(broken as IDisposable)?.Dispose();Check(finallyCalls==1,"Repeat disposal safe");
  // The existing nonfatal manager-hazard exception is preserved verbatim.
  Party(2);PlayerContext.Current=s.Players[1];calls=0;Drain(DeathHooks.Hazard((g,w)=>{calls++;return CountBody();},new(),0));Check(calls==0,"Extra's nonfatal hazard manager remains local");
  PlayerContext.Current=null;Drain(DeathHooks.Hazard((g,w)=>{calls++;return CountBody();},new(),0));Check(calls==1,"Native story/P1 hazard recovery remains available");
  Console.WriteLine("PASS "+checks+" actual PartyDeath and extracted hook checks with controlled game/API doubles; Unity gameplay untested.");
 }
}
