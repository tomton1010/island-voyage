using UnityEngine;

namespace IslandVoyage
{
    public sealed class MarineAnimal
    {
        public readonly MarineKind Kind;
        public readonly Transform Root;
        readonly RigPlayback animation;
        readonly System.Random random;
        readonly float phase;
        Vector3 destination;
        Vector3 velocity;
        float age,duration,depth=-3.6f,jumpVelocity;
        bool airborne,breached,spouted;
        public bool Active=>Root.gameObject.activeSelf;
        public MarineAnimal(MarineKind kind,int index,Vector3 position,Transform parent)
        {
            Kind=kind;random=new System.Random(810+(int)kind*100+index);phase=index*1.3f;
            Root=BlenderAssets.Create(kind.ToString(),parent,position);
            animation=new RigPlayback(Root,"Swim",index*.23f);
            foreach(var renderer in Root.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Root.gameObject.SetActive(false);
        }
        public void Begin(Vector3 position,Vector3 goal,float lifetime)
        {
            destination=goal;age=0;duration=lifetime;depth=-3.6f;airborne=breached=spouted=false;
            velocity=(goal-position).normalized*(Kind==MarineKind.Whale?1.8f:Kind==MarineKind.Shark?2.5f:3.5f);
            Root.position=new Vector3(position.x,depth,position.z);Root.rotation=Quaternion.LookRotation(velocity);Root.gameObject.SetActive(true);
        }
        public void Tick(float dt,float time,Vector3 boat,VoyageEffects effects)
        {
            if(!Active)return;age+=dt;
            if(age>=duration){Root.gameObject.SetActive(false);return;}
            Vector3 p=Root.position;p.y=0;
            if(Vector3.Distance(p,destination)<6)destination=WorldLayout.RandomWater(random,Kind==MarineKind.Whale?12:8);
            Vector3 direction=destination-p;
            Vector3 away=p-boat;away.y=0;
            if(away.magnitude<9)direction+=away.normalized*35;
            float margin=Kind==MarineKind.Whale?12:7;
            direction+=WorldLayout.Avoidance(p+velocity*3,margin)*30+WorldLayout.Avoidance(p,margin)*40;
            direction.y=0;
            float turn=Vector3.SignedAngle(velocity,direction,Vector3.up),turnRate=Kind==MarineKind.Whale?14:28;
            Vector3 heading=Quaternion.Euler(0,Mathf.Clamp(turn,-turnRate*dt,turnRate*dt),0)*velocity.normalized;
            float speed=Kind==MarineKind.Whale?1.8f:Kind==MarineKind.Shark?2.5f:3.5f;
            velocity=Vector3.MoveTowards(velocity,heading*speed,dt*.85f);
            Vector3 moved=WorldLayout.Move(p,p+velocity*dt,Kind==MarineKind.Whale?6:3);
            float targetDepth=Kind==MarineKind.Whale?(age>20 && age<30?-.8f:-2.6f):Kind==MarineKind.Shark?-.85f:-1.0f;
            bool prepareBreach=Kind==MarineKind.Dolphin && !breached && age>18+phase*3 && age<duration-15 && WorldLayout.ShoreDistance(p)>15 && away.magnitude>9;
            if(prepareBreach)targetDepth=-.18f;
            if(age>duration-9)targetDepth=-5;
            float surface=OceanSimulation.Height(moved,time);
            if(prepareBreach && depth>-.23f && !airborne){airborne=true;breached=true;jumpVelocity=5.4f;}
            if(airborne)
            {
                jumpVelocity-=9.81f*dt;moved.y=Root.position.y+jumpVelocity*dt;
                if(jumpVelocity<0 && moved.y<surface-.25f)
                {airborne=false;depth=-.35f;effects.Ring(moved,new Color(.9f,.98f,.96f,.7f),3,1.5f);effects.Spout(moved+Vector3.up*.3f);}
            }
            else {depth=Mathf.MoveTowards(depth,targetDepth,dt*(prepareBreach?1.25f:.55f));moved.y=surface+depth;}
            Root.position=moved;
            Vector3 facing=velocity+Vector3.up*(airborne?jumpVelocity:0);
            Root.rotation=Quaternion.RotateTowards(Root.rotation,Quaternion.LookRotation(facing),dt*(airborne?110:turnRate));
            animation.Play(airborne?"Glide":"Swim",.2f);
            animation.SetSpeed(airborne?.85f:Mathf.Clamp(velocity.magnitude/speed,.5f,1.3f),dt);
            if(Kind==MarineKind.Whale && depth>-.95f && !spouted)
            {spouted=true;effects.Spout(Root.TransformPoint(new Vector3(0,.58f,.86f)));}
        }
    }
}
