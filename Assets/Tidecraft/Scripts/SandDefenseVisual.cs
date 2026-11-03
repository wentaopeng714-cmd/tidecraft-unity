using UnityEngine;
namespace Tidecraft
{
    // Shared sand material is preserved. Damaged buildings darken, flash on impact,
    // and show a small world-space durability bar.
    public sealed class SandDefenseVisual : MonoBehaviour
    {
        WallRecord wall; TowerRecord tower; Renderer body; MaterialPropertyBlock tint;
        LineRenderer bar, back; float lastHealth = 100, flash;
        Vector3 anchor; Camera view;
        public void Initialize(WallRecord w, TowerRecord t, Material foam, Camera camera)
        {
            wall = w; tower = t; view = camera; body = GetComponent<Renderer>(); tint = new MaterialPropertyBlock();
            anchor = new Vector3(body.bounds.center.x, body.bounds.max.y + .32f, body.bounds.center.z);
            back = MakeBar("Durability background", foam, .16f, Palette.Ink);
            bar = MakeBar("Durability", foam, .10f, Palette.Shallow);
            lastHealth = Health; Apply();
        }
        float Health => wall != null ? wall.health : tower.health;
        LineRenderer MakeBar(string label, Material material, float width, Color color)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.positionCount = 2;
            line.useWorldSpace = true; line.widthMultiplier = width; line.startColor = line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return line;
        }
        void Update()
        {
            if (Health < lastHealth) flash = .32f;
            lastHealth = Health; flash = Mathf.Max(0, flash - Time.deltaTime); Apply();
        }
        void Apply()
        {
            float fraction = Mathf.Clamp01(Health / 100);
            Color weathered = Color.Lerp(Color.white, Palette.WetSand, (1 - fraction) * .5f);
            tint.SetColor("_BaseColor", Color.Lerp(weathered, Palette.Coral, Mathf.Clamp01(flash * 4)));
            body.SetPropertyBlock(tint);
            bar.enabled = back.enabled = fraction < .995f;
            if (!bar.enabled) return;
            Vector3 right = view ? view.transform.right : Vector3.right;
            Vector3 from = anchor - right * .85f;
            back.SetPosition(0, from); back.SetPosition(1, anchor + right * .85f);
            bar.SetPosition(0, from + Vector3.up * .018f); bar.SetPosition(1, from + right * (1.7f * fraction) + Vector3.up * .018f);
            bar.startColor = bar.endColor = fraction > .45f ? Palette.Shallow : Palette.Coral;
        }
    }
}
