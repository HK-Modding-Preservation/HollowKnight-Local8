using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

// The approved sprites share one small embedded atlas. All GPU copies belong to this menu.
internal static class MenuArt
{
    internal const int Players=0, Game=1, Pvp=2, Settings=3, Controller=4, Keyboard=5,
        Header=6, Corner=7, Divider=8, Button=9, Selection=10, Track=11, Fill=12,
        Handle=13, Check=14, Close=15, Arrow=16;
    private struct Region
    {
        internal string Name;
        internal int X,Y,Width,Height,Left,Right,Top,Bottom;
        internal Region(string n,int x,int y,int w,int h,int l,int r,int t,int b)
        {Name=n;X=x;Y=y;Width=w;Height=h;Left=l;Right=r;Top=t;Bottom=b;}
    }
    private static readonly Region[] regions = {
        new Region("icon_players",444,116,64,64,0,0,0,0),
        new Region("icon_game",0,216,64,64,0,0,0,0),
        new Region("icon_pvp",68,216,64,64,0,0,0,0),
        new Region("icon_settings",136,216,64,64,0,0,0,0),
        new Region("icon_controller",204,216,64,48,0,0,0,0),
        new Region("icon_keyboard",272,216,64,48,0,0,0,0),
        new Region("ornament_header",0,0,512,112,0,0,0,0),
        new Region("ornament_corner",100,116,80,80,0,0,0,0),
        new Region("ornament_divider",0,284,384,64,0,0,0,0),
        new Region("frame_button",184,116,256,40,28,28,19,19),
        new Region("frame_selection",0,116,96,96,24,24,24,24),
        new Region("bar_track",0,352,320,20,12,12,9,9),
        new Region("bar_fill",0,376,320,16,10,10,7,7),
        new Region("bar_handle",324,376,24,32,0,0,0,0),
        new Region("mark_check",352,376,32,32,0,0,0,0),
        new Region("mark_close",388,376,32,32,0,0,0,0),
        new Region("mark_arrow",424,376,24,32,0,0,0,0),
    };
    private static readonly Texture2D[] textures = new Texture2D[regions.Length];
    private static readonly Texture2D[] steps = new Texture2D[2];
    private static Texture2D barFill;
    private static readonly List<Texture2D> surfaces = new List<Texture2D>();
    private static bool attempted, ready;

    internal static bool Ready {get {Load();return ready;}}
    private static Texture2D NewTexture(int w,int h,string name)
        => new Texture2D(w,h,TextureFormat.RGBA32,false) {name="Local8.Menu."+name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
    private static void Load()
    {
        if(attempted)return;
        attempted=true;Texture2D atlas=null;
        try
        {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Local8.UI.Atlas"))
            {
                if(stream==null)return;
                byte[] bytes=new byte[stream.Length];int count=0;
                while(count<bytes.Length){int n=stream.Read(bytes,count,bytes.Length-count);if(n==0)return;count+=n;}
                atlas=NewTexture(2,2,"atlas");
                if(!atlas.LoadImage(bytes)||atlas.width!=512||atlas.height!=512)return;
                for(int i=0;i<regions.Length;i++)
                {
                    Region r=regions[i];Texture2D t=NewTexture(r.Width,r.Height,r.Name);textures[i]=t;
                    t.SetPixels(atlas.GetPixels(r.X,atlas.height-r.Y-r.Height,r.Width,r.Height));
                    t.Apply(false,false);
                }
                steps[0]=StepTexture(false);steps[1]=StepTexture(true);
                barFill=Pill(1024,16,Color.white,"bar.fill");
                ready=true;
            }
        }
        catch(Exception){DestroyTextures();}
        finally{if(atlas)UnityEngine.Object.Destroy(atlas);}
    }
    internal static Texture2D Texture(int id){Load();return id>=0&&id<textures.Length?textures[id]:null;}
    internal static RectOffset Border(int id)
    {Region r=regions[id];return new RectOffset(r.Left,r.Right,r.Top,r.Bottom);}

    // Equal-width mathematical glyphs: no language/font dependency or extra PNG decode.
    private static Texture2D StepTexture(bool plus)
    {
        const int size=32;Color[] pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float px=Mathf.Abs(x+.5f-size*.5f),py=Mathf.Abs(y+.5f-size*.5f);
            float horizontal=CapsuleDistance(px,py);
            float distance=plus?Mathf.Min(horizontal,CapsuleDistance(py,px)):horizontal;
            pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(.5f-distance));
        }
        Texture2D result=NewTexture(size,size,plus?"step.plus":"step.minus");
        result.SetPixels(pixels);result.Apply(false,true);return result;
    }
    private static float CapsuleDistance(float along,float across)
    {
        float end=Mathf.Max(0,along-11f);
        return Mathf.Sqrt(end*end+across*across)-1.75f;
    }
    internal static void Step(Rect rect,bool plus,Color tint)
    {
        Load();Texture2D texture=steps[plus?1:0];if(!texture)return;
        Color old=GUI.color;
        try{GUI.color=tint;GUI.DrawTexture(rect,texture);}
        finally{GUI.color=old;}
    }

    // Keep the generated white frame intact; give its enclosed interior an opaque ink fill.
    internal static Texture2D Surface(int id,Color fill,Color edge,string name)
    {
        Texture2D source=Texture(id);if(!source)return null;
        int w=source.width,h=source.height;Color[] mask=source.GetPixels(),pixels=new Color[mask.Length];
        bool[] outside=new bool[mask.Length];Queue<int> queue=new Queue<int>();
        for(int x=0;x<w;x++){Enqueue(x,mask,outside,queue);Enqueue((h-1)*w+x,mask,outside,queue);}
        for(int y=0;y<h;y++){Enqueue(y*w,mask,outside,queue);Enqueue(y*w+w-1,mask,outside,queue);}
        while(queue.Count>0)
        {
            int i=queue.Dequeue(),x=i%w,y=i/w;
            if(x>0)Enqueue(i-1,mask,outside,queue);if(x<w-1)Enqueue(i+1,mask,outside,queue);
            if(y>0)Enqueue(i-w,mask,outside,queue);if(y<h-1)Enqueue(i+w,mask,outside,queue);
        }
        for(int i=0;i<pixels.Length;i++)
        {
            float a=mask[i].a*edge.a,b=outside[i]?0:fill.a*(1-a),sum=a+b;
            pixels[i]=sum>0?new Color((edge.r*a+fill.r*b)/sum,(edge.g*a+fill.g*b)/sum,(edge.b*a+fill.b*b)/sum,sum):Color.clear;
        }
        Texture2D result=NewTexture(w,h,name);result.SetPixels(pixels);result.Apply(false,true);surfaces.Add(result);return result;
    }
    private static void Enqueue(int i,Color[] mask,bool[] outside,Queue<int> queue)
    {if(!outside[i]&&mask[i].a<.06f){outside[i]=true;queue.Enqueue(i);}}
    internal static void Seal(){foreach(Texture2D t in textures)if(t)t.Apply(false,true);}

    internal static void Draw(Rect rect,int id,Color tint,bool flipX=false,bool flipY=false)
    {
        Texture2D texture=Texture(id);if(!texture||rect.width<=0||rect.height<=0)return;
        float scale=Mathf.Min(rect.width/texture.width,rect.height/texture.height);
        Rect fit=new Rect(rect.center.x-texture.width*scale*.5f,rect.center.y-texture.height*scale*.5f,texture.width*scale,texture.height*scale);
        Color old=GUI.color;
        try {GUI.color=tint;GUI.DrawTextureWithTexCoords(fit,texture,new Rect(flipX?1:0,flipY?1:0,flipX?-1:1,flipY?-1:1),true);}
        finally{GUI.color=old;}
    }
    internal static void Nine(Rect rect,int id,Color tint)
    {
        Texture2D texture=Texture(id);if(!texture)return;Region r=regions[id];
        Nine(rect,texture,new Rect(0,0,r.Width,r.Height),r.Left,r.Right,r.Top,r.Bottom,tint);
    }
    internal static void FillBar(Rect rect,Color tint,float fraction=1)
    {
        Load();fraction=Mathf.Clamp01(fraction);
        if(!barFill||rect.width<=0||rect.height<=0||fraction<=0)return;
        Color old=GUI.color;
        try
        {
            // One continuous strip, including its round caps. Crop the same
            // full-width UV field for progress; never join independently scaled
            // cap/center pieces or introduce a seam at a fractional GUI scale.
            GUI.color=tint;
            GUI.DrawTextureWithTexCoords(new Rect(rect.x,rect.y,rect.width*fraction,rect.height),barFill,new Rect(0,0,fraction,1),true);
        }
        finally{GUI.color=old;}
    }
    // Small code-drawn control surfaces avoid stretching the atlas's horizontal
    // diamond into a vertical thumb. White RGB also avoids dark seams when tinted.
    internal static Texture2D Pill(int width,int height,Color tint,string name)
    {
        Texture2D result=NewTexture(width,height,name);Color[] pixels=new Color[width*height];
        float radius=Mathf.Min(width,height)*.5f;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            float dx=Mathf.Max(0,Mathf.Abs(x+.5f-width*.5f)-(width*.5f-radius));
            float dy=Mathf.Max(0,Mathf.Abs(y+.5f-height*.5f)-(height*.5f-radius));
            Color c=tint;c.a*=Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy));pixels[y*width+x]=c;
        }
        result.SetPixels(pixels);result.Apply(false,true);surfaces.Add(result);return result;
    }
    private static void Nine(Rect rect,Texture2D texture,Rect source,float left,float right,float top,float bottom,Color tint)
    {
        if(rect.width<=0||rect.height<=0)return;
        float sx=left+right>rect.width?rect.width/(left+right):1,sy=top+bottom>rect.height?rect.height/(top+bottom):1;
        float dl=left*sx,dr=right*sx,dt=top*sy,db=bottom*sy;
        Color old=GUI.color;
        try
        {
            GUI.color=tint;
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            {
                float ox=x==0?0:x==1?dl:rect.width-dr,oy=y==0?0:y==1?dt:rect.height-db;
                float dw=x==0?dl:x==1?rect.width-dl-dr:dr,dh=y==0?dt:y==1?rect.height-dt-db:db;
                float ix=x==0?0:x==1?left:source.width-right,iy=y==0?0:y==1?top:source.height-bottom;
                float iw=x==0?left:x==1?source.width-left-right:right,ih=y==0?top:y==1?source.height-top-bottom:bottom;
                if(dw<=0||dh<=0||iw<=0||ih<=0)continue;
                GUI.DrawTextureWithTexCoords(new Rect(rect.x+ox,rect.y+oy,dw,dh),texture,
                    new Rect((source.x+ix)/texture.width,1-(source.y+iy+ih)/texture.height,iw/texture.width,ih/texture.height),true);
            }
        }
        finally{GUI.color=old;}
    }
    private static void DestroyTextures()
    {
        for(int i=0;i<textures.Length;i++){if(textures[i])UnityEngine.Object.Destroy(textures[i]);textures[i]=null;}
        for(int i=0;i<steps.Length;i++){if(steps[i])UnityEngine.Object.Destroy(steps[i]);steps[i]=null;}
        foreach(Texture2D t in surfaces)if(t)UnityEngine.Object.Destroy(t);surfaces.Clear();ready=false;
        barFill=null;
    }
    internal static void Release(){DestroyTextures();attempted=false;}
}
