using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IslandVoyage
{
    // Reuses the editable Blender resident skin; layered poses and two-bone IK keep hands on the tackle.
    public sealed class FishingPresentation
    {
        readonly GameBootstrap game;
        readonly FishingController fishing;
        readonly Transform actor,model,tackle,bobber,reelHandle;
        readonly LineRenderer rod,line,leader;
        readonly LineRenderer[] guides;
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        readonly Dictionary<Transform,Quaternion> bindRotations=new Dictionary<Transform,Quaternion>();
        readonly Dictionary<Transform,Vector3> bindPositions=new Dictionary<Transform,Vector3>();
        readonly Vector3[] rodPoints=new Vector3[19],linePoints=new Vector3[33];
        float pitch=28,crank,yaw;
        public Vector3 RodTip {get;private set;}
        public Vector3 Grip {get;private set;}
        public float HandError {get;private set;}
        public bool LineVisible=>line.gameObject.activeSelf;
        public Transform Angler=>actor;
        public FishingPresentation(GameBootstrap game,FishingController fishing)
        {
            this.game=game;this.fishing=fishing;
            actor=new GameObject("Seated angler · constrained hands").transform;actor.SetParent(game.Boat.Root,false);actor.localPosition=new Vector3(0,.21f,-1.88f);
            // The aft seat keeps the head behind the mainsail and the feet on the closed aft deck.
            var seat=new Geometry();seat.Box(new Vector3(0,.93f,-1.88f),new Vector3(.54f,.07f,.30f),Art.Hex("B58F63"),Quaternion.identity);
            for(int side=-1;side<=1;side+=2)seat.Box(new Vector3(side*.21f,.78f,-1.88f),new Vector3(.045f,.27f,.21f),Art.Hex("806347"),Quaternion.identity);
            Art.Object("Aft fishing seat",seat.Build("Aft fishing seat"),game.Boat.Root,Vector3.zero);
            model=BlenderAssets.Create("Resident0",actor,Vector3.zero);
            var animation=model.GetComponentInChildren<Animation>();animation.Stop();animation.enabled=false;
            foreach(var t in model.GetComponentsInChildren<Transform>())
            {bones[t.name]=t;bindRotations[t]=t.localRotation;bindPositions[t]=t.localPosition;}
            var root=new GameObject("Fishing tackle · flexible rod / line / float").transform;
            rod=VoyageEffects.Line(root,"Carbon rod with cork grip",19,.032f);rod.endWidth=.007f;
            rod.startColor=Art.Hex("344B48");rod.endColor=Art.Hex("637569");rod.generateLightingData=true;rod.sharedMaterial=Art.Material;
            line=VoyageEffects.Line(root,"Continuous fishing line",33,.009f);line.startColor=line.endColor=new Color(.93f,.93f,.77f,.85f);
            leader=VoyageEffects.Line(root,"Leader to hooked fish",2,.007f);leader.startColor=leader.endColor=new Color(.85f,.92f,.86f,.7f);
            guides=new LineRenderer[6];
            for(int i=0;i<guides.Length;i++)
            {guides[i]=VoyageEffects.Line(root,"Rod line guide",13,.006f);guides[i].startColor=guides[i].endColor=new Color(.70f,.74f,.69f,.95f);}
            var grip=new Geometry();
            grip.Cylinder(new Vector3(0,0,-.22f),new Vector3(0,0,.15f),.039f,.038f,Art.Hex("BCA576"),12);
            grip.Cylinder(new Vector3(0,0,-.24f),new Vector3(0,0,-.21f),.043f,.043f,Art.Hex("545D54"),12);
            grip.Cylinder(new Vector3(-.08f,-.11f,-.08f),new Vector3(.07f,-.11f,-.08f),.068f,.068f,Art.Hex("BBC4BA"),14);
            grip.Box(new Vector3(0,-.06f,-.08f),new Vector3(.032f,.10f,.09f),Art.Hex("576661"),Quaternion.identity);
            tackle=Art.Object("Cork handle and spinning reel",grip.Build("Detailed fishing tackle"),root,Vector3.zero);
            var handle=new Geometry();
            handle.Cylinder(Vector3.zero,new Vector3(0,.09f,0),.011f,.011f,Art.Hex("B5BCAF"),8);
            handle.Cylinder(new Vector3(0,.09f,0),new Vector3(-.06f,.09f,0),.018f,.018f,Art.Hex("514C43"),8);
            reelHandle=Art.Object("Reel crank",handle.Build("Reel crank"),tackle,new Vector3(-.10f,-.11f,-.08f));
            var floatShape=new Geometry();
            floatShape.Sphere(Vector3.zero,new Vector3(.072f,.11f,.072f),Art.Hex("F5E7C3"),8,12);
            floatShape.Cylinder(new Vector3(0,.035f,0),new Vector3(0,.29f,0),.028f,.019f,Art.Hex("DD704A"),10);
            floatShape.Cylinder(new Vector3(0,-.16f,0),new Vector3(0,-.08f,0),.015f,.015f,Art.Hex("455A56"),8);
            bobber=Art.Object("Red-tipped fishing float",floatShape.Build("Fishing float"),root,Vector3.zero);
            bobber.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            Tick(1);
        }
        void Rotate(string name,Vector3 axis,float degrees)
        {var bone=bones[name];bone.rotation=Quaternion.AngleAxis(degrees,actor.TransformDirection(axis))*bone.rotation;}
        public void Tick(float dt)
        {
            var stage=fishing.Stage;bool active=fishing.Active;
            Vector3 look=active?fishing.Aim-game.Boat.Root.position:game.Boat.Root.forward;
            look=game.Boat.Root.InverseTransformDirection(look);look.y=0;
            float desiredYaw=look.sqrMagnitude>.01f?Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg:0;
            yaw=Mathf.LerpAngle(yaw,desiredYaw,1-Mathf.Exp(-dt*4));actor.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(var pair in bindRotations){pair.Key.localRotation=pair.Value;pair.Key.localPosition=bindPositions[pair.Key];}
            foreach(string side in new[]{"L","R"})
            {Rotate("Thigh."+side,Vector3.right,-83);Rotate("Shin."+side,Vector3.right,92);Rotate("Foot."+side,Vector3.right,-9);}
            float tension=fishing.Fight==null?0:fishing.Fight.Tension;
            float lean=stage==FishingStage.Casting?-4*Mathf.Sin(Mathf.Clamp01(fishing.StageTime/VoyageRules.CastDuration)*Mathf.PI):stage==FishingStage.Fighting?-3-tension*4:-1;
            Rotate("Chest",Vector3.right,lean);Rotate("Head",Vector3.right,6);
            float desiredPitch=28;
            if(stage==FishingStage.Casting)
            {
                float t=fishing.StageTime;
                desiredPitch=t<.32f?Mathf.Lerp(28,104,Mathf.SmoothStep(0,1,t/.32f)):Mathf.Lerp(104,25,Mathf.SmoothStep(0,1,(t-.32f)/.30f));
            }
            else if(stage==FishingStage.Hooking)desiredPitch=83;
            else if(stage==FishingStage.Fighting)desiredPitch=32+tension*21+(fishing.Reeling?Mathf.Sin(game.WorldTime*3)*2:0);
            else if(stage==FishingStage.Landing)desiredPitch=68;
            else if(stage==FishingStage.Retrieving)desiredPitch=47;
            pitch=Mathf.Lerp(pitch,desiredPitch,1-Mathf.Exp(-dt*12));
            Grip=actor.TransformPoint(new Vector3(.10f,1.07f,.34f));
            Vector3 horizontal=actor.forward;
            Vector3 rodDirection=(horizontal*Mathf.Cos(pitch*Mathf.Deg2Rad)+actor.up*Mathf.Sin(pitch*Mathf.Deg2Rad)).normalized;
            tackle.position=Grip;tackle.rotation=Quaternion.LookRotation(rodDirection,actor.up);
            crank+=dt*(fishing.Reeling?570:stage==FishingStage.Retrieving?340:0);reelHandle.localRotation=Quaternion.Euler(crank,0,0);
            float bend=stage==FishingStage.Fighting?(.12f+tension*.48f):stage==FishingStage.Hooking?.18f:.035f;
            Vector3 load=active?(fishing.Bobber-Grip).normalized:-actor.up;
            for(int i=0;i<rodPoints.Length;i++)
            {float u=i/(float)(rodPoints.Length-1);rodPoints[i]=Grip+rodDirection*(u*2.5f)+load*(bend*u*u);}
            rod.SetPositions(rodPoints);RodTip=rodPoints[rodPoints.Length-1];
            for(int i=0;i<guides.Length;i++)
            {
                Vector3 point=rodPoints[3+i*3];float radius=Mathf.Lerp(.035f,.018f,i/5f);
                for(int j=0;j<13;j++){float a=j/12f*Mathf.PI*2;guides[i].SetPosition(j,point+tackle.right*Mathf.Cos(a)*radius+tackle.up*Mathf.Sin(a)*radius);}
            }
            Vector3 rightWrist=Grip+rodDirection*.035f+actor.up*.04f;
            Vector3 leftWrist=tackle.TransformPoint(new Vector3(-.12f,-.015f,-.09f));
            if(fishing.Reeling || stage==FishingStage.Retrieving)
            {float phase=crank*Mathf.Deg2Rad;leftWrist+=tackle.up*(Mathf.Cos(phase)*.03f)+tackle.forward*(Mathf.Sin(phase)*.03f);}
            SolveArm("R",rightWrist,actor.position+actor.right*.60f+actor.up*.90f-actor.forward*.12f);
            SolveArm("L",leftWrist,actor.position-actor.right*.55f+actor.up*.90f);
            HandError=Vector3.Distance(bones["Hand.R"].position,rightWrist);
            line.gameObject.SetActive(active);bobber.gameObject.SetActive(active);
            leader.gameObject.SetActive(active && fishing.Target!=null && fishing.Target.HasHookedFish);
            if(!active)return;
            bobber.position=fishing.Bobber;
            float dx=OceanSimulation.Height(bobber.position+Vector3.right*.1f,game.WorldTime)-OceanSimulation.Height(bobber.position-Vector3.right*.1f,game.WorldTime);
            float dz=OceanSimulation.Height(bobber.position+Vector3.forward*.1f,game.WorldTime)-OceanSimulation.Height(bobber.position-Vector3.forward*.1f,game.WorldTime);
            bobber.rotation=Quaternion.FromToRotation(Vector3.up,new Vector3(-dx/.2f,1,-dz/.2f).normalized);
            float sag=stage==FishingStage.Fighting?.035f+(1-tension)*.12f:stage==FishingStage.Casting?.16f:.28f;
            for(int i=0;i<linePoints.Length;i++)
            {
                float u=i/(float)(linePoints.Length-1);Vector3 point=Vector3.Lerp(RodTip,fishing.Bobber,u)-Vector3.up*Mathf.Sin(u*Mathf.PI)*sag;
                linePoints[i]=point;
            }
            line.SetPositions(linePoints);
            if(leader.gameObject.activeSelf){leader.SetPosition(0,fishing.Bobber);leader.SetPosition(1,fishing.Target.HookedMouth);}
        }
        void SolveArm(string side,Vector3 target,Vector3 pole)
        {
            Transform upper=bones["UpperArm."+side],lower=bones["Forearm."+side],hand=bones["Hand."+side];
            Vector3 shoulder=upper.position;float a=Vector3.Distance(shoulder,lower.position),b=Vector3.Distance(lower.position,hand.position);
            Vector3 axis=(target-shoulder).normalized;float d=Mathf.Clamp(Vector3.Distance(target,shoulder),Mathf.Abs(a-b)+.01f,a+b-.008f);
            Vector3 radial=Vector3.ProjectOnPlane(pole-shoulder,axis).normalized;
            if(radial.sqrMagnitude<.01f)radial=-actor.up;
            float along=(a*a+d*d-b*b)/(2*d),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Vector3 elbow=shoulder+axis*along+radial*height;
            upper.rotation=Quaternion.FromToRotation(lower.position-shoulder,elbow-shoulder)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,shoulder+axis*d-lower.position)*lower.rotation;
        }
    }
}
