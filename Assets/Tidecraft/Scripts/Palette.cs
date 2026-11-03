using UnityEngine;
namespace Tidecraft
{
    public static class Palette
    {
        public static Color Hex(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }
        public static readonly Color Sand = Hex("#E8CC91"), SandLight = Hex("#F9E4B1"), SandDark = Hex("#B99A62"), WetSand = Hex("#A3936E");
        public static readonly Color Sea = Hex("#218E9C"), Shallow = Hex("#70D3C2"), Deep = Hex("#15596F"), Foam = Hex("#D9F5E5");
        public static readonly Color Ink = Hex("#153D46"), Cream = Hex("#FFF3D5"), Coral = Hex("#DE7258"), Leaf = Hex("#668C69"), Rock = Hex("#7D8E86");
        public static readonly Color Sun = Hex("#FFF0CD"), Sky = Hex("#A5CDD0"), Ground = Hex("#675C48");
        public static Color Alpha(Color c, float a) { c.a = a; return c; }
    }
}
