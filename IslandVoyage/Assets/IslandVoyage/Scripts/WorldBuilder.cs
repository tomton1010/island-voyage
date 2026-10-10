using UnityEngine;
using UnityEngine.Rendering;

namespace IslandVoyage
{
    public static class WorldBuilder
    {
        public static Transform Create()
        {
            Transform root=new GameObject("Tropical archipelago · Blender scenery and simulated ocean").transform;
            var waterMesh=new Geometry();
            waterMesh.Quad(new Vector3(-600,0,-600),new Vector3(-600,0,600),new Vector3(600,0,600),new Vector3(600,0,-600),Color.white);
            Mesh plane=waterMesh.Build("Ocean plane");
            Surface(root,plane,"Ocean floor",-4.5f,false);
            Surface(root,OceanSimulation.SurfaceMesh(),"Transparent tropical water",0,true);
            var random=new System.Random(2107);
            for(int islandIndex=0;islandIndex<WorldLayout.Islands.Length;islandIndex++)
            {
                var island=WorldLayout.Islands[islandIndex];
                var sand=Art.Object("Sand island "+islandIndex,IslandMesh(island,false),root,Vector3.zero);
                sand.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                sand.gameObject.AddComponent<MeshCollider>().sharedMesh=sand.GetComponent<MeshFilter>().sharedMesh;
                var grass=Art.Object("Low grassy interior "+islandIndex,IslandMesh(island,true),root,Vector3.zero);grass.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                int palmCount=islandIndex==0 ? 34 : 10;
                for(int i=0;i<palmCount;i++)
                {
                    float angle=(float)random.NextDouble()*Mathf.PI*2,ratio=.28f+(float)random.NextDouble()*.48f;
                    Vector3 p=new Vector3(island.center.x+Mathf.Cos(angle)*island.size.x*ratio,WorldLayout.LandHeight(island,ratio),island.center.y+Mathf.Sin(angle)*island.size.y*ratio);
                    if(islandIndex==0 && Vector2.Distance(new Vector2(p.x,p.z),new Vector2(39,15))<8)continue;
                    Transform palm=BlenderAssets.Create("Palm"+(i%4),root,p);palm.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                    palm.localScale=Vector3.one*(.8f+(float)random.NextDouble()*.45f);
                    BlenderAssets.Wind(palm,(float)random.NextDouble());
                }
                for(int i=0;i<18;i++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2,r=.72f+(float)random.NextDouble()*.27f;
                    Vector3 p=new Vector3(island.center.x+Mathf.Cos(a)*island.size.x*r,WorldLayout.LandHeight(island,r),island.center.y+Mathf.Sin(a)*island.size.y*r);
                    var stone=BlenderAssets.Create("Rock"+(i%3),root,p);stone.localScale=new Vector3(.2f+(float)random.NextDouble()*.65f,.4f+(float)random.NextDouble()*.5f,.5f);stone.localRotation=Quaternion.Euler(0,i*73,0);
                }
                for(int i=0;i<15;i++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2,r=1.12f+(float)random.NextDouble()*.58f;
                    Vector3 p=new Vector3(island.center.x+Mathf.Cos(a)*island.size.x*r,-.65f-(r-1)*2.6f,island.center.y+Mathf.Sin(a)*island.size.y*r);
                    if(!WorldLayout.IsWater(p,2))continue;
                    Transform reef=BlenderAssets.Create("Reef"+(i%5),root,p);reef.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                    reef.localScale=Vector3.one*(.5f+(float)random.NextDouble()*.8f);
                }
            }
            Transform house=BlenderAssets.Create("Cottage",root,new Vector3(39,2.45f,15));
            house.localRotation=Quaternion.Euler(0,-18,0);house.localScale=Vector3.one*1.25f;
            var houseCollider=house.gameObject.AddComponent<BoxCollider>();houseCollider.center=new Vector3(0,1.5f,0);houseCollider.size=new Vector3(6,3,4.5f);
            BlenderAssets.Create("Jetty",root,new Vector3(43,0,-7));
            IslandDetails.Create(root);
            return root;
        }

        static void Surface(Transform parent,Mesh mesh,string name,float height,bool surface)
        {
            Transform t=Art.Object(name,mesh,parent,new Vector3(0,height,0));
            var material=new Material(Shader.Find("IslandVoyage/Water")){name=name};
            OceanSimulation.Configure(material);if(surface)OceanSimulation.Surface=t;
            material.SetFloat("_Surface",surface?1:0);material.SetFloat("_ZWrite",surface?0:1);material.renderQueue=surface?3000:1990;
            var renderer=t.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }

        static Mesh IslandMesh(WorldLayout.Island island,bool grass)
        {
            var g=new Geometry();const int sectors=96,rings=9;
            float outer=grass?.35f:1.07f;
            Vector3 Point(float r,float angle)
            {
                float ratio=r*island.Edge(angle);
                return new Vector3(island.center.x+Mathf.Cos(angle)*island.size.x*ratio,WorldLayout.LandHeight(island,ratio)+(grass?.035f:0),island.center.y+Mathf.Sin(angle)*island.size.y*ratio);
            }
            for(int ring=0;ring<rings;ring++)
            {
                float r0=outer*ring/rings,r1=outer*(ring+1)/rings;
                Color c=grass?Color.Lerp(Art.Hex("7D9565"),Art.Hex("A5AE79"),(float)ring/rings):Color.Lerp(Art.Hex("C9BA96"),Art.Hex("EADFC1"),(float)ring/rings);
                for(int s=0;s<sectors;s++)
                {float a=s*Mathf.PI*2/sectors,b=(s+1)*Mathf.PI*2/sectors;g.Quad(Point(r0,a),Point(r0,b),Point(r1,b),Point(r1,a),c);}
            }
            return g.Build(grass?"Island grass":"Island sand");
        }

        static Mesh Palm(int variant)
        {
            var g=new Geometry();float height=5.8f+variant*.4f;Vector3 last=Vector3.zero;
            for(int i=1;i<=8;i++)
            {
                float t=i/8f;Vector3 next=new Vector3(t*t*(.7f+variant*.2f),t*height,Mathf.Sin(t*1.2f)*.2f);
                g.Cylinder(last,next,.28f*(1-.4f*(t-.125f)),.28f*(1-.4f*t),i%2==0?Art.Hex("9C7950"):Art.Hex("B19562"));last=next;
            }
            for(int leaf=0;leaf<9;leaf++)g.Leaf(last,3.0f+(leaf%3)*.25f,.55f,leaf*40+variant*10,leaf%2==0?Art.Hex("5DA845"):Art.Hex("87BE4C"));
            for(int i=0;i<3;i++)g.Sphere(last+new Vector3(Mathf.Cos(i*2.1f)*.3f,-.35f,Mathf.Sin(i*2.1f)*.3f),Vector3.one*.28f,Art.Hex("7B6844"),5,8);
            return g.Build("Palm variant "+variant);
        }

        static Mesh Coral(int variant)
        {
            var g=new Geometry();Color[] palette={Art.Hex("D07FA7"),Art.Hex("7D78B6"),Art.Hex("93C392"),Art.Hex("CFA66A"),Art.Hex("89B4A6")};
            Color c=palette[variant];
            for(int i=0;i<5;i++)
            {
                float a=i*2.4f;Vector3 p=new Vector3(Mathf.Sin(a)*.75f,0,Mathf.Cos(a)*.75f);
                if(variant==2 || variant==4)
                    g.Sphere(p+Vector3.up*.22f,new Vector3(.5f,.21f,.42f),c,5,8);
                else
                {
                    Vector3 top=p+new Vector3(.1f,.8f+i*.11f,0);g.Cylinder(p,top,.13f,.1f,c,6);
                    g.Cylinder(top-Vector3.up*.4f,top+new Vector3(.35f,.15f,.2f),.075f,.05f,c,6);
                    g.Cylinder(top-Vector3.up*.3f,top+new Vector3(-.3f,.05f,-.15f),.075f,.05f,c,6);
                }
                g.Cylinder(p+Vector3.right*.28f,p+new Vector3(.4f,.55f,.1f),.035f,.008f,Art.Hex("429E83"),5);
            }
            return g.Build("Coral variant "+variant);
        }

        static Mesh House()
        {
            var g=new Geometry();Color wood=Art.Hex("A67D50"),wall=Art.Hex("F1EBD4"),roof=Art.Hex("BD8E58"),green=Art.Hex("468D76");
            g.Box(new Vector3(0,1.5f,0),new Vector3(6,3,4.5f),wall,Quaternion.identity);
            g.Quad(new Vector3(-3.5f,2.8f,-2.8f),new Vector3(-3.5f,4.5f,0),new Vector3(3.5f,4.5f,0),new Vector3(3.5f,2.8f,-2.8f),roof);
            g.Quad(new Vector3(3.5f,2.8f,2.8f),new Vector3(3.5f,4.5f,0),new Vector3(-3.5f,4.5f,0),new Vector3(-3.5f,2.8f,2.8f),roof);
            for(int side=-1;side<=1;side+=2)g.Triangle(new Vector3(side*3,3,-2.25f),new Vector3(side*3,4.45f,0),new Vector3(side*3,3,2.25f),wall);
            g.Box(new Vector3(0,1.1f,-2.3f),new Vector3(1.2f,2.2f,.15f),green,Quaternion.identity);
            for(int side=-1;side<=1;side+=2)
            {
                g.Box(new Vector3(side*1.9f,1.9f,-2.31f),new Vector3(1.05f,.95f,.12f),Art.Hex("264E4F"),Quaternion.identity);
                for(int shutter=-1;shutter<=1;shutter+=2)g.Box(new Vector3(side*1.9f+shutter*.65f,1.9f,-2.4f),new Vector3(.3f,1.08f,.15f),green,Quaternion.identity);
            }
            g.Box(new Vector3(1.8f,4.0f,1),new Vector3(.65f,2.6f,.75f),Art.Hex("D8D9CB"),Quaternion.identity);
            g.Box(new Vector3(1.8f,5.35f,1),new Vector3(.86f,.2f,.95f),wall,Quaternion.identity);
            for(int i=0;i<12;i++)g.Box(new Vector3(-3.0f+i*.55f,.15f,-3.4f),new Vector3(.50f,.18f,2.1f),wood,Quaternion.identity);
            for(int i=0;i<4;i++)
            {
                float x=-3+i*2;g.Cylinder(new Vector3(x,0,-4.3f),new Vector3(x,2.9f,-4.3f),.065f,.065f,wood);
                g.Cylinder(new Vector3(x,2.9f,-4.3f),new Vector3(x,3,-2.1f),.06f,.06f,wood);
            }
            for(int i=0;i<5;i++)g.Box(new Vector3(0,-i*.17f,-4.5f-i*.5f),new Vector3(2,.18f,.55f),wood,Quaternion.identity);
            return g.Build("Cottage and veranda");
        }

        static Mesh Dock()
        {
            var g=new Geometry();Color wood=Art.Hex("B79263");
            for(int i=0;i<24;i++)g.Box(new Vector3(0,.4f,-7+i*.6f),new Vector3(2.5f,.18f,.52f),i%2==0?wood:Art.Hex("C6A271"),Quaternion.identity);
            for(int i=0;i<6;i++)for(int side=-1;side<=1;side+=2)
                g.Cylinder(new Vector3(side*1.15f,-1.6f,-6+i*2.4f),new Vector3(side*1.15f,1.0f,-6+i*2.4f),.12f,.11f,Art.Hex("826A4B"));
            return g.Build("Jetty planks and posts");
        }

        public static Transform Boat(Transform root)
        {
            var model=BlenderAssets.Create("Sailboat",root,Vector3.zero);BlenderAssets.Wind(model,.27f);return model;
        }
        static Transform LegacyBoat(Transform root)
        {
            var g=new Geometry();Color wood=Art.Hex("B47C46"),deck=Art.Hex("E1B776");
            g.Sphere(new Vector3(0,0,0),new Vector3(1.13f,.55f,2.25f),wood,8,16);
            g.Sphere(new Vector3(0,.36f,0),new Vector3(1.04f,.10f,2.1f),deck,5,16);
            for(int i=-1;i<=1;i++)g.Box(new Vector3(0,.53f,i*.85f),new Vector3(1.85f,.10f,.28f),wood,Quaternion.identity);
            g.Cylinder(new Vector3(0,.4f,.2f),new Vector3(0,4.15f,.2f),.065f,.05f,Art.Hex("8B6541"));
            g.Cylinder(new Vector3(-1.25f,1.2f,-1.65f),new Vector3(1.05f,1.2f,1.6f),.045f,.045f,wood);
            g.Triangle(new Vector3(.02f,1.28f,.12f),new Vector3(.02f,4.0f,.12f),new Vector3(-1.25f,1.28f,-1.65f),Art.Hex("FFF3CF"));
            g.Triangle(new Vector3(.02f,1.35f,.3f),new Vector3(.02f,3.6f,.3f),new Vector3(1.05f,1.35f,1.65f),Art.Hex("F0DBA8"));
            g.Cylinder(new Vector3(.02f,4.01f,.14f),new Vector3(-1.25f,1.26f,-1.65f),.025f,.025f,Art.Hex("CA725B"));
            for(int side=-1;side<=1;side+=2)
                g.Cylinder(new Vector3(side*.96f,.52f,-1.35f),new Vector3(side*.96f,.52f,1.35f),.04f,.04f,wood);
            return Art.Object("Handcrafted sailboat",g.Build("Wooden sailboat"),root,Vector3.zero);
        }
    }
}
