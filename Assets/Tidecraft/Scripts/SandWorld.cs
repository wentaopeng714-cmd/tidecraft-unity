using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Tidecraft
{
    public sealed class SandWorld : MonoBehaviour
    {
        public SandModel Model { get; private set; }
        public int VisibleWetCells { get; private set; }
        public bool Filling => front.Count > 0;
        Mesh terrainMesh, waterMesh;
        MeshFilter terrainFilter, waterFilter;
        Vector3[] terrainVertices;
        Color[] terrainColors;
        readonly bool[] visible = new bool[128 * 128];
        readonly Queue<int> front = new Queue<int>();
        readonly bool[] queued = new bool[128 * 128];
        readonly List<Vector3> waterVertices = new List<Vector3>(90000);
        readonly List<Color> waterColors = new List<Color>(90000);
        readonly List<int> waterTriangles = new List<int>(90000);
        readonly Vector3[] clipInput = new Vector3[3], clipOutput = new Vector3[6];
        float nextFlow;
        public void Initialize(SandModel model, Material sand, Material water)
        {
            Model = model;
            var ground = new GameObject("Sculptable beach"); ground.transform.SetParent(transform, false);
            terrainFilter = ground.AddComponent<MeshFilter>(); ground.AddComponent<MeshRenderer>().sharedMaterial = sand;
            int n = SandModel.Size;
            terrainVertices = new Vector3[n * n]; terrainColors = new Color[n * n];
            var tris = new int[(n - 1) * (n - 1) * 6]; int t = 0;
            for (int z = 0; z < n - 1; z++) for (int x = 0; x < n - 1; x++)
            {
                int a = SandModel.Index(x, z), b = a + 1, c = a + n, d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b; tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }
            terrainMesh = new Mesh { name = "Editable sand" }; terrainMesh.MarkDynamic(); terrainMesh.vertices = terrainVertices; terrainMesh.triangles = tris; terrainFilter.sharedMesh = terrainMesh;
            var surface = new GameObject("Connected ocean"); surface.transform.SetParent(transform, false);
            waterFilter = surface.AddComponent<MeshFilter>(); var wr = surface.AddComponent<MeshRenderer>(); wr.sharedMaterial = water; wr.shadowCastingMode = ShadowCastingMode.Off;
            waterMesh = new Mesh { name = "Ocean connected to excavations", indexFormat = IndexFormat.UInt32 }; waterMesh.MarkDynamic(); waterFilter.sharedMesh = waterMesh;
            RebuildGround(); ResetWater();
            var skirt = new MeshFactory();
            for (int i = 0; i < n - 1; i++)
            {
                float a = SandModel.World(i), b = SandModel.World(i + 1), e = SandModel.Extent;
                skirt.Face(new Vector3(a, -2.8f, -e), new Vector3(b, -2.8f, -e), new Vector3(b, Model.Sample(b, -e), -e), new Vector3(a, Model.Sample(a, -e), -e), Palette.SandDark, Vector3.back);
                skirt.Face(new Vector3(a, -2.8f, e), new Vector3(a, Model.Sample(a, e), e), new Vector3(b, Model.Sample(b, e), e), new Vector3(b, -2.8f, e), Palette.SandDark, Vector3.forward);
                skirt.Face(new Vector3(-e, -2.8f, a), new Vector3(-e, Model.Sample(-e, a), a), new Vector3(-e, Model.Sample(-e, b), b), new Vector3(-e, -2.8f, b), Palette.SandDark, Vector3.left);
                skirt.Face(new Vector3(e, -2.8f, a), new Vector3(e, -2.8f, b), new Vector3(e, Model.Sample(e, b), b), new Vector3(e, Model.Sample(e, a), a), Palette.SandDark, Vector3.right);
            }
            var edge = new GameObject("Sand tray edge"); edge.transform.SetParent(transform); edge.AddComponent<MeshFilter>().sharedMesh = skirt.Build("Beach edges"); edge.AddComponent<MeshRenderer>().sharedMaterial = sand;
        }
        public void RebuildGround()
        {
            int n = SandModel.Size;
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                int i = SandModel.Index(x, z); float y = Model.heights[i];
                terrainVertices[i] = new Vector3(SandModel.World(x), y, SandModel.World(z));
                float noise = Mathf.PerlinNoise(x * .42f, z * .42f);
                Color dry = Color.Lerp(Palette.Sand, Palette.SandLight, noise * .38f);
                terrainColors[i] = Color.Lerp(Palette.WetSand, dry, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.25f, .65f, y))).linear;
            }
            terrainMesh.vertices = terrainVertices; terrainMesh.colors = terrainColors; terrainMesh.RecalculateNormals(); terrainMesh.RecalculateBounds();
        }
        public void ResetWater()
        {
            front.Clear(); System.Array.Clear(queued, 0, queued.Length);
            System.Array.Copy(Model.ocean, visible, visible.Length); RebuildWater();
        }
        public void TopologyChanged()
        {
            Model.SolveOcean(); front.Clear(); System.Array.Clear(queued, 0, queued.Length);
            for (int i = 0; i < visible.Length; i++) if (!Model.ocean[i]) visible[i] = false;
            for (int i = 0; i < visible.Length; i++) if (visible[i]) EnqueueNeighbours(i);
            RebuildWater();
        }
        void Enqueue(int x, int z)
        {
            if (x < 0 || z < 0 || x >= 128 || z >= 128) return;
            int i = SandModel.CellIndex(x, z);
            if (Model.ocean[i] && !visible[i] && !queued[i]) { front.Enqueue(i); queued[i] = true; }
        }
        void EnqueueNeighbours(int i) { int x = i % 128, z = i / 128; Enqueue(x - 1, z); Enqueue(x + 1, z); Enqueue(x, z - 1); Enqueue(x, z + 1); }
        void Update()
        {
            if (front.Count == 0 || Time.time < nextFlow) return;
            nextFlow = Time.time + .045f;
            int count = front.Count;
            for (int j = 0; j < count; j++) { int i = front.Dequeue(); visible[i] = true; EnqueueNeighbours(i); }
            RebuildWater();
        }
        void RebuildWater()
        {
            waterVertices.Clear(); waterColors.Clear(); waterTriangles.Clear(); VisibleWetCells = 0;
            for (int z = 0; z < 128; z++) for (int x = 0; x < 128; x++)
            {
                if (!visible[SandModel.CellIndex(x, z)]) continue;
                VisibleWetCells++;
                Vector3 a = terrainVertices[SandModel.Index(x, z)], b = terrainVertices[SandModel.Index(x + 1, z)];
                Vector3 c = terrainVertices[SandModel.Index(x, z + 1)], d = terrainVertices[SandModel.Index(x + 1, z + 1)];
                ClipTriangle(a, c, b); ClipTriangle(b, c, d);
            }
            waterMesh.Clear(); waterMesh.SetVertices(waterVertices); waterMesh.SetColors(waterColors); waterMesh.SetTriangles(waterTriangles, 0); waterMesh.RecalculateBounds();
        }
        void ClipTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            clipInput[0] = a; clipInput[1] = b; clipInput[2] = c; int count = 0;
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = clipInput[i], q = clipInput[(i + 1) % 3]; bool inside = p.y < SandModel.Sea, next = q.y < SandModel.Sea;
                if (inside) clipOutput[count++] = p;
                if (inside != next) clipOutput[count++] = Vector3.Lerp(p, q, (SandModel.Sea - p.y) / (q.y - p.y));
            }
            for (int i = 1; i < count - 1; i++) { WaterVertex(clipOutput[0]); WaterVertex(clipOutput[i]); WaterVertex(clipOutput[i + 1]); }
        }
        void WaterVertex(Vector3 p)
        {
            waterTriangles.Add(waterVertices.Count); waterVertices.Add(new Vector3(p.x, SandModel.Sea + .012f, p.z));
            waterColors.Add(new Color(Mathf.Max(0, SandModel.Sea - p.y), 0, 0, 1));
        }
        public bool Pick(Camera camera, Vector2 screen, out Vector3 hit)
        {
            Ray ray = camera.ScreenPointToRay(screen); hit = default;
            if (ray.direction.y >= -.02f) return false;
            float start = Mathf.Max(0, (2.1f - ray.origin.y) / ray.direction.y), end = (-2.5f - ray.origin.y) / ray.direction.y;
            for (float t = start; t < end; t += .12f)
            {
                Vector3 p = ray.GetPoint(t);
                if (!Model.Contains(p.x, p.z, .4f) || p.y > Mathf.Max(Model.Sample(p.x, p.z), SandModel.Sea)) continue;
                float lo = Mathf.Max(start, t - .12f), hi = t;
                for (int k = 0; k < 7; k++) { float mid = (lo + hi) / 2; Vector3 m = ray.GetPoint(mid); if (m.y > Mathf.Max(Model.Sample(m.x, m.z), SandModel.Sea)) lo = mid; else hi = mid; }
                hit = ray.GetPoint(hi); hit.y = Model.Sample(hit.x, hit.z); return true;
            }
            return false;
        }
        void OnDestroy() { if (terrainMesh) Destroy(terrainMesh); if (waterMesh) Destroy(waterMesh); }
    }
}
