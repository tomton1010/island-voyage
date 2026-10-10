using UnityEngine;

namespace IslandVoyage
{
    public sealed class FishSchool
    {
        sealed class Agent
        { public Transform visual;public Renderer[] renderers;public RigPlayback animation;public Vector3 position,velocity,offset;public float phase,respawnAt,depth;public bool active=true; }
        readonly Agent[] fish;
        readonly System.Random random;
        readonly Vector3 home;
        readonly float spread;
        readonly MaterialPropertyBlock visibility=new MaterialPropertyBlock();
        float encounterVisibility=1;
        Agent hooked;
        public bool HasHookedFish=>hooked!=null && hooked.active;
        public Vector3 HookedPosition=>hooked==null?Center:hooked.position;
        public Vector3 HookedMouth=>hooked==null?Center:hooked.position+hooked.visual.forward*.22f;
        public float AverageDepth {get;private set;}
        public float MinimumSubmergence
        {get {float depth=float.MaxValue;foreach(var f in fish)if(f.active && f!=hooked)depth=Mathf.Min(depth,OceanSimulation.Height(f.position,OceanSimulation.Time)-f.position.y);return depth;}}
        public float MinimumSeabedClearance
        {get {float clearance=float.MaxValue;foreach(var f in fish)if(f.active)clearance=Mathf.Min(clearance,f.position.y-WorldLayout.GroundHeight(f.position));return clearance;}}
        public Vector3 Home=>home;
        public float Spread=>spread;
        Vector3 escapeTarget;
        float fearUntil;
        bool migrating;
        Vector3 migrationTarget;
        public readonly int SpeciesIndex;
        public readonly Transform Root;
        public Vector3 Center {get;private set;}
        public SchoolMood Mood {get;private set;}=SchoolMood.Calm;
        public int ActiveCount {get;private set;}
        public string Name=>AnimalModels.Species[SpeciesIndex].Name;
        public FishSchool(int species,Vector3 center,int count,Transform parent,int id=0,float spread=6)
        {
            this.spread=spread;SpeciesIndex=species;Center=home=center;random=new System.Random(500+species+id*37);ActiveCount=count;
            Root=new GameObject(Name+" school").transform;Root.SetParent(parent);fish=new Agent[count];
            for(int i=0;i<count;i++)
            {
                Vector3 p=SafeOffset(center,i);
                float depth=species<5?1.4f+species*.12f+(float)random.NextDouble()*.8f:2.65f+(float)random.NextDouble()*1.2f;
                p.y=WorldLayout.SubmergedHeight(p,depth,OceanSimulation.Time);
                var t=BlenderAssets.Create(BlenderAssets.FishNames[species],Root,p);
                t.localScale=Vector3.one*(.52f+(float)random.NextDouble()*.27f);
                float phase=(float)random.NextDouble();
                fish[i]=new Agent{visual=t,renderers=t.GetComponentsInChildren<Renderer>(),animation=new RigPlayback(t,"Swim",phase),position=p,offset=new Vector3(p.x-center.x,0,p.z-center.z),depth=depth,phase=phase*Mathf.PI*2,velocity=new Vector3(.2f,0,.6f)};
                AverageDepth+=depth/count;
                foreach(var renderer in fish[i].renderers)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                ApplyWaterAppearance(fish[i]);
            }
        }
        public void SetEncounterVisibility(float amount)
        {
            encounterVisibility=Mathf.Clamp01(amount);foreach(var f in fish)ApplyWaterAppearance(f);
        }
        void ApplyWaterAppearance(Agent f)
        {
            visibility.SetFloat("_EncounterVisibility",encounterVisibility);visibility.SetFloat("_Aquatic",1);
            float surface=OceanSimulation.Height(f.position,OceanSimulation.Time);
            visibility.SetFloat("_WaterDepth",Mathf.Max(0,surface-f.position.y));visibility.SetFloat("_WaterSurface",surface);
            foreach(var renderer in f.renderers)renderer.SetPropertyBlock(visibility);
        }
        public bool TryHook(Vector3 bait)
        {
            if(hooked!=null || Mood!=SchoolMood.Calm)return false;
            float nearest=float.MaxValue;
            foreach(var f in fish)if(f.active){float distance=(f.position-bait).sqrMagnitude;if(distance<nearest){nearest=distance;hooked=f;}}
            return hooked!=null;
        }
        public void MoveHooked(Vector3 position,Vector3 forward,float dt,bool landing=false)
        {
            if(!HasHookedFish)return;
            hooked.position=landing?position:Vector3.MoveTowards(hooked.position,position,dt*3.5f);
            if(!landing)hooked.position.y=Mathf.Max(hooked.position.y,WorldLayout.GroundHeight(hooked.position)+.45f);
            hooked.visual.position=hooked.position;
            if(forward.sqrMagnitude>.001f)hooked.visual.rotation=Quaternion.Slerp(hooked.visual.rotation,Quaternion.LookRotation(forward),1-Mathf.Exp(-dt*6));
            hooked.animation.SetSpeed(landing?1.2f:2,dt);ApplyWaterAppearance(hooked);
        }
        public void ReleaseHook(float time,bool scare,Vector3 boat)
        {
            if(hooked!=null){hooked.velocity=hooked.visual.forward*.8f;hooked.velocity.y=0;hooked=null;}
            if(scare)Spook(boat,time);
        }
        public bool LandHook(float time)
        {
            if(!HasHookedFish)return false;
            hooked.active=false;hooked.visual.gameObject.SetActive(false);hooked.respawnAt=time+24;ActiveCount--;hooked=null;return true;
        }
        Vector3 SafeOffset(Vector3 center,int index)
        {
            float angle=index*2.39996f;
            float radius=spread*Mathf.Sqrt((index+.5f)/fish.Length);
            for(int attempt=0;attempt<16;attempt++)
            {
                Vector3 offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(radius*(1-attempt*.045f));
                Vector3 p=center+offset;if(WorldLayout.IsWater(p,1,2)){p.y=-.4f;return p;}angle+=.63f;
            }
            return new Vector3(center.x,-.4f,center.z);
        }
        public float DistanceFrom(Vector3 position)
        {
            float nearest=float.MaxValue;foreach(var f in fish)if(f.active)
            {float dx=f.position.x-position.x,dz=f.position.z-position.z;nearest=Mathf.Min(nearest,dx*dx+dz*dz);}
            return Mathf.Max(0,Mathf.Sqrt(nearest)-1.25f);
        }
        public Vector3 NearestFish(Vector3 position)
        {
            Vector3 result=Center;float nearest=float.MaxValue;foreach(var f in fish)if(f.active)
            {float d=(new Vector2(f.position.x-position.x,f.position.z-position.z)).sqrMagnitude;if(d<nearest){nearest=d;result=f.position;}}return result;
        }
        public void Spook(Vector3 threat,float time)
        {
            Vector3 away=Center-threat;away.y=0;if(away.sqrMagnitude<.05f)away=Vector3.forward;
            escapeTarget=WorldLayout.Clamp(Center+away.normalized*13,4);
            if(!WorldLayout.IsWater(escapeTarget,3))escapeTarget=WorldLayout.RandomWater(random,4);
            fearUntil=time+2.8f;Mood=SchoolMood.Fleeing;
        }
        public bool CatchOne(float time,Vector3? position=null)
        {
            Agent selected=null;float nearest=float.MaxValue;Vector3 p=position??Center;
            foreach(var f in fish)if(f.active && f!=hooked){float d=(f.position-p).sqrMagnitude;if(d<nearest){nearest=d;selected=f;}}
            if(selected==null)return false;
            selected.active=false;selected.visual.gameObject.SetActive(false);selected.respawnAt=time+24;ActiveCount--;return true;
        }
        public void BeginMigration(Vector3 center,Vector3 destination,Vector3 direction)
        {
            migrating=true;hooked=null;Center=center;Center=new Vector3(center.x,0,center.z);migrationTarget=destination;Mood=SchoolMood.Calm;fearUntil=0;ActiveCount=fish.Length;
            for(int i=0;i<fish.Length;i++)
            {
                var f=fish[i];f.position=SafeOffset(Center,i);f.offset=f.position-Center;f.offset.y=0;
                f.position.y=WorldLayout.SubmergedHeight(f.position,f.depth,OceanSimulation.Time);
                f.velocity=direction*1.4f;f.active=true;f.respawnAt=0;f.visual.gameObject.SetActive(true);f.visual.position=f.position;f.visual.rotation=Quaternion.LookRotation(direction);
                ApplyWaterAppearance(f);
            }
        }
        public void Tick(float dt,float time,Vector3 boat,float boatSpeed)
        {
            if(VoyageRules.IsThreat(boatSpeed,DistanceFrom(boat)))Spook(boat,time);
            if(Mood==SchoolMood.Fleeing && time>=fearUntil)Mood=SchoolMood.Returning;
            Vector3 target;
            if(Mood==SchoolMood.Fleeing)target=escapeTarget;
            else if(Mood==SchoolMood.Returning)target=migrating?migrationTarget:home;
            else if(migrating)target=migrationTarget;
            else target=home+new Vector3(Mathf.Sin(time*.033f+SpeciesIndex)*4,0,Mathf.Cos(time*.025f+SpeciesIndex)*3);
            float centerSpeed=Mood==SchoolMood.Fleeing?2.4f:migrating?1.8f:.75f;
            Vector3 next=Vector3.MoveTowards(Center,target,dt*centerSpeed);
            Center=WorldLayout.Move(Center,next,3,2);
            if(Mood==SchoolMood.Returning && time>fearUntil+4 && (migrating || Vector3.Distance(Center,home)<1))Mood=SchoolMood.Calm;
            for(int i=0;i<fish.Length;i++)
            {
                Agent f=fish[i];
                if(f==hooked)continue;
                if(!f.active)
                {
                    if(time<f.respawnAt || Vector3.Distance(boat,Center)<16)continue;
                    f.position=SafeOffset(Center,i);f.position.y=WorldLayout.SubmergedHeight(f.position,f.depth,time);f.active=true;f.visual.gameObject.SetActive(true);ActiveCount++;
                }
                Vector3 goal=Center+f.offset*(Mood==SchoolMood.Fleeing?1.65f:1)+new Vector3(Mathf.Sin(time*.13f+f.phase),0,Mathf.Cos(time*.11f+f.phase))*1.4f;
                Vector3 separation=Vector3.zero,alignment=Vector3.zero;int neighbors=0;
                for(int j=0;j<fish.Length;j++)
                {
                    if(i==j || !fish[j].active)continue;
                    Vector3 offset=f.position-fish[j].position;offset.y=0;float d=offset.sqrMagnitude;
                    if(d>.0001f && d<2.0f)separation+=offset/(d+.1f)*1.25f;
                    if(d<16){alignment+=fish[j].velocity;neighbors++;}
                }
                Vector3 steering=(goal-f.position)*.55f+separation+WorldLayout.Avoidance(f.position,4,2)*5+(neighbors>0?alignment/neighbors*.8f:Vector3.zero);
                steering.y=0;
                float speed=Mood==SchoolMood.Fleeing?3.7f:migrating?1.9f:.6f+SpeciesIndex*.055f;
                float turn=Vector3.SignedAngle(f.velocity,steering,Vector3.up);
                Vector3 direction=Quaternion.Euler(0,Mathf.Clamp(turn,-dt*(Mood==SchoolMood.Fleeing?150:65),dt*(Mood==SchoolMood.Fleeing?150:65)),0)*f.velocity.normalized;
                f.velocity=Vector3.MoveTowards(f.velocity,direction*speed,dt*(Mood==SchoolMood.Fleeing?5:1.5f));
                f.position=WorldLayout.Move(f.position,f.position+f.velocity*dt,.45f,2);
                float desiredY=WorldLayout.SubmergedHeight(f.position,f.depth-Mathf.Sin(time*.35f+f.phase)*.13f,time);
                f.position.y=Mathf.Max(WorldLayout.GroundHeight(f.position)+.45f,Mathf.MoveTowards(f.position.y,desiredY,dt*.85f));
                f.visual.position=f.position;
                if(f.velocity.sqrMagnitude>.001f)
                    f.visual.rotation=Quaternion.LookRotation(new Vector3(f.velocity.x,0,f.velocity.z));
                f.animation.SetSpeed(Mathf.Clamp(.55f+f.velocity.magnitude*.6f,.7f,2.6f),dt);
                ApplyWaterAppearance(f);
            }
        }
    }
}
