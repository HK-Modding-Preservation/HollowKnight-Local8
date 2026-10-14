using System;using System.Collections.Generic;using System.Linq;using KO.HollowKnight8;
internal class Test {
 static int checks;
 static void Check(bool ok,string what){checks++;if(!ok)throw new Exception(what);}
 static List<CharmCell> Grid(int count,HashSet<int> owned=null)=>Enumerable.Range(1,count).Select(id=>new CharmCell(id,((id-1)%10)*1.5+(((id-1)/10)%2)*.65,-((id-1)/10)*2+((id%2)*.08),owned==null||owned.Contains(id))).ToList();
 static void Catalog(int[] ids){CharmExtensions.Reset();int max=ids.Length==0?40:Math.Max(40,ids.Max());CharmIconList.Instance=new(){spriteList=new UnityEngine.Sprite[max+1]};foreach(int id in ids)CharmIconList.Instance.spriteList[id]=new();CharmExtensions.Discover();}
 static void Main(){
  CharmExtensions.Install();
  var random=new Random(194);
  for(int sample=0;sample<1000;sample++){
   var owned=new HashSet<int>(Enumerable.Range(1,40).Where(_=>random.Next(3)!=0));var grid=Grid(40,owned);grid=grid.OrderBy(_=>random.Next()).ToList();
   foreach(int id in Enumerable.Range(1,40))foreach(var d in new[]{(-1,0),(1,0),(0,1),(0,-1)})Check(CharmGridRules.Next(id,d.Item1,d.Item2,grid)==LegacyCharmGridRules.Next(id,d.Item1,d.Item2,grid),"Vanilla navigation parity");
  }
  var pale=Grid(50,new HashSet<int>{1,33,39,43});
  Check(CharmGridRules.Next(33,0,-1,pale)==43,"Down reaches owned Pale Court row");
  Check(CharmGridRules.Next(43,0,1,pale)==33,"Up returns to closest native column");
  Check(CharmGridRules.Next(39,0,-1,pale)==43,"Unowned Pale Court slots skipped");
  Check(CharmGridRules.Next(43,1,0,pale)==43,"Padding is unselectable");
  Check(CharmGridRules.Next(43,0,-1,pale)==43,"Last row does not wrap");
  pale.Add(new CharmCell(701,3,-10,true));pale.Add(new CharmCell(502,0,-10,true));
  Check(CharmGridRules.Next(43,0,-1,pale)==701,"A sixth partial row with arbitrary IDs");
  Check(CharmGridRules.Next(701,-1,0,pale)==502,"Sparse non-sequential mod IDs");
  Check(CharmGridRules.Next(701,0,1,pale)==43,"Sixth row returns to fifth");
  pale.Add(new CharmCell(701,999,999,false));pale.Add(new CharmCell(0,0,0,true));pale.Add(new CharmCell(900,double.NaN,0,true));
  Check(CharmGridRules.Next(43,0,-1,pale)==701,"Duplicate and invalid boards ignored");
  var removed=Grid(40,new HashSet<int>{1,40});removed.Add(new CharmCell(43,3,-8,false));
  Check(CharmGridRules.Next(40,0,-1,removed)==40,"Unowned extension does not change bottom boundary");
  Catalog(Array.Empty<int>());var native=new PlayerData();native.Owned.UnionWith(Enumerable.Range(1,40));native.equippedCharm_1=true;native.equippedCharms.Add(1);native.charmSlotsFilled=1;
  var original=new CharmLoadout();original.Read(native);Check(ReferenceEquals(original.Order,native.equippedCharms),"No-mod native alias/allocation behaviour preserved");original.Write(native);Check(native.Writes==0&&native.MissingProviderCalls==0,"Vanilla never calls extra provider setters");
  Catalog(new[]{41,42,43,44,101,701});var data=new PlayerData();data.Owned.UnionWith(Enumerable.Range(1,40));data.Owned.UnionWith(new[]{43,101,701});foreach(var e in CharmExtensions.Entries){data.Provider[e.Id]=false;data.Costs[e.Id]=2;}
  Check(CharmExtensions.Ids().SequenceEqual(Enumerable.Range(1,44).Concat(new[]{101,701})),"Runtime registry uses real registered IDs and no padding");
  Check(Charms.FirstOwned(data)==1,"Vanilla first owned remains unchanged");var onlyExtra=new PlayerData();onlyExtra.Owned.Add(43);Check(Charms.FirstOwned(onlyExtra)==43,"First owned can be modded");
  Check(Charms.EditorNeighbor(33,10)==43,"F8 editor reaches extra ID");
  for(int id=1;id<=200;id++)Check(!CharmExtensions.IsRole(id),"No positive charm ID is a role");for(int role=0;role<9;role++)Check(CharmExtensions.IsRole(CharmExtensions.Role(role)),"Role cursor uses separate domain");
  var slots=new PlayerSlot[8];for(int i=0;i<8;i++){slots[i]=new(){Index=i,Hero=new(){Data=data}};slots[i].Charms.Initialize(data,new[]{i+1,43,101,701,701,42,9999,-2,0});Check(slots[i].Charms.Ids().SequenceEqual(new[]{i+1,43,101,701}),"Custom and native loadout initialization, duplicates and unowned rejected");Check(slots[i].Charms.Used==7,"Mod costs count independently");}
  var session=new CoopSession{Data=data,Primary=slots[0],Players=slots.ToList()};Plugin.Self=new(){Session=session};slots[0].Apply(data);
  Charms.Toggle(slots[1],43);Check(!slots[1].Charms.IsEquipped(43),"Extra can unequip by reselecting");Check(slots[0].Charms.IsEquipped(43)&&data.Provider[43],"Primary/provider state restored after P2 removes charm");
  Charms.Toggle(slots[1],43);Check(slots[1].Charms.IsEquipped(43)&&slots[1].Charms.Used==7,"P2 can equip again with correct cost");
  Charms.Toggle(slots[0],101);Check(!slots[0].Charms.IsEquipped(101)&&!data.Provider[101],"P1 custom unequip works, including IDs above 100");Check(slots[1].Charms.IsEquipped(101),"P1 does not alter P2 loadout");
  int saves=Charms.Saves;slots[2].Editable=false;Charms.Toggle(slots[2],43);Check(slots[2].Charms.IsEquipped(43)&&Charms.Saves==saves,"Bench requirement preserved");
  Charms.Toggle(slots[0],9999);Check(Charms.Saves==saves,"Unknown ID is rejected");
  for(int cycle=0;cycle<100;cycle++)foreach(var p in slots){using(PlayerContext.Enter(p)){Check(data.GetBool("equippedCharm_43")==p.Charms.IsEquipped(43),"Provider sees current player's equipment");Check(data.GetBool("equippedCharm_101")==p.Charms.IsEquipped(101),"ID>100 provider isolation");}Check(data.GetBool("equippedCharm_101")==slots[0].Charms.IsEquipped(101),"P1 restored after scoped provider state");}
  var copy=PvpCopy.Run(slots[1].Charms);new CharmLoadout().Write(data);Check(data.Provider.Values.All(v=>!v),"No-charms PvP disables custom provider flags");copy.Write(data);Check(data.Provider[43]&&data.Provider[101]&&data.Provider[701],"PvP snapshot restores custom charms");copy.Extra.Remove(43);Check(slots[1].Charms.IsEquipped(43),"PvP snapshot has no shared mutable custom set");
  var read=new CharmLoadout();read.Read(data);data.equippedCharms.RemoveAll(id=>id>40);Check(read.Order.Contains(43)&&read.Ids().Contains(43),"SFCore before-save list removal cannot erase slot snapshot");read.Read(data);Check(read.Order.Contains(43),"Provider flags recover save-stripped order");
  var saved=slots.Select(p=>p.Charms.Ids()).ToArray();for(int i=0;i<8;i++){var reload=new CharmLoadout();reload.Initialize(data,saved[i]);Check(reload.Ids().SequenceEqual(saved[i]),"Eight-player save/reload retains mod IDs");}
  var full=new CharmLoadout();data.charmSlots=1;full.Initialize(data,new[]{1,43});Check(!full.IsEquipped(43)&&full.Used==1,"Extra cannot bypass notch cap");data.canOvercharm=true;full.Initialize(data,new[]{43});Check(full.IsEquipped(43)&&full.Overcharmed,"Vanilla overcharm rule preserved for mods");
  Check(data.MissingProviderCalls==0,"No unregistered/padded ID sent to equipment hooks");
  PerformanceAndInvalidation();
  VoidHeart();
  CharmExtensions.Uninstall();
  Console.WriteLine("PASS custom charm compatibility: "+checks+" checks");
 }

 static void VoidHeart(){
  Catalog(Array.Empty<int>());var data=new PlayerData{Royal=3};data.Owned.UnionWith(Enumerable.Range(1,40));data.Costs[36]=5;
  var slots=Enumerable.Range(0,8).Select(i=>new PlayerSlot{Index=i,Hero=new(){Data=data}}).ToArray();
  var session=new CoopSession{Data=data,Primary=slots[0],Players=slots.ToList()};Plugin.Self=new(){Session=session};slots[0].Apply(data);
  Charms.Toggle(slots[0],36);Check(slots[0].Charms.IsEquipped(36)&&slots[0].Charms.Used==5,"Kingsoul keeps normal equip and notch cost before Void Heart");
  Charms.Toggle(slots[0],36);Check(!slots[0].Charms.IsEquipped(36),"Kingsoul can still be unequipped");
  int[] existing={1};Check(ReferenceEquals(VoidHeartRules.Include(data,existing),existing),"No royal progression is unlocked or inserted before acquisition");
  data.Royal=4;data.Costs[36]=0;
  foreach(var p in slots){p.Charms.Initialize(data,new[]{p.Index+1});Check(!p.Charms.IsEquipped(36),"Legacy profile missing Void Heart fixture");}
  slots[0].Apply(data);int saves=Charms.Saves;VoidHeartRules.Ensure(session);
  Check(Charms.Saves==saves+1,"All repaired slots are saved in one batch");
  foreach(var p in slots){
   Check(p.Charms.IsEquipped(36)&&p.Charms.Used==1&&p.Charms.Ids().Length==2,"P1-P8 receive permanent zero-notch Void Heart, retain other charms");
   int updates=p.Hero.Updates;saves=Charms.Saves;Charms.Toggle(p,36);
   Check(p.Charms.IsEquipped(36)&&p.Hero.Updates==updates&&Charms.Saves==saves,"P1-P8 cannot unequip Void Heart through either menu's shared Toggle");
   int[] reload=VoidHeartRules.Include(data,new[]{p.Index+1});var loaded=new CharmLoadout();loaded.Initialize(data,reload);
   Check(loaded.IsEquipped(36)&&loaded.Used==1,"Restoring an older loadout includes acquired Void Heart for free");
   Check(ReferenceEquals(VoidHeartRules.Include(data,reload),reload),"Existing permanent charm is not duplicated");
  }
  int total=slots.Sum(p=>p.Hero.Updates);saves=Charms.Saves;for(int i=0;i<100;i++)VoidHeartRules.Ensure(session);
  Check(slots.Sum(p=>p.Hero.Updates)==total&&Charms.Saves==saves,"Stable invariant causes no CharmUpdate or save per frame");
  var secondary=slots[2];secondary.Charms.Initialize(data,new[]{3});BossSequenceController.BoundCharms=true;VoidHeartRules.Ensure(session);
  Check(!secondary.Charms.IsEquipped(36)&&ReferenceEquals(VoidHeartRules.Include(data,existing),existing),"Native Pantheon charm binding is never overridden");BossSequenceController.BoundCharms=false;
  PvpCharms.Active=true;PvpCharms.Allowed=false;VoidHeartRules.Ensure(session);Check(!secondary.Charms.IsEquipped(36),"Temporary no-charms PvP loadout remains suppressed");PvpCharms.Active=false;
  secondary.Ready=false;VoidHeartRules.Ensure(session);Check(!secondary.Charms.IsEquipped(36),"Pending clone is not updated before native initialization");secondary.Ready=true;VoidHeartRules.Ensure(session);Check(secondary.Charms.IsEquipped(36),"Adventure invariant resumes when binding/temporary suppression ends");
  data.Royal=0;secondary.Charms.Initialize(data,new[]{3});VoidHeartRules.Ensure(session);Check(!secondary.Charms.IsEquipped(36),"Unacquired royal charm is never granted");
 }

 static void PerformanceAndInvalidation(){
  Catalog(new[]{41,42,43,44});var data=new PlayerData();data.Owned.UnionWith(new[]{1,43});for(int id=41;id<=44;id++)data.Provider[id]=id==43;
  data.equippedCharm_1=true;data.equippedCharms.AddRange(new[]{1,43});var slots=Enumerable.Range(0,8).Select(_=>new CharmLoadout()).ToArray();
  foreach(var slot in slots){slot.Read(data);slot.Write(data);slot.Read(data);}
  data.Reads=0;long start=GC.GetAllocatedBytesForCurrentThread();
  for(int frame=0;frame<100;frame++){
   UnityEngine.Time.frameCount++;
   for(int scope=0;scope<100;scope++)foreach(var slot in slots){slot.Read(data);slot.Write(data);}
  }
  long allocated=GC.GetAllocatedBytesForCurrentThread()-start;
  Check(data.Reads==400,"80,000 unchanged context switches read providers once per frame, not once per scope");
  Check(data.Writes==0,"Identical player loadouts never rewrite provider flags");
  Check(allocated<100000,"Reused lists eliminate per-context allocations: "+allocated+" bytes for 80,000 switches");
  Console.WriteLine("Loadout hot path: 80,000 unchanged read/write pairs, "+data.Reads+" provider reads, "+data.Writes+" writes, "+allocated+" bytes (includes provider double string parsing).");
  var savedOrder=slots[0].Order;data.SetBool("equippedCharm_43",false);slots[0].Read(data);
  Check(!slots[0].IsEquipped(43)&&!slots[0].Order.Contains(43),"Native/provider removal invalidates same-frame cache immediately");
  data.SetBool("equippedCharm_43",true);slots[0].Read(data);Check(slots[0].IsEquipped(43)&&ReferenceEquals(savedOrder,slots[0].Order),"Re-equip preserves reusable snapshot and does not wait a frame");
  data.Provider[43]=false;UnityEngine.Time.frameCount++;slots[0].Read(data);Check(!slots[0].IsEquipped(43),"Direct provider settings changes are re-read next frame");
  var another=new PlayerData();for(int id=41;id<=44;id++)another.Provider[id]=id==44;slots[0].Read(another);Check(slots[0].IsEquipped(44)&&!slots[0].IsEquipped(43),"Changing saves does not reuse another PlayerData's provider flags");
  Modding.ModHooks.SetBoolProxy reject=(name,value)=>name=="equippedCharm_43"?false:value;Modding.ModHooks.SetPlayerBoolHook+=reject;
  slots[1].Write(data);slots[1].Read(data);Check(!slots[1].IsEquipped(43),"Cache respects the final value returned by later provider hooks");Modding.ModHooks.SetPlayerBoolHook-=reject;
  Catalog(new[]{43});data.Provider[43]=true;slots[0].Read(data);Check(slots[0].IsEquipped(43),"Registry reset invalidates all cached provider state");
 }
}
