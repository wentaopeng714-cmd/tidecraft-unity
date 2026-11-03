using System;
using System.Collections.Generic;
namespace Tidecraft.Verification
{
    // The same production model is exercised both without the Editor and through the Tools menu.
    public static class ModelChecks
    {
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Channel(SandModel model, float end = 10)
        {
            for (float x = 1; x <= end; x += .12f) model.Brush(x, -1, .85f, false);
            model.SolveOcean();
        }
        public static string Run()
        {
            var passed = new List<string>();
            var m = new SandModel(); m.Reset();
            Require(m.Sample(1, -1) < SandModel.Sea, "Basin must be below sea level");
            Require(!m.WetAt(1, -1), "A sealed below-sea-level basin must stay dry"); passed.Add("sealed basin stays dry");
            Require(m.WetAt(12, -1), "Open sea should be wet"); passed.Add("open sea seeded from boundary");
            Channel(m, 4); Require(!m.WetAt(1, -1), "An unfinished ditch must not fill"); passed.Add("unfinished channel stays dry");
            Channel(m); Require(m.WetAt(1, -1), "A completed channel should connect the basin");
            int wetCount = m.ConnectedCells; passed.Add("completed channel fills basin");
            var dug = (float[])m.heights.Clone();
            for (int j = 0; j < 50; j++) m.Brush(4, -1, 1.3f, true);
            m.SolveOcean(); Require(!m.WetAt(1, -1), "A raised sand bank must break connectivity"); passed.Add("sand bank disconnects basin");
            m.Restore(dug); m.SolveOcean(); Require(m.WetAt(1, -1) && m.ConnectedCells == wetCount, "Undo must restore exact topology"); passed.Add("height snapshot restores connectivity");
            for (float z = -2.3f; z < .3f; z += .1f) m.BlockDisc(4, z, .38f);
            m.SolveOcean(); Require(!m.WetAt(1, -1), "Wall footprint must block flow"); passed.Add("built walls block water");
            Array.Clear(m.barriers, 0, m.barriers.Length); m.SolveOcean(); Require(m.WetAt(1, -1), "Removing wall must reconnect flow"); passed.Add("wall removal reconnects water");
            foreach (float x in new[] { -16f, 16f }) foreach (float z in new[] { -16f, 16f }) { m.Brush(x, z, .85f, false); m.Brush(x, z, 1.3f, true); Require(!float.IsNaN(m.Sample(x, z)), "Border edit invalid"); }
            passed.Add("edge brushes remain in bounds");
            var baseline = new SandModel(); baseline.Reset(); m.Reset();
            for (int i = 0; i < m.heights.Length; i++) Require(m.heights[i] == baseline.heights[i], "Reset left edited terrain");
            Require(!m.WetAt(1, -1), "Reset basin must be dry"); passed.Add("reset is deterministic");
            float h = m.Sample(1, -1); m.SolveOcean(); Require(m.Sample(1, -1) == h, "Flood solver must not change terrain"); passed.Add("hydrology preserves terrain");
            return "PASS " + passed.Count + "/" + passed.Count + "\n- " + string.Join("\n- ", passed);
        }
    }
}
