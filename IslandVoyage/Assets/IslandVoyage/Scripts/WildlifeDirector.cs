using System.Collections.Generic;
using UnityEngine;

namespace IslandVoyage
{
    public sealed class WildlifeDirector
    {
        public readonly List<MarineAnimal> Animals=new List<MarineAnimal>();
        readonly System.Random random=new System.Random(9152);
        float nextEncounter=45;
        public int ActiveCount {get{int count=0;foreach(var animal in Animals)if(animal.Active)count++;return count;}}
        public WildlifeDirector()
        {
            var root=new GameObject("Occasional wildlife · maximum two active").transform;
            Animals.Add(new MarineAnimal(MarineKind.Whale,0,Vector3.zero,root));
            Animals.Add(new MarineAnimal(MarineKind.Shark,0,Vector3.zero,root));
            Animals.Add(new MarineAnimal(MarineKind.Dolphin,0,Vector3.zero,root));
            Animals.Add(new MarineAnimal(MarineKind.Dolphin,1,Vector3.zero,root));
        }
        public void Tick(float dt,float time,Vector3 boat,Camera view,VoyageEffects effects)
        {
            foreach(var animal in Animals)animal.Tick(dt,time,boat,effects);
            if(time<nextEncounter || ActiveCount>0)return;
            nextEncounter=time+90+(float)random.NextDouble()*55;
            float choice=(float)random.NextDouble();MarineKind kind=choice<.13f?MarineKind.Whale:choice<.40f?MarineKind.Shark:MarineKind.Dolphin;
            Spawn(kind,boat,view);
        }
        public bool Spawn(MarineKind kind,Vector3 boat,Camera view)
        {
            if(ActiveCount>0)return false;Vector3 start=Vector3.zero;bool found=false;
            float minimumDepth=Animals.Find(animal=>animal.Kind==kind).RequiredWaterDepth;
            for(int i=0;i<100;i++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2,r=75+(float)random.NextDouble()*45;
                Vector3 p=boat+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);p.y=0;
                if(!WorldLayout.IsWater(p,14,minimumDepth))continue;
                Vector3 screen=view.WorldToViewportPoint(p);
                if(screen.z>0 && screen.x>-.1f && screen.x<1.1f && screen.y>-.1f && screen.y<1.1f)continue;
                start=p;found=true;break;
            }
            if(!found)return false;
            Vector3 goal=boat+(start-boat).normalized*20;goal.y=0;
            if(!WorldLayout.IsWater(goal,12,minimumDepth))goal=WorldLayout.RandomWater(random,14,minimumDepth);
            int requested=kind==MarineKind.Dolphin && random.NextDouble()<.45?2:1,spawned=0;
            foreach(var animal in Animals)if(animal.Kind==kind && spawned<requested)
            {
                Vector3 p=start+new Vector3(spawned*4,0,-spawned*2);
                if(!WorldLayout.IsWater(p,12,minimumDepth))continue;
                animal.Begin(p,goal+new Vector3(spawned*4,0,-spawned*2),75);spawned++;
            }
            return spawned>0;
        }
    }
}
