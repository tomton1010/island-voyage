using System.Collections.Generic;
using UnityEngine;

namespace IslandVoyage
{
    public sealed class IslandResidents
    {
        sealed class Walker
        {
            public Transform root;
            public CharacterController controller;
            public RigPlayback animation;
            public Vector3[] route;public Vector3 velocity;public int waypoint;public float gait,wait,gravity,pace,stuck;
        }
        readonly List<Walker> walkers=new List<Walker>();
        public int Count=>walkers.Count;
        public IslandResidents()
        {
            Transform root=new GameObject("Island residents · walking, resting, gardening").transform;
            Vector3[] main={new Vector3(28,0,9),new Vector3(31,0,3),new Vector3(37,0,2),new Vector3(47,0,5),new Vector3(49,0,17),new Vector3(45,0,26),new Vector3(34,0,27),new Vector3(27,0,21)};
            for(int i=0;i<4;i++)Create(root,main,i*2,i);
            for(int k=1;k<=2;k++)
            {
                var island=WorldLayout.Islands[k==1?1:4];Vector3 center=new Vector3(island.center.x,0,island.center.y);
                var route=new[]{center+new Vector3(-island.size.x*.45f,0,-island.size.y*.35f),center+new Vector3(island.size.x*.3f,0,-island.size.y*.4f),center+new Vector3(island.size.x*.45f,0,island.size.y*.35f),center+new Vector3(-island.size.x*.4f,0,island.size.y*.4f)};
                Create(root,route,k,k+3);
            }
        }
        void Create(Transform parent,Vector3[] route,int waypoint,int index)
        {
            var walker=new Walker{route=route,waypoint=waypoint%route.Length,pace=.68f+(index%3)*.14f,gait=index*1.7f};
            walker.root=new GameObject("Resident "+(index+1)).transform;walker.root.SetParent(parent,false);
            Vector3 start=route[walker.waypoint];start.y=WorldLayout.GroundHeight(start)+.06f;walker.root.position=start;walker.waypoint=(walker.waypoint+1)%route.Length;
            var model=BlenderAssets.Create("Resident"+(index%3),walker.root,Vector3.zero);
            walker.animation=new RigPlayback(model,"Idle",index*.17f,true);
            walker.controller=walker.root.gameObject.AddComponent<CharacterController>();walker.controller.height=1.8f;walker.controller.radius=.24f;walker.controller.center=new Vector3(0,.9f,0);walker.controller.stepOffset=.23f;walker.controller.slopeLimit=42;
            walkers.Add(walker);
        }
        public void Tick(float dt,float time)
        {
            foreach(var w in walkers)
            {
                Vector3 previous=w.root.position,delta=w.route[w.waypoint]-previous;delta.y=0;
                if(delta.magnitude<.65f){w.waypoint=(w.waypoint+1)%w.route.Length;w.wait=3+(w.waypoint%3)*3;}
                w.wait=Mathf.Max(0,w.wait-dt);Vector3 desired=w.wait>0?Vector3.zero:delta.normalized*w.pace;
                foreach(var other in walkers)if(other!=w)
                {Vector3 away=previous-other.root.position;away.y=0;if(away.sqrMagnitude<1 && away.sqrMagnitude>.001f)desired+=away.normalized*.8f;}
                w.velocity=Vector3.MoveTowards(w.velocity,desired,dt*1.2f);
                if(w.velocity.sqrMagnitude>.015f)w.root.rotation=Quaternion.RotateTowards(w.root.rotation,Quaternion.LookRotation(w.velocity),dt*80);
                if(w.controller.isGrounded && w.gravity<0)w.gravity=-1;else w.gravity=Mathf.Max(-12,w.gravity-9.81f*dt);
                w.controller.Move((w.velocity+Vector3.up*w.gravity)*dt);
                float speed=(new Vector2(w.root.position.x-previous.x,w.root.position.z-previous.z)).magnitude/Mathf.Max(dt,.001f);
                if(w.wait<=0 && speed<.1f)w.stuck+=dt;else w.stuck=0;
                if(w.stuck>3){w.waypoint=(w.waypoint+1)%w.route.Length;w.stuck=0;}
                w.animation.Play(speed>.09f?"Walk":w.wait>3 && w.waypoint%3==0?"Wave":"Idle",.35f);
                w.animation.SetSpeed(speed>.09f?Mathf.Clamp(speed/.639f,.15f,1.65f):1,dt);
            }
        }
    }
}
