using System;
using System.Linq;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using KO.HollowKnight8;

static partial class Test
{
    // The four published HK Blocker Control graphs use these same decisions:
    // Dormant: outer alert -> ALERT; Idle: attack -> CLOSE, unalert -> UNALERT;
    // Closed: !attack -> FAR. Animation, attacks and event dispatch are IO.
    static Collider2D Shape(Fsm f, string name, float width, float z, bool alert)
    {
        var go = new GameObject { name = name };
        go.transform.position = new(0, 0, z);
        f.GameObject.transform.Children[name] = go.transform;
        var shape = go.Add<Collider2D>();
        shape.Size = new(width, 12, 0);
        if (alert) go.Add<AlertRange>();
        return shape;
    }

    static BoolTest Decision(Fsm f, string name, string yes, string no, bool repeat = true)
    {
        var action = new BoolTest {
            Fsm = f, boolVariable = new() { Name = name, UseVariable = true },
            isTrue = yes == null ? null : new(yes), isFalse = no == null ? null : new(no), everyFrame = repeat
        };
        f.ActiveState.Actions = new FsmStateAction[] { action };
        return action;
    }

    static void Sense(Fsm f, string child, FsmBool value)
    {
        new CheckAlertRangeByName { Fsm = f, childName = child, storeResult = value }.Run();
    }

    static void NativeBody()
    {
        var party=Party(2);party.Players[0].Hero.transform.position=new(30,0);
        var hero=party.Players[1].Hero;hero.transform.position=new(7,0);
        var child=new GameObject {name="HeroBox"};child.transform.position=new(3.8f,0,-.03f);
        child.Add<Collider2D>();child.Add<HeroBox>();hero.transform.Children["HeroBox"]=child.transform;
        var f=Blocker();var range=Range(f);range.enabled=false;
        Occupancy(f,range,true);
        child.transform.position=new(100,0);Occupancy(f,range,false);
        var nail=new GameObject();nail.transform.position=new(1,0);nail.Add<Collider2D>();hero.transform.Children["Nail"]=nail.transform;
        ElderBaldurRange.Reset();Occupancy(f,range,false);
    }

    static void Encounter()
    {
        foreach (int count in new[] { 2, 4, 8 })
        for (int last = 0; last < count; last++)
        {
            var party = Party(count);
            var f = Blocker();
            var alert = Shape(f, "Alert Range New", 80, 2, true);
            var attack = Shape(f, "Attack Range", 8, -3, true);
            var unalert = Shape(f, "Unalert Range", 100, 4, false);
            var survivor = party.Players[last];
            foreach (var p in party.Players) p.Hero.transform.position = new(20, 0, -.01f * (p.Index + 1));

            f.ActiveState.Name = "Dormant";
            var wake = Decision(f, "In Alert Range", "ALERT", null);
            // Exact native Bounds is 3D; exact Physics2D semantics ignore Z.
            Check(!alert.bounds.Intersects(survivor.Hero.GetComponent<Collider2D>().bounds)
                && alert.Distance(survivor.Hero.GetComponent<Collider2D>()).isOverlapped,
                "negative control lost native 3D bounds / 2D contact distinction");
            Sense(f, "Alert Range New", wake.boolVariable);
            wake.Run();
            Check(wake.Selected?.Name == "ALERT", "dormant Baldur never wakes across render depths");

            f.ActiveState.Name = "Idle";
            var sleep = Decision(f, "Unalert Range", null, "UNALERT", false);
            sleep.boolVariable.Value = false; // Separate detector's exiting player.
            sleep.Native_OnEnter();
            Check(sleep.Selected?.Name == "UNALERT" && sleep.Finished,
                "native negative control did not reproduce premature sleeping");
            sleep.Run();
            Check(sleep.Selected == null && sleep.boolVariable.Value,
                "one exiting actor made Baldur sleep with another in unalert range");

            var close = Decision(f, "In Attack Range", "CLOSE", null);
            Sense(f, "Attack Range", close.boolVariable); close.Run();
            Check(close.Selected == null, "outer alert range was mistaken for close-defense range");
            survivor.Hero.transform.position = new(1, 0, -.05f);
            close.Update();
            Check(close.Selected?.Name == "CLOSE", "nearby hero did not trigger native defense");

            f.ActiveState.Name = "Closed";
            var far = Decision(f, "In Attack Range", null, "FAR");
            foreach (var p in party.Players.Where(p => p != survivor))
            {
                p.Hero.transform.position = new(30, 0, p.Index + 1);
                far.boolVariable.Value = false;
                Check(ElderBaldurRange.BlockFar(f, Far), "raw bool FAR ignored remaining close actor");
                far.Update();
                Check(far.Selected == null && far.boolVariable.Value,
                    "native bool opened armor while someone remained close");
            }
            survivor.Hero.transform.position = new(25, 0, -.05f);
            far.boolVariable.Value = true; far.Update();
            Check(far.Selected?.Name == "FAR" && !ElderBaldurRange.BlockFar(f, Far),
                "all-far native bool cannot open the Closed state");

            f.ActiveState.Name = "Idle";
            sleep = Decision(f, "Unalert Range", null, "UNALERT", false); sleep.Run();
            Check(sleep.Selected == null, "all outside defense but inside unalert cannot stay awake to attack");
            foreach (var p in party.Players) p.Hero.transform.position = new(150, 0, p.Index);
            sleep.boolVariable.Value = true; sleep.Run();
            Check(sleep.Selected?.Name == "UNALERT", "all outside unalert cannot return to sleep");
            f.ActiveState.Name = "Dormant";
            wake = Decision(f, "In Alert Range", "ALERT", null); wake.Run();
            Check(wake.Selected == null, "distant party woke a dormant enemy");
            survivor.Hero.transform.position = new(20, 0, -.03f); wake.Update();
            Check(wake.Selected?.Name == "ALERT", "returning last hero cannot wake sleeping Baldur");

            f.Name = "Other Control";
            wake.boolVariable.Value = false; wake.Update();
            Check(wake.Selected == null, "Baldur rule altered another FSM");
            f.Name = "Blocker Control"; wake.boolVariable.UseVariable = false;
            wake.Update();
            Check(wake.Selected == null, "literal bool was replaced by a proximity variable");
            Check(attack.enabled && unalert.enabled, "range fix changes native colliders");
        }
        Console.WriteLine("Encounter decisions: exact native BoolTest entry/update IL, separate alert/attack/unalert geometry, stale child-exit bool negative control, 2/4/8 slots, wake/close/all-far/open/sleep/wake. Animation/event/Physics2D IO is controlled.");
    }
}
