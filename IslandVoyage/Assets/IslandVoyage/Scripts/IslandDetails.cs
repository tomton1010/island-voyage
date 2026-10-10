using UnityEngine;

namespace IslandVoyage
{
    public static class IslandDetails
    {
        static float Range(System.Random random,float a,float b)=>a+(float)random.NextDouble()*(b-a);

        public static void Create(Transform root)
        {
            var random=new System.Random(4821);
            for(int k=0;k<WorldLayout.Islands.Length;k++)
            {
                var island=WorldLayout.Islands[k];var grass=new Geometry();var stones=new Geometry();
                int count=k==0?30:18;
                for(int i=0;i<count;i++)
                {
                    float angle=Range(random,0,Mathf.PI*2),ratio=Range(random,1.13f,1.58f);
                    Vector3 p=new Vector3(island.center.x+Mathf.Cos(angle)*island.size.x*ratio,-2.3f,island.center.y+Mathf.Sin(angle)*island.size.y*ratio);
                    if(!WorldLayout.IsWater(p,2))continue;
                    p=WorldLayout.OnGround(p,.04f);
                    Transform reef=BlenderAssets.Create("Reef"+((i+k)%5),root,p);
                    reef.localRotation=Quaternion.Euler(0,Range(random,0,360),0);reef.localScale=Vector3.one*Range(random,.65f,1.05f);
                    WorldBuilder.GroundReef(reef);
                    Vector3 stone=WorldLayout.OnGround(p+new Vector3(1.4f,0,1),.16f);
                    if(stone.y<-.8f)stones.Sphere(stone,new Vector3(1,.34f,.8f),Art.Hex("86BEB0"),4,7);
                    for(int j=0;j<5;j++)
                    {
                        Vector3 tuft=WorldLayout.OnGround(p+new Vector3(Range(random,-2.6f,2.6f),0,Range(random,-2.6f,2.6f)));
                        if(tuft.y>-1.7f)continue;
                        for(int blade=0;blade<3;blade++)
                        {
                            Vector3 bottom=WorldLayout.OnGround(tuft+new Vector3(blade*.11f,0,blade*.06f),-.015f);
                            Vector3 tip=bottom+new Vector3(Range(random,-.3f,.3f),Range(random,.5f,1.15f),Range(random,-.25f,.25f));
                            Color color=blade%2==0?Art.Hex("337E68"):Art.Hex("68AC83");
                            grass.Triangle(WorldLayout.OnGround(bottom-Vector3.right*.05f,-.015f),tip,WorldLayout.OnGround(bottom+Vector3.right*.05f,-.015f),color);
                            grass.Triangle(WorldLayout.OnGround(bottom-Vector3.forward*.05f,-.015f),tip,WorldLayout.OnGround(bottom+Vector3.forward*.05f,-.015f),color);
                        }
                    }
                }
                var aquatic=new MaterialPropertyBlock();aquatic.SetFloat("_Aquatic",1);
                foreach(var detail in new[]{Art.Object("Seagrass meadows",grass.Build("Sea grass"),root,Vector3.zero),Art.Object("Visible shallow reef stones",stones.Build("Reef stones"),root,Vector3.zero)})
                {var renderer=detail.GetComponent<MeshRenderer>();renderer.SetPropertyBlock(aquatic);renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
                Beach(root,island,k,random);
            }
            Props(root);
        }

        static Mesh Reef(int variant)
        {
            var g=new Geometry();var random=new System.Random(97+variant*31);
            Color purple=Art.Hex(variant%2==0?"8B75BE":"B57AAD"),pink=Art.Hex("D98BAA"),green=Art.Hex("82BDA4");
            g.Sphere(new Vector3(0,-.06f,0),new Vector3(1.65f,.22f,1.4f),Art.Hex("7EB6A6"),4,10);
            // Hollow tube sponges with dark openings and pale rims.
            for(int i=0;i<3;i++)
            {
                Vector3 p=new Vector3(Range(random,-1.2f,.2f),0,Range(random,-1,1));float h=Range(random,.8f,1.5f),r=Range(random,.18f,.29f);
                g.Cylinder(p,p+Vector3.up*h,r,r*.9f,purple,8);
                for(int s=0;s<8;s++)
                {
                    float a=s*Mathf.PI/4,b=(s+1)*Mathf.PI/4;
                    Vector3 d0=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),d1=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));Vector3 top=p+Vector3.up*(h+.015f);
                    g.Quad(top+d0*r*.9f,top+d1*r*.9f,top+d1*r*.62f,top+d0*r*.62f,Art.Hex("BD9DE0"));
                }
                g.Cylinder(p+Vector3.up*(h+.008f),p+Vector3.up*(h+.012f),r*.62f,r*.62f,Art.Hex("4B4274"),8);
            }
            // Layered table coral and branching pink sea fans.
            for(int i=0;i<4;i++)
            {
                Vector3 p=new Vector3(Range(random,.15f,1.5f),.12f,Range(random,-1.2f,1.2f));
                for(int layer=0;layer<3;layer++)g.Sphere(p+Vector3.up*layer*.18f,new Vector3(.64f-layer*.1f,.085f,.49f-layer*.06f),Color.Lerp(green,Art.Hex("C8D795"),layer*.22f),4,9);
            }
            for(int i=0;i<2;i++)
            {
                Vector3 p=new Vector3(Range(random,-1,1),0,Range(random,-1.4f,1.4f));
                g.Cylinder(p,p+Vector3.up*.8f,.07f,.04f,pink,5);
                for(int branch=-2;branch<=2;branch++)
                {
                    Vector3 end=p+new Vector3(branch*.32f,1.25f-Mathf.Abs(branch)*.15f,.08f);
                    g.Cylinder(p+Vector3.up*.45f,end,.045f,.018f,pink,5);
                    g.Cylinder(end-Vector3.up*.32f,end+Vector3.right*.14f,.025f,.01f,Art.Hex("EBA5B6"),5);
                }
            }
            return g.Build("Reef garden "+variant);
        }

        static void Beach(Transform root,WorldLayout.Island island,int index,System.Random random)
        {
            for(int i=0;i<(index==0?44:14);i++)
            {
                float a=Range(random,0,Mathf.PI*2),r=Range(random,.35f,.85f);
                Vector3 p=WorldLayout.OnGround(new Vector3(island.center.x+Mathf.Cos(a)*island.size.x*r,0,island.center.y+Mathf.Sin(a)*island.size.y*r));
                if(index==0 && Vector2.Distance(new Vector2(p.x,p.z),new Vector2(39,15))<7)continue;
                var shrub=BlenderAssets.Create("Shrub",root,p);shrub.localRotation=Quaternion.Euler(0,i*73,0);shrub.localScale=Vector3.one*Range(random,.7f,1.25f);
            }
            for(int i=0;i<30;i++)
            {
                float a=Range(random,0,Mathf.PI*2),r=Range(random,.90f,.98f);
                Vector3 p=WorldLayout.OnGround(new Vector3(island.center.x+Mathf.Cos(a)*island.size.x*r,0,island.center.y+Mathf.Sin(a)*island.size.y*r));
                var shell=BlenderAssets.Create(i%4==0?"Starfish":"Shell",root,p);shell.localRotation=Quaternion.Euler(0,i*53,0);
            }
        }

        static void Props(Transform root)
        {
            var g=new Geometry();Color wood=Art.Hex("A27A4C"),dark=Art.Hex("775D41"),green=Art.Hex("528B69");
            // Bench, signboard and a picnic table on the beach below the cottage.
            for(int plank=0;plank<3;plank++)g.Box(new Vector3(0,.9f,plank*.25f),new Vector3(3.2f,.14f,.21f),wood,Quaternion.identity);
            g.Box(new Vector3(0,1.6f,.7f),new Vector3(3.2f,.75f,.15f),wood,Quaternion.identity);
            for(int side=-1;side<=1;side+=2)g.Box(new Vector3(side*1.25f,.55f,.3f),new Vector3(.18f,1.1f,.7f),dark,Quaternion.identity);
            BlenderAssets.Create("Bench",root,WorldLayout.OnGround(new Vector3(32,0,3),-.02f));
            g=new Geometry();
            for(int side=-1;side<=1;side+=2)g.Cylinder(new Vector3(side*.85f,0,0),new Vector3(side*.85f,2.1f,0),.08f,.08f,wood);
            g.Box(new Vector3(0,1.6f,0),new Vector3(2.15f,1,.16f),wood,Quaternion.identity);
            g.Box(new Vector3(0,1.6f,-.1f),new Vector3(1.9f,.76f,.07f),green,Quaternion.identity);
            g.Box(new Vector3(0,2.2f,0),new Vector3(2.45f,.18f,.5f),Art.Hex("94AD5F"),Quaternion.Euler(0,0,3));
            BlenderAssets.Create("Noticeboard",root,WorldLayout.OnGround(new Vector3(35,0,7),-.025f));
            g=new Geometry();
            g.Box(new Vector3(0,1.15f,0),new Vector3(3,.16f,1.7f),wood,Quaternion.identity);
            for(int side=-1;side<=1;side+=2)
            {
                g.Box(new Vector3(side*1.05f,.55f,0),new Vector3(.18f,1.1f,1.6f),dark,Quaternion.Euler(0,0,side*12));
                g.Box(new Vector3(0,.65f,side*1.2f),new Vector3(3.4f,.12f,.45f),wood,Quaternion.identity);
            }
            Art.Object("Picnic table",g.Build("Table"),root,WorldLayout.OnGround(new Vector3(38,0,1)));
            g=new Geometry();
            g.Sphere(Vector3.zero,new Vector3(.46f,1.8f,.13f),Art.Hex("72ACA0"),8,12);
            g.Box(new Vector3(0,0,-.14f),new Vector3(.09f,3.1f,.04f),Art.Hex("F4E1B7"),Quaternion.identity);
            var board=Art.Object("Surfboard beside veranda",g.Build("Surfboard"),root,new Vector3(35,3.3f,10));board.localRotation=Quaternion.Euler(0,-15,12);
            board.position+=Vector3.up*(WorldLayout.GroundHeight(board.position)-board.GetComponent<MeshRenderer>().bounds.min.y-.025f);
        }
    }
}
