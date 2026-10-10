using System.Collections.Generic;
using UnityEngine;

namespace IslandVoyage
{
    public static class SchoolPlanner
    {
        public static void Populate(List<FishSchool> schools,Transform parent)
        {
            var random=new System.Random(7016);
            schools.Add(new FishSchool(0,new Vector3(3,0,12),13,parent,0,5.5f));
            for(int species=0;species<8;species++)for(int group=species==0?1:0;group<5;group++)
            {
                Vector3 p=Vector3.zero;float spread=5.5f+(float)random.NextDouble()*4;bool found=false;
                for(int attempt=0;attempt<500;attempt++)
                {
                    if(species<5)
                    {
                        var island=WorldLayout.Islands[random.Next(WorldLayout.Islands.Length)];float a=(float)random.NextDouble()*Mathf.PI*2;
                        float distance=spread+4+(float)random.NextDouble()*10;
                        p=new Vector3(island.center.x+Mathf.Cos(a)*(island.size.x+distance),0,island.center.y+Mathf.Sin(a)*(island.size.y+distance));
                    }
                    else p=WorldLayout.RandomWater(random,22);
                    if(!WorldLayout.IsWater(p,spread+3,2))continue;
                    bool crowded=false;foreach(var existing in schools)if(Vector3.Distance(existing.Home,p)<spread+existing.Spread+13){crowded=true;break;}
                    if(crowded)continue;found=true;break;
                }
                if(!found)continue;
                schools.Add(new FishSchool(species,p,random.Next(10,19),parent,schools.Count,spread));
            }
        }
    }
}
