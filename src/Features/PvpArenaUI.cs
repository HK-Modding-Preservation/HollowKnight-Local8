using UnityEngine;
namespace KO.HollowKnight8 {
internal static class PvpArenaUI {
 static string T(string key){return PvpArena.Text(key);}
 static bool health,abilities,players;
 static GUIStyle Label {get{return MenuTheme.Label;}}
 static GUIStyle Button {get{return MenuTheme.ButtonStyle;}}
 internal static void Draw(Local8Runtime runtime,CoopSession session,float width,ref float row){
  var c=PvpArena.Options;MenuTheme.Section(T("title"),width,ref row,2);
  bool prior=GUI.enabled;
  try {
   GUI.enabled=prior&&!PvpMatch.Running;
   float inset=MenuTheme.FoldInset(width);
   Toggle(T("charms"),ref c.AllowCharms,width,ref row);
   GUI.Label(new Rect(5,row,width-10,25),T("destination"),Label);row+=29;
   float gridHeight=Mathf.CeilToInt(PvpArenaCatalog.Keys.Length/2f)*48f;
   int picked=GUI.SelectionGrid(new Rect(5,row,width-10,gridHeight),c.Arena,Names(),2,Button);
   if(picked!=c.Arena){c.Arena=picked;PvpArena.SaveOptions();}row+=gridHeight+10;
   if(c.Arena>0){
    GUI.Label(new Rect(7,row,width-14,44),T("details"),new GUIStyle(MenuTheme.Small){wordWrap=true});row+=52;
    GUI.Label(new Rect(5,row,width-10,25),T("presets"),Label);row+=30;
    string[] presets={T("native"),T("twenty"),T("magic"),T("air")};
    for(int i=0;i<4;i++)if(MenuTheme.Button(new Rect(5+i*(width-10)/4,row,(width-10)/4-4,31),presets[i],Button))Preset(c,i);row+=42;
    Fold(UiLocalization.Get("menu.health_soul"),ref health,width,ref row);
    if(health){
     float start=row;
     Number(T("masks"),ref c.Masks,0,50,width,ref row,true,inset);
     Number(T("blue"),ref c.BlueMasks,0,50,width,ref row,false,inset);
     Number(T("capacity"),ref c.SoulCapacity,1,999,width,ref row,false,inset);
     Number(T("soul"),ref c.Soul,0,c.SoulCapacity,width,ref row,false,inset);
     Number(T("reserve"),ref c.Reserve,0,99,width,ref row,false,inset);
     Toggle(T("infinitesoul"),ref c.InfiniteSoul,width,ref row,inset);
     MenuTheme.FoldGuide(start,row,inset);row+=5;
    }
   }
   // This duel rule also applies to current-room matches; temporary arena
   // abilities below remain specific to an arena trip.
   Fold(UiLocalization.Get("menu.abilities"),ref abilities,width,ref row);
   if(abilities){
    float start=row;
    Toggle(UiLocalization.Get("pvp.spells_only"),ref Local8Mod.Settings.PvpSpellsOnly,width,ref row,inset,UiLocalization.Get("pvp.spells_only_tip"));
    if(Local8Mod.Settings.PvpSpellsOnly)
     Number(UiLocalization.Get("pvp.soul_regeneration"),ref Local8Mod.Settings.PvpSoulRegeneration,1,99,width,ref row,false,inset+8);
    if(c.Arena>0){
     Toggle(T("wings"),ref c.InfiniteWings,width,ref row,inset);
     Toggle(T("dash"),ref c.InfiniteDash,width,ref row,inset);
     Toggle(T("abilities"),ref c.AllAbilities,width,ref row,inset);
     Toggle(T("healing"),ref c.AllowHealing,width,ref row,inset);
     Number(T("cooldown"),ref c.DashCooldown,-1,2000,width,ref row,true,inset);
     Number(T("hazard"),ref c.HazardMasks,1,20,width,ref row,false,inset);
     float inner=width-inset;
     GUI.Label(new Rect(5+inset,row+4,inner*.31f,29),T("spells"),Label);
     c.SpellLevel=GUI.SelectionGrid(new Rect(inset+inner*.32f,row,inner*.67f,32),c.SpellLevel+1,new[]{T("native"),T("off"),"1","2"},4,Button)-1;row+=43;
    }
    MenuTheme.FoldGuide(start,row,inset);row+=5;
   }
   if(c.Arena>0){
    Fold(T("perplayer"),ref players,width,ref row);
    if(players){
     float start=row;
     foreach(var p in session.Players)Number("P"+(p.Index+1)+"  "+T("masks"),ref c.PlayerMasks[p.Index],0,50,width,ref row,true,inset);
     MenuTheme.FoldGuide(start,row,inset);row+=5;
    }
   }
  }
  finally {GUI.enabled=prior;}
  if(PvpArena.Active&&MenuTheme.Button(new Rect(5,row,width-10,36),T("return"),Button))PvpArena.Back();row+=44;
  if(c.Arena>0&&!PvpArena.Active&&MenuTheme.Button(new Rect(5,row,width-10,36),T("enter"),Button))PvpArena.StartRequest();row+=44;
  c.Normalize();PvpArena.SaveOptions();
 }
 static string[] Names(){var a=new string[PvpArenaCatalog.Keys.Length];for(int i=0;i<a.Length;i++)a[i]=T(PvpArenaCatalog.Keys[i]);return a;}
 static void Fold(string label,ref bool value,float width,ref float row){if(MenuTheme.Fold(new Rect(5,row,width-10,36),label,value))value=!value;row+=43;}
 static void Toggle(string label,ref bool value,float width,ref float row,float inset=0,string tip=""){if(MenuTheme.Toggle(new Rect(5+inset,row,width-inset-10,39),label,value,tip))value=!value;row+=45;}
 static void Number(string label,ref int n,int min,int max,float width,ref float row,bool native=false,float inset=0){
  float inner=width-inset;
  GUI.Label(new Rect(8+inset,row,inner-20,25),label+": "+(native&&n==min?T("native"):n.ToString()),Label);row+=26;
  float v=MenuTheme.Slider(new Rect(18+inset,row,inner-178,24),n,min,max);n=Mathf.RoundToInt(v);
  if(MenuTheme.Step(new Rect(width-148,row-5,65,34),false))n=Mathf.Max(min,n-1);
  if(MenuTheme.Step(new Rect(width-73,row-5,65,34),true))n=Mathf.Min(max,n+1);row+=37;
 }
 static void Preset(PvpArenaOptions c,int i){Local8Mod.Settings.PvpSpellsOnly=i==4;Local8Mod.Settings.PvpSoulRegeneration=PvpSpellOnly.Rate(Local8Mod.Settings.PvpSoulRegeneration);c.Masks=i==1?20:0;c.BlueMasks=0;c.SoulCapacity=99;c.Soul=99;c.Reserve=0;c.InfiniteSoul=i==2;c.InfiniteWings=i==3;c.InfiniteDash=i==3;c.AllAbilities=i>1;c.SpellLevel=i==2||i==4?2:-1;c.AllowHealing=true;c.DashCooldown=-1;c.HazardMasks=1;for(int p=0;p<8;p++)c.PlayerMasks[p]=0;}
}
}
