using UnityEngine;

namespace IslandVoyage
{
    public sealed class BoatFollowCamera
    {
        public const float DefaultDistance=11.5f,MinDistance=8,MaxDistance=24;
        public static readonly float DefaultElevation=Mathf.Atan(.52f)*Mathf.Rad2Deg;
        public const float MinElevation=12,MaxElevation=65;
        public float Distance {get;private set;}=DefaultDistance;
        public float OrbitYaw {get;private set;}
        public float Elevation {get;private set;}=DefaultElevation;
        public bool Dragging {get;private set;}
        readonly Camera view;
        readonly Transform boat;
        readonly RaycastHit[] obstacles=new RaycastHit[16];
        Vector3 positionVelocity;
        float heading,headingVelocity,currentDistance,currentElevation=DefaultElevation;
        Vector2 lastPointer;

        public BoatFollowCamera(Camera view,Transform boat)
        {
            this.view=view;this.boat=boat;
            view.orthographic=false;view.fieldOfView=58;view.nearClipPlane=.15f;view.farClipPlane=550;
            view.clearFlags=CameraClearFlags.Skybox;Snap();
        }
        public void Zoom(float change)=>Distance=Mathf.Clamp(Distance+change,MinDistance,MaxDistance);
        public void ResetZoom()=>Distance=DefaultDistance;
        public void ResetView(){ResetZoom();OrbitYaw=0;Elevation=DefaultElevation;Dragging=false;}
        // Positions are sampled once per frame. Release, focus loss and overlays end the gesture.
        public void UpdatePointer(Vector2 pointer,bool down,bool held,bool allowed,bool overUI)
        {
            if(!allowed || !held){Dragging=false;return;}
            if(down)
            {
                Dragging=!overUI;lastPointer=pointer;return;
            }
            if(!Dragging)return;
            Vector2 delta=pointer-lastPointer;lastPointer=pointer;
            OrbitYaw=Mathf.DeltaAngle(0,OrbitYaw+delta.x*.24f*1600/Mathf.Max(1,Screen.width));
            Elevation=Mathf.Clamp(Elevation-delta.y*.14f*900/Mathf.Max(1,Screen.height),MinElevation,MaxElevation);
        }
        public void Snap()
        {
            heading=boat.eulerAngles.y+OrbitYaw;headingVelocity=0;positionVelocity=Vector3.zero;currentDistance=Distance;currentElevation=Elevation;
            Tick(0,true);
        }
        public void Tick(float dt,bool snap=false)
        {
            // Follow yaw only, keeping the horizon level when buoyancy pitches or rolls the hull.
            if(!snap)
            {
                heading=Mathf.SmoothDampAngle(heading,boat.eulerAngles.y+OrbitYaw,ref headingVelocity,.16f,Mathf.Infinity,dt);
                currentDistance=Mathf.Lerp(currentDistance,Distance,1-Mathf.Exp(-dt*6));
                currentElevation=Mathf.Lerp(currentElevation,Elevation,1-Mathf.Exp(-dt*10));
            }
            Vector3 forward=Quaternion.Euler(0,heading,0)*Vector3.forward;
            float framing=Mathf.Clamp(1.6f/Mathf.Max(.8f,view.aspect),1,1.4f);
            float distance=currentDistance*framing;
            Vector3 anchor=new Vector3(boat.position.x,Mathf.Clamp(boat.position.y,-.3f,.3f),boat.position.z);
            Vector3 focus=anchor+Vector3.up*1.4f+forward*2.4f;
            Vector3 desired=anchor-forward*distance+Vector3.up*(distance*Mathf.Tan(currentElevation*Mathf.Deg2Rad)+1.25f);
            desired.y=Mathf.Max(desired.y,Mathf.Max(WorldLayout.GroundHeight(desired)+.7f,OceanSimulation.Height(desired,Time.time)+.9f));
            desired=ClearBoom(focus,desired);
            Vector3 position=snap?desired:Vector3.SmoothDamp(view.transform.position,desired,ref positionVelocity,.12f,Mathf.Infinity,dt);
            // Check the interpolated position as well, so turning cannot sweep through a wall.
            position=ClearBoom(focus,position);
            view.transform.SetPositionAndRotation(position,Quaternion.LookRotation(focus-position,Vector3.up));
        }
        Vector3 ClearBoom(Vector3 focus,Vector3 position)
        {
            Vector3 boom=position-focus;float length=boom.magnitude;
            if(length<.001f)return position;
            int count=Physics.SphereCastNonAlloc(focus,.35f,boom/length,obstacles,length,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            float clearLength=length;
            for(int i=0;i<count;i++)
                if(!obstacles[i].transform.IsChildOf(boat))clearLength=Mathf.Min(clearLength,Mathf.Max(.2f,obstacles[i].distance-.2f));
            return focus+boom/length*clearLength;
        }
    }
}
