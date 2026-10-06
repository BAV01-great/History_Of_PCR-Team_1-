using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    /// <summary>
    /// Procedural, VR-optimised DNA double helix: 2 backbones + 4 base colours = 6 draw calls and ~1-3k triangles
    /// for a hero helix, regardless of length. No textures, no downloaded assets.
    /// </summary>
    public class DnaHelix : MonoBehaviour
    {
        // Team colour key (History_of_PCR_3D_Asset_Handoff): A red, T blue, C green, G yellow.
        // Index order here is A, T, G, C so that index ^ 1 is always the complementary base (A-T, G-C).
        public static readonly Color[] BaseColors =
        {
            new Color(1.00f, 0.25f, 0.25f), // A red
            new Color(0.25f, 0.45f, 1.00f), // T blue
            new Color(1.00f, 0.85f, 0.20f), // G yellow
            new Color(0.25f, 0.90f, 0.40f), // C green
        };
        // Strand A dark grey, strand B light grey: they read against the dark navy scene and keep the base colours bold
        static readonly Color Backbone1 = new Color(0.42f, 0.46f, 0.52f);
        static readonly Color Backbone2 = new Color(0.80f, 0.84f, 0.90f);

        public float SpinDegPerSec = 18f;
        public float Height { get; private set; }

        readonly List<Material> mats = new List<Material>();
        readonly List<Color> baseEmission = new List<Color>();
        float glow = 1f;
        Transform spinRoot;

        public static DnaHelix Create(Transform parent, string name, Vector3 localPos, int pairs, float radius, float rise,
            float twistDeg = 36f, int seed = 1, float emission = 1.3f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var h = go.AddComponent<DnaHelix>();
            h.Build(pairs, radius, rise, twistDeg, seed, emission);
            return h;
        }

        /// <summary>
        /// Wraps one of the team's Blender models (DNA_Helix_Whole, DNA_Ladder_Whole, ...) so it gets the same spin and glow control
        /// as the procedural helix. The model is scaled so it is targetHeight metres tall (its import scale is not assumed) and its
        /// bottom centre is placed at the origin. Materials are rebuilt as URP Lit with the model's colours.
        /// </summary>
        public static DnaHelix WrapModel(Transform parent, string name, Vector3 localPos, GameObject prefab, float targetHeight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var h = go.AddComponent<DnaHelix>();
            h.spinRoot = new GameObject("Spin").transform;
            h.spinRoot.SetParent(go.transform, false);
            var model = Instantiate(prefab, h.spinRoot);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            var rs = model.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds();
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            float k = b.size.y > 1e-4f ? targetHeight / b.size.y : 1f;
            model.transform.localScale = Vector3.one * k;
            // bottom centre of the scaled model onto the wrapper's origin
            var nb = rs.Length > 0 ? rs[0].bounds : new Bounds();
            for (int i = 1; i < rs.Length; i++) nb.Encapsulate(rs[i].bounds);
            model.transform.position += go.transform.position - new Vector3(nb.center.x, nb.min.y, nb.center.z);
            h.Height = targetHeight;

            foreach (var r in rs)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    var m = src[i];
                    var col = Color.white;
                    if (m != null) col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
                    col.a = 1f;
                    var emis = col * 0.5f;
                    var nm = Mats.LitNew(col, emis, 0.55f, 0f);
                    var tex = m != null ? (m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture) : null;
                    if (tex != null) nm.SetTexture("_BaseMap", tex);
                    dst[i] = nm;
                    h.mats.Add(nm);
                    h.baseEmission.Add(emis);
                }
                r.sharedMaterials = dst;
            }
            return h;
        }

        void Build(int pairs, float radius, float rise, float twistDeg, int seed, float emission)
        {
            spinRoot = new GameObject("Spin").transform;
            spinRoot.SetParent(transform, false);
            Height = (pairs - 1) * rise;

            var b1 = new MeshBuilder();
            var b2 = new MeshBuilder();
            var rungs = new MeshBuilder[4];
            for (int i = 0; i < 4; i++) rungs[i] = new MeshBuilder();

            float bb = radius * 0.16f;
            float rr = radius * 0.075f;
            var rnd = new System.Random(seed);
            int sub = pairs > 30 ? 2 : 4;                        // tube rings per base pair (smooth curve)
            int sides = pairs > 30 ? 6 : 8;
            var path1 = new List<Vector3>(); var path2 = new List<Vector3>();

            for (int i = 0; i < pairs; i++)
            {
                float ang = i * twistDeg * Mathf.Deg2Rad;
                float y = i * rise;
                var p1 = new Vector3(Mathf.Cos(ang) * radius, y, Mathf.Sin(ang) * radius);
                var p2 = new Vector3(-p1.x, y, -p1.z);

                if (i == 0) { path1.Add(p1); path2.Add(p2); }
                else
                    for (int k = 1; k <= sub; k++)
                    {
                        float f = (i - 1 + k / (float)sub);
                        float a2 = f * twistDeg * Mathf.Deg2Rad, y2 = f * rise;
                        var q = new Vector3(Mathf.Cos(a2) * radius, y2, Mathf.Sin(a2) * radius);
                        path1.Add(q); path2.Add(new Vector3(-q.x, y2, -q.z));
                    }

                int b = rnd.Next(4);
                int comp = b ^ 1; // A<->T, G<->C
                var inner1 = Vector3.Lerp(p1, p2, 0.08f);
                var inner2 = Vector3.Lerp(p2, p1, 0.08f);
                var mid = (p1 + p2) * 0.5f;
                rungs[b].Prism(inner1, mid, rr, 6);
                rungs[comp].Prism(inner2, mid, rr, 6);
            }

            b1.Tube(path1, bb, sides); b2.Tube(path2, bb, sides);
            AddPart("Backbone A", b1.ToMesh("backbone1"), Backbone1, emission * 0.35f);
            AddPart("Backbone B", b2.ToMesh("backbone2"), Backbone2, emission * 0.35f);
            for (int i = 0; i < 4; i++)
            {
                if (rungs[i].VertexCount > 0) AddPart("Base " + "ATGC"[i], rungs[i].ToMesh("base" + i), BaseColors[i], emission * 0.5f);   // low glow keeps the four colours saturated
            }
        }

        void AddPart(string name, Mesh mesh, Color color, float emission)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(spinRoot, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mat = Mats.LitNew(color * 0.7f, color * emission, 0.55f, 0f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            mats.Add(mat);
            baseEmission.Add(color * emission);
        }

        /// <summary>1 = default brightness. Used for proximity glow / pulses.</summary>
        public void SetGlow(float g)
        {
            glow = g;
            for (int i = 0; i < mats.Count; i++) mats[i].SetColor("_EmissionColor", baseEmission[i] * g);
        }

        public float Glow => glow;

        void Update()
        {
            if (spinRoot != null) spinRoot.Rotate(0f, SpinDegPerSec * Time.deltaTime, 0f, Space.Self);
        }

        void OnDestroy()
        {
            foreach (var m in mats) if (m != null) Destroy(m);
        }
    }
}
