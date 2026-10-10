using UnityEngine;
using UnityEngine.Rendering;

namespace IslandVoyage
{
    public enum WeatherKind { Clear, Cloudy, Rain, Fog, Thunderstorm }

    // Profiles change over real simulation seconds, independently of the day/night clock rate.
    public sealed class WeatherSystem
    {
        public static Vector3 Wind {get;private set;}=new Vector3(2.82f,0,1.03f);
        public static float CloudCover {get;private set;}
        public static float Darkness {get;private set;}
        public static float Mist {get;private set;}
        public static float Flash {get;private set;}
        public WeatherKind Kind {get;private set;}
        public float RainAmount {get;private set;}
        static readonly string[] names={"晴朗","多云","降雨","海雾","雷暴"};
        public string Name=>names[(int)Kind];
        public float Visibility=>Mathf.Lerp(270,65,Mist);
        public int RainParticleCount=>rain.particleCount;
        public int LightningCount {get;private set;}
        public bool Automatic {get;set;}
        public float WaveScale {get;private set;}=1;
        readonly ParticleSystem rain;
        readonly AudioSource ambience,thunder;
        readonly AudioClip thunderClip;
        readonly LineRenderer bolt;
        readonly System.Random random=new System.Random();
        float untilChange=210,nextLightning=10,thunderDelay=-1,boltTime;
        static readonly float[] clouds={.18f,.72f,.90f,.48f,1};
        static readonly float[] darkness={0,.12f,.28f,.12f,.66f};
        static readonly float[] mist={0,.10f,.36f,1,.55f};
        static readonly float[] rainfall={0,0,.62f,0,1};
        static readonly float[] winds={3,5,8,2,17};
        static readonly float[] waves={1,1.25f,1.7f,.9f,2.8f};
        public WeatherSystem(bool automatic=true)
        {
            Automatic=automatic;
            var root=new GameObject("Weather · wind / rain / distant thunder");
            rain=new GameObject("Wind-driven rain",typeof(ParticleSystem)).GetComponent<ParticleSystem>();rain.transform.SetParent(root.transform);
            var main=rain.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=1.35f;main.startSpeed=0;main.startSize=.045f;main.maxParticles=1600;main.gravityModifier=.6f;
            main.startColor=new Color(.72f,.85f,.92f,.43f);
            var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(46,1,46);
            var emission=rain.emission;emission.rateOverTime=0;
            var velocity=rain.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=0;velocity.y=-17;velocity.z=0;
            var fade=rain.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(1,.8f),new GradientAlphaKey(0,1)});fade.color=gradient;
            var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=3;renderer.velocityScale=.035f;
            renderer.sharedMaterial=new Material(Shader.Find("IslandVoyage/Rain"));renderer.shadowCastingMode=ShadowCastingMode.Off;
            rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);rain.gameObject.SetActive(false);
            ambience=root.AddComponent<AudioSource>();ambience.loop=true;ambience.clip=NoiseClip("Wind and rainfall",3,false);ambience.volume=0;ambience.Play();
            thunder=new GameObject("Distance-delayed thunder",typeof(AudioSource)).GetComponent<AudioSource>();thunder.transform.SetParent(root.transform);thunder.volume=.22f;thunderClip=NoiseClip("Rolling thunder",4,true);
            bolt=VoyageEffects.Line(root.transform,"Lightning beyond the islands",9,.09f);bolt.startColor=bolt.endColor=new Color(.68f,.83f,1,.9f);bolt.gameObject.SetActive(false);
            SetWeather(WeatherKind.Clear,true);
        }
        public void Cycle()=>SetWeather((WeatherKind)(((int)Kind+1)%5));
        public void SetWeather(WeatherKind kind,bool immediate=false)
        {
            Kind=kind;untilChange=180+(float)random.NextDouble()*180;
            if(immediate){int i=(int)kind;CloudCover=clouds[i];Darkness=darkness[i];Mist=mist[i];RainAmount=rainfall[i];WaveScale=waves[i];Wind=new Vector3(.94f,0,.341174f)*winds[i];Flash=0;thunderDelay=-1;}
            Apply();
        }
        public void Tick(float dt,float time,Vector3 boat,bool muted)
        {
            untilChange-=dt;
            if(Automatic && untilChange<=0){int next=random.Next(5);if(next==(int)Kind)next=(next+1)%5;SetWeather((WeatherKind)next);}
            int i=(int)Kind;float blend=1-Mathf.Exp(-dt/8);
            CloudCover=Mathf.Lerp(CloudCover,clouds[i],blend);Darkness=Mathf.Lerp(Darkness,darkness[i],blend);Mist=Mathf.Lerp(Mist,mist[i],blend);
            RainAmount=Mathf.Lerp(RainAmount,rainfall[i],blend);WaveScale=Mathf.Lerp(WaveScale,waves[i],blend);
            float gust=1+.08f*Mathf.Sin(time*.37f)+.04f*Mathf.Sin(time*.91f);
            Wind=Vector3.Lerp(Wind,new Vector3(.94f,0,.341174f)*winds[i]*gust,blend);
            Flash=Mathf.MoveTowards(Flash,0,dt*4);boltTime=Mathf.Max(0,boltTime-dt);nextLightning-=dt;
            if(Kind==WeatherKind.Thunderstorm && Darkness>.45f && nextLightning<=0)
            {
                nextLightning=9+(float)random.NextDouble()*13;Flash=1;boltTime=.18f;LightningCount++;
                Vector3 strike=WorldLayout.Clamp(boat+new Vector3(-45,0,65),3);
                for(int j=0;j<9;j++)bolt.SetPosition(j,strike+new Vector3(j==8?0:((float)random.NextDouble()-.5f)*5,48-j*5.8f,0));
                thunderDelay=Vector3.Distance(boat,strike)/343f;
            }
            bolt.gameObject.SetActive(boltTime>0);
            if(thunderDelay>=0){thunderDelay-=dt;if(thunderDelay<0)thunder.PlayOneShot(thunderClip);}
            rain.transform.position=boat+Vector3.up*17;
            var emission=rain.emission;emission.rateOverTime=RainAmount*850;
            var velocity=rain.velocityOverLifetime;velocity.x=Wind.x*.55f;velocity.z=Wind.z*.55f;
            bool raining=RainAmount>.015f;
            if(raining && !rain.gameObject.activeSelf){rain.gameObject.SetActive(true);rain.Play();}
            if(!raining && rain.gameObject.activeSelf){rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);rain.gameObject.SetActive(false);}
            ambience.volume=Mathf.Lerp(.012f,.10f,RainAmount)+Darkness*.035f;ambience.pitch=.8f+Wind.magnitude*.02f;ambience.mute=thunder.mute=muted;
            Apply();
        }
        void Apply()
        {
            OceanSimulation.WaveScale=WaveScale;
            Shader.SetGlobalFloat("_CloudCover",CloudCover);Shader.SetGlobalFloat("_StormDarkness",Darkness);Shader.SetGlobalFloat("_Lightning",Flash);
            Shader.SetGlobalVector("_WeatherWind",new Vector4(Wind.x,Wind.z,0,0));
            Shader.SetGlobalVector("_SeaFog",new Vector4(Mathf.Lerp(100,22,Mist),Visibility,0,0));
        }
        static AudioClip NoiseClip(string name,int seconds,bool thunder)
        {
            const int rate=22050;var rng=new System.Random(thunder?872:321);var samples=new float[rate*seconds];float low=0;
            for(int i=0;i<samples.Length;i++)
            {
                float n=(float)rng.NextDouble()*2-1;low=Mathf.Lerp(low,n,thunder?.008f:.055f);
                float t=i/(float)samples.Length;float envelope=thunder?Mathf.Sin(Mathf.PI*t)*Mathf.Exp(-t*2):1;
                samples[i]=(low*(thunder?3:1)+n*(thunder?.025f:.045f))*envelope;
            }
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
    }
}
