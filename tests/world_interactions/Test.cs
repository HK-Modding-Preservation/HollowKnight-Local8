using System;
using System.Linq;
using UnityEngine;
using KO.HollowKnight8;

class NativeProbe {
 static int checks,scenarios;
 static void Check(bool yes,string why){checks++;if(!yes)throw new Exception(why);}
 static CoopSession Party(int n){
  WorldContactRouting.Reset();PhysicsGuard.Reset();Physics2D.Reset();Diagnostics.Messages.Clear();Time.frameCount++;Time.unscaledTime+=10;
  var s=new CoopSession();Plugin.Self=new(){Session=s};PlayerContext.Current=null;
  for(int i=0;i<n;i++){var go=new GameObject();var h=go.Add<HeroController>();go.Add<Rigidbody2D>();go.Add<Collider2D>();var conveyor=go.Add<ConveyorMovementHero>();conveyor.heroCon=h;h.transform.position=new(i*5,0,0);s.Players.Add(new(){Index=i,Hero=h});}
  HeroController.instance=s.Players[0].Hero;return s;
 }
 static Collider2D Nail(HeroController h,int angle){var go=new GameObject{tag="Nail Attack",layer=17};go.Parent(h.gameObject);go.transform.position=h.transform.position;go.Add<PlayMakerFSM>().FsmVariables.Direction.Value=angle;return go.Add<Collider2D>();}
 static Collider2D Box(HeroController h,string tag="HeroBox"){var go=new GameObject{tag=tag};go.Parent(h.gameObject);go.transform.position=h.transform.position;return go.Add<Collider2D>();}
 static SteepSlope Slope(HeroController h,bool left=true,bool right=false){var go=new GameObject();go.transform.position=h.transform.position;go.Add<Collider2D>();var slope=go.Add<SteepSlope>();slope.slideLeft=left;slope.slideRight=right;slope.Start();return slope;}
 static Collision2D Collision(HeroController h,SteepSlope slope,Collider2D body=null)=>new(){gameObject=h.gameObject,collider=body??h.GetComponent<Collider2D>(),otherCollider=slope.GetComponent<Collider2D>()};
 static T Sensor<T>(Vector3 position,float size=10)where T:MonoBehaviour,new(){var go=new GameObject();go.transform.position=position;go.Add<Collider2D>().Size=new(size,size,1);var result=go.Add<T>();if(result is AlertRange alert)alert.colliders=go.GetComponents<Collider2D>();return result;}
 static ShadowGateColliderControl Gate(){var go=new GameObject();var g=go.Add<ShadowGateColliderControl>();g.disableCollider=go.Add<Collider2D>();g.Setup();return g;}
 static void FreshFrame(float dt=.2f){Time.frameCount++;Time.unscaledTime+=dt;}
 static int Recoils(HeroController h)=>h.RecoilL+h.RecoilR+h.RecoilD;

 static void Baseline(){
  WorldContactRouting.Uninstall();var s=Party(2);var p=s.Players[1].Hero;var tink=new GameObject().Add<TinkEffect>();tink.OnTriggerEnter2D(Nail(p,0));Check(Recoils(s.Players[0].Hero)==1&&Recoils(p)==0,"negative control reproduces native wrong owner");
  var gate=Gate();p.cState.dashing=true;gate.FixedUpdate();Check(gate.disableCollider.enabled,"negative control reproduces guest blocked");s.Players[0].Hero.cState.dashing=true;gate.FixedUpdate();Check(!gate.disableCollider.enabled,"negative control reproduces global gate opening");
  var slope=Slope(p);slope.OnCollisionEnter2D(Collision(p,slope));Check(s.Players[0].Hero.cState.slidingLeft&&!p.cState.slidingLeft,"negative control reproduces cached slope owner");
  p.cState.superDashing=true;Check(!GrassCut.ShouldCut(Box(p)),"negative control reproduces grass singleton");
  WorldContactRouting.Install();WorldContactRouting.Install();Check(On.TinkEffect.Count_OnTriggerEnter2D==1&&On.SteepSlope.Count_OnCollisionStay2D==1&&On.Walker.Count_UpdateWalking==1,"idempotent production installation");
 }
 static void Contacts(){
  foreach(int count in new[]{2,4,5,8})for(int actor=0;actor<count;actor++){
   var s=Party(count);var p=s.Players[actor];var h=p.Hero;
   foreach(int angle in new[]{0,90,180,270}){
    int before=Recoils(h);var tink=new GameObject().Add<TinkEffect>();tink.OnTriggerEnter2D(Nail(h,angle));Check(Recoils(h)==before+(angle==270?0:1),"Tink applies native direction to contacting actor");
    Check(s.Players.Where(x=>x!=p).All(x=>Recoils(x.Hero)==0),"Tink never recoils bystanders");Check(HeroController.instance==s.Players[0].Hero&&PlayerContext.Current==null,"Tink restores context");
   }
   foreach(bool dashing in new[]{false,true}){
    foreach(var other in s.Players)other.Hero.cState.superDashing=!dashing;
    h.cState.superDashing=dashing;var box=Box(h);var vine=new GameObject().Add<BreakableInfectedVine>();vine.OnTriggerEnter2D(box);Check(vine.activated==dashing,"vine checks contacting HeroBox superdash");Check(GrassCut.ShouldCut(box)==dashing,"grass checks contacting HeroBox superdash");
    var large=new GameObject().Add<InfectedBurstLarge>();large.OnTriggerEnter2D(box);var small=new GameObject().Add<InfectedBurstSmall>();small.OnTriggerEnter2D(box);Check(large.effects.Active==dashing&&small.effects.Active==dashing,"both infected bursts check contacting HeroBox");
    Check(HeroController.instance==s.Players[0].Hero,"crystal contact restores hero");
   }
   foreach(string tag in new[]{"Nail Attack","Hero Spell"}){var attack=Box(h,tag);var vine=new GameObject().Add<BreakableInfectedVine>();vine.OnTriggerEnter2D(attack);var large=new GameObject().Add<InfectedBurstLarge>();large.OnTriggerEnter2D(attack);var small=new GameObject().Add<InfectedBurstSmall>();small.OnTriggerEnter2D(attack);Check(vine.activated&&large.effects.Active&&small.effects.Active&&(GrassCut.ShouldCut(attack)==(tag=="Nail Attack")),"native nail/spell branches preserved");}
   p.Down=true;var disabled=new GameObject().Add<TinkEffect>();int hits=Recoils(h);disabled.OnTriggerEnter2D(Nail(h,0));Check(Recoils(h)==hits,"downed actor cannot cause Tink recoil");Check(!GrassCut.ShouldCut(Box(h)),"downed HeroBox cannot authorize grass cutting");p.Down=false;
   var previous=HeroController.instance;h.OnRecoil=()=>throw new InvalidOperationException("native callback failure");try{new GameObject().Add<TinkEffect>().OnTriggerEnter2D(Nail(h,0));throw new Exception("expected failure");}catch(InvalidOperationException){}finally{h.OnRecoil=null;}Check(HeroController.instance==previous&&PlayerContext.Current==null,"exception restores Tink context");
   if(actor!=0){using(PlayerContext.Enter(s.Players[0])){new GameObject().Add<TinkEffect>().OnTriggerEnter2D(Nail(h,0));Check(PlayerContext.Current==s.Players[0]&&HeroController.instance==s.Players[0].Hero,"nested contacting actor restores outer owner");}}
   scenarios++;
  }
 }
 static void Slopes(){
  foreach(int count in new[]{2,4,5,8})for(int actor=0;actor<count;actor++){
   var s=Party(count);var p=s.Players[actor];var h=p.Hero;var a=Slope(h);var b=Slope(h);var c=Collision(h,a);var d=Collision(h,b);var cached=a.hc;
   a.OnCollisionEnter2D(c);b.OnCollisionEnter2D(d);Check(h.cState.slidingLeft,"actual actor enters slope");Check(s.Players.Where(x=>x!=p).All(x=>!x.Hero.cState.slidingLeft),"slope never changes another actor flag");
   h.cState.superDashing=true;a.OnCollisionStay2D(c);Check(h.CancelledDash==1&&h.ResetLanding==1&&h.GetComponent<Rigidbody2D>().velocity.y==-20,"slope body, landing and superdash belong to same hero");Check(s.Players.Where(x=>x!=p).All(x=>x.Hero.CancelledDash==0),"slope preserves bystander superdash");Check(a.hc==cached,"scoped slope cache restored");
   a.OnCollisionExit2D(c);Check(h.cState.slidingLeft,"exiting one slope retains other slope");b.OnCollisionExit2D(d);Check(!h.cState.slidingLeft,"last slope exit releases flag");
   var secondBody=h.gameObject.Add<Collider2D>();a.OnCollisionEnter2D(c);var multi=Collision(h,a,secondBody);a.OnCollisionEnter2D(multi);a.OnCollisionExit2D(c);Check(h.cState.slidingLeft,"multiple body colliders retain slope");a.OnCollisionExit2D(multi);Check(!h.cState.slidingLeft,"last body collider releases slope");
   var right=Slope(h,false,true);a.OnCollisionEnter2D(c);right.OnCollisionEnter2D(Collision(h,right));a.OnCollisionExit2D(c);Check(!h.cState.slidingLeft&&h.cState.slidingRight,"opposite slope flags independent");right.OnCollisionExit2D(Collision(h,right));Check(!h.cState.slidingRight,"right slope released");
   a.OnCollisionEnter2D(c);a.enabled=false;SteepSlopeRouting.Tick();Check(!h.cState.slidingLeft,"disabled slope missing exit pruned");a.enabled=true;a.OnCollisionEnter2D(c);p.Down=true;SteepSlopeRouting.Tick();Check(!h.cState.slidingLeft,"death releases slope flags");p.Down=false;
   h.cState.slidingLeft=true;a.OnCollisionEnter2D(c);a.OnCollisionExit2D(c);Check(h.cState.slidingLeft,"preexisting slide flag preserved");h.cState.slidingLeft=false;a.OnCollisionEnter2D(c);SteepSlopeRouting.Reset();Check(!h.cState.slidingLeft,"scene reset releases own slope flag");scenarios++;
  }
 }
 static void Gates(){
  foreach(int count in new[]{2,4,5,8})for(int actor=0;actor<count;actor++){
   var s=Party(count);var p=s.Players[actor];var h=p.Hero;var box=Box(h);var gate=Gate();h.cState.dashing=true;gate.FixedUpdate();Check(gate.disableCollider.enabled,"gate stays globally solid");Check(Physics2D.GetIgnoreCollision(gate.disableCollider,h.GetComponent<Collider2D>())&&Physics2D.GetIgnoreCollision(gate.disableCollider,box),"gate ignores actual dashing body and HeroBox");
   foreach(var q in s.Players.Where(q=>q!=p))Check(!Physics2D.GetIgnoreCollision(gate.disableCollider,q.Hero.GetComponent<Collider2D>()),"non-dashing teammate remains blocked");
   h.cState.dashing=false;gate.FixedUpdate();Check(!Physics2D.GetIgnoreCollision(gate.disableCollider,box),"end dash restores collision");
   var before=h.GetComponent<Collider2D>();Physics2D.IgnoreCollision(gate.disableCollider,before,true);h.cState.dashing=true;gate.FixedUpdate();h.cState.dashing=false;gate.FixedUpdate();Check(Physics2D.GetIgnoreCollision(gate.disableCollider,before),"preexisting collision ignore preserved");Physics2D.IgnoreCollision(gate.disableCollider,before,false);
   h.cState.dashing=true;gate.FixedUpdate();p.Down=true;ShadowGateRouting.Tick();Check(!Physics2D.GetIgnoreCollision(gate.disableCollider,box),"death releases gate collision");p.Down=false;gate.FixedUpdate();gate.enabled=false;ShadowGateRouting.Tick();Check(!Physics2D.GetIgnoreCollision(gate.disableCollider,box),"disabled gate releases pair");gate.enabled=true;gate.FixedUpdate();gate.OnDestroy();Check(gate.Destroyed==1&&!Physics2D.GetIgnoreCollision(gate.disableCollider,box),"destroy releases pair and invokes original");
   gate=Gate();gate.unlocked=false;gate.FixedUpdate();Check(!Physics2D.GetIgnoreCollision(gate.disableCollider,box),"locked gate grants no dash exemption");
   gate.unlocked=true;gate.FixedUpdate();WorldContactRouting.Reset();Check(!Physics2D.GetIgnoreCollision(gate.disableCollider,box),"scene reset releases gate pair");scenarios++;
  }
  var party=Party(2);var g=Gate();party.Players[0].Hero.cState.dashing=true;g.disableCollider.enabled=false;g.FixedUpdate();Check(g.disableCollider.enabled&&!Physics2D.GetIgnoreCollision(g.disableCollider,party.Players[1].Hero.GetComponent<Collider2D>()),"joining during P1 dash closes shared vanilla opening");
 }
 static void Sensors(){
  foreach(int count in new[]{2,4,5,8}){
   var s=Party(count);foreach(var p in s.Players)p.Hero.transform.position=new(0,0,0);
   var alert=Sensor<AlertRange>(new());var wake=Sensor<EnemyWakeRegion>(new());
   foreach(var p in s.Players){alert.OnTriggerEnter2D(p.Hero.GetComponent<Collider2D>());wake.OnTriggerEnter2D(p.Hero.GetComponent<Collider2D>());}Check(alert.IsHeroInRange&&wake.fsm.LastEvent=="WAKE","all occupants enter");
   foreach(var p in s.Players.Take(count-1)){p.Hero.transform.position=new(100,0,0);alert.OnTriggerExit2D(p.Hero.GetComponent<Collider2D>());wake.OnTriggerExit2D(p.Hero.GetComponent<Collider2D>());Check(alert.IsHeroInRange&&wake.fsm.LastEvent=="WAKE","one exit cannot forget remaining occupants");}
   var last=s.Players.Last();last.Hero.transform.position=new(100,0,0);alert.OnTriggerExit2D(last.Hero.GetComponent<Collider2D>());wake.OnTriggerExit2D(last.Hero.GetComponent<Collider2D>());Check(!alert.IsHeroInRange&&wake.fsm.LastEvent=="SLEEP","last exit clears alert and sends sleep");
   last.Hero.transform.position=new();alert.OnTriggerEnter2D(last.Hero.GetComponent<Collider2D>());wake.OnTriggerEnter2D(last.Hero.GetComponent<Collider2D>());last.Down=true;FreshFrame();PartySensorOccupancy.Tick();Check(!alert.IsHeroInRange&&wake.fsm.LastEvent=="SLEEP","death without exit clears alert and sleeps once");int events=wake.fsm.Events.Count;FreshFrame();PartySensorOccupancy.Tick();Check(wake.fsm.Events.Count==events,"cleanup does not spam sleep");last.Down=false;
   FreshFrame();PartySensorOccupancy.Tick();Check(alert.IsHeroInRange&&wake.fsm.LastEvent=="WAKE","reviving in place restores detection without a new Enter");
   var sensor=alert.gameObject.Add<Collider2D>();sensor.offset=new(1,0);alert.colliders=alert.GetComponents<Collider2D>();PartySensorOccupancy.Reset();alert.OnTriggerEnter2D(last.Hero.GetComponent<Collider2D>());alert.OnTriggerExit2D(last.Hero.GetComponent<Collider2D>());Check(alert.IsHeroInRange,"composite sensor retains body inside another shape");last.Hero.transform.position=new(100,0,0);alert.OnTriggerExit2D(last.Hero.GetComponent<Collider2D>());Check(!alert.IsHeroInRange,"composite sensor final exit clears");scenarios++;
  }
 }
 static void Walkers(){
  foreach(int count in new[]{2,4,5,8})for(int actor=0;actor<count;actor++){
   var s=Party(count);foreach(var q in s.Players)q.Hero.transform.position=new(-30-q.Index*5,0,0);var p=s.Players[actor];p.Hero.transform.position=new(0,0,0);
   var go=new GameObject();var walker=go.Add<Walker>();walker.waitHeroX=0;walker.Start();Check(walker.didFulfilHeroXCondition&&walker.Begins==1,"any eligible hero activates native wait threshold");Check(walker.hero==s.Players[0].Hero,"Walker cached field restored after callback");
   walker.lineOfSightDetector=go.Add<LineOfSightDetector>();walker.alertRange=Sensor<AlertRange>(new(),100);walker.alertRange.OnTriggerEnter2D(p.Hero.GetComponent<Collider2D>());walker.currentFacing=-1;p.Hero.transform.position=new(3,0,0);FreshFrame();walker.UpdateWalking();Check(Physics2D.LastRayHero==p.Hero&&walker.Turns==1&&walker.LastFacing==1,"Walker facing and native sight query use same guest");Check(HeroController.instance==s.Players[0].Hero,"Walker restores singleton");
   walker.Turns=0;var old=p.Hero;foreach(var q in s.Players)q.Down=true;FreshFrame();walker.UpdateWalking();Check(walker.Turns==0&&!walker.lineOfSightDetector.CanSeeHero,"no eligible target does not chase dead P1");foreach(var q in s.Players)q.Down=false;
   // Force a target change: P1 on the left is visible; guest on right is blocked.
   p.Hero.transform.position=new(100,0,0);var other=s.Players.First(q=>q!=p);other.Hero.transform.position=new(-2,0,0);FreshFrame();walker.UpdateWalking();p.Hero.transform.position=new(1,0,0);other.Hero.transform.position=new(-20,0,0);Physics2D.Obstructed=h=>h==p.Hero;FreshFrame();walker.Turns=0;walker.UpdateWalking();Check(Physics2D.LastRayHero==p.Hero&&!walker.lineOfSightDetector.CanSeeHero&&walker.Turns==0,"new target behind wall cannot reuse previous target visibility");scenarios++;
  }
 }
 static void Conveyors(){
  foreach(int count in new[]{2,4,5,8})for(int actor=0;actor<count;actor++){
   var s=Party(count);var p=s.Players[actor];var h=p.Hero;var conveyor=h.GetComponent<ConveyorMovementHero>();conveyor.Start();h.cState.wallSliding=true;h.cState.onConveyorV=true;conveyor.StartConveyorMove(0,5);conveyor.LateUpdate();var rb=h.GetComponent<Rigidbody2D>();Check(rb.gravityScale==0&&conveyor.gravityOff,"native conveyor owns zero gravity");
   PhysicsGuard.Tick(s);FreshFrame(3);PhysicsGuard.Tick(s);Check(rb.gravityScale==0&&conveyor.gravityOff&&Diagnostics.Messages.Count==0,"guard respects long vertical conveyor hold");
   h.cState.wallSliding=false;conveyor.StopConveyorMove();Check(rb.gravityScale>0&&!conveyor.gravityOff,"native conveyor restores gravity on exit");
   rb.gravityScale=0;h.cState.onConveyorV=true;PhysicsGuard.Reset();PhysicsGuard.Tick(s);FreshFrame(2);PhysicsGuard.Tick(s);Check(rb.gravityScale>0&&Diagnostics.Messages.Any(x=>x.Contains("PHYSICS restored")),"stale conveyor flag does not hide real stuck body");
   rb.gravityScale=0;h.cState.dashing=true;PhysicsGuard.Reset();PhysicsGuard.Tick(s);FreshFrame(3);PhysicsGuard.Tick(s);Check(rb.gravityScale==0,"ordinary dash remains excluded from recovery");h.cState.dashing=false;
   var zone=new GameObject().Add<ConveyorZone>();zone.OnTriggerEnter2D(h.GetComponent<Collider2D>());Check(h.ConveyorSpeed==5&&h.cState.inConveyorZone,"native horizontal conveyor still targets contacting hero");zone.OnTriggerExit2D(h.GetComponent<Collider2D>());Check(!h.cState.inConveyorZone,"native horizontal conveyor exit preserved");scenarios++;
  }
 }
 static void EdgeCases(){
  // Cached ownership survives a respawn in the real session resolver. Old
  // colliders must never borrow the replacement hero's state or recoil it.
  var s=Party(2);s.Players[0].Hero.transform.position=new(-100,0,0);var p=s.Players[1];var old=p.Hero;var slope=Slope(old);var contact=Collision(old,slope);slope.OnCollisionEnter2D(contact);
  var alert=Sensor<AlertRange>(old.transform.position);var wake=Sensor<EnemyWakeRegion>(old.transform.position);alert.OnTriggerEnter2D(old.GetComponent<Collider2D>());wake.OnTriggerEnter2D(old.GetComponent<Collider2D>());
  var oldAttack=Nail(old,0);var oldBox=Box(old);old.gameObject.Attributed=p;var replacement=new GameObject();p.Hero=replacement.Add<HeroController>();replacement.Add<Rigidbody2D>();replacement.Add<Collider2D>();p.Hero.cState.superDashing=true;
  new GameObject().Add<TinkEffect>().OnTriggerEnter2D(oldAttack);Check(Recoils(p.Hero)==0&&Recoils(s.Players[0].Hero)==0,"old nail cannot recoil replacement or primary");Check(!GrassCut.ShouldCut(oldBox),"old HeroBox cannot borrow replacement superdash");
  p.Hero.cState.slidingLeft=true;slope.OnCollisionExit2D(contact);Check(!old.cState.slidingLeft&&p.Hero.cState.slidingLeft,"old slope exit releases old body only");FreshFrame();PartySensorOccupancy.Tick();Check(!alert.IsHeroInRange&&wake.fsm.LastEvent=="SLEEP","orphan body is not retained as foreign sensor occupant");
  // Reset must retain P1 inside, but drop guests after session deactivation.
  s=Party(2);s.Players[0].Hero.transform.position=new();s.Players[1].Hero.transform.position=new(100,0,0);alert=Sensor<AlertRange>(new());alert.OnTriggerEnter2D(s.Players[0].Hero.GetComponent<Collider2D>());s.Active=false;PartySensorOccupancy.Reset();Check(alert.IsHeroInRange,"reset retains native P1 occupancy even with inactive session");
  s=Party(2);s.Players[0].Hero.transform.position=new(100,0,0);s.Players[1].Hero.transform.position=new();alert=Sensor<AlertRange>(new());alert.OnTriggerEnter2D(s.Players[1].Hero.GetComponent<Collider2D>());s.Active=false;PartySensorOccupancy.Reset();Check(!alert.IsHeroInRange,"reset drops guest-only occupancy for vanilla");
  // A wake event can immediately clear all scene caches; no mutation during
  // dictionary enumeration and no repeated SLEEP events may leak through.
  s=Party(2);s.Players[0].Hero.transform.position=new(100,0,0);p=s.Players[1];p.Hero.transform.position=new();wake=Sensor<EnemyWakeRegion>(new());wake.OnTriggerEnter2D(p.Hero.GetComponent<Collider2D>());wake.fsm.OnEvent=()=>WorldContactRouting.Reset();p.Down=true;FreshFrame();PartySensorOccupancy.Tick();Check(wake.fsm.LastEvent=="SLEEP","scene-resetting native wake event is safe");
  // Disabled gate cleanup must not reopen a gate normalized at coop takeover.
  s=Party(2);var gate=Gate();s.Players[0].Hero.cState.dashing=true;gate.disableCollider.enabled=false;gate.FixedUpdate();gate.enabled=false;ShadowGateRouting.Tick();s.Players[0].Hero.cState.dashing=false;gate.enabled=true;gate.FixedUpdate();Check(gate.disableCollider.enabled,"re-enabled gate remains solid after takeover during dash");
  gate=Gate();gate.disableCollider.enabled=false;gate.FixedUpdate();Check(!gate.disableCollider.enabled,"unrelated pre-disabled gate preserved");
  // A foreign inner hook changing the cached hero must survive our finally.
  WorldContactRouting.Uninstall();On.SteepSlope.hook_OnCollisionStay2D foreign=(orig,self,c)=>{orig(self,c);self.hc=s.Players[0].Hero;};On.SteepSlope.OnCollisionStay2D+=foreign;WorldContactRouting.Install();p=s.Players[1];slope=Slope(p.Hero);slope.hc=p.Hero;slope.OnCollisionStay2D(Collision(p.Hero,slope));Check(slope.hc==s.Players[0].Hero,"foreign same-call cache assignment preserved");WorldContactRouting.Uninstall();On.SteepSlope.OnCollisionStay2D-=foreign;
  On.SteepSlope.hook_OnCollisionStay2D throwing=(orig,self,c)=>{orig(self,c);throw new InvalidOperationException("slope native extension");};On.SteepSlope.OnCollisionStay2D+=throwing;WorldContactRouting.Install();slope=Slope(p.Hero);var prior=slope.hc;try{slope.OnCollisionStay2D(Collision(p.Hero,slope));throw new Exception("expected slope failure");}catch(InvalidOperationException){}Check(slope.hc==prior&&PlayerContext.Current==null,"throwing inner slope hook restores cache and context");WorldContactRouting.Uninstall();On.SteepSlope.OnCollisionStay2D-=throwing;WorldContactRouting.Install();
  // Empty recorded ranges do not perform party-wide geometry every update.
  s=Party(8);for(int i=0;i<50;i++){alert=Sensor<AlertRange>(new(1000+i*20,0,0));alert.OnTriggerEnter2D(s.Players[0].Hero.GetComponent<Collider2D>());}Physics2D.DistanceCalls=0;for(int i=0;i<1000;i++){FreshFrame();PartySensorOccupancy.Tick();}Check(Physics2D.DistanceCalls==0,"empty sensor steady state has no collider distance queries");
  // Losing a target and reacquiring nearly equal ones must not flip every frame.
  s=Party(2);var go=new GameObject();var walker=go.Add<Walker>();walker.hero=s.Players[0].Hero;walker.lineOfSightDetector=go.Add<LineOfSightDetector>();walker.alertRange=Sensor<AlertRange>(new(),100);s.Players[0].Hero.transform.position=new(-3,0,0);s.Players[1].Hero.transform.position=new(3.1f,0,0);walker.alertRange.OnTriggerEnter2D(s.Players[0].Hero.GetComponent<Collider2D>());FreshFrame();walker.UpdateWalking();Check(Physics2D.LastRayHero==s.Players[0].Hero,"nearest walking target selected");s.Players[1].Hero.transform.position=new(2.9f,0,0);FreshFrame();walker.lineOfSightDetector.Update();Check(Physics2D.LastRayHero==s.Players[0].Hero,"near equal distance keeps target");s.Players[1].Hero.transform.position=new(1,0,0);FreshFrame();walker.lineOfSightDetector.Update();Check(Physics2D.LastRayHero==s.Players[1].Hero,"substantially closer hero may become target");
  // Non-hero contacts use exactly the native entry points.
  var nail=new GameObject{tag="Nail Attack"};nail.Add<PlayMakerFSM>();new GameObject().Add<TinkEffect>().OnTriggerEnter2D(nail.Add<Collider2D>());Check(Recoils(s.Players[0].Hero)==1,"unowned foreign nail retains native behavior");
  // Simultaneous contacts keep the other actor's collision/slide lease alive.
  s=Party(2);foreach(var q in s.Players)q.Hero.transform.position=new();slope=Slope(s.Players[0].Hero);var one=Collision(s.Players[0].Hero,slope);var two=Collision(s.Players[1].Hero,slope);slope.OnCollisionEnter2D(one);slope.OnCollisionEnter2D(two);slope.OnCollisionExit2D(one);Check(!s.Players[0].Hero.cState.slidingLeft&&s.Players[1].Hero.cState.slidingLeft,"shared slope keeps each actor separate");gate=Gate();foreach(var q in s.Players)q.Hero.cState.dashing=true;gate.FixedUpdate();s.Players[0].Hero.cState.dashing=false;gate.FixedUpdate();Check(!Physics2D.GetIgnoreCollision(gate.disableCollider,s.Players[0].Hero.GetComponent<Collider2D>())&&Physics2D.GetIgnoreCollision(gate.disableCollider,s.Players[1].Hero.GetComponent<Collider2D>()),"simultaneous gate dash leases end independently");
  s=Party(2);s.Players[0].Hero.transform.position=new(100,0,0);p=s.Players[1];p.Hero.transform.position=new();alert=Sensor<AlertRange>(new());var extra=Box(p.Hero);alert.OnTriggerEnter2D(p.Hero.GetComponent<Collider2D>());alert.OnTriggerEnter2D(extra);extra.transform.position=new(100,0,0);alert.OnTriggerExit2D(extra);Check(alert.IsHeroInRange,"one body collider exit retains another collider of same hero");p.Hero.transform.position=new(100,0,0);alert.OnTriggerExit2D(p.Hero.GetComponent<Collider2D>());Check(!alert.IsHeroInRange,"last body collider exit clears occupancy");
  // A wake script on a rigidbody receives its child trigger's contacts too.
  s=Party(2);s.Players[0].Hero.transform.position=new(100,0,0);p=s.Players[1];p.Hero.transform.position=new(6,0,0);var root=new GameObject();root.Add<Rigidbody2D>();root.Add<Collider2D>();wake=root.Add<EnemyWakeRegion>();var child=new GameObject();child.Parent(root);child.Add<Collider2D>().Size=new(20,20,1);wake.OnTriggerEnter2D(p.Hero.GetComponent<Collider2D>());Check(wake.fsm.LastEvent=="WAKE","wake sensor includes trigger attached through child transform");
  scenarios+=15;
 }
 static void Lifecycle(){
  var s=Party(2);s.Active=false;var guest=s.Players[1].Hero;guest.cState.superDashing=true;Check(!GrassCut.ShouldCut(Box(guest)),"inactive session preserves native singleton behavior");
  WorldContactRouting.Uninstall();WorldContactRouting.Uninstall();Check(On.TinkEffect.Count_OnTriggerEnter2D==0&&On.ShadowGateColliderControl.Count_FixedUpdate==0&&On.AlertRange.Count_OnTriggerExit2D==0&&On.Walker.Count_UpdateWalking==0,"all hooks removed idempotently");WorldContactRouting.Install();Check(On.TinkEffect.Count_OnTriggerEnter2D==1,"reinstall balanced");WorldContactRouting.Uninstall();
 }
 static void Main(){Baseline();Contacts();Slopes();Gates();Sensors();Walkers();Conveyors();EdgeCases();Lifecycle();Console.WriteLine($"PASS world_interactions checks={checks}; corrected scenarios={scenarios}; exact native IL with production hooks; Unity dependencies simulated.");}
}
