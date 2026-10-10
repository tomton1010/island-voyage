using UnityEngine;

namespace IslandVoyage
{
    public enum FishingStage { Idle, Casting, Waiting, Nibbling, Bite, Hooking, Fighting, Landing, Retrieving, Cooldown }

    public sealed class FishingController
    {
        readonly GameBootstrap game;
        readonly System.Random random;
        public FishingPresentation Visual {get;}
        public FishingStage Stage {get;private set;}
        public FishSchool Target {get;private set;}
        public FishingFight Fight {get;private set;}
        public Vector3 Bobber {get;private set;}
        public Vector3 Aim=>Stage==FishingStage.Casting?castTo:Bobber;
        public float StageTime {get;private set;}
        public float Weight {get;private set;}
        public bool Reeling {get;private set;}
        public bool Active=>Stage!=FishingStage.Idle && Stage!=FishingStage.Cooldown;
        public float Cooldown=>Stage==FishingStage.Cooldown?Mathf.Max(0,VoyageRules.CastCooldown-StageTime):0;
        public bool CanStart=>Stage==FishingStage.Idle && game.Nearby!=null && VoyageRules.CanFish(game.Boat.Speed,game.Nearby.DistanceFrom(game.Boat.Root.position),game.Nearby.Mood,game.Nearby.ActiveCount,0);
        public float BiteRemaining=>Stage==FishingStage.Bite?Mathf.Max(0,VoyageRules.BiteWindow-StageTime):0;
        Vector3 castFrom,castTo,retrieveFrom,landingFrom,fightDirection;
        bool launched;
        float waitDuration;
        public FishingController(GameBootstrap game,bool deterministic=false)
        {this.game=game;random=deterministic?new System.Random(8701):new System.Random();Visual=new FishingPresentation(game,this);}
        void Change(FishingStage stage){Stage=stage;StageTime=0;}
        public void PressAction()
        {
            if(game.Paused || game.Hud.HasModal)return;
            if(Stage==FishingStage.Bite)
            {
                if(!Target.TryHook(Bobber)){Retrieve("鱼已游离，收竿再试",false);return;}
                float distance=Vector2.Distance(new Vector2(game.Boat.Root.position.x,game.Boat.Root.position.z),new Vector2(Target.HookedPosition.x,Target.HookedPosition.z));
                Fight=new FishingFight(Target.SpeciesIndex,distance,(float)random.NextDouble()*4);
                float[] minimum={.08f,.15f,.12f,.08f,.04f,.08f,.30f,.15f};
                float[] maximum={.22f,.55f,.42f,.24f,.14f,.28f,1.35f,.55f};
                Weight=Mathf.Lerp(minimum[Target.SpeciesIndex],maximum[Target.SpeciesIndex],(float)random.NextDouble());
                fightDirection=Vector3.ProjectOnPlane(Target.HookedPosition-game.Boat.Root.position,Vector3.up).normalized;
                if(fightDirection.sqrMagnitude<.1f)fightDirection=game.Boat.Root.forward;
                Change(FishingStage.Hooking);game.Effects.FishingCue(1);game.Hud.Toast("咬住了！稳住张力，鱼冲刺时松开收线",3);return;
            }
            if(Stage==FishingStage.Waiting || Stage==FishingStage.Nibbling)
            {Retrieve("提竿太早，鱼受惊了；等浮漂明显下沉再按 E",true);return;}
            if(Stage!=FishingStage.Idle)return;
            if(!CanStart){game.Hud.Toast(game.Boat.Speed>VoyageRules.SlowSpeed?"先减速或停船，再抛竿":"靠近平静鱼群，按 E 抛竿",2);return;}
            Target=game.Nearby;castTo=Target.NearestFish(game.Boat.Root.position);
            // Assist the throw to clear the hull and rigging instead of casting through the mast.
            Vector3 sideSpot=game.Boat.Root.InverseTransformPoint(castTo);
            sideSpot.x=(sideSpot.x>=0?1:-1)*Mathf.Max(3,Mathf.Abs(sideSpot.x));
            castTo=game.Boat.Root.TransformPoint(sideSpot);
            Vector3 offset=Vector3.ProjectOnPlane(castTo-game.Boat.Root.position,Vector3.up);
            if(offset.sqrMagnitude<.01f)offset=game.Boat.Root.forward;
            castTo=game.Boat.Root.position+offset.normalized*Mathf.Clamp(offset.magnitude,4,14);
            for(int i=1;i<=12;i++)if(!WorldLayout.IsWater(Vector3.Lerp(game.Boat.Root.position,castTo,i/12f),.15f))
            {Target=null;game.Hud.Toast("抛竿方向被岛岸遮挡，请移到开阔水域",2);return;}
            castTo.y=OceanSimulation.Height(castTo,game.WorldTime)+.05f;
            launched=false;Bobber=castFrom=Visual.RodTip;
            waitDuration=3.2f+(float)random.NextDouble()*3.2f+Target.AverageDepth*.22f;
            Change(FishingStage.Casting);
        }
        public void Cancel(){if(Active && Stage!=FishingStage.Retrieving)Retrieve("已收竿，可重新选择鱼群",false);}
        public void Reset()
        {
            if(Target!=null)Target.ReleaseHook(game.WorldTime,false,game.Boat.Root.position);
            Target=null;Fight=null;Reeling=false;Change(FishingStage.Idle);
        }
        void Retrieve(string message,bool scare)
        {
            if(Target!=null)Target.ReleaseHook(game.WorldTime,scare,game.Boat.Root.position);
            retrieveFrom=Bobber;Change(FishingStage.Retrieving);Reeling=false;
            game.Hud.Toast(message,3);if(scare)game.Effects.FishingCue(3);
        }
        public void Tick(float dt,bool reel)
        {
            if(dt<=0)return;StageTime+=dt;Reeling=reel && Stage==FishingStage.Fighting;
            if(Active && Stage!=FishingStage.Retrieving)
            {
                if(Target==null || !Target.Root.gameObject.activeInHierarchy)
                {Retrieve("鱼群已经游远，收竿再试",false);return;}
                if(game.Boat.Speed>VoyageRules.SlowSpeed+.2f || Vector3.ProjectOnPlane(Bobber-game.Boat.Root.position,Vector3.up).magnitude>21)
                {Retrieve("船速过快或距离太远，钓鱼已中断",true);return;}
                if((Stage==FishingStage.Waiting || Stage==FishingStage.Nibbling || Stage==FishingStage.Bite) && Target.Mood!=SchoolMood.Calm)
                {Retrieve("鱼群受到惊扰，收竿再试",false);return;}
            }
            switch(Stage)
            {
                case FishingStage.Casting:
                    if(StageTime<.32f){Bobber=Visual.RodTip;break;}
                    if(!launched){castFrom=Visual.RodTip;launched=true;}
                    float duration=VoyageRules.CastDuration-.32f;
                    float elapsed=Mathf.Min(duration,StageTime-.32f),t=elapsed/duration;
                    Vector3 end=castTo;end.y=OceanSimulation.Height(end,game.WorldTime)+.05f;
                    float vy=(end.y-castFrom.y+.5f*9.81f*duration*duration)/duration;
                    Bobber=Vector3.Lerp(castFrom,end,t);Bobber=new Vector3(Bobber.x,castFrom.y+vy*elapsed-.5f*9.81f*elapsed*elapsed,Bobber.z);
                    if(StageTime>=VoyageRules.CastDuration){Bobber=end;game.Effects.Ring(end,new Color(.8f,.96f,.92f,.7f),1.1f,.75f);game.Effects.FishingCue(0);Change(FishingStage.Waiting);}
                    break;
                case FishingStage.Waiting:
                case FishingStage.Nibbling:
                case FishingStage.Bite:
                    Bobber+=Vector3.ProjectOnPlane(WeatherSystem.Wind,Vector3.up)*dt*.002f;
                    float dip=Stage==FishingStage.Bite?-.13f:Stage==FishingStage.Nibbling?Mathf.Sin(StageTime*13)*.055f:.025f*Mathf.Sin(StageTime*3);
                    Bobber=new Vector3(Bobber.x,OceanSimulation.Height(Bobber,game.WorldTime)+.07f+dip,Bobber.z);
                    if(Stage==FishingStage.Waiting && StageTime>=waitDuration)
                    {
                        Vector3 nearest=Target.NearestFish(Bobber);nearest.y=Bobber.y;
                        if(Vector3.Distance(nearest,Bobber)>7){Retrieve("鱼群游离了饵点，换个位置试试",false);break;}
                        Change(FishingStage.Nibbling);
                    }
                    else if(Stage==FishingStage.Nibbling && StageTime>=1.05f)
                    {Change(FishingStage.Bite);game.Effects.FishingCue(2);game.Effects.Ring(Bobber,new Color(1,.79f,.35f,.95f),1.8f,.6f);}
                    else if(Stage==FishingStage.Bite && StageTime>VoyageRules.BiteWindow)Retrieve("错过提竿时机，鱼脱钩了",true);
                    break;
                case FishingStage.Hooking:
                    Bobber=SurfaceOf(Target.HookedPosition);
                    if(StageTime>=.36f)Change(FishingStage.Fighting);
                    break;
                case FishingStage.Fighting:
                    Fight.Advance(dt,Reeling,game.Weather.WaveScale);
                    Vector3 direction=Quaternion.AngleAxis(Mathf.Sin(Fight.Elapsed*.7f)*12,Vector3.up)*fightDirection;
                    Vector3 desired=game.Boat.Root.position+direction*Fight.LineOut;
                    Vector3 localFish=game.Boat.Root.InverseTransformPoint(desired);
                    localFish.x=(localFish.x>=0?1:-1)*Mathf.Max(1.3f,Mathf.Abs(localFish.x));
                    desired=game.Boat.Root.TransformPoint(localFish);
                    if(!WorldLayout.IsWater(desired,.6f))desired=Target.HookedPosition;
                    desired.y=Mathf.Max(-4.05f,OceanSimulation.Height(desired,game.WorldTime)-Mathf.Lerp(.35f,Target.AverageDepth,Fight.Stamina));
                    Target.MoveHooked(desired,game.Boat.Root.position-desired,dt);
                    Bobber=SurfaceOf(Target.HookedPosition);
                    if(Fight.Outcome==FightOutcome.LineBroken)Retrieve("线断了：张力过高；下次鱼冲刺时松开收线",true);
                    else if(Fight.Outcome==FightOutcome.HookLost)Retrieve("脱钩了：鱼线长时间过松；间歇收线保持张力",true);
                    else if(Fight.Outcome==FightOutcome.Landable)
                    {landingFrom=Target.HookedPosition;Change(FishingStage.Landing);game.Effects.Ring(Bobber,new Color(.9f,.97f,.88f,.8f),1.2f,.9f);}
                    break;
                case FishingStage.Landing:
                    float progress=Mathf.Clamp01(StageTime/1.6f),ease=Mathf.SmoothStep(0,1,progress);
                    Vector3 landing=game.Boat.Root.TransformPoint(new Vector3(.35f,.80f,-.75f));
                    Vector3 fish=Vector3.Lerp(landingFrom,landing,ease)+Vector3.up*Mathf.Sin(progress*Mathf.PI)*.55f;
                    Target.MoveHooked(fish,game.Boat.Root.forward,dt,true);Bobber=Target.HookedMouth+Vector3.up*.20f;
                    if(progress>=1)
                    {
                        int species=Target.SpeciesIndex;
                        if(Target.LandHook(game.WorldTime))
                        {
                            bool record=Weight>game.BestWeights[species];game.Catches[species]++;game.BestWeights[species]=Mathf.Max(game.BestWeights[species],Weight);
                            game.Hud.Toast($"钓获 {Target.Name} · {Weight:0.00} kg"+(record?" · 新纪录！":""),4);game.Effects.CatchFeedback(true,landing);
                        }
                        Target=null;Change(FishingStage.Cooldown);
                    }
                    break;
                case FishingStage.Retrieving:
                    float recover=Mathf.SmoothStep(0,1,Mathf.Clamp01(StageTime/.9f));
                    Bobber=Vector3.Lerp(retrieveFrom,Visual.RodTip,recover)+Vector3.up*Mathf.Sin(recover*Mathf.PI)*.25f;
                    if(recover>=1){Target=null;Fight=null;Change(FishingStage.Cooldown);}
                    break;
                case FishingStage.Cooldown:
                    if(StageTime>=VoyageRules.CastCooldown){Fight=null;Change(FishingStage.Idle);}
                    break;
            }
        }
        Vector3 SurfaceOf(Vector3 position)=>new Vector3(position.x,OceanSimulation.Height(position,game.WorldTime)+.02f,position.z);
    }
}
