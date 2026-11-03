using System;
using System.Collections.Generic;

namespace Tidecraft
{
    // Engine-independent heightfield and ocean connectivity. Four-neighbour flow cannot jump a bank.
    [Serializable] public sealed class SandModel
    {
        public const int Size = 129;
        public const float Extent = 16f, Step = 0.25f, Sea = 0.08f;
        public float[] heights = new float[Size * Size];
        [NonSerialized] public bool[] ocean = new bool[(Size - 1) * (Size - 1)];
        [NonSerialized] public bool[] barriers = new bool[(Size - 1) * (Size - 1)];
        [NonSerialized] public int[] distance = new int[(Size - 1) * (Size - 1)];
        [NonSerialized] readonly Queue<int> queue = new Queue<int>();
        public int ConnectedCells { get; private set; }
        public int Revision { get; private set; }
        public static int Index(int x, int z) => z * Size + x;
        public static int CellIndex(int x, int z) => z * (Size - 1) + x;
        public static float World(int i) => i * Step - Extent;
        public static int Grid(float p) => Math.Max(0, Math.Min(Size - 1, (int)Math.Floor((p + Extent) / Step)));
        public bool Contains(float x, float z, float margin = 0) => Math.Abs(x) < Extent - margin && Math.Abs(z) < Extent - margin;
        public void Reset()
        {
            for (int z = 0; z < Size; z++) for (int x = 0; x < Size; x++)
            {
                float wx = World(x), wz = World(z);
                float coast = 6f + 0.8f * (float)Math.Sin(wz * 0.35f) + 0.4f * (float)Math.Cos(wz * 0.85f);
                float y = Clamp((coast - wx) * 0.4f, -1.8f, 0.92f);
                y += 0.035f * (float)(Math.Sin(wx * 1.3) * Math.Cos(wz * 1.1));
                // A dry, below-sea-level basin: dig from its east bank to the sea to fill it.
                float r = (float)Math.Sqrt((wx - 1f) * (wx - 1f) + (wz + 1f) * (wz + 1f));
                if (r < 2.8f) y = Lerp(-0.62f, y, Smooth(Clamp((r - 1.85f) / 0.95f, 0, 1)));
                heights[Index(x, z)] = y;
            }
            Array.Clear(barriers, 0, barriers.Length);
            SolveOcean(); Revision++;
        }
        public float Sample(float wx, float wz)
        {
            float gx = Clamp((wx + Extent) / Step, 0, Size - 1.001f), gz = Clamp((wz + Extent) / Step, 0, Size - 1.001f);
            int x = (int)gx, z = (int)gz;
            return Lerp(Lerp(heights[Index(x, z)], heights[Index(x + 1, z)], gx - x),
                Lerp(heights[Index(x, z + 1)], heights[Index(x + 1, z + 1)], gx - x), gz - z);
        }
        public bool Brush(float wx, float wz, float radius, bool raise)
        {
            bool changed = false;
            int x0 = Grid(wx - radius), x1 = Math.Min(Size - 1, Grid(wx + radius) + 1);
            int z0 = Grid(wz - radius), z1 = Math.Min(Size - 1, Grid(wz + radius) + 1);
            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
            {
                float dx = World(x) - wx, dz = World(z) - wz;
                float r = (float)Math.Sqrt(dx * dx + dz * dz) / radius;
                if (r >= 1) continue;
                int i = Index(x, z);
                float strength = Smooth(Clamp((1 - r) * 2.2f, 0, 1));
                float target = raise ? Lerp(heights[i], 1.6f, strength * 0.2f) : Lerp(heights[i], -0.78f, strength);
                if (Math.Abs(target - heights[i]) > 0.001f) { heights[i] = target; changed = true; }
            }
            if (changed) Revision++;
            return changed;
        }
        public float CellHeight(int x, int z) => (heights[Index(x, z)] + heights[Index(x + 1, z)] + heights[Index(x, z + 1)] + heights[Index(x + 1, z + 1)]) * 0.25f;
        public bool WetAt(float x, float z) => ocean[CellIndex(Math.Min(Size - 2, Grid(x)), Math.Min(Size - 2, Grid(z)))];
        public void SolveOcean()
        {
            int n = Size - 1; ConnectedCells = 0; queue.Clear();
            Array.Clear(ocean, 0, ocean.Length);
            for (int i = 0; i < distance.Length; i++) distance[i] = -1;
            for (int i = 0; i < n; i++) { Visit(i, 0, 0); Visit(i, n - 1, 0); Visit(0, i, 0); Visit(n - 1, i, 0); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue(), x = i % n, z = i / n, d = distance[i] + 1;
                Visit(x - 1, z, d); Visit(x + 1, z, d); Visit(x, z - 1, d); Visit(x, z + 1, d);
            }
        }
        void Visit(int x, int z, int depth)
        {
            int n = Size - 1;
            if (x < 0 || z < 0 || x >= n || z >= n) return;
            int i = CellIndex(x, z);
            if (ocean[i] || barriers[i] || CellHeight(x, z) >= Sea - 0.025f) return;
            ocean[i] = true; distance[i] = depth; ConnectedCells++; queue.Enqueue(i);
        }
        public void BlockDisc(float wx, float wz, float radius)
        {
            for (int z = Grid(wz - radius); z < Math.Min(Size - 1, Grid(wz + radius) + 1); z++)
            for (int x = Grid(wx - radius); x < Math.Min(Size - 1, Grid(wx + radius) + 1); x++)
            {
                float dx = World(x) + Step / 2 - wx, dz = World(z) + Step / 2 - wz;
                if (dx * dx + dz * dz < radius * radius) barriers[CellIndex(x, z)] = true;
            }
        }
        public void Restore(float[] values) { if (values == null || values.Length != heights.Length) throw new ArgumentException("Invalid heightfield"); Array.Copy(values, heights, heights.Length); Revision++; }
        static float Clamp(float v, float a, float b) => Math.Max(a, Math.Min(b, v));
        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        static float Smooth(float t) => t * t * (3 - 2 * t);
    }
    [Serializable] public sealed class WallRecord { public List<Point> points = new List<Point>(); }
    [Serializable] public struct Point { public float x, z; public Point(float x, float z) { this.x = x; this.z = z; } }
    [Serializable] public sealed class TowerRecord { public float x, z, yaw; public int style; }
    [Serializable] public sealed class SandSave
    {
        public int version = 1;
        public float[] heights;
        public List<WallRecord> walls = new List<WallRecord>();
        public List<TowerRecord> towers = new List<TowerRecord>();
    }
}
