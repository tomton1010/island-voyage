using System;
using UnityEngine;

namespace IslandVoyage
{
    public static class WorldLayout
    {
        public struct Island
        {
            public Vector2 center, size;
            public float phase;
            public Island(float x, float z, float rx, float rz, float phase) { center = new Vector2(x, z); size = new Vector2(rx, rz); this.phase = phase; }
            public float Edge(float angle) => 1 + .045f * Mathf.Sin(angle * 5 + phase) + .035f * Mathf.Cos(angle * 3 - phase);
            public float Distance(Vector3 p)
            {
                Vector2 q = new Vector2((p.x - center.x) / size.x, (p.z - center.y) / size.y);
                return (q.magnitude - Edge(Mathf.Atan2(q.y, q.x))) * Mathf.Min(size.x, size.y);
            }
        }
        public const float HalfWidth=260,HalfHeight=220;
        public const float SeabedHeight=-4.5f,ReefOuterRatio=1.7f;
        public static readonly Island[] Islands = {
            new Island(38,18,22,25,.5f), new Island(-45,35,18,20,2), new Island(-43,-35,8,7,4),
            new Island(-118,104,25,30,1.1f),new Island(122,116,31,26,3.2f),
            new Island(-132,-108,33,25,2.6f),new Island(151,-120,24,28,4.7f)
        };
        public static float ShoreDistance(Vector3 p)
        {
            float result = float.MaxValue;
            foreach (var island in Islands) result = Mathf.Min(result, island.Distance(p));
            return result;
        }
        public static Vector3 Clamp(Vector3 p, float margin = 0)
        { p.x = Mathf.Clamp(p.x, -HalfWidth + margin, HalfWidth - margin); p.z = Mathf.Clamp(p.z, -HalfHeight + margin, HalfHeight - margin); return p; }
        public static bool IsWater(Vector3 p, float margin = 1,float minimumDepth=0)
            => Mathf.Abs(p.x) <= HalfWidth - margin && Mathf.Abs(p.z) <= HalfHeight - margin && ShoreDistance(p) > margin && (minimumDepth<=0 || GroundHeight(p)<=-minimumDepth);
        public static Vector3 Move(Vector3 from, Vector3 proposed, float margin = 1,float minimumDepth=0)
        {
            proposed = Clamp(proposed, margin);
            if (IsWater(proposed, margin,minimumDepth)) return proposed;
            Vector3 xOnly = new Vector3(proposed.x, from.y, from.z), zOnly = new Vector3(from.x, from.y, proposed.z);
            bool x = IsWater(xOnly, margin,minimumDepth), z = IsWater(zOnly, margin,minimumDepth);
            if (x && z) return (xOnly - from).sqrMagnitude > (zOnly - from).sqrMagnitude ? xOnly : zOnly;
            return x ? xOnly : z ? zOnly : from;
        }
        public static Vector3 Avoidance(Vector3 position, float margin = 4,float minimumDepth=0)
        {
            Vector3 force = Vector3.zero;
            if (ShoreDistance(position) < margin)
            {
                force.x = ShoreDistance(position + Vector3.right * .3f) - ShoreDistance(position - Vector3.right * .3f);
                force.z = ShoreDistance(position + Vector3.forward * .3f) - ShoreDistance(position - Vector3.forward * .3f);
                force = force.normalized * 3;
            }
            if(minimumDepth>0 && GroundHeight(position)>-minimumDepth-1)
            {
                Vector3 deeper=new Vector3(GroundHeight(position-Vector3.right*.3f)-GroundHeight(position+Vector3.right*.3f),0,GroundHeight(position-Vector3.forward*.3f)-GroundHeight(position+Vector3.forward*.3f));
                force+=deeper.normalized*3;
            }
            if (Mathf.Abs(position.x) > HalfWidth-12) force.x -= Mathf.Sign(position.x) * 3;
            if (Mathf.Abs(position.z) > HalfHeight-12) force.z -= Mathf.Sign(position.z) * 3;
            return force;
        }
        public static Vector3 RandomWater(System.Random random, float margin = 3,float minimumDepth=0)
        {
            for (int i = 0; i < 200; i++)
            {
                Vector3 p = new Vector3(((float)random.NextDouble()*2-1)*(HalfWidth-14), 0, ((float)random.NextDouble()*2-1)*(HalfHeight-14));
                if (IsWater(p, margin,minimumDepth)) return p;
            }
            return new Vector3(-10, 0, 0);
        }
        // Ratios are measured against the irregular shoreline, shared by scenery and colliders.
        public static float LandHeight(Island island, float ratio)
            => ratio<=1 ? 1.8f*(1-ratio*ratio) : Mathf.Lerp(0,SeabedHeight,Mathf.SmoothStep(0,1,(ratio-1)/(ReefOuterRatio-1)));
        public static float IslandRatio(Island island,Vector3 position)
        {
            Vector2 q=new Vector2((position.x-island.center.x)/island.size.x,(position.z-island.center.y)/island.size.y);
            return q.magnitude/island.Edge(Mathf.Atan2(q.y,q.x));
        }
        public static float GroundHeight(Vector3 position)
        {
            float height=SeabedHeight;
            foreach(var island in Islands)
            {
                if(Mathf.Abs(position.x-island.center.x)>island.size.x*ReefOuterRatio*1.08f || Mathf.Abs(position.z-island.center.y)>island.size.y*ReefOuterRatio*1.08f)continue;
                float ratio=IslandRatio(island,position);
                if(ratio<ReefOuterRatio)height=Mathf.Max(height,LandHeight(island,ratio));
            }
            return height;
        }
        public static Vector3 OnGround(Vector3 position,float offset=0)
            => new Vector3(position.x,GroundHeight(position)+offset,position.z);
        public static float SubmergedHeight(Vector3 position,float depth,float time,float clearance=.45f)
        {
            float surface=OceanSimulation.Height(position,time);
            return Mathf.Clamp(surface-depth,GroundHeight(position)+clearance,surface-.45f);
        }
    }
}
