using System.Collections.Generic;
using UnityEngine;

namespace IslandVoyage
{
    public sealed class VoyageEffects
    {
        sealed class Ripple {public LineRenderer line;public Vector3 center;public float age,duration,radius;public Color color;public readonly Vector3[] points=new Vector3[49];}
        sealed class Drop {public Transform visual;public Vector3 position,velocity;public float remaining;}
        static Material material;
        public static Material Material=>material!=null?material:(material=new Material(Shader.Find("IslandVoyage/Effects")){name="Water effects"});
        readonly Transform root;
        readonly List<Ripple> ripples=new List<Ripple>();
        readonly List<Drop> drops=new List<Drop>();
        readonly AudioSource music,sound;
        readonly AudioClip success,miss,splash,bite,hook;
        Mesh spoutMesh;
        public bool Muted {get;private set;}
        public VoyageEffects()
        {
            root=new GameObject("Ripples and ocean audio").transform;
            music=root.gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.035f;music.clip=OceanSound();music.Play();
            sound=root.gameObject.AddComponent<AudioSource>();sound.volume=.17f;
            success=Tone("Catch",660,.35f);miss=Tone("Miss",175,.23f);splash=Tone("Ripple",300,.08f);
            bite=Tone("Bite signal",880,.20f);hook=Tone("Hook set",540,.12f);
        }
        public static LineRenderer Line(Transform parent,string name,int count,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=Material;line.positionCount=count;line.startWidth=line.endWidth=width;line.useWorldSpace=true;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
        }
        public void Ring(Vector3 position,Color color,float radius=3,float duration=1.2f)
        {
            Ripple ripple=ripples.Find(r=>r.age>=r.duration);
            if(ripple==null){ripple=new Ripple{line=Line(root,"Surface ripple",49,.045f)};ripples.Add(ripple);}
            ripple.center=new Vector3(position.x,.05f,position.z);ripple.color=color;ripple.radius=radius;ripple.age=0;ripple.duration=duration;ripple.line.gameObject.SetActive(true);
        }
        public void FishingCue(int cue)
        {if(!Muted)sound.PlayOneShot(cue==0?splash:cue==1?hook:cue==2?bite:miss,cue==0?.5f:1);}
        public void CatchFeedback(bool caught,Vector3 position)
        {Ring(position,caught?new Color(.96f,.89f,.57f,.9f):new Color(1,.67f,.49f,.8f),4,1.6f);if(!Muted)sound.PlayOneShot(caught?success:miss);}
        public void Click(Vector3 position){Ring(position,new Color(.9f,.99f,.94f,.6f),2);if(!Muted)sound.PlayOneShot(splash,.35f);}
        public void ToggleMute(){Muted=!Muted;music.mute=sound.mute=Muted;}
        public void Spout(Vector3 point)
        {
            if(spoutMesh==null){var geometry=new Geometry();geometry.Sphere(Vector3.zero,new Vector3(.07f,.13f,.07f),Art.Hex("EEF8E5"),4,7);spoutMesh=geometry.Build("Spout droplet");}
            for(int i=0;i<12;i++)
            {
                float angle=i*Mathf.PI*2/12;
                var drop=drops.Find(d=>d.remaining<=0);
                if(drop==null){drop=new Drop{visual=Art.Object("Whale spray",spoutMesh,root,point)};drops.Add(drop);}
                drop.remaining=1.1f+i*.02f;drop.position=point;drop.velocity=new Vector3(Mathf.Cos(angle)*.6f,3.5f+i*.1f,Mathf.Sin(angle)*.6f);drop.visual.gameObject.SetActive(true);
            }
        }
        public void Tick(float dt)
        {
            foreach(var r in ripples)
            {
                if(r.age>=r.duration)continue;r.age+=dt;float t=Mathf.Clamp01(r.age/r.duration);float radius=.2f+t*r.radius;
                for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;r.points[i]=r.center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);r.points[i].y=OceanSimulation.Height(r.points[i],OceanSimulation.Time)+.045f;}
                r.line.SetPositions(r.points);Color color=r.color;color.a*=1-t;r.line.startColor=r.line.endColor=color;
                if(t>=1)r.line.gameObject.SetActive(false);
            }
            foreach(var drop in drops)
            {
                if(drop.remaining<=0)continue;drop.remaining-=dt;drop.velocity+=Vector3.down*9.81f*dt;drop.position+=drop.velocity*dt;drop.visual.position=drop.position;
                if(drop.position.y<OceanSimulation.Height(drop.position,OceanSimulation.Time))drop.remaining=0;
                if(drop.remaining<=0)drop.visual.gameObject.SetActive(false);
            }
        }
        static AudioClip Tone(string name,float frequency,float duration)
        {
            const int rate=22050;float[] data=new float[(int)(duration*rate)];
            for(int i=0;i<data.Length;i++){float t=(float)i/rate;data[i]=Mathf.Sin(t*frequency*Mathf.PI*2)*Mathf.Sin(Mathf.PI*i/data.Length)*.25f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        static AudioClip OceanSound()
        {
            const int rate=22050;float[] data=new float[rate*6];var random=new System.Random(2026);float smooth=0;
            for(int i=0;i<data.Length;i++) {smooth=Mathf.Lerp(smooth,(float)random.NextDouble()*2-1,.03f);data[i]=smooth*(.5f+.3f*Mathf.Sin(i/(float)rate*Mathf.PI/3));}
            // Cross-fade the loop seam.
            for(int i=0;i<3000;i++){float t=i/3000f;data[data.Length-3000+i]=Mathf.Lerp(data[data.Length-3000+i],data[i],t);}
            var clip=AudioClip.Create("Soft generated ocean",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
    }
}
