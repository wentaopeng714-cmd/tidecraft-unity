using System.Runtime.InteropServices;
namespace Tidecraft
{
    // DOM-visible state lets the browser host distinguish a loaded engine from a ready game.
    public static class WebBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void Tidecraft_ReportState(int walls, int towers, int connected, int ready);
        [DllImport("__Internal")] static extern void Tidecraft_ReportWaves(string phase, int wave, int health, int hits, int broken, int flooded, int paused);
#endif
        public static void Report(int walls, int towers, bool connected, bool ready)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Tidecraft_ReportState(walls, towers, connected ? 1 : 0, ready ? 1 : 0);
#endif
        }
        public static void ReportWaves(string phase, int wave, int health, int hits, int broken, int flooded, bool paused)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Tidecraft_ReportWaves(phase, wave, health, hits, broken, flooded, paused ? 1 : 0);
#endif
        }
    }
}
