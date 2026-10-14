using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace KO.HollowKnight8 {
internal static class CoopBath {
 sealed class Region {internal Collider2D Collider;internal int Amount=1;internal float Interval=.1f;}
 sealed class Definition {
  internal FsmState[] States;internal FsmStateAction[][] Actions;internal FsmState[] Identity;
  internal bool Refills;internal float Interval=.1f;
  internal bool Matches(Fsm f){if(States!=f.States||States==null||Identity.Length!=States.Length)return false;
   for(int i=0;i<States.Length;i++)if(Identity[i]!=States[i]||Actions[i]!=(States[i]==null?null:States[i].Actions))return false;return true;}
 }
 static readonly Dictionary<Fsm,Definition> definitions=new Dictionary<Fsm,Definition>();
 static readonly List<Fsm> expired=new List<Fsm>();
 static readonly List<Region> regions=new List<Region>();
 static readonly float[] nextGain=new float[8];
 static readonly PlayerSlot[] occupants=new PlayerSlot[8];
 static int scene=-1,mask;
 static float nextScan;
 internal static void Reset(){regions.Clear();definitions.Clear();expired.Clear();System.Array.Clear(nextGain,0,8);System.Array.Clear(occupants,0,8);scene=-1;mask=0;nextScan=0;}
 static bool Active(CoopSession s){var gm=GameManager.instance;return s!=null&&s.Active&&s.Gameplay&&gm&&!gm.isPaused&&!gm.IsLoadingSceneTransition&&gm.HasFinishedEnteringScene&&Time.timeScale>0;}
 static bool Named(string name){name=name.ToLowerInvariant();return name=="spa"||name.StartsWith("spa ")||name.StartsWith("spa_")||name.Contains("hot spring")||name.Contains("hotspring")||name.Contains("bath");}
 static void Add(Collider2D c,int amount,float interval){if(!c||!c.isTrigger||c.GetComponentInParent<HeroController>()||c.GetComponentInParent<HealthManager>()||c.GetComponentInParent<DamageHero>())return;
  foreach(var r in regions)if(r.Collider==c){if(amount>0){r.Amount=amount;r.Interval=interval;}return;}
  regions.Add(new Region{Collider=c,Amount=amount>0?amount:1,Interval=interval});
 }
 static Definition Describe(Fsm f){if(f==null||f.States==null)return null;
  if(definitions.TryGetValue(f,out var found)&&found.Matches(f))return found;
  var d=new Definition{States=f.States,Identity=(FsmState[])f.States.Clone(),Actions=new FsmStateAction[f.States.Length][]};
  for(int i=0;i<f.States.Length;i++){var state=f.States[i];var actions=state==null?null:state.Actions;d.Actions[i]=actions;if(actions==null)continue;
   bool refill=false;foreach(var a in actions){var call=a as CallMethodProper;if(call!=null&&call.methodName!=null&&call.methodName.Value=="TryAddMPChargeSpa")refill=true;}
   if(!refill)continue;d.Refills=true;
   foreach(var a in actions){var wait=a as Wait;if(wait!=null&&wait.time!=null&&wait.time.Value>=.02f&&wait.time.Value<=1f)d.Interval=wait.time.Value;}
  }definitions[f]=d;return d;
 }
 static void Register(Fsm f,int amount,Definition d=null){if(f==null||!f.GameObject||RoleSystem.Session.Resolve(f)!=null)return;
  if(d==null){d=Describe(f);if(amount>0&&d!=null&&!d.Refills){definitions.Remove(f);d=Describe(f);}}
  float interval=d==null ? .1f : d.Interval;
  // Read a live refill state's delay when the native callback runs. Mods can
  // change an action value in place without replacing the cached state array.
  if(amount>0&&f.ActiveState!=null&&f.ActiveState.Actions!=null)foreach(var action in f.ActiveState.Actions){
   var wait=action as Wait;if(wait!=null&&wait.time!=null&&wait.time.Value>=.02f&&wait.time.Value<=1f)interval=wait.time.Value;}
  var cs=f.GameObject.GetComponentsInChildren<Collider2D>(true);bool found=false;foreach(var c in cs)if(c.isTrigger){Add(c,amount,interval);found=true;}
  if(!found&&f.GameObject.transform.parent)foreach(var c in f.GameObject.transform.parent.GetComponents<Collider2D>())Add(c,amount,interval);
 }
 static void Scan(){
  int nowScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;if(scene!=nowScene){Reset();scene=nowScene;}
  if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+3f;
  regions.RemoveAll(r=>!r.Collider);
  expired.Clear();foreach(var pair in definitions)if(pair.Key==null||!pair.Key.GameObject)expired.Add(pair.Key);
  foreach(var f in expired)definitions.Remove(f);expired.Clear();
  foreach(var c in Object.FindObjectsOfType<Collider2D>())if(c.isTrigger&&(Named(c.name)||(c.transform.parent&&Named(c.transform.parent.name))))Add(c,0,.1f);
  // Do not depend on a remembered enter event: native FSMs may have gone idle
  // when the first bather filled up, or that bather may already have left.
  foreach(var f in Object.FindObjectsOfType<PlayMakerFSM>()){
   if(f.Fsm==null||f.Fsm.States==null)continue;
   // Large rooms contain thousands of unrelated actions. Classify those once;
   // keep occupancy and the individual refill timers live every tick.
   var d=Describe(f.Fsm);if(d!=null&&d.Refills)Register(f.Fsm,0,d);
  }
 }
 static Region Inside(PlayerSlot p){
  if(p==null||!p.Alive||!p.Ready||p.SpawnPending||p.ArenaTransfer||!p.Hero||!p.Hero.gameObject.activeInHierarchy)return null;
  Vector2 pos=p.Hero.transform.position;Collider2D hero=p.Hero.GetComponent<Collider2D>();
  foreach(var r in regions){var c=r.Collider;if(!c||!c.enabled||!c.gameObject.activeInHierarchy)continue;
   // The hero's feet may overlap shallow water while its pivot is above it.
   // Collider2D queries ignore the unrelated scene Z used by sprites.
   if(c.OverlapPoint(pos))return r;
   if(hero&&hero.enabled){var d=c.Distance(hero);if(d.isOverlapped)return r;}
  }return null;
 }
 internal static bool Refill(On.HeroController.orig_TryAddMPChargeSpa orig,HeroController hero,int amount){
  var s=RoleSystem.Session;if(s==null||!s.Active)return orig(hero,amount);
  if(!Active(s))return false;
  Scan();Register(WorldRouting.Current,Mathf.Max(1,amount));
  return Pump(s);
 }
 internal static void Tick(CoopSession s){if(!Active(s))return;Scan();Pump(s);}
 static bool Pump(CoopSession s){
  bool needs=false;int currentMask=0;
  foreach(var p in s.Players){if(p.Index<0||p.Index>=8)continue;int i=p.Index;var region=Inside(p);
   if(region==null){occupants[i]=null;nextGain[i]=0;continue;}
   currentMask|=1<<i;if(occupants[i]!=p){occupants[i]=p;nextGain[i]=Time.time;}
   using(PlayerContext.Enter(p)){
    var data=s.Data;bool room=data.MPCharge<data.maxMP||(!BossSequenceController.BoundSoul&&data.MPReserve<data.MPReserveMax);needs|=room;
    if(!room||Time.time<nextGain[i])continue;
    nextGain[i]=Time.time+region.Interval;
    data.AddMPCharge(region.Amount);p.Capture(data);
   }
  }
  if(mask!=currentMask){mask=currentMask;Diagnostics.Write("SPA occupants="+mask+" regions="+regions.Count+" concurrent refill");}
  return needs;
 }
}
}
