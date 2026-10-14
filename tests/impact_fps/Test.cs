using System;
using System.Collections.Generic;
using System.Linq;
using GlobalEnums;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using KO.HollowKnight8;
using Fx=On.HutongGames.PlayMaker.Actions;

internal static class Test
{
    private static int checks;
    private static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    private static void Reset(int players=4)
    {
        DamageParticleBudget.Uninstall();RoarMotionRecovery.Uninstall();Alpha222Roar.Uninstall();PhysicsGuard.Reset();
        GameManager.instance=new();Plugin.Self=new(){Session=new()};
        for(int i=0;i<players;i++){
            var p=new PlayerSlot{Index=i};p.Hero.damageEffectFSM=new(){Fsm=new(){Name="Hero Damaged Effects",Player=p,GameObject=p.Hero.gameObject}};
            Plugin.Self.Session.Players.Add(p);
        }
        Time.time=Time.unscaledTime=100;Time.deltaTime=.1f;
        Controls.LastBlocked=false;Controls.Pumps=0;BossSceneController.IsBossScene=true;
        ScriptedParty.Active=CoopEnding.Active=Charms.NativeMenuOpen=Charms.Editing=TransitionVote.Pending=PvpMatch.BlocksInput=ArenaGather.TransferActive=InteractionRouter.Interaction=false;
        PickupCard.Owner=InteractionRouter.ActivePlayer=PlayerContext.Current=null;PlayerContext.TargetingEnemy=false;
        WorldRouting.Current=null;WorldRouting.warp=null;WorldRouting.warpAt=default;WorldRouting.warpTime=0;NativeDreamFx.Trails=0;
        RoleSystem.Joining=null;Application.isFocused=true;
        Diagnostics.Lines.Clear();
    }
    private static PlayerSlot P(int i=1)=>Plugin.Self.Session.Players[i];
    private static Fsm Hurt(PlayerSlot p)=>p.Hero.damageEffectFSM.Fsm;
    private static GameObject Emitter(out ParticleSystem ps){var go=new GameObject("Knight Black Hit Pt");ps=go.Add(new ParticleSystem());return go;}

    private static void ParticleFilters()
    {
        Reset(8);DamageParticleBudget.Install();DamageParticleBudget.Install();
        Check(Fx.FlingObjectsFromGlobalPool.Subscribers==1&&Fx.PlayParticleEmitter.Subscribers==1,"idempotent FX hooks");
        var blood=new GameObject("Knight Blood");
        // Every participant must obey either option, including P1 and P8.
        for(int mask=0;mask<4;mask++)for(int i=0;i<8;i++){
            Plugin.Self.ReduceHitEffects.Value=(mask&1)!=0;Plugin.Self.PerformanceMode.Value=(mask&2)!=0;
            var p=P(i);var a=new FlingObjectsFromGlobalPool{Fsm=Hurt(p),gameObject=new(){Value=blood}};
            Fx.FlingObjectsFromGlobalPool.Run(a);Check(a.NativeCalls==(mask==0?1:0),"hurt debris option/actor routing");Check(a.Finishes==1,"native state completion");
        }
        Plugin.Self.ReduceHitEffects.Value=true;Plugin.Self.PerformanceMode.Value=false;
        var v=new FlingObjectsFromGlobalPoolVel{Fsm=Hurt(P()),gameObject=new(){Value=blood}};Fx.FlingObjectsFromGlobalPoolVel.Run(v);
        Check(v.NativeCalls==0&&v.Finishes==1,"velocity burst removed");
        var t=new FlingObjectsFromGlobalPoolTime{Fsm=Hurt(P()),gameObject=new(){Value=blood}};
        for(int n=0;n<10;n++)Fx.FlingObjectsFromGlobalPoolTime.Run(t);
        Check(t.NativeCalls==0&&t.Finishes==0,"continuous native action must remain unfinished");
        Plugin.Self.ReduceHitEffects.Value=false;Fx.FlingObjectsFromGlobalPoolTime.Run(t);
        Check(t.NativeCalls==1&&t.Finishes==0,"continuous emission resumes when option is disabled");
        Plugin.Self.ReduceHitEffects.Value=true;
        var emitter=Emitter(out var ps);var play=new PlayParticleEmitter{Fsm=Hurt(P()),gameObject=new(){GameObject=emitter}};
        Fx.PlayParticleEmitter.Run(play);Check(play.NativeCalls==0&&play.Finishes==1&&ps.Emits==0&&ps.Stops==1,"PS bursts removed without recursive stopping");
        var inState=new PlayParticleEmitterInState{Fsm=Hurt(P()),gameObject=new(){GameObject=emitter}};
        Fx.PlayParticleEmitterInState.Run(inState);Check(inState.NativeCalls==0&&inState.Finishes==0,"state-bound emitter keeps its lifetime");
        var emission=new SetParticleEmission{Fsm=Hurt(P()),gameObject=new(){GameObject=emitter},emission=new(){Value=true}};
        Fx.SetParticleEmission.Run(emission);Check(emission.NativeCalls==0&&emission.Finishes==1,"ON suppressed");
        emission=new(){Fsm=Hurt(P()),gameObject=new(){GameObject=emitter},emission=new(){Value=false}};
        ps.Emission=true;Fx.SetParticleEmission.Run(emission);Check(emission.NativeCalls==1&&!ps.Emission,"native OFF action always executes");
        Plugin.Self.ReduceHitEffects.Value=false;Fx.PlayParticleEmitter.Run(play);Check(play.NativeCalls==1&&ps.Emits==20,"normal particles return without a saved prefab mutation");
        Plugin.Self.ReduceHitEffects.Value=true;
        // A black effect in a spell/charm/boss/roar FSM is not hurt decoration.
        foreach(string name in new[]{"Spell Control","Thorn Counter","Focus","Grimmchild","Roar Lock","Soul Tyrant Control","Hero Damaged Effects"}){
            var f=new Fsm{Name=name,Player=name=="Soul Tyrant Control"?null:P(),GameObject=new("Other")};
            var a=new FlingObjectsFromGlobalPool{Fsm=f,gameObject=new(){Value=blood}};Fx.FlingObjectsFromGlobalPool.Run(a);
            Check(a.NativeCalls==1&&a.Finishes==1,"foreign/same-named FSM must keep its spawns");
        }
        // The owner identity must remain live across clone replacement.
        var oldFsm=Hurt(P());P().Hero.damageEffectFSM=new(){Fsm=new(){Name="Hero Damaged Effects",Player=P()}};
        var old=new FlingObjectsFromGlobalPool{Fsm=oldFsm,gameObject=new(){Value=blood}};Fx.FlingObjectsFromGlobalPool.Run(old);Check(old.NativeCalls==1,"replaced FSM cannot inherit the filter");
        foreach(Component gameplay in new Component[]{new HeroController(),new HealthManager(),new DamageHero(),new DamageEnemies(),new KnightHatchling()}){
            var prefab=new GameObject("Black Charm Effect");var child=new GameObject("nested gameplay");child.Add(gameplay);prefab.Children.Add(child);
            var a=new FlingObjectsFromGlobalPool{Fsm=Hurt(P()),gameObject=new(){Value=prefab}};Fx.FlingObjectsFromGlobalPool.Run(a);Check(a.NativeCalls==1,"modded gameplay spawn cannot be filtered");
        }
        // Live component changes in a prefab must invalidate the decision.
        var mutable=new GameObject("Black Hit");var first=new FlingObjectsFromGlobalPool{Fsm=Hurt(P()),gameObject=new(){Value=mutable}};Fx.FlingObjectsFromGlobalPool.Run(first);Check(first.NativeCalls==0,"initial cosmetic burst");
        mutable.Add(new DamageEnemies());var second=new FlingObjectsFromGlobalPool{Fsm=Hurt(P()),gameObject=new(){Value=mutable}};Fx.FlingObjectsFromGlobalPool.Run(second);Check(second.NativeCalls==1,"live new offensive component preserved");
        var rootPlay=new PlayParticleEmitter{Fsm=Hurt(P()),gameObject=new(){GameObject=P().Hero.gameObject}};Fx.PlayParticleEmitter.Run(rootPlay);Check(rootPlay.NativeCalls==1,"hero root never cleared");
        var nullPrefab=new FlingObjectsFromGlobalPool{Fsm=Hurt(P())};Fx.FlingObjectsFromGlobalPool.Run(nullPrefab);Check(nullPrefab.NativeCalls==1,"null/malformed action retains native handling");
        Plugin.Self.Session.Active=false;var inactive=new FlingObjectsFromGlobalPool{Fsm=Hurt(P()),gameObject=new(){Value=blood}};Fx.FlingObjectsFromGlobalPool.Run(inactive);Check(inactive.NativeCalls==1,"inactive coop retains native FX");
        DamageParticleBudget.ResetWindow();Check(DamageParticleBudget.Report()=="hit_particle_bursts_skipped=0 hit_emitters_skipped=0","particle summary reset");
        DamageParticleBudget.Uninstall();Check(Fx.PlayParticleEmitter.Subscribers==0&&Fx.FlingObjectsFromGlobalPool.Subscribers==0,"FX hooks uninstalled");
    }

    private static void StartHurt(PlayerSlot p)
    {
        var h=p.Hero;h.prevGravityScale=h.GetComponent<Rigidbody2D>().gravityScale;h.GetComponent<Rigidbody2D>().gravityScale=0;
        h.GetComponent<Rigidbody2D>().velocity=new(-10,5);h.cState.recoiling=true;h.hero_state=ActorStates.no_input;
    }

    private static void IntroFrames()
    {
        Reset();var p=P();StartHurt(p);GameManager.instance.gameState=GameState.CUTSCENE;Plugin.Self.Session.Gameplay=false;
        for(int n=0;n<10;n++)if(LegacyHooks.Frame(p.Hero,out _))p.Hero.NativeUpdate();
        Check(p.Hero.GetComponent<Rigidbody2D>().gravityScale==0&&p.Hero.recoilTimer==0,"reproduce alpha221 weightless recoil during boss intro");
        for(int n=0;n<10;n++){bool run=Hooks.Frame(p.Hero,out _);Check(run&&Controls.LastBlocked,"boss intro advances native motion with input blocked");if(run)p.Hero.NativeUpdate();}
        Check(!p.Hero.cState.recoiling&&p.Hero.GetComponent<Rigidbody2D>().gravityScale==2.4f,"native recoil can finish during boss intro");
        Check(p.Hero.GetComponent<Rigidbody2D>().velocity.Equals(Vector2.zero),"recoil velocity expires natively");
        // Existing seat, travel, down, revival and item/cinematic holds still win.
        Action<PlayerSlot>[] holds={q=>q.Down=true,q=>q.Hazard=true,q=>q.Ready=false,q=>q.Retiring=true,q=>q.InputBlocked=true,q=>q.Reviving=true,q=>q.ArenaTransfer=true,q=>q.Scripted=true,q=>q.Challenge=true,q=>q.CustomBench=true,q=>q.Warp=true,q=>q.WakeUntil=110};
        foreach(var hold in holds){Reset();p=P();GameManager.instance.gameState=GameState.CUTSCENE;Plugin.Self.Session.Gameplay=false;hold(p);Check(!Hooks.Frame(p.Hero,out _),"existing movement suspension must beat intro routing");}
        foreach(var state in Enum.GetValues<GameState>())for(int boss=0;boss<2;boss++)for(int paused=0;paused<2;paused++)for(int loading=0;loading<2;loading++)for(int entered=0;entered<2;entered++){
            Reset();p=P();var gm=GameManager.instance;gm.gameState=state;gm.isPaused=paused!=0;gm.IsLoadingSceneTransition=loading!=0;gm.HasFinishedEnteringScene=entered!=0;BossSceneController.IsBossScene=boss!=0;Plugin.Self.Session.Gameplay=state==GameState.PLAYING;
            bool old=LegacyHooks.Frame(p.Hero,out _),now=Hooks.Frame(p.Hero,out _);
            bool intro=state==GameState.CUTSCENE&&boss!=0&&paused==0&&loading==0&&entered!=0;
            Check(now==(old||intro),"only native boss intro adds eligible frames");if(intro)Check(Controls.LastBlocked,"cinematic input remains blocked");
        }
        Reset();Plugin.Self.Session.Gameplay=false;Check(Hooks.Frame(P(0).Hero,out _)==LegacyHooks.Frame(P(0).Hero,out _),"primary cinematic routing unchanged");
    }

    private static void PulseCleanup()
    {
        Reset();var p=P();StartHurt(p);var pulse=p.Hero.gameObject.Add(new InvulnerablePulse());
        PulseUnderTest.Old(p,GameManager.instance);p.Hero.NativeUpdate();Check(p.Hero.GetComponent<Rigidbody2D>().gravityScale==0&&!p.Hero.cState.recoiling,"reproduce cleanup losing recoil's gravity restoration");
        Reset();p=P();StartHurt(p);pulse=p.Hero.gameObject.Add(new InvulnerablePulse());PulseUnderTest.New(p,GameManager.instance);
        Check(!pulse.pulsing&&p.Hero.cState.recoiling,"cleanup stops only invulnerability pulse");
        for(int n=0;n<10;n++)p.Hero.NativeUpdate();Check(p.Hero.GetComponent<Rigidbody2D>().gravityScale==2.4f&&!p.Hero.cState.recoiling,"cleanup keeps native gravity restoration");
        foreach(string hold in new[]{"frozen","roar","slowed","protection","invincible","shadow","transition","recent"}){
            Reset();p=P();pulse=p.Hero.gameObject.Add(new InvulnerablePulse());
            switch(hold){case "frozen":p.Hero.cState.recoilFrozen=true;break;case "roar":p.Hero.controlReqlinquished=true;break;case "slowed":GameManager.instance.TimeSlowed=true;break;case "protection":p.ProtectionUntil=150;break;case "invincible":p.Vitals.Invincible=true;break;case "shadow":p.Hero.cState.shadowDashing=true;break;case "transition":p.Hero.cState.transitioning=true;break;case "recent":p.LastDamageAt=99;break;}
            PulseUnderTest.New(p,GameManager.instance);Check(pulse.pulsing&&pulse.Stops==0,"live invulnerability cannot be prematurely removed: "+hold);
        }
    }

    private static void RoarRelease()
    {
        // Execute the delivered alpha222 implementation to reproduce both
        // the logged residual -5 push and writes AFTER its immediate repair.
        Reset();Alpha222Roar.Install();var actor=P();var f=Roar(actor);var body=actor.Hero.GetComponent<Rigidbody2D>();
        WorldRouting.Current=f;On.HeroController.Relinquish(actor.Hero);body.velocity=new(-5,0);
        On.HeroController.Regain(actor.Hero);Check(body.velocity.x==-5,"reproduce alpha222 roar exit retaining the logged push");
        body.gravityScale=0;Check(body.gravityScale==0,"alpha222 cannot repair a later native action after discarding its backup");

        Reset(8);RoarMotionRecovery.Install();RoarMotionRecovery.Install();Check(On.HeroController.Subscribers==2,"idempotent roar hooks");
        for(int i=0;i<8;i++){
            var p=P(i);var h=p.Hero;var b=h.GetComponent<Rigidbody2D>();b.gravityScale=1.3f+i*.4f;
            float normal=b.gravityScale;f=Roar(p);PlayerContext.Current=p;var enter=WorldRouting.Begin(f);On.HeroController.Relinquish(h);
            b.gravityScale=0;h.prevGravityScale=0;var at=h.transform.position;
            On.HeroController.Relinquish(h);WorldRouting.End(f,enter);
            float displacement=0;
            for(int step=0;step<150;step++){
                var fixedCall=WorldRouting.Begin(f);b.velocity=new(-5,step%2==0?2:-3);
                float vertical=b.velocity.y;WorldRouting.End(f,fixedCall);
                Check(b.velocity.x==(i==0?-5:0)&&b.velocity.y==vertical,"roar fixed action cannot drag guests; host and vertical motion preserved");
                displacement+=b.velocity.x*.02f;
            }
            Check(i==0?displacement<0:displacement==0,"guest drift must be stopped BEFORE physics integration");
            var exit=WorldRouting.Begin(f);On.HeroController.Regain(h);Check(b.gravityScale==0,"repair must wait for native actions after RegainControl");
            b.gravityScale=0;b.velocity=new(-5,4);WorldRouting.End(f,exit);
            Check(b.gravityScale==normal,"deferred release restores the actor's original gravity after all native writes");
            Check(h.transform.position.Equals(at)&&b.velocity.Equals(new Vector2(i==0?-5:0,4)),"release preserves position, vertical motion, and host velocity");
            Check(!h.controlReqlinquished&&h.acceptingInput&&!RoarMotionRecovery.Active,"native control release preserved and lease ends");
        }
        // A same-FSM callback can nest through a foreign FSM and return to
        // its caller, which still has native actions left to execute.
        Reset();RoarMotionRecovery.Install();actor=P();f=Roar(actor);PlayerContext.Current=actor;
        var outer=WorldRouting.Begin(f);On.HeroController.Relinquish(actor.Hero);
        var foreign=new Fsm{Name="Nested audio",Player=actor};var middle=WorldRouting.Begin(foreign);var inner=WorldRouting.Begin(f);
        body=actor.Hero.GetComponent<Rigidbody2D>();body.gravityScale=0;actor.Hero.prevGravityScale=0;On.HeroController.Regain(actor.Hero);
        WorldRouting.End(f,inner);Check(body.gravityScale==0&&RoarMotionRecovery.Active&&WorldRouting.Current==foreign,"nested release must not discard the outer action's backup");
        WorldRouting.End(foreign,middle);Check(WorldRouting.Current==f,"nested foreign callback restores the outer roar scope");
        body.gravityScale=0;body.velocity=new(-5,7);WorldRouting.End(f,outer);
        Check(body.gravityScale==2.4f&&body.velocity.Equals(new Vector2(0,7))&&!RoarMotionRecovery.Active&&WorldRouting.Current==null,"outermost callback cleans later native writes exactly once");

        Reset();RoarMotionRecovery.Install();actor=P();f=Roar(actor);PlayerContext.Current=actor;outer=WorldRouting.Begin(f);
        On.HeroController.Relinquish(actor.Hero);WorldRouting.End(f,outer);body=actor.Hero.GetComponent<Rigidbody2D>();
        actor.InputBlocked=true;Application.isFocused=false;body.gravityScale=.79f;body.velocity=new(-5,-2);
        int pumps=Controls.Pumps;RoarMotionRecovery.LateTick(Plugin.Self.Session);
        Check(body.velocity.Equals(new Vector2(0,-2))&&body.gravityScale==.79f&&actor.InputBlocked&&Controls.Pumps==pumps,"suspended guest frames still shed drift without enabling input or changing vertical physics");
        body.velocity=new(-5,-2);WorldRouting.Current=f;On.HeroController.Regain(actor.Hero);WorldRouting.Current=null;
        RoarMotionRecovery.LateTick(Plugin.Self.Session);Check(body.velocity.Equals(new Vector2(0,-2))&&!RoarMotionRecovery.Active,"late fallback handles a release outside a routed frame");

        Action<PlayerSlot>[] holds={q=>q.Hero.cState.recoilFrozen=true,q=>q.Hero.cState.recoiling=true,q=>q.Hero.cState.casting=true,q=>q.Hero.cState.dashing=true,q=>q.Hero.cState.shadowDashing=true,q=>q.Hero.cState.superDashing=true,q=>q.Hero.cState.freezeCharge=true,q=>q.Hero.cState.bouncing=true,q=>q.Hero.cState.doubleJumping=true,q=>q.Hero.cState.swimming=true,q=>q.Hero.cState.dead=true,q=>q.Hero.cState.transitioning=true,q=>q.Hero.inAcid=true,q=>q.Down=true,q=>q.Hazard=true,q=>q.Retiring=true,q=>q.Ready=false,q=>q.Alive=false,q=>q.ArenaTransfer=true,q=>q.SpawnPending=true,q=>q.Reviving=true,q=>q.WakeUntil=110,q=>q.Warp=true,q=>q.Bench=true,q=>q.Scripted=true,q=>q.Challenge=true,q=>q.AcidAssistActive=true,q=>q.Voting=true,q=>RoleSystem.Joining=q,q=>GameManager.instance.IsLoadingSceneTransition=true,q=>q.Hero.gameObject.activeInHierarchy=false};
        foreach(var hold in holds){
            Reset();RoarMotionRecovery.Install();var p=P();f=Roar(p);PlayerContext.Current=p;outer=WorldRouting.Begin(f);On.HeroController.Relinquish(p.Hero);
            body=p.Hero.GetComponent<Rigidbody2D>();body.gravityScale=0;p.Hero.prevGravityScale=0;hold(p);On.HeroController.Regain(p.Hero);
            body.gravityScale=0;body.velocity=new(-7,3);WorldRouting.End(f,outer);RoarMotionRecovery.LateTick(Plugin.Self.Session);
            Check(body.gravityScale==0&&body.velocity.Equals(new Vector2(-7,3))&&!RoarMotionRecovery.Active,"damage/spells/travel/role/death owners win over both gravity and drift cleanup");
        }

        // Root and live identity are required, independently of the name.
        foreach(string name in new[]{"NPC Dialogue","Spell Control","Roar Lock"}){
            Reset();RoarMotionRecovery.Install();actor=P();f=new(){Name=name,Player=actor,GameObject=new("Foreign boss")};WorldRouting.Current=f;
            On.HeroController.Relinquish(actor.Hero);body=actor.Hero.GetComponent<Rigidbody2D>();body.gravityScale=0;actor.Hero.prevGravityScale=0;
            On.HeroController.Regain(actor.Hero);body.velocity=new(-6,5);RoarMotionRecovery.AfterFsm(f,actor);
            Check(body.gravityScale==0&&body.velocity.Equals(new Vector2(-6,5))&&!RoarMotionRecovery.Active,"foreign and same-named control owners retain their behavior");
        }
        foreach(string replacement in new[]{"removed","slot","hero","body","session","disabled","kinematic","reset"}){
            Reset();RoarMotionRecovery.Install();actor=P();f=Roar(actor);WorldRouting.Current=f;On.HeroController.Relinquish(actor.Hero);
            body=actor.Hero.GetComponent<Rigidbody2D>();body.velocity=new(-5,0);
            switch(replacement){
                case "removed":Plugin.Self.Session.Players.Remove(actor);break;
                case "slot":Plugin.Self.Session.Players[1]=new(){Index=1};break;
                case "hero":actor.Hero=new();break;
                case "body":body.Destroyed=true;actor.Hero.gameObject.Add(new Rigidbody2D{isKinematic=true});break;
                case "session":Plugin.Self.Session=new();Plugin.Self.Session.Players.Add(actor);break;
                case "disabled":body.simulated=false;break;
                case "kinematic":body.isKinematic=true;break;
                case "reset":RoarMotionRecovery.Reset();break;
            }
            RoarMotionRecovery.LateTick(Plugin.Self.Session);
            Check(!RoarMotionRecovery.Active&&body.velocity.x==-5,"stale/disabled identity discarded without writing an old actor: "+replacement);
        }
        Reset();RoarMotionRecovery.Install();actor=P();f=Roar(actor);PlayerContext.Current=actor;outer=WorldRouting.Begin(f);
        actor.Hero.GetComponent<Rigidbody2D>().gravityScale=5.5f;On.HeroController.Relinquish(actor.Hero);RoarMotionRecovery.Reset();WorldRouting.End(f,outer);
        actor.Hero=new();f=Roar(actor);outer=WorldRouting.Begin(f);actor.Hero.controlReqlinquished=true;body=actor.Hero.GetComponent<Rigidbody2D>();
        body.gravityScale=0;actor.Hero.prevGravityScale=0;On.HeroController.Regain(actor.Hero);WorldRouting.End(f,outer);
        Check(body.gravityScale==actor.Hero.DEFAULT_GRAVITY&&!RoarMotionRecovery.Active,"new actor cannot inherit the old scene's backup or negative nesting depth");

        // The bounded FSM trace records parameters without running actions.
        Reset();RoarMotionRecovery.Install();actor=P();f=Roar(actor);
        var velocity=new SetVelocity2d{x=new(){Value=-5},y=new(){IsNone=true},everyFrame=true};var gravity=new SetGravity2dScale{gravityScale=new(){Value=0}};
        f.States=Enumerable.Range(0,40).Select(i=>new FsmState{Name="test"+i,Actions=new FsmStateAction[]{velocity,gravity,null}}).ToArray();
        WorldRouting.Current=f;On.HeroController.Relinquish(actor.Hero);
        var trace=Diagnostics.Lines.Where(x=>x.StartsWith("ROAR FSM state=")).ToArray();
        Check(trace.Length==24&&trace[0].Contains("x=-5,y=keep,fixed=True")&&trace[0].Contains("gravity=0"),"FSM trace has a strict state limit and useful native parameters");
        Check(velocity.NativeCalls==0&&velocity.Finishes==0&&gravity.NativeCalls==0,"diagnostics must not execute or finish native actions");
        On.HeroController.Relinquish(actor.Hero);Check(Diagnostics.Lines.Count(x=>x.StartsWith("ROAR FSM state="))==24,"FSM trace is emitted once per scene");
        RoarMotionRecovery.Uninstall();Check(On.HeroController.Subscribers==0&&!RoarMotionRecovery.Active,"native control hooks and pending leases removed");
        Reset();RoarMotionRecovery.Install();f=new(){Name="Idle world"};PlayerContext.Current=P();
        for(int n=0;n<5000;n++){outer=WorldRouting.Begin(f);WorldRouting.End(f,outer);RoarMotionRecovery.LateTick(Plugin.Self.Session);}
        long allocated=GC.GetAllocatedBytesForCurrentThread();
        for(int n=0;n<10000;n++){outer=WorldRouting.Begin(f);WorldRouting.End(f,outer);RoarMotionRecovery.LateTick(Plugin.Self.Session);}
        Check(GC.GetAllocatedBytesForCurrentThread()==allocated,"roar checks add no managed allocation to idle world callbacks after warmup");
        Console.WriteLine("Alpha222 residual roar push reproduced; alpha223 protects outermost actions, guest physics, and native damage/dash/spell owners.");
    }

    private static Fsm Roar(PlayerSlot p)=>new(){Name="Roar Lock",ActiveStateName="Roar",Player=p,GameObject=p.Hero.gameObject};

    private static void PhysicsWatchdog()
    {
        Reset();var p=P();var b=p.Hero.GetComponent<Rigidbody2D>();b.gravityScale=0;p.Hero.prevGravityScale=3.8f;PhysicsGuard.Tick(Plugin.Self.Session);Time.unscaledTime+=1.5f;PhysicsGuard.Tick(Plugin.Self.Session);Check(b.gravityScale==3.8f,"watchdog uses native saved gravity");
        Action<PlayerSlot>[] holds={q=>q.Hero.cState.casting=true,q=>q.Hero.cState.dashing=true,q=>q.Hero.cState.superDashing=true,q=>q.Hero.cState.freezeCharge=true,q=>q.Hero.cState.recoilFrozen=true,q=>q.Hero.cState.recoiling=true,q=>q.Hero.cState.bouncing=true,q=>q.Hero.cState.doubleJumping=true,q=>q.Hero.cState.swimming=true,q=>q.Hero.controlReqlinquished=true,q=>q.Down=true,q=>q.SpawnPending=true,q=>q.Bench=true};
        foreach(var hold in holds){Reset();p=P();b=p.Hero.GetComponent<Rigidbody2D>();b.gravityScale=0;hold(p);for(int n=0;n<60;n++){PhysicsGuard.Tick(Plugin.Self.Session);Time.unscaledTime+=.1f;}Check(b.gravityScale==0,"watchdog cannot interrupt a valid body owner");}
        foreach(string menu in new[]{"panel","paused","slowed","cutscene","charms","interaction"}){
            Reset();p=P();b=p.Hero.GetComponent<Rigidbody2D>();b.gravityScale=0;PhysicsGuard.Tick(Plugin.Self.Session);Time.unscaledTime+=1.3f;
            switch(menu){case "panel":Plugin.Self.Panel=true;break;case "paused":GameManager.instance.isPaused=true;break;case "slowed":GameManager.instance.TimeSlowed=true;break;case "cutscene":Plugin.Self.Session.Gameplay=false;break;case "charms":Charms.NativeMenuOpen=true;break;case "interaction":InteractionRouter.ActivePlayer=p;break;}
            PhysicsGuard.Tick(Plugin.Self.Session);Time.unscaledTime+=20;
            Plugin.Self.Panel=GameManager.instance.isPaused=GameManager.instance.TimeSlowed=Charms.NativeMenuOpen=false;Plugin.Self.Session.Gameplay=true;InteractionRouter.ActivePlayer=null;
            PhysicsGuard.Tick(Plugin.Self.Session);Check(b.gravityScale==0,"time in menus is not a stuck-body timeout");Time.unscaledTime+=1.5f;PhysicsGuard.Tick(Plugin.Self.Session);Check(b.gravityScale==2.4f,"fresh eligible timeout still recovers an invalid body");
        }
    }

    private readonly record struct WarpResult(PlayerSlot Player,Vector3 At,float When,int Trails,Fsm Current);
    private static WarpResult Warp(bool legacy,Fsm f,WorldRouting.Call call)
    {
        WorldRouting.warp=P(0);WorldRouting.warpAt=new(87,65,43);WorldRouting.warpTime=37;WorldRouting.Current=f;NativeDreamFx.Trails=0;
        if(legacy)WorldRouting.LegacyEnd(f,call);else WorldRouting.End(f,call);
        return new(WorldRouting.warp,WorldRouting.warpAt,WorldRouting.warpTime,NativeDreamFx.Trails,WorldRouting.Current);
    }
    private static void WarpEquivalence()
    {
        Reset();var random=new Random(73291);var s=Plugin.Self.Session;var gm=GameManager.instance;var p=P();var f=new Fsm{Name="World",Player=p};var outer=new Fsm{Name="outer"};
        for(int n=0;n<100000;n++){
            var h=p.Hero;var from=new Vector3((float)random.NextDouble()*100,(float)random.NextDouble()*150-90,0);var at=new Vector3(from.x+(float)random.NextDouble()*24-12,from.y+(float)random.NextDouble()*24-12,4);
            if(n%19==0)at=from;if(n%59==0)at.y=-100;if(n%61==0)at.x=from.x+6;if(n%71==0)at.x=float.NaN;if(n%73==0)at.x=float.PositiveInfinity;if(n%79==0)at.y=float.NegativeInfinity;
            h.transform.position=at;h.controlReqlinquished=random.Next(2)==0;s.Active=random.Next(20)!=0;p.Ready=random.Next(20)!=0;p.Alive=random.Next(20)!=0;gm.GameplayScene=random.Next(20)!=0;gm.IsLoadingSceneTransition=random.Next(20)==0;gm.HasFinishedEnteringScene=random.Next(20)!=0;
            p.ArenaTransfer=random.Next(20)==0;ArenaGather.TransferActive=random.Next(20)==0;p.Warp=random.Next(20)==0;CoopEnding.Active=random.Next(20)==0;ScriptedParty.Active=random.Next(20)==0;p.Bench=random.Next(20)==0;InteractionRouter.Interaction=random.Next(2)==0;s.InBounds=random.Next(2)==0;
            var call=new WorldRouting.Call{Player=n%89==0?null:p,Previous=outer,Position=from};
            var old=Warp(true,f,call);var now=Warp(false,f,call);Check(old.Equals(now),"live warp decision/feedback/scoped restoration differs at "+n);
        }
        Reset();s=Plugin.Self.Session;gm=GameManager.instance;p=P();f=new(){Name="No movement"};var call2=new WorldRouting.Call{Player=p,Position=p.Hero.transform.position};
        for(int n=0;n<20000;n++)WorldRouting.LegacyEnd(f,call2);int before=gm.WorldChecks;gm.WorldChecks=0;
        for(int n=0;n<20000;n++)WorldRouting.End(f,call2);int after=gm.WorldChecks;
        Check(before==20000&&after==0,"unchanged actors must avoid world/scene checks");
        var first=WorldRouting.Begin(f);var inner=new Fsm{Name="inner"};var second=WorldRouting.Begin(inner);WorldRouting.End(inner,second);Check(WorldRouting.Current==f,"nested unchanged callback restores outer scope");WorldRouting.End(f,first);Check(WorldRouting.Current==first.Previous,"outer scope restored");
        Console.WriteLine("World guards for 20,000 unmoved callbacks: alpha221="+before+", alpha222="+after+" (controlled fixture, not measured Unity FPS).");
    }
    internal static void Main()
    {
        ParticleFilters();IntroFrames();PulseCleanup();RoarRelease();PhysicsWatchdog();WarpEquivalence();
        Console.WriteLine("PASS "+checks+" production impact/motion/warp checks; native Unity integration still requires an in-game trial.");
    }
}
