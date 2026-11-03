using UnityEngine;
namespace Tidecraft
{
    public sealed class SandAudio : MonoBehaviour
    {
        public static float SfxGain = .55f, AmbientGain = .22f;
        AudioSource sfx, ambient; AudioClip click, stamp, scrape, flow;
        public bool Muted { get; private set; }
        public void Initialize()
        {
            sfx = gameObject.AddComponent<AudioSource>(); ambient = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = ambient.playOnAwake = false; ambient.loop = true;
            click = Tone("Tool click", .045f, 480, 300, .15f);
            stamp = Tone("Sand stamp", .19f, 180, 65, .48f);
            scrape = Tone("Soft sand scrape", .1f, 100, 70, .8f);
            flow = Tone("Water connected", .5f, 420, 760, .16f);
            ambient.clip = Tone("Soft shore", 8, 40, 40, 1); ambient.Play(); ApplyGain();
        }
        static AudioClip Tone(string name, float seconds, float from, float to, float noise)
        {
            const int rate = 22050; int n = (int)(seconds * rate); float[] data = new float[n]; var random = new System.Random(17); float phase = 0, low = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n; phase += Mathf.Lerp(from, to, t) * Mathf.PI * 2 / rate;
                low = .96f * low + .04f * ((float)random.NextDouble() * 2 - 1);
                float envelope = name == "Soft shore" ? (.5f + .5f * Mathf.Sin(t * Mathf.PI * 4)) : Mathf.Pow(1 - t, 2);
                envelope *= Mathf.Min(1, i / 110f) * Mathf.Min(1, (n - i - 1) / 110f);
                data[i] = (Mathf.Sin(phase) * (1 - noise) * .16f + low * noise * .8f) * envelope;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        public void ApplyGain() { if (!sfx) return; sfx.volume = Muted ? 0 : SfxGain; ambient.volume = Muted ? 0 : AmbientGain; }
        public void Toggle() { Muted = !Muted; ApplyGain(); }
        public void Click() => sfx.PlayOneShot(click);
        public void Stamp() => sfx.PlayOneShot(stamp);
        public void Scrape() => sfx.PlayOneShot(scrape, .5f);
        public void Flow() => sfx.PlayOneShot(flow);
        void OnDestroy() { if (click) Destroy(click); if (stamp) Destroy(stamp); if (scrape) Destroy(scrape); if (flow) Destroy(flow); if (ambient && ambient.clip) Destroy(ambient.clip); }
    }
}
