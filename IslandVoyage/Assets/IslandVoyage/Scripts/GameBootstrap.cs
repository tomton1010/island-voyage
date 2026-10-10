using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IslandVoyage
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public BoatController Boat {get;private set;}
        public readonly List<FishSchool> Schools=new List<FishSchool>();
        public readonly List<MarineAnimal> Animals=new List<MarineAnimal>();
        public readonly int[] Catches=new int[8];
        public readonly float[] BestWeights=new float[8];
        public FishingController Fishing {get;private set;}
        public Camera View {get;private set;}
        public VoyageEffects Effects {get;private set;}
        public VoyageHud Hud {get;private set;}
        public WildlifeDirector Wildlife {get;private set;}
        public DayNightCycle Cycle {get;private set;}
        public IslandResidents Residents {get;private set;}
        public BoatFollowCamera CameraRig {get;private set;}
        public WeatherSystem Weather {get;private set;}
        public SeaEventDirector Events {get;private set;}
        public IEnumerable<FishSchool> NavigableSchools
        {
            get
            {
                foreach(var school in Schools)yield return school;
                if(Events!=null && Events.MigrationVisible)foreach(var school in Events.MigrationSchools)yield return school;
            }
        }
        public float WorldTime {get;private set;}
        public bool Paused {get;private set;}
        public float Cooldown=>Fishing.Cooldown;
        public bool Casting=>Fishing.Active;
        public FishSchool Nearby {get;private set;}
        public bool CanCast=>Fishing.CanStart;
        bool hadFocus=true;
        bool testing,runtimeChecked,previewSailing,previewFishing,fishingTestReel,fishingValidated;

        void Awake()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;
            Time.timeScale=1;Time.fixedDeltaTime=.02f;Time.maximumDeltaTime=.1f;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--asset-gallery")>=0){enabled=false;gameObject.AddComponent<AssetGallery>();return;}
            testing=Array.IndexOf(Environment.GetCommandLineArgs(),"--self-test")>=0;
            previewSailing=Array.IndexOf(Environment.GetCommandLineArgs(),"--sailing")>=0;
            previewFishing=Array.IndexOf(Environment.GetCommandLineArgs(),"--fishing-demo")>=0;
            Shader.SetGlobalFloat("_BoatWaterMask",0);
            Weather=new WeatherSystem(!testing);
            WorldBuilder.Create();
            Effects=new VoyageEffects();Boat=new BoatController();
            var cameraObject=new GameObject("Ocean camera",typeof(Camera),typeof(AudioListener));View=cameraObject.GetComponent<Camera>();cameraObject.tag="MainCamera";
            VoyageLook.Configure(View);
            CameraRig=new BoatFollowCamera(View,Boat.Root);
            Cycle=new DayNightCycle(View);Residents=new IslandResidents();Wildlife=new WildlifeDirector();Animals.AddRange(Wildlife.Animals);
            Transform fishRoot=new GameObject("Natural reef and pelagic fish distribution").transform;SchoolPlanner.Populate(Schools,fishRoot);
            Events=new SeaEventDirector(this,!testing);
            Fishing=new FishingController(this,testing||previewFishing);
            string[] startArgs=Environment.GetCommandLineArgs();int hourIndex=Array.IndexOf(startArgs,"--hour");
            if(hourIndex>=0 && hourIndex+1<startArgs.Length && float.TryParse(startArgs[hourIndex+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float hour))Cycle.SetHour(hour);
            Hud=new VoyageHud(this);FindNearby();
            Hud.Toast("停船靠近鱼群 · E 抛竿 / 提竿 · 空格收线 · Q 收竿",5);
            int weatherIndex=Array.IndexOf(startArgs,"--weather"),eventIndex=Array.IndexOf(startArgs,"--event");
            if(weatherIndex>=0 && weatherIndex+1<startArgs.Length && Enum.TryParse(startArgs[weatherIndex+1],true,out WeatherKind weather))Weather.SetWeather(weather,true);
            if(eventIndex>=0 && eventIndex+1<startArgs.Length && Enum.TryParse(startArgs[eventIndex+1],true,out SeaEventKind encounter))Events.Start(encounter,true);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--scenic")>=0)Hud.ToggleScenic();
            if(previewFishing)
            {
                CameraRig.UpdatePointer(Vector2.zero,true,true,true,false);
                CameraRig.UpdatePointer(new Vector2(Screen.width*.17f,0),false,true,true,false);
                CameraRig.UpdatePointer(Vector2.zero,false,false,true,false);
            }
            ApplyPreviewView(startArgs);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--self-test")>=0)StartCoroutine(RuntimeValidation());
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--capture")>=0)StartCoroutine(Capture());
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.H))Hud.ToggleScenic();
            if(Input.GetKeyDown(KeyCode.B) && !Paused)Hud.ToggleBook();
            if(Input.GetKeyDown(KeyCode.T))Cycle.ChangeRate();
            if(Application.isFocused)
            {
                if(Input.GetKeyDown(KeyCode.Equals)||Input.GetKeyDown(KeyCode.KeypadPlus))AdjustZoom(-2);
                if(Input.GetKeyDown(KeyCode.Minus)||Input.GetKeyDown(KeyCode.KeypadMinus))AdjustZoom(2);
                if(Input.GetKeyDown(KeyCode.C))CameraRig.ResetView();
                if(!(EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject()))AdjustZoom(-Input.mouseScrollDelta.y*1.5f);
            }
            if(Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            {if(Hud.HasModal)Hud.CloseModal();else SetPaused(!Paused);}
            bool focus=Application.isFocused;
            if(!testing)CameraRig.UpdatePointer(Input.mousePosition,Input.GetMouseButtonDown(1),Input.GetMouseButton(1),focus&&!Paused&&!Hud.HasModal,EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject());
            if(!testing && !previewFishing && !focus && hadFocus && Fishing.Active)SetPaused(true);hadFocus=focus;
            Time.timeScale=Paused||Hud.HasModal?0:1;
            if(Paused || Hud.HasModal)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);WorldTime=Time.time;
            if(Input.GetKeyDown(KeyCode.Y))CycleWeather();
            if(Input.GetKeyDown(KeyCode.N))Events.PreviewNext();
            Weather.Tick(dt,WorldTime,Boat.Root.position,Effects.Muted);Events.Tick(dt,WorldTime);
            OceanSimulation.Tick(Time.time,Boat.Root.position);Cycle.Tick(dt);
            Vector2 move=Vector2.zero;
            if(focus)
            {
                move.x=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
                move.y=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
            }
            if(previewSailing)Boat.SetInput(Vector2.up,false);
            else if(!testing)Boat.SetInput(move,focus&&(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)));
            Boat.TickVisual(dt,WorldTime);
            foreach(var school in Schools)school.Tick(dt,WorldTime,Boat.Root.position,Boat.Speed);
            Wildlife.Tick(dt,WorldTime,Boat.Root.position,View,Effects);Residents.Tick(dt,WorldTime);
            FindNearby();
            bool overUI=EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
            if(focus && !testing && !previewFishing)
            {
                if(Input.GetMouseButtonDown(0) && !Fishing.Active && !overUI)ClickWater();
                if(Input.GetKeyDown(KeyCode.E))TryCast();
                if(Input.GetKeyDown(KeyCode.Q))Fishing.Cancel();
            }
            if(previewFishing)
            {
                if(Fishing.Stage==FishingStage.Idle || Fishing.Stage==FishingStage.Bite)Fishing.PressAction();
                if(Fishing.Fight!=null){if(Fishing.Fight.Tension>.63f)fishingTestReel=false;else if(Fishing.Fight.Tension<.33f)fishingTestReel=true;}
            }
            bool reel=testing||previewFishing?fishingTestReel:focus&&(Input.GetKey(KeyCode.Space)||(Input.GetMouseButton(0)&&!overUI));
            Fishing.Tick(dt,reel);Effects.Tick(dt);
        }
        void LateUpdate()
        {
            if(CameraRig==null)return;CameraRig.Tick(Mathf.Min(Time.unscaledDeltaTime,.05f));Fishing.Visual.Tick(Mathf.Min(Time.deltaTime,.05f));Hud.Tick();
        }
        void FixedUpdate(){if(Boat!=null && !Paused && !Hud.HasModal)Boat.PhysicsTick(Time.fixedDeltaTime,Time.fixedTime);}
        public void AdjustZoom(float change)=>CameraRig.Zoom(change);
        public void CycleWeather(){if(Paused || Hud.HasModal)return;Weather.Cycle();Hud.Toast("天气转为"+Weather.Name+"，海况逐渐变化",3);}
        void FindNearby()
        {
            Nearby=null;float nearest=16;
            foreach(var school in NavigableSchools)
            {float distance=school.DistanceFrom(Boat.Root.position);if(distance<nearest && school.ActiveCount>0){nearest=distance;Nearby=school;}}
            // Prefer the closest eligible school if another nearby one is frightened.
            FishSchool eligible=null;float best=VoyageRules.FishingRange+.001f;
            foreach(var school in NavigableSchools)
            {float distance=school.DistanceFrom(Boat.Root.position);if(distance<best && school.Mood==SchoolMood.Calm && school.ActiveCount>0){best=distance;eligible=school;}}
            if(eligible!=null)Nearby=eligible;
        }
        void ClickWater()
        {
            Ray ray=View.ScreenPointToRay(Input.mousePosition);var plane=new Plane(Vector3.up,Vector3.zero);
            if(!plane.Raycast(ray,out float distance))return;
            if(distance>View.farClipPlane || Physics.Raycast(ray,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return;
            Vector3 p=ray.GetPoint(distance);if(!WorldLayout.IsWater(p,.1f))return;Effects.Click(p);
            FishSchool hit=null;float nearest=2;
            foreach(var school in NavigableSchools){float d=school.DistanceFrom(p);if(d<nearest){nearest=d;hit=school;}}
            if(hit!=null){hit.Spook(p,WorldTime);Hud.Toast(hit.Name+"群受到了惊吓",2);}
        }
        public void TryCast()=>Fishing.PressAction();
        public void SetPaused(bool value){Paused=value;Time.timeScale=value||Hud.HasModal?0:1;Hud.ShowPause(value);}
        public void ResetBoat(){Fishing.Reset();Boat.Reset();CameraRig.Snap();SetPaused(false);Hud.Toast("小船已返回出发位置",2);}
        public int TotalCatch {get{int total=0;foreach(int count in Catches)total+=count;return total;}}

        void ApplyPreviewView(string[] args)
        {
            int positionIndex=Array.IndexOf(args,"--boat-position");
            if(positionIndex>=0 && positionIndex+1<args.Length)
            {
                string[] coordinates=args[positionIndex+1].Split(',');
                if(coordinates.Length!=2)throw new ArgumentException("Preview boat position requires x,z");
                var position=new Vector3(float.Parse(coordinates[0],System.Globalization.CultureInfo.InvariantCulture),0,float.Parse(coordinates[1],System.Globalization.CultureInfo.InvariantCulture));
                if(!WorldLayout.IsWater(position,2,1))throw new ArgumentException("Preview boat position must be navigable water");
                Boat.Body.position=position;Boat.Root.position=position;Physics.SyncTransforms();
            }
            float PreviewFloat(string flag,float fallback)
            {int index=Array.IndexOf(args,flag);return index>=0 && index+1<args.Length?float.Parse(args[index+1],System.Globalization.CultureInfo.InvariantCulture):fallback;}
            if(positionIndex>=0 || Array.IndexOf(args,"--view-yaw")>=0 || Array.IndexOf(args,"--view-elevation")>=0 || Array.IndexOf(args,"--view-distance")>=0)
                CameraRig.SetPreviewView(PreviewFloat("--view-yaw",CameraRig.OrbitYaw),PreviewFloat("--view-elevation",CameraRig.Elevation),PreviewFloat("--view-distance",CameraRig.Distance));
        }

        IEnumerator Capture()
        {
            string[] captureArgs=Environment.GetCommandLineArgs();int delayIndex=Array.IndexOf(captureArgs,"--capture-delay");
            float delay=3;if(delayIndex>=0 && delayIndex+1<captureArgs.Length)float.TryParse(captureArgs[delayIndex+1],out delay);
            yield return new WaitForSeconds(Mathf.Max(3,delay));
            while(testing && !runtimeChecked)yield return null;
            yield return null;
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture");
            if(index>=0 && index+1<args.Length)
            {
                string path=System.IO.Path.GetFullPath(args[index+1]);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                var frame=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var previous=RenderTexture.active;float aspect=View.aspect;Texture2D capture=null;
                try
                {
                    Debug.Log("VOYAGE_CAPTURE_CONTEXT: camera="+View.transform.position+" forward="+View.transform.forward+" far="+View.farClipPlane+" fog="+RenderSettings.fogColor+" night="+Shader.GetGlobalFloat("_Night"));
                    frame.Create();View.aspect=16f/9;Hud.Display.renderMode=RenderMode.ScreenSpaceCamera;Hud.Display.worldCamera=View;Hud.Display.planeDistance=1;Canvas.ForceUpdateCanvases();
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(View,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=frame});
                    RenderTexture.active=frame;capture=new Texture2D(1600,900,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1600,900),0,0);capture.Apply();
                    byte[] png=capture.EncodeToPNG();System.IO.File.WriteAllBytes(path,png);Debug.Log("VOYAGE_CAPTURE_PASSED: "+path+" ("+png.Length+" bytes)");
                }
                finally
                {RenderTexture.active=previous;View.aspect=aspect;Hud.Display.renderMode=RenderMode.ScreenSpaceOverlay;Hud.Display.worldCamera=null;if(capture!=null)Destroy(capture);frame.Release();Destroy(frame);}
                if(Array.IndexOf(args,"--preview-quit")>=0)Application.Quit(0);
            }
        }
        IEnumerator RuntimeValidation()
        {
            yield return null;
            try
            {
                EnvironmentValidation.Validate(this);
                Require(Schools.Count==40,"Forty habitat-distributed schools");Require(Wildlife.ActiveCount==0,"No permanent large animals at startup");
                for(int species=0;species<8;species++)Require(Schools.FindAll(s=>s.SpeciesIndex==species).Count==5,"Five groups per species");
                foreach(var school in Schools)Require(WorldLayout.IsWater(school.Home,school.Spread+2),"School habitats are navigable");
                Require(WorldLayout.IsWater(new Vector3(225,0,180),3),"Expanded offshore area");Require(Residents.Count==6,"Six island residents");
                Require(Boat.Visual.GetComponentInChildren<SkinnedMeshRenderer>()!=null,"Blender sail uses skinning");
                foreach(var school in Schools)Require(school.Root.GetComponentsInChildren<SkinnedMeshRenderer>().Length==school.ActiveCount,"Every fish uses an imported Blender skin");
                foreach(var animal in Animals)Require(animal.Root.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length>=9,"Large animal uses a real skeleton");
                Cycle.SetHour(12);float day=Cycle.Daylight;Cycle.SetHour(0);Require(day>.95f && Cycle.Daylight<.05f,"Day and night lighting");Cycle.SetHour(9.5f);
                Require(Wildlife.Spawn(MarineKind.Dolphin,Boat.Root.position,View),"Off-screen wildlife encounter");Require(Wildlife.ActiveCount>=1 && Wildlife.ActiveCount<=2,"Wildlife active population cap");
                foreach(var animal in Animals)animal.Root.gameObject.SetActive(false);
                var target=Schools[0];int before=target.ActiveCount;target.Spook(Boat.Root.position,WorldTime);
                Require(target.Mood==SchoolMood.Fleeing,"Click/high-speed scare transition");
                target.Tick(.01f,WorldTime+3,Boat.Root.position,0);Require(target.Mood==SchoolMood.Returning,"Fish start returning");
                for(int i=0;i<1400;i++)target.Tick(.02f,WorldTime+3+i*.02f,Boat.Root.position,0);
                Require(target.Mood==SchoolMood.Calm,"Fish return to calm");
                Require(target.CatchOne(WorldTime) && target.ActiveCount==before-1,"Catch removes exactly one fish");Require(target.Mood==SchoolMood.Calm,"Successful catch keeps school calm");
                target.Spook(Boat.Root.position,WorldTime);Require(target.Mood==SchoolMood.Fleeing && TotalCatch==0,"Disturbance does not award a catch");
                Vector3 land=new Vector3(26,0,15),water=new Vector3(0,0,15);Require(WorldLayout.Move(water,land,1.25f)!=land,"Island collision");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return new WaitForSeconds(2);
            try
            {
                Require(Mathf.Abs(Boat.Root.position.y)<.8f && Mathf.Abs(Boat.Body.linearVelocity.y)<1,"Stable four-point buoyancy");
                ValidateSailPose("calm wind");
                Require(!View.orthographic && Mathf.Abs(View.fieldOfView-58)<.01f,"Perspective third-person camera");
                Require(Vector3.Dot(View.transform.position-Boat.Root.position,Boat.Root.forward)<-8,"Camera follows behind the stern");
                Require(Mathf.Abs(Vector3.Dot(View.transform.right,Vector3.up))<.001f,"Wave motion keeps the camera horizon level");
                Vector3 mast=View.WorldToViewportPoint(Boat.Root.position+Vector3.up*4.3f);
                Vector3 hull=View.WorldToViewportPoint(Boat.Root.position+Vector3.up*.2f);
                Require(mast.z>0 && mast.y>.05f && mast.y<.95f && hull.y>.05f && hull.y<.95f,"Close camera frames hull and complete mast");
                Require(Hud.Minimap.SchoolMarkerCount==Schools.Count,"Minimap includes every fish school");
                Require(VoyageMinimap.NormalizedPosition(new Vector3(-260,0,-220))==Vector2.zero && VoyageMinimap.NormalizedPosition(new Vector3(260,0,220))==Vector2.one,"Map corners match world navigation bounds");
                Require(VoyageMinimap.NormalizedPosition(Vector3.zero)==Vector2.one*.5f,"Map origin sits at chart center");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            Vector3 obstructionFocus=Boat.Root.position+Vector3.up*1.4f+(Quaternion.Euler(0,Boat.Root.eulerAngles.y,0)*Vector3.forward)*2.4f;
            Vector3 clearCameraPosition=View.transform.position;var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.name="Temporary camera obstruction check";obstruction.transform.position=Vector3.Lerp(obstructionFocus,clearCameraPosition,.2f);obstruction.transform.localScale=Vector3.one*2;
            Physics.SyncTransforms();CameraRig.Snap();
            try
            {
                var collider=obstruction.GetComponent<Collider>();
                Require(Vector3.Distance(collider.ClosestPoint(View.transform.position),View.transform.position)>.3f,"Near obstruction keeps the camera outside the wall");
                Require(Vector3.Distance(View.transform.position,obstructionFocus)<Vector3.Distance(clearCameraPosition,obstructionFocus)*.5f,"Camera retracts its boom for a nearby obstacle");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            Destroy(obstruction);yield return null;CameraRig.Snap();
            float nearDistance=Vector3.ProjectOnPlane(View.transform.position-Boat.Root.position,Vector3.up).magnitude;
            AdjustZoom(100);yield return new WaitForSeconds(1);
            try{Require(CameraRig.Distance==BoatFollowCamera.MaxDistance && Vector3.ProjectOnPlane(View.transform.position-Boat.Root.position,Vector3.up).magnitude>nearDistance+8,"Zoom extends the actual camera boom");}
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            AdjustZoom(-100);yield return new WaitForSeconds(1);
            try{Require(CameraRig.Distance==BoatFollowCamera.MinDistance && Vector3.ProjectOnPlane(View.transform.position-Boat.Root.position,Vector3.up).magnitude<10,"Close zoom is bounded");}
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            CameraRig.ResetZoom();SetPaused(true);Vector3 pausedPosition=Boat.Body.position;float pausedTime=WorldTime;
            yield return new WaitForSecondsRealtime(.3f);
            try{Require((Boat.Body.position-pausedPosition).sqrMagnitude<.00001f && Mathf.Abs(WorldTime-pausedTime)<.001f,"Pause freezes physics and world time");}
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            SetPaused(false);
            Boat.SetInput(Vector2.up,false);yield return new WaitForSeconds(6);
            try
            {
                Require(Boat.Speed>4 && Boat.Speed<6.1f,"Thrust and water drag reach bounded high speed");Require(Boat.Wake.ActiveCount>0,"Speed produces a wake");Require(Mathf.Abs(Boat.Root.position.y)<.8f,"Boat stays afloat while moving");
                Require(Vector3.Dot(Boat.Velocity,Vector3.ProjectOnPlane(Boat.Root.forward,Vector3.up).normalized)>4 && Mathf.Abs(Mathf.DeltaAngle(Boat.Root.eulerAngles.y,BoatController.StartHeading))<3,"Forward throttle follows hull heading without steering toward world north");
                Vector2 expected=Vector2.Scale(VoyageMinimap.NormalizedPosition(Boat.Root.position),Hud.Minimap.MapSize);
                Require((Hud.Minimap.BoatMapPosition-expected).magnitude<1,"Minimap follows the moving boat");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            Boat.SetInput(Vector2.up,true);yield return new WaitForSeconds(3);
            try{Require(Boat.Speed<=2.01f,"Slow assist decelerates below fishing threshold");}
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            ResetBoat();Boat.Body.position=new Vector3(-16,0,-14);Boat.Body.rotation=Quaternion.Euler(0,125,0);Boat.SetInput(Vector2.down,false);
            Vector3 reverseStart=Boat.Body.position;Vector3 reverseHeading=Quaternion.Euler(0,125,0)*Vector3.forward;
            yield return new WaitForSeconds(3);
            try
            {
                Require(Vector3.Dot(Boat.Velocity,reverseHeading)<-1.8f && Vector3.Dot(Boat.Body.position-reverseStart,reverseHeading)<-3,"Reverse throttle moves stern-first at an arbitrary heading");
                Require(Mathf.Abs(Mathf.DeltaAngle(Boat.Root.eulerAngles.y,125))<3,"Reverse does not turn the hull around");
                Require(Vector3.Dot(Boat.Wake.LastTravelDirection,reverseHeading)<-.95f,"Reverse wake follows actual stern-first travel");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            ResetBoat();Boat.Body.position=new Vector3(-16,0,-14);Boat.Body.rotation=Quaternion.identity;Boat.SetInput(Vector2.up,false);
            yield return new WaitForSeconds(2);Boat.SetInput(new Vector2(1,1),false);float rightHeading=Boat.Root.eulerAngles.y;
            yield return new WaitForSeconds(2);
            try{Require(Mathf.DeltaAngle(rightHeading,Boat.Root.eulerAngles.y)>15,"D turns the moving bow to starboard");}
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            Boat.SetInput(new Vector2(-1,1),false);float leftHeading=Boat.Root.eulerAngles.y;
            yield return new WaitForSeconds(2.5f);
            try
            {
                Require(Mathf.DeltaAngle(leftHeading,Boat.Root.eulerAngles.y)<-10,"A turns the moving bow to port");
                ValidateSailPose("turning hull");
                Require(Mathf.Abs(Mathf.DeltaAngle(Hud.Minimap.BoatHeading,-Boat.Root.eulerAngles.y))<1,"Minimap arrow follows the actual bow heading");
                Require(Vector3.Dot(Vector3.ProjectOnPlane(View.transform.forward,Vector3.up).normalized,Vector3.ProjectOnPlane(Boat.Root.forward,Vector3.up).normalized)>.94f,"Third-person camera follows a turning boat");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            ResetBoat();Boat.SetInput(Vector2.zero,false);yield return new WaitForSeconds(.25f);
            try{Require(Vector2.Distance(new Vector2(Boat.Root.position.x,Boat.Root.position.z),new Vector2(0,8))<.1f,"Reset clears old physics interpolation and returns to the departure point");}
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            try
            {
                Vector3 sole=Boat.Root.TransformPoint(new Vector3(0,.26f,0));
                Require(sole.y-OceanSimulation.Height(sole,WorldTime)>.18f,"Raised cockpit sole stays above the calm waterline");
                Require(BlenderAssets.Find(Boat.Visual,"Raised dry cockpit sole")!=null,"Imported Blender hull includes its fitted dry sole");
                Require(Shader.GetGlobalFloat("_BoatWaterMask")==1 && Shader.GetGlobalMatrix("_BoatWorldToLocal").MultiplyPoint3x4(Boat.Root.position).magnitude<.01f,"Water exclusion follows the boat transform");
                float beforeCloud=WeatherSystem.CloudCover;Weather.SetWeather(WeatherKind.Rain);
                Weather.Tick(.1f,WorldTime,Boat.Root.position,false);
                Require(WeatherSystem.CloudCover>beforeCloud && WeatherSystem.CloudCover<beforeCloud+.03f,"Weather transitions gradually");
                Weather.SetWeather(WeatherKind.Rain,true);
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return new WaitForSeconds(2);
            try
            {
                Require(Weather.RainParticleCount>100 && Weather.RainAmount>.5f,"Rain emits live bounded particles");
                Require(WeatherSystem.Wind.magnitude>6 && OceanSimulation.WaveScale>1.5f,"Rain changes actual wind and wave physics");
                Weather.SetWeather(WeatherKind.Fog,true);Cycle.Tick(0);
                Require(RenderSettings.fogEndDistance<70 && Weather.Visibility<70,"Sea fog reduces actual visibility");
                Weather.SetWeather(WeatherKind.Thunderstorm,true);
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return new WaitForSeconds(10);
            try
            {
                Require(Weather.LightningCount>0,"Thunderstorm produces timed lightning and distant thunder");
                Require(Mathf.Abs(Boat.Root.position.y)<2 && Vector3.Dot(Boat.Root.up,Vector3.up)>.65f,"Rigid-body buoyancy remains stable in storm waves");
                ValidateSailPose("storm wind");
                Weather.SetWeather(WeatherKind.Clear,true);ResetBoat();
                Require(Events.Start(SeaEventKind.Migration,true),"Safe migration route exists near departure");
                Require(Events.MigrationSchools.Count==8 && Events.MigrationVisible,"Migration activates eight pooled schools");
                foreach(var school in Events.MigrationSchools)Require(school.ActiveCount==18 && WorldLayout.IsWater(school.Center,3,2),"Migration fish start in deep enough water");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            Vector3 migrationStart=Events.MigrationSchools[0].Center;
            yield return new WaitForSeconds(2);
            try
            {
                float travelled=Vector3.Distance(migrationStart,Events.MigrationSchools[0].Center);
                Debug.Log("VOYAGE_MIGRATION_DISTANCE: "+travelled.ToString("F3")+"m");
                Require(travelled>2 && travelled<4,"Migration swims continuously with bounded speed");
                foreach(var school in Events.MigrationSchools)Require(school.MinimumSeabedClearance>=.449f,"Migrating fish remain above the coastal seabed");
                var migrant=Events.MigrationSchools[0];Require(migrant.CatchOne(WorldTime),"Migrating fish can be caught");migrant.Spook(Boat.Root.position,WorldTime);Require(migrant.Mood==SchoolMood.Fleeing,"Migration responds to disturbance");
                Events.Finish();Require(!Events.MigrationVisible && Events.Stage==SeaEventStage.Idle,"Migration pool is disabled after the event");
                Require(Events.Start(SeaEventKind.RogueSwell),"Rogue swell warning starts");
                Require(Events.Stage==SeaEventStage.Warning && OceanSimulation.SwellStrength==0,"Hazard provides a warning before changing physics");
                Events.Start(SeaEventKind.RogueSwell,true);Events.Tick(0,WorldTime);
                Vector3 p=OceanSimulation.SwellCenter;float raised=OceanSimulation.Height(p,WorldTime);float strength=OceanSimulation.SwellStrength;
                OceanSimulation.SwellStrength=0;float baseline=OceanSimulation.Height(p,WorldTime);OceanSimulation.SwellStrength=strength;
                Require(raised-baseline>1 && raised-baseline<1.4f,"Travelling swell is sampled by the real buoyancy surface");
                OceanSimulation.Tick(WorldTime,Boat.Root.position);
                Require(Mathf.Abs(Shader.GetGlobalFloat("_SwellStrength")-OceanSimulation.SwellStrength)<.001f && Mathf.Abs(Shader.GetGlobalFloat("_WaveScale")-OceanSimulation.WaveScale)<.001f,"GPU surface shares the physics wave parameters");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return new WaitForSeconds(5);
            try
            {
                Require(Mathf.Abs(Boat.Root.position.y)<2.5f && Vector3.Dot(Boat.Root.up,Vector3.up)>.65f,"Boat stays afloat while the event swell passes");
                Events.Finish();Cycle.SetHour(21);Events.Start(SeaEventKind.LuminousSea,true);Events.Tick(0,WorldTime);
                Require(Shader.GetGlobalFloat("_Bioluminescence")>.9f,"Luminous sea glows at night");
                Cycle.SetHour(12);Events.Tick(0,WorldTime);Require(Shader.GetGlobalFloat("_Bioluminescence")==0,"Daylight suppresses natural bioluminescence");
                Events.Finish();Require(OceanSimulation.SwellStrength==0 && Shader.GetGlobalFloat("_Bioluminescence")==0,"Event cleanup restores normal sea conditions");
                Weather.SetWeather(WeatherKind.Clear,true);Cycle.SetHour(9.5f);ResetBoat();
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            // Exercise the actual pointer gesture driver and resulting camera, not a separate orbit formula.
            float hullHeading=Boat.Root.eulerAngles.y;
            CameraRig.UpdatePointer(new Vector2(600,400),true,true,true,false);
            CameraRig.UpdatePointer(new Vector2(600+Screen.width*.24f,340),false,true,true,false);
            CameraRig.UpdatePointer(Vector2.zero,false,false,true,false);
            Boat.SetInput(Vector2.up,false);yield return new WaitForSeconds(2);
            try
            {
                Require(Mathf.Abs(CameraRig.OrbitYaw-92.16f)<.01f && !CameraRig.Dragging,"Right-button drag changes orbit and release preserves it");
                Vector3 cameraForward=Vector3.ProjectOnPlane(View.transform.forward,Vector3.up).normalized;
                Require(Mathf.Abs(Vector3.Dot(cameraForward,Vector3.ProjectOnPlane(Boat.Root.forward,Vector3.up).normalized))<.2f,"Orbit visibly moves the camera to the boat's side");
                Require(Mathf.Abs(Mathf.DeltaAngle(hullHeading,Boat.Root.eulerAngles.y))<3 && Vector3.Dot(Boat.Velocity,Vector3.ProjectOnPlane(Boat.Root.forward,Vector3.up).normalized)>2,"Orbit does not change bow-forward propulsion");
                Require(Mathf.Abs(Vector3.Dot(View.transform.right,Vector3.up))<.001f,"Orbit horizon remains level");
                ValidateSailPose("orbit camera");
                float yaw=CameraRig.OrbitYaw;
                CameraRig.UpdatePointer(new Vector2(1,1),true,true,true,true);CameraRig.UpdatePointer(new Vector2(900,900),false,true,true,false);
                Require(CameraRig.OrbitYaw==yaw,"Starting on a UI panel cannot rotate the camera");
                CameraRig.UpdatePointer(Vector2.zero,true,true,true,false);CameraRig.UpdatePointer(new Vector2(0,10000),false,true,true,false);
                Require(CameraRig.Elevation==BoatFollowCamera.MinElevation,"Lower orbit angle is bounded");
                CameraRig.UpdatePointer(new Vector2(0,-10000),false,true,true,false);Require(CameraRig.Elevation==BoatFollowCamera.MaxElevation,"Upper orbit angle is bounded");
                CameraRig.UpdatePointer(Vector2.zero,false,true,false,false);Require(!CameraRig.Dragging,"Focus loss or pause cancels a camera drag");
                CameraRig.ResetView();Require(CameraRig.OrbitYaw==0 && CameraRig.Distance==BoatFollowCamera.DefaultDistance && CameraRig.Elevation==BoatFollowCamera.DefaultElevation,"C restores default orbit, elevation and zoom");
                ResetBoat();Boat.SetInput(Vector2.zero,false);
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return FishingValidation();if(!fishingValidated)yield break;
            runtimeChecked=true;
            Debug.Log("VOYAGE_SAIL_TESTS_PASSED: baked canvas remains upright and attached to mast in calm wind, turning, storm wind and orbit camera");
            Debug.Log("VOYAGE_ORBIT_TESTS_PASSED: pointer-gesture/release/UI-block/focus-loss/elevation-bounds/default-reset, real-camera-side-view/level-horizon/bow-forward-independent verified");
            Debug.Log("VOYAGE_RUNTIME_TESTS_PASSED: dry-cockpit/water-mask, smooth-weather/live-rain/fog/wind/storm-buoyancy/lightning, safe-migration/swim/catch/scare/pool-cleanup, swell-warning/CPU-GPU-parameters/actual-buoyancy, night-glow/day-suppression/cleanup, perspective/stern-follow/horizon/framing/zoom/obstruction, hull-local-forward/reverse/left/right/reset, minimap-position/heading/bounds/schools, species=8 schools=40 rare-wildlife<=2 residents=6, buoyancy/drag/wake/reverse-wake/pause/day-night/scare/recovery/catch/failure/collision verified");
            yield return new WaitForSeconds(2);Application.Quit(0);
        }
        IEnumerator FishingValidation()
        {
            ResetBoat();Weather.SetWeather(WeatherKind.Clear,true);Cycle.SetHour(9.5f);
            float deadline=Time.time+16;
            while(!Fishing.CanStart && Time.time<deadline)yield return null;
            FishSchool school=Nearby;int before=school==null?0:school.ActiveCount,awards=TotalCatch;
            try
            {
                Require(Fishing.CanStart,"A quiet school can be selected for angling");
                foreach(var group in Schools)
                {Require(group.MinimumSubmergence>1.0f,"Ordinary fish remain below the near-surface band");Require(group.MinimumSeabedClearance>=.449f,"Fish remain above the actual coastal seabed");}
                var renderer=Schools[1].Root.GetComponentInChildren<Renderer>();var properties=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);
                Require(properties.GetFloat("_Aquatic")==1 && properties.GetFloat("_WaterDepth")>1,"Fish use depth-dependent water absorption");
                TryCast();Require(Fishing.Stage==FishingStage.Casting,"E begins a rod cast");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return new WaitForSeconds(.7f);
            try
            {
                Require(Fishing.Visual.LineVisible && Fishing.Visual.HandError<.06f,"Line is visible and the angler's hand stays on the grip during casting");
                Require(Fishing.Bobber.y>OceanSimulation.Height(Fishing.Bobber,WorldTime),"Cast float travels through the air before splashdown");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            SetPaused(true);float heldTimer=Fishing.StageTime;Vector3 heldFloat=Fishing.Bobber;
            yield return new WaitForSecondsRealtime(.3f);
            try{Require(Fishing.StageTime==heldTimer && Fishing.Bobber==heldFloat,"Pause freezes fishing timing and float motion");}
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            SetPaused(false);deadline=Time.time+14;
            while(Fishing.Stage!=FishingStage.Bite && Time.time<deadline)yield return null;
            try
            {
                Require(Fishing.Stage==FishingStage.Bite,"Waiting and nibbling progress to a finite bite window");
                Require(TotalCatch==awards && school.ActiveCount==before,"Waiting cannot automatically award fish");
                TryCast();Require(Fishing.Stage==FishingStage.Hooking && school.HasHookedFish,"Timed E sets the hook on a real school fish");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            fishingTestReel=true;deadline=Time.time+65;float largestHandError=0;
            while(Fishing.Active && Time.time<deadline)
            {
                if(Fishing.Fight!=null){if(Fishing.Fight.Tension>.63f)fishingTestReel=false;else if(Fishing.Fight.Tension<.33f)fishingTestReel=true;}
                largestHandError=Mathf.Max(largestHandError,Fishing.Visual.HandError);yield return null;
            }
            fishingTestReel=false;
            try
            {
                Require(Fishing.Stage==FishingStage.Cooldown && TotalCatch==awards+1,"Controlled reel/release lands exactly one fish");
                Require(school.ActiveCount==before-1 && !school.HasHookedFish,"Landing consumes the reserved fish once and clears the hook");
                Require(BestWeights[school.SpeciesIndex]>0,"Landing records species weight");
                Require(largestHandError<.06f,"Hand-grip constraint stays continuous through fighting and landing");
                Debug.Log("VOYAGE_FISHING_DETAIL: duration="+Fishing.Fight.Elapsed.ToString("F2")+"s max hand error="+largestHandError.ToString("F4"));
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return new WaitForSeconds(1.3f);TryCast();yield return new WaitForSeconds(1.4f);
            try
            {
                Require(Fishing.Stage==FishingStage.Waiting,"A second cast returns to waiting");
                TryCast();Require(Fishing.Stage==FishingStage.Retrieving && TotalCatch==awards+1,"Early hook fails without awarding fish");
                Require(!school.HasHookedFish,"Failed attempt clears reserved fish");
                Fishing.Reset();Require(Fishing.Stage==FishingStage.Idle,"Reset cleans up the fishing state");
            }
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return null;
            try{Require(!Fishing.Visual.LineVisible,"Cleanup hides fishing line and float");}
            catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            fishingValidated=true;
            Debug.Log("VOYAGE_FISHING_TESTS_PASSED: cast-ballistics/hand-IK/visible-line/pause/wait-nibble-bite/timed-hook/real-fish/reel-release/landing/exactly-one-award/weight/early-hook/cleanup/submergence/absorption verified");
        }
        void ValidateSailPose(string condition)
        {
            var skin=Boat.Visual.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
            try
            {
                skin.BakeMesh(mesh);Vector3 tip=Vector3.zero;float highest=float.MinValue,lowest=float.MaxValue;
                foreach(var vertex in mesh.vertices)
                {
                    Vector3 point=Boat.Root.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                    lowest=Mathf.Min(lowest,point.y);if(point.y>highest){highest=point.y;tip=point;}
                }
                Require(highest>4.10f && highest<4.22f && lowest>1.0f,"Canvas stays upright during "+condition);
                Require(new Vector2(tip.x,tip.z-.20f).magnitude<.08f,"Canvas head stays attached to mast during "+condition);
                Debug.Log("VOYAGE_SAIL_POSE: "+condition+" local tip="+tip+" height="+highest.ToString("F3"));
            }
            finally {Destroy(mesh);}
        }
        static void Require(bool value,string label){if(!value)throw new InvalidOperationException("Runtime check failed: "+label);}
    }
}
