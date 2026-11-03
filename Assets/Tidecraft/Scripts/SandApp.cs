using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
namespace Tidecraft
{
    public enum SandTool { Wall, Tower, Dig, Raise, Remove }
    public sealed class SandApp : MonoBehaviour
    {
        public Shader sandShader, waterShader;
        public TMP_FontAsset interfaceFont;
        public bool Ready { get; private set; }
        public SandModel Model { get; private set; }
        public SandWorld World { get; private set; }
        public BeachCamera View { get; private set; }
        public SandHUD Hud { get; private set; }
        public SandAudio Audio { get; private set; }
        public SandWaves Waves { get; private set; }
        public SandTool Tool { get; private set; } = SandTool.Wall;
        public float BrushRadius { get; private set; } = .85f;
        public int TowerStyle { get; private set; }
        public int WallCount => walls.Count;
        public int TowerCount => towers.Count;
        public bool StrokeActive => drawing;
        public readonly List<WallRecord> walls = new List<WallRecord>();
        public readonly List<TowerRecord> towers = new List<TowerRecord>();
        readonly List<SandSave> undo = new List<SandSave>(), redo = new List<SandSave>();
        Material sandMaterial, waterMaterial, ringMaterial, previewMaterial;
        Transform structures;
        Mesh[] towerMeshes;
        GameObject towerPreview, wallPreview;
        MeshFilter wallPreviewFilter;
        LineRenderer ring;
        WallRecord activeWall;
        SandSave beforeStroke;
        bool drawing, changed, hasPoint, cursorValid, basinConnected, popNextTower;
        Vector3 lastPoint, cursorPoint;
        float nextGround, nextSfx;
        string SavePath => Path.Combine(Application.persistentDataPath, "tidecraft-v1.json");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame() { if (UnityEngine.Object.FindFirstObjectByType<SandApp>() == null) new GameObject("Tidecraft prototype").AddComponent<SandApp>(); }
        void Awake()
        {
            Application.targetFrameRate = Application.platform == RuntimePlatform.WebGLPlayer ? -1 : 60;
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Screen.orientation = ScreenOrientation.Portrait;
                Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = false;
                Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            }
            Model = new SandModel(); Model.Reset();
            if (!sandShader) sandShader = Shader.Find("Tidecraft/Sand");
            if (!waterShader) waterShader = Shader.Find("Tidecraft/Water");
            if (!sandShader || !waterShader) { Debug.LogError("Tidecraft shaders missing. Run Tools/Tidecraft/Prepare playable scene."); return; }
            sandMaterial = new Material(sandShader) { name = "Warm sand" };
            waterMaterial = new Material(waterShader) { name = "Turquoise sea" };
            waterMaterial.SetColor("_Deep", Palette.Deep); waterMaterial.SetColor("_Shallow", Palette.Shallow); waterMaterial.SetColor("_Foam", Palette.Foam);
            ringMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); ringMaterial.SetColor("_BaseColor", Palette.Cream);
            previewMaterial = new Material(sandShader); previewMaterial.SetColor("_BaseColor", Palette.Shallow);
            SetLighting();
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None)) cam.gameObject.SetActive(false);
            View = new GameObject("Touch camera").AddComponent<BeachCamera>(); View.Initialize();
            var cameraData = View.Camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing = true;
            World = new GameObject("Beach").AddComponent<SandWorld>(); World.Initialize(Model, sandMaterial, waterMaterial);
            structures = new GameObject("Player sandcastles").transform;
            towerMeshes = new[] { MeshFactory.Tower(0), MeshFactory.Tower(1), MeshFactory.Tower(2) };
            CreateCursor(); CreateDecor(); SeedCastle(); RebuildStructures();
            Audio = gameObject.AddComponent<SandAudio>(); Audio.Initialize();
            Waves = new GameObject("Wave defence challenge").AddComponent<SandWaves>(); Waves.Initialize(this, sandMaterial);
            RebuildStructures();
            Hud = gameObject.AddComponent<SandHUD>(); Hud.Initialize(this, interfaceFont);
            gameObject.AddComponent<SandInput>().Initialize(this);
            Ready = true; SetTool(SandTool.Wall);
            WebBridge.Report(WallCount, TowerCount, Model.WetAt(1, -1), true);
            Debug.Log("[Tidecraft] Ready: drag walls, stamp towers, dig a channel from the dry basin to the ocean.");
        }
        void SetLighting()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) l.gameObject.SetActive(false);
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Sky; RenderSettings.ambientEquatorColor = Palette.Cream * .65f; RenderSettings.ambientGroundColor = Palette.Ground;
            var sun = new GameObject("Late afternoon sun").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.color = Palette.Sun; sun.intensity = 1.15f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .65f;
            sun.transform.rotation = Quaternion.Euler(43, -35, 0); QualitySettings.shadowDistance = 55;
            var volume = new GameObject("Beach colour grade").AddComponent<Volume>(); volume.isGlobal = true; volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = volume.profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.Neutral);
            var color = volume.profile.Add<ColorAdjustments>(); color.saturation.Override(5); color.contrast.Override(4);
            var vignette = volume.profile.Add<Vignette>(); vignette.intensity.Override(.14f); vignette.color.Override(Palette.Ink);
            var bloom = volume.profile.Add<Bloom>(); bloom.intensity.Override(.25f); bloom.threshold.Override(1.1f);
        }
        void SeedCastle()
        {
            var w = new WallRecord();
            AddLine(w, new Vector3(-9, 0, -3), new Vector3(-5, 0, -3)); AddLine(w, new Vector3(-5, 0, -3), new Vector3(-5, 0, 1));
            walls.Add(w); towers.Add(new TowerRecord { x = -9, z = -3 }); towers.Add(new TowerRecord { x = -5, z = -3, style = 1 });
        }
        public static void AddLine(WallRecord w, Vector3 from, Vector3 to)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / .22f));
            if (w.points.Count == 0) w.points.Add(new Point(from.x, from.z));
            for (int i = 1; i <= steps; i++) { Vector3 p = Vector3.Lerp(from, to, (float)i / steps); w.points.Add(new Point(p.x, p.z)); }
        }
        GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material; return go;
        }
        void CreateCursor()
        {
            ring = new GameObject("Tool footprint").AddComponent<LineRenderer>(); ring.loop = true; ring.positionCount = 64; ring.widthMultiplier = .045f; ring.sharedMaterial = ringMaterial; ring.useWorldSpace = true;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            towerPreview = MeshObject("Tower placement preview", towerMeshes[0], previewMaterial, transform); towerPreview.SetActive(false);
            wallPreview = MeshObject("Wall drawing preview", new Mesh(), sandMaterial, transform); wallPreviewFilter = wallPreview.GetComponent<MeshFilter>(); wallPreview.SetActive(false);
        }
        void CreateDecor()
        {
            var b = new MeshFactory(); var random = new System.Random(16);
            for (int i = 0; i < 90; i++)
            {
                float x = -15 + (float)random.NextDouble() * 7, z = -15 + (float)random.NextDouble() * 30;
                if (Mathf.Abs(z) < 6 && x > -12) continue;
                float y = Model.Sample(x, z), s = .09f + (float)random.NextDouble() * .16f;
                if (i % 3 == 0) b.Cylinder(new Vector3(x, y - .04f, z), s * 1.5f, s, s * 1.1f, Palette.Rock, 7);
                else for (int k = 0; k < 3; k++) b.Box(new Vector3(x + k * .05f, y + s / 2, z), new Vector3(.045f, s * 2, .07f), Palette.Leaf, k * 60 + i * 13);
            }
            MeshObject("Sparse coastal grass and pebbles", b.Build("Coastal details"), sandMaterial, transform);
        }
        public void SetTool(SandTool tool)
        {
            if (drawing) PointerEnd(false);
            Tool = tool; towerPreview.SetActive(false); ring.enabled = false;
            if (Audio) Audio.Click();
            if (Hud) { Hud.Refresh(); Hud.Message(tool == SandTool.Dig ? "从凹池右边挖向大海，打通后海水会流进来" : tool == SandTool.Wall ? "单指拖动画城墙 · 双指移动、缩放和旋转镜头" : tool == SandTool.Tower ? "按住预览，松手盖塔 · 靠近墙端会自动吸附" : tool == SandTool.Raise ? "按住或拖动堆高沙地，可以重新封住水道" : "点击移除一段墙或一座塔"); }
        }
        public void CycleTower() { TowerStyle = (TowerStyle + 1) % 3; towerPreview.GetComponent<MeshFilter>().sharedMesh = towerMeshes[TowerStyle]; Hud.Refresh(); Audio.Click(); }
        public void CycleBrush() { BrushRadius = BrushRadius < .8f ? .85f : BrushRadius < 1.1f ? 1.3f : .55f; Hud.Refresh(); Audio.Click(); }
        Vector2 Aim(Vector2 pos, bool touch) => touch && Tool == SandTool.Tower ? pos + Vector2.up * Mathf.Min(90, Screen.height * .12f) : pos;
        bool Pick(Vector2 screen, bool touch, out Vector3 point) => World.Pick(View.Camera, Aim(screen, touch), out point);
        public void PointerBegin(Vector2 screen, bool touch)
        {
            if (Waves && !Waves.CanBuild) { Hud.Message("海浪来袭中，退潮后继续加固防线"); return; }
            if (Hud.ModalOpen || !Pick(screen, touch, out var p)) return;
            beforeStroke = Snapshot(); drawing = true; changed = false; hasPoint = false;
            if (Tool == SandTool.Wall) { activeWall = new WallRecord(); wallPreview.SetActive(true); }
            ApplyPointer(p); UpdateCursor(p);
        }
        public void PointerMove(Vector2 screen, bool touch)
        {
            if (!drawing) return;
            if (Hud.IsOverUI(screen) || !Pick(screen, touch, out var p)) { hasPoint = false; cursorValid = false; return; }
            // Do not bridge over UI, sea, or a stroke leaving the board.
            if (!hasPoint && Tool == SandTool.Wall && activeWall.points.Count > 0) return;
            ApplyPointer(p); UpdateCursor(p);
        }
        void ApplyPointer(Vector3 p)
        {
            if (Tool == SandTool.Tower) { cursorPoint = SnapTower(p); cursorValid = CanTower(cursorPoint); hasPoint = true; return; }
            if (Tool == SandTool.Remove) { if (!changed) changed = EraseAt(p); return; }
            if (Tool == SandTool.Wall)
            {
                if (Model.Sample(p.x, p.z) < SandModel.Sea + .08f) { hasPoint = false; return; }
                if (!hasPoint) { lastPoint = SnapWall(p); activeWall.points.Add(new Point(lastPoint.x, lastPoint.z)); hasPoint = true; return; }
                float length = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(lastPoint.x, 0, lastPoint.z));
                if (length < .18f || activeWall.points.Count > 1800) return;
                // Sample every segment; a fast swipe must not build across open sea.
                for (float t = .15f; t <= length; t += .15f)
                {
                    Vector3 q = Vector3.Lerp(lastPoint, p, t / length);
                    if (Model.Sample(q.x, q.z) <= SandModel.Sea + .08f) { hasPoint = false; return; }
                }
                AddLine(activeWall, lastPoint, p); lastPoint = p; changed = true; ReplaceMesh(wallPreviewFilter, MeshFactory.Wall(activeWall, Model)); return;
            }
            if (hasPoint)
            {
                float distance = Vector2.Distance(new Vector2(lastPoint.x, lastPoint.z), new Vector2(p.x, p.z));
                int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (BrushRadius * .3f)));
                if (steps == 1 && distance < .03f && Time.time < nextGround) return;
                for (int i = 1; i <= steps; i++) { Vector3 q = Vector3.Lerp(lastPoint, p, (float)i / steps); changed |= Model.Brush(q.x, q.z, BrushRadius, Tool == SandTool.Raise); }
            }
            else changed |= Model.Brush(p.x, p.z, BrushRadius, Tool == SandTool.Raise);
            lastPoint = p; hasPoint = true;
            if (Time.time >= nextGround) { nextGround = Time.time + .09f; World.RebuildGround(); RefreshHydrology(); }
            if (Time.time > nextSfx) { nextSfx = Time.time + .14f; Audio.Scrape(); }
        }
        public void PointerEnd(bool cancel)
        {
            if (!drawing) return;
            drawing = false;
            if (cancel) { ApplySave(beforeStroke, false); }
            else
            {
                if (Tool == SandTool.Tower && cursorValid) { towers.Add(new TowerRecord { x = cursorPoint.x, z = cursorPoint.z, style = TowerStyle }); changed = true; popNextTower = true; Audio.Stamp(); }
                else if (Tool == SandTool.Tower) Hud.Message("请在干沙地放塔，避开已有塔楼和水面");
                if (Tool == SandTool.Wall && activeWall != null && activeWall.points.Count > 1 && changed) { walls.Add(activeWall); Audio.Stamp(); }
                if (changed) { PushUndo(beforeStroke); World.RebuildGround(); RebuildStructures(); }
            }
            activeWall = null; beforeStroke = null; wallPreview.SetActive(false); towerPreview.SetActive(false); ring.enabled = false;
            Hud.Refresh();
        }
        Vector3 SnapWall(Vector3 p)
        {
            foreach (var wall in walls) foreach (var end in new[] { wall.points[0], wall.points[wall.points.Count - 1] }) if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(end.x, end.z)) < .75f) return new Vector3(end.x, Model.Sample(end.x, end.z), end.z);
            foreach (var t in towers) if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(t.x, t.z)) < .9f) return new Vector3(t.x, Model.Sample(t.x, t.z), t.z);
            return p;
        }
        Vector3 SnapTower(Vector3 p) { p = SnapWall(p); p.y = Model.Sample(p.x, p.z); return p; }
        bool CanTower(Vector3 p)
        {
            if (!Model.Contains(p.x, p.z, 1) || p.y < SandModel.Sea + .1f) return false;
            if (Waves && Vector2.Distance(new Vector2(p.x, p.z), SandWaves.Goal) < 1.45f) return false;
            foreach (var t in towers) if (Vector2.Distance(new Vector2(t.x, t.z), new Vector2(p.x, p.z)) < 1.3f) return false;
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; if (Model.Sample(p.x + Mathf.Cos(a) * .7f, p.z + Mathf.Sin(a) * .7f) < SandModel.Sea + .02f) return false; }
            return true;
        }
        void UpdateCursor(Vector3 p)
        {
            if (Tool == SandTool.Tower)
            {
                p = SnapTower(p); cursorPoint = p; cursorValid = CanTower(p); towerPreview.SetActive(true); towerPreview.transform.position = p;
                previewMaterial.SetColor("_BaseColor", cursorValid ? Palette.Shallow : Palette.Coral);
            }
            ring.enabled = Tool != SandTool.Tower; float radius = Tool == SandTool.Wall ? .36f : BrushRadius;
            ringMaterial.SetColor("_BaseColor", Tool == SandTool.Dig || Tool == SandTool.Remove ? Palette.Coral : Palette.Cream);
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2 / 64; float x = p.x + Mathf.Cos(a) * radius, z = p.z + Mathf.Sin(a) * radius;
                ring.SetPosition(i, new Vector3(x, Mathf.Max(Model.Sample(x, z), SandModel.Sea) + .04f, z));
            }
        }
        public void Hover(Vector2 p) { if (!Hud.ModalOpen && !Hud.IsOverUI(p) && Pick(p, false, out var world)) UpdateCursor(world); else { ring.enabled = false; towerPreview.SetActive(false); } }
        void ReplaceMesh(MeshFilter filter, Mesh mesh) { if (filter.sharedMesh) Destroy(filter.sharedMesh); filter.sharedMesh = mesh; }
        public void RebuildStructures()
        {
            for (int i = structures.childCount - 1; i >= 0; i--)
            {
                var child = structures.GetChild(i); child.gameObject.SetActive(false);
                if (child.name.StartsWith("Wall")) Destroy(child.GetComponent<MeshFilter>().sharedMesh); Destroy(child.gameObject);
            }
            foreach (var w in walls)
            {
                var go = MeshObject("Wall", MeshFactory.Wall(w, Model), sandMaterial, structures);
                if (Waves) go.AddComponent<SandDefenseVisual>().Initialize(w, null, Waves.FoamMaterial, View.Camera);
            }
            foreach (var t in towers)
            {
                var go = MeshObject("Tower", towerMeshes[Mathf.Clamp(t.style, 0, 2)], sandMaterial, structures);
                go.transform.position = new Vector3(t.x, Model.Sample(t.x, t.z), t.z); go.transform.rotation = Quaternion.Euler(0, t.yaw, 0);
                if (popNextTower && t == towers[towers.Count - 1]) go.AddComponent<SandStamp>();
                if (Waves) go.AddComponent<SandDefenseVisual>().Initialize(null, t, Waves.FoamMaterial, View.Camera);
            }
            popNextTower = false;
            RefreshHydrology();
            Waves?.RebuildDefences();
        }
        public void RefreshHydrology()
        {
            Array.Clear(Model.barriers, 0, Model.barriers.Length);
            foreach (var w in walls) foreach (var p in w.points) Model.BlockDisc(p.x, p.z, .36f);
            foreach (var t in towers) Model.BlockDisc(t.x, t.z, .72f);
            World.TopologyChanged();
            bool wet = Model.WetAt(1, -1);
            if (wet && !basinConnected && Audio) { Audio.Flow(); Hud?.Message("引水成功！海水正沿着你挖出的沟渠进入凹池"); }
            basinConnected = wet;
            WebBridge.Report(WallCount, TowerCount, wet, Ready);
        }
        bool EraseAt(Vector3 p)
        {
            for (int i = towers.Count - 1; i >= 0; i--) if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(towers[i].x, towers[i].z)) < 1) { towers.RemoveAt(i); RebuildStructures(); return true; }
            for (int i = walls.Count - 1; i >= 0; i--) foreach (var q in walls[i].points) if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(q.x, q.z)) < .7f) { walls.RemoveAt(i); RebuildStructures(); return true; }
            return false;
        }
        SandSave Snapshot()
        {
            var save = new SandSave { heights = (float[])Model.heights.Clone() };
            foreach (var w in walls) save.walls.Add(new WallRecord { points = new List<Point>(w.points), health = w.health });
            foreach (var t in towers) save.towers.Add(new TowerRecord { x = t.x, z = t.z, style = t.style, yaw = t.yaw, health = t.health }); return save;
        }
        void PushUndo(SandSave state) { undo.Add(state); if (undo.Count > 24) undo.RemoveAt(0); redo.Clear(); }
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;
        public void Undo()
        {
            if (drawing) PointerEnd(false); if (!CanUndo) return;
            redo.Add(Snapshot()); var last = undo[undo.Count - 1]; undo.RemoveAt(undo.Count - 1); ApplySave(last); Hud.Message("已撤销上一步");
        }
        public void Redo()
        {
            if (drawing) PointerEnd(false); if (!CanRedo) return;
            undo.Add(Snapshot()); var last = redo[redo.Count - 1]; redo.RemoveAt(redo.Count - 1); ApplySave(last); Hud.Message("已重做");
        }
        public SandSave CaptureBeach() => Snapshot();
        public void RecordWaveUndo() { PushUndo(Snapshot()); Hud.Refresh(); }
        public void RestoreDefenceCheckpoint(SandSave save) { PushUndo(Snapshot()); ApplySave(save); }
        void ApplySave(SandSave save, bool resetChallenge = true)
        {
            if (resetChallenge) Waves?.ResetChallenge();
            Model.Restore(save.heights); walls.Clear(); towers.Clear();
            foreach (var w in save.walls) walls.Add(new WallRecord { points = new List<Point>(w.points), health = w.health > 0 ? w.health : 100 });
            foreach (var t in save.towers) towers.Add(new TowerRecord { x = t.x, z = t.z, style = t.style, yaw = t.yaw, health = t.health > 0 ? t.health : 100 });
            World.RebuildGround(); RebuildStructures(); World.ResetWater(); if (Hud) Hud.Refresh();
            if (resetChallenge) Waves?.ResetChallenge();
        }
        public void Save()
        {
            try
            {
                if (drawing) PointerEnd(false);
                string json = JsonUtility.ToJson(Snapshot());
#if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.SetString("TidecraftBeach.v1", json); PlayerPrefs.Save();
#else
                string temp = SavePath + ".tmp"; File.WriteAllText(temp, json); File.Copy(temp, SavePath, true); File.Delete(temp);
#endif
                Hud.Message("作品已保存在这台设备上"); Audio.Click();
            }
            catch (Exception e) { Hud.Message("保存失败：" + e.Message); Debug.LogException(e); }
        }
        public void Load()
        {
            if (drawing) PointerEnd(false);
#if UNITY_WEBGL && !UNITY_EDITOR
            bool exists = PlayerPrefs.HasKey("TidecraftBeach.v1");
#else
            bool exists = File.Exists(SavePath);
#endif
            if (!exists) { Hud.Message("还没有存档，先点保存"); return; }
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                string json = PlayerPrefs.GetString("TidecraftBeach.v1");
#else
                string json = File.ReadAllText(SavePath);
#endif
                var data = JsonUtility.FromJson<SandSave>(json); Validate(data); PushUndo(Snapshot()); ApplySave(data); Hud.Message("已读取作品，仍可撤销");
            }
            catch (Exception e) { Hud.Message("存档无法读取，当前作品已保留"); Debug.LogWarning("[Tidecraft] Load rejected: " + e.Message); }
        }
        public static void Validate(SandSave s)
        {
            if (s == null || s.version != 1 || s.heights == null || s.heights.Length != SandModel.Size * SandModel.Size || s.walls == null || s.towers == null || s.walls.Count > 500 || s.towers.Count > 500) throw new ArgumentException("Invalid save");
            foreach (float h in s.heights) if (float.IsNaN(h) || float.IsInfinity(h) || h < -3 || h > 3) throw new ArgumentException("Invalid height");
            foreach (var w in s.walls) { if (w == null || float.IsNaN(w.health) || float.IsInfinity(w.health) || w.health < 0 || w.health > 100 || w.points == null || w.points.Count < 2 || w.points.Count > 2000) throw new ArgumentException("Invalid wall"); foreach (var p in w.points) CheckPoint(p.x, p.z); }
            foreach (var t in s.towers) { if (t == null || float.IsNaN(t.health) || float.IsInfinity(t.health) || t.health < 0 || t.health > 100 || t.style < 0 || t.style > 2 || float.IsNaN(t.yaw) || float.IsInfinity(t.yaw)) throw new ArgumentException("Invalid tower"); CheckPoint(t.x, t.z); }
        }
        static void CheckPoint(float x, float z) { if (float.IsNaN(x) || float.IsNaN(z) || float.IsInfinity(x) || float.IsInfinity(z) || Mathf.Abs(x) > 16 || Mathf.Abs(z) > 16) throw new ArgumentException("Invalid position"); }
        public void ResetBeach() { if (drawing) PointerEnd(false); PushUndo(Snapshot()); Waves?.ResetChallenge(); Model.Reset(); walls.Clear(); towers.Clear(); basinConnected = false; World.RebuildGround(); RebuildStructures(); World.ResetWater(); Hud.Refresh(); Hud.Message("沙滩已清空，凹池已恢复 · 可撤销"); }
        public void DemoChannel()
        {
            PushUndo(Snapshot());
            for (float x = 1; x < 10; x += .15f) Model.Brush(x, -1, .85f, false);
            World.RebuildGround(); RefreshHydrology(); Hud.Refresh();
        }
        void OnApplicationPause(bool pause) { if (pause && Ready) { if (drawing) PointerEnd(false); Save(); } }
        void OnDestroy()
        {
            if (sandMaterial) Destroy(sandMaterial); if (waterMaterial) Destroy(waterMaterial); if (ringMaterial) Destroy(ringMaterial); if (previewMaterial) Destroy(previewMaterial);
            if (towerMeshes != null) foreach (var mesh in towerMeshes) if (mesh) Destroy(mesh);
        }
    }
}
