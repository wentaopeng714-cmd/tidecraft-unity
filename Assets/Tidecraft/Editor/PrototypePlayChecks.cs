#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Tidecraft.Editor
{
    public static class PrototypePlayChecks
    {
        static void Assert(bool condition, string message) { if (!condition) throw new Exception("[Tidecraft play check] " + message); }
        static Vector2 Screen(SandApp app, float x, float z)
        {
            return app.View.Camera.WorldToScreenPoint(new Vector3(x, Mathf.Max(app.Model.Sample(x, z), SandModel.Sea), z));
        }
        public static string Run()
        {
            Assert(Application.isPlaying, "Requires Play Mode");
            var app = UnityEngine.Object.FindFirstObjectByType<SandApp>(); Assert(app && app.Ready, "Game not ready");
            app.View.Home(); app.View.Snap(); app.ResetBeach();
            var results = new List<string>();
            app.SetTool(SandTool.Wall); app.PointerBegin(Screen(app, -8, -3), false);
            for (int i = 1; i <= 30; i++) app.PointerMove(Screen(app, -8 + i * .13f, -3), false);
            app.PointerEnd(false);
            Assert(app.WallCount == 1 && app.walls[0].points.Count > 10, "Wall stroke was not built through screen picking"); results.Add("screen-picked continuous wall");
            app.SetTool(SandTool.Tower); app.PointerBegin(Screen(app, -4.1f, -3), false); app.PointerEnd(false);
            Assert(app.TowerCount == 1 && Mathf.Abs(app.towers[0].x + 4.1f) < .25f, "Tower did not snap to wall endpoint"); results.Add("tower preview / stamp / wall-end snap");
            app.PointerBegin(Screen(app, -4.1f, -3), false); app.PointerEnd(false);
            Assert(app.TowerCount == 1, "Overlapping tower was allowed"); results.Add("overlap rejected");
            app.SetTool(SandTool.Dig); app.PointerBegin(Screen(app, 1, -1), false);
            for (float x = 1; x < 4; x += .13f) app.PointerMove(Screen(app, x, -1), false);
            app.PointerEnd(false); Assert(!app.Model.WetAt(1, -1), "Unfinished ditch filled"); results.Add("unfinished picked ditch remains dry");
            app.PointerBegin(Screen(app, 3.8f, -1), false);
            for (float x = 3.8f; x < 10; x += .13f) app.PointerMove(Screen(app, x, -1), false);
            app.PointerEnd(false); Assert(app.Model.WetAt(1, -1), "Finished picked ditch did not connect basin"); results.Add("picked ditch connects to sea");
            app.Undo(); Assert(!app.Model.WetAt(1, -1), "Undo did not restore dry basin");
            app.Redo(); Assert(app.Model.WetAt(1, -1), "Redo did not restore channel"); results.Add("undo and redo restore topology");
            int walls = app.WallCount, towers = app.TowerCount;
            app.SetTool(SandTool.Wall); app.PointerBegin(Screen(app, -9, 3), false); app.PointerMove(Screen(app, -6, 3), false); app.PointerEnd(true);
            Assert(app.WallCount == walls && app.TowerCount == towers, "Cancelled stroke changed scene"); results.Add("gesture cancellation restores state");
            Assert(app.Hud.IsOverUI(new Vector2(ScreenWidth() / 2, 30)), "Toolbar raycast not blocking edits"); results.Add("toolbar blocks world input");
            Debug.Log("[Tidecraft] Play checks passed: " + results.Count);
            return "PASS " + results.Count + "/" + results.Count + "\n- " + string.Join("\n- ", results);
        }
        static float ScreenWidth() => UnityEngine.Screen.width;
    }
}
#endif
