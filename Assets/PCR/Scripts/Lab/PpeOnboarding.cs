using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    /// <summary>
    /// Opening step: a lab coat on a hook by the door. The "wear" step reaches for it with the gloved hands, brings it onto the player and
    /// puts white coat sleeves on the hands. Until it is on, the step system accepts nothing else. (Gloves are on from the start; the script
    /// has no goggles or glove step, so there are none.)
    /// </summary>
    public class PpeOnboarding : MonoBehaviour
    {
        public const string CoatId = "lab_coat";

        public void Build(Transform parent, Vector3 hookPos)
        {
            var root = new GameObject("Gowning").transform;
            root.SetParent(parent, false);
            Gen.Box("HookRail", root, hookPos + new Vector3(0, 1.95f, 0.1f), new Vector3(0.7f, 0.04f, 0.04f), Mats.Lit(Theme.NavyMid, null, 0.4f));
            var coat = BuildCoat(root, hookPos);
            LabInteractable.Make(coat, CoatId, "Lab coat");
        }

        /// <summary>A full white lab coat on a hanger: a smooth lofted body with a flared hem and soft folds, hanging sleeves with cuffs, a collar, lapels, buttons and pockets.</summary>
        static GameObject BuildCoat(Transform parent, Vector3 hook)
        {
            var coat = new GameObject("LabCoat").transform;
            coat.SetParent(parent, false);
            coat.position = hook;
            var white = Mats.LitNew(new Color(0.93f, 0.94f, 0.97f), new Color(0.05f, 0.06f, 0.09f), 0.22f);
            var shade = Mats.LitNew(new Color(0.80f, 0.83f, 0.90f), new Color(0.04f, 0.05f, 0.07f), 0.22f);
            var metal = Mats.LitNew(new Color(0.55f, 0.58f, 0.62f), null, 0.7f, 0.8f);
            var button = Mats.LitNew(new Color(0.55f, 0.6f, 0.7f), null, 0.6f);
            const float Z = 0.1f;                                   // the coat hangs 10 cm in front of the wall rail

            void Add(string name, MeshBuilder mb, Material m)
            {
                var g = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                g.transform.SetParent(coat, false);
                g.GetComponent<MeshFilter>().sharedMesh = mb.ToMesh(name);
                var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            }

            // body: rings of (height, half width, half depth), a superellipse cross-section, with folds that deepen towards the hem
            float[][] prof =
            {
                new[] { 1.700f, 0.075f, 0.060f }, new[] { 1.665f, 0.150f, 0.072f }, new[] { 1.620f, 0.235f, 0.085f }, new[] { 1.560f, 0.262f, 0.095f },
                new[] { 1.440f, 0.268f, 0.105f }, new[] { 1.250f, 0.258f, 0.102f }, new[] { 1.050f, 0.262f, 0.104f }, new[] { 0.850f, 0.282f, 0.110f },
                new[] { 0.700f, 0.305f, 0.116f }, new[] { 0.625f, 0.315f, 0.118f },
            };
            const int N = 32;
            var body = new List<Vector3[]>();
            foreach (var pr in prof)
            {
                float drop = Mathf.InverseLerp(1.5f, 0.62f, pr[0]);
                var ring = new Vector3[N];
                for (int i = 0; i < N; i++)
                {
                    float th = 2f * Mathf.PI * i / N, c = Mathf.Cos(th), sn = Mathf.Sin(th);
                    float x = pr[1] * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.8f), zz = pr[2] * Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.8f);
                    float fold = 1f + 0.04f * drop * Mathf.Sin(7f * th + pr[0] * 5f);
                    ring[i] = new Vector3(x * fold, pr[0], Z + zz * fold);
                }
                body.Add(ring);
            }
            var mbBody = new MeshBuilder(); mbBody.Loft(body); Add("Body", mbBody, white);

            // sleeves: tapered tubes hanging from the shoulders, a little out from the body, with a cuff
            for (int sd = -1; sd <= 1; sd += 2)
            {
                var rings = new List<Vector3[]>();
                var from = new Vector3(sd * 0.245f, 1.585f, Z); var to = new Vector3(sd * 0.335f, 0.930f, Z + 0.015f);
                var axis = (to - from).normalized; var u = Vector3.Cross(axis, Vector3.forward).normalized; var w = Vector3.Cross(axis, u);
                for (int k = -2; k <= 9; k++)
                {
                    float f = Mathf.Max(0f, k / 9f); var cen = Vector3.Lerp(from, to, f) + (k < 0 ? axis * (k * 0.025f) : Vector3.zero);
                    float rad = k == -2 ? 0.05f : k == -1 ? 0.085f : Mathf.Lerp(0.092f, 0.060f, f) * (k == 9 ? 1.08f : 1f);   // a rounded shoulder cap, then the taper and cuff
                    var ring = new Vector3[20];
                    for (int i = 0; i < 20; i++) { float th = 2f * Mathf.PI * i / 20; ring[i] = cen + (u * Mathf.Cos(th) * rad * 1.05f + w * Mathf.Sin(th) * rad) * (1f + 0.03f * Mathf.Sin(5f * th + f * 6f)); }
                    rings.Add(ring);
                }
                var mb = new MeshBuilder(); mb.Loft(rings, true); Add(sd < 0 ? "SleeveL" : "SleeveR", mb, white);
            }

            // collar: a short stand-up band around the neck, open at the front by the lapels
            var collar = new List<Vector3[]>();
            foreach (var cy in new[] { 1.675f, 1.705f, 1.735f })
            {
                var ring = new Vector3[24]; float rw = cy > 1.72f ? 0.095f : 0.085f;
                for (int i = 0; i < 24; i++) { float th = 2f * Mathf.PI * i / 24; ring[i] = new Vector3(Mathf.Cos(th) * rw, cy, Z - 0.005f + Mathf.Sin(th) * rw * 0.75f); }
                collar.Add(ring);
            }
            var mbc = new MeshBuilder(); mbc.Loft(collar); Add("Collar", mbc, shade);

            // lapels: two soft folded panels in a V, plus the placket line, buttons and pockets
            var lap = new MeshBuilder();
            foreach (int sd in new[] { -1, 1 })
            {
                var rings = new List<Vector3[]>();
                for (int k = 0; k <= 5; k++)
                {
                    float f = k / 5f; var cen = new Vector3(sd * Mathf.Lerp(0.055f, 0.012f, f), Mathf.Lerp(1.665f, 1.35f, f), Z + 0.108f - 0.012f * f);
                    float hw = Mathf.Lerp(0.07f, 0.02f, f);
                    var ring = new Vector3[8]; for (int i = 0; i < 8; i++) { float th = 2f * Mathf.PI * i / 8; ring[i] = cen + new Vector3(Mathf.Cos(th) * hw * 1.0f, 0f, Mathf.Sin(th) * 0.012f); }
                    rings.Add(ring);
                }
                lap.Loft(rings, true);
            }
            Add("Lapels", lap, shade);

            var det = new MeshBuilder();
            for (int i = 0; i < 4; i++) det.Sphere(new Vector3(0.018f, 1.27f - i * 0.17f, Z + 0.112f), 0.014f, 8, 6);
            Add("Buttons", det, button);
            var pocketL = new MeshBuilder(); var pocketR = new MeshBuilder(); var pocketB = new MeshBuilder();
            foreach (var pk in new[] { (pocketL, new Vector3(-0.15f, 0.86f, Z + 0.111f), 0.075f, 0.085f), (pocketR, new Vector3(0.15f, 0.86f, Z + 0.111f), 0.075f, 0.085f), (pocketB, new Vector3(-0.13f, 1.37f, Z + 0.104f), 0.05f, 0.058f) })
            {
                var rings = new List<Vector3[]>();
                foreach (var dz in new[] { 0f, 0.008f })
                {
                    var ring = new Vector3[4] { pk.Item2 + new Vector3(-pk.Item3, pk.Item4, dz), pk.Item2 + new Vector3(pk.Item3, pk.Item4, dz), pk.Item2 + new Vector3(pk.Item3, -pk.Item4, dz), pk.Item2 + new Vector3(-pk.Item3, -pk.Item4, dz) };
                    rings.Add(ring);
                }
                pk.Item1.Loft(rings);
            }
            Add("PocketL", pocketL, shade); Add("PocketR", pocketR, shade); Add("PocketB", pocketB, shade);

            // hanger: hook over the rail, then arms sloping to the shoulders
            var hg = new MeshBuilder();
            hg.Tube(new List<Vector3> { new Vector3(0, 1.97f, Z), new Vector3(0, 1.915f, Z), new Vector3(0.012f, 1.88f, Z), new Vector3(0, 1.845f, Z) }, 0.006f, 6);
            hg.Tube(new List<Vector3> { new Vector3(-0.25f, 1.69f, Z), new Vector3(-0.12f, 1.77f, Z), new Vector3(0, 1.845f, Z), new Vector3(0.12f, 1.77f, Z), new Vector3(0.25f, 1.69f, Z) }, 0.007f, 6);
            Add("Hanger", hg, metal);

            // one box collider that covers the whole coat, so the highlight and the press work on the lot
            var col = coat.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.3f, Z); col.size = new Vector3(0.95f, 1.45f, 0.25f);
            return coat.gameObject;
        }

        public void Register(StepManager steps) { steps.Register("wear", Wear); }

        IEnumerator Wear(StepDef step, GameObject item)
        {
            var hands = GloveHands.Instance;
            var cam = Camera.main;
            if (hands != null)
            {
                hands.SetPose(true, HandPose.Open);
                yield return hands.ReachTo(true, LabUtil.Approach(item, 0.17f), 0.5f);
                hands.SetPose(true, HandPose.Grab);
                yield return new WaitForSeconds(0.2f);
            }
            Sfx.Press(item.transform.position);
            if (cam != null)
            {
                var to = cam.transform.position + cam.transform.forward * 0.42f + Vector3.down * 0.12f;
                yield return LabUtil.Move(item.transform, to, 0.6f, null, 0.35f);
            }
            if (hands != null) hands.SetSleeves(true);
            NarrationManager.Instance.ShowMessage("Lab coat on.", 2f);
            Sfx.Correct(item.transform.position);
            Destroy(item);
            if (hands != null) { hands.SetPose(true, HandPose.Relaxed); yield return hands.ReturnToRest(true, 0.45f); }
        }
    }
}
