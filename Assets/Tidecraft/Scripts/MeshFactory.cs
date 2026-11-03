using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Tidecraft
{
    public sealed class MeshFactory
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> triangles = new List<int>();
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int i = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            colors.Add(color.linear); colors.Add(color.linear); colors.Add(color.linear);
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
        }
        public void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col, Vector3 normal)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0) { Triangle(a, c, b, col); Triangle(a, d, c, col); }
            else { Triangle(a, b, c, col); Triangle(a, c, d, col); }
        }
        public void Box(Vector3 center, Vector3 size, Color col, float yaw = 0)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0); Vector3 h = size / 2;
            Vector3[] p = new Vector3[8];
            for (int i = 0; i < 8; i++) p[i] = center + q * new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            Face(p[0], p[4], p[6], p[2], col, q * Vector3.left); Face(p[1], p[3], p[7], p[5], col, q * Vector3.right);
            Face(p[0], p[1], p[5], p[4], col, Vector3.down); Face(p[2], p[6], p[7], p[3], col, Vector3.up);
            Face(p[0], p[2], p[3], p[1], col, q * Vector3.back); Face(p[4], p[5], p[7], p[6], col, q * Vector3.forward);
        }
        public void Cylinder(Vector3 center, float bottom, float top, float height, Color col, int sides = 16)
        {
            Vector3 up = center + Vector3.up * height;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 da = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), db = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                Face(center + da * bottom, center + db * bottom, up + db * top, up + da * top, col, da);
                Triangle(up, up + db * top, up + da * top, col); Triangle(center, center + da * bottom, center + db * bottom, col);
            }
        }
        public Mesh Build(string name)
        {
            var m = new Mesh { name = name, indexFormat = vertices.Count > 65530 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(vertices); m.SetColors(colors); m.SetTriangles(triangles, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        public static Mesh Wall(WallRecord record, SandModel model)
        {
            var b = new MeshFactory(); var pts = record.points;
            if (pts.Count < 2) return b.Build("Empty wall");
            var sides = new Vector3[pts.Count]; var ground = new Vector3[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i]; ground[i] = new Vector3(p.x, model.Sample(p.x, p.z) - .05f, p.z);
                var prev = pts[Mathf.Max(0, i - 1)]; var next = pts[Mathf.Min(pts.Count - 1, i + 1)];
                sides[i] = Vector3.Cross(Vector3.up, new Vector3(next.x - prev.x, 0, next.z - prev.z)).normalized;
            }
            float crenelDistance = 0;
            for (int i = 1; i < ground.Length; i++)
            {
                Vector3 a = ground[i - 1], c = ground[i], sa = sides[i - 1], sc = sides[i];
                Vector3 al = a - sa * .36f, ar = a + sa * .36f, cl = c - sc * .36f, cr = c + sc * .36f;
                Vector3 atl = a - sa * .24f + Vector3.up * 1.25f, atr = a + sa * .24f + Vector3.up * 1.25f;
                Vector3 ctl = c - sc * .24f + Vector3.up * 1.25f, ctr = c + sc * .24f + Vector3.up * 1.25f;
                b.Face(al, cl, ctl, atl, Palette.Sand, -sa); b.Face(ar, atr, ctr, cr, Palette.Sand, sa);
                b.Face(atl, ctl, ctr, atr, Palette.SandLight, Vector3.up);
                if (i == 1) b.Face(al, atl, atr, ar, Palette.Sand, a - c);
                if (i == ground.Length - 1) b.Face(cl, cr, ctr, ctl, Palette.Sand, c - a);
                float length = Vector3.Distance(a, c); float offset = .65f - crenelDistance;
                while (offset <= length)
                {
                    Vector3 p = Vector3.Lerp(a, c, offset / length);
                    float yaw = Mathf.Atan2(c.x - a.x, c.z - a.z) * Mathf.Rad2Deg;
                    b.Box(p + Vector3.up * 1.37f, new Vector3(.48f, .25f, .32f), Palette.SandLight, yaw);
                    offset += .65f;
                }
                crenelDistance = (crenelDistance + length) % .65f;
            }
            return b.Build("Continuous sand wall");
        }
        public static Mesh Tower(int style)
        {
            var b = new MeshFactory();
            float h = style == 2 ? 2.65f : 2.1f;
            if (style == 1)
            {
                b.Box(new Vector3(0, h / 2, 0), new Vector3(1.28f, h, 1.28f), Palette.Sand);
                b.Box(new Vector3(0, h, 0), new Vector3(1.5f, .18f, 1.5f), Palette.SandLight);
                for (int i = 0; i < 4; i++)
                {
                    float yaw = i * 90; Vector3 p = Quaternion.Euler(0, yaw, 0) * new Vector3(.55f, h + .22f, .55f);
                    b.Box(p, new Vector3(.38f, .4f, .38f), Palette.SandLight);
                }
            }
            else
            {
                b.Cylinder(Vector3.zero, .86f, .68f, h, Palette.Sand);
                b.Cylinder(Vector3.up * (h - .12f), .78f, .78f, .24f, Palette.SandLight);
                b.Cylinder(Vector3.up * (h + .12f), .53f, .53f, .015f, Palette.SandDark);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45 * Mathf.Deg2Rad;
                    b.Box(new Vector3(Mathf.Cos(a) * .65f, h + .27f, Mathf.Sin(a) * .65f), new Vector3(.29f, .32f, .29f), Palette.SandLight, -i * 45);
                }
            }
            b.Box(new Vector3(0, h + .65f, 0), new Vector3(.055f, 1.1f, .055f), Palette.Ground);
            b.Face(new Vector3(0, h + 1.17f, 0), new Vector3(.66f, h + .99f, .015f), new Vector3(.48f, h + .64f, .015f), new Vector3(0, h + .77f, 0), Palette.Coral, Vector3.back);
            b.Face(new Vector3(0, h + 1.17f, 0), new Vector3(0, h + .77f, 0), new Vector3(.48f, h + .64f, .015f), new Vector3(.66f, h + .99f, .015f), Palette.Coral, Vector3.forward);
            // Small dark inset, sized to read from the gameplay camera.
            b.Box(new Vector3(0, 1.35f, -.69f), new Vector3(.18f, .31f, .035f), Palette.SandDark);
            return b.Build("Sand tower " + style);
        }
    }
}
