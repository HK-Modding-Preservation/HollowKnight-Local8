using System;
using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

// Screen UI only: this theme never touches scene renderers, materials or player input.
internal static class MenuTheme
{
    internal static readonly Color Ink = new Color(.014f,.018f,.028f,.995f);
    internal static readonly Color Paper = new Color(.93f,.94f,.97f);
    internal static readonly Color Muted = new Color(.65f,.68f,.75f);
    internal static readonly Color Accent = new Color(.72f,.85f,.96f);
    private static GUISkin skin;
    private static GUIStyle label,small,heading,button,chosen,navigation,selectedNavigation,tooltip,card,selectedCard,sliderInput,thumbInput,iconButton,fold,stepButton;
    private static readonly Dictionary<int,GUIStyle> textStyles = new Dictionary<int,GUIStyle>();
    private static readonly Dictionary<int,GUIStyle> centeredStyles = new Dictionary<int,GUIStyle>();
    private static Font font;
    private static string language;
    private static float nextFontSearch;

    internal static GUISkin Skin {get{Prepare();return skin;}}
    internal static GUIStyle Label {get{Prepare();return label;}}
    internal static GUIStyle Small {get{Prepare();return small;}}
    internal static GUIStyle Heading {get{Prepare();return heading;}}
    internal static GUIStyle ButtonStyle {get{Prepare();return button;}}
    internal static GUIStyle TooltipStyle {get{Prepare();return tooltip;}}

    internal static void Prepare()
    {
        if(!skin)
        {
            skin=UnityEngine.Object.Instantiate(GUI.skin);skin.hideFlags=HideFlags.HideAndDontSave;
            label=new GUIStyle(GUI.skin.label){fontSize=16,alignment=TextAnchor.MiddleLeft,richText=false,wordWrap=true,clipping=TextClipping.Clip,padding=new RectOffset(0,0,0,0)};
            label.normal.textColor=Paper;
            small=new GUIStyle(label){fontSize=13};small.normal.textColor=Muted;
            heading=new GUIStyle(label){fontSize=22,fontStyle=FontStyle.Normal};
            button=new GUIStyle(GUI.skin.button){fontSize=15,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Normal,richText=false,wordWrap=true,clipping=TextClipping.Clip,padding=new RectOffset(22,22,5,1),fixedHeight=0,fixedWidth=0};
            if(MenuArt.Ready)
            {
                Texture2D normal=MenuArt.Surface(MenuArt.Button,new Color(.034f,.040f,.056f),new Color(.47f,.50f,.58f,.88f),"button.normal");
                Texture2D hover=MenuArt.Surface(MenuArt.Button,new Color(.07f,.085f,.108f),Paper,"button.hover");
                Texture2D active=MenuArt.Surface(MenuArt.Button,new Color(.09f,.13f,.17f),Accent,"button.selected");
                button.border=MenuArt.Border(MenuArt.Button);
                SetStates(button,normal,hover,active);
                Texture2D plainCard=MenuArt.Surface(MenuArt.Selection,new Color(.025f,.030f,.044f),new Color(.35f,.38f,.45f,.72f),"card.normal");
                Texture2D activeCard=MenuArt.Surface(MenuArt.Selection,new Color(.045f,.061f,.085f),new Color(.61f,.65f,.72f,.9f),"card.selected");
                card=new GUIStyle(button){border=MenuArt.Border(MenuArt.Selection)};SetStates(card,plainCard,activeCard,activeCard);
                selectedCard=new GUIStyle(card);SetState(selectedCard.normal,activeCard,Paper);
                skin.verticalScrollbar=new GUIStyle(GUIStyle.none){fixedWidth=16,fixedHeight=0,border=new RectOffset(4,4,4,4),margin=new RectOffset(0,0,0,0)};
                Texture2D track=MenuArt.Pill(8,16,new Color(.22f,.25f,.32f,.9f),"scroll.track");
                SetStates(skin.verticalScrollbar,track,track,track);
                skin.verticalScrollbarThumb=new GUIStyle(GUIStyle.none){fixedWidth=16,fixedHeight=0,border=new RectOffset(5,5,6,6),margin=new RectOffset(0,0,0,0),padding=new RectOffset(0,0,0,0)};
                Texture2D scrollThumb=MenuArt.Pill(12,28,new Color(.68f,.75f,.84f),"scroll.thumb");
                Texture2D scrollHover=MenuArt.Pill(12,28,Paper,"scroll.hover");
                SetStates(skin.verticalScrollbarThumb,scrollThumb,scrollHover,scrollHover);
                MenuArt.Seal();
            }
            else {card=new GUIStyle(button);selectedCard=new GUIStyle(card);}
            chosen=new GUIStyle(button);CopyState(button.onNormal,chosen.normal);
            stepButton=new GUIStyle(button){padding=new RectOffset(0,0,0,0),wordWrap=false};
            foreach(GUIStyleState state in new[]{stepButton.normal,stepButton.hover,stepButton.active,stepButton.focused,stepButton.onNormal,stepButton.onHover,stepButton.onActive,stepButton.onFocused})state.textColor=Color.clear;
            navigation=new GUIStyle(button){alignment=TextAnchor.MiddleLeft,fontSize=17,padding=new RectOffset(46,17,4,2)};
            navigation.normal.background=null;navigation.normal.textColor=Muted;
            fold=new GUIStyle(button){alignment=TextAnchor.MiddleLeft,padding=new RectOffset(34,22,5,1)};
            selectedNavigation=new GUIStyle(navigation);CopyState(button.onNormal,selectedNavigation.normal);
            iconButton=new GUIStyle(GUIStyle.none){padding=new RectOffset(0,0,0,0)};
            tooltip=new GUIStyle(label){fontSize=14,padding=new RectOffset(14,14,11,11),alignment=TextAnchor.UpperLeft};
            // Keep Unity's slider input/drag/keyboard handling; the art is drawn separately.
            sliderInput=new GUIStyle(GUIStyle.none){fixedHeight=24,margin=new RectOffset(0,0,0,0)};
            thumbInput=new GUIStyle(GUIStyle.none){fixedHeight=24,fixedWidth=24,margin=new RectOffset(0,0,0,0)};
            skin.label=label;skin.button=button;
            skin.verticalScrollbarUpButton=new GUIStyle(GUIStyle.none){fixedHeight=0};
            skin.verticalScrollbarDownButton=new GUIStyle(GUIStyle.none){fixedHeight=0};
        }
        string lang=RoleText.Language;
        if(language!=lang||(!font&&Time.unscaledTime>=nextFontSearch))
        {
            language=lang;nextFontSearch=Time.unscaledTime+5f;
            string sample=UiLocalization.Get("menu.font_sample")+UiLocalization.Get("tab.players")+UiLocalization.Get("respawn.label");
            int best=int.MinValue;Font picked=null;
            foreach(Font candidate in Resources.FindObjectsOfTypeAll<Font>())
            {
                if(!candidate)continue;int missing=0;
                foreach(char c in sample)if(!char.IsWhiteSpace(c)&&!candidate.HasCharacter(c))missing++;
                string name=candidate.name.ToLowerInvariant();int score=-missing*100;
                if(name.Contains("perpetua"))score+=40;else if(name.Contains("trajan"))score+=30;else if(name.Contains("noto")||name.Contains("cjk"))score+=20;
                if(score>best){best=score;picked=candidate;}
            }
            font=picked;Font selected=font?font:skin.font;
            foreach(GUIStyle style in new[]{label,small,heading,button,chosen,navigation,selectedNavigation,tooltip,card,selectedCard,fold})style.font=selected;
            foreach(GUIStyle style in textStyles.Values)style.font=selected;
            foreach(GUIStyle style in centeredStyles.Values)style.font=selected;
        }
    }
    private static void SetState(GUIStyleState state,Texture2D background,Color tint){state.background=background;state.textColor=tint;}
    private static void CopyState(GUIStyleState source,GUIStyleState target){target.background=source.background;target.textColor=source.textColor;}
    private static void SetStates(GUIStyle style,Texture2D normal,Texture2D hover,Texture2D active)
    {
        SetState(style.normal,normal,Paper);SetState(style.hover,hover,Color.white);SetState(style.active,active,Color.white);SetState(style.focused,hover,Color.white);
        SetState(style.onNormal,active,Color.white);SetState(style.onHover,active,Color.white);SetState(style.onActive,active,Color.white);SetState(style.onFocused,active,Color.white);
    }
    internal static void Fill(Rect rect,Color tint)
    {
        Color old=GUI.color;try{GUI.color=tint;GUI.DrawTexture(rect,Texture2D.whiteTexture);}finally{GUI.color=old;}
    }
    internal static void Border(Rect rect,Color tint)
    {
        Fill(new Rect(rect.x,rect.y,rect.width,1),tint);Fill(new Rect(rect.x,rect.yMax-1,rect.width,1),tint);
        Fill(new Rect(rect.x,rect.y,1,rect.height),tint);Fill(new Rect(rect.xMax-1,rect.y,1,rect.height),tint);
    }
    internal static void Frame(Rect rect)
    {
        Prepare();Fill(rect,Ink);Color rim=new Color(.56f,.59f,.66f,.52f);
        Fill(new Rect(rect.x+70,rect.y+7,Mathf.Max(0,rect.width-140),1),rim);
        Fill(new Rect(rect.x+70,rect.yMax-8,Mathf.Max(0,rect.width-140),1),rim);
        Fill(new Rect(rect.x+10,rect.y+70,1,Mathf.Max(0,rect.height-140)),rim);
        Fill(new Rect(rect.xMax-11,rect.y+70,1,Mathf.Max(0,rect.height-140)),rim);
        for(int i=0;i<4;i++)
        {
            bool right=(i&1)!=0,bottom=(i&2)!=0;
            MenuArt.Draw(new Rect(right?rect.xMax-77:rect.x+3,bottom?rect.yMax-77:rect.y+3,74,74),MenuArt.Corner,Paper,right,bottom);
        }
    }
    internal static void HeaderArt(Rect rect)=>MenuArt.Draw(rect,MenuArt.Header,Paper);

    internal static float FoldInset(float width)=>Mathf.Clamp(width*.035f,12f,24f);
    internal static void FoldGuide(float top,float bottom,float inset)
    {
        if(bottom<=top)return;
        Fill(new Rect(5+inset*.45f,top+3,1,Mathf.Max(0,bottom-top-10)),new Color(Muted.r,Muted.g,Muted.b,.25f));
    }
    internal static void Divider(Rect rect)=>MenuArt.Draw(rect,MenuArt.Divider,new Color(.68f,.71f,.78f,.78f));
    internal static void Glyph(Rect rect,int id,Color tint){Prepare();MenuArt.Draw(rect,Mathf.Clamp(id,0,3),tint);}
    internal static void DeviceGlyph(Rect rect,bool keyboard,Color tint){Prepare();MenuArt.Draw(rect,keyboard?MenuArt.Keyboard:MenuArt.Controller,tint);}
    internal static void Mark(Rect rect,Color tint){Prepare();MenuArt.Draw(rect,MenuArt.Handle,tint);}
    // Native fonts differ by language. Fit long button labels without changing their hit area.
    internal static bool Button(Rect rect,string value,GUIStyle style)=>Button(rect,new GUIContent(value),style);
    internal static bool Button(Rect rect,GUIContent content,GUIStyle style)
    {
        int old=style.fontSize;
        try
        {
            if(content.text.Length>0&&style.wordWrap)
                while(style.fontSize>12&&style.CalcHeight(content,rect.width)>rect.height)style.fontSize--;
            return GUI.Button(rect,content,style);
        }
        finally{style.fontSize=old;}
    }
    internal static bool Choice(Rect rect,GUIContent content,bool selected)
    {
        Prepare();bool clicked=Button(rect,content,selected?chosen:button);
        if(selected&&rect.width>75)Mark(new Rect(rect.x+7,rect.center.y-7,8,14),Accent);
        return clicked;
    }
    internal static bool Step(Rect rect,bool increase)
    {
        Prepare();bool clicked=Button(rect,increase?"+":"-",stepButton);
        float size=Mathf.Min(18,Mathf.Min(rect.width-20,rect.height-12));
        if(size>0)MenuArt.Step(new Rect(rect.center.x-size*.5f,rect.center.y-size*.5f,size,size),increase,GUI.enabled?Paper:Muted);
        return clicked;
    }
    internal static bool Card(Rect rect,GUIContent content,bool selected,Color tint)
    {
        Prepare();bool clicked=Button(rect,content,selected?selectedCard:card);
        if(selected)MenuArt.Nine(rect,MenuArt.Selection,tint);
        return clicked;
    }
    internal static bool Nav(Rect rect,string value,string tip,int icon,bool selected)
    {
        Prepare();bool clicked=Button(rect,new GUIContent(value,tip),selected?selectedNavigation:navigation);
        Glyph(new Rect(rect.x+15,rect.center.y-12,24,24),icon,selected?Paper:Muted);
        return clicked;
    }
    internal static bool IconButton(Rect rect,int icon,string tip,bool flip=false)
    {
        Prepare();bool clicked=Button(rect,new GUIContent("",tip),iconButton);
        Color tint=GUI.enabled?Paper:Muted;
        if(Event.current!=null&&rect.Contains(Event.current.mousePosition))Fill(rect,new Color(.13f,.16f,.20f,.60f));
        MenuArt.Draw(new Rect(rect.x+5,rect.y+5,rect.width-10,rect.height-10),icon,tint,flip);
        return clicked;
    }
    internal static bool Fold(Rect rect,string value,bool open)
    {
        Prepare();bool clicked=Button(rect,new GUIContent(value),fold);
        MenuArt.Draw(new Rect(rect.x+11,rect.center.y-9,14,18),MenuArt.Arrow,open?Accent:Muted,open);
        return clicked;
    }
    internal static bool Toggle(Rect rect,string value,bool enabled,string tip="")
    {
        Prepare();bool clicked=Button(rect,new GUIContent("",tip),button);
        float stateWidth=Mathf.Min(82,rect.width*.28f);
        GUIContent caption=new GUIContent(value.Trim().TrimEnd(':','：'),tip);
        Rect textRect=new Rect(rect.x+26,rect.y+6,Mathf.Max(20,rect.width-stateWidth-62),rect.height-8);
        int oldSize=label.fontSize;
        try{while(label.fontSize>12&&label.CalcHeight(caption,textRect.width)>textRect.height)label.fontSize--;GUI.Label(textRect,caption,label);}
        finally{label.fontSize=oldSize;}
        MenuArt.Draw(new Rect(rect.xMax-stateWidth-18,rect.center.y-10,20,20),enabled?MenuArt.Check:MenuArt.Close,enabled?Accent:new Color(.52f,.55f,.63f,.6f));
        Centered(new Rect(rect.xMax-stateWidth+8,rect.y+6,stateWidth-33,rect.height-8),UiLocalization.Get(enabled?"common.yes":"common.no"),enabled?Paper:Muted,13);
        return clicked;
    }
    internal static float Slider(Rect rect,float value,float min,float max,Color? tint=null)
    {
        Prepare();float next=GUI.HorizontalSlider(rect,value,min,max,sliderInput,thumbInput);
        Color rail=new Color(.68f,.71f,.78f,GUI.enabled?.82f:.36f),fill=tint??Accent;
        if(!GUI.enabled)fill.a*=.36f;
        // Continuous cached surfaces: the old sliced atlas frame had a dark
        // join at its cap, even when the progress strip itself was seamless.
        MenuArt.FillBar(new Rect(rect.x+4,rect.center.y-7,rect.width-8,14),rail);
        MenuArt.FillBar(new Rect(rect.x+6,rect.center.y-5,rect.width-12,10),new Color(.045f,.055f,.075f,GUI.enabled?1:.7f));
        float interval=Mathf.Max(0,rect.width-24),fraction=max>min?Mathf.Clamp01((next-min)/(max-min)):0;
        float end=rect.x+12+interval*fraction;
        // A visible unfilled rail, rather than a transparent/black hole.
        MenuArt.FillBar(new Rect(rect.x+12,rect.center.y-3,interval,6),new Color(.32f,.35f,.42f,GUI.enabled?.9f:.4f));
        if(fraction>0)MenuArt.FillBar(new Rect(rect.x+12,rect.center.y-3,interval,6),fill,fraction);
        MenuArt.Draw(new Rect(end-9,rect.center.y-12,18,24),MenuArt.Handle,GUI.enabled?Paper:Muted);
        return next;
    }
    internal static void Section(string value,float width,ref float row,int icon=-1)
    {
        Prepare();if(row>0)row+=10;
        float offset=icon>=0?35:5;
        if(icon>=0)Glyph(new Rect(5,row+1,23,23),icon,Paper);
        GUI.Label(new Rect(offset,row,width-offset-10,27),value,heading);
        Divider(new Rect(5,row+29,Mathf.Min(width-10,335),22));row+=48;
    }
    private static GUIStyle TextStyle(int size,bool center)
    {
        var cache=center?centeredStyles:textStyles;GUIStyle style;
        if(!cache.TryGetValue(size,out style))
        {
            style=new GUIStyle(label){fontSize=size,alignment=center?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft};cache.Add(size,style);
        }
        return style;
    }
    internal static void Text(Rect rect,string value,Color? tint=null,int size=0)
    {
        Prepare();GUIStyle style=size==0?label:TextStyle(size,false);Color old=style.normal.textColor;
        try{style.normal.textColor=tint??Paper;GUI.Label(rect,value,style);}finally{style.normal.textColor=old;}
    }
    internal static float Paragraph(float x,float y,float width,string value,Color? tint=null,int size=13)
    {
        Prepare();GUIStyle style=TextStyle(size,false);
        float height=Mathf.Max(size*1.5f,style.CalcHeight(new GUIContent(value),width));
        Text(new Rect(x,y,width,height),value,tint,size);return height;
    }
    internal static void Centered(Rect rect,string value,Color? tint=null,int size=15)
    {
        Prepare();GUIStyle style=TextStyle(size,true);Color old=style.normal.textColor;
        try{style.normal.textColor=tint??Paper;GUI.Label(rect,value,style);}finally{style.normal.textColor=old;}
    }
    internal static void Release()
    {
        MenuArt.Release();if(skin)UnityEngine.Object.Destroy(skin);skin=null;
        label=small=heading=button=chosen=navigation=selectedNavigation=tooltip=card=selectedCard=sliderInput=thumbInput=iconButton=fold=stepButton=null;
        textStyles.Clear();centeredStyles.Clear();font=null;language=null;nextFontSearch=0;
    }
}
