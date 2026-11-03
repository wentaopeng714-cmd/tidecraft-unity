using UnityEngine;
namespace Tidecraft
{
    public sealed class SandStamp : MonoBehaviour
    {
        float elapsed;
        Vector3 baseline;
        void Awake() { baseline = transform.localScale; }
        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / .24f), x = t - 1;
            float ease = 1 + 2.70158f * x * x * x + 1.70158f * x * x;
            transform.localScale = Vector3.Scale(baseline, new Vector3(Mathf.LerpUnclamped(1.12f, 1, ease), Mathf.LerpUnclamped(.5f, 1, ease), Mathf.LerpUnclamped(1.12f, 1, ease)));
            if (t >= 1) { transform.localScale = baseline; Destroy(this); }
        }
    }
}
