using HutongGames.PlayMaker.Actions;
namespace KO.HollowKnight8 {
// Direct native action entry covers data writes outside WorldRouting.RunFsm,
// and generic CallMethod/SendMessage control calls. Native actions always run.
internal static class ShadeRitualTriggers {
 static bool installed;
 internal static void Install(){if(installed)return;installed=true;
  On.HutongGames.PlayMaker.Actions.SetPlayerDataBool.OnEnter+=Flag;
  On.HutongGames.PlayMaker.Actions.SetPlayerDataInt.OnEnter+=Level;
  On.HutongGames.PlayMaker.Actions.SetFsmBool.OnEnter+=Zone;
  On.HeroController.RelinquishControl+=Control;
  On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float+=Animation;
  On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimation.OnEnter+=PlayAction;
  On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimationV2.OnEnter+=PlayActionV2;
  On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimationWithEvents.OnEnter+=PlayActionEvents;
 }
 internal static void Uninstall(){if(!installed)return;installed=false;
  On.HutongGames.PlayMaker.Actions.SetPlayerDataBool.OnEnter-=Flag;
  On.HutongGames.PlayMaker.Actions.SetPlayerDataInt.OnEnter-=Level;
  On.HutongGames.PlayMaker.Actions.SetFsmBool.OnEnter-=Zone;
  On.HeroController.RelinquishControl-=Control;
  On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float-=Animation;
  On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimation.OnEnter-=PlayAction;
  On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimationV2.OnEnter-=PlayActionV2;
  On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimationWithEvents.OnEnter-=PlayActionEvents;
 }
 static void Flag(On.HutongGames.PlayMaker.Actions.SetPlayerDataBool.orig_OnEnter orig,SetPlayerDataBool a){
  if(a.boolName!=null&&a.value!=null){ShadeCloakRitual.NativeAction(a.Fsm,a.boolName.Value,a.value.Value?1:0);SharedAcquisition.NativeAction(a.Fsm,a.boolName.Value,a.value.Value?1:0);}orig(a);
 }
 static void Level(On.HutongGames.PlayMaker.Actions.SetPlayerDataInt.orig_OnEnter orig,SetPlayerDataInt a){
  if(a.intName!=null&&a.value!=null){ShadeCloakRitual.NativeAction(a.Fsm,a.intName.Value,a.value.Value);SharedAcquisition.NativeAction(a.Fsm,a.intName.Value,a.value.Value);}orig(a);
 }
 static void Animation(On.tk2dSpriteAnimator.orig_Play_tk2dSpriteAnimationClip_float_float orig,tk2dSpriteAnimator animator,tk2dSpriteAnimationClip clip,float time,float fps){if(!SharedAcquisition.Replaying){SharedAcquisition.AnimationStarting(animator,clip);ShadeCloakRitual.AnimationStarting(animator,clip);}orig(animator,clip,time,fps);}
 static void PlayAction(On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimation.orig_OnEnter orig,Tk2dPlayAnimation a){var before=SharedAcquisition.AnimationSource;try{SharedAcquisition.AnimationSource=a.Fsm;SharedAcquisition.AnimationAction(a);orig(a);}finally{SharedAcquisition.AnimationSource=before;}}
 static void PlayActionV2(On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimationV2.orig_OnEnter orig,Tk2dPlayAnimationV2 a){var before=SharedAcquisition.AnimationSource;try{SharedAcquisition.AnimationSource=a.Fsm;SharedAcquisition.AnimationAction(a);orig(a);}finally{SharedAcquisition.AnimationSource=before;}}
 static void PlayActionEvents(On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimationWithEvents.orig_OnEnter orig,Tk2dPlayAnimationWithEvents a){var before=SharedAcquisition.AnimationSource;try{SharedAcquisition.AnimationSource=a.Fsm;SharedAcquisition.AnimationAction(a);orig(a);}finally{SharedAcquisition.AnimationSource=before;}}
 static void Zone(On.HutongGames.PlayMaker.Actions.SetFsmBool.orig_OnEnter orig,SetFsmBool a){ShadeShriekZone.Action(a);orig(a);}
 static void Control(On.HeroController.orig_RelinquishControl orig,HeroController h){ShadeCloakRitual.ControlTaken(h);SharedAcquisition.ControlTaken(h);orig(h);}
}
}
