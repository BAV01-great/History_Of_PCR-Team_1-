using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    public static class LabProps
    {
        const float CounterHeight = 0.9f;
        static float scale = -1f;
        static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();

        static float Scale
        {
            get
            {
                if (scale > 0f) return scale;
                scale = 1f;
                var p = Resources.Load<GameObject>("LabAssets/counter_counter");
                if (p == null) return scale;
                var t = Object.Instantiate(p, new Vector3(0, -1000f, 0), Quaternion.identity);
                var b = BoundsOf(t);
                if (b.size.y > 1e-4f) scale = CounterHeight / b.size.y;
                if (Application.isPlaying) Object.Destroy(t); else Object.DestroyImmediate(t);
                return scale;
            }
        }

        public static Bounds BoundsOf(GameObject go)
        {
            bool any = false;
            var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var w = m.MultiplyPoint3x4(corner);
                    if (!any) { b = new Bounds(w, Vector3.zero); any = true; } else b.Encapsulate(w);
                }
            }
            if (any) return b;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return b;
            b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        public static GameObject Spawn(string name, Transform parent, Vector3 pos, float yaw = 0f, Color? tint = null, float sizeMul = 1f, float fitLargest = 0f)
        {
            var prefab = Resources.Load<GameObject>(name.Contains("/") ? name : "LabAssets/" + name);
            if (prefab == null) return null;

            var wrap = new GameObject(name);
            if (parent != null) wrap.transform.SetParent(parent, false);
            wrap.transform.position = Vector3.zero;
            wrap.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var model = Object.Instantiate(prefab, wrap.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = prefab.transform.localRotation;
            if (fitLargest > 0f)
            {
                model.transform.localScale = Vector3.one;
                var size = BoundsOf(model).size;
                float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                model.transform.localScale = Vector3.one * (largest > 1e-4f ? fitLargest / largest : 1f) * sizeMul;
            }
            else model.transform.localScale = model.transform.localScale * (Scale * sizeMul);

            bool glassy = name.Contains("glassware") || name.Contains("vial");
            var t = tint ?? Color.white;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++) dst[i] = Convert(src[i], t, glassy);
                r.sharedMaterials = dst;
            }

            var b = BoundsOf(model);
            model.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            wrap.transform.position = pos;
            return wrap;
        }

        static Material Convert(Material src, Color tint, bool glassy)
        {
            string n = src != null ? src.name.ToLowerInvariant() : "solid";
            Texture tex = null;
            if (src != null) tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.mainTexture;
            bool emissive = n.Contains("emissive");
            bool glass = !emissive && (n.Contains("25%") || glassy);
            var src0 = Color.white;
            if (src != null) src0 = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : (src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white);
            src0.a = 1f;
            tint = new Color(tint.r * src0.r, tint.g * src0.g, tint.b * src0.b, tint.a);
            string key = $"{n}|{(tex != null ? tex.GetInstanceID() : 0)}|{tint}|{glass}";
            if (MatCache.TryGetValue(key, out var m) && m != null) return m;

            if (glass)
            {
                m = Mats.LitNew(new Color(tint.r, tint.g, tint.b, 0.38f), null, 0.95f, 0f);
                Mats.MakeTransparent(m, false);
            }
            else if (emissive) m = Mats.LitNew(tint, tint * 2.2f, 0.4f, 0f);
            else m = Mats.LitNew(tint, null, 0.55f, 0f);
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
            MatCache[key] = m;
            return m;
        }
    }
}
