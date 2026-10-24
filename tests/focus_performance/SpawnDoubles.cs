using UnityEngine;
namespace HutongGames.PlayMaker {
 public class Fsm { public GameObject GameObject; }
}
namespace HutongGames.PlayMaker.Actions {
 public class SpawnObjectFromGlobalPool { public HutongGames.PlayMaker.Fsm Fsm; }
 public class CreateObject { public HutongGames.PlayMaker.Fsm Fsm; public HutongGames.PlayMaker.FsmGameObject storeObject; }
}
namespace On.HutongGames.PlayMaker.Actions {
 public static class SpawnObjectFromGlobalPool { public delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool action); }
 public static class CreateObject { public delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.CreateObject action); }
}
namespace KO.HollowKnight8 {
 internal static class SpellSafety {
  internal static void Pool(On.HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool.orig_OnEnter orig, HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool action)=>orig(action);
  internal static void Create(On.HutongGames.PlayMaker.Actions.CreateObject.orig_OnEnter orig, HutongGames.PlayMaker.Actions.CreateObject action)=>orig(action);
 }
 internal static class PlayerContext { internal static PlayerSlot Current; }
 internal static class WeaverlingLifetime { internal static void Spawned(GameObject result,PlayerSlot owner){} }
}
