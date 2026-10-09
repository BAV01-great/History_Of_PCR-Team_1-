using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
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
        Transform beacon;
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
            if (go.GetComponentInChildren<HoloButton>() == null)
            {
                var bb = TargetBounds();
                float ringDia = Mathf.Clamp(Mathf.Max(bb.size.x, bb.size.z) + 0.3f, 0.6f, 1.6f);
                beacon = new GameObject("HighlightBeacon").transform;
                Gen.Prim(PrimitiveType.Cylinder, "BeaconRing", beacon, new Vector3(0, 0.012f, 0), new Vector3(ringDia, 0.004f, ringDia), Mats.UnlitColor(new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f), true));
                Gen.Prim(PrimitiveType.Cylinder, "BeaconColumn", beacon, new Vector3(0, 1.2f, 0), new Vector3(ringDia * 0.7f, 1.2f, ringDia * 0.7f), Mats.UnlitColor(new Color(Cyan.r, Cyan.g, Cyan.b, 0.18f), true));
            }
            Place();
        }

        void Remove()
        {
            foreach (var h in hulls) if (h != null) Destroy(h);
            hulls.Clear();
            if (outline != null) { Destroy(outline); outline = null; }
            if (beacon != null) Destroy(beacon.gameObject);
            beacon = null; target = null;
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
            if (target == null || beacon == null) return;
            var b = TargetBounds();
            beacon.position = new Vector3(b.center.x, b.min.y, b.center.z);
        }

        void Update()
        {
            if (target == null) { if (beacon != null) Remove(); return; }
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
