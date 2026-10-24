using UnityEngine;
namespace KO.HollowKnight8 {
internal static class SpawnSafety {
 static readonly Collider2D[] overlaps=new Collider2D[128];
 static readonly RaycastHit2D[] casts=new RaycastHit2D[128];
 const int SolidLayers=(1<<8)|(1<<25);
 internal static bool Solid(Collider2D c){return c&&c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&(c.gameObject.layer==8||c.gameObject.layer==25)&&!c.GetComponentInParent<HeroController>();}
 internal static Vector2 Clearance(Vector3 size){return new Vector2(Mathf.Max(.1f,size.x+.10f),Mathf.Max(.1f,size.y-.04f));}
 static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
 internal static bool BodyInside(Vector3 at,Vector3 offset,Vector3 size){
  var gm=GameManager.instance;
  if(!gm||!Finite(at.x)||!Finite(at.y)||!Finite(at.z)||!Finite(offset.x)||!Finite(offset.y)
   ||!Finite(size.x)||!Finite(size.y)||size.x<=0||size.y<=0)return false;
  Vector3 center=at+offset;
  return RecoveryRules.InRoom(center.x-size.x*.5f,center.y-size.y*.5f,gm.sceneWidth,gm.sceneHeight)
   &&RecoveryRules.InRoom(center.x+size.x*.5f,center.y+size.y*.5f,gm.sceneWidth,gm.sceneHeight);
 }
 // The native check shrinks width by .14, admitting a thin wall at the body's
 // edge. Keep a horizontal margin while allowing the feet to touch the floor.
 internal static bool Footprint(bool normal,PlayerSlot p,Vector3 at,Vector3 offset,Vector3 size,bool arena){
  if(!normal||!BodyInside(at,offset,size))return false;
  int count=Physics2D.OverlapBoxNonAlloc(at+offset,Clearance(size),0f,overlaps,SolidLayers);
  if(count==overlaps.Length)return false;
  for(int i=0;i<count;i++)if(Solid(overlaps[i]))return false;
  return true;
 }
 internal static bool ClearAt(PlayerSlot p,Vector3 at){
  if(p==null||!p.Hero)return false;
  var b=DuelGround.Body(p);string why;
  return DuelGround.Clear(p,at,b.center-p.Hero.transform.position,b.size,out why);
 }
 internal static bool Path(PlayerSlot p,Vector3 from,Vector3 to){
  if(p==null||!p.Hero||!ClearAt(p,to))return false;
  var b=DuelGround.Body(p);Vector3 offset=b.center-p.Hero.transform.position;
  if(!Footprint(true,p,from,offset,b.size,false))return false;
  Vector2 delta=to-from;float distance=delta.magnitude;if(distance<.01f)return true;
  int count=Physics2D.BoxCastNonAlloc(from+offset,Clearance(b.size),0f,delta/distance,casts,distance,SolidLayers);
  if(count==casts.Length)return false;
  for(int i=0;i<count;i++)if(Solid(casts[i].collider))return false;
  return true;
 }
 static bool FloorHit(Vector2 origin,float distance,out RaycastHit2D floor){
  floor=default(RaycastHit2D);float nearest=float.MaxValue;
  int count=Physics2D.RaycastNonAlloc(origin,Vector2.down,casts,distance,SolidLayers);
  if(count==casts.Length)return false;
  for(int i=0;i<count;i++)if(Solid(casts[i].collider)&&casts[i].distance<nearest){nearest=casts[i].distance;floor=casts[i];}
  return floor.collider&&floor.normal.y>=.6f;
 }
 internal static bool Floor(CoopSession s,PlayerSlot p,Vector3 origin,out Vector3 result,Vector3? avoid=null){
  result=Vector3.zero;
  if(s==null||p==null||!p.Hero)return false;
  Physics2D.SyncTransforms();var b=DuelGround.Body(p);Vector3 offset=b.center-p.Hero.transform.position;
  if(!Footprint(true,p,origin,offset,b.size,false))return false;
  float foot=b.extents.y-offset.y;
  for(int i=0;i<=24;i++)for(int side=0;side<(i==0?1:2);side++){
   float x=origin.x+i*(side==0?1:-1);RaycastHit2D hit;
   if(!FloorHit(new Vector2(x,origin.y+3f),20f,out hit))continue;
   var at=new Vector3(x,hit.point.y+foot+.06f,origin.z);
   if(avoid.HasValue&&Vector2.Distance(at,avoid.Value)<=3.5f||!s.IsSafe(at,p)||!Path(p,origin,at))continue;
   result=at;return true;
  }return false;
 }
 // Both revival and manual rescue use this same bounded search. A free point
 // across a wall is not a point alongside the ally: sweep the body to it too.
 internal static bool NearAlly(CoopSession s,PlayerSlot ally,PlayerSlot arriving,out Vector3 result){
  result=Vector3.zero;
  if(s==null||ally==null||!ally.Hero||arriving==null||!arriving.Hero)return false;
  Physics2D.SyncTransforms();Vector3 origin=ally.Hero.transform.position;
  var b=DuelGround.Body(arriving);Vector3 offset=b.center-arriving.Hero.transform.position;
  var anchorBody=DuelGround.Body(ally);
  // Align feet, not root transforms: another skin/scale can have a different
  // body height or collider offset, even while the ally stands on safe ground.
  origin.y+=b.extents.y-offset.y-(anchorBody.extents.y-(anchorBody.center.y-ally.Hero.transform.position.y));
  if(!s.InRoom(origin)||!ArenaGather.Allows(origin)||!Footprint(true,arriving,origin,offset,b.size,false))return false;
  float foot=b.size.y*.5f-offset.y,step=Mathf.Max(.8f,b.size.x+.16f);
  int preferred=(arriving.Index&1)==0?1:-1;
  for(int i=1;i<=4;i++)for(int side=0;side<2;side++){
   float x=origin.x+i*step*(side==0?preferred:-preferred);
   RaycastHit2D hit;
   if(FloorHit(new Vector2(x,origin.y+1.5f),5f,out hit)){
    var at=new Vector3(x,hit.point.y+foot+.06f,origin.z);
    if(!PartyRules.Near(at.x-origin.x,at.y-origin.y)||!s.InRoom(at)||!ArenaGather.Allows(at)||!s.IsSafe(at,arriving)||!Path(arriving,origin,at))continue;
    result=at;return true;
   }
  }
  // Sharing a narrow ledge is fine. Empty air over a pit is not a landing.
  if(s.IsSafe(origin,arriving)){result=origin;return true;}return false;
 }
 // Recheck immediately before health/control/collider restoration. Saved
 // dream checkpoints and moving gates can bypass earlier destination checks.
 internal static bool Recovery(CoopSession s,PlayerSlot p,ref Vector3 at){
  Physics2D.SyncTransforms();
  if(s.InRoom(at)&&s.IsSafe(at,p))return true;
  Vector3 near;
  if(Living(s,p,out near)){at=near;Diagnostics.Write("SPAWN landing fallback P"+(p.Index+1)+" -> "+at);return true;}
  p.HazardUntil=Time.time+.35f;
  Diagnostics.Throttled("SPAWN waiting for clear body P"+(p.Index+1),new System.InvalidOperationException("Destination intersects terrain"));
  return false;
 }
 static bool Anchor(PlayerSlot p){return p!=null&&p.Hero&&p.Ready&&p.Alive&&p.Connected&&!p.SpawnPending
  &&!p.ArenaTransfer&&!p.Hero.cState.transitioning&&!TransitionVote.Holding(p)&&!EmergencyWarp.Active(p)&&!CoopEnding.HoldsActor(p);}
 internal static bool Living(CoopSession s,PlayerSlot p,out Vector3 at){
  at=Vector3.zero;uint tried=0;
  for(int attempt=0;attempt<s.Players.Count;attempt++){
   int best=-1;
   for(int i=0;i<s.Players.Count;i++)if((tried&(1u<<i))==0&&s.Players[i]!=p&&Anchor(s.Players[i])
    &&(best<0||s.Players[i].LifeStartedAt<s.Players[best].LifeStartedAt))best=i;
   if(best<0)return false;tried|=1u<<best;
   if(NearAlly(s,s.Players[best],p,out at))return true;
  }return false;
 }
 internal static bool Join(CoopSession s,PlayerSlot arriving,out Vector3 at){
  at=Vector3.zero;
  if(s==null||arriving==null||s.Primary==null||!s.Primary.Hero)return false;
  Physics2D.SyncTransforms();
  // Before cloning, use the exact template body. FinishSpawn checks the
  // resulting actor again after native initialization/charm changes.
  PlayerSlot body=arriving.Hero?arriving:s.Primary;
  var arena=ArenaGather.Anchor;
  if(Anchor(arena)&&NearAlly(s,arena,body,out at))return true;
  if(Anchor(s.Primary)&&NearAlly(s,s.Primary,body,out at))return true;
  foreach(var ally in s.Players)if(ally!=arriving&&Anchor(ally)&&NearAlly(s,ally,body,out at))return true;
  foreach(var ally in s.Players)if(ally!=arriving&&ally.Hero&&ally.Ready&&ally.HasSafePoint
   &&Vector2.Distance(ally.Hero.transform.position,ally.SafePoint)<8f&&ArenaGather.Allows(ally.SafePoint)
   &&s.IsSafe(ally.SafePoint,body)){at=ally.SafePoint;return true;}
  // A downed P1 can be the native arrival proxy while the living guests are
  // still parked. Only accept its validated current-room arrival, never its
  // corpse coordinates or an unchecked clamped fallback.
  if(s.HasArrival&&s.Primary.Down&&ArenaGather.Allows(s.Arrival)&&s.IsSafe(s.Arrival,body)){at=s.Arrival;return true;}
  return false;
 }
 internal static bool RestoreRescue(CoopSession s,EmergencyWarp.Trip trip){
  var p=trip.Player;
  if(s==null||!s.Active||!s.Gameplay||!p.Ready)return true;
  Physics2D.SyncTransforms();
  var at=p.Hero.transform.position;
  if(s.IsSafe(at,p))return true;
  if(ArenaGather.Allows(trip.Origin)&&s.IsSafe(trip.Origin,p))at=trip.Origin;
  else if(p.HasSafePoint&&ArenaGather.Allows(p.SafePoint)&&s.IsSafe(p.SafePoint,p))at=p.SafePoint;
  else if(!Living(s,p,out at)){
   // A moving gate can invalidate the rescue origin. Keep this actor hidden
   // and retry the normal recovery path, without taking health or reviving.
   foreach(var item in trip.Colliders)if(item.Key){p.ColliderStates[item.Key]=item.Value;item.Key.enabled=false;}
   foreach(var item in trip.Renderers)if(item.Key){p.RendererStates[item.Key]=item.Value;item.Key.enabled=false;}
   p.Hazard=true;p.HazardUntil=Time.time+.15f;p.InputBlocked=true;ActorRecovery.Freeze(p);
   Diagnostics.Write("SPAWN rescue waiting for safe landing P"+(p.Index+1));return false;
  }
  p.Hero.transform.position=at;return true;
 }
}
}
