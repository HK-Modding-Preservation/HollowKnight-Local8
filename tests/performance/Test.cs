using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Test
{
    private static int checks;
    private sealed class Actor
    {
        public int Count = 4;
        private string Hidden = "native";
        private int Add(int n) => Count + n;
        public string ReadHidden() => Hidden;
    }
    private static void Check(bool ok, string reason) { checks++; if (!ok) throw new Exception(reason); }
    private static void Near(double a, double b, double tolerance, string reason) => Check(Math.Abs(a-b) <= tolerance, reason+": "+a+" vs "+b);
    private static PlayerSlot[] actors = Enumerable.Range(0,8).Select(i => new PlayerSlot { Index = i }).ToArray();

    private static void Reflection()
    {
        var actor = new Actor();
        Check(Reflect.Get<int>(actor, "Count") == 4, "field read");
        Reflect.Set(actor, "Count", 9);
        Check(Reflect.Get<int>(actor, "Count") == 9, "field write");
        Check(Reflect.Get<string>(actor, "Hidden") == actor.ReadHidden(), "private field");
        Check(Reflect.Get<int>(actor, "Missing", 77) == 77 && Reflect.Get<int>(null, "Count", 42) == 42, "missing and null fallback");
        Check((int)Reflect.Call(actor, "Add", 3) == 12, "private method");
        bool missing = false; try { Reflect.Call(actor, "Missing"); } catch (MissingMethodException) { missing = true; }
        Check(missing, "missing method must still throw");
        Type Make(string assembly)
        {
            var m=AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assembly), AssemblyBuilderAccess.Run).DefineDynamicModule("test");
            var t=m.DefineType("ExternalMod.Actor", TypeAttributes.Public);
            t.DefineField("Slot", typeof(int), FieldAttributes.Public);
            return t.CreateType();
        }
        Type one=Make("ProviderOne"), two=Make("ProviderTwo");
        Check(Reflect.Field(one,"Slot").DeclaringType == one && Reflect.Field(two,"Slot").DeclaringType == two, "same type name in different mods");
        Type type=typeof(Actor);
        for(int i=0;i<5000;i++){Reflect.Field(type,"Count");Reflect.Method(type,"Add");ReferenceReflect.Field(type,"Count");ReferenceReflect.Method(type,"Add");}
        long start=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<80000;i++){ReferenceReflect.Field(type,"Count");ReferenceReflect.Method(type,"Add");}
        long old=GC.GetAllocatedBytesForCurrentThread()-start;
        start=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<80000;i++){Reflect.Field(type,"Count");Reflect.Method(type,"Add");}
        long current=GC.GetAllocatedBytesForCurrentThread()-start;
        Check(current == 0 && old > 0, "steady reflection cache allocations");
        Console.WriteLine($"Reflection: 160000 lookups, temporary bytes {old} -> {current}");
    }

    private static void Frames()
    {
        var frames=new FramePlayers();var roster=actors.ToList();var random=new Random(197);
        for(int iteration=0;iteration<3000;iteration++)
        {
            roster.Clear();
            for(int p=0;p<8;p++)
            {
                actors[p].Alive=random.Next(3)!=0;actors[p].Warping=random.Next(4)==0;
                if(random.Next(5)!=0) roster.Add(actors[p]);
            }
            var expected=roster.ToArray();var snapshot=frames.Snapshot(roster);
            Check(snapshot.SequenceEqual(expected), "roster snapshot/order");
            roster.Reverse();
            for(int i=0;i<8;i++)Check(FramePlayers.FindByIndex(roster,i)==roster.Find(p=>p.Index==i),"indexed lookup for absent/reordered actors");
            Check(snapshot.SequenceEqual(expected), "snapshot survives source mutation");
            var living=frames.Living(roster);
            Check(living.SequenceEqual(roster.Where(p=>p.Alive)), "living exact length/order");
            var revivable=frames.Revivable(living);
            Check(revivable.SequenceEqual(living.Where(p=>!p.Warping)), "rescue exclusion");
            Check(living.SequenceEqual(roster.Where(p=>p.Alive)) && snapshot.SequenceEqual(expected), "independent buffers");
            Check(frames.TryStartActor() && !frames.TryStartActor() && !frames.TryStartActor(), "one new actor per frame");
        }
        roster=actors.ToList();foreach(var actor in actors){actor.Alive=true;actor.Warping=false;}
        for(int i=0;i<5000;i++){frames.Snapshot(roster);frames.Revivable(frames.Living(roster));}
        long start=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<80000;i++){var a=roster.ToArray();var b=roster.Where(p=>p.Alive).ToArray();var c=b.Where(p=>!p.Warping).ToArray();GC.KeepAlive(a);GC.KeepAlive(c);}
        long old=GC.GetAllocatedBytesForCurrentThread()-start;
        start=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<80000;i++){frames.Snapshot(roster);frames.Revivable(frames.Living(roster));}
        long current=GC.GetAllocatedBytesForCurrentThread()-start;
        Check(current == 0 && old > 0, "steady roster allocations");
        Check(FramePlayers.FindByIndex(null,0)==null,"no session index lookup");
        Console.WriteLine($"Frame lists: 80000 eight-player frames, temporary bytes {old} -> {current}");
    }

    private static void SummonsAndJoining()
    {
        OwnerTag Tag(PlayerSlot p,string name,bool active=true) => new OwnerTag{Player=p,gameObject=new GameObject{name=name,activeInHierarchy=active}};
        var tags=new List<OwnerTag>();
        foreach(var p in actors){tags.Add(Tag(p,"Orbit Shield"));tags.Add(Tag(p,"GrimmChild"));for(int i=0;i<4;i++)tags.Add(Tag(p,"Knight Hatchling"));tags.Add(Tag(p,"Knight"));}
        tags.Add(Tag(actors[0],"GRIMM CHILD",false));tags.Add(Tag(null,"DREAMSHIELD"));
        UnityEngine.Object.World=tags.ToArray();UnityEngine.Object.Scans=0;
        OwnerTag[] snapshot=null;
        foreach(var p in actors.Skip(1))for(int kind=0;kind<3;kind++)Check(SummonUnderTest.CountOwned(p,kind,ref snapshot) == (kind==2?4:1), "independent summon ownership");
        Check(UnityEngine.Object.Scans == 1, "one discovery per unchanged pass");
        tags[1].Player=actors[1];
        Check(SummonUnderTest.CountOwned(actors[0],1,ref snapshot)==0 && SummonUnderTest.CountOwned(actors[1],1,ref snapshot)==2, "pool reassignment uses live tags");
        tags[0].gameObject.activeInHierarchy=false;
        Check(SummonUnderTest.CountOwned(actors[0],0,ref snapshot)==0, "recycled summon ignored immediately");
        tags.Add(Tag(actors[0],"DREAM SHIELD"));UnityEngine.Object.World=tags.ToArray();snapshot=null;
        Check(SummonUnderTest.CountOwned(actors[0],0,ref snapshot)==1 && UnityEngine.Object.Scans==2, "new pass discovers new objects");
        UnityEngine.Object.World=Array.Empty<UnityEngine.Object>();snapshot=null;
        Check(SummonUnderTest.CountOwned(actors[0],0,ref snapshot)==0, "room change invalidation");
        Console.WriteLine("Summons: 21 ownership checks, world searches 21 -> 1 (no spawn in pass)");

        var collision=new CollisionUnderTest();
        var join=new PlayerSlot{Hero=new HeroController(),Colliders=Enumerable.Range(0,18).Select(_=>new Collider2D()).ToArray()};
        collision.Players.Add(join);
        for(int i=0;i<7;i++)collision.Players.Add(new PlayerSlot{Hero=new HeroController{Colliders=Enumerable.Range(0,13).Select(_=>new Collider2D()).ToArray()}});
        collision.Players.Add(new PlayerSlot());
        collision.Join(join);
        Check(Physics2D.Pairs.Count == 7*18*13, "all collision pairs preserved");
        Check(collision.Players.Skip(1).Where(p=>p.Hero).All(p=>p.Hero.Reads==1), "one hierarchy read per actor");
        foreach(var p in collision.Players.Skip(1).Where(p=>p.Hero))foreach(var a in join.Colliders)foreach(var b in p.Hero.Colliders)Check(Physics2D.Pairs.Contains((a,b)),"pair coverage");
        Console.WriteLine("Join collision setup: hierarchy searches 126 -> 7; all 1638 collision pairs preserved");
    }

    private static void Edges()
    {
        var old=new ReferenceLightEdgeFade();var current=new LightEdgeFade();
        var random=new Random(8197);
        for(int shape=0;shape<100;shape++)
        {
            int sides=3+random.Next(18);var points=new LightPoint[sides+1];var triangles=new ushort[sides*3];
            double sx=1+random.NextDouble()*50, sy=1+random.NextDouble()*30, cx=random.NextDouble()*30, cy=random.NextDouble()*20;
            points[0]=new LightPoint(cx,cy);
            for(int i=0;i<sides;i++)
            {
                double a=2*Math.PI*i/sides;points[i+1]=new LightPoint(cx+Math.Cos(a)*sx,cy+Math.Sin(a)*sy);
                triangles[i*3]=0;triangles[i*3+1]=(ushort)(i+1);triangles[i*3+2]=(ushort)((i+1)%sides+1);
            }
            old.Topology(triangles);current.Topology(triangles);old.Project(points);current.Project(points);
            Check(old.EdgeCount==current.EdgeCount && current.EdgeCount==sides,"outer polygon only");
            for(int sample=0;sample<600;sample++)
            {
                double x=cx+(random.NextDouble()*2-1)*sx*1.2, y=cy+(random.NextDouble()*2-1)*sy*1.2;
                Near(old.At(x,y),current.At(x,y),1e-14,"native edge feather parity");
            }
            // Re-project the same mutable array, as the renderer does each frame.
            for(int i=0;i<points.Length;i++)points[i].X+=4.75;
            old.Project(points);current.Project(points);
            for(int i=0;i<50;i++)Near(old.At(cx+i*.1,cy),current.At(cx+i*.1,cy),1e-14,"moved source parity");
        }
        current.Topology(Array.Empty<ushort>());Near(current.At(0,0),1,0,"unprojected topology");
        current.Project(Array.Empty<LightPoint>());Near(current.At(0,0),1,0,"empty sprite");
        // Representative CPU math only, not a Unity FPS estimate.
        var pts=Enumerable.Range(0,25).Select(i=>i==0?new LightPoint(20,20):new LightPoint(20+18*Math.Cos(2*Math.PI*(i-1)/24),20+14*Math.Sin(2*Math.PI*(i-1)/24))).ToArray();
        var ts=Enumerable.Range(0,24).SelectMany(i=>new ushort[]{0,(ushort)(i+1),(ushort)((i+1)%24+1)}).ToArray();
        old.Topology(ts);old.Project(pts);current.Topology(ts);current.Project(pts);
        double checksum=0;for(int i=0;i<30000;i++){checksum+=old.At(i%43,i%37);checksum+=current.At(i%43,i%37);}
        long Old(){var w=Stopwatch.StartNew();for(int i=0;i<200000;i++)checksum+=old.At(i%43,i%37);return w.ElapsedTicks;}
        long New(){var w=Stopwatch.StartNew();for(int i=0;i<200000;i++)checksum+=current.At(i%43,i%37);return w.ElapsedTicks;}
        var before=new long[5];var after=new long[5];for(int i=0;i<5;i++){before[i]=Old();after[i]=New();}Array.Sort(before);Array.Sort(after);
        Console.WriteLine($"Outer-rim math: median 200000 samples {before[2]*1000.0/Stopwatch.Frequency:F1} ms -> {after[2]*1000.0/Stopwatch.Frequency:F1} ms; checksum {checksum:F1}");
    }

    private static void Grid()
    {
        var normal=new LightBlendGrid();var reduced=new LightBlendGrid();var smooth=new LightBlendGrid();var random=new Random(1970);
        for(int count=2;count<=8;count++)for(int scenario=0;scenario<5;scenario++)
        {
            double width=scenario==0?30:120, height=scenario==0?18:75;
            var centres=Enumerable.Range(0,count).Select(_=>new LightPoint(random.NextDouble()*width,random.NextDouble()*height)).ToArray();
            normal.Prepare(centres,count,width,height,3.5);reduced.Prepare(centres,count,width,height,3.5,true);
            Check(reduced.Columns<normal.Columns && reduced.Rows<normal.Rows,"lower mesh resolution");
            smooth.Prepare(centres,count,width,height,3.5,true,true);
            Check(smooth.Columns>=32&&smooth.Columns<=64&&smooth.Rows>=20&&smooth.Rows<=40,"smooth mesh resolution bounds");
            foreach(var grid in new[]{normal,reduced,smooth})
            {
                var field=new Dictionary<(float,float),double>();var list=new List<LightWeightedVertex>();
                for(int player=0;player<count;player++)
                {
                    list.Clear();
                    grid.Tessellate(new LightVertex(-.1f,-.1f,0,0,0),new LightVertex(1.1f,-.1f,0,1,0),new LightVertex(1.1f,1.1f,0,1,1),player,list);
                    grid.Tessellate(new LightVertex(-.1f,-.1f,0,0,0),new LightVertex(1.1f,1.1f,0,1,1),new LightVertex(-.1f,1.1f,0,0,1),player,list);
                    // Same shared field evaluated independently by each glow.
                    var unique=new Dictionary<(float,float),double>();
                    foreach(var v in list)
                    {
                        Check(v.Weight>=-1e-14 && v.Weight<=1+1e-14,"bounded light ownership");
                        var key=(v.Vertex.X,v.Vertex.Y);
                        if(unique.TryGetValue(key,out double previous))Near(previous,v.Weight,2e-5,"continuous grid vertex/diagonal");
                        unique[key]=v.Weight;
                    }
                    foreach(var pair in unique){field.TryGetValue(pair.Key,out double sum);field[pair.Key]=sum+pair.Value;}
                }
                // Clipping can emit near-identical boundary coordinates in
                // different zero-weight regions. Test unity at true grid nodes.
                int nodes=0;
                foreach(var pair in field)
                {
                    double gx=(pair.Key.Item1+.01)/(1.02/grid.Columns), gy=(pair.Key.Item2+.01)/(1.02/grid.Rows);
                    if(Math.Abs(gx-Math.Round(gx))<2e-5 && Math.Abs(gy-Math.Round(gy))<2e-5)
                    {Near(pair.Value,1,3e-5,"one shared glow budget");nodes++;}
                }
                Check(nodes>grid.Columns*grid.Rows/2,"no blank shared-light field");
            }
        }
        var coincident=Enumerable.Repeat(new LightPoint(20,10),8).ToArray();var weights=new double[8];
        for(int i=0;i<1000;i++){LightBlendRules.Fill(coincident,8,i*.03,i*.02,3.5,weights);Near(weights.Sum(),1,1e-14,"no brightness growth with eight coincident players");foreach(double w in weights)Near(w,.125,1e-14,"equal overlapping lights");}
        normal.Prepare(coincident,8,120,75,3.5);reduced.Prepare(coincident,8,120,75,3.5,true);
        int before=(normal.Columns+1)*(normal.Rows+1), after=(reduced.Columns+1)*(reduced.Rows+1);
        Console.WriteLine($"Large-room light field: samples per rebuild {before} -> {after}; normalized budget preserved for 2–8 players");
        Plugin.Self=null;Check(!PerformanceTuning.Enabled && PerformanceTuning.CosmeticRate(22)==22,"default full detail");
        Plugin.Self=new Local8Runtime{PerformanceMode=new Setting<bool>{Value=true}};
        Check(PerformanceTuning.Enabled && PerformanceTuning.CosmeticRate(22)==11,"optional cosmetic rate");
        Plugin.Self.PerformanceMode.Value=false;Check(PerformanceTuning.CosmeticRate(22)==22,"live setting restored");
    }

    private static void SmoothEdges()
    {
        // A 12-sided tight sprite: all straight outside edges must disappear,
        // while equal distances from its center have equal fade in every direction.
        var pts=new LightPoint[13];var triangles=new ushort[36];pts[0]=new LightPoint(20,20);
        for(int i=0;i<12;i++)
        {
            double angle=2*Math.PI*i/12;pts[i+1]=new LightPoint(20+18*Math.Cos(angle),20+14*Math.Sin(angle));
            triangles[i*3]=0;triangles[i*3+1]=(ushort)(i+1);triangles[i*3+2]=(ushort)((i+1)%12+1);
        }
        var fade=new LightEdgeFade();fade.Topology(triangles);fade.Project(pts,true);
        Near(fade.At(20,20),1,0,"smooth light center retained");
        double radius=Math.Cos(Math.PI/12);
        for(int i=0;i<360;i++)
        {
            double angle=2*Math.PI*i/360;
            Near(fade.At(20+18*.84*radius*Math.Cos(angle),20+14*.84*radius*Math.Sin(angle)),fade.At(20+18*.84*radius,20),1e-12,"round light has no angular seam");
            Near(fade.At(20+18*Math.Cos(angle),20+14*Math.Sin(angle)),0,1e-12,"polygon edge still visible");
        }
        for(double r=0;r<1.2;r+=.01){double v=fade.At(20+18*r,20);Check(v>=0&&v<=1,"unbounded round fade");}
        fade.Project(pts,false);Near(fade.At(20,20),1,0,"legacy toggle restores native center");
    }

    private static int Main()
    {
        try {Reflection();Frames();SummonsAndJoining();Edges();SmoothEdges();Grid();Console.WriteLine($"PASS performance: {checks} checks");return 0;}
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
