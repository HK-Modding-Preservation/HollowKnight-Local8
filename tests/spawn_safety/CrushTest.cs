using System;
using System.Linq;
using UnityEngine;
using KO.HollowKnight8;

static partial class Test
{
    static VinePlatform Platform(float x=30,float y=8)
    {
        var go=new GameObject {layer=8,name="Vine Platform"};go.transform.position=new(x,y);
        var shape=go.Add<BoxCollider2D>();shape.size=new(4,1);
        var body=go.Add<Rigidbody2D>();body.isKinematic=true;
        var platform=go.Add<VinePlatform>();platform.collider=shape;platform.Body=body;
        go.Add<VinePlatformCut>().body=body;
        return platform;
    }
    static void Cut(VinePlatform platform)=>platform.GetComponent<VinePlatformCut>().Cut();
    static void CrushStep(CoopSession s,float seconds=.1f){Time.time+=seconds;Time.unscaledTime+=seconds;FallingPlatformHazard.Tick(s);}

    static void Crushing()
    {
        FallingPlatformHazard.Install();FallingPlatformHazard.Install();
        Check(On.VinePlatform.Count==1&&On.VinePlatformCut.Count==1,"duplicate falling-platform hooks");
        foreach(int count in new[]{2,4,8})for(int actor=0;actor<count;actor++)foreach(bool landFirst in new[]{false,true})
        {
            var s=Start();for(int i=1;i<count;i++)Guest(s,i,active:true).Hero.transform.position=new(10+i*2,3.02f);
            var p=s.Players[actor];p.Hero.transform.position=new(30,3.02f);p.Vitals.Blue=0;
            p.HasSafePoint=true;p.SafePoint=new(50,3.02f);
            var platform=Platform();Cut(platform);Cut(platform);platform.transform.position=new(30,3.7f);
            if(landFirst)platform.Land();else CrushStep(s);
            Check(p.Vitals.Health==4&&p.Hazard&&p.Hero.DamageCalls==1,"descending underside did not apply one native base-damage call to the correct actor");
            Check(!p.Hero.GetComponent<Rigidbody2D>().simulated,"head hit left the actor moving inside terrain");
            Check(s.Players.Where(q=>q!=p).All(q=>q.Hero.DamageCalls==0&&q.Vitals.Health==5),"platform damage affected another actor");
            int soul=p.Vitals.Soul,reserve=p.Vitals.Reserve;
            for(int i=0;i<10;i++)CrushStep(s);
            Check(s.Recoveries==1&&!p.Hazard&&s.IsSafe(p.Hero.transform.position,p),"crushed actor failed complete production recovery to a safe ally landing");
            Check(p.Hero.transform.position.x<25,"recovery ignored the living companions");
            Check(p.Vitals.Health==4&&p.Vitals.Soul==soul&&p.Vitals.Reserve==reserve,"non-respawn crush recovery restored health or reset resources");
            Check(p.Hero.DamageCalls==1&&p.ProtectionUntil>Time.time&&p.Hero.acceptingInput,"same falling object charged repeatedly or failed to restore protected controls");
            if(!landFirst)platform.Land();
            p.Hero.transform.position=new(30,3.02f);p.ProtectionUntil=0;CrushStep(s,3);
            Check(p.Hero.DamageCalls==1,"stationary landed platform damaged a passing player");
        }
        foreach(string mode in new[]{"on-top","side","uncut","rising","stationary"})
        {
            var s=Start();Guest(s,active:true).Hero.transform.position=new(10,3.02f);
            var p=s.Primary;p.Vitals.Blue=0;p.Hero.transform.position=new(mode=="side"?34:30,mode=="on-top"?10:3.02f);
            var platform=Platform();if(mode!="uncut")Cut(platform);
            if(mode!="stationary")platform.transform.position=new(30,mode=="rising"?9:3.7f);
            CrushStep(s);Check(p.Hero.DamageCalls==0&&!p.Hazard,"non-head contact became crushing damage: "+mode);
        }
        {
            var s=Start();Guest(s,active:true).Hero.transform.position=new(10,3.02f);var p=s.Primary;p.Hero.transform.position=new(30,3.02f);
            var platform=Platform();Cut(platform);platform.transform.position=new(30,.2f);CrushStep(s);
            Check(p.Vitals.Health==5&&p.Vitals.Blue==1,"fast swept hit ignored blue masks or missed the head");
            CrushStep(s,1);Check(!p.Hazard&&p.Hero.DamageCalls==1,"fast descent failed recovery or duplicated damage");
        }
        {
            var s=Start();Guest(s,active:true).Hero.transform.position=new(10,3.02f);var p=s.Primary;p.Vitals.Health=1;p.Vitals.Blue=0;p.Hero.transform.position=new(30,3.02f);
            var platform=Platform();Cut(platform);platform.transform.position=new(30,3.7f);CrushStep(s);CrushStep(s,1);
            Check(p.Down&&p.Vitals.Health==0&&s.Recoveries==0,"fatal crush revived the actor for free");
        }
        {
            var s=Start(false);var ally=Guest(s,active:true);ally.Hero.transform.position=new(10,3.02f);var p=s.Primary;p.Hero.transform.position=new(30,3.02f);p.Vitals.Blue=0;
            var platform=Platform();Cut(platform);platform.transform.position=new(30,3.7f);CrushStep(s);platform.Land();
            for(int i=0;i<10;i++)CrushStep(s,.5f);
            Check(p.Hazard&&p.Vitals.Health==4&&p.Hero.DamageCalls==1&&s.Recoveries==0&&p.HazardUntil>Time.time,"unsafe ally landing teleported into air or repeatedly charged damage");
            Terrain(0,0,20,2);CrushStep(s,.5f);
            Check(!p.Hazard&&s.Recoveries==1&&s.IsSafe(p.Hero.transform.position,p),"pending crush recovery failed to retry after safe ground appeared");
        }
        {
            var s=Start();s.Primary.Down=true;var p=Guest(s,active:true);p.Hero.transform.position=new(30,3.02f);p.Vitals.Blue=0;p.HasSafePoint=true;p.SafePoint=new(50,3.02f);
            var platform=Platform();Cut(platform);platform.transform.position=new(30,3.7f);CrushStep(s);CrushStep(s,1);
            Check(!p.Hazard&&p.Hero.transform.position==new Vector3(50,3.02f)&&s.Primary.Down,"no living companion fallback was unsafe or revived a teammate");
        }
        foreach(string guard in new[]{"pause","story","ending","transfer","wipe","bench","vote","warp","blocked","unready","control","down","retired","spawn","protection"})
        {
            var s=Start();Guest(s,active:true).Hero.transform.position=new(10,3.02f);var p=s.Primary;p.Hero.transform.position=new(30,3.02f);var platform=Platform();Cut(platform);
            switch(guard){case "pause":GameManager.instance.isPaused=true;break;case "story":ScriptedParty.Active=true;break;case "ending":CoopEnding.Active=true;break;case "transfer":ArenaGather.TransferActive=true;break;case "wipe":s.TeamWipe=true;break;case "bench":BenchSeats.SeatedPlayers.Add(0);break;case "vote":TransitionVote.Waiting.Add(0);break;case "warp":EmergencyWarp.Warping.Add(0);break;case "blocked":p.InputBlocked=true;break;case "unready":p.Ready=false;break;case "control":p.Hero.controlReqlinquished=true;break;case "down":p.Down=true;break;case "retired":p.Retiring=true;break;case "spawn":p.SpawnPending=true;break;case "protection":p.ProtectionUntil=Time.time+3;break;}
            platform.transform.position=new(30,3.7f);CrushStep(s);Check(p.Hero.DamageCalls==0,"crushing ignored "+guard);
        }
        {
            var s=Start();var p=s.Primary;p.Hero.transform.position=new(30,3.02f);var platform=Platform();Cut(platform);
            p.Hero.OnDamage=()=>FallingPlatformHazard.Reset();platform.transform.position=new(30,3.7f);CrushStep(s);CrushStep(s,1);
            Check(!p.Hazard&&s.Recoveries==0,"synchronous scene reset retained a stale recovery");
        }
        FallingPlatformHazard.Uninstall();FallingPlatformHazard.Uninstall();
        Check(On.VinePlatform.Count==0&&On.VinePlatformCut.Count==0,"falling-platform hooks survived shutdown");
        Console.WriteLine("PASS falling-vine head sweep, one native base-damage call, P1-P8 ally placement, complete production Recover, blue/fatal/unsafe/retry/side/on-top/reset/guard cases. Native damage and cut/land callbacks are controlled IO, not a Unity playthrough.");
    }
}
