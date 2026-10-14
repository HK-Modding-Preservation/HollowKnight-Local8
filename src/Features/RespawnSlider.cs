using UnityEngine;
namespace KO.HollowKnight8 {
internal static class RespawnSlider {
 internal static void Draw(Local8Runtime runtime,float width,ref float row){
  if(runtime==null||runtime.RespawnSeconds==null)return;
  float current=Mathf.Clamp(runtime.RespawnSeconds.Value,10f,60f);
  int seconds=Mathf.RoundToInt(current);
  MenuTheme.Text(new Rect(8f,row,width-16f,24f),RoleText.Get("respawn.duration")+": "+seconds+" "+RoleText.Get("respawn.seconds"),MenuTheme.Muted,14);
  bool enabled=GUI.enabled;
  try {
   GUI.enabled=enabled&&runtime.TimedRespawn.Value;
   float next=MenuTheme.Slider(new Rect(40f,row+31f,width-87f,24f),current,10f,60f);
   int selected=Mathf.RoundToInt(next);
   if(selected!=seconds)runtime.RespawnSeconds.Value=selected;
  }finally{GUI.enabled=enabled;}
  MenuTheme.Text(new Rect(8f,row+24f,28f,24f),"10",MenuTheme.Muted,12);
  MenuTheme.Text(new Rect(width-38f,row+24f,31f,24f),"60",MenuTheme.Muted,12);
  row+=59f;
 }
 internal static void Normalize(Local8Runtime runtime){if(runtime!=null&&runtime.RespawnSeconds!=null)runtime.RespawnSeconds.Value=Mathf.Clamp(runtime.RespawnSeconds.Value,10f,60f);}
}
}
