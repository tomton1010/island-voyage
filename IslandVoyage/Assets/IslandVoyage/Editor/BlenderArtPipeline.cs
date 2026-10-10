using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class BlenderArtPipeline : AssetPostprocessor
{
    const string Root="Assets/Resources/Blender/Models/";
    bool Ours=>assetPath.StartsWith(Root,StringComparison.OrdinalIgnoreCase);
    void OnPreprocessModel()
    {
        if(!Ours)return;var m=(ModelImporter)assetImporter;
        string name=Path.GetFileNameWithoutExtension(assetPath);
        bool animated=name.StartsWith("Resident")||name.StartsWith("Palm")||name=="Sailboat"||new[]{"Whale","Shark","Dolphin","Clownfish","BlueTang","YellowTang","Butterflyfish","Damselfish","Sardine","Mackerel","FlyingFish"}.Contains(name);
        m.globalScale=1;m.useFileScale=true;m.bakeAxisConversion=true;m.preserveHierarchy=true;
        m.importCameras=false;m.importLights=false;m.importBlendShapes=false;
        m.importNormals=ModelImporterNormals.Import;m.importTangents=ModelImporterTangents.CalculateMikk;
        m.meshCompression=ModelImporterMeshCompression.Off;m.isReadable=true;
        m.animationType=animated?ModelImporterAnimationType.Legacy:ModelImporterAnimationType.None;m.importAnimation=animated;
        m.animationCompression=ModelImporterAnimationCompression.Off;m.optimizeGameObjects=false;
        m.skinWeights=ModelImporterSkinWeights.Custom;m.maxBonesPerVertex=4;m.minBoneWeight=.001f;
        m.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
    }
    void OnPreprocessAnimation()
    {
        if(!Ours)return;var importer=(ModelImporter)assetImporter;
        var clips=importer.defaultClipAnimations;
        foreach(var clip in clips)
        {
            clip.name=clip.name.Substring(clip.name.LastIndexOf('|')+1);clip.loopTime=true;clip.loop=true;
            clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;
        }
        importer.clipAnimations=clips;
    }
    void OnPostprocessMaterial(Material material)
    {
        if(!Ours)return;
        material.shader=Shader.Find("IslandVoyage/BlenderSurface");material.SetColor("_BaseColor",Color.white);material.enableInstancing=true;
        material.SetFloat("_Smoothness",material.name.Contains("Marine")?.47f:material.name.Contains("Wood")?.26f:material.name.Contains("Resident")?.16f:.22f);
        material.SetFloat("_Detail",material.name.Contains("Wood")?1:0);
    }
    public static void ValidatePilot()=>Validate(new[]{"Dolphin","Resident0","Clownfish"});
    public static void ValidateAll()
    {
        AssetDatabase.Refresh();string[] names=Directory.GetFiles(Root,"*.fbx").Select(Path.GetFileNameWithoutExtension).ToArray();
        if(names.Length!=34)throw new Exception("Expected 34 Blender models, found "+names.Length);
        Validate(names);
    }
    static void Validate(string[] names)
    {
        var lines=new System.Collections.Generic.List<string>();
        foreach(string name in names)
        {
            string path=Root+name+".fbx";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null)throw new Exception("Missing Blender prefab: "+name);
            var instance=UnityEngine.Object.Instantiate(prefab);instance.name=name;
            try
            {
                var forward=instance.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="ModelForward");
                if(forward==null||forward.position.z<.9f||Mathf.Abs(forward.position.y)>.01f)throw new Exception("Invalid FBX coordinate system: "+name+" "+(forward==null?"no marker":forward.position.ToString()));
                var renderers=instance.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("No renderers: "+name);
                foreach(var renderer in renderers)foreach(var mat in renderer.sharedMaterials)
                    if(mat==null||mat.shader.name!="IslandVoyage/BlenderSurface")throw new Exception("Material mapping failed: "+name);
                var meshes=instance.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh).Concat(instance.GetComponentsInChildren<SkinnedMeshRenderer>().Select(m=>m.sharedMesh)).ToArray();
                foreach(var mesh in meshes)if(mesh.vertexCount<3||mesh.colors.Length!=mesh.vertexCount)throw new Exception("Vertex paint lost: "+name);
                var animation=instance.GetComponent<Animation>();var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                if(skins.Length>0)
                {
                    if(animation==null)throw new Exception("No imported animation component: "+name);
                    string clipName=name.StartsWith("Resident")?"Walk":name.StartsWith("Palm")||name=="Sailboat"?"Wind":"Swim";
                    if(animation[clipName]==null)throw new Exception("Missing clip "+clipName+" in "+name+": "+string.Join(",",animation.Cast<AnimationState>().Select(s=>s.name)));
                    foreach(var skin in skins)
                    {
                        if(skin.bones.Length<2||skin.bones.Any(b=>b==null))throw new Exception("Invalid skin skeleton: "+name);
                        foreach(var w in skin.sharedMesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.02f)throw new Exception("Invalid skin weights: "+name);
                    }
                    animation.Play(clipName);animation[clipName].time=animation[clipName].length*.15f;animation.Sample();
                    var before=new Mesh();skins[0].BakeMesh(before);var vertices=before.vertices;
                    animation[clipName].time=animation[clipName].length*.63f;animation.Sample();
                    var after=new Mesh();skins[0].BakeMesh(after);float deformation=0;
                    for(int i=0;i<vertices.Length;i++)deformation=Mathf.Max(deformation,(vertices[i]-after.vertices[i]).magnitude);
                    UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);
                    if(deformation<.003f)throw new Exception("Skeleton does not deform geometry: "+name+" "+deformation);
                    lines.Add(name+": skin+"+clipName+" deformation="+deformation.ToString("F4"));
                }
                else lines.Add(name+": static Blender mesh, vertex paint and scale verified");
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        Directory.CreateDirectory("Builds");File.WriteAllLines("Builds/BlenderAssetValidation.txt",lines);
        Debug.Log("BLENDER_UNITY_ASSETS_PASSED: "+names.Length+" Blender FBX models; materials, metres, skin weights, clips and real mesh deformation verified");
    }
}
