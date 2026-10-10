using UnityEngine;

namespace IslandVoyage
{
    public sealed class BoatController
    {
        public const float StartHeading=20;
        public readonly Transform Root, Visual;
        public readonly Rigidbody Body;
        public readonly BoatWake Wake;
        public Vector3 Velocity=>new Vector3(Body.linearVelocity.x,0,Body.linearVelocity.z);
        public float Speed
        {
            get
            {
                float speed=Velocity.magnitude;
                // Position subtraction introduces tiny errors at the slow/high-speed boundaries.
                if(Mathf.Abs(speed-VoyageRules.SlowSpeed)<.001f)return VoyageRules.SlowSpeed;
                if(Mathf.Abs(speed-VoyageRules.FastSpeed)<.001f)return VoyageRules.FastSpeed;
                return speed;
            }
        }
        public bool SlowHeld {get;private set;}
        readonly Vector3[] probes={new Vector3(-.64f,-.34f,-1.45f),new Vector3(.64f,-.34f,-1.45f),new Vector3(-.64f,-.34f,1.45f),new Vector3(.64f,-.34f,1.45f)};
        readonly Transform rudder,sailRig;
        readonly Quaternion rudderBindRotation,sailBindRotation;
        readonly Vector3 rudderYawAxis,sailYawAxis,sailBindPosition,sailPivot;
        Vector2 input;
        float steering;
        public BoatController()
        {
            Root=new GameObject("Player sailboat").transform;Root.position=new Vector3(0,0,8);Root.rotation=Quaternion.Euler(0,StartHeading,0);
            Visual=WorldBuilder.Boat(Root);
            var hull=Root.gameObject.AddComponent<BoxCollider>();hull.size=new Vector3(1.72f,.6f,4.15f);hull.center=new Vector3(0,-.05f,0);
            Body=Root.gameObject.AddComponent<Rigidbody>();Body.mass=320;Body.linearDamping=.06f;Body.angularDamping=1.1f;
            Body.centerOfMass=new Vector3(0,-.24f,0);Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            Body.maxAngularVelocity=1.3f;Wake=new BoatWake();rudder=BlenderAssets.Find(Visual,"Rudder");sailRig=BlenderAssets.Find(Visual,"SailRig");
            // FBX keeps a 90-degree axis conversion on the model's inner parent.
            // Rotate around the hull's upright axis, retaining the imported bind pose.
            rudderBindRotation=rudder.localRotation;rudderYawAxis=rudder.parent.InverseTransformDirection(Root.up);
            sailBindRotation=sailRig.localRotation;sailYawAxis=sailRig.parent.InverseTransformDirection(Root.up);
            sailBindPosition=sailRig.localPosition;
            sailPivot=sailRig.parent.InverseTransformPoint(Root.TransformPoint(new Vector3(0,1.03f,.20f)));
        }
        // X is rudder input, Y is signed hull-local throttle. Turning does not reduce throttle.
        public void SetInput(Vector2 value,bool slow){input=new Vector2(Mathf.Clamp(value.x,-1,1),Mathf.Clamp(value.y,-1,1));SlowHeld=slow;}
        public void PhysicsTick(float dt,float time)
        {
            foreach(var probe in probes)
            {
                Vector3 point=Root.TransformPoint(probe);float submerged=OceanSimulation.Height(point,time)-point.y;
                if(submerged>0)
                {
                    float buoyancy=1000*9.81f*.42f*Mathf.Min(submerged,.7f)-Body.GetPointVelocity(point).y*850;
                    Body.AddForceAtPosition(Vector3.up*Mathf.Clamp(buoyancy,-1000,5000),point);
                }
            }
            Vector3 forward=new Vector3(Root.forward.x,0,Root.forward.z).normalized;
            Vector3 velocity=Velocity;float longitudinal=Vector3.Dot(velocity,forward);Vector3 lateral=velocity-forward*longitudinal;
            float desiredSpeed=input.y*(SlowHeld?1.85f:input.y>=0?VoyageRules.MaxSpeed:2.5f);
            steering=Mathf.MoveTowards(steering,input.x,dt*2.6f);
            // Rudder authority comes from water flow; its yaw response reverses when backing up.
            float targetYaw=steering*.68f*Mathf.Clamp01(Mathf.Abs(longitudinal)/2.4f)*(longitudinal<-.15f?-1:1);
            Body.AddTorque(Vector3.up*Mathf.Clamp((targetYaw-Body.angularVelocity.y)*3,-1.5f,1.5f),ForceMode.Acceleration);
            float thrust=Mathf.Clamp((desiredSpeed-longitudinal)*1.5f,-3.2f,2.6f);
            Body.AddForce(forward*(thrust-longitudinal*Mathf.Abs(longitudinal)*.018f)*Body.mass-lateral*Body.mass*2.5f);
            // Air drag is much weaker than water drag; shelter reduces wind near the shore.
            Vector3 air=WeatherSystem.Wind-velocity;
            float shelter=Mathf.Lerp(.3f,1,Mathf.Clamp01(WorldLayout.ShoreDistance(Root.position)/20));
            Body.AddForce(air*air.magnitude*.20f*shelter);
            Vector3 clamped=WorldLayout.Clamp(Body.position,3);
            if((clamped-Body.position).sqrMagnitude>.0001f){Body.position=clamped;Body.linearVelocity=new Vector3(0,Body.linearVelocity.y,0);}
        }
        public void TickVisual(float dt,float time)
        {
            Wake.Tick(dt,time,Root,Velocity);rudder.localRotation=Quaternion.AngleAxis(-steering*24,rudderYawAxis)*rudderBindRotation;
            Shader.SetGlobalFloat("_BoatWaterMask",1);Shader.SetGlobalMatrix("_BoatWorldToLocal",Root.worldToLocalMatrix);
            float relativeWind=Mathf.DeltaAngle(Root.eulerAngles.y,Mathf.Atan2(WeatherSystem.Wind.x,WeatherSystem.Wind.z)*Mathf.Rad2Deg);
            var trim=Quaternion.AngleAxis(Mathf.Clamp(relativeWind*.25f,-18,18),sailYawAxis);
            sailRig.localRotation=Quaternion.Slerp(sailRig.localRotation,trim*sailBindRotation,1-Mathf.Exp(-dt*2));
            // Turn at the mast rather than swinging its attachment around the hull origin.
            sailRig.localPosition=sailPivot+(sailRig.localRotation*Quaternion.Inverse(sailBindRotation))*(sailBindPosition-sailPivot);
        }
        public void Reset()
        {
            Body.interpolation=RigidbodyInterpolation.None;
            Body.position=new Vector3(0,0,8);Body.rotation=Quaternion.Euler(0,StartHeading,0);Body.linearVelocity=Body.angularVelocity=Vector3.zero;
            Root.SetPositionAndRotation(Body.position,Body.rotation);Physics.SyncTransforms();Body.interpolation=RigidbodyInterpolation.Interpolate;
            input=Vector2.zero;steering=0;SlowHeld=false;Wake.Clear();
        }
    }
}
