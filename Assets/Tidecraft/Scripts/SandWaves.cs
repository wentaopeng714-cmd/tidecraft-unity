using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tidecraft
{
    public enum WavePhase { Build, Warning, Surge, Ebb, Repair, Won, Lost }
    public sealed class SandWaves : MonoBehaviour
    {
        public static readonly Vector2 Goal = new Vector2(-7, -1);
        public WavePhase Phase { get; private set; }
        public int Wave { get; private set; }
        public float GoalHealth { get; private set; } = 100;
        public float Remaining { get; private set; }
        public int DefenceHits { get; private set; }
        public int BrokenDefences { get; private set; }
        public bool Paused { get; private set; }
        public bool CanBuild => Phase != WavePhase.Surge && Phase != WavePhase.Ebb;
        public Material FoamMaterial { get; private set; }
        public WaveFlood Flood { get; private set; }
        public int SplashCount => spray ? spray.particleCount : 0;
        SandApp app; SandSave checkpoint;
        readonly object[] obstacles = new object[WaveFlood.Size * WaveFlood.Size];
        readonly HashSet<object> hitThisWave = new HashSet<object>();
        readonly List<Vector3> vertices = new List<Vector3>(24000);
        readonly List<Color> colors = new List<Color>(24000);
        readonly List<int> indices = new List<int>(36000);
        readonly LineRenderer[] swell = new LineRenderer[3];
        Mesh mesh; MeshRenderer surface; ParticleSystem spray; Transform flag;
        float accumulator, ebbTime, nextReport, nextSound, nextMesh; int goalCells;
        public void Initialize(SandApp owner, Material sand)
        {
            app = owner; Flood = new WaveFlood();
            FoamMaterial = new Material(Resources.Load<Shader>("TideWave")) { name = "Tidecraft surge and foam" };
            var water = new GameObject("Advancing storm surge"); water.transform.SetParent(transform, false);
            water.AddComponent<MeshFilter>().sharedMesh = mesh = new Mesh { name = "Transient surge", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic(); surface = water.AddComponent<MeshRenderer>(); surface.sharedMaterial = FoamMaterial; surface.shadowCastingMode = ShadowCastingMode.Off;
            for (int k = 0; k < swell.Length; k++)
            {
                var line = new GameObject("Rolling sea crest " + k).AddComponent<LineRenderer>(); line.transform.SetParent(transform, false);
                line.sharedMaterial = FoamMaterial; line.positionCount = 65; line.useWorldSpace = true; line.shadowCastingMode = ShadowCastingMode.Off;
                swell[k] = line;
            }
            var b = new MeshFactory();
            b.Cylinder(Vector3.zero, .95f, .78f, .25f, Palette.Shallow, 12);
            b.Cylinder(Vector3.up * .25f, .62f, .5f, .55f, Palette.SandLight, 12);
            b.Box(new Vector3(0, 1.7f, 0), new Vector3(.07f, 2.1f, .07f), Palette.Ink);
            b.Face(new Vector3(0, 2.7f, -.02f), new Vector3(1.08f, 2.5f, -.02f), new Vector3(.88f, 1.93f, -.02f), new Vector3(0, 2.1f, -.02f), Palette.Shallow, Vector3.back);
            b.Face(new Vector3(0, 2.7f, .02f), new Vector3(0, 2.1f, .02f), new Vector3(.88f, 1.93f, .02f), new Vector3(1.08f, 2.5f, .02f), Palette.Shallow, Vector3.forward);
            var goal = new GameObject("Protect the turquoise flag"); goal.transform.SetParent(transform, false); flag = goal.transform;
            flag.position = new Vector3(Goal.x, app.Model.Sample(Goal.x, Goal.y), Goal.y);
            goal.AddComponent<MeshFilter>().sharedMesh = b.Build("Protected flag platform"); goal.AddComponent<MeshRenderer>().sharedMaterial = sand;
            var ring = new GameObject("Goal boundary").AddComponent<LineRenderer>(); ring.transform.SetParent(transform, false);
            ring.sharedMaterial = FoamMaterial; ring.positionCount = 65; ring.widthMultiplier = .075f; ring.startColor = ring.endColor = Palette.Shallow;
            for (int i = 0; i <= 64; i++) { float a = i * Mathf.PI * 2 / 64; float x = Goal.x + Mathf.Cos(a) * 1.25f, z = Goal.y + Mathf.Sin(a) * 1.25f; ring.SetPosition(i, new Vector3(x, app.Model.Sample(x, z) + .04f, z)); }
            for (int z = 0; z < WaveFlood.Size; z++) for (int x = 0; x < WaveFlood.Size; x++) if (InsideGoal(x, z)) goalCells++;
            CreateSpray(); RebuildDefences(); ResetChallenge();
        }
        bool InsideGoal(int x, int z) => (new Vector2(WaveFlood.World(x), WaveFlood.World(z)) - Goal).sqrMagnitude < 1.15f * 1.15f;
        public void RebuildDefences()
        {
            System.Array.Clear(obstacles, 0, obstacles.Length);
            foreach (var wall in app.walls) foreach (var p in wall.points) Block(p.x, p.z, .48f, wall);
            foreach (var tower in app.towers) Block(tower.x, tower.z, .94f, tower);
        }
        void Block(float x, float z, float radius, object defence)
        {
            for (int iz = WaveFlood.Grid(z - radius); iz <= WaveFlood.Grid(z + radius); iz++)
            for (int ix = WaveFlood.Grid(x - radius); ix <= WaveFlood.Grid(x + radius); ix++)
                if ((new Vector2(WaveFlood.World(ix) - x, WaveFlood.World(iz) - z)).sqrMagnitude < radius * radius) obstacles[WaveFlood.Index(ix, iz)] = defence;
        }
        public void Action()
        {
            if (Phase == WavePhase.Build)
            {
                if (app.StrokeActive) app.PointerEnd(false);
                checkpoint = app.CaptureBeach(); Wave = 0; GoalHealth = 100; DefenceHits = BrokenDefences = 0; Paused = false; NextWave();
            }
            else if (Phase == WavePhase.Won || Phase == WavePhase.Lost)
            {
                var saved = checkpoint;
                if (saved != null) app.RestoreDefenceCheckpoint(saved); else ResetChallenge();
                app.Hud.Message("已恢复迎浪前的作品，重新加固后再挑战");
            }
            else { Paused = !Paused; app.Hud.Message(Paused ? "海浪已暂停，点继续海浪恢复" : "海浪继续推进"); }
            app.Audio.Click(); Publish();
        }
        public void ResetChallenge()
        {
            if (Flood == null) return;
            Phase = WavePhase.Build; Wave = 0; GoalHealth = 100; Remaining = 0; Paused = false;
            DefenceHits = BrokenDefences = 0; checkpoint = null; Flood.Clear(); mesh.Clear(); hitThisWave.Clear();
            if (flag) { flag.rotation = Quaternion.identity; flag.position = new Vector3(Goal.x, app.Model.Sample(Goal.x, Goal.y), Goal.y); }
            Publish();
        }
        void NextWave()
        {
            Wave++; Phase = WavePhase.Warning; Remaining = 3; hitThisWave.Clear(); accumulator = 0;
            app.Hud?.Message("海浪将至！用城墙围住青色旗台，堵住墙端和缺口");
            app.Audio.Warning(); Debug.Log("[Tidecraft] Wave " + Wave + "/3 warning"); Publish();
        }
        void BeginSurge()
        {
            if (app.StrokeActive) app.PointerEnd(false);
            if (Wave == 1) checkpoint = app.CaptureBeach();
            app.RecordWaveUndo(); RebuildDefences();
            Flood.Begin(48 + Wave * 4, 1.18f + (Wave - 1) * .2f);
            Phase = WavePhase.Surge; accumulator = 0; app.Audio.Wave();
            app.Hud.Message("第 " + Wave + " 波来袭！观察海水从哪里突破，退潮后补上缺口");
            Debug.Log("[Tidecraft] Wave " + Wave + " advances; defences=" + (app.WallCount + app.TowerCount)); Publish();
        }
        void Update()
        {
            if (app == null || !app.Ready) return;
            AmbientWaves();
            Tick(Mathf.Min(Time.deltaTime, .1f));
        }
        public void Tick(float dt)
        {
            dt = Mathf.Clamp(dt, 0, .1f);
            if (!Paused && !app.Hud.ModalOpen)
            {
                if (Phase == WavePhase.Warning || Phase == WavePhase.Repair)
                {
                    Remaining = Mathf.Max(0, Remaining - dt);
                    if (Remaining <= 0) { if (Phase == WavePhase.Warning) BeginSurge(); else NextWave(); }
                }
                else if (Phase == WavePhase.Surge)
                {
                    accumulator += dt;
                    while (accumulator >= .11f && !Flood.Complete)
                    {
                        accumulator -= .11f; Flood.Advance(app.Model.Sample, HitBarrier);
                        foreach (int i in Flood.NewCells) if (InsideGoal(i % WaveFlood.Size, i / WaveFlood.Size))
                        {
                            float damage = (30 + Wave * 12f) / Mathf.Max(1, goalCells);
                            GoalHealth = Mathf.Max(0, GoalHealth - damage);
                            Splash(new Vector3(WaveFlood.World(i % WaveFlood.Size), 1.15f, WaveFlood.World(i / WaveFlood.Size)), Palette.Foam, 4);
                        }
                    }
                    if (Flood.Complete || GoalHealth <= 0) { Phase = WavePhase.Ebb; ebbTime = 2.2f; if (GoalHealth <= 0) app.Audio.Warning(); }
                    if (Time.time >= nextMesh) { nextMesh = Time.time + .06f; RenderSurge(1); }
                }
                else if (Phase == WavePhase.Ebb)
                {
                    ebbTime -= dt; if (Time.time >= nextMesh) { nextMesh = Time.time + .06f; RenderSurge(Mathf.Clamp01(ebbTime / 2.2f)); }
                    if (ebbTime <= 0) FinishWave();
                }
            }
            if (flag) flag.rotation = Quaternion.Euler(0, 0, GoalHealth <= 0 ? -35 : Mathf.Sin(Time.time * 2.5f) * 1.4f);
            if (Time.unscaledTime >= nextReport) { nextReport = Time.unscaledTime + .15f; Publish(); }
        }
        bool HitBarrier(int cell)
        {
            object defence = obstacles[cell]; if (defence == null) return false;
            var wall = defence as WallRecord; var tower = defence as TowerRecord;
            if ((wall != null && !app.walls.Contains(wall)) || (tower != null && !app.towers.Contains(tower))) return false;
            if (hitThisWave.Add(defence))
            {
                float damage = 16 + Wave * 12;
                if (tower != null) damage *= tower.style == 1 ? .55f : tower.style == 0 ? .7f : .9f;
                if (wall != null) wall.health = Mathf.Max(0, wall.health - damage); else tower.health = Mathf.Max(0, tower.health - damage);
                DefenceHits++;
                Vector3 p = new Vector3(WaveFlood.World(cell % WaveFlood.Size), 1.2f, WaveFlood.World(cell / WaveFlood.Size));
                Splash(p, Palette.Foam, 16);
                if (Time.time >= nextSound) { nextSound = Time.time + .18f; app.Audio.Impact(); }
                bool broken = wall != null ? wall.health <= 0 : tower.health <= 0;
                if (broken)
                {
                    BrokenDefences++; Splash(p, Palette.Sand, 22);
                    if (wall != null) app.walls.Remove(wall); else app.towers.Remove(tower);
                    app.RebuildStructures(); app.Hud.Refresh(); app.Hud.Message("防线被冲开了！退潮后补墙，或点撤销恢复这一波前的作品");
                    Debug.Log("[Tidecraft] Wave " + Wave + " broke a defence; remaining=" + (app.WallCount + app.TowerCount));
                    return false;
                }
            }
            return true;
        }
        void FinishWave()
        {
            Flood.Clear(); mesh.Clear();
            if (GoalHealth <= 0)
            {
                Phase = WavePhase.Lost; app.Hud.Message("旗台失守！连好墙线并堵住缺口，再试一次");
            }
            else if (Wave >= 3)
            {
                Phase = WavePhase.Won; app.Audio.Victory();
                Splash(new Vector3(Goal.x, 2, Goal.y), Palette.Shallow, 44);
                app.Hud.Message("守住了！三轮海浪已退去，你的沙堡防线成功了");
            }
            else { Phase = WavePhase.Repair; Remaining = 16; app.Hud.Message("海浪退去，抓紧补墙或堆高沙堤；下一波会更强"); app.Audio.Flow(); }
            Debug.Log("[Tidecraft] Wave " + Wave + " finished: " + Phase + ", flag=" + GoalHealth.ToString("F0")); Publish();
        }
        public string ActionLabel => Phase == WavePhase.Build ? "迎战海浪" : Phase == WavePhase.Won || Phase == WavePhase.Lost ? "重新布防" : Paused ? "继续海浪" : "暂停海浪";
        public string StatusLabel => Phase == WavePhase.Build ? "守护青色旗台 · 挡住三轮海浪" : Paused ? "海浪暂停 · 旗台 " + Mathf.CeilToInt(GoalHealth) + "%" : Phase == WavePhase.Warning ? "第 " + Wave + "/3 波 · " + Mathf.CeilToInt(Remaining) + " 秒后抵达" : Phase == WavePhase.Repair ? "休整 " + Mathf.CeilToInt(Remaining) + " 秒 · 加固防线" : Phase == WavePhase.Won ? "防守成功！旗台 " + Mathf.CeilToInt(GoalHealth) + "%" : Phase == WavePhase.Lost ? "旗台失守 · 重新加固再挑战" : "第 " + Wave + "/3 波 · 旗台 " + Mathf.CeilToInt(GoalHealth) + "%";
        void Publish()
        {
            app.Hud?.RefreshWaves();
            WebBridge.ReportWaves(Phase.ToString().ToLowerInvariant(), Wave, Mathf.CeilToInt(GoalHealth), DefenceHits, BrokenDefences, Flood?.WetCount ?? 0, Paused);
        }
        void AmbientWaves()
        {
            for (int k = 0; k < swell.Length; k++)
            {
                float p = Mathf.Repeat(Time.time * .12f + k / 3f, 1);
                Color c = Palette.Alpha(Palette.Foam, Mathf.Sin(p * Mathf.PI) * .8f); swell[k].startColor = swell[k].endColor = c; swell[k].widthMultiplier = .1f + p * .14f;
                for (int i = 0; i <= 64; i++)
                {
                    float z = -16 + i * .5f;
                    float coast = 6 + .8f * Mathf.Sin(z * .35f) + .4f * Mathf.Cos(z * .85f);
                    swell[k].SetPosition(i, new Vector3(coast + (1 - p) * 7 + Mathf.Sin(z * .6f + k) * .2f, SandModel.Sea + .08f + Mathf.Sin(p * Mathf.PI) * .08f, z));
                }
            }
        }
        bool VisibleCell(int x, int z, float retreat)
        {
            if (x < 0 || z < 0 || x >= WaveFlood.Size || z >= WaveFlood.Size) return false;
            int a = Flood.arrival[WaveFlood.Index(x, z)]; return a >= 0 && a <= Flood.Layer * retreat + 1;
        }
        float SurfaceHeight(float x, float z)
        {
            int arrived = Flood.arrival[WaveFlood.Index(WaveFlood.Grid(x), WaveFlood.Grid(z))];
            float crest = arrived < 0 ? 0 : Mathf.Exp(-(Flood.Layer - arrived) * .45f) * (.28f + Wave * .08f);
            return Mathf.Max(SandModel.Sea, app.Model.Sample(x, z)) + .08f + crest + .035f * Mathf.Sin(x * 3 + z * 2 - Time.time * 5);
        }
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            int i = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int j = 0; j < 4; j++) colors.Add(color);
            indices.Add(i); indices.Add(i + 1); indices.Add(i + 2); indices.Add(i); indices.Add(i + 2); indices.Add(i + 3);
        }
        void Edge(Vector3 a, Vector3 b, Vector3 inward, float alpha)
        {
            a.y += .08f; b.y += .08f;
            Quad(a, b, b + inward * .32f + Vector3.up * .035f, a + inward * .32f + Vector3.up * .035f, Palette.Alpha(Palette.Foam, alpha));
        }
        void RenderSurge(float retreat)
        {
            vertices.Clear(); colors.Clear(); indices.Clear();
            for (int z = 0; z < WaveFlood.Size; z++) for (int x = 0; x < WaveFlood.Size; x++)
            {
                if (!VisibleCell(x, z, retreat)) continue;
                float x0 = x * .5f - 16, z0 = z * .5f - 16;
                var a = new Vector3(x0, SurfaceHeight(x0, z0), z0); var b = new Vector3(x0, SurfaceHeight(x0, z0 + .5f), z0 + .5f);
                var c = new Vector3(x0 + .5f, SurfaceHeight(x0 + .5f, z0 + .5f), z0 + .5f); var d = new Vector3(x0 + .5f, SurfaceHeight(x0 + .5f, z0), z0);
                Color water = Color.Lerp(Palette.Sea, Palette.Shallow, .4f); water.a = .74f * Mathf.Min(1, retreat * 4);
                Quad(a, b, c, d, water);
                float alpha = Mathf.Min(1, retreat * 3);
                if (!VisibleCell(x - 1, z, retreat)) Edge(a, b, Vector3.right, alpha);
                if (!VisibleCell(x, z - 1, retreat)) Edge(d, a, Vector3.forward, alpha);
                if (!VisibleCell(x, z + 1, retreat)) Edge(b, c, Vector3.back, alpha);
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
        }
        void CreateSpray()
        {
            var go = new GameObject("Pooled sea spray and sand fragments"); go.transform.SetParent(transform, false); spray = go.AddComponent<ParticleSystem>();
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = spray.main; main.playOnAwake = false; main.loop = false; main.duration = .8f; main.startLifetime = .65f; main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(.09f, .22f); main.startColor = Palette.Foam; main.gravityModifier = 1.2f; main.maxParticles = 384; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = spray.emission; emission.enabled = false; var shape = spray.shape; shape.enabled = false;
            var size = spray.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            var color = spray.colorOverLifetime; color.enabled = true; var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Palette.Foam, 0), new GradientColorKey(Palette.Foam, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); color.color = gradient;
            var renderer = spray.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = FoamMaterial; renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        void Splash(Vector3 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var p = new ParticleSystem.EmitParams { position = position + Random.insideUnitSphere * .22f, velocity = new Vector3(Random.Range(-1.7f, 1.4f), Random.Range(1.5f, 3.8f), Random.Range(-1.8f, 1.8f)), startColor = color };
                spray.Emit(p, 1);
            }
        }
        void OnDestroy()
        {
            if (mesh) Destroy(mesh); if (FoamMaterial) Destroy(FoamMaterial);
            if (flag) { var f = flag.GetComponent<MeshFilter>(); if (f && f.sharedMesh) Destroy(f.sharedMesh); }
        }
    }
}
