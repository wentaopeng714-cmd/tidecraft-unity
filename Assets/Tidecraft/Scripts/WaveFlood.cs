using System;
using System.Collections.Generic;

namespace Tidecraft
{
    // A moving, finite surge. Four-neighbour propagation goes around wall ends and
    // through openings; it cannot jump a sealed defence or a sufficiently high bank.
    public sealed class WaveFlood
    {
        public const int Size = 64;
        public const float Cell = .5f;
        public readonly int[] arrival = new int[Size * Size];
        readonly bool[] rejected = new bool[Size * Size];
        List<int> front = new List<int>(256), next = new List<int>(256);
        public readonly List<int> NewCells = new List<int>(256);
        public int Layer { get; private set; }
        public int Limit { get; private set; }
        public int WetCount { get; private set; }
        public float Level { get; private set; }
        public bool Complete => Layer >= Limit || front.Count == 0;
        public static float World(int i) => -16 + (i + .5f) * Cell;
        public static int Grid(float p) => Math.Max(0, Math.Min(Size - 1, (int)((p + 16) / Cell)));
        public static int Index(int x, int z) => z * Size + x;
        public WaveFlood() { Clear(); }
        public void Clear()
        {
            for (int i = 0; i < arrival.Length; i++) arrival[i] = -1;
            Array.Clear(rejected, 0, rejected.Length); front.Clear(); next.Clear(); NewCells.Clear();
            Layer = WetCount = 0; Limit = 0;
        }
        public void Begin(int limit, float level)
        {
            Clear(); Limit = limit; Level = level;
            for (int z = 0; z < Size; z++)
            {
                int i = Index(Size - 1, z); arrival[i] = 0; front.Add(i); WetCount++;
            }
        }
        public void Advance(Func<float, float, float> terrain, Func<int, bool> barrier)
        {
            NewCells.Clear(); if (Complete) return;
            next.Clear(); Layer++;
            foreach (int i in front)
            {
                int x = i % Size, z = i / Size;
                Visit(x - 1, z, terrain, barrier); Visit(x + 1, z, terrain, barrier);
                Visit(x, z - 1, terrain, barrier); Visit(x, z + 1, terrain, barrier);
            }
            var swap = front; front = next; next = swap;
        }
        void Visit(int x, int z, Func<float, float, float> terrain, Func<int, bool> barrier)
        {
            if (x < 0 || x >= Size || z < 0 || z >= Size) return;
            int i = Index(x, z);
            if (arrival[i] >= 0 || rejected[i]) return;
            if (terrain(World(x), World(z)) > Level || (barrier != null && barrier(i))) { rejected[i] = true; return; }
            arrival[i] = Layer; next.Add(i); NewCells.Add(i); WetCount++;
        }
    }
}
