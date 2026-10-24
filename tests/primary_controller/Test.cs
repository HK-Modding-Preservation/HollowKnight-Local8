using System;
using System.Linq;
using InControl;
using KO.HollowKnight8;
using UnityEngine;

static class Test {
 static int checks;
 static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
 static InputDevice Pad(string name="Pad")=>new InputDevice{Name=name};
 static CoopSession Start(string preference,params InputDevice[] pads){
  return StartInput(preference,null,pads.Length>0,pads);
 }
 static CoopSession StartInput(string preference,LobbyRoster lobby,bool fromPad,params InputDevice[] pads){
  InputManager.Devices=pads.ToList();InputManager.ActiveDevice=pads.FirstOrDefault()??InputDevice.Null;KeyboardMemory.Saved.Clear();
  var vanilla=new HeroActions();
  vanilla.LastInputType=fromPad?BindingSourceType.DeviceBindingSource:BindingSourceType.KeyBindingSource;
  foreach(var a in vanilla.Actions){a.AddBinding(new KeyBindingSource(Key.Return));a.AddBinding(new MouseBindingSource(1));a.AddBinding(new DeviceBindingSource(InputControlType.Action1));}
  vanilla.jump.ClearBindings();vanilla.jump.AddBinding(new KeyBindingSource(Key.Space));vanilla.jump.AddBinding(new DeviceBindingSource(InputControlType.Action4));
  var runtime=new Local8Runtime();runtime.PrimaryDevice.Value=preference;Local8Runtime.Self=runtime;
  if(lobby!=null)runtime.Lobby=lobby;
  var session=new CoopSession{plugin=runtime,originalActions=vanilla};runtime.Session=session;
  session.Initialize(new PlayerSlot{Index=0,ProfileId="primary",Health=7});
  GameManager.instance=new GameManager();GameManager.instance.inputHandler.inputActions=session.Primary.Actions;
  return session;
 }
 static void End(CoopSession s){foreach(var p in s.Players)Controls.Dispose(p);s.originalActions.Destroy();GameManager.instance=null;}
 static void Press(CoopSession s,InputDevice d){d.CommandWasPressed=true;s.Press(d);d.CommandWasPressed=false;}
 static void BindingKind(PlayerSlot p,bool keyboard){
  Check(p.Actions.Device==p.Device,"action set follows another controller");
  foreach(var a in p.Actions.Actions)foreach(var b in a.Bindings)Check(keyboard?(b is KeyBindingSource||b is MouseBindingSource):(b is DeviceBindingSource||b is UnknownDeviceBindingSource),"mixed keyboard/controller bindings");
 }
 static void Migration(){
  Check(PrimaryController.Normalize("Keyboard",true)=="Auto","legacy forced keyboard not migrated");
  Check(PrimaryController.Normalize("Keyboard",false)=="Keyboard","explicit keyboard choice lost on reload");
  Check(PrimaryController.Normalize("FirstGamepad",true)=="Auto","old gamepad preference disabled");
  Check(PrimaryController.Normalize(null,true)=="Auto","empty settings force keyboard");
  Check(PrimaryController.Normalize("saved-pad",true)=="saved-pad","saved pad was overwritten");
 }
 static void EightControllers(){
  var pads=Enumerable.Range(0,8).Select(i=>Pad("Pad "+i)).ToArray();var s=Start("Auto",pads);
  Check(s.Primary.Device==pads[0],"P1 did not receive first gamepad");Press(s,pads[0]);Check(s.Players.Count==1,"P1 Start created duplicate P2");
  for(int i=1;i<8;i++)Press(s,pads[i]);
  Check(s.Players.Count==8&&s.Players.Select(p=>p.Device).Distinct().Count()==8,"eight pads still require a ninth keyboard player");
  Check(s.Players.Select(p=>p.Actions).Distinct().Count()==8,"players share one action set");
  foreach(var p in s.Players){BindingKind(p,false);Check(p.Connected,"connected pad marked disconnected");}
  foreach(var pad in pads)Press(s,pad);Check(s.Players.Count==8,"repeated Start duplicates a player");
  Check(s.originalActions.jump.Bindings.Count==2&&!s.originalActions.IsDestroyed,"vanilla bindings modified/destroyed");
  Check(s.Primary.Actions.jump.Bindings.OfType<DeviceBindingSource>().Single().Control==InputControlType.Action4,"native custom gamepad jump binding lost");
  s.plugin.PrimaryDevice.Value="Keyboard";s.AssignPrimaryAutomatic();Check(s.plugin.PrimaryDevice.Value=="Auto","automatic button does not persist mode");End(s);
 }
 static void Selection(){
  InputDevice a=Pad("Same"),b=Pad("Same"),passive=Pad("Passive"),gone=Pad("Gone");passive.Passive=true;gone.IsAttached=false;
  InputManager.Devices=new(){a,b,passive,gone};
  Check(PrimaryController.Resolve("Auto",InputManager.Devices,b)==b,"active controller ignored");
  Check(PrimaryController.Resolve("Auto",InputManager.Devices,passive)==InputDevice.Null,"idle controller claimed P1 without joining");
  Check(PrimaryController.Resolve("Auto",new[]{gone,passive},gone)==InputDevice.Null,"detached controller assigned");
  Check(PrimaryController.Resolve(Controls.Key(b),InputManager.Devices,a)==b,"specific controller preference ignored");
  Check(Controls.Key(a)!=Controls.Key(b),"same-name controllers share identity");
  var s=Start("Keyboard",a);Press(s,a);Check(s.Primary.Device==InputDevice.Null&&s.Players.Count==2,"explicit keyboard mode stolen by first Start");BindingKind(s.Primary,true);
  Check(s.Primary.Actions.jump.Bindings.OfType<KeyBindingSource>().Single().Control==Key.Space,"P1 native keyboard binding lost");End(s);
 }
 static void HotPlug(){
  var s=Start("Auto");Check(s.Primary.Device==InputDevice.Null,"no-pad fallback missing");
  InputDevice a=Pad(),b=Pad();InputManager.Devices.Add(a);Press(s,a);
  Check(s.Players.Count==2&&s.Primary.Device==InputDevice.Null&&s.Players[1].Device==a,"late pad stole keyboard P1");Check(s.plugin.PrimaryDevice.Value=="Auto","joining pad saves accidental fixed preference");
  InputManager.Devices.Add(b);Press(s,b);Check(s.Players.Count==3&&s.Players[2].Device==b,"second guest pad does not join P3");End(s);
  s=Start("Auto");a=Pad();InputManager.Devices.Add(a);s.plugin.Panel=true;Press(s,a);Check(s.Players.Count==2&&s.Primary.Device==InputDevice.Null,"Start cannot add a pad while the group panel is open");
  s.plugin.Panel=false;s.CanJoin=false;InputManager.Devices.Add(b);Press(s,b);Check(s.Primary.Device==InputDevice.Null&&s.Players.Count==2,"pad assigned during unsafe loading/join state");
  s.CanJoin=true;MenuInput.Waiting=true;Press(s,b);MenuInput.Waiting=false;Check(s.Players.Count==2,"key capture joins a pad");
  ActionKeys.Capturing=1;Press(s,b);ActionKeys.Capturing=0;Check(s.Players.Count==2,"action binding joins a pad");End(s);
 }
 static void Swap(){
  InputDevice a=Pad("A"),b=Pad("B"),c=Pad("C");var s=Start("Auto",a,b,c);Press(s,b);Press(s,c);
  var p1=s.Primary;var p3=s.Players[2];p3.ProfileId="third";p3.Health=3;
  Check(s.AssignPrimary(c),"occupied controller rejected");
  Check(s.Primary==p1&&p3==s.Players[2]&&p1.Device==c&&p3.Device==a,"controller swap reordered characters");
  Check(p1.ProfileId=="primary"&&p1.Health==7&&p3.ProfileId=="third"&&p3.Health==3,"swap changes player identity or health");
  Check(s.plugin.PrimaryDevice.Value==Controls.Key(c),"manual pad preference not remembered");
  Check(GameManager.instance.inputHandler.inputActions==p1.Actions,"native input handler references destroyed P1 actions");
  for(int i=0;i<30;i++){Check(s.AssignPrimary(i%2==0?a:c),"repeat swap rejected");foreach(var p in s.Players)BindingKind(p,false);Check(s.Players.Select(p=>p.Device).Distinct().Count()==3,"one controller controls two players");}
  End(s);
  s=Start("Keyboard",a);Press(s,a);s.Join(InputDevice.Null);KeyboardMemory.Saved[2]=Key.Return;
  s.Players[2].Actions.attack.ClearBindings();s.Players[2].Actions.attack.AddBinding(new KeyBindingSource(Key.Return));var kept=s.Players[2].Actions;
  Check(s.AssignPrimary(a)&&s.Primary.Device==a&&s.Players[1].Device==InputDevice.Null,"keyboard/controller swap failed");
  Check(s.Players[2].Actions.attack.Bindings.OfType<KeyBindingSource>().Single().Control==Key.Return,"other keyboard player's saved binding lost");
  Check(s.Players[2].Actions==kept&&!kept.IsDestroyed,"P1 swap recreates unrelated keyboard controls");
  BindingKind(s.Primary,false);BindingKind(s.Players[1],true);Check(s.Players.Select(p=>p.Actions).Distinct().Count()==3,"swap shares actions");End(s);
 }
 static void Reconnect(){
  InputDevice a=Pad("A"),b=Pad("B");var s=Start("Auto",a,b);Press(s,b);var p2Actions=s.Players[1].Actions;var oldActions=s.Primary.Actions;
  a.IsAttached=false;InputManager.Devices.Remove(a);Controls.Pump(s.Primary,false);Check(s.Primary.InputBlocked&&!s.Primary.Connected,"disconnected P1 follows another pad");
  var replacement=Pad("A");InputManager.Devices.Add(replacement);s.Reconnect();
  Check(s.Primary.Device==replacement&&s.Primary.Actions!=oldActions&&oldActions.IsDestroyed,"P1 reconnect did not rebuild controller actions");
  Check(s.Players[1].Device==b&&s.Players[1].Actions==p2Actions,"reconnecting P1 steals P2 controls");
  Check(GameManager.instance.inputHandler.inputActions==s.Primary.Actions,"reconnect leaves destroyed native actions");BindingKind(s.Primary,false);Controls.Pump(s.Primary,false);Check(!s.Primary.InputBlocked,"reconnected P1 remains blocked");End(s);
  InputManager.Devices=new(){a};a.IsAttached=true;string saved=Controls.Key(a);s=Start(saved,b);Check(s.Primary.Device==InputDevice.Null&&s.Primary.DeviceKey==saved,"missing saved controller reservation lost");Press(s,b);
  s.Refresh();s.originalActions.jump.ClearBindings();s.originalActions.jump.AddBinding(new KeyBindingSource(Key.Return));s.Refresh();
  Check(s.Primary.DeviceKey==saved,"native settings refresh drops the missing controller reservation");
  InputManager.Devices.Add(a);s.Reconnect();Check(s.Primary.Device==a&&s.Players[1].Device==b,"saved controller does not recover after keyboard fallback");BindingKind(s.Primary,false);End(s);
 }
 static void Guards(){
  var a=Pad();var s=Start("Auto",a);for(int i=0;i<4;i++)s.Join(InputDevice.Null);
  Check(s.AssignPrimary(InputDevice.Null)&&s.Players.Count(p=>Controls.IsKeyboard(p.Device))==4&&s.Players[1].Device==a,"keyboard selection creates a fifth profile instead of swapping the occupied device");
  var detached=Pad();detached.IsAttached=false;Check(!s.AssignPrimary(detached)&&!s.AssignPrimary(null),"invalid controller assigned");
  var passive=Pad();passive.Passive=true;Check(!s.AssignPrimary(passive),"passive device assigned");End(s);
 }
 static void PersistentPorts(){
  var a=new UnityInputDevice{Name="Same",Meta="Xbox",JoystickId=1};var b=new UnityInputDevice{Name="Same",Meta="Xbox",JoystickId=2};
  var s=Start("Auto",a,b);Press(s,b);string saved=Controls.Key(a);
  var replacement=new UnityInputDevice{Name="Same",Meta="Xbox",JoystickId=1};
  a.IsAttached=false;InputManager.Devices.Remove(a);InputManager.Devices.Add(replacement);s.Reconnect();
  Check(Controls.Key(replacement)==saved&&replacement.GUID!=a.GUID,"persisted input key depends on random InControl GUID");
  Check(s.Primary.Device==replacement&&s.Players[1].Device==b,"identical-name pad reconnect steals another port");End(s);
  string legacy=Guid.NewGuid().ToString("N")+"|Same|1";
  s=Start(legacy,replacement,b);Check(s.Primary.Device==b&&s.plugin.PrimaryDevice.Value==Controls.Key(b),"legacy GUID key not upgraded to persistent port key");End(s);
  var native=new NativeInputDevice{Name="Native pad",Info=new NativeInfo{name="Xbox",location="usb-port-2"}};
  saved=Controls.Key(native);native.GUID=Guid.NewGuid();Check(Controls.Key(native)==saved,"native hardware location not retained");
  var first=Pad("Same");var second=Pad("Same");InputManager.Devices=new(){first,second};saved=Controls.Key(second);InputManager.Devices.Reverse();
  Check(Controls.Find(saved)==second,"reattached same-name device follows enumeration instead of its surviving GUID");
 }
 static void Lobby(){
  var a=Pad("A");var b=Pad("B");InputManager.Devices=new(){a,b};InputManager.ActiveDevice=a;
  var lobby=new LobbyRoster();lobby.Poll(false);Check(lobby.Count==0,"attached pads auto-join an empty lobby");
  b.CommandWasPressed=true;lobby.Poll(false);b.CommandWasPressed=false;
  Check(lobby.Count==1&&lobby.Primary.Device==b,"join order follows enumeration instead of pressed pad");
  a.CommandWasPressed=true;lobby.Poll(false);lobby.Poll(false);a.CommandWasPressed=false;
  Check(lobby.Count==2&&lobby.Members[1].Device==a,"repeat Start duplicates or reorders the lobby");
  var s=StartInput("Keyboard",lobby,false,a,b);Check(s.Primary.Device==b,"saved preference or active pad overrides explicit P1 join");
  lobby.Apply(s);lobby.Apply(s);Check(s.Players.Count==2&&s.Players[1].Device==a,"lobby handoff duplicates or swaps guests");End(s);
  lobby=new();lobby.Poll(true);b.CommandWasPressed=true;lobby.Poll(false);b.CommandWasPressed=false;
  s=StartInput("Auto",lobby,true,a,b);lobby.Apply(s);
  Check(s.Primary.Device==InputDevice.Null&&s.Players[1].Device==b,"controller activation stole keyboard lobby P1");End(s);
  s=StartInput("Auto",null,false,a,b);Press(s,b);Check(s.Primary.Device==InputDevice.Null&&s.Players[1].Device==b,"keyboard save entry was replaced by an idle connected pad");End(s);
  var port=new UnityInputDevice{Name="Same",Meta="DualSense",JoystickId=2};InputManager.Devices=new(){port};lobby=new();lobby.Join(port);port.IsAttached=false;
  lobby.Refresh();Check(!lobby.Primary.Connected,"missing lobby pad silently became keyboard");
  var replacement=new UnityInputDevice{Name="Same",Meta="DualSense",JoystickId=2};InputManager.Devices.Add(replacement);lobby.Refresh();
  Check(lobby.Primary.Device==replacement&&lobby.Primary.Connected,"lobby reconnect lost the original port");
  lobby.Clear();Check(lobby.Count==0,"returning to menu retains the last group");
  for(int i=0;i<8;i++)Check(lobby.Join(Pad("P"+i)),"eight-controller lobby needs a keyboard slot");
  Check(!lobby.Join(Pad("Ninth"))&&!lobby.Join(InputDevice.Null),"lobby exceeds eight players");
  lobby.RemoveLast();Check(lobby.Count==7,"remove last misses lobby reservation");
  lobby.AssignPrimary(InputDevice.Null);Check(lobby.Count==7&&lobby.Primary.Device==InputDevice.Null,"explicit lobby P1 assignment adds a phantom player");
 }
 static void ChangeLobbyPrimary(){
  var a=Pad("Host pad");var b=Pad("Guest pad");InputManager.Devices=new(){a,b};
  var lobby=new LobbyRoster();Check(lobby.AssignPrimary(InputDevice.Null)&&lobby.Count==1,"empty lobby cannot select keyboard P1");
  var primary=lobby.Primary;Check(lobby.AssignPrimary(a)&&lobby.Count==1&&lobby.Primary==primary,"choosing P1 pad adds/reorders players");
  lobby.Join(b);var guest=lobby.Members[1];Check(lobby.AssignPrimary(b)&&lobby.Count==2&&lobby.Primary==primary&&guest.Device==a,"lobby swap moves slot identity or duplicates pad");
  Check(lobby.AssignPrimary(InputDevice.Null)&&lobby.Count==2&&guest.Device==a,"return to keyboard moves guest");
  for(int i=0;i<20;i++){Check(lobby.AssignPrimary(b)&&lobby.AssignPrimary(InputDevice.Null)&&lobby.Count==2,"repeated P1 switching creates extra slots");}
  b.IsAttached=false;Check(!lobby.AssignPrimary(b)&&!lobby.AssignPrimary(null)&&lobby.Primary.Device==InputDevice.Null,"invalid menu device replaces P1");b.IsAttached=true;
  var s=StartInput("Auto",lobby,true,a,b);lobby.Apply(s);Check(!lobby.AssignPrimary(b),"applied lobby can mutate live roster");
  Check(s.Primary.Device==InputDevice.Null&&s.Players[1].Device==a,"switched menu devices lost during save handoff");
  var actions=s.Primary.Actions;Check(s.AssignPrimary(InputDevice.Null)&&s.Primary.Actions==actions&&!actions.IsDestroyed,"reselecting P1 device destroys active bindings");
  End(s);
 }
 static void StreamedControllers(){
  // Steam/Parsec deliver virtual gamepads through the same InControl ports.
  // This tests the consumer contract, not a live remote-streaming session.
  var pads=Enumerable.Range(1,8).Select(i=>new UnityInputDevice{Name="Xbox 360 Controller",Meta="XInput",JoystickId=i}).ToArray();
  var s=StartInput("Auto",null,false,pads);for(int i=0;i<7;i++)Press(s,pads[i]);
  Check(s.Players.Count==8&&s.Primary.Device==InputDevice.Null,"late stream controllers steal host keyboard or lose slots");
  var guest=s.Players[1];guest.ProfileId="stream-guest";guest.Health=2;
  Check(s.AssignPrimary(pads[0])&&guest.Device==InputDevice.Null&&s.Primary.Device==pads[0],"stream pad/host keyboard swap failed");
  Check(guest.ProfileId=="stream-guest"&&guest.Health==2,"stream swap replaces guest identity");
  Check(s.AssignPrimary(InputDevice.Null)&&s.Primary.Device==InputDevice.Null,"stream host cannot return to keyboard");
  var before=s.Primary.Actions;Check(s.AssignPrimary(pads[7])&&before.IsDestroyed,"host cannot select an unjoined stream pad at eight slots");
  var disconnected=pads[7];disconnected.IsAttached=false;InputManager.Devices.Remove(disconnected);Controls.Pump(s.Primary,false);
  var replacement=new UnityInputDevice{Name=disconnected.Name,Meta="XInput",JoystickId=8};InputManager.Devices.Add(replacement);s.Reconnect();
  Check(s.Primary.Device==replacement&&s.Players.Select(p=>p.Device).Where(d=>d!=InputDevice.Null).Distinct().Count()==8,"stream reconnect merges matching-name pads");
  Application.isFocused=false;Controls.Pump(s.Primary,false);Check(s.Primary.InputBlocked,"overlay focus loss leaves stream input held");Application.isFocused=true;Controls.Pump(s.Primary,false);
  Check(!s.Primary.InputBlocked,"returning from overlay permanently blocks stream input");End(s);
 }
 static void HealthFeedback(){
  var one=new HealthChangeFeedback();var two=new HealthChangeFeedback();one.Observe(5,2,5,10,true);two.Observe(3,0,5,10,true);
  Check(one.Fade(10)==0&&two.Fade(10)==0,"first HUD appearance flashes every mask");
  one.Observe(5,1,5,11,true);Check(one.Lost(6)&&!one.Lost(5)&&one.Count(11)==7,"lost blue mask disappears before its fade");
  Check(one.Fade(11)==1&&one.Fade(11.25f)>.45f&&one.Fade(11.5f)==0&&one.Count(11.5f)==6,"health fade does not settle within half a second");
  Check(two.Fade(11)==0,"P1 health change flashes P2");
  one.Observe(3,1,5,12,true);Check(one.Lost(3)&&one.Lost(4)&&!one.Lost(2),"multi-mask damage affects unchanged cells");
  one.Observe(4,1,5,12.2f,true);Check(one.Gained(3)&&!one.Gained(2)&&!one.Gained(5),"healing glows unchanged masks");
  one.Observe(4,1,5,12.3f,false);Check(one.Fade(12.3f)==0,"disabling effects leaves a HUD animation running");
  one.Observe(2,0,5,13,false);one.Observe(2,0,5,14,true);Check(one.Fade(14)==0,"re-enabling effects replays old damage");
  one.Observe(0,0,5,15,true);one.Observe(5,0,5,15.1f,true);Check(one.Gained(0)&&one.Gained(4),"respawn masks do not receive feedback");
 }
 static void StableBindings(){
  var a=Pad("Same pad");var b=Pad("Same pad");var s=Start("Keyboard",a,b);s.Join(InputDevice.Null);s.Join(InputDevice.Null);Press(s,a);
  var keyboards=s.Players.Skip(1).Where(p=>Controls.IsKeyboard(p.Device)).ToArray();var actions=keyboards.Select(p=>p.Actions).ToArray();var signatures=keyboards.Select(p=>Controls.Signature(p.Actions)).ToArray();
  Check(Controls.Label(a)!=Controls.Label(b)&&Controls.Key(a)!=Controls.Key(b),"identical pad names cannot be distinguished");
  Check(s.AssignPrimary(b),"cannot move P1 from keyboard to a free pad");
  for(int n=0;n<50;n++){
   Check(s.AssignPrimary(n%2==0?a:b),"repeated occupied pad selection failed");
   for(int i=0;i<keyboards.Length;i++)Check(keyboards[i].Actions==actions[i]&&!actions[i].IsDestroyed&&Controls.Signature(actions[i])==signatures[i],"P1 change mutates a guest's default keyboard keys");
  }
  var previousPad=s.Primary.Device;
  Check(s.AssignPrimary(InputDevice.Null)&&s.Primary.Device==InputDevice.Null&&keyboards[0].Device==previousPad,"choosing the occupied keyboard did not exchange devices");
  Check(s.Players.Count(p=>Controls.IsKeyboard(p.Device))==2&&keyboards[1].Actions==actions[1],"keyboard exchange recreates unrelated controls or adds a phantom player");
  s.Refresh();var before=s.Primary.Actions;
  s.originalActions.jump.ClearBindings();s.originalActions.jump.AddBinding(new KeyBindingSource(Key.Return));s.Refresh();
  Check(s.Primary.Actions!=before&&before.IsDestroyed&&GameManager.instance.inputHandler.inputActions==s.Primary.Actions,"native keyboard update leaves stale P1 action reference");
  Check(keyboards[1].Actions==actions[1],"native settings refresh recreates guest keyboard controls");
  Check(s.AssignPrimary(b),"cannot switch to pad after native keyboard update");before=s.Primary.Actions;string key=s.Primary.DeviceKey;
  s.originalActions.jump.ClearBindings();s.originalActions.jump.AddBinding(new KeyBindingSource(Key.Space));s.Refresh();
  Check(s.Primary.Actions==before&&!before.IsDestroyed,"keyboard-only setting change destroys unchanged pad controls");
  s.originalActions.Actions.Add(null);bool threw=false;
  try{Controls.RebindPrimary(s.Primary,s.originalActions,a);}catch(NullReferenceException){threw=true;}finally{s.originalActions.Actions.Remove(null);}
  Check(threw&&s.Primary.Actions==before&&!before.IsDestroyed&&s.Primary.Device==b&&s.Primary.DeviceKey==key,"failed control preparation destroys working assignment");
  var other=s.Players.First(p=>p.Device==a);var otherActions=other.Actions;string otherKey=other.DeviceKey;
  s.originalActions.Actions.Add(null);threw=false;
  try{s.AssignPrimary(a);}catch(NullReferenceException){threw=true;}finally{s.originalActions.Actions.Remove(null);}
  Check(threw&&s.Primary.Actions==before&&s.Primary.Device==b&&s.Primary.DeviceKey==key&&other.Actions==otherActions&&other.Device==a&&other.DeviceKey==otherKey&&!before.IsDestroyed&&!otherActions.IsDestroyed,"failed occupied-device exchange changes either player's working controls");
  GameManager.instance.inputHandler.inputActions=other.Actions;
  Check(s.AssignPrimary(a)&&GameManager.instance.inputHandler.inputActions==other.Actions&&!other.Actions.IsDestroyed,"occupied-device exchange leaves a destroyed guest action set in the native handler");
  End(s);
 }
 static void KeyboardProfiles(){
  var a=Pad("A");var b=Pad("B");var s=Start("Keyboard",a,b);s.Join(InputDevice.Null);s.Join(InputDevice.Null);Press(s,a);
  var retained=s.Players.Skip(1).Where(p=>Controls.IsKeyboard(p.Device)).ToArray();var actions=retained.Select(p=>p.Actions).ToArray();
  Check(s.AssignPrimary(b),"P1 cannot leave the keyboard");s.Join(InputDevice.Null);
  Check(s.Players.Last().KeyboardProfile==0&&s.Players.Last().Actions.left.Bindings.OfType<KeyBindingSource>().Single().Control==Key.A,"late keyboard player inherits keypad controls from its player number instead of its free layout");
  for(int i=0;i<retained.Length;i++)Check(retained[i].Actions==actions[i],"adding a keyboard player changes existing layouts");
  s.Join(InputDevice.Null);Check(s.Players.Last().KeyboardProfile==3&&s.Players.Last().Actions.left.Bindings.OfType<KeyBindingSource>().Single().Control==Key.Pad4,"fourth keyboard layout misses the physical keypad defaults");
  for(int n=0;n<80;n++){
   Check(s.AssignPrimary(n%2==0?InputDevice.Null:(n%4==1?a:b)),"keyboard/controller exchange refused");
   var keyboards=s.Players.Where(p=>Controls.IsKeyboard(p.Device)).ToArray();
   Check(keyboards.Length==4&&keyboards.Select(p=>p.KeyboardProfile).Distinct().Count()==4,"device exchange assigns the same default keyboard layout to two players");
   Check(s.Players.Where(p=>!Controls.IsKeyboard(p.Device)).Select(p=>p.Device).Distinct().Count()==s.Players.Count-keyboards.Length,"exchange duplicates a controller");
   Check(s.Players.Select(p=>p.Actions).Distinct().Count()==s.Players.Count&&s.Players.All(p=>!p.Actions.IsDestroyed),"exchange shares or destroys live controls");
   for(int i=0;i<retained.Length;i++)Check(retained[i].Actions==actions[i],"default keyboard exchange chooses an unrelated keyboard player");
  }
  End(s);
 }
 static void KeyboardRecovery(){
  var pad=Pad();var s=Start("Auto",pad);s.Join(InputDevice.Null);var p=s.Players[1];var actions=p.Actions;var jump=actions.jump;string original=Controls.Signature(actions);
  Check(!KeyboardBindingData.TryLoad(actions,"!not-base64!")&&Controls.Signature(actions)==original&&p.Actions==actions,"bad saved data leaves a keyboard player without controls");
  var candidate=new HeroActions();foreach(var a in candidate.Actions)a.AddBinding(new KeyBindingSource(Key.Return));candidate.jump.AddBinding(new DeviceBindingSource(InputControlType.Action1));
  Check(!KeyboardBindingData.TryLoad(actions,candidate.Save())&&Controls.Signature(actions)==original,"keyboard restore accepts controller bindings");candidate.Destroy();
  KeyboardMemory.Saved[p.Index]=Key.Return;actions.attack.ClearBindings();actions.attack.AddBinding(new KeyBindingSource(Key.Return));
  Check(Controls.ResetKeyboard(p,s.originalActions)&&p.Actions==actions&&actions.jump==jump&&!actions.IsDestroyed,"keyboard reset destroys live action/menu references");
  Check(actions.attack.Bindings.OfType<KeyBindingSource>().Single().Control==Key.Q&&KeyboardMemory.Saved[p.Index]==Key.Q,"keyboard reset reloads old saved controls or fails to save defaults");
  Controls.RebindPrimary(p,s.originalActions,pad);Controls.RebindPrimary(p,s.originalActions,InputDevice.Null);
  Check(p.Actions.attack.Bindings.OfType<KeyBindingSource>().Single().Control==Key.Q,"reset defaults disappear after switching devices");
  End(s);
 }
 static void Main(){Migration();Selection();EightControllers();HotPlug();Swap();Reconnect();PersistentPorts();Guards();Lobby();ChangeLobbyPrimary();StreamedControllers();StableBindings();KeyboardProfiles();KeyboardRecovery();HealthFeedback();Check(HeroActions.Created==HeroActions.Destroyed,"controller reassignment leaks action sets");Console.WriteLine("PASS "+checks+" production P1 controller, lobby join order, health feedback, eight-pad, keyboard, swap, binding and reconnect checks. Explicit host doubles; no physical-controller or native Unity gameplay test.");}
}
