using UnityEngine;
using UnityEngine.UI;

namespace IslandVoyage
{
    // North-up chart: shared world layout, no second camera or duplicated scene rendering.
    public sealed class VoyageMinimap
    {
        readonly GameBootstrap game;
        readonly RectTransform map,boat;
        readonly Image[] schools,wildlife,migration;
        readonly Text position;
        readonly Color calm=new Color(.98f,.78f,.39f),frightened=new Color(.98f,.43f,.33f);
        float nextRefresh;
        public Vector2 BoatMapPosition=>boat.anchoredPosition;
        public float BoatHeading=>boat.localEulerAngles.z;
        public int SchoolMarkerCount=>schools.Length;
        public Vector2 MapSize=>map.sizeDelta;

        public VoyageMinimap(GameBootstrap game,RectTransform canvas,Font font,Sprite rounded)
        {
            this.game=game;
            var panel=Rect(canvas,"Archipelago minimap",new Vector2(0,1),new Vector2(28,-128),new Vector2(260,280));
            var background=panel.gameObject.AddComponent<Image>();background.sprite=rounded;background.type=Image.Type.Sliced;background.color=new Color(.045f,.12f,.17f,.94f);background.raycastTarget=true;
            Label(panel,font,"群岛航图",new Vector2(13,-7),new Vector2(170,25),17,new Color(.93f,.94f,.85f));
            Label(panel,font,"北 N ↑",new Vector2(199,-9),new Vector2(55,22),13,new Color(.72f,.89f,.84f));
            map=Rect(panel,"North-up ocean chart",new Vector2(0,1),new Vector2(12,-38),new Vector2(236,236*WorldLayout.HalfHeight/WorldLayout.HalfWidth));
            var texture=map.gameObject.AddComponent<RawImage>();texture.texture=ChartTexture();texture.raycastTarget=false;
            var dots=CircleSprite();schools=new Image[game.Schools.Count];wildlife=new Image[game.Animals.Count];
            for(int i=0;i<schools.Length;i++)schools[i]=Marker(map,"Fish school "+i,dots,new Vector2(4.5f,4.5f),calm);
            for(int i=0;i<wildlife.Length;i++)wildlife[i]=Marker(map,"Wildlife encounter "+i,dots,new Vector2(7,7),new Color(.61f,.91f,.94f));
            migration=new Image[8];
            for(int i=0;i<8;i++)migration[i]=Marker(map,"Migrating shoal "+i,dots,new Vector2(6,6),new Color(.1f,1,.85f));
            boat=Marker(map,"Player boat heading",ArrowSprite(),new Vector2(14,18),Color.white).rectTransform;
            Label(panel,font,"▲ 小船",new Vector2(13,-241),new Vector2(67,18),12,Color.white);
            Label(panel,font,"• 鱼群",new Vector2(80,-241),new Vector2(67,18),12,calm);
            Label(panel,font,"• 大型动物",new Vector2(147,-241),new Vector2(102,18),12,new Color(.61f,.91f,.94f));
            position=Label(panel,font,"",new Vector2(13,-259),new Vector2(240,18),12,new Color(.67f,.81f,.83f));Tick();
        }
        public static Vector2 NormalizedPosition(Vector3 point)=>new Vector2(Mathf.Clamp01((point.x+WorldLayout.HalfWidth)/(WorldLayout.HalfWidth*2)),Mathf.Clamp01((point.z+WorldLayout.HalfHeight)/(WorldLayout.HalfHeight*2)));
        Vector2 MapPoint(Vector3 point)=>Vector2.Scale(NormalizedPosition(point),map.sizeDelta);
        public void Tick()
        {
            boat.anchoredPosition=MapPoint(game.Boat.Root.position);boat.localRotation=Quaternion.Euler(0,0,-game.Boat.Root.eulerAngles.y);
            if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;
            for(int i=0;i<schools.Length;i++)
            {
                var school=game.Schools[i];var marker=schools[i];marker.gameObject.SetActive(school.ActiveCount>0);
                marker.rectTransform.anchoredPosition=MapPoint(school.Center);marker.color=school.Mood==SchoolMood.Calm?calm:frightened;
                marker.rectTransform.sizeDelta=Vector2.one*(school==game.Nearby?7:4.5f);
            }
            for(int i=0;i<wildlife.Length;i++)
            {var animal=game.Animals[i];wildlife[i].gameObject.SetActive(animal.Active);wildlife[i].rectTransform.anchoredPosition=MapPoint(animal.Root.position);}
            for(int i=0;i<migration.Length;i++)
            {
                bool visible=game.Events.MigrationVisible && i<game.Events.MigrationSchools.Count;
                migration[i].gameObject.SetActive(visible);
                if(visible)migration[i].rectTransform.anchoredPosition=MapPoint(game.Events.MigrationSchools[i].Center);
            }
            position.text=$"航向 {game.Boat.Root.eulerAngles.y:000}°   X {game.Boat.Root.position.x:0} / Z {game.Boat.Root.position.z:0}";
        }
        static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor;rect.pivot=anchor;rect.anchoredPosition=offset;rect.sizeDelta=size;return rect;
        }
        static Text Label(Transform parent,Font font,string text,Vector2 offset,Vector2 size,int fontSize,Color color)
        {
            var label=Rect(parent,"Chart label",new Vector2(0,1),offset,size).gameObject.AddComponent<Text>();
            label.font=font;label.text=text;label.fontSize=fontSize;label.color=color;label.raycastTarget=false;label.alignment=TextAnchor.MiddleLeft;return label;
        }
        static Image Marker(Transform parent,string name,Sprite sprite,Vector2 size,Color color)
        {
            var rect=Rect(parent,name,Vector2.zero,Vector2.zero,size);rect.pivot=Vector2.one*.5f;
            var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;return image;
        }
        static Texture2D ChartTexture()
        {
            const int width=384,height=325;var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Shared-layout island chart",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[width*height];Color deep=Art.Hex("153D56"),shallow=Art.Hex("4A9D97"),sand=Art.Hex("DECE9E"),land=Art.Hex("667C62");
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                var p=new Vector3(((float)x/(width-1)*2-1)*WorldLayout.HalfWidth,0,((float)y/(height-1)*2-1)*WorldLayout.HalfHeight);
                float d=WorldLayout.ShoreDistance(p);Color color=d<0?Color.Lerp(sand,land,Mathf.InverseLerp(2,9,-d)):Color.Lerp(shallow,deep,Mathf.SmoothStep(0,1,d/36));
                float grid=Mathf.Min(Mathf.Abs(p.x-Mathf.Round(p.x/40)*40),Mathf.Abs(p.z-Mathf.Round(p.z/40)*40));
                if(d>0 && grid<.55f)color=Color.Lerp(color,Color.white,.10f);pixels[y*width+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        static Sprite CircleSprite()=>Icon("Chart dot",(x,y)=>Mathf.Clamp01((.85f-Mathf.Sqrt(x*x+y*y))*16));
        static Sprite ArrowSprite()=>Icon("Bow direction",(x,y)=>y>-.7f && y<.9f && Mathf.Abs(x)<(.9f-y)*.48f && !(y<-.22f && Mathf.Abs(x)<(-.22f-y)*.48f)?1:0);
        static Sprite Icon(string name,System.Func<float,float,float> alpha)
        {
            const int size=32;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear};var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=new Color(1,1,1,alpha((x+.5f)/size*2-1,(y+.5f)/size*2-1));
            texture.SetPixels(pixels);texture.Apply(false,true);return Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f);
        }
    }
}
