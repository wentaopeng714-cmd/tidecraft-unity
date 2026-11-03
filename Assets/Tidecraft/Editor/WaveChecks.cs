using System;
using System.Collections.Generic;
namespace Tidecraft.Verification
{
    public static class WaveChecks
    {
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static WaveFlood Run(int limit, float level, Func<float,float,float> height, Func<int,bool> block)
        {
            var wave = new WaveFlood(); wave.Begin(limit, level);
            int guard=0; while(!wave.Complete && guard++<200) wave.Advance(height,block);
            Require(guard<200,"Surge must terminate"); return wave;
        }
        static bool Reached(WaveFlood wave,float x=-7,float z=-1) => wave.arrival[WaveFlood.Index(WaveFlood.Grid(x),WaveFlood.Grid(z))]>=0;
        public static string Run()
        {
            var passed=new List<string>(); var beach=new SandModel(); beach.Reset();
            var before=(float[])beach.heights.Clone();
            var open=Run(60,1.2f,beach.Sample,null);
            Require(Reached(open),"An unprotected flag must be reachable by the surge"); passed.Add("open beach exposes flag");
            var full=Run(70,1.2f,beach.Sample,i=>i%64==29);
            Require(!Reached(full),"A full barrier must stop the wave"); passed.Add("sealed defence blocks surge");
            var gap=Run(70,1.2f,beach.Sample,i=>i%64==29 && i/64!=WaveFlood.Grid(-1));
            Require(Reached(gap),"A one-cell opening must leak"); passed.Add("gap admits water");
            var shortWall=Run(70,1.2f,beach.Sample,i=>i%64==29 && i/64>23 && i/64<39);
            Require(Reached(shortWall),"Water must flow around the wall end"); passed.Add("wall ends can be bypassed");
            Func<float,float,float> bank=(x,z)=>Math.Abs(x+.75f)<.26f?1.5f:beach.Sample(x,z);
            Require(!Reached(Run(70,1.35f,bank,null)),"A raised bank must hold back a weaker wave");
            Require(Reached(Run(70,1.6f,bank,null)),"A stronger wave must overtop a low bank"); passed.Add("surge height governs sand banks");
            Require(!Reached(Run(35,1.2f,beach.Sample,null)),"Finite run-up must not flood the whole map"); passed.Add("finite run-up limits reach");
            var broken=Run(70,1.2f,beach.Sample,i=>false); Require(Reached(broken),"Removed barriers must admit water"); passed.Add("broken defence opens path");
            for(int i=0;i<before.Length;i++) Require(beach.heights[i]==before[i],"Surge must preserve the editable beach"); passed.Add("surge leaves terrain intact");
            open.Clear(); Require(open.WetCount==0 && !Reached(open),"Reset must clear transient inundation"); passed.Add("retreat clears transient water");
            return "PASS "+passed.Count+"/"+passed.Count+"\n- "+string.Join("\n- ",passed);
        }
    }
}
