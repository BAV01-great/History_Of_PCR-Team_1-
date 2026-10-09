using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    public class MorphHelix : MonoBehaviour
    {
        int pairs;
        float radius, rise, beadR, rungR;
        Transform[] a, b, ra, rb, na, nb, sa, sb;
        int[] baseIdx;
        bool dirty = true;
        float twist = 36f, separation, rungScale = 1f, newProgress, newOffset = 0.05f;

        public int Pairs => pairs;
        public float Height => (pairs - 1) * rise;

        static Material backA, backB;

        static Material BaseMat(int i, float glow) => Mats.Lit(DnaHelix.BaseColors[i] * 0.7f, DnaHelix.BaseColors[i] * glow, 0.5f);

        public static MorphHelix Create(Transform parent, string name, Vector3 localPos, int pairs = 24, float radius = 0.22f, float rise = 0.07f, int seed = 3, float glow = 0.9f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var h = go.AddComponent<MorphHelix>();
            h.Build(pairs, radius, rise, seed, glow);
            return h;
        }

        void Build(int n, float r, float rs, int seed, float glow)
        {
            pairs = n; radius = r; rise = rs; beadR = r * 0.1f; rungR = r * 0.055f;
            backA = backA != null ? backA : Mats.Lit(new Color(0.34f, 0.38f, 0.45f), new Color(0.34f, 0.38f, 0.45f) * 0.3f, 0.55f);
            backB = backB != null ? backB : Mats.Lit(new Color(0.78f, 0.82f, 0.90f), new Color(0.78f, 0.82f, 0.90f) * 0.3f, 0.55f);
            a = new Transform[n]; b = new Transform[n]; ra = new Transform[n]; rb = new Transform[n]; na = new Transform[n]; nb = new Transform[n];
            sa = new Transform[n - 1]; sb = new Transform[n - 1];
            baseIdx = new int[n];
            var rnd = new System.Random(seed);
            for (int i = 0; i < n; i++)
            {
                int bi = rnd.Next(4); baseIdx[i] = bi; int comp = bi ^ 1;
                a[i] = Gen.Prim(PrimitiveType.Sphere, "A", transform, Vector3.zero, Vector3.one * beadR * 2f, backA).transform;
                b[i] = Gen.Prim(PrimitiveType.Sphere, "B", transform, Vector3.zero, Vector3.one * beadR * 2f, backB).transform;
                ra[i] = Gen.Prim(PrimitiveType.Cylinder, "RungA", transform, Vector3.zero, Vector3.one * rungR, BaseMat(bi, glow * 1.2f)).transform;
                rb[i] = Gen.Prim(PrimitiveType.Cylinder, "RungB", transform, Vector3.zero, Vector3.one * rungR, BaseMat(comp, glow * 1.2f)).transform;
                if (i < n - 1)
                {
                    sa[i] = Gen.Prim(PrimitiveType.Cylinder, "BackboneA", transform, Vector3.zero, Vector3.one * beadR, backA).transform;
                    sb[i] = Gen.Prim(PrimitiveType.Cylinder, "BackboneB", transform, Vector3.zero, Vector3.one * beadR, backB).transform;
                }
                na[i] = Gen.Prim(PrimitiveType.Sphere, "NewA", transform, Vector3.zero, Vector3.one * beadR * 1.6f, BaseMat(comp, glow * 1.6f)).transform;
                nb[i] = Gen.Prim(PrimitiveType.Sphere, "NewB", transform, Vector3.zero, Vector3.one * beadR * 1.6f, BaseMat(bi, glow * 1.6f)).transform;
            }
            Refresh();
        }

        public void Set(float twistDeg, float sep, float rungs, float newBuilt)
        {
            twist = twistDeg; separation = sep; rungScale = rungs; newProgress = newBuilt; dirty = true;
        }
        public void SetTwist(float t) { twist = t; dirty = true; }
        public void SetSeparation(float s, float rungs) { separation = s; rungScale = rungs; dirty = true; }
        public void SetNewProgress(float p) { newProgress = p; dirty = true; }

        public Vector3 BeadA(int i) => a[Mathf.Clamp(i, 0, pairs - 1)].position;
        public Vector3 BeadB(int i) => b[Mathf.Clamp(i, 0, pairs - 1)].position;

        public void Highlight(int from, int to, Material mat)
        {
            for (int i = from; i <= to && i < pairs; i++)
            {
                a[i].GetComponent<Renderer>().sharedMaterial = mat != null ? mat : backA;
                b[i].GetComponent<Renderer>().sharedMaterial = mat != null ? mat : backB;
            }
        }

        void LateUpdate() { if (dirty) Refresh(); }

        void Refresh()
        {
            dirty = false;
            for (int i = 0; i < pairs; i++)
            {
                float ang = i * twist * Mathf.Deg2Rad, y = i * rise;
                var p1 = new Vector3(Mathf.Cos(ang) * radius - separation, y, Mathf.Sin(ang) * radius);
                var p2 = new Vector3(-Mathf.Cos(ang) * radius + separation, y, -Mathf.Sin(ang) * radius);
                a[i].localPosition = p1; b[i].localPosition = p2;
                var mid = (p1 + p2) * 0.5f;
                Rung(ra[i], p1, mid, rungScale);
                Rung(rb[i], p2, mid, rungScale);
                if (i > 0) { Segment(sa[i - 1], a[i - 1].localPosition, p1); Segment(sb[i - 1], b[i - 1].localPosition, p2); }
                float shown = newProgress * pairs;
                bool on = i < shown;
                Vector3 inward = (p2 - p1).normalized;
                na[i].gameObject.SetActive(on);
                nb[i].gameObject.SetActive(on);
                if (on)
                {
                    float pop = Mathf.Clamp01(shown - i);
                    na[i].localPosition = p1 + inward * newOffset; nb[i].localPosition = p2 - inward * newOffset;
                    na[i].localScale = nb[i].localScale = Vector3.one * beadR * 1.6f * pop;
                }
            }
        }

        void Segment(Transform t, Vector3 from, Vector3 to)
        {
            var d = to - from;
            t.localPosition = from + d * 0.5f;
            t.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            t.localScale = new Vector3(beadR * 1.1f, d.magnitude * 0.5f, beadR * 1.1f);
        }

        void Rung(Transform t, Vector3 from, Vector3 to, float scale)
        {
            var d = to - from;
            float len = d.magnitude * scale;
            if (len < 0.004f) { t.gameObject.SetActive(false); return; }
            t.gameObject.SetActive(true);
            t.localPosition = from + d * (0.5f * scale);
            t.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            t.localScale = new Vector3(rungR * 2f, len * 0.5f, rungR * 2f);
        }
    }
}
