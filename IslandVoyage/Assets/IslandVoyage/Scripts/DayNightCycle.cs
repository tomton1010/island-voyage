using UnityEngine;
using UnityEngine.Rendering;

namespace IslandVoyage
{
    public sealed class DayNightCycle
    {
        readonly Light sun;
        readonly Camera camera;
        readonly MeshRenderer cottageGlow;
        readonly Material glowMaterial;
        public float Hour {get;private set;}=9.5f;
        public int RateIndex {get;private set;}
        static readonly int[] rates={1,4,12,0};
        public int Rate=>rates[RateIndex];
        public float Daylight {get;private set;}=1;
        public string Clock=>$"{Mathf.FloorToInt(Hour):00}:{Mathf.FloorToInt((Hour%1)*60):00}";
        public void ChangeRate()=>RateIndex=(RateIndex+1)%rates.Length;
        public void SetHour(float hour){Hour=Mathf.Repeat(hour,24);Tick(0);}
        public DayNightCycle(Camera camera)
        {
            this.camera=camera;
            sun=new GameObject("Sun · tropical latitude",typeof(Light)).GetComponent<Light>();
            sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowBias=.035f;sun.shadowNormalBias=.22f;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=180;RenderSettings.fogEndDistance=400;
            var geometry=new Geometry();
            for(int side=-1;side<=1;side+=2)geometry.Box(new Vector3(side*1.9f,1.9f,-2.42f),new Vector3(.82f,.73f,.035f),Color.white,Quaternion.identity);
            var glow=Art.Object("Cottage lamplight",geometry.Build("Lit windows"),null,new Vector3(39,2.45f,15));
            glow.rotation=Quaternion.Euler(0,-18,0);glow.localScale=Vector3.one*1.25f;
            cottageGlow=glow.GetComponent<MeshRenderer>();glowMaterial=new Material(VoyageEffects.Material);cottageGlow.sharedMaterial=glowMaterial;cottageGlow.shadowCastingMode=ShadowCastingMode.Off;
            Tick(0);
        }
        public void Tick(float dt)
        {
            // One full day is twenty real minutes at 1×; acceleration changes the clock, not gravity.
            Hour=Mathf.Repeat(Hour+dt*24/1200*Rate,24);
            float latitude=12*Mathf.Deg2Rad,declination=10*Mathf.Deg2Rad,h=(Hour-12)*Mathf.PI/12;
            Vector3 towardSun=new Vector3(-Mathf.Cos(declination)*Mathf.Sin(h),Mathf.Sin(latitude)*Mathf.Sin(declination)+Mathf.Cos(latitude)*Mathf.Cos(declination)*Mathf.Cos(h),Mathf.Cos(latitude)*Mathf.Sin(declination)-Mathf.Sin(latitude)*Mathf.Cos(declination)*Mathf.Cos(h));
            Daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.12f,.28f,towardSun.y));
            sun.transform.rotation=Quaternion.LookRotation(-towardSun,Vector3.up);
            sun.color=Color.Lerp(new Color(.58f,.69f,.95f),Color.Lerp(new Color(1,.66f,.4f),new Color(1,.96f,.84f),Mathf.InverseLerp(.04f,.55f,towardSun.y)),Daylight);
            sun.intensity=Mathf.Lerp(0,1.55f,Daylight)*(1-WeatherSystem.Darkness*.85f)+WeatherSystem.Flash*2;
            var ambient=Color.Lerp(new Color(.05f,.075f,.125f),new Color(.22f,.27f,.30f),Daylight);
            ambient*=1-WeatherSystem.Darkness*.4f;ambient+=new Color(.15f,.18f,.23f)*WeatherSystem.Flash;
            RenderSettings.ambientLight=ambient;Shader.SetGlobalColor("_VoyageAmbient",ambient);
            Shader.SetGlobalFloat("_OceanLight",Mathf.Lerp(.075f,1,Daylight)*(1-WeatherSystem.Darkness*.6f)+WeatherSystem.Flash*.5f);Shader.SetGlobalFloat("_Night",1-Daylight);
            Color horizon=Color.Lerp(new Color(.025f,.055f,.115f),new Color(.66f,.81f,.83f),Daylight);
            horizon=Color.Lerp(horizon,Color.Lerp(new Color(.022f,.032f,.052f),new Color(.36f,.43f,.48f),Daylight),WeatherSystem.Darkness);
            horizon+=new Color(.23f,.27f,.30f)*WeatherSystem.Flash;
            Shader.SetGlobalVector("_SeaHorizon",new Vector4(horizon.r,horizon.g,horizon.b,1));
            RenderSettings.fogStartDistance=Mathf.Lerp(180,22,WeatherSystem.Mist);RenderSettings.fogEndDistance=Mathf.Lerp(400,65,WeatherSystem.Mist);
            RenderSettings.fogColor=horizon;camera.backgroundColor=horizon;
            glowMaterial.SetColor("_BaseColor",new Color(1,.63f,.19f,(1-Daylight)*.9f));
        }
    }
}
