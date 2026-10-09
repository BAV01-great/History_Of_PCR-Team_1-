using UnityEngine;

namespace PCR
{
    public class HueCycle : MonoBehaviour
    {
        public float Offset, Speed = 0.05f, Alpha = 0.4f;
        Renderer r;
        MaterialPropertyBlock mpb;
        void Awake() { r = GetComponent<Renderer>(); mpb = new MaterialPropertyBlock(); }
        void Update()
        {
            if (r == null) r = GetComponent<Renderer>();
            if (r == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            var c = Color.HSVToRGB(Mathf.Repeat(Offset + Time.time * Speed, 1f), 0.8f, 1f);
            c.a = Alpha;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);
            r.SetPropertyBlock(mpb);
        }
    }

    public class PulseScale : MonoBehaviour
    {
        public float Delay;
        void Update()
        {
            float s = 0.75f + 0.25f * Mathf.Sin((Time.time - Delay) * 3.2f);
            transform.localScale = new Vector3(s, 1f, s);
        }
    }

    public class Bob : MonoBehaviour
    {
        public float Amplitude = 0.05f, Speed = 1.2f;
        Vector3 basePos;
        void Start() => basePos = transform.position;
        void Update() => transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * Speed) * Amplitude;
    }

    public class BillboardY : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var d = transform.position - cam.transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
