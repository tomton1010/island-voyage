using UnityEngine;

namespace IslandVoyage
{
    public static class BoatModel
    {
        public static Transform Create(Transform parent)
        {
            var g=new Geometry();Color wood=Art.Hex("B6814D"),trim=Art.Hex("7B5435"),deck=Art.Hex("E5BA78");
            float[] z={-2.35f,-2.15f,-1.6f,-.7f,.4f,1.5f,2.25f,2.7f},width={.10f,.68f,1.02f,1.12f,1.05f,.85f,.52f,.035f};
            Vector3 Hull(int row,float angle)=>new Vector3(Mathf.Cos(angle)*width[row],.46f-Mathf.Sin(angle)*(.65f+width[row]*.13f),z[row]);
            for(int row=0;row<z.Length-1;row++)for(int arc=0;arc<12;arc++)
            {
                float a=arc*Mathf.PI/12,b=(arc+1)*Mathf.PI/12;
                g.Quad(Hull(row,a),Hull(row+1,a),Hull(row+1,b),Hull(row,b),Color.Lerp(wood,trim,arc%3==0?.22f:.02f));
            }
            for(int row=0;row<z.Length-1;row++)
            {
                g.Quad(new Vector3(-width[row],.31f,z[row]),new Vector3(-width[row+1],.31f,z[row+1]),new Vector3(width[row+1],.31f,z[row+1]),new Vector3(width[row],.31f,z[row]),deck);
                for(int arc=0;arc<=6;arc++)
                {
                    float a=arc*Mathf.PI/6;g.Cylinder(Hull(row,a),Hull(row+1,a),arc==0||arc==6?.075f:.018f,arc==0||arc==6?.075f:.018f,trim,6);
                }
                g.Cylinder(new Vector3(-width[row],.32f,z[row]),new Vector3(width[row],.32f,z[row]),.012f,.012f,trim,5);
            }
            for(int plank=-3;plank<=3;plank++)g.Box(new Vector3(plank*.24f,.33f,-.25f),new Vector3(.22f,.045f,3.7f),Color.Lerp(deck,wood,(plank+3)%3*.09f),Quaternion.identity);
            for(int bench=0;bench<3;bench++)g.Box(new Vector3(0,.64f,-1.4f+bench*1.15f),new Vector3(1.84f,.13f,.32f),wood,Quaternion.identity);
            g.Box(new Vector3(0,-.43f,-.1f),new Vector3(.13f,.3f,3.4f),trim,Quaternion.identity);
            g.Cylinder(new Vector3(0,.36f,.15f),new Vector3(0,4.6f,.15f),.078f,.045f,trim,10);
            g.Box(new Vector3(0,.68f,-2.22f),new Vector3(.36f,.15f,.4f),trim,Quaternion.identity);
            g.Box(new Vector3(.52f,.57f,-.95f),new Vector3(.48f,.43f,.47f),Art.Hex("8D9F73"),Quaternion.identity);
            for(int i=0;i<4;i++)g.Box(new Vector3(.52f,.59f+i*.09f,-1.19f),new Vector3(.51f,.036f,.025f),trim,Quaternion.identity);
            g.Cylinder(new Vector3(-.7f,.4f,-.9f),new Vector3(-.7f,.93f,-.9f),.22f,.19f,Art.Hex("B29263"),12);
            for(int band=0;band<2;band++)g.Cylinder(new Vector3(-.7f,.48f+band*.34f,-.9f),new Vector3(-.7f,.53f+band*.34f,-.9f),.229f,.229f,Art.Hex("697A78"),12);
            var model=Art.Object("Planked wooden sailing dinghy",g.Build("Hull, strakes, keel, deck and fittings"),parent,Vector3.zero);
            var rudder=new Geometry();rudder.Box(new Vector3(0,-.4f,-.18f),new Vector3(.09f,.85f,.52f),trim,Quaternion.Euler(0,0,0));rudder.Cylinder(Vector3.zero,new Vector3(0,.7f,1.05f),.045f,.045f,wood);
            Art.Object("Rudder",rudder.Build("Rudder and tiller"),model,new Vector3(0,.35f,-2.32f));
            Transform sailRig=new GameObject("Sail rig").transform;sailRig.SetParent(model,false);
            var boom=new Geometry();boom.Cylinder(new Vector3(-1.8f,1.23f,-1.35f),new Vector3(1.4f,1.23f,1.2f),.042f,.042f,trim);
            Art.Object("Boom",boom.Build("Sail boom"),sailRig,Vector3.zero);
            Art.Object("Main sail",Sail(new Vector3(0,4.5f,.15f),new Vector3(-1.8f,1.28f,-1.35f),new Vector3(0,1.28f,.15f),Art.Hex("FFF2D1")),sailRig,Vector3.zero);
            Art.Object("Jib",Sail(new Vector3(0,3.95f,.18f),new Vector3(0,1.32f,.18f),new Vector3(1.37f,1.32f,1.2f),Art.Hex("EDDDB8")),sailRig,Vector3.zero);
            Rope(model,new Vector3(0,4.6f,.15f),new Vector3(0,.55f,2.5f));
            for(int side=-1;side<=1;side+=2)Rope(model,new Vector3(0,4.55f,.15f),new Vector3(side*.94f,.54f,-1.6f));
            Rope(sailRig,new Vector3(0,4.52f,.15f),new Vector3(-1.8f,1.28f,-1.35f));
            for(int turn=0;turn<3;turn++)
            {
                var cord=VoyageEffects.Line(model,"Coiled rope",33,.019f);cord.useWorldSpace=false;cord.startColor=cord.endColor=Art.Hex("D5C299");
                for(int i=0;i<33;i++){float a=i*Mathf.PI/16;cord.SetPosition(i,new Vector3(-.44f+Mathf.Cos(a)*(.15f+turn*.035f),.38f+turn*.016f,.58f+Mathf.Sin(a)*(.15f+turn*.035f)));}
            }
            return model;
        }
        static void Rope(Transform root,Vector3 a,Vector3 b)
        {var line=VoyageEffects.Line(root,"Standing rigging",2,.018f);line.useWorldSpace=false;line.SetPosition(0,a);line.SetPosition(1,b);line.startColor=line.endColor=new Color(.61f,.52f,.38f,.85f);}
        static Mesh Sail(Vector3 a,Vector3 b,Vector3 c,Color color)
        {
            var g=new Geometry();const int steps=10;
            Vector3 Point(int i,int j)
            {
                float u=(float)i/steps,v=(float)j/steps,w=1-u-v;
                return a*w+b*u+c*v+new Vector3(.1f,0,.25f)*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(v*Mathf.PI)*w*4;
            }
            for(int i=0;i<steps;i++)for(int j=0;j<steps-i;j++)
            {
                Color cloth=Color.Lerp(color,Art.Hex("DCCBAD"),(i+j)%3==0?.08f:0);
                g.Triangle(Point(i,j),Point(i+1,j),Point(i,j+1),cloth);
                if(j<steps-i-1)g.Triangle(Point(i+1,j),Point(i+1,j+1),Point(i,j+1),cloth);
            }
            return g.Build("Curved stitched sail");
        }
    }
}
