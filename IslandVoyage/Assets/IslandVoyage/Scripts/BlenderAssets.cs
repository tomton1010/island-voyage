using System;
using System.Collections.Generic;
using UnityEngine;

namespace IslandVoyage
{
    public static class BlenderAssets
    {
        public static readonly string[] FishNames={"Clownfish","BlueTang","YellowTang","Butterflyfish","Damselfish","Sardine","Mackerel","FlyingFish"};
        static readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
        public static Transform Create(string name,Transform parent,Vector3 position)
        {
            if(!prefabs.TryGetValue(name,out var prefab))
            {
                prefab=Resources.Load<GameObject>("Blender/Models/"+name);
                if(prefab==null)throw new InvalidOperationException("Missing Blender asset: "+name);
                prefabs.Add(name,prefab);
            }
            var instance=UnityEngine.Object.Instantiate(prefab,parent,false).transform;
            instance.name=name;instance.localPosition=position;instance.localRotation=Quaternion.identity;return instance;
        }
        public static Transform Find(Transform root,string name)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;
            throw new InvalidOperationException("Missing Blender transform: "+name);
        }
        public static void Wind(Transform model,float phase)
        {new RigPlayback(model,"Wind",phase).SetSpeed(.85f+Mathf.Repeat(phase,.3f),1);}
    }
    public sealed class RigPlayback
    {
        readonly Animation animation;
        string current;
        float speed=1;
        public RigPlayback(Transform model,string clip,float phase=0,bool alwaysAnimate=false)
        {
            animation=model.GetComponentInChildren<Animation>();
            if(animation==null)throw new InvalidOperationException("Blender model has no animation: "+model.name);
            animation.playAutomatically=false;animation.cullingType=alwaysAnimate?AnimationCullingType.AlwaysAnimate:AnimationCullingType.BasedOnRenderers;
            foreach(AnimationState state in animation){state.wrapMode=WrapMode.Loop;state.speed=1;}
            Play(clip,0);animation[clip].normalizedTime=Mathf.Repeat(phase,1);
        }
        public void Play(string clip,float fade=.3f)
        {
            if(current==clip)return;
            if(animation[clip]==null)throw new InvalidOperationException("Missing Blender action: "+clip);
            current=clip;if(fade<=0)animation.Play(clip);else animation.CrossFade(clip,fade);
        }
        public void SetSpeed(float target,float dt)
        {
            speed=Mathf.Lerp(speed,target,1-Mathf.Exp(-dt*5));
            if(current!=null)animation[current].speed=speed;
        }
    }
}
