using UnityEngine;

namespace IslandVoyage
{
    public sealed class BoatWake
    {
        sealed class Crest {public LineRenderer[] arms;public Transform foam;public MeshRenderer foamRenderer;public readonly MaterialPropertyBlock tint=new MaterialPropertyBlock();public Vector3 center,forward;public float age=9,speed,variation;}
        readonly Crest[] crests=new Crest[28];
        int next;float interval;
        public int ActiveCount {get;private set;}
        public Vector3 LastTravelDirection {get;private set;}
        public BoatWake()
        {
            Transform root=new GameObject("Gravity wave wake · pooled crests").transform;
            Mesh puff=new Mesh{name="Soft stern foam plane"};
            puff.vertices=new[]{new Vector3(-.5f,0,-.7f),new Vector3(.5f,0,-.7f),new Vector3(.5f,0,.7f),new Vector3(-.5f,0,.7f)};
            puff.triangles=new[]{0,2,1,0,3,2};puff.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
            puff.colors=new[]{Color.white,Color.white,Color.white,Color.white};puff.RecalculateNormals();puff.RecalculateBounds();
            var wakeMaterial=new Material(Shader.Find("IslandVoyage/Foam")){name="Broken wake foam"};
            var patchMaterial=new Material(wakeMaterial);patchMaterial.SetFloat("_Patch",1);
            for(int i=0;i<crests.Length;i++)
            {
                var crest=new Crest{arms=new LineRenderer[2],variation=Mathf.Sin(i*17.73f)};
                for(int side=0;side<2;side++)
                {
                    crest.arms[side]=VoyageEffects.Line(root,"Kelvin wake arm",13,.05f);crest.arms[side].gameObject.SetActive(false);
                    crest.arms[side].sharedMaterial=wakeMaterial;crest.arms[side].alignment=LineAlignment.TransformZ;crest.arms[side].transform.rotation=Quaternion.Euler(90,0,0);
                    crest.arms[side].widthCurve=new AnimationCurve(new Keyframe(0,.4f),new Keyframe(.2f,1),new Keyframe(.45f,.6f),new Keyframe(.8f,.3f),new Keyframe(1,0));
                }
                crest.foam=Art.Object("Stern foam patch",puff,root,Vector3.zero);crest.foamRenderer=crest.foam.GetComponent<MeshRenderer>();crest.foamRenderer.sharedMaterial=patchMaterial;crest.foamRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;crest.foam.gameObject.SetActive(false);
                crests[i]=crest;
            }
        }
        public void Tick(float dt,float time,Transform boat,Vector3 velocity)
        {
            float speed=velocity.magnitude;
            interval-=dt;
            if(speed>.55f && interval<=0)
            {
                var crest=crests[next++%crests.Length];crest.forward=velocity/speed;LastTravelDirection=crest.forward;
                crest.center=boat.position-crest.forward*1.8f;crest.center.y=0;crest.speed=speed;crest.age=0;
                foreach(var arm in crest.arms)arm.gameObject.SetActive(true);interval=Mathf.Lerp(.48f,.22f,speed/6);
                crest.foam.gameObject.SetActive(true);
            }
            ActiveCount=0;
            foreach(var crest in crests)
            {
                if(crest.age>5.2f)continue;crest.age+=dt;ActiveCount++;
                float distance=crest.age*(.9f+crest.speed*.35f),spread=distance*.354f;
                Vector3 right=Vector3.Cross(Vector3.up,crest.forward);
                for(int side=0;side<2;side++)
                {
                    var arm=crest.arms[side];float sign=side==0?-1:1;
                    for(int i=0;i<13;i++)
                    {
                        float t=i/12f;Vector3 p=crest.center-crest.forward*(distance*t+.4f)+right*sign*(.46f+spread*t+Mathf.Sin(t*24+crest.age*1.1f)*.055f);
                        p.y=OceanSimulation.Height(p,time)+.038f;arm.SetPosition(i,p);
                    }
                    float alpha=Mathf.Clamp01(crest.speed/5)*Mathf.Pow(1-Mathf.Clamp01(crest.age/5.2f),1.5f)*.42f;
                    arm.startColor=new Color(.90f,.97f,.92f,alpha);arm.endColor=new Color(.90f,.97f,.92f,alpha*.05f);
                    arm.startWidth=.055f+crest.speed*.016f;arm.endWidth=.03f;
                    if(crest.age>=5.2f)arm.gameObject.SetActive(false);
                }
                Vector3 foamPosition=crest.center-crest.forward*crest.age*.22f+right*crest.variation*(.18f+crest.age*.08f);foamPosition.y=OceanSimulation.Height(foamPosition,time)+.04f;crest.foam.position=foamPosition;
                crest.foam.localScale=Vector3.one*(.9f+crest.age*.28f+crest.variation*.12f);crest.foam.rotation=Quaternion.LookRotation(crest.forward)*Quaternion.Euler(0,crest.variation*28,0);
                float foamAlpha=Mathf.Clamp01(crest.speed/5)*Mathf.Pow(1-Mathf.Clamp01(crest.age/4.2f),1.6f)*.55f;
                crest.tint.SetColor("_BaseColor",new Color(.90f,.97f,.92f,foamAlpha));crest.foamRenderer.SetPropertyBlock(crest.tint);
                if(crest.age>=4.2f)crest.foam.gameObject.SetActive(false);
            }
        }
        public void Clear(){ActiveCount=0;interval=0;LastTravelDirection=Vector3.zero;foreach(var crest in crests){crest.age=9;foreach(var arm in crest.arms)arm.gameObject.SetActive(false);crest.foam.gameObject.SetActive(false);}}
    }
}
