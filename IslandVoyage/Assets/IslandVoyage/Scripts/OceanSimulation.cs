using UnityEngine;
using UnityEngine.Rendering;

namespace IslandVoyage
{
    // The CPU buoyancy sampler and GPU water shader use the same gravity-dispersed waves.
    public static class OceanSimulation
    {
        public static float Time {get;private set;}
        public static Transform Surface;
        public static float WaveScale=1,SwellStrength;
        public static Vector3 SwellCenter;
        public static Vector2 SwellDirection=Vector2.up;
        static readonly Vector2[] directions={new Vector2(.94f,.341174f),new Vector2(.52f,-.854166f),new Vector2(-.31f,-.950736f)};
        static readonly float[] wavelengths={18,7.5f,3.1f},amplitudes={.16f,.07f,.025f};
        public static void Tick(float time,Vector3 center)
        {
            Time=time;Shader.SetGlobalFloat("_OceanTime",time);
            Shader.SetGlobalFloat("_WaveScale",WaveScale);
            Shader.SetGlobalVector("_Swell",new Vector4(SwellCenter.x,SwellCenter.z,SwellDirection.x,SwellDirection.y));
            Shader.SetGlobalFloat("_SwellStrength",SwellStrength);
            if(Surface!=null)Surface.position=new Vector3(Mathf.Round(center.x/20)*20,0,Mathf.Round(center.z/20)*20);
        }
        public static float Height(Vector3 point,float time)
        {
            float height=0;
            for(int i=0;i<3;i++)
            {
                float k=2*Mathf.PI/wavelengths[i],omega=Mathf.Sqrt(9.81f*k);
                height+=amplitudes[i]*Mathf.Sin(k*(point.x*directions[i].x+point.z*directions[i].y)-omega*time);
            }
            float shore=Mathf.SmoothStep(0,1,Mathf.Clamp01(WorldLayout.ShoreDistance(point)/9));
            float along=Vector2.Dot(new Vector2(point.x-SwellCenter.x,point.z-SwellCenter.z),SwellDirection);
            float pulse=SwellStrength*Mathf.Exp(-along*along/128);
            return (height*WaveScale+pulse)*(.18f+.82f*shore);
        }
        public static Mesh SurfaceMesh()
        {
            const int steps=256;const float width=320;
            var vertices=new Vector3[(steps+1)*(steps+1)];var triangles=new int[steps*steps*6];
            for(int z=0;z<=steps;z++)for(int x=0;x<=steps;x++)vertices[z*(steps+1)+x]=new Vector3((float)x/steps*width-width/2,0,(float)z/steps*width-width/2);
            int index=0;for(int z=0;z<steps;z++)for(int x=0;x<steps;x++)
            {
                int a=z*(steps+1)+x,b=a+steps+1;
                triangles[index++]=a;triangles[index++]=b;triangles[index++]=a+1;triangles[index++]=a+1;triangles[index++]=b;triangles[index++]=b+1;
            }
            var mesh=new Mesh{name="Moving wave grid",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.triangles=triangles;
            mesh.bounds=new Bounds(Vector3.zero,new Vector3(width,8,width));return mesh;
        }
        public static void Configure(Material material)
        {
            var data=new Vector4[8];var phasesA=Vector4.zero;var phasesB=Vector4.zero;
            for(int i=0;i<WorldLayout.Islands.Length;i++)
            {
                var island=WorldLayout.Islands[i];data[i]=new Vector4(island.center.x,island.center.y,island.size.x,island.size.y);
                if(i<4)phasesA[i]=island.phase;else phasesB[i-4]=island.phase;
            }
            material.SetVectorArray("_Islands",data);material.SetInt("_IslandCount",WorldLayout.Islands.Length);
            material.SetVector("_PhasesA",phasesA);material.SetVector("_PhasesB",phasesB);
        }
    }
}
