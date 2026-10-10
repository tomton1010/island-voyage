using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IslandVoyage
{
    // All scenery and animals are authored as colored meshes: no purchased art is required.
    public sealed class Geometry
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> triangles = new List<int>();

        int Vertex(Vector3 position, Vector3 normal, Color color)
        {
            int index = vertices.Count;
            vertices.Add(position); normals.Add(normal); colors.Add(color);
            return index;
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int i = Vertex(a, n, color); Vertex(b, n, color); Vertex(c, n, color);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        { Triangle(a, b, c, color); Triangle(a, c, d, color); }

        public void Sphere(Vector3 center, Vector3 scale, Color color, int rings = 7, int segments = 12)
        { Sphere(center, scale, Quaternion.identity, color, rings, segments); }

        public void Sphere(Vector3 center, Vector3 scale, Quaternion rotation, Color color, int rings = 7, int segments = 12)
        {
            int start = vertices.Count;
            for (int r = 0; r <= rings; r++)
            {
                float theta = Mathf.PI * r / rings;
                for (int s = 0; s <= segments; s++)
                {
                    float phi = 2 * Mathf.PI * s / segments;
                    Vector3 unit = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    Vector3 normal = new Vector3(unit.x / scale.x, unit.y / scale.y, unit.z / scale.z).normalized;
                    Vertex(center + rotation * Vector3.Scale(unit, scale), rotation * normal, color);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = start + r * (segments + 1) + s, b = a + segments + 1;
                    triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                    triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
                }
        }

        public void Cylinder(Vector3 from, Vector3 to, float bottom, float top, Color color, int sides = 8)
        {
            Vector3 axis = (to - from).normalized;
            Vector3 right = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.forward : Vector3.up).normalized;
            Vector3 front = Vector3.Cross(right, axis).normalized;
            for (int i = 0; i < sides; i++)
            {
                float a = 2 * Mathf.PI * i / sides, b = 2 * Mathf.PI * (i + 1) / sides;
                Vector3 d0 = right * Mathf.Cos(a) + front * Mathf.Sin(a), d1 = right * Mathf.Cos(b) + front * Mathf.Sin(b);
                Quad(from + d0 * bottom, to + d0 * top, to + d1 * top, from + d1 * bottom, color);
                Triangle(from, from + d0 * bottom, from + d1 * bottom, color);
                Triangle(to, to + d1 * top, to + d0 * top, color);
            }
        }

        public void Box(Vector3 center, Vector3 size, Color color, Quaternion rotation)
        {
            Vector3[] points = new Vector3[8];
            for (int i = 0; i < 8; i++)
                points[i] = center + rotation * new Vector3((i & 1) == 0 ? -size.x / 2 : size.x / 2, (i & 2) == 0 ? -size.y / 2 : size.y / 2, (i & 4) == 0 ? -size.z / 2 : size.z / 2);
            Quad(points[0], points[4], points[6], points[2], color);
            Quad(points[1], points[3], points[7], points[5], color);
            Quad(points[0], points[1], points[5], points[4], color);
            Quad(points[2], points[6], points[7], points[3], color);
            Quad(points[0], points[2], points[3], points[1], color);
            Quad(points[4], points[5], points[7], points[6], color);
        }

        public void Leaf(Vector3 root, float length, float width, float angle, Color color)
        {
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            for (int i = 0; i < 7; i++)
            {
                float t0 = i / 7f, t1 = (i + 1) / 7f;
                Vector3 c0 = root + rotation * new Vector3(0, Mathf.Sin(t0 * Mathf.PI) * .9f - t0 * .8f, t0 * length);
                Vector3 c1 = root + rotation * new Vector3(0, Mathf.Sin(t1 * Mathf.PI) * .9f - t1 * .8f, t1 * length);
                Vector3 w0 = rotation * Vector3.right * (Mathf.Sin(t0 * Mathf.PI) * width);
                Vector3 w1 = rotation * Vector3.right * (Mathf.Sin(t1 * Mathf.PI) * width);
                Quad(c0 - w0, c1 - w1, c1, c0, color);
                Quad(c0, c1, c1 + w1, c0 + w0, Color.Lerp(color, Color.white, .12f));
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
        public void BodyProfile(float[] z,float[] widths,float[] heights,Color dorsal,Color ventral,int segments=24)
        {
            int start=vertices.Count;
            for(int row=0;row<z.Length;row++)
            {
                int previous=Mathf.Max(0,row-1),next=Mathf.Min(z.Length-1,row+1);
                float dz=z[next]-z[previous],dw=(widths[next]-widths[previous])/dz,dh=(heights[next]-heights[previous])/dz;
                for(int s=0;s<=segments;s++)
                {
                    float a=s*Mathf.PI*2/segments,c=Mathf.Cos(a),v=Mathf.Sin(a);
                    Vector3 normal=new Vector3(c/widths[row],v/heights[row],-(c*c*dw/widths[row]+v*v*dh/heights[row])).normalized;
                    Vertex(new Vector3(c*widths[row],v*heights[row],z[row]),normal,Color.Lerp(ventral,dorsal,Mathf.SmoothStep(0,1,(v+.6f)/1.15f)));
                }
            }
            for(int row=0;row<z.Length-1;row++)for(int s=0;s<segments;s++)
            {
                int a=start+row*(segments+1)+s,b=a+segments+1;
                triangles.Add(a);triangles.Add(a+1);triangles.Add(b);triangles.Add(b);triangles.Add(a+1);triangles.Add(b+1);
            }
        }
    }

    public static class Art
    {
        static Material material;
        public static Material Material
        {
            get
            {
                if (material == null)
                {
                    material = new Material(Shader.Find("IslandVoyage/Stylized")) { name = "Procedural vertex colors", enableInstancing = true };
                }
                return material;
            }
        }
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out Color value); return value; }
        public static Transform Object(string name, Mesh mesh, Transform parent, Vector3 position)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = Material;
            return go.transform;
        }
    }
}
