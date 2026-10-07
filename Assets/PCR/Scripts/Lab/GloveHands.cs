using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace PCR
{
    public enum HandPose { Relaxed, Open, Point, Pinch, Grab, PipetteGrip }

    public class GloveHands : MonoBehaviour
    {
        public static GloveHands Instance { get; private set; }

        class Hand
        {
            public bool Right;
            public Transform Root, HoldPoint, ThumbBase;
            public Transform[][] Fingers = new Transform[5][];
            public readonly float[] Curl = new float[5];
            public readonly float[] Target = new float[5];
            public readonly Quaternion[][] BoneRest = new Quaternion[5][];
            public readonly Vector3[] CurlAxis = { Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.right };
            public float ThumbPress;
            public Vector3 RestPos; public Quaternion RestRot;
            public bool Busy;
            public Transform Held;
            public GameObject Sleeve, GloveCuff;
        }

        static readonly float[][] Poses =
        {
            new[] { 0.28f, 0.16f, 0.26f, 0.37f, 0.48f },
            new[] { 0.02f, 0.02f, 0.00f, 0.04f, 0.06f },
            new[] { 0.45f, 0.02f, 0.88f, 0.94f, 0.97f },
            new[] { 0.55f, 0.52f, 0.30f, 0.26f, 0.30f },
            new[] { 0.62f, 0.70f, 0.78f, 0.82f, 0.86f },
            new[] { 0.30f, 0.60f, 0.72f, 0.78f, 0.84f },
        };
        static readonly float[][] SegLen =
        {
            new[] { 0.036f, 0.027f, 0.000f },
            new[] { 0.040f, 0.024f, 0.020f },
            new[] { 0.044f, 0.027f, 0.021f },
            new[] { 0.041f, 0.025f, 0.020f },
            new[] { 0.033f, 0.019f, 0.017f },
        };
        static readonly float[] FingerX = { -0.031f, -0.0105f, 0.010f, 0.0285f };
        static readonly float[] FingerZ = { 0.100f, 0.103f, 0.100f, 0.092f };
        static readonly float[] FingerSplay = { -3f, 0f, 3f, 7f };
        static readonly float[] MaxAngle = { 80f, 95f, 65f };

        Hand left, right;
        Hand[] hands;
        Transform rig;
        bool attached, xr, gloved = true, sleeves;
        readonly System.Collections.Generic.List<Renderer> skinParts = new System.Collections.Generic.List<Renderer>();
        Material skin, glove, coat;

        public bool Gloved => gloved;
        public bool XrMode => xr;

        void Awake()
        {
            Instance = this;
            skin = Mats.LitNew(new Color(0.80f, 0.60f, 0.49f), null, 0.30f);
            glove = Mats.LitNew(new Color(0.18f, 0.58f, 1.0f), new Color(0.02f, 0.07f, 0.16f), 0.38f);
            coat = Mats.LitNew(new Color(0.95f, 0.96f, 0.98f), null, 0.2f);
            rig = new GameObject("GloveHands").transform;
            right = BuildHand(true);
            left = BuildHand(false);
            hands = new[] { right, left };
            ApplyMaterials();
            right.Root.gameObject.SetActive(false); left.Root.gameObject.SetActive(false);
        }

        static readonly System.Collections.Generic.Dictionary<string, Mesh> meshCache = new System.Collections.Generic.Dictionary<string, Mesh>();

        static Mesh Tapered(float r0, float r1, float len)
        {
            string key = $"{r0:F4}|{r1:F4}|{len:F4}";
            if (meshCache.TryGetValue(key, out var cached)) return cached;
            const int sides = 12, caps = 4;
            var verts = new System.Collections.Generic.List<Vector3>();
            var rings = new System.Collections.Generic.List<(float rad, float z)>();
            for (int k = 0; k <= caps; k++) { float phi = -Mathf.PI / 2f + Mathf.PI / 2f * k / caps; rings.Add((r0 * Mathf.Cos(phi), r0 * Mathf.Sin(phi))); }
            for (int k = 0; k <= caps; k++) { float phi = Mathf.PI / 2f * k / caps; rings.Add((r1 * Mathf.Cos(phi), len + r1 * Mathf.Sin(phi))); }
            foreach (var (rad, z) in rings)
                for (int j = 0; j <= sides; j++) { float a = 2f * Mathf.PI * j / sides; verts.Add(new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, z)); }
            var tris = new System.Collections.Generic.List<int>();
            for (int r = 0; r < rings.Count - 1; r++)
                for (int j = 0; j < sides; j++)
                {
                    int i0 = r * (sides + 1) + j, i1 = i0 + 1, i2 = i0 + sides + 1, i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i2); tris.Add(i1); tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            var m = new Mesh { name = "TaperedCapsule" };
            m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            meshCache[key] = m;
            return m;
        }

        Hand BuildHand(bool isRight)
        {
            var h = new Hand { Right = isRight };
            float m = isRight ? 1f : -1f;
            h.Root = new GameObject(isRight ? "RightHand" : "LeftHand").transform;
            h.Root.SetParent(rig, false);
            h.RestPos = new Vector3(0.15f * m, -0.13f, 0.40f);
            h.RestRot = Quaternion.Euler(-12f, -12f * m, 10f * m);
            h.Root.localPosition = h.RestPos; h.Root.localRotation = h.RestRot;

            var model = Resources.Load<GameObject>("PCRModels/Hands/" + (isRight ? "RightHand" : "LeftHand"));
            if (model != null && BuildFromModel(h, model)) { FinishHand(h); return h; }

            Part(h, PrimitiveType.Sphere, "Back", h.Root, new Vector3(0, 0.004f, 0.055f), new Vector3(0.084f, 0.030f, 0.098f));
            Part(h, PrimitiveType.Sphere, "Heel", h.Root, new Vector3(-0.004f * m, -0.006f, 0.022f), new Vector3(0.076f, 0.034f, 0.062f));
            Part(h, PrimitiveType.Sphere, "Ridge", h.Root, new Vector3(0, 0.002f, 0.094f), new Vector3(0.082f, 0.026f, 0.030f));
            MeshPart(h, "Wrist", h.Root, new Vector3(0, 0, -0.07f), Quaternion.identity, Tapered(0.030f, 0.037f, 0.07f)).localScale = new Vector3(1.15f, 0.82f, 1f);
            h.HoldPoint = new GameObject("HoldPoint").transform;
            h.HoldPoint.SetParent(h.Root, false);
            h.HoldPoint.localPosition = new Vector3(0, -0.012f, 0.09f);

            for (int f = 1; f <= 4; f++)
            {
                float r = f == 4 ? 0.0092f : f == 3 ? 0.0100f : 0.0105f;
                h.Fingers[f] = Finger(h, h.Root, new Vector3(FingerX[f - 1] * m, 0, FingerZ[f - 1]), Quaternion.Euler(0, FingerSplay[f - 1] * m, 0), SegLen[f], r, 3);
            }
            h.ThumbBase = new GameObject("ThumbBase").transform;
            h.ThumbBase.SetParent(h.Root, false);
            h.ThumbBase.localPosition = new Vector3(-0.036f * m, -0.004f, 0.034f);
            h.ThumbBase.localRotation = Quaternion.Euler(6f, -34f * m, -22f * m);
            Part(h, PrimitiveType.Sphere, "ThumbMound", h.Root, new Vector3(-0.030f * m, -0.006f, 0.040f), new Vector3(0.046f, 0.034f, 0.058f));
            Part(h, PrimitiveType.Sphere, "Web", h.Root, new Vector3(-0.026f * m, -0.002f, 0.072f), new Vector3(0.030f, 0.014f, 0.040f));
            h.Fingers[0] = Finger(h, h.ThumbBase, Vector3.zero, Quaternion.identity, SegLen[0], 0.0125f, 2);

            h.GloveCuff = MeshPart(h, "Cuff", h.Root, new Vector3(0, 0, -0.075f), Quaternion.identity, Tapered(0.040f, 0.042f, 0.035f)).gameObject;
            h.GloveCuff.transform.localScale = new Vector3(1.12f, 0.88f, 1f);
            var sl = Part(h, PrimitiveType.Cylinder, "Sleeve", h.Root, new Vector3(0, 0.002f, -0.17f), new Vector3(0.115f, 0.12f, 0.115f), Quaternion.Euler(90, 0, 0));
            skinParts.Remove(sl.GetComponent<Renderer>());
            sl.GetComponent<Renderer>().sharedMaterial = coat;
            h.Sleeve = sl.gameObject; h.Sleeve.SetActive(false);
            FinishHand(h);
            return h;
        }

        static void FinishHand(Hand h)
        {
            for (int f = 0; f < 5; f++)
            {
                if (h.Fingers[f] == null || h.BoneRest[f] != null) continue;
                h.BoneRest[f] = new Quaternion[h.Fingers[f].Length];
                for (int j = 0; j < h.Fingers[f].Length; j++) h.BoneRest[f][j] = h.Fingers[f][j].localRotation;
            }
        }

        bool BuildFromModel(Hand h, GameObject prefab)
        {
            string px = h.Right ? "R_" : "L_";
            var model = Instantiate(prefab, h.Root);
            model.name = "HandModel";
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.one;
            var all = model.GetComponentsInChildren<Transform>(true);
            Transform Find(string n) { foreach (var t in all) if (t.name == px + n) return t; return null; }
            var palm = Find("Palm"); var wrist = Find("Wrist");
            string[] names = { "Thumb", "Index", "Middle", "Ring", "Little" };
            var chains = new Transform[5][]; var tips = new Transform[5];
            bool ok = palm != null && wrist != null;
            for (int f = 0; f < 5 && ok; f++)
            {
                string[] joints = f == 0 ? new[] { "Metacarpal", "Proximal", "Distal" } : new[] { "Proximal", "Intermediate", "Distal" };
                chains[f] = new Transform[3];
                for (int j = 0; j < 3; j++) { chains[f][j] = Find(names[f] + joints[j]); if (chains[f][j] == null) ok = false; }
                tips[f] = Find(names[f] + "Tip");
                if (tips[f] == null) ok = false;
            }
            if (!ok) { Destroy(model); Debug.LogWarning("[PCR] Hand model bones not found; using the procedural hand."); return false; }

            var fingerDir = h.Root.InverseTransformDirection(tips[2].position - wrist.position).normalized;
            var axis = DetectCurlAxis(chains[2], tips[2], palm);
            var restRot = chains[2][0].localRotation;
            var before = tips[2].position;
            chains[2][0].localRotation = restRot * Quaternion.AngleAxis(35f, axis);
            var disp = h.Root.InverseTransformDirection(tips[2].position - before);
            chains[2][0].localRotation = restRot;
            var palmDir = (disp - fingerDir * Vector3.Dot(disp, fingerDir)).normalized;
            Debug.Log($"[PCR] {h.Root.name}: fingerDir={fingerDir} curlAxis={axis} palmDir={palmDir} skinned={model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length} bones={all.Length}");
            if (palmDir.sqrMagnitude > 0.5f)
                model.transform.localRotation = Quaternion.Inverse(Quaternion.LookRotation(fingerDir, -palmDir));

            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.receiveShadows = false;
                smr.updateWhenOffscreen = true;
                skinParts.Add(smr);
            }
            for (int f = 0; f < 5; f++)
            {
                h.Fingers[f] = chains[f];
                h.BoneRest[f] = new[] { chains[f][0].localRotation, chains[f][1].localRotation, chains[f][2].localRotation };
                h.CurlAxis[f] = DetectCurlAxis(chains[f], tips[f], palm);
            }
            h.HoldPoint = new GameObject("HoldPoint").transform;
            h.HoldPoint.SetParent(h.Root, false);
            h.HoldPoint.localPosition = h.Root.InverseTransformPoint(palm.position) + new Vector3(0, -0.015f, 0.05f);

            var wl = h.Root.InverseTransformPoint(wrist.position);
            h.GloveCuff = MeshPart(h, "Cuff", h.Root, wl + new Vector3(0, 0, -0.02f), Quaternion.identity, Tapered(0.034f, 0.036f, 0.04f)).gameObject;
            h.GloveCuff.transform.localScale = new Vector3(1.15f, 0.9f, 1f);
            var sl = Part(h, PrimitiveType.Cylinder, "Sleeve", h.Root, wl + new Vector3(0, 0, -0.14f), new Vector3(0.105f, 0.12f, 0.105f), Quaternion.Euler(90, 0, 0));
            skinParts.Remove(sl.GetComponent<Renderer>());
            sl.GetComponent<Renderer>().sharedMaterial = coat;
            h.Sleeve = sl.gameObject; h.Sleeve.SetActive(false);
            return true;
        }

        static Vector3 DetectCurlAxis(Transform[] chain, Transform tip, Transform palm)
        {
            var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
            var rest = chain[0].localRotation;
            float best = float.MaxValue; var bestAxis = Vector3.right;
            foreach (var ax in axes)
                foreach (float sign in new[] { 1f, -1f })
                {
                    chain[0].localRotation = rest * Quaternion.AngleAxis(sign * 35f, ax);
                    float d = Vector3.Distance(tip.position, palm.position);
                    if (d < best) { best = d; bestAxis = ax * sign; }
                }
            chain[0].localRotation = rest;
            return bestAxis;
        }

        Transform Part(Hand h, PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            skinParts.Add(r);
            return go.transform;
        }

        Transform MeshPart(Hand h, string name, Transform parent, Vector3 pos, Quaternion rot, Mesh mesh)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localRotation = rot;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            skinParts.Add(r);
            return go.transform;
        }

        Transform[] Finger(Hand h, Transform parent, Vector3 pos, Quaternion rot, float[] lens, float radius, int joints)
        {
            var js = new Transform[joints];
            Transform p = parent;
            float[] taper = joints == 3 ? new[] { 1f, 0.9f, 0.82f, 0.68f } : new[] { 1f, 0.88f, 0.72f };
            for (int i = 0; i < joints; i++)
            {
                var j = new GameObject("J" + i).transform;
                j.SetParent(p, false);
                j.localPosition = i == 0 ? pos : new Vector3(0, 0, lens[i - 1]);
                j.localRotation = i == 0 ? rot : Quaternion.identity;
                MeshPart(h, "Seg", j, Vector3.zero, Quaternion.identity, Tapered(radius * taper[i], radius * taper[i + 1], lens[i]));
                js[i] = j;
                p = j;
            }
            return js;
        }

        void ApplyMaterials()
        {
            foreach (var r in skinParts)
            {
                if (r == null) continue;
                if (r.sharedMaterial == coat) continue;
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = gloved ? glove : skin;
                r.sharedMaterials = mats;
            }
        }

        public void Show(bool visible) { right.Root.gameObject.SetActive(visible); left.Root.gameObject.SetActive(visible); }
        public void SetGloved(bool on) { gloved = on; ApplyMaterials(); if (on) { right.GloveCuff.SetActive(true); left.GloveCuff.SetActive(true); } }
        public void SetSleeves(bool on) { sleeves = on; right.Sleeve.SetActive(on); left.Sleeve.SetActive(on); }

        Hand H(bool isRight) => isRight ? right : left;

        public void SetPose(bool isRight, HandPose pose)
        {
            var t = Poses[(int)pose];
            var h = H(isRight);
            for (int i = 0; i < 5; i++) h.Target[i] = t[i];
        }

        public void SetThumbPress(bool isRight, float amount) { H(isRight).ThumbPress = Mathf.Clamp01(amount); }

        public void ShiftRest(Vector3 d) { right.RestPos += new Vector3(-d.x, d.y, d.z); left.RestPos += new Vector3(d.x, d.y, d.z); }

        public Vector3 HoldWorldPosition(bool isRight) => H(isRight).HoldPoint.position;

        public IEnumerator ReachTo(bool isRight, Vector3 worldPos, float seconds = 0.45f)
        {
            var h = H(isRight);
            h.Busy = true;
            if (xr) { yield return new WaitForSeconds(seconds); yield break; }
            var from = h.Root.position; var fromRot = h.Root.rotation;
            var dir = worldPos - from; dir.y *= 0.4f;
            var toRot = dir.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(8f, 0, 0) : fromRot;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float e = Mathf.SmoothStep(0, 1, t / seconds);
                h.Root.position = Vector3.Lerp(from, worldPos, e);
                h.Root.rotation = Quaternion.Slerp(fromRot, toRot, e);
                yield return null;
            }
            h.Root.position = worldPos; h.Root.rotation = toRot;
        }

        public IEnumerator CarryTo(bool isRight, Vector3 holdWorldPos, float seconds = 0.6f, Quaternion? rot = null)
        {
            var h = H(isRight);
            if (xr) { yield return new WaitForSeconds(seconds); yield break; }
            var from = h.Root.position; var fromRot = h.Root.rotation;
            var offset = h.Root.position - h.HoldPoint.position;
            var to = holdWorldPos + offset;
            var toRot = rot ?? fromRot;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float e = Mathf.SmoothStep(0, 1, t / seconds);
                h.Root.position = Vector3.Lerp(from, to, e);
                h.Root.rotation = Quaternion.Slerp(fromRot, toRot, e);
                yield return null;
            }
            h.Root.position = to; h.Root.rotation = toRot;
        }

        public IEnumerator ReturnToRest(bool isRight, float seconds = 0.5f)
        {
            var h = H(isRight);
            if (xr) { h.Busy = false; yield break; }
            var from = h.Root.position; var fromRot = h.Root.rotation;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float e = Mathf.SmoothStep(0, 1, t / seconds);
                var to = h.Root.parent.TransformPoint(h.RestPos); var toRot = h.Root.parent.rotation * h.RestRot;
                h.Root.position = Vector3.Lerp(from, to, e);
                h.Root.rotation = Quaternion.Slerp(fromRot, toRot, e);
                yield return null;
            }
            h.Root.localPosition = h.RestPos; h.Root.localRotation = h.RestRot;
            h.Busy = false;
            SetPose(isRight, HandPose.Relaxed);
        }

        public void Hold(bool isRight, Transform item, Vector3 localPos, Vector3 localEuler)
        {
            var h = H(isRight);
            h.Held = item;
            item.SetParent(h.HoldPoint, true);
            item.localPosition = localPos;
            item.localRotation = Quaternion.Euler(localEuler);
        }

        public Transform Release(bool isRight)
        {
            var h = H(isRight);
            var it = h.Held;
            if (it != null) it.SetParent(null, true);
            h.Held = null;
            return it;
        }

        public Transform Held(bool isRight) => H(isRight).Held;

        public Vector3 IndexTip(bool isRight)
        {
            var j = H(isRight).Fingers[1];
            return j[2].TransformPoint(new Vector3(0, 0, SegLen[1][2]));
        }

        void Attach()
        {
            var cam = Camera.main;
            if (cam == null) return;
            xr = XRSettings.isDeviceActive;
            Transform lc = null, rc = null;
            var top = cam.transform.root;
            foreach (var t in top.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Left Controller" || t.name == "LeftHand Controller") lc = t;
                else if (t.name == "Right Controller" || t.name == "RightHand Controller") rc = t;
            }
            if (lc != null && rc != null)
            {
                left.Root.SetParent(lc, false); right.Root.SetParent(rc, false);
                left.RestPos = Vector3.zero; right.RestPos = Vector3.zero;
                left.RestRot = Quaternion.identity; right.RestRot = Quaternion.identity;
                left.Root.localPosition = Vector3.zero; right.Root.localPosition = Vector3.zero;
                left.Root.localRotation = Quaternion.identity; right.Root.localRotation = Quaternion.identity;
                xr = true;
            }
            else
            {
                rig.SetParent(cam.transform, false);
                rig.localPosition = Vector3.zero; rig.localRotation = Quaternion.identity;
                xr = false;
            }
            var lamp = new GameObject("HandLight").AddComponent<Light>();
            lamp.transform.SetParent(rig, false);
            lamp.transform.localPosition = new Vector3(0, 0.25f, -0.15f);
            lamp.type = LightType.Point; lamp.color = new Color(0.75f, 0.88f, 1f); lamp.range = 1.6f; lamp.intensity = 0.22f; lamp.shadows = LightShadows.None;
            Show(!xr);
            SetPose(true, HandPose.Relaxed); SetPose(false, HandPose.Relaxed);
            attached = true;
        }

        void LateUpdate()
        {
            if (!attached) { Attach(); if (!attached) return; }
            if (hands == null) return;
            float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
            float bob = Mathf.Sin(Time.time * 1.4f);
            foreach (var h in hands)
            {
                for (int f = 0; f < 5; f++)
                {
                    float drift = (Mathf.PerlinNoise(Time.time * 0.45f + f * 3.7f, h.Right ? 0.3f : 9.1f) - 0.5f) * 0.10f;
                    h.Curl[f] = Mathf.Lerp(h.Curl[f], Mathf.Clamp01(h.Target[f] + drift * (1f - h.Target[f] * 0.6f)), k);
                    float c = h.Curl[f] + (f == 0 ? h.ThumbPress * 0.5f : 0f);
                    var js = h.Fingers[f];
                    for (int j = 0; j < js.Length; j++)
                    {
                        float a = c * MaxAngle[j] * (f == 0 && j == 0 ? 0.7f : 1f);
                        js[j].localRotation = h.BoneRest[f][j] * Quaternion.AngleAxis(a, h.CurlAxis[f]);
                    }
                }
                if (!h.Busy && !xr)
                {
                    float m = h.Right ? 1f : -1f;
                    h.Root.localPosition = h.RestPos + new Vector3(0, bob * 0.004f, bob * 0.003f * m);
                }
            }
        }
    }
}
