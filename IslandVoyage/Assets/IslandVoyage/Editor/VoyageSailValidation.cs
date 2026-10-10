using System;
using System.Linq;
using IslandVoyage;
using UnityEditor;
using UnityEngine;

public static class VoyageSailValidation
{
    public static void Inspect()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Blender/Models/Sailboat.fbx");
        var model=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var rig=BlenderAssets.Find(model.transform,"SailRig");
            var arm=BlenderAssets.Find(model.transform,"SailArmature");
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
            Debug.Log("SAIL_IMPORT: model="+model.transform.localEulerAngles+" rig="+rig.localEulerAngles+" arm="+arm.localEulerAngles);
            for(var parent=rig;parent!=null;parent=parent.parent)
                Debug.Log("SAIL_PARENT: "+parent.name+" rotation="+parent.localEulerAngles+" scale="+parent.localScale+" up="+parent.up+" forward="+parent.forward);
            Report(model.transform,skin,"Imported");
            rig.localRotation=Quaternion.Euler(0,18,0);
            Report(model.transform,skin,"Former faulty local-Y wind assignment");
            rig.localRotation=Quaternion.identity;
            var vertical=rig.parent.InverseTransformDirection(model.transform.up);
            var pivot=rig.parent.InverseTransformPoint(model.transform.TransformPoint(new Vector3(0,1.03f,.20f)));
            foreach(float angle in new[]{-18f,18f})
            {
                var trim=Quaternion.AngleAxis(angle,vertical);
                rig.localRotation=trim;rig.localPosition=pivot+trim*(-pivot);
                Report(model.transform,skin,"Correct mast-axis trim "+angle);
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(model);}
    }
    static void Report(Transform model,SkinnedMeshRenderer skin,string label)
    {
        var mesh=new Mesh();
        try
        {
            skin.BakeMesh(mesh);
            var points=mesh.vertices.Select(v=>model.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();
            var bounds=new Bounds(points[0],Vector3.zero);
            foreach(var point in points)bounds.Encapsulate(point);
            Debug.Log("SAIL_POSE: "+label+" min="+bounds.min+" max="+bounds.max+" size="+bounds.size);
        }
        finally {UnityEngine.Object.DestroyImmediate(mesh);}
    }
}
