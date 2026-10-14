using System;
using System.Linq;
using KO.HollowKnight8;
using UnityEngine;
static class Test {
 static int checks;
 static void Check(bool yes,string why){checks++;if(!yes)throw new Exception(why);}
 static Local8Runtime Runtime(int count=8,int rate=11){
  PvpSpellOnly.End();Local8Mod.Settings=new Local8Settings{PvpMode=2,PvpSpellsOnly=true,PvpSoulRegeneration=rate};
  PvpMatch.Running=PvpMatch.CanFight=true;GameManager.instance=new GameManager();PlayerContext.Current=null;
  Charms.NativeMenuOpen=RoleSystem.JoiningMenu=ShopMenuRouting.MenuVisible=StagMenuRouting.HasOwner=false;Application.isFocused=true;
  var r=new Local8Runtime{Session=new CoopSession()};Plugin.Self=r;
  for(int i=0;i<count;i++)r.Session.Players.Add(new PlayerSlot{Index=i});r.Session.Primary=r.Session.Players[0];
  PvpSpellOnly.Begin(r.Session);return r;
 }
 static void Tick(Local8Runtime r,float seconds){Time.deltaTime=seconds;PvpSpellOnly.Tick(r);}
 static void Press(PlayerSlot p){foreach(var a in new[]{p.Actions.attack,p.Actions.dreamNail,p.Actions.superDash,p.Actions.cast,p.Actions.jump,p.Actions.dash,p.Actions.focus,p.Actions.pause})a.Pressed=true;}
 static PvpCombat.Attack Attack(PlayerSlot p,PvpKind kind,string name){return new PvpCombat.Attack{Owner=p,Kind=kind,Collider=new Collider2D{transform=new Transform{name=name,parent=p.Hero.transform}}};}
 static void Main(){
  foreach(int n in new[]{int.MinValue,-1,0,1,11,99,100,int.MaxValue}){var c=new Local8Settings{PvpSoulRegeneration=n};c.Ensure();Check(c.PvpSoulRegeneration>=1&&c.PvpSoulRegeneration<=99,"Serialized regeneration can disable the fight");}
  var r=Runtime();Tick(r,.25f);Tick(r,.25f);Tick(r,.25f);Tick(r,.25f);
  foreach(var p in r.Session.Players)Check(p.Vitals.Soul==11,"Regeneration does not award each independent owner");
  Check(r.Session.Data.MPCharge==11,"Extra-player regeneration was redirected to P1");
  r.Session.Players[1].Vitals.MaxSoul=5;r.Session.Players[1].Vitals.Soul=4;Tick(r,1);
  Check(r.Session.Players[1].Vitals.Soul==5&&r.Session.Primary.Vitals.Soul==22,"Custom capacity is ignored or changes another player");
  Check(r.Session.Players.All(p=>p.Vitals.Reserve==0),"Regeneration grants reserve or changes unrelated vitals");
  // Different slots and changing presentation settings cannot share a fractional remainder.
  r=Runtime(2,1);Tick(r,.5f);r.Session.Players[1].Alive=false;Tick(r,.5f);
  Check(r.Session.Primary.Vitals.Soul==1&&r.Session.Players[1].Vitals.Soul==0,"A downed player regenerates or delays the other owner");
  r.Session.Players[1].Alive=true;Tick(r,.5f);Check(r.Session.Players[1].Vitals.Soul==0,"Death retained a partial regeneration credit");Tick(r,.5f);Check(r.Session.Players[1].Vitals.Soul==1,"Recovered player cannot regenerate");
  Local8Mod.Settings.PvpSoulRegeneration=99;Local8Mod.Settings.PvpSpellsOnly=false;Tick(r,1);Check(r.Session.Primary.Vitals.Soul==3,"Live settings overwrite the duel's rule snapshot");
  r=Runtime(1,11);var p0=r.Session.Primary;p0.Vitals.Soul=99;for(int i=0;i<10;i++)Tick(r,1);p0.Vitals.Soul=0;Tick(r,.1f);Check(p0.Vitals.Soul==1,"Full vessel banks a burst of delayed mana");
  r=Runtime(1,1);Tick(r,.5f);PvpSpellOnly.Round();Tick(r,.5f);Check(r.Session.Primary.Vitals.Soul==0,"New round retained the previous fraction");Tick(r,.5f);Check(r.Session.Primary.Vitals.Soul==1,"Fractional regeneration does not reach a whole point");
  foreach(float delta in new[]{-1f,0,float.NaN,float.PositiveInfinity}){int before=r.Session.Primary.Vitals.Soul;Tick(r,delta);Check(r.Session.Primary.Vitals.Soul==before,"Invalid clock grants mana");}
  // Pause, F8, room changes, intermission and invalid actors never generate catch-up mana.
  Action<Local8Runtime>[] blocked={x=>x.Panel=true,x=>x.Enabled.Value=false,x=>x.Session.Active=false,x=>x.Session.Gameplay=false,x=>x.Session.TeamWipe=true,x=>GameManager.instance.isPaused=true,x=>GameManager.instance.IsLoadingSceneTransition=true,x=>GameManager.instance=null,x=>PvpMatch.CanFight=false,x=>PvpMatch.Running=false,x=>Local8Mod.Settings.PvpMode=1,x=>PlayerContext.Current=x.Session.Players[1]};
  foreach(var block in blocked){r=Runtime(2);block(r);for(int i=0;i<4;i++)Tick(r,.5f);Check(r.Session.Players.All(p=>p.Vitals.Soul==0),"Blocked match state regenerated SOUL");}
  Action<PlayerSlot>[] invalid={p=>p.Alive=false,p=>p.Ready=false,p=>p.Connected=false,p=>p.Hero=null,p=>p.Hazard=true,p=>p.ArenaTransfer=true,p=>p.SpawnPending=true,p=>p.Vitals.AtBench=true,p=>p.Hero.cState.transitioning=true};
  foreach(var mutate in invalid){r=Runtime(2);mutate(r.Session.Players[1]);Tick(r,1);Check(r.Session.Primary.Vitals.Soul==11&&r.Session.Players[1].Vitals.Soul==0,"Actor exclusion blocks P1 or regenerates an invalid extra");}
  r=Runtime(2);r.Session.Players.RemoveAt(1);Tick(r,1);Check(r.Session.Primary.Vitals.Soul==11,"Roster removal breaks the surviving owner");
  // Execute the real Controls.Pump; never replace bindings or clear movement/spell/pause actions.
  r=Runtime(2);foreach(var p in r.Session.Players){Press(p);Controls.Pump(p,false);Check(!p.Actions.attack.Pressed&&!p.Actions.dreamNail.Pressed&&!p.Actions.superDash.Pressed,"Native offensive input still enters a spell duel");Check(p.Actions.cast.Pressed&&p.Actions.jump.Pressed&&p.Actions.dash.Pressed&&p.Actions.focus.Pressed&&p.Actions.pause.Pressed,"Spell rule steals a movement, focus or pause control");}
  var outsider=new PlayerSlot{Index=1};Press(outsider);Controls.Pump(outsider,false);Check(outsider.Actions.attack.Pressed,"Spell rule modifies a different actor reusing the slot");
  Action<Local8Runtime>[] menus={x=>x.Panel=true,x=>Charms.NativeMenuOpen=true,x=>RoleSystem.JoiningMenu=true,x=>ShopMenuRouting.MenuVisible=true,x=>StagMenuRouting.HasOwner=true,x=>GameManager.instance.isPaused=true,x=>x.Session.Gameplay=false,x=>x.Session.Primary.Hero.controlReqlinquished=true};
  foreach(var menu in menus){r=Runtime(2);menu(r);Press(r.Session.Primary);Controls.Pump(r.Session.Primary,false);Check(r.Session.Primary.Actions.attack.Pressed,"Spell duel steals submit/cancel from a native menu");}
  r=Runtime();foreach(var p in r.Session.Players){
   foreach(PvpKind kind in Enum.GetValues(typeof(PvpKind)))Check(PvpSpellOnly.Allows(Attack(p,kind,"Quake Hit"))==(kind==PvpKind.Spell),"Another attack kind can damage in spell-only mode");
   foreach(string name in new[]{"Fireball2 Clone","Scream Hit","Quake Hit","Spell Beam"})Check(PvpSpellOnly.Allows(Attack(p,PvpKind.Spell,name)),"A real spell was rejected");
   foreach(string name in new[]{"Thorn Hit","Spore Cloud","Dung Cloud","Grimmball","Shield Hit","Hatchling"}){
    var attack=Attack(p,PvpKind.Spell,"Hitbox");attack.Collider.transform.parent=new Transform{name=name,parent=p.Hero.transform};Check(!PvpSpellOnly.Allows(attack),"A SPELL-typed charm bypasses the weapon rule");
   }
  }
  Check(!PvpSpellOnly.Allows(null),"Null attack can damage a player");PvpSpellOnly.End();Check(PvpSpellOnly.Allows(Attack(r.Session.Primary,PvpKind.Nail,"Slash")),"Ordinary attacks remain blocked after the duel");
  Press(r.Session.Primary);Controls.Pump(r.Session.Primary,false);Tick(r,1);Check(r.Session.Primary.Actions.attack.Pressed&&r.Session.Primary.Vitals.Soul==0,"Leaving PvP keeps suppressed input or regeneration");
  foreach(int mode in new[]{0,1}){r=Runtime();Local8Mod.Settings.PvpMode=mode;foreach(var p in r.Session.Players){Press(p);Controls.Pump(p,false);Check(p.Actions.attack.Pressed&&PvpSpellOnly.Allows(Attack(p,PvpKind.Charm,"Shield")),"Adventure/friendly fire inherited duel restrictions");}Tick(r,1);Check(r.Session.Players.All(p=>p.Vitals.Soul==0),"Adventure receives PvP regeneration");}
  // Arena normalization is applied to a runtime copy; unlocked/native levels remain native.
  r=Runtime(2);var saved=new PvpArenaOptions{SoulCapacity=1,SpellLevel=0};var live=saved.Copy();PvpSpellOnly.ArenaRules(live,r.Session);Check(live.SoulCapacity==33&&live.SpellLevel==1&&saved.SoulCapacity==1&&saved.SpellLevel==0,"Arena safety overwrites preferences or prevents casting");
  live=new PvpArenaOptions{SoulCapacity=250,SpellLevel=-1,InfiniteSoul=true};PvpSpellOnly.ArenaRules(live,r.Session);Check(live.SpellLevel==-1&&live.SoulCapacity==250&&live.InfiniteSoul,"Arena loses native levels/capacity or an explicitly chosen infinite-SOUL rule");
  r.Session.Data.fireballLevel=0;live=new PvpArenaOptions{SpellLevel=-1};PvpSpellOnly.ArenaRules(live,r.Session);Check(live.SpellLevel==1,"Arena has no castable spell");
  Check(PvpSpellOnly.StartIssue(r.Session)=="pvp.spells_required","Adventure-room spell gate fails");r.Session.Data.screamLevel=1;r.Session.Primary.Vitals.MaxSoul=10;Check(PvpSpellOnly.StartIssue(r.Session)=="pvp.spell_capacity","Adventure allows an unusable SOUL capacity");r.Session.Primary.Vitals.MaxSoul=99;Check(PvpSpellOnly.StartIssue(r.Session)=="","Valid spell duel is rejected");
  Local8Mod.Settings.PvpSpellsOnly=false;r.Session.Data.screamLevel=0;Check(PvpSpellOnly.StartIssue(r.Session)=="","Normal duels require spell unlocks");
  Console.WriteLine("PASS "+checks+" production spell-duel checks: eight independent owners, fractional SOUL/caps, pause/round/exit gates, native input isolation, SPELL-typed charm filtering, arena-copy safety and settings limits. Native Unity combat remains untested.");
 }
}
