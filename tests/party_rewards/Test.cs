using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using HutongGames.PlayMaker;
using KO.HollowKnight8;

partial class NativeProbe
{
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static HeroController Hero(float x){var g=new GameObject();g.transform.position=new(x,0);g.Add<SpriteFlash>();return g.Add<HeroController>();}
 static CoopSession Party(int n=8){SoulOrbRouting.Reset();Lifeblood.Reset();var s=new CoopSession();Plugin.Self=new(){Session=s};PlayerContext.Current=null;PlayerContext.TargetingEnemy=false;Time.unscaledTime+=10;Time.time=Time.unscaledTime;GameManager.instance=new(){playerData=s.Data};for(int i=0;i<n;i++)s.Players.Add(new(){Index=i,Hero=Hero(i*2)});HeroController.instance=s.Primary.Hero;GameManager.instance.NativeAssign(s.Primary);return s;}
 static Fsm Totem(string name="Soul Totem"){var go=new GameObject{name=name};return new(){GameObject=go,Name="Soul Totem"};}
 static Collider2D Nail(PlayerSlot p){var g=new GameObject{tag="Nail Attack"};g.transform.parent=p.Hero.transform;return g.Add<Collider2D>();}
 static void Hit(Fsm f,PlayerSlot p,bool accepted=true)
 {
  var hitter=SoulOrbRouting.Track(f,Nail(p));Check(hitter==p,"totem did not resolve actual nail");
  using(SoulOrbRouting.EnterContact(f,hitter))
  {
   Check(SoulOrbRouting.Resolve(f)==p,"contact did not expose actual hitter during native callback");
   if(accepted)SoulOrbRouting.StateEntered(new(){Fsm=f});
  }
 }
 static SoulOrb Orb(PlayerSlot p){var g=new GameObject();g.Add<Rigidbody2D>();g.Add<SpriteRenderer>();g.Add<TrailRenderer>();g.Add<AudioSource>();var o=g.Add<SoulOrb>();o.getParticles=g.Add<ParticleSystem>();o.Awake();using(PlayerContext.Enter(p))o.OnEnable();o.Start();return o;}
 static void Context(CoopSession s){Check(PlayerContext.Current==null&&!PlayerContext.TargetingEnemy&&HeroController.instance==s.Primary.Hero,"native reward leaked singleton/context across yield");}
 static void Negative()
 {
  SoulOrbRouting.Uninstall();var s=Party(2);var p=s.Players[1];var o=Orb(p);var r=o.Zoom(false);Check(r.MoveNext()&&s.Data.MPCharge==2&&p.Vitals.Soul==0,"exact native iterator did not reproduce wrong soul owner");Check(s.Primary.Hero.GetComponent<SpriteFlash>().SoulFlashes==1&&p.Hero.GetComponent<SpriteFlash>().SoulFlashes==0&&o.target==s.Primary.Hero.transform,"exact native target and soul flash did not reproduce P1 bug");Check(!r.MoveNext()&&!o.gameObject.activeInHierarchy,"native negative control does not recycle");SoulOrbRouting.Install();SoulOrbRouting.Install();Lifeblood.Install();Lifeblood.Install();Check(On.SoulOrb.Count_Zoom==1&&On.HealthManager.Count_OnEnable==1,"reward hooks installed twice");
 }
 static void Soul()
 {
  foreach(int count in new[]{2,4,8})for(int i=0;i<count;i++)
  {
   var s=Party(count);var p=s.Players[i];var f=Totem();Hit(f,p);Check(SoulOrbRouting.Resolve(f)==p,"totem forgot actual accepted nail hitter");
   var other=s.Players.First(q=>q!=p);var body=other.Hero.gameObject.Add<Collider2D>();body.gameObject.tag="Player";Check(SoulOrbRouting.Track(f,body)==null&&SoulOrbRouting.Resolve(f)==p,"walking past a totem stole its delayed reward");
   var o=Orb(SoulOrbRouting.Resolve(f));Check(o.target==p.Hero.transform,"native Start overwrote orb target with P1");o.transform.position=new(p.Hero.transform.position.x+10,0);var r=o.Zoom();Check(r.MoveNext()&&s.Players.All(q=>q.Vitals.Soul==0),"orb awarded soul before reaching its hitter");Context(s);
   o.transform.position=p.Hero.transform.position;Check(r.MoveNext()&&r.Current is WaitForSeconds wait&&Math.Abs(wait.Seconds-.4f)<.001,"native collection timing changed");Check(p.Vitals.Soul==2&&p.Hero.GetComponent<SpriteFlash>().SoulFlashes==1&&s.Players.Where(q=>q!=p).All(q=>q.Vitals.Soul==0&&q.Hero.GetComponent<SpriteFlash>().SoulFlashes==0),"soul and flash do not belong exclusively to hitter");Context(s);Check(!r.MoveNext()&&!o.gameObject.activeInHierarchy,"owned native orb did not recycle");Context(s);
   Check(SoulOrbRouting.Resolve(new(){GameObject=f.GameObject,Name="Soul Totem"})==p,"totem FSMs on same source lost hitter");
  }
  {
   var s=Party();var f=Totem();Hit(f,s.Players[2]);Hit(f,s.Players[7],accepted:false);
   Check(SoulOrbRouting.Resolve(f)==s.Players[2],"ignored cooldown hit stole delayed accepted reward");
   var o=Orb(SoulOrbRouting.Resolve(f));var r=o.Zoom(false);Check(r.MoveNext()&&s.Players[2].Vitals.Soul==2&&s.Players[7].Vitals.Soul==0,"ignored contact received pending totem orbs");Check(!r.MoveNext(),"accepted totem flight failed cleanup");
   Hit(f,s.Players[7]);Check(SoulOrbRouting.Resolve(f)==s.Players[7],"next accepted hit could not change totem owner");
   using(SoulOrbRouting.EnterContact(f,s.Players[4])){SoulOrbRouting.Reset();SoulOrbRouting.StateEntered(new(){Fsm=f});}
   Check(SoulOrbRouting.Resolve(f)==null,"scene reset resurrected a previous contact scope");
  }
  {
   var s=Party();var flights=new IEnumerator[8];var orbs=new SoulOrb[8];for(int i=0;i<8;i++){orbs[i]=Orb(s.Players[i]);flights[i]=orbs[i].Zoom(false);}for(int i=7;i>=0;i--){Check(flights[i].MoveNext(),"simultaneous native collection ended early");Context(s);}Check(s.Players.All(p=>p.Vitals.Soul==2&&p.Hero.GetComponent<SpriteFlash>().SoulFlashes==1),"interleaved orbs crossed award/flash ownership");for(int i=0;i<8;i++)Check(!flights[i].MoveNext(),"simultaneous orb failed to recycle");
  }
  {
   var s=Party();var o=Orb(s.Players[7]);var old=o.Zoom();o.transform.position=new(100,0);Check(old.MoveNext(),"old flight did not start");o.gameObject.SetActive(false);o.gameObject.SetActive(true);using(PlayerContext.Enter(s.Players[2]))o.OnEnable();Check(o.target==s.Players[2].Hero.transform,"pooled enable retained old cached target");Check(!old.MoveNext()&&o.gameObject.activeInHierarchy,"old coroutine removed newly reused orb");var fresh=o.Zoom(false);Check(fresh.MoveNext()&&s.Players[2].Vitals.Soul==2&&s.Players[7].Vitals.Soul==0,"pooled reward reused previous owner");Check(!fresh.MoveNext(),"pooled flight did not finish");
  }
  {
   var s=Party();var p=s.Players[7];var o=Orb(p);p.Hero=Hero(20);var r=o.Zoom(false);Check(!r.MoveNext()&&!o.gameObject.activeInHierarchy&&s.Players.All(q=>q.Vitals.Soul==0),"replaced hero awarded replacement slot/P1 or left an orphan orb");Context(s);
   o=Orb(s.Players[3]);s.Players[3].Hero.OnSoul=()=>throw new InvalidOperationException("native failure");r=o.Zoom(false);try{r.MoveNext();throw new Exception("exception not propagated");}catch(InvalidOperationException){}Context(s);
  }
  {
   var s=Party();var o=Orb(s.Players[5]);o.awardSoul=false;o.dontRecycle=true;var r=o.Zoom(false);Check(r.MoveNext()&&s.Players.All(p=>p.Vitals.Soul==0)&&s.Players[5].Hero.GetComponent<SpriteFlash>().SoulFlashes==1,"native visual-only orb changed reward semantics");int recycled=ObjectPoolExtensions.Recycles;Check(!r.MoveNext()&&!o.gameObject.activeInHierarchy&&ObjectPoolExtensions.Recycles==recycled,"dontRecycle native behavior changed");
   o=Orb(s.Players[4]);r=o.Zoom();o.transform.position=new(100,0);Check(r.MoveNext(),"scene cleanup flight did not start");SoulOrbRouting.Reset();Check(!r.MoveNext()&&!o.gameObject.activeInHierarchy,"scene cleanup stranded an owned flight");
  }
 }
 static Fsm Bug(string name="Health Scuttler"){var g=new GameObject{name=name};g.Add<HealthManager>().OnEnable();return new(){GameObject=g,Name="Health Scuttler"};}
 static bool Award(Fsm f,string name="ADD BLUE HEALTH"){using(Lifeblood.Enter(f))return Lifeblood.Award(new(name));}
 static void Blue()
 {
  foreach(int count in new[]{2,4,8})
  {
   var s=Party(count);foreach(var p in s.Players)p.Hero.transform.position=new(p.Index,0);var f=Bug();Lifeblood.Hit(f.GameObject.GetComponent<HealthManager>(),s.Players.Last());Check(!Award(f,"OTHER EVENT"),"non-health event consumed a bug");Check(Award(f)&&s.Players.All(p=>p.Vitals.Blue==1&&p.HealFlashUntil>Time.unscaledTime),"nearby players did not all receive lifeblood");Context(s);
   for(int n=0;n<10;n++){Time.unscaledTime+=.7f;Check(Award(f)&&s.Players.All(p=>p.Vitals.Blue==1),"late/multi-hero broadcast duplicated shared lifeblood");}
   f.GameObject.GetComponent<HealthManager>().OnEnable();Check(Award(f)&&s.Players.All(p=>p.Vitals.Blue==2),"pooled bug remained consumed across lifetimes");Context(s);
  }
  foreach(string guard in new[]{"far","down","hazard","retired","unready","disconnected","spawn","transfer","warp","waiting","zerohealth","inactive"})
  {
   var s=Party();foreach(var p in s.Players)p.Hero.transform.position=new();var excluded=s.Players[6];
   switch(guard){case "far":excluded.Hero.transform.position=new(Lifeblood.ShareRadius+.01f,0);break;case "down":excluded.Down=true;break;case "hazard":excluded.Hazard=true;break;case "retired":excluded.Retiring=true;break;case "unready":excluded.Ready=false;break;case "disconnected":excluded.Connected=false;break;case "spawn":excluded.SpawnPending=true;break;case "transfer":excluded.ArenaTransfer=true;break;case "warp":excluded.Warp=true;break;case "waiting":excluded.Holding=true;break;case "zerohealth":excluded.Vitals.Health=0;break;case "inactive":excluded.Hero.gameObject.SetActive(false);break;}
   var f=Bug("Life Seed");Check(Award(f)&&excluded.Vitals.Blue==0&&s.Players.Where(p=>p!=excluded).All(p=>p.Vitals.Blue==1),"lifeblood eligibility failed for "+guard);Context(s);
  }
  {
   var s=Party();foreach(var p in s.Players)p.Hero.transform.position=new(30,0);s.Players[2].Hero.transform.position=new(Lifeblood.ShareRadius,0);var f=Bug();Lifeblood.Hit(f.GameObject.GetComponent<HealthManager>(),s.Players[7]);Check(Award(f)&&s.Players[2].Vitals.Blue==1&&s.Players.Where(p=>p.Index!=2).All(p=>p.Vitals.Blue==0),"lifeblood radius did not center on bug or rewarded a distant killer");
   s=Party();foreach(var p in s.Players)p.Hero.transform.position=new();for(int i=0;i<4;i++)Check(Award(Bug()),"distinct bug was treated as duplicate");Check(s.Players.All(p=>p.Vitals.Blue==4),"separate bugs did not stack once each");
   var pickup=new Fsm{GameObject=new GameObject{name="Blue Health Pickup"},Name="Blue Health"};Check(Lifeblood.Track(pickup,s.Players[3].Hero.gameObject.Add<Collider2D>())==s.Players[3],"ordinary pickup lost collector");Check(Award(pickup)&&s.Players[3].Vitals.Blue==5&&s.Players.Where(p=>p.Index!=3).All(p=>p.Vitals.Blue==4),"non-bug blue pickup was unintentionally shared");Check(!Award(Totem()),"soul totem classified as lifeblood");Context(s);
  }
 }
 static void Main(){Negative();Soul();Blue();Scuttlers();Totems();SoulOrbRouting.Uninstall();Lifeblood.Uninstall();Check(On.SoulOrb.Count_Zoom==0&&On.SoulOrb.Count_OnEnable==0&&On.HealthManager.Count_OnEnable==0&&On.ScuttlerControl.Count_Hit==0&&On.ScuttlerControl.Count_Heal==0,"reward hooks not removed");Console.WriteLine($"PASS {checks} party reward checks: exact native SoulOrb initialization, targeting, flight, award/flash and recycle iterator IL; all slots, concurrent/pool/orphan/exception cases; nearby lifeblood eligibility, radius, repeated broadcast, pooled lifecycle and unchanged ordinary pickups. Unity, player-data IO and health FSM asset callbacks are controlled boundaries.");}
}
