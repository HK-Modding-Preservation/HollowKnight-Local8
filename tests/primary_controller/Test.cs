using System;
using System.Linq;
using InControl;
using KO.HollowKnight8;

static class Test {
 static int checks;
 static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
 static InputDevice Pad(string name="Pad")=>new InputDevice{Name=name};
 static CoopSession Start(string preference,params InputDevice[] pads){
  InputManager.Devices=pads.ToList();InputManager.ActiveDevice=pads.FirstOrDefault()??InputDevice.Null;KeyboardMemory.Saved.Clear();
  var vanilla=new HeroActions();
  foreach(var a in vanilla.Actions){a.AddBinding(new KeyBindingSource(Key.Return));a.AddBinding(new MouseBindingSource(1));a.AddBinding(new DeviceBindingSource(InputControlType.Action1));}
  vanilla.jump.ClearBindings();vanilla.jump.AddBinding(new KeyBindingSource(Key.Space));vanilla.jump.AddBinding(new DeviceBindingSource(InputControlType.Action4));
  var runtime=new Local8Runtime();runtime.PrimaryDevice.Value=preference;Local8Runtime.Self=runtime;
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
  Check(PrimaryController.Resolve("Auto",InputManager.Devices,passive)==a,"passive controller assigned");
  Check(PrimaryController.Resolve("Auto",new[]{gone,passive},gone)==InputDevice.Null,"detached controller assigned");
  Check(PrimaryController.Resolve(Controls.Key(b),InputManager.Devices,a)==b,"specific controller preference ignored");
  Check(Controls.Key(a)!=Controls.Key(b),"same-name controllers share identity");
  var s=Start("Keyboard",a);Press(s,a);Check(s.Primary.Device==InputDevice.Null&&s.Players.Count==2,"explicit keyboard mode stolen by first Start");BindingKind(s.Primary,true);
  Check(s.Primary.Actions.jump.Bindings.OfType<KeyBindingSource>().Single().Control==Key.Space,"P1 native keyboard binding lost");End(s);
 }
 static void HotPlug(){
  var s=Start("Auto");Check(s.Primary.Device==InputDevice.Null,"no-pad fallback missing");
  InputDevice a=Pad(),b=Pad();InputManager.Devices.Add(a);Press(s,a);
  Check(s.Players.Count==1&&s.Primary.Device==a,"late first pad joins P2 instead of P1");Check(s.plugin.PrimaryDevice.Value=="Auto","auto claim saves accidental fixed preference");
  InputManager.Devices.Add(b);Press(s,b);Check(s.Players.Count==2&&s.Players[1].Device==b,"second pad does not join P2");End(s);
  s=Start("Auto");a=Pad();InputManager.Devices.Add(a);s.plugin.Panel=true;Press(s,a);Check(s.Players.Count==1&&s.Primary.Device==InputDevice.Null,"Start changes roster behind F8");
  s.plugin.Panel=false;s.CanJoin=false;Press(s,a);Check(s.Primary.Device==InputDevice.Null,"pad assigned during unsafe loading/join state");End(s);
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
  Check(s.AssignPrimary(a)&&s.Primary.Device==a&&s.Players[1].Device==InputDevice.Null,"keyboard/controller swap failed");
  Check(s.Players[2].Actions.attack.Bindings.OfType<KeyBindingSource>().Single().Control==Key.Return,"other keyboard player's saved binding lost");
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
  InputManager.Devices.Add(a);s.Reconnect();Check(s.Primary.Device==a&&s.Players[1].Device==b,"saved controller does not recover after keyboard fallback");BindingKind(s.Primary,false);End(s);
 }
 static void Guards(){
  var a=Pad();var s=Start("Auto",a);for(int i=0;i<4;i++)s.Join(InputDevice.Null);
  var actions=s.Primary.Actions;Check(!s.AssignPrimary(InputDevice.Null)&&s.Primary.Actions==actions,"fifth keyboard player accepted");
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
 }
 static void Main(){Migration();Selection();EightControllers();HotPlug();Swap();Reconnect();PersistentPorts();Guards();Check(HeroActions.Created==HeroActions.Destroyed,"controller reassignment leaks action sets");Console.WriteLine("PASS "+checks+" production P1 controller, eight-pad, keyboard, swap, binding and reconnect checks. Explicit host doubles; no physical-controller or native Unity gameplay test.");}
}
