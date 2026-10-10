using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IslandVoyage
{
    public sealed class VoyageHud
    {
        readonly GameBootstrap game;
        readonly RectTransform canvas;
        readonly Canvas display;
        readonly Font font;
        readonly Sprite rounded;
        readonly Color ink=Art.Hex("23474A"),cream=new Color(.99f,.97f,.90f,.95f),teal=Art.Hex("2D8074");
        readonly Text speed,prompt,detail,total,toast,targetTag;
        readonly RectTransform toastPanel,promptPanel;
        readonly GameObject pauseOverlay,bookOverlay;
        readonly Text[] recordCounts=new Text[8];
        readonly Text muteLabel;
        readonly Text clock,timeRate;
        readonly Text weather,seaReport,eventStatus,eventHint;
        readonly RectTransform fightBars;
        readonly Image tensionFill,staminaFill;
        readonly Text tensionValue,staminaValue;
        public Canvas Display=>display;
        public VoyageMinimap Minimap {get;}
        float toastUntil;
        public bool Scenic {get;private set;}
        public bool HasModal=>bookOverlay.activeSelf;
        public VoyageHud(GameBootstrap game)
        {
            this.game=game;font=Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded=RoundedSprite();
            var go=new GameObject("Ocean journal · Chinese interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var c=go.GetComponent<Canvas>();display=c;c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=20;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;canvas=go.GetComponent<RectTransform>();
            if(EventSystem.current==null)new GameObject("UI events",typeof(EventSystem),typeof(StandaloneInputModule));
            var title=Rect(canvas,"Voyage title",new Vector2(0,1),new Vector2(30,-24),new Vector2(300,84));
            var brand=Label(title,"潮汐群岛",30,new Vector2(0,-2),new Vector2(285,52),cream);brand.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(0,-2);
            var subtitle=Label(title,"ISLAND VOYAGE  /  海上的慢时光",14,new Vector2(2,-56),new Vector2(295,24),cream);subtitle.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(0,-1);
            Minimap=new VoyageMinimap(game,canvas,font,rounded);
            Button(canvas,"暂停",new Vector2(1,1),new Vector2(-28,-28),new Vector2(70,40),()=>game.SetPaused(!game.Paused));
            var mute=Button(canvas,"声音",new Vector2(1,1),new Vector2(-108,-28),new Vector2(70,40),()=>game.Effects.ToggleMute());muteLabel=mute.GetComponentInChildren<Text>();
            Button(canvas,"海景 H",new Vector2(1,1),new Vector2(-188,-28),new Vector2(85,40),ToggleScenic);
            var timePanel=Panel(canvas,"Time and camera",new Vector2(1,1),new Vector2(-28,-80),new Vector2(282,69),new Color(.99f,.97f,.9f,.82f));
            clock=Label(timePanel,"09:30",19,new Vector2(13,-3),new Vector2(175,31),ink);
            var rate=Button(timePanel,"时间×1",new Vector2(0,1),new Vector2(173,-6),new Vector2(98,28),game.Cycle.ChangeRate);timeRate=rate.GetComponentInChildren<Text>();
            Label(timePanel,"右键拖拽环绕 · 滚轮缩放 · C 复位",12,new Vector2(12,-40),new Vector2(265,23),teal);
            var conditions=Panel(canvas,"Weather and sea encounters",new Vector2(1,1),new Vector2(-28,-160),new Vector2(282,165),new Color(.99f,.97f,.9f,.87f));
            weather=Label(conditions,"晴朗",19,new Vector2(13,-6),new Vector2(254,28),ink);
            seaReport=Label(conditions,"",13,new Vector2(13,-35),new Vector2(254,24),teal);
            eventStatus=Label(conditions,"海况平稳",16,new Vector2(13,-64),new Vector2(254,26),ink);
            eventHint=Label(conditions,"",12,new Vector2(13,-94),new Vector2(254,24),teal);
            Button(conditions,"切换天气 Y",new Vector2(0,1),new Vector2(12,-128),new Vector2(126,28),game.CycleWeather);
            Button(conditions,"自然奇遇 N",new Vector2(0,1),new Vector2(145,-128),new Vector2(125,28),()=>{if(!game.Paused && !HasModal)game.Events.PreviewNext();});
            var controls=Panel(canvas,"Navigation",new Vector2(0,0),new Vector2(28,24),new Vector2(316,88),new Color(.99f,.97f,.9f,.83f));
            speed=Label(controls,"平静航行",19,new Vector2(17,-8),new Vector2(285,29),ink);
            Label(controls,"W 前进 · S 倒航 · A/D 转舵",15,new Vector2(17,-40),new Vector2(285,24),teal);
            Label(controls,"E 抛竿 / 提竿 · 空格收线 · Q 收竿",13,new Vector2(17,-63),new Vector2(285,22),ink);
            var journal=Panel(canvas,"Catch summary",new Vector2(1,0),new Vector2(-28,24),new Vector2(180,78),new Color(.99f,.97f,.9f,.83f));
            total=Label(journal,"鱼获  0 条",20,new Vector2(15,-6),new Vector2(150,30),ink);
            Button(journal,"鱼获记录  B",new Vector2(0,1),new Vector2(13,-42),new Vector2(154,28),OpenBook);
            promptPanel=Panel(canvas,"Fishing prompt",new Vector2(.5f,0),new Vector2(0,24),new Vector2(500,88),new Color(.99f,.97f,.9f,.94f));
            prompt=Label(promptPanel,"停船准备钓鱼",20,new Vector2(16,-7),new Vector2(468,34),ink);prompt.alignment=TextAnchor.MiddleCenter;
            detail=Label(promptPanel,"E 抛竿 · 空格 / 左键收线 · Q 收竿",14,new Vector2(16,-45),new Vector2(468,25),teal);detail.alignment=TextAnchor.MiddleCenter;
            fightBars=Rect(promptPanel,"Line tension and fish stamina",new Vector2(0,1),new Vector2(16,-77),new Vector2(468,79));
            tensionFill=Meter(fightBars,"鱼线张力",new Vector2(0,0),teal,out tensionValue,true);
            staminaFill=Meter(fightBars,"鱼的耐力",new Vector2(0,-33),Art.Hex("758D9E"),out staminaValue,false);
            fightBars.gameObject.SetActive(false);
            toastPanel=Panel(canvas,"Voyage feedback",new Vector2(.5f,0),new Vector2(0,210),new Vector2(600,50),new Color(.12f,.28f,.29f,.88f));
            toast=Label(toastPanel,"",18,new Vector2(15,-8),new Vector2(570,34),cream);toast.alignment=TextAnchor.MiddleCenter;
            targetTag=Label(canvas,"",17,Vector2.zero,new Vector2(210,30),Color.white);targetTag.alignment=TextAnchor.MiddleCenter;targetTag.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(1,-1);
            pauseOverlay=Overlay("Paused");var pause=Panel(pauseOverlay.transform,"Pause card",new Vector2(.5f,.5f),Vector2.zero,new Vector2(460,360),cream);
            Label(pause,"暂泊片刻",33,new Vector2(34,-30),new Vector2(390,48),ink);
            Label(pause,"8 种鱼 · 海洋动物偶遇 · 7 座岛屿",16,new Vector2(35,-88),new Vector2(390,32),teal);
            Button(pause,"继续航行",new Vector2(0,1),new Vector2(35,-155),new Vector2(390,45),()=>game.SetPaused(false));
            Button(pause,"小船返回出发点",new Vector2(0,1),new Vector2(35,-218),new Vector2(390,45),game.ResetBoat);
            Label(pause,"P / Esc 继续",15,new Vector2(35,-296),new Vector2(390,25),teal);
            pauseOverlay.SetActive(false);
            bookOverlay=Overlay("Catch journal");var book=Panel(bookOverlay.transform,"Journal card",new Vector2(.5f,.5f),Vector2.zero,new Vector2(530,670),cream);
            Label(book,"鱼获记录",32,new Vector2(32,-25),new Vector2(460,48),ink);
            Label(book,"等口提竿 · 稳住张力 · 记录每种鱼的最大重量",16,new Vector2(33,-87),new Vector2(464,29),teal);
            for(int i=0;i<8;i++)
            {
                var row=Panel(book,"Species row "+i,new Vector2(0,1),new Vector2(28,-145-i*52),new Vector2(474,45),new Color(.84f,.89f,.8f,.35f));
                Label(row,AnimalModels.Species[i].Name,20,new Vector2(18,-5),new Vector2(325,34),ink);
                recordCounts[i]=Label(row,"0 条",14,new Vector2(337,-1),new Vector2(120,43),teal);recordCounts[i].alignment=TextAnchor.MiddleRight;
            }
            Button(book,"返回海湾",new Vector2(0,1),new Vector2(30,-589),new Vector2(470,48),CloseModal);bookOverlay.SetActive(false);
        }
        RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(anchor.x,anchor.y);r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {var r=Rect(parent,name,anchor,position,size);var image=r.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;return r;}
        Image Meter(Transform parent,string name,Vector2 position,Color color,out Text value,bool safeZone)
        {
            var row=Rect(parent,name,new Vector2(0,1),position,new Vector2(468,30));
            Label(row,name,13,new Vector2(0,0),new Vector2(82,27),ink);
            var track=Panel(row,"Meter track",new Vector2(0,1),new Vector2(88,-8),new Vector2(299,12),new Color(.15f,.31f,.32f,.18f));
            if(safeZone)Panel(track,"Comfortable tension",new Vector2(0,1),new Vector2(299*.28f,0),new Vector2(299*.50f,12),new Color(.35f,.72f,.52f,.32f));
            var fill=Panel(track,"Value",new Vector2(0,1),Vector2.zero,new Vector2(299,12),color).GetComponent<Image>();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillAmount=0;
            value=Label(row,"0%",13,new Vector2(398,0),new Vector2(70,27),ink);value.alignment=TextAnchor.MiddleRight;return fill;
        }
        Text Label(Transform parent,string value,int size,Vector2 position,Vector2 dimensions,Color color)
        {
            var r=Rect(parent,"Text",new Vector2(0,1),position,dimensions);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.alignment=TextAnchor.MiddleLeft;return t;
        }
        GameObject Button(Transform parent,string label,Vector2 anchor,Vector2 position,Vector2 size,Action action)
        {
            var r=Panel(parent,label,anchor,position,size,teal);r.GetComponent<Image>().raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();var colors=b.colors;colors.highlightedColor=new Color(.82f,1,.88f);colors.pressedColor=new Color(.64f,.8f,.7f);b.colors=colors;b.onClick.AddListener(()=>action());
            var t=Label(r,label,16,Vector2.zero,size,cream);var tr=t.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;t.alignment=TextAnchor.MiddleCenter;return r.gameObject;
        }
        GameObject Overlay(string name)
        {
            var r=Panel(canvas,name,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.04f,.12f,.15f,.45f));r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.GetComponent<Image>().raycastTarget=true;return r.gameObject;
        }
        public void Toast(string message,float duration){toast.text=message;toastUntil=Time.unscaledTime+duration;toastPanel.gameObject.SetActive(true);}
        public void ToggleScenic(){Scenic=!Scenic;display.enabled=!Scenic;}
        public void ToggleBook(){if(HasModal)CloseModal();else OpenBook();}
        public void ShowPause(bool value){pauseOverlay.SetActive(value);if(value){Scenic=false;display.enabled=true;pauseOverlay.transform.SetAsLastSibling();}}
        void OpenBook(){Scenic=false;display.enabled=true;bookOverlay.SetActive(true);bookOverlay.transform.SetAsLastSibling();}
        public void CloseModal()=>bookOverlay.SetActive(false);
        public void Tick()
        {
            Minimap.Tick();
            string mode=game.Boat.Speed>=VoyageRules.FastSpeed?"快速航行":game.Boat.Speed<=VoyageRules.SlowSpeed?"平静航行":"巡航中";
            speed.text=mode+"  "+game.Boat.Speed.ToString("0.0")+" m/s";speed.color=game.Boat.Speed>=VoyageRules.FastSpeed?Art.Hex("AE7049"):ink;
            total.text="鱼获  "+game.TotalCatch+" 条";muteLabel.text=game.Effects.Muted?"静音":"声音";
            clock.text=game.Cycle.Clock+"  ·  "+(game.Cycle.Daylight<.2f?"夜航":game.Cycle.Hour<12?"上午":"午后");timeRate.text=game.Cycle.Rate==0?"时间暂停":"时间×"+game.Cycle.Rate;
            weather.text=game.Weather.Name+"  ·  "+(game.Weather.WaveScale>2?"风浪较大":game.Weather.WaveScale>1.3f?"海面起伏":"海面平缓");
            seaReport.text=$"风 {WeatherSystem.Wind.magnitude:0.0} m/s · 能见度约 {game.Weather.Visibility:0} m";
            var stage=game.Events.Stage;
            eventStatus.text=game.Events.Name+(stage==SeaEventStage.Warning?$" · {game.Events.Remaining:0}s 后到来":stage==SeaEventStage.Active?$" · 余 {game.Events.Remaining:0}s":stage==SeaEventStage.Recovery?" · 正在消退":"");
            eventStatus.color=game.Events.Kind==SeaEventKind.RogueSwell?Art.Hex("A45138"):ink;
            eventHint.text=game.Events.Kind==SeaEventKind.LuminousSea && game.Cycle.Daylight>.4f?"白天光芒微弱 · 夜航时观察更佳":game.Events.Hint;
            for(int i=0;i<8;i++)recordCounts[i].text=game.Catches[i]+" 条"+(game.BestWeights[i]>0?$"\n最大 {game.BestWeights[i]:0.00} kg":"");
            toastPanel.gameObject.SetActive(Time.unscaledTime<toastUntil);
            var fishing=game.Fishing;var fight=fishing.Fight;
            bool fighting=fishing.Stage==FishingStage.Fighting;
            promptPanel.gameObject.SetActive(!game.Paused && !HasModal && (fishing.Active || game.Cooldown>0 || (game.Nearby!=null && game.Nearby.DistanceFrom(game.Boat.Root.position)<16)));
            promptPanel.sizeDelta=new Vector2(500,fighting?160:88);fightBars.gameObject.SetActive(fighting);prompt.color=ink;
            switch(fishing.Stage)
            {
                case FishingStage.Casting:prompt.text="抛竿入水…";detail.text="摆竿、出线 · 稍等浮漂落水";break;
                case FishingStage.Waiting:prompt.text="静候鱼口 · "+fishing.Target.Name;detail.text="留意浮漂 · 明显下沉后按 E · Q 收竿";break;
                case FishingStage.Nibbling:prompt.text="鱼正在试饵…";detail.text="先别提竿，等浮漂明显下沉";break;
                case FishingStage.Bite:prompt.text=$"咬钩！[ E ] 提竿 · {fishing.BiteRemaining:0.0}s";prompt.color=Art.Hex("AE643D");detail.text="及时提竿，把握这一口！";break;
                case FishingStage.Hooking:prompt.text="上钩了！稳住鱼竿";detail.text="空格 / 左键收线 · 鱼冲刺时松开泄力";break;
                case FishingStage.Fighting:
                    prompt.text=fight.Tension>.78f?"张力偏高！松开收线":fight.Tension<.24f?"鱼线偏松，收线稳住":fight.Surging?"鱼在冲刺 · 留意张力":"稳住收线 · 正在拉近";
                    detail.text=$"{fishing.Target.Name} · 剩余 {fight.LineOut:0.0} m · 按住空格 / 左键收线，松开泄力";
                    tensionFill.fillAmount=fight.Tension;tensionFill.color=fight.Tension>.78f?Art.Hex("C16B49"):fight.Tension<.24f?Art.Hex("D1AD56"):teal;
                    staminaFill.fillAmount=fight.Stamina;tensionValue.text=Mathf.RoundToInt(fight.Tension*100)+"%";staminaValue.text=Mathf.RoundToInt(fight.Stamina*100)+"%";break;
                case FishingStage.Landing:prompt.text="收鱼入船…";detail.text="鱼已疲惫，正在提起鱼获";break;
                case FishingStage.Retrieving:prompt.text="正在收竿…";detail.text="收回鱼线与浮漂";break;
                case FishingStage.Cooldown:prompt.text="整理钓具";detail.text=$"{game.Cooldown:0.0} 秒后可再次抛竿";break;
                default:
                    if(game.CanCast){prompt.text="[ E ] 抛竿 · "+game.Nearby.Name;detail.text=$"鱼群深约 {game.Nearby.AverageDepth:0.0} m · 停船更易观察鱼口";}
                    else if(game.Nearby!=null && game.Nearby.Mood!=SchoolMood.Calm){prompt.text="鱼群受惊了";detail.text="保持距离，等鱼群恢复后抛竿";}
                    else {prompt.text="靠近鱼群，准备钓鱼";detail.text=game.Boat.Speed>VoyageRules.SlowSpeed?"按住 Shift 减速，避免惊扰鱼群":"根据小地图寻找鱼群，12 米内按 E 抛竿";}break;
            }
            bool floatTag=fishing.Active && fishing.Stage!=FishingStage.Casting && fishing.Stage!=FishingStage.Retrieving;
            targetTag.gameObject.SetActive((game.Nearby!=null || floatTag) && !game.Paused && !HasModal);
            if(game.Nearby!=null || floatTag)
            {
                Vector3 position=floatTag?fishing.Bobber:game.Nearby.NearestFish(game.Boat.Root.position);
                position.y=OceanSimulation.Height(position,game.WorldTime)+.5f;
                Vector3 point=game.View.WorldToScreenPoint(position);
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,point,null,out Vector2 local))targetTag.rectTransform.localPosition=new Vector3(local.x-105,local.y+16,0);
                targetTag.text=floatTag?(fishing.Stage==FishingStage.Bite?"咬钩！ E 提竿":"浮漂"):game.Nearby.Name+"群  ·  "+(game.Nearby.Mood==SchoolMood.Calm?"巡游":"受惊");
                targetTag.gameObject.SetActive(point.z>0 && !game.Paused && !HasModal);
            }
        }
        static Sprite RoundedSprite()
        {
            const int size=48;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);tex.name="Procedural rounded UI";tex.filterMode=FilterMode.Bilinear;Color[] pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {float dx=Mathf.Max(0,12-x,x-35),dy=Mathf.Max(0,12-y,y-35);float alpha=Mathf.Clamp01(12.5f-Mathf.Sqrt(dx*dx+dy*dy));pixels[y*size+x]=new Color(1,1,1,alpha);}
            tex.SetPixels(pixels);tex.Apply();return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(13,13,13,13));
        }
    }
}
