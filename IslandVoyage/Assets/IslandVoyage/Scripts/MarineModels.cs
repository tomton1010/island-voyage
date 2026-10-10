using UnityEngine;

namespace IslandVoyage
{
    public static class MarineModels
    {
        static readonly Mesh[] bodies=new Mesh[3],tails=new Mesh[3];
        public static float Scale(MarineKind kind)=>kind==MarineKind.Whale?1.65f:kind==MarineKind.Shark?.87f:.79f;
        public static float TailZ(MarineKind kind)=>kind==MarineKind.Whale?-2.25f:kind==MarineKind.Shark?-2.1f:-1.7f;
        public static Mesh Body(MarineKind kind)
        {
            int index=(int)kind;if(bodies[index]!=null)return bodies[index];var g=new Geometry();
            Color top=Art.Hex(kind==MarineKind.Whale?"426675":kind==MarineKind.Shark?"607F87":"668E99"),belly=Art.Hex("C8D5CF");
            if(kind==MarineKind.Whale)
                g.BodyProfile(new[]{-2.3f,-1.9f,-1.3f,-.6f,.2f,.8f,1.3f,1.75f,2.0f,2.2f},new[]{.09f,.22f,.47f,.69f,.8f,.76f,.68f,.55f,.36f,.025f},new[]{.07f,.18f,.38f,.55f,.61f,.55f,.43f,.32f,.22f,.025f},top,belly,32);
            else if(kind==MarineKind.Shark)
                g.BodyProfile(new[]{-2.15f,-1.6f,-1.0f,-.3f,.5f,1.1f,1.55f,1.94f,2.17f},new[]{.06f,.18f,.32f,.46f,.5f,.43f,.31f,.13f,.025f},new[]{.075f,.17f,.3f,.4f,.4f,.33f,.25f,.11f,.025f},top,belly,28);
            else
            {
                g.BodyProfile(new[]{-1.72f,-1.35f,-.7f,0,.6f,1.02f,1.32f,1.55f,1.75f},new[]{.07f,.19f,.35f,.43f,.39f,.30f,.14f,.07f,.025f},new[]{.07f,.17f,.31f,.36f,.36f,.30f,.15f,.075f,.025f},top,belly,28);
                g.Sphere(new Vector3(0,.11f,1.15f),new Vector3(.26f,.25f,.32f),top,10,18);
            }
            float finHeight=kind==MarineKind.Shark?1.0f:kind==MarineKind.Dolphin?.78f:.67f;
            Fin(g,new Vector3(0,.28f,-.1f),new Vector3(0,finHeight,-.42f),new Vector3(0,.24f,-.95f),top);
            for(int side=-1;side<=1;side+=2)
            {
                Fin(g,new Vector3(side*.35f,-.12f,.6f),new Vector3(side*(kind==MarineKind.Whale?1.6f:1.05f),-.35f,-.28f),new Vector3(side*.26f,-.22f,-.6f),top);
                float eyeZ=kind==MarineKind.Whale?1.45f:kind==MarineKind.Shark?1.47f:1.17f,eyeX=kind==MarineKind.Whale?.61f:kind==MarineKind.Shark?.30f:.24f;
                g.Sphere(new Vector3(side*eyeX,.05f,eyeZ),new Vector3(.025f,.038f,.047f),Art.Hex("183440"),6,8);
                if(kind==MarineKind.Shark)for(int slit=0;slit<5;slit++)g.Cylinder(new Vector3(side*(.45f-slit*.012f),-.16f,.86f-slit*.095f),new Vector3(side*(.46f-slit*.014f),.18f,.86f-slit*.095f),.012f,.012f,Art.Hex("456068"),5);
                g.Cylinder(new Vector3(side*eyeX*.55f,-.1f,eyeZ+.16f),new Vector3(side*eyeX*.83f,-.13f,eyeZ+.04f),.012f,.012f,Art.Hex("496D76"),5);
            }
            if(kind==MarineKind.Whale)
            {
                g.Sphere(new Vector3(0,.54f,.86f),new Vector3(.09f,.018f,.08f),Art.Hex("284651"),6,10);
                for(int groove=-2;groove<=2;groove++)g.Cylinder(new Vector3(groove*.08f,-.43f,.8f),new Vector3(groove*.07f,-.26f,1.7f),.012f,.012f,Art.Hex("A0BABD"),5);
            }
            return bodies[index]=g.Build("Refined "+kind+" · tapered body and anatomical fins");
        }
        static void Fin(Geometry g,Vector3 root,Vector3 tip,Vector3 rear,Color color)
        {
            Vector3 offset=Vector3.Cross(tip-root,rear-root).normalized*.045f;
            g.Triangle(root+offset,tip,rear+offset,color);g.Triangle(rear-offset,tip,root-offset,Color.Lerp(color,Color.white,.08f));
            g.Quad(root-offset,root+offset,rear+offset,rear-offset,color);
        }
        public static Mesh Tail(MarineKind kind)
        {
            int index=(int)kind;if(tails[index]!=null)return tails[index];var g=new Geometry();Color color=Art.Hex("587D88");
            if(kind==MarineKind.Shark)
            {
                Fin(g,Vector3.zero,new Vector3(0,.94f,-1.0f),new Vector3(0,.23f,-.67f),color);
                Fin(g,Vector3.zero,new Vector3(0,-.55f,-.85f),new Vector3(0,-.09f,-.54f),color);
            }
            else for(int side=-1;side<=1;side+=2)Fin(g,Vector3.zero,new Vector3(side*(kind==MarineKind.Whale?1.1f:.79f),0,-.66f),new Vector3(side*.45f,.025f,-.8f),color);
            return tails[index]=g.Build("Articulated "+kind+" flukes");
        }
    }
}
