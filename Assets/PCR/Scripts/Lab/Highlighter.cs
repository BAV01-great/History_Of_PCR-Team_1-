using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    /// <summary>
    /// Marks the next thing to interact with: a pulsing outline (inverted-hull shader "PCR/Outline") around every mesh of the target,
    /// plus a bobbing diamond marker above it so it can be found across the room. One target at a time.
    /// </summary>
    public class Highlighter : MonoBehaviour
    {
        static Highlighter inst;
        static Highlighter Inst
        {
            get
            {
                if (inst == null) inst = new GameObject("Highlighter").AddComponent<Highlighter>();
                return inst;
            }
        }

        public static readonly Color Cyan = new Color(0.25f, 0.92f, 1f, 1f);

        public static void Show(GameObject target) => Inst.Apply(target);
        public static void Clear() { if (inst != null) inst.Remove(); }

        GameObject target;
        Material outline;
        readonly List<GameObject> hulls = new List<GameObject>();
        Transform marker;
        float t;

        void Apply(GameObject go)
        {
            Remove();
            if (go == null) return;
            target = go;
            var sh = Shader.Find("PCR/Outline");
            if (sh != null)
            {
                outline = new Material(sh);
                outline.SetColor("_Color", Cyan);
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    var r = mf.GetComponent<MeshRenderer>();
                    if (r == null || !r.enabled || mf.sharedMesh == null) continue;
                    var h = new GameObject("OutlineHull");
                    h.transform.SetParent(mf.transform, false);
                    h.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    var hr = h.AddComponent<MeshRenderer>();
                    hr.sharedMaterial = outline;
                    hr.shadowCastingMode = ShadowCastingMode.Off;
                    hr.receiveShadows = false;
                    hulls.Add(h);
                }
            }
            // floating diamond marker
            var m = new GameObject("HighlightMarker").transform;
            Gen.Prim(PrimitiveType.Sphere, "Diamond", m, Vector3.zero, new Vector3(0.07f, 0.12f, 0.07f), Mats.Lit(Cyan * 0.5f, Cyan * 2.5f, 0.3f));
            var halo = Gen.Prim(PrimitiveType.Quad, "Halo", m, Vector3.zero, Vector3.one * 0.45f, Mats.Glow(new Color(Cyan.r, Cyan.g, Cyan.b, 0.8f)));
            halo.AddComponent<BillboardY>();
            marker = m;
            Place();
        }

        void Remove()
        {
            foreach (var h in hulls) if (h != null) Destroy(h);
            hulls.Clear();
            if (outline != null) { Destroy(outline); outline = null; }
            if (marker != null) Destroy(marker.gameObject);
            marker = null; target = null;
        }

        Bounds TargetBounds()
        {
            var rs = target.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(target.transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in rs)
            {
                if (r.GetComponent<MeshFilter>() != null && r.transform.name == "OutlineHull") continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        void Place()
        {
            if (target == null || marker == null) return;
            var b = TargetBounds();
            marker.position = new Vector3(b.center.x, b.max.y + 0.22f + Mathf.Sin(t * 2.4f) * 0.04f, b.center.z);
            marker.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
        }

        void Update()
        {
            if (target == null) { if (marker != null) Remove(); return; }
            t += Time.deltaTime;
            if (outline != null)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(t * 3.2f);
                outline.SetColor("_Color", Color.Lerp(Cyan, Color.white, k * 0.6f));
                outline.SetFloat("_Width", Mathf.Lerp(0.008f, 0.015f, k));
            }
            Place();
        }
    }
}
