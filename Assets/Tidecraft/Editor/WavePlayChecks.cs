#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Tidecraft.Verification
{
    public static class WavePlayChecks
    {
        static void Require(bool value,string message) { if(!value) throw new Exception(message); }
        static void RunUntil(SandWaves waves,Func<bool> condition)
        {
            for(int i=0;i<1800 && !condition();i++) waves.Tick(.1f);
            Require(condition(),"Wave simulation did not reach expected phase");
        }
        public static string Run()
        {
            var app=UnityEngine.Object.FindFirstObjectByType<SandApp>();
            Require(app && app.Ready,"A live game is required");
            var original=app.CaptureBeach(); var passed=new List<string>();
            int particleSystems=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
            try
            {
                app.ResetBeach(); app.Waves.Action();
                RunUntil(app.Waves,()=>app.Waves.Phase==WavePhase.Lost || app.Waves.Phase==WavePhase.Won);
                Require(app.Waves.Phase==WavePhase.Lost && app.Waves.GoalHealth<=0,"An undefended flag must lose the challenge");
                passed.Add("undefended flag loses after repeated surges");
                app.ResetBeach();
                var wall=new WallRecord();
                SandApp.AddLine(wall,new Vector3(-9.5f,0,-3.5f),new Vector3(-4.5f,0,-3.5f));
                SandApp.AddLine(wall,new Vector3(-4.5f,0,-3.5f),new Vector3(-4.5f,0,1.5f));
                SandApp.AddLine(wall,new Vector3(-4.5f,0,1.5f),new Vector3(-9.5f,0,1.5f));
                SandApp.AddLine(wall,new Vector3(-9.5f,0,1.5f),new Vector3(-9.5f,0,-3.5f));
                app.walls.Add(wall); app.RebuildStructures();
                string built=JsonUtility.ToJson(app.CaptureBeach()); app.Waves.Action();
                RunUntil(app.Waves,()=>app.Waves.Phase==WavePhase.Repair);
                Require(app.Waves.GoalHealth>99 && wall.health<100 && wall.health>0,"Sealed wall should take the hit and protect the flag");
                passed.Add("first wave damages wall while flag stays safe");
                float captured=app.CaptureBeach().walls[0].health; wall.health-=1;
                Require(captured!=wall.health,"Durability snapshot must not alias live records"); wall.health+=1;
                passed.Add("durability is copied into construction snapshots");
                app.Waves.Action(); float timer=app.Waves.Remaining; app.Waves.Tick(.1f);
                Require(app.Waves.Paused && app.Waves.Remaining==timer,"Pause must freeze simulation timers"); app.Waves.Action();
                passed.Add("pause preserves current wave and countdown");
                RunUntil(app.Waves,()=>app.Waves.Phase==WavePhase.Won || app.Waves.Phase==WavePhase.Lost);
                Require(app.Waves.Phase==WavePhase.Won && app.Waves.GoalHealth>0,"A sealed defence must make victory attainable");
                Require(app.Waves.BrokenDefences>0 && app.walls.Count==0,"Accumulated impact must eventually destroy a weakened wall");
                passed.Add("three-wave victory is achievable; exhausted wall breaks");
                app.Waves.Action();
                Require(app.Waves.Phase==WavePhase.Build && JsonUtility.ToJson(app.CaptureBeach())==built,"Retry must restore the pre-battle construction exactly");
                passed.Add("retry restores pre-battle work and durability");
                app.Waves.Action(); RunUntil(app.Waves,()=>app.Waves.Phase==WavePhase.Repair); app.Undo();
                Require(app.Waves.Phase==WavePhase.Build && app.walls.Count==1 && app.walls[0].health==100,"Undo must restore the pre-wave defence");
                passed.Add("undo recovers wave damage and resets challenge safely");
                Require(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length==particleSystems,"Impact effects must reuse their particle system");
                passed.Add("impact effects reuse one particle system");
                return "PASS "+passed.Count+"/"+passed.Count+"\n- "+string.Join("\n- ",passed);
            }
            finally { app.RestoreDefenceCheckpoint(original); }
        }
    }
}
#endif
