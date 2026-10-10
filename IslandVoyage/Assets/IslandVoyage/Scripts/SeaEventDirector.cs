using System.Collections.Generic;
using UnityEngine;

namespace IslandVoyage
{
    public enum SeaEventKind { None, RogueSwell, Migration, LuminousSea }
    public enum SeaEventStage { Idle, Warning, Active, Recovery }

    public sealed class SeaEventDirector
    {
        readonly GameBootstrap game;
        readonly System.Random random=new System.Random();
        readonly Transform migrationRoot;
        readonly List<FishSchool> migration=new List<FishSchool>();
        Vector3 routeStart,routeEnd,routeDirection;
        float elapsed,untilNext=65;
        int nextManual=2;
        public bool Automatic {get;set;}
        public SeaEventKind Kind {get;private set;}
        public SeaEventStage Stage {get;private set;}
        public float Remaining=>Stage==SeaEventStage.Warning?Mathf.Max(0,8-elapsed):Stage==SeaEventStage.Active?Mathf.Max(0,Duration-elapsed):Stage==SeaEventStage.Recovery?Mathf.Max(0,10-elapsed):untilNext;
        float Duration=>Kind==SeaEventKind.Migration?75:Kind==SeaEventKind.RogueSwell?32:55;
        public string Name=>Kind==SeaEventKind.RogueSwell?"异常涌浪":Kind==SeaEventKind.Migration?"大鱼群迁徙":Kind==SeaEventKind.LuminousSea?"荧光海":"暂无自然奇遇";
        public string Hint=>Kind==SeaEventKind.RogueSwell?"Shift 减速 · 靠近岛岸避浪":Kind==SeaEventKind.Migration?"航图青色点 · 慢行观察 / E 抛竿":Kind==SeaEventKind.LuminousSea?"夜间最明亮 · 尾流与浪花会发光":"天气与奇遇会随航行自然出现";
        public bool MigrationVisible=>Kind==SeaEventKind.Migration && Stage!=SeaEventStage.Idle && migrationRoot.gameObject.activeSelf;
        public IReadOnlyList<FishSchool> MigrationSchools=>migration;
        public Vector3 Position=>Kind==SeaEventKind.Migration && migration.Count>0?migration[0].Center:OceanSimulation.SwellCenter;
        public float Intensity=>Stage==SeaEventStage.Active?Mathf.Min(Mathf.Clamp01(elapsed/5),Mathf.Clamp01((Duration-elapsed)/6)):0;
        public SeaEventDirector(GameBootstrap game,bool automatic=true)
        {
            this.game=game;Automatic=automatic;
            migrationRoot=new GameObject("Occasional migration · capped reusable population").transform;migrationRoot.gameObject.SetActive(false);
            OceanSimulation.SwellStrength=0;Shader.SetGlobalFloat("_Bioluminescence",0);
        }
        public void PreviewNext()
        {
            var kind=(SeaEventKind)nextManual;nextManual=nextManual==2?3:nextManual==3?1:2;
            Start(kind);
        }
        public bool Start(SeaEventKind kind,bool immediate=false)
        {
            Finish();
            if(kind==SeaEventKind.Migration && !PlanRoute(immediate))
            {game.Hud?.Toast("附近水域过窄，前往开阔海面再观察迁徙",3);return false;}
            Kind=kind;Stage=immediate?SeaEventStage.Active:SeaEventStage.Warning;elapsed=immediate?7:0;
            if(kind==SeaEventKind.Migration)PrepareMigration();
            if(kind==SeaEventKind.RogueSwell)
            {
                routeDirection=Vector3.ProjectOnPlane(game.Boat.Root.forward,Vector3.up).normalized;
                OceanSimulation.SwellDirection=new Vector2(routeDirection.x,routeDirection.z);
                OceanSimulation.SwellCenter=game.Boat.Root.position-routeDirection*(immediate?20:85);
            }
            game.Hud?.Toast(immediate?Name:Name+"即将出现 · "+Hint,5);
            return true;
        }
        public void Finish()
        {
            // The finite hidden pool is reused and reset for the next encounter.
            migrationRoot.gameObject.SetActive(false);Kind=SeaEventKind.None;Stage=SeaEventStage.Idle;elapsed=0;
            OceanSimulation.SwellStrength=0;Shader.SetGlobalFloat("_Bioluminescence",0);
            untilNext=115+(float)random.NextDouble()*100;
        }
        bool PlanRoute(bool immediate)
        {
            Vector3 boat=game.Boat.Root.position;boat.y=0;
            for(int i=0;i<36;i++)
            {
                float offset=i==0?0:((i+1)/2)*10*(i%2==1?-1:1);
                Vector3 direction=Quaternion.Euler(0,game.Boat.Root.eulerAngles.y+offset,0)*Vector3.forward;
                Vector3 start=boat+direction*(immediate?21:54),end=boat-direction*80;
                Vector3 sideways=Vector3.Cross(Vector3.up,direction);
                bool safe=true;
                for(int j=0;j<=32 && safe;j++)
                {
                    Vector3 center=Vector3.Lerp(start,end,j/32f);
                    if(!WorldLayout.IsWater(center,13,2)){safe=false;break;}
                    // Check the whole eight-school corridor, including spread and longitudinal offsets.
                    foreach(float side in new[]{-10.2f,10.2f})foreach(float along in new[]{-7f,7f})
                        if(!WorldLayout.IsWater(center+sideways*side+direction*along,1,2)){safe=false;break;}
                }
                if(!safe)continue;routeStart=start;routeEnd=end;routeDirection=-direction;return true;
            }
            return false;
        }
        void PrepareMigration()
        {
            if(migration.Count==0)
                for(int i=0;i<8;i++)migration.Add(new FishSchool(i,routeStart,18,migrationRoot,100+i,3.1f));
            Vector3 sideways=Vector3.Cross(Vector3.up,routeDirection);
            for(int i=0;i<migration.Count;i++)
            {
                Vector3 offset=sideways*((i%4-1.5f)*3.8f)+routeDirection*((i/4-.5f)*5);
                migration[i].BeginMigration(routeStart+offset,routeEnd+offset,routeDirection);
                migration[i].SetEncounterVisibility(Stage==SeaEventStage.Warning?0:1);
            }
            migrationRoot.gameObject.SetActive(true);
        }
        public void Tick(float dt,float time)
        {
            if(Stage==SeaEventStage.Idle)
            {
                untilNext-=dt;
                if(Automatic && untilNext<=0)
                {
                    SeaEventKind kind=game.Cycle.Daylight<.18f?SeaEventKind.LuminousSea:random.NextDouble()<.70?SeaEventKind.Migration:SeaEventKind.RogueSwell;
                    if(!Start(kind))untilNext=30;
                }
                return;
            }
            elapsed+=dt;
            if(Stage==SeaEventStage.Warning && elapsed>=8){Stage=SeaEventStage.Active;elapsed-=8;game.Hud?.Toast(Name+"正在发生 · "+Hint,4);}
            if(Stage==SeaEventStage.Active && elapsed>=Duration){Stage=SeaEventStage.Recovery;elapsed-=Duration;game.Hud?.Toast(Name+"逐渐结束",3);}
            if(Stage==SeaEventStage.Recovery && elapsed>=10){Finish();return;}
            if(Kind==SeaEventKind.RogueSwell)
            {
                if(Stage!=SeaEventStage.Warning)OceanSimulation.SwellCenter+=routeDirection*dt*6.5f;
                OceanSimulation.SwellStrength=Intensity*1.35f;
                if(Intensity>.35f && Mathf.Abs(Vector3.Dot(game.Boat.Root.position-OceanSimulation.SwellCenter,routeDirection))<13)
                    foreach(var school in game.Schools)if(Vector3.Distance(school.Center,game.Boat.Root.position)<22)school.Spook(OceanSimulation.SwellCenter,time);
            }
            if(MigrationVisible)
            {
                // During the warning fish approach from afar; the swimmer's acceleration stays bounded.
                float visibility=Stage==SeaEventStage.Warning?Mathf.SmoothStep(0,1,elapsed/3):Stage==SeaEventStage.Recovery?Mathf.Clamp01(1-elapsed/10):1;
                foreach(var school in migration){school.SetEncounterVisibility(visibility);school.Tick(dt,time,game.Boat.Root.position,game.Boat.Speed);}
                if(Stage==SeaEventStage.Recovery)
                {
                    bool outside=true;foreach(var school in migration)if(game.View.WorldToViewportPoint(school.Center).z>0 && Vector3.Distance(game.Boat.Root.position,school.Center)<65)outside=false;
                    // Delay despawning visible fish: fade via the sea's distance haze after they leave.
                    if(outside)migrationRoot.gameObject.SetActive(false);
                }
            }
            Shader.SetGlobalFloat("_Bioluminescence",Kind==SeaEventKind.LuminousSea?Intensity*Mathf.Clamp01((.4f-game.Cycle.Daylight)/.4f):0);
        }
    }
}
