using UnityEngine;

namespace IslandVoyage
{
    public sealed class FishSpecies
    {
        public readonly string Name;
        public readonly Color Color;
        public readonly float Width, Height, Length;
        public FishSpecies(string name, string color, float width, float height, float length)
        { Name = name; Color = Art.Hex(color); Width = width; Height = height; Length = length; }
    }
    public enum MarineKind { Whale, Shark, Dolphin }

    public static class AnimalModels
    {
        public static readonly FishSpecies[] Species = {
            new FishSpecies("小丑鱼", "F39347", .25f,.36f,.63f), new FishSpecies("蓝吊鱼", "4386DA", .19f,.46f,.73f),
            new FishSpecies("黄吊鱼", "F3D74C", .18f,.48f,.61f), new FishSpecies("蝴蝶鱼", "EDDE99", .18f,.47f,.58f),
            new FishSpecies("雀鲷", "9B83C5", .20f,.32f,.49f), new FishSpecies("沙丁鱼", "A0C6CA", .18f,.21f,.76f),
            new FishSpecies("鲭鱼", "57AAAC", .23f,.28f,.86f), new FishSpecies("飞鱼", "78C9DD", .17f,.21f,.71f)
        };
        static readonly Mesh[] fishMeshes = new Mesh[8];
        static readonly Mesh[] fishTails=new Mesh[8];
        static readonly Mesh[] marineMeshes = new Mesh[3];
        public static Mesh Fish(int type)
        {
            if (fishMeshes[type] != null) return fishMeshes[type];
            var s = Species[type]; var g = new Geometry();
            g.Sphere(Vector3.zero, new Vector3(s.Width,s.Height,s.Length), s.Color);
            Color fin = type == 1 ? Art.Hex("F3DC52") : Color.Lerp(s.Color, Color.white,.15f);
            g.Triangle(new Vector3(0,s.Height*.6f,.22f),new Vector3(0,s.Height*1.4f,-.15f),new Vector3(0,s.Height*.6f,-.45f),fin);
            float spread = type == 7 ? .85f : .38f;
            for (int side = -1; side <= 1; side += 2)
            {
                g.Triangle(new Vector3(side*.08f,-.05f,.12f),new Vector3(side*spread,-.08f,-.18f),new Vector3(side*.1f,-.12f,-.32f),fin);
                g.Sphere(new Vector3(side*s.Width*.84f,s.Height*.19f,s.Length*.64f),new Vector3(.038f,.06f,.065f),Art.Hex("122E3A"),4,6);
            }
            if (type == 0 || type == 3 || type == 6)
            {
                Color stripe = type == 0 ? Art.Hex("FFF4D9") : Art.Hex("37596A");
                for(int band=0;band<3;band++)
                {
                    float z=(band-1)*s.Length*.48f, taper=Mathf.Sqrt(1-z*z/(s.Length*s.Length));
                    g.Sphere(new Vector3(0,0,z),new Vector3(s.Width*taper*1.02f,s.Height*taper*1.02f,.045f),stripe,6,10);
                }
            }
            return fishMeshes[type] = g.Build(s.Name);
        }
        public static Mesh FishTail(int type)
        {
            if(fishTails[type]!=null)return fishTails[type];var s=Species[type];var g=new Geometry();
            g.Triangle(Vector3.zero,new Vector3(0,s.Height*.85f,-s.Length*.75f),new Vector3(0,-s.Height*.85f,-s.Length*.75f),type==1?Art.Hex("F3DC52"):s.Color);
            return fishTails[type]=g.Build("Propulsive fish tail");
        }

        public static Mesh Marine(MarineKind kind)
        {
            int index=(int)kind;
            if(marineMeshes[index]!=null)return marineMeshes[index];
            var g=new Geometry();
            float size=kind==MarineKind.Whale ? 2.15f : kind==MarineKind.Shark ? 1.05f : .85f;
            Color top=Art.Hex(kind==MarineKind.Whale ? "536F89" : kind==MarineKind.Shark ? "738E98" : "779FAB");
            g.Sphere(Vector3.zero,new Vector3(.8f*size,.63f*size,2.45f*size),top,9,16);
            g.Sphere(new Vector3(0,-.34f*size,.32f*size),new Vector3(.69f*size,.3f*size,1.9f*size),Art.Hex("DAE4DD"),7,12);
            if(kind!=MarineKind.Whale)
                g.Sphere(new Vector3(0,0,2.0f*size),new Vector3(.29f*size,.23f*size,.9f*size),top,6,12);
            float dorsal=kind==MarineKind.Shark ? 1.65f : .92f;
            g.Triangle(new Vector3(0,.35f*size,.2f*size),new Vector3(0,dorsal*size,-.4f*size),new Vector3(0,.3f*size,-1.2f*size),top);
            for(int side=-1;side<=1;side+=2)
            {
                g.Triangle(new Vector3(side*.5f*size,-.12f*size,.55f*size),new Vector3(side*1.7f*size,-.35f*size,-.6f*size),new Vector3(side*.4f*size,-.25f*size,-.9f*size),top);
                g.Sphere(new Vector3(side*.57f*size,.15f*size,1.65f*size),new Vector3(.045f*size,.072f*size,.08f*size),Art.Hex("142F3D"),4,6);
            }
            return marineMeshes[index]=g.Build(kind.ToString());
        }
        public static Mesh MarineTail(MarineKind kind)
        {
            var g=new Geometry();Color c=Art.Hex("63899A");
            if(kind==MarineKind.Shark)
            {
                g.Triangle(Vector3.zero,new Vector3(0,1.1f,-1.15f),new Vector3(0,.25f,-.95f),c);
                g.Triangle(Vector3.zero,new Vector3(0,-.7f,-1.1f),new Vector3(0,-.12f,-.65f),c);
            }
            else
                for(int side=-1;side<=1;side+=2)
                    g.Sphere(new Vector3(side*.6f,0,-.2f),new Vector3(.83f,.11f,.45f),Quaternion.Euler(0,side*28,0),c,5,9);
            return g.Build(kind+" tail");
        }
    }
}
