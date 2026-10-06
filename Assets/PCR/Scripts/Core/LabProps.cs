using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    /// <summary>
    /// Spawns MilkAndBanana's CC0 "Lab Assets" (Assets/Resources/LabAssets) with:
    ///  - a global scale calibrated at runtime so the counter model is 0.9 m high (works whatever the FBX import scale),
    ///  - bottom-centre placement regardless of the model's pivot,
    ///  - URP materials rebuilt from the model's texture atlas with a vivid tint, glass and emissive handled by material name.
    /// Returns null (and the scene simply skips the prop) if a model is missing.
    /// </summary>
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
                Object.Destroy(t);
                return scale;
            }
        }

        static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        /// <param name="name">File name without extension, e.g. "machine_microscope".</param>
        /// <param name="pos">World position of the prop's bottom centre.</param>
        /// <param name="tint">Multiplies the atlas colour. Use saturated colours (can exceed 1) for a vivid look. Null = white.</param>
        public static GameObject Spawn(string name, Transform parent, Vector3 pos, float yaw = 0f, Color? tint = null, float sizeMul = 1f, float fitLargest = 0f)
        {
            // A name containing '/' is a full Resources path (e.g. "PCRModels/PCR"); otherwise it's a Lab Assets file.
            var prefab = Resources.Load<GameObject>(name.Contains("/") ? name : "LabAssets/" + name);
            if (prefab == null) return null;

            var wrap = new GameObject(name);
            if (parent != null) wrap.transform.SetParent(parent, false);
            wrap.transform.position = Vector3.zero;
            wrap.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var model = Object.Instantiate(prefab, wrap.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            if (fitLargest > 0f)
            {
                // Fit the model's largest dimension to a size in metres (for the team's own Blender models, whose import scale we don't assume).
                model.transform.localScale = Vector3.one;
                var size = BoundsOf(model).size;
                float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                model.transform.localScale = Vector3.one * (largest > 1e-4f ? fitLargest / largest : 1f) * sizeMul;
            }
            else model.transform.localScale = Vector3.one * Scale * sizeMul;

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

            // Put the bottom centre of the model at the wrapper origin, then move the wrapper into place.
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
            // Flat-colour models (e.g. Kenney) carry their colour in the material, not a texture: keep it and multiply by the tint.
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
