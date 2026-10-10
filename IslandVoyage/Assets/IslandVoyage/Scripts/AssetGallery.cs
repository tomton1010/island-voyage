using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IslandVoyage
{
    // Developer capture mode: uses the same imported assets, skins and shaders as the game.
    public sealed class AssetGallery : MonoBehaviour
    {
        IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--asset-gallery");string folder=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(folder);
            var camera=new GameObject("Asset inspection camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.backgroundColor=new Color(.12f,.17f,.19f);camera.clearFlags=CameraClearFlags.SolidColor;camera.aspect=1.5f;
            VoyageLook.Configure(camera);Shader.SetGlobalColor("_VoyageAmbient",new Color(.24f,.30f,.35f));
            var light=new GameObject("Studio key",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.94f,.82f);light.intensity=2.1f;light.transform.rotation=Quaternion.Euler(38,-32,0);light.shadows=LightShadows.Soft;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.position=new Vector3(0,-.65f,0);floor.transform.localScale=Vector3.one*3;
            var floorMaterial=new Material(Shader.Find("IslandVoyage/BlenderSurface"));floorMaterial.SetColor("_BaseColor",new Color(.16f,.20f,.23f));floorMaterial.SetFloat("_Smoothness",.12f);floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            var frame=new RenderTexture(1200,800,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);frame.Create();var texture=new Texture2D(1200,800,TextureFormat.RGB24,false);
            foreach(string name in new[]{"Sailboat","Dolphin","Shark","Whale","Resident0","Palm0","Clownfish"})
            {
                var model=BlenderAssets.Create(name,null,Vector3.zero);var animation=model.GetComponent<Animation>();
                string clip=name.StartsWith("Resident")?"Walk":name.StartsWith("Palm")||name=="Sailboat"?"Wind":"Swim";
                new RigPlayback(model,clip,.13f,true);
                float size=name=="Sailboat"?3.4f:name=="Whale"?3.5f:name=="Palm0"?4.4f:name=="Resident0"?1.05f:name=="Clownfish"?.55f:2;
                Vector3 target=Vector3.up*(name=="Sailboat"?1.8f:name=="Palm0"?2.5f:name=="Resident0"?.92f:0);
                camera.orthographicSize=size;camera.transform.position=target+new Vector3(8,5,10);camera.transform.LookAt(target);
                yield return null;Save(camera,frame,texture,Path.Combine(folder,name+".png"));
                if(name=="Dolphin"||name=="Resident0")
                {
                    string frames=Path.Combine(folder,name+"Frames");Directory.CreateDirectory(frames);
                    for(int f=0;f<60;f++)
                    {
                        animation[clip].speed=0;animation[clip].time=f/30f;animation.Sample();
                        Save(camera,frame,texture,Path.Combine(frames,f.ToString("D3")+".png"));
                        if(f%5==0)yield return null;
                    }
                }
                Destroy(model.gameObject);yield return null;
            }
            Destroy(floor);Destroy(texture);frame.Release();Destroy(frame);Debug.Log("BLENDER_GALLERY_CAPTURE_PASSED: "+folder);Application.Quit(0);
        }
        static void Save(Camera camera,RenderTexture frame,Texture2D texture,string path)
        {
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=frame});
            var previous=RenderTexture.active;RenderTexture.active=frame;texture.ReadPixels(new Rect(0,0,1200,800),0,0);texture.Apply();RenderTexture.active=previous;File.WriteAllBytes(path,texture.EncodeToPNG());
        }
    }
}
