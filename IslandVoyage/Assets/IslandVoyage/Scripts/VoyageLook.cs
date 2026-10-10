using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IslandVoyage
{
    public static class VoyageLook
    {
        public static void Configure(Camera camera)
        {
            camera.allowHDR=true;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
            data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            if(RenderSettings.skybox==null || RenderSettings.skybox.shader.name!="IslandVoyage/GradientSky")
                RenderSettings.skybox=new Material(Resources.Load<Shader>("VoyageSky")){name="Tropical day and night sky"};
            var volume=new GameObject("Ocean look · filmic highlights").AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;
            volume.sharedProfile=Resources.Load<VolumeProfile>("Blender/LookProfile");
            if(volume.sharedProfile==null)throw new System.InvalidOperationException("Missing look profile; run Prepare Project.");
        }
    }
}
