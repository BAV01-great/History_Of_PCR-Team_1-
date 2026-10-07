using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PCR
{
    public class MilestoneVisuals : MonoBehaviour
    {
        TimelineStage stage;
        JourneyAudio snd;
        readonly List<GameObject> spawned = new List<GameObject>();
        float stageLightBase = 1.6f;

        Transform R => stage.VisualRoot;
        static readonly Color Gold = new Color(1f, 0.82f, 0.25f), Cyan = new Color(0.25f, 0.92f, 1f), Magenta = new Color(0.95f, 0.25f, 0.75f),
                              Orange = new Color(1f, 0.6f, 0.15f), Red = new Color(1f, 0.3f, 0.25f), Blue = new Color(0.3f, 0.55f, 1f);

        public void Init(TimelineStage s, JourneyAudio a) { stage = s; snd = a; stageLightBase = s.Fill.intensity; }

        public IEnumerator Play(int i)
        {
            Stop();
            switch (i)
            {
                case 0: yield return M1869(); break;
                case 1: yield return M1953(); break;
                case 2: yield return M1983(); break;
                case 3: yield return M1985(); break;
                case 4: yield return M1988(); break;
                case 5: yield return M1993(); break;
                case 6: yield return M1990s(); break;
                case 7: yield return MToday(); break;
            }
        }

        public void Stop()
        {
            foreach (var g in spawned) if (g != null) Destroy(g);
            spawned.Clear();
            if (stage != null) { stage.Fill.intensity = stageLightBase; stage.Fill.color = new Color(0.72f, 0.86f, 1f); RenderSettings.ambientLight = LabLightingAmbient; }
        }

        static readonly Color LabLightingAmbient = new Color(0.100f, 0.135f, 0.260f);

        GameObject Obj(string name, Vector3 local)
        {
            var g = new GameObject(name);
            g.transform.SetParent(R, false);
            g.transform.localPosition = local;
            spawned.Add(g);
            return g;
        }

        Material Glass(Color c, float a, float smooth = 0.9f)
        {
            var m = Mats.LitNew(new Color(c.r, c.g, c.b, a), null, smooth, 0f);
            Mats.MakeTransparent(m, false);
            return m;
        }

        Material Glow(Color c, float k) => Mats.LitNew(c * 0.6f, c * k, 0.4f, 0f);

        static void SetA(Material m, float a) { var c = m.GetColor("_BaseColor"); c.a = a; m.SetColor("_BaseColor", c); }

        static IEnumerator Over(float seconds, Action<float> f)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime) { f(Mathf.Clamp01(t / seconds)); yield return null; }
            f(1f);
        }

        static IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

        IEnumerator Pop(Transform t, float seconds = 0.45f)
        {
            var to = t.localScale;
            t.localScale = Vector3.zero;
            yield return Over(seconds, k => { float e = 1f - Mathf.Pow(1f - k, 3f); t.localScale = to * e * (1f + 0.12f * Mathf.Sin(k * Mathf.PI)); });
            t.localScale = to;
        }

        Text Caption(string text, Vector3 local, int size, Color c, float width = 2.2f, TextAnchor a = TextAnchor.MiddleCenter, bool outline = true)
        {
            var cv = Ui.Canvas("Caption", R, new Vector2(width * 1000f, size * 1.7f), local);
            spawned.Add(cv.gameObject);
            return Ui.Label(cv.transform, text, size, c, a, new Vector2(width * 1000f, size * 1.7f), Vector2.zero, FontStyle.Bold, outline);
        }

        void Say(string id) => NarrationManager.Instance.Say(id);

        GameObject Model(string res, Vector3 local, float fit, Color? tint = null)
        {
            var g = LabProps.Spawn(res, R, R.TransformPoint(local), 0f, tint, 1f, fit);
            if (g == null) return null;
            var b = LabUtil.BoundsOf(g);
            g.transform.position += R.TransformPoint(local) - b.center;
            spawned.Add(g);
            return g;
        }

        IEnumerator M1869()
        {
            yield return Wait(1.5f);
            var cell = Obj("Cell", new Vector3(0, 0, 0));
            var memM = Glass(new Color(0.4f, 0.9f, 0.6f), 0.28f);
            var membrane = Gen.Prim(PrimitiveType.Sphere, "Membrane", cell.transform, Vector3.zero, Vector3.one * 1.1f, memM).transform;
            var rnd = new System.Random(5);
            for (int k = 0; k < 7; k++)
                Gen.Prim(PrimitiveType.Sphere, "Organelle", cell.transform, new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, ((float)rnd.NextDouble() - 0.5f) * 0.5f) * 0.7f,
                    Vector3.one * (0.06f + (float)rnd.NextDouble() * 0.06f), Glow(Color.Lerp(Orange, Magenta, (float)rnd.NextDouble()), 0.6f));
            var nucM = Glass(Blue, 0.5f);
            var nucleus = Gen.Prim(PrimitiveType.Sphere, "Nucleus", cell.transform, new Vector3(0.03f, 0.02f, 0f), Vector3.one * 0.42f, nucM).transform;
            snd.Cue("pop");
            StartCoroutine(Pop(cell.transform, 0.7f));
            var lbl = Caption("A CELL", new Vector3(0, -0.85f, 0), 70, Cyan, 2f);
            yield return Wait(0.5f);
            Say("m1869");
            yield return Wait(1.8f);
            lbl.text = "THE NUCLEUS OPENS";
            snd.Cue("bubble");
            yield return Over(2.6f, k =>
            {
                nucleus.localScale = Vector3.one * Mathf.Lerp(0.42f, 0.62f, k);
                SetA(nucM, Mathf.Lerp(0.5f, 0.08f, k));
            });
            lbl.text = "NUCLEIN, LATER KNOWN AS DNA";
            snd.Cue("pluck");
            var dnaIn = Part("DNA_Helix_Whole", cell.transform, Vector3.one * 1.3f, 0.8f);
            if (dnaIn != null) { dnaIn.transform.localPosition = new Vector3(0.03f, -0.17f, 0f); StartCoroutine(Pop(dnaIn.transform, 0.6f)); }
            yield return Over(3.2f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                nucleus.localScale = Vector3.one * Mathf.Lerp(0.62f, 0.8f, e);
                SetA(memM, Mathf.Lerp(0.28f, 0.06f, e));
                if (dnaIn != null) dnaIn.transform.Rotate(0, 70f * Time.deltaTime, 0, Space.Self);
            });
            yield return Over(3f, k => { if (dnaIn != null) dnaIn.transform.Rotate(0, 70f * Time.deltaTime, 0, Space.Self); });
        }

        IEnumerator M1953()
        {
            yield return Wait(1.5f);
            var h = MorphHelix.Create(R, "Helix1953", new Vector3(0, -0.8f, 0), 28, 0.2f, 0.06f, 11, 1.1f);
            h.Set(0f, 0.9f, 0f, 0f);
            spawned.Add(h.gameObject);
            snd.Cue("pop");
            StartCoroutine(Pop(h.transform, 0.6f));
            var lbl = Caption("TWO SINGLE STRANDS", new Vector3(0, 0.9f, 0), 74, Cyan, 2f);
            Say("m1953");
            yield return Wait(2.2f);
            snd.Cue("swirl");
            lbl.text = "THEY JOIN TOGETHER";
            yield return Over(3f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                h.SetSeparation(0.9f * (1f - e), Mathf.Clamp01((k - 0.5f) * 2f));
            });
            lbl.text = "IT TWISTS";
            yield return Over(6f, k => h.SetTwist(36f * Mathf.SmoothStep(0, 1, k)));
            lbl.text = "THE DOUBLE HELIX";
            snd.Cue("chime");
            lbl.color = Color.white;
            var glow = Glow(Cyan, 1f);
            yield return Over(2f, k => h.transform.localRotation = Quaternion.Euler(0, 90f * k, 0));
            yield return Wait(1.2f);
        }

        Transform card;
        HoloButton cardButton;
        string cardBody = "";

        HoloButton ShowCard(string title, string body, Color accent, string button = null, Color? buttonColor = null, bool interactable = true)
        {
            if (card != null) Destroy(card.gameObject);
            if (cardButton != null) Destroy(cardButton.gameObject);
            cardButton = null;
            var size = new Vector2(980, button != null ? 640 : 560);
            var cv = Ui.Canvas("StageCard", R, size, new Vector3(1.25f, 0.45f, 0));
            spawned.Add(cv.gameObject);
            Ui.Rect(cv.transform, new Color(0.04f, 0.07f, 0.17f, 0.94f), size, Vector2.zero);
            float top = size.y / 2f;
            Ui.Label(cv.transform, title, 84, accent, TextAnchor.UpperCenter, new Vector2(920, 120), new Vector2(0, top - 90), FontStyle.Bold);
            int bodySize = body.Length > 90 ? 46 : 58;
            Ui.Label(cv.transform, body, bodySize, Color.white, TextAnchor.UpperCenter, new Vector2(880, 300), new Vector2(0, top - 300), FontStyle.BoldAndItalic);
            card = cv.transform; cardBody = body;
            StartCoroutine(Pop(card, 0.4f));
            if (button != null)
            {
                cardButton = HoloButton.Create(R, new Vector3(1.25f, 0.45f - size.y / 2000f + 0.13f, -0.05f), button, buttonColor ?? Gold, new Vector2(0.72f, 0.17f), 56);
                cardButton.Interactable = interactable;
                spawned.Add(cardButton.gameObject);
            }
            return cardButton;
        }

        IEnumerator WaitPress(HoloButton b)
        {
            if (b == null) yield break;
            bool done = false;
            b.Pressed += () => done = true;
            float delay = Mathf.Clamp(1.8f + cardBody.Length * 0.035f, 2.6f, 6.5f);
            float t = 0f;
            while (!done)
            {
                t += Time.deltaTime;
                if (t > delay) b.Press();
                yield return null;
            }
            yield return Wait(0.45f);
        }

        const float EnsH = 2.5f, EnsW = 4.5f;
        static readonly Vector3 EnsScale = new Vector3(EnsW, EnsH, EnsW);
        static readonly Vector3 HelixScale = Vector3.one * 5.4f;
        const float LadderX = 0.025f * EnsW;

        GameObject Part(string res, Transform parent, Vector3 scale, float glow = 0.5f)
        {
            var prefab = Resources.Load<GameObject>("PCRModels/" + res);
            if (prefab == null) return null;
            var g = Instantiate(prefab, parent);
            g.transform.localPosition = Vector3.zero;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = scale;
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int k = 0; k < src.Length; k++)
                {
                    var c = Color.white;
                    if (src[k] != null) c = src[k].HasProperty("_BaseColor") ? src[k].GetColor("_BaseColor") : (src[k].HasProperty("_Color") ? src[k].GetColor("_Color") : Color.white);
                    c.a = 1f;
                    dst[k] = Mats.LitNew(c, c * glow, 0.55f, 0f);
                }
                r.sharedMaterials = dst;
            }
            return g;
        }

        GameObject PageHelix(Transform page, Vector3 pos, float length)
        {
            var g = Part("DNA_Helix_Whole", page, Vector3.one * (length / 0.277f), 0.6f);
            if (g == null) return new GameObject("PageHelixMissing");
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return g;
        }

        IEnumerator M1983()
        {
            var ens = Obj("Ensemble", new Vector3(0, -0.8f, 0)).transform;
            var helix = Part("DNA_Helix_Whole", ens, HelixScale, 0.6f);
            var ladder = Part("DNA_Ladder_Whole", ens, EnsScale, 0.6f);
            if (ladder != null) ladder.transform.localPosition = new Vector3(LadderX, 0, 0);
            var ga = new GameObject("StrandSideA").transform; ga.SetParent(ens, false); ga.localPosition = new Vector3(LadderX, 0, 0);
            var gb = new GameObject("StrandSideB").transform; gb.SetParent(ens, false); gb.localPosition = new Vector3(LadderX, 0, 0);
            var strandA = Part("DNA_StrandA_Straight", ga, EnsScale, 0.6f);
            var strandB = Part("DNA_StrandB_Straight", gb, EnsScale, 0.6f);
            var primerF = Part("Primer_F_Straight", gb, EnsScale, 1.4f);
            var primerR = Part("Primer_R_Straight", ga, EnsScale, 1.4f);
            var extF = new GameObject[4]; var extR = new GameObject[4];
            for (int e = 0; e < 4; e++) { extF[e] = Part("Ext_F_Straight_" + (e + 1), gb, EnsScale, 1.2f); extR[e] = Part("Ext_R_Straight_" + (e + 1), ga, EnsScale, 1.2f); }
            void On(GameObject g, bool v) { if (g != null) g.SetActive(v); }
            On(ladder, false); On(strandA, false); On(strandB, false); On(primerF, false); On(primerR, false);
            foreach (var g in extF) On(g, false);
            foreach (var g in extR) On(g, false);
            StartCoroutine(Pop(ens, 0.5f));
            Say("m1983_a");
            snd.Cue("idea");
            var basePos = ens.localPosition;
            var green = new Color(0.1f, 0.72f, 0.3f);
            var amber = new Color(0.92f, 0.62f, 0.1f);
            var okGreen = new Color(0.3f, 1f, 0.6f);
            var introCap = Caption("THE BIRTH OF PCR", new Vector3(0, 0.88f, 0), 64, Gold, 2.4f);
            yield return Wait(0.5f);
            while (NarrationManager.Instance.IsSpeaking) yield return null;
            Destroy(introCap.transform.parent.gameObject);
            yield return Wait(0.4f);

            var heatBtn = ShowCard("DENATURATION", "Start denaturation by heating the DNA.", Color.white, "HEAT >>>", new Color(0.9f, 0.12f, 0.1f));
            Say("m1983_b1");
            yield return WaitPress(heatBtn);
            Say("m1983_b2");
            snd.Cue("sizzle");
            yield return Over(1.6f, k => ens.localPosition = basePos + new Vector3(Mathf.Sin(k * 90f) * 0.014f * k, 0, 0));
            On(helix, false); On(ladder, true);
            yield return Over(1.2f, k => ens.localPosition = basePos + new Vector3(Mathf.Sin(k * 90f) * 0.014f, 0, 0));
            ens.localPosition = basePos;
            On(ladder, false); On(strandA, true); On(strandB, true);
            snd.Cue("split");
            yield return Over(2.8f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                ga.localPosition = new Vector3(LadderX + 0.55f * e, 0, 0);
                gb.localPosition = new Vector3(LadderX - 0.55f * e, 0, 0);
            });
            yield return WaitPress(ShowCard("COMPLETE", "Denaturation complete!! The next phase is ANNEALING.", okGreen, "CONTINUE >>>", green));

            yield return WaitPress(ShowCard("ANNEALING", "Let's cool down and close up those strands with complement primers.", Gold, "PRIMERS >>>", amber));
            ShowCard(">>> 50 °C", "Cooling the mixture to 50 °C for primers.", Blue, "COOLING...", Blue, false);
            snd.Cue("swirl");
            yield return Wait(2.6f);
            yield return WaitPress(ShowCard("COOLED!!", "Mixture is now at 50 °C. Adding primers to complement strands.", Gold, "PRIMERS >>>", amber));
            Say("m1983_b3");
            On(primerF, true); On(primerR, true);
            yield return Over(2.4f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                if (primerF != null) primerF.transform.localPosition = new Vector3(Mathf.Lerp(0.9f, 0f, e), 0, 0);
                if (primerR != null) primerR.transform.localPosition = new Vector3(Mathf.Lerp(-0.9f, 0f, e), 0, 0);
            });
            snd.Cue("click");
            yield return WaitPress(ShowCard("COMPLETE!!", "Annealing completed! The last stage is EXTENSION.", okGreen, "CONTINUE >>>", green));

            yield return WaitPress(ShowCard("EXTENSION!!", "Complete the process by heating and adding Taq polymerase.", okGreen, "POLYMERASE >>>", green));
            ShowCard(">>> 72 °C", "Heating the mixture for Taq DNA polymerase.", Red, "HEATING...", new Color(0.85f, 0.3f, 0.12f), false);
            snd.Cue("sizzle");
            yield return Wait(2.6f);
            yield return WaitPress(ShowCard("HEATED", "Mixture is now at 72 °C. Adding DNA polymerase.", new Color(1f, 0.6f, 0.65f), "POLYMERASE >>>", green));
            Say("m1983_b4");
            var newCap = Caption("NEW DNA STRANDS ARE BUILT", new Vector3(-0.2f, 0.88f, 0), 52, Gold, 2.4f);
            var taqA = Model("PCRModels/Taq", Vector3.zero, 0.16f, new Color(2f, 1.3f, 0.6f));
            var taqB = Model("PCRModels/Taq", Vector3.zero, 0.16f, new Color(2f, 1.3f, 0.6f));
            snd.Cue("ticks");
            for (int e = 0; e < 4; e++)
            {
                if (taqA != null) taqA.transform.position = gb.TransformPoint(new Vector3(0.3f, (0.24f + 0.1f * e) * EnsH, 0));
                if (taqB != null) taqB.transform.position = ga.TransformPoint(new Vector3(-0.45f, (0.35f - 0.1f * e) * EnsH, 0));
                On(extF[e], true); On(extR[e], true);
                if (extF[e] != null) StartCoroutine(Pop(extF[e].transform, 0.3f));
                if (extR[e] != null) StartCoroutine(Pop(extR[e].transform, 0.3f));
                snd.Cue("pop");
                yield return Wait(0.95f);
            }
            if (taqA != null) Destroy(taqA);
            if (taqB != null) Destroy(taqB);
            Destroy(newCap.transform.parent.gameObject);
            snd.Cue("chime");
            yield return WaitPress(ShowCard("CYCLE COMPLETE!", "One cycle of PCR is now complete and the DNA is now duplicated.\nPCR simply repeats the cycle 25 to 35 times to have over a billion copies of DNA from the original.", Gold, "END CYCLE", amber));
            if (card != null) Destroy(card.gameObject);
            if (cardButton != null) Destroy(cardButton.gameObject);

            Say("m1983_b5");
            var temp = Caption("", new Vector3(-1.25f, 0.1f, 0), 96, Cyan, 1.4f);
            ens.gameObject.SetActive(false);
            var head = Caption("THE DNA DOUBLES EVERY CYCLE", new Vector3(0, 0.78f, 0), 60, Cyan, 2.6f);
            snd.Cue("sparkle");
            var copies = new List<GameObject>();
            int n = 1;
            for (int round = 1; round <= 3; round++)
            {
                n *= 2;
                foreach (var g in copies) Destroy(g);
                copies.Clear();
                for (int c = 0; c < n; c++)
                {
                    float x = (c - (n - 1) / 2f) * Mathf.Min(0.42f, 1.8f / n);
                    var holder = Obj("Copy", new Vector3(x, -0.5f, 0)).transform;
                    Part("DNA_Helix_Whole", holder, HelixScale * 0.6f, 0.7f);
                    copies.Add(holder.gameObject);
                    StartCoroutine(Pop(holder, 0.35f));
                }
                snd.Cue("pop");
                temp.text = "x" + n;
                temp.color = Cyan;
                yield return Over(1.3f, k2 => { foreach (var g in copies) if (g != null) g.transform.Rotate(0, 80f * Time.deltaTime, 0, Space.World); });
            }
            snd.Cue("ticks");
            head.text = "REPEAT 25 TO 35 TIMES: OVER A BILLION COPIES";
            yield return Over(3.2f, k2 => { foreach (var g in copies) if (g != null) g.transform.Rotate(0, 80f * Time.deltaTime, 0, Space.World); });
        }

        IEnumerator M1985()
        {
            var journal = Obj("Journal", new Vector3(0, -0.25f, 0));
            var cover = Mats.Lit(Theme.NavyMid, null, 0.4f);
            var page = Mats.Lit(new Color(0.96f, 0.94f, 0.88f), new Color(0.55f, 0.53f, 0.48f), 0.3f);
            var ink = Mats.Lit(new Color(0.1f, 0.1f, 0.2f), null, 0.2f);
            Gen.Box("Cover", journal.transform, new Vector3(0, -0.02f, 0), new Vector3(1.1f, 0.03f, 1.4f), cover);
            Gen.Box("Page", journal.transform, new Vector3(0, 0.002f, 0), new Vector3(1.0f, 0.02f, 1.3f), page);
            journal.transform.localRotation = Quaternion.Euler(-68f, 0f, 0f);
            journal.transform.localScale = Vector3.one * 1.2f;
            var cv = Ui.Canvas("PageText", journal.transform, new Vector2(1000, 1300), new Vector3(0, 0.016f, 0));
            cv.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cv.transform.localScale = Vector3.one * 0.001f;
            var dark = new Color(0.1f, 0.1f, 0.15f);
            Ui.Label(cv.transform, "Enzymatic Amplification of β-Globin Genomic Sequences and Restriction Site Analysis for Diagnosis of Sickle Cell Anemia", 54, dark, TextAnchor.UpperCenter, new Vector2(920, 300), new Vector2(0, 490), FontStyle.Bold);
            Ui.Label(cv.transform, "Saiki, R. K., et al. (1985)", 44, new Color(0.35f, 0.2f, 0.2f), TextAnchor.UpperCenter, new Vector2(900, 70), new Vector2(0, 300), FontStyle.Italic);
            snd.Cue("rustle");
            StartCoroutine(Pop(journal.transform, 0.8f));
            Say("m1985_a");
            yield return Wait(1.3f);
            var one = PageHelix(journal.transform, new Vector3(-0.3f, 0.02f, -0.36f), 0.5f);
            StartCoroutine(Pop(one.transform, 0.4f));
            snd.Cue("pop");
            yield return Wait(1.6f);
            Ui.Label(cv.transform, "Amplification", 40, dark, TextAnchor.MiddleCenter, new Vector2(300, 50), new Vector2(-10, -65), FontStyle.Italic);
            Gen.Box("ArrowShaft", journal.transform, new Vector3(-0.02f, 0.016f, -0.11f), new Vector3(0.24f, 0.004f, 0.012f), ink);
            for (int a = -1; a <= 1; a += 2)
            {
                var head = Gen.Box("ArrowHead", journal.transform, new Vector3(0.075f, 0.016f, -0.11f + a * 0.017f), new Vector3(0.06f, 0.004f, 0.012f), ink);
                head.transform.localRotation = Quaternion.Euler(0, a * 35f, 0);
            }
            snd.Cue("pop");
            yield return Wait(0.8f);
            for (int i = 0; i < 4; i++)
            {
                var d = PageHelix(journal.transform, new Vector3(0.22f + (i % 2) * 0.19f, 0.02f, i < 2 ? -0.06f : -0.42f), 0.3f);
                StartCoroutine(Pop(d.transform, 0.4f));
                snd.Cue("pop");
                yield return Wait(0.35f);
            }
            snd.Cue("sparkle");
            yield return Wait(1.5f);
            while (NarrationManager.Instance.IsSpeaking) yield return null;

            Say("m1985_b");
            Destroy(journal);
            var h = MorphHelix.Create(R, "HeatedDNA", new Vector3(-0.5f, -0.7f, 0), 24, 0.17f, 0.06f, 31, 1.0f);
            spawned.Add(h.gameObject);
            var enzyme = Model("PCRModels/Taq_Broken", new Vector3(0.55f, 0.1f, 0), 0.34f, new Color(0.7f, 0.8f, 1f));
            var cap = Caption("HEAT SPLITS THE DNA", new Vector3(0, 0.9f, 0), 70, Red, 2.2f);
            snd.Cue("split");
            yield return Over(2.2f, k => h.SetSeparation(0.32f * k, 1f - k));
            cap.text = "...AND KEEPS DESTROYING THE ENZYME";
            snd.Cue("sizzle");
            if (enzyme != null)
                yield return Over(2.4f, k =>
                {
                    enzyme.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 40f) * 12f * (1f - k));
                    enzyme.transform.localScale = Vector3.one * (1f - 0.35f * k);
                });
            else yield return Wait(2.4f);
            yield return Wait(1.5f);
        }

        IEnumerator M1988()
        {
            var gauge = Obj("Thermometer", new Vector3(-1.0f, -0.2f, 0));
            var glassM = Glass(new Color(0.85f, 0.95f, 1f), 0.22f, 0.95f);
            var hot = Mats.LitNew(Red * 0.8f, Red * 1.6f, 0.6f);
            var tick = Mats.Lit(new Color(0.9f, 0.95f, 1f), null, 0.3f);
            const float bulbY = -0.78f, tMin = 20f, tMax = 100f, y0 = -0.68f, span = 1.28f;
            Gen.Prim(PrimitiveType.Cylinder, "Stem", gauge.transform, new Vector3(0, (bulbY + y0 + span) / 2f + 0.02f, 0), new Vector3(0.11f, (y0 + span - bulbY) / 2f + 0.08f, 0.11f), glassM);
            Gen.Prim(PrimitiveType.Sphere, "StemTop", gauge.transform, new Vector3(0, y0 + span + 0.1f, 0), new Vector3(0.11f, 0.11f, 0.11f), glassM);
            Gen.Prim(PrimitiveType.Sphere, "Bulb", gauge.transform, new Vector3(0, bulbY, 0), Vector3.one * 0.27f, glassM);
            Gen.Prim(PrimitiveType.Sphere, "BulbFluid", gauge.transform, new Vector3(0, bulbY, 0), Vector3.one * 0.2f, hot);
            var level = Gen.Prim(PrimitiveType.Cylinder, "Mercury", gauge.transform, new Vector3(0, bulbY, 0), new Vector3(0.045f, 0.01f, 0.045f), hot).transform;
            for (float t = tMin; t <= tMax + 0.1f; t += 5f)
            {
                bool major = Mathf.Abs(t % 10f) < 0.1f;
                float ty = y0 + (t - tMin) / (tMax - tMin) * span;
                Gen.Box("Tick", gauge.transform, new Vector3(0.1f + (major ? 0.05f : 0.025f), ty, 0), new Vector3(major ? 0.1f : 0.05f, 0.012f, 0.02f), tick);
                if (major) Caption(Mathf.RoundToInt(t).ToString(), new Vector3(-1.0f + 0.3f, -0.2f + ty, 0), 38, Color.white, 0.4f);
            }
            Caption("°C", new Vector3(-1.0f, -0.2f + y0 + span + 0.3f, 0), 56, Color.white, 0.5f);
            var tempText = Caption("25 °C", new Vector3(-1.0f, -1.2f, 0), 70, Red, 1.2f);
            System.Action<float> SetTemp = c =>
            {
                float topY = y0 + Mathf.Clamp01((c - tMin) / (tMax - tMin)) * span;
                float h = Mathf.Max(0.01f, topY - bulbY);
                level.localScale = new Vector3(0.045f, h / 2f, 0.045f);
                level.localPosition = new Vector3(0, bulbY + h / 2f, 0);
            };
            SetTemp(25f);
            var oldE = Model("PCRModels/Taq_Broken", new Vector3(0.1f, 0.05f, 0), 0.4f, new Color(0.65f, 0.75f, 1f));
            var cap = Caption("EARLIER POLYMERASE + HEAT", new Vector3(0, 0.92f, 0), 68, Red, 2.4f);
            yield return Over(2f, k =>
            {
                float c = Mathf.Lerp(25f, 95f, k);
                SetTemp(c);
                tempText.text = Mathf.RoundToInt(c) + " °C";
                if (oldE != null) oldE.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 50f) * 10f * k);
            });
            snd.Cue("sizzle");
            yield return Wait(0.5f);
            snd.Cue("crack");
            cap.text = "THE POLYMERASE IS DESTROYED";
            if (oldE != null)
            {
                var dir = new[] { Vector3.left, Vector3.right, Vector3.up, Vector3.down };
                yield return Over(1.2f, k =>
                {
                    oldE.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.001f, k * k);
                });
                Destroy(oldE);
            }
            for (int k = 0; k < 8; k++)
            {
                var shard = Gen.Prim(PrimitiveType.Cube, "Shard", R, new Vector3(0.1f, 0.05f, 0), Vector3.one * 0.05f, Mats.Lit(new Color(0.5f, 0.55f, 0.7f), null, 0.4f));
                spawned.Add(shard);
                StartCoroutine(Fly(shard.transform, UnityEngine.Random.onUnitSphere * 0.9f, 1.4f));
            }
            yield return Wait(1.2f);

            Say("m1988");
            cap.text = "TAQ POLYMERASE: STILL ACTIVE AT 95 °C";
            cap.color = Orange;
            var taq = Model("PCRModels/Taq", new Vector3(0.1f, 0.1f, 0), 0.5f, new Color(1.5f, 1.15f, 0.9f));
            snd.Cue("warm");
            if (taq != null) { var s0 = taq.transform.localScale; taq.transform.localScale = Vector3.zero; yield return Over(0.7f, k => taq.transform.localScale = s0 * k); }
            var cycleText = Caption("", new Vector3(1.0f, 0.55f, 0), 80, Cyan, 1.6f);
            var tc = Model("PCRModels/PCR", new Vector3(1.1f, -0.55f, 0), 0.5f, null);
            snd.Cue("ticks");
            float elapsed = 0f; int cycle = 0; float period = 0.9f; float next = 0f;
            while (elapsed < 7.5f)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= next)
                {
                    cycle++; period = Mathf.Max(0.12f, period * 0.78f); next = elapsed + period;
                    cycleText.text = "CYCLE " + cycle;
                    SetTemp(cycle % 2 == 0 ? 95f : 72f);
                    tempText.text = (cycle % 2 == 0 ? 95 : 72) + " °C";
                    if (taq != null) taq.transform.localRotation = Quaternion.Euler(0, cycle * 30f, 0);
                }
                yield return null;
            }
            cycleText.text = "AUTOMATED PCR";
            yield return Wait(1.5f);
        }

        IEnumerator Fly(Transform t, Vector3 dir, float seconds)
        {
            var p0 = t.position; var s0 = t.localScale;
            for (float e = 0; e < seconds && t != null; e += Time.deltaTime)
            {
                float k = e / seconds;
                t.position = p0 + dir * k; t.localScale = s0 * (1f - k); t.Rotate(200f * Time.deltaTime, 120f * Time.deltaTime, 0);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        IEnumerator M1993()
        {
            yield return Over(1.4f, k =>
            {
                stage.Fill.intensity = Mathf.Lerp(stageLightBase, 0.15f, k);
                RenderSettings.ambientLight = Color.Lerp(LabLightingAmbient, new Color(0.01f, 0.015f, 0.04f), k);
            });
            var medal = Obj("Medal", new Vector3(0.1f, -0.2f, 0.2f));
            var goldM = Mats.LitNew(new Color(0.92f, 0.7f, 0.18f), new Color(0.95f, 0.65f, 0.12f) * 0.5f, 0.95f, 1f);
            var darkGold = Mats.LitNew(new Color(0.65f, 0.45f, 0.1f), new Color(0.6f, 0.4f, 0.08f) * 0.3f, 0.8f, 1f);
            var face = Gen.Prim(PrimitiveType.Cylinder, "Face", medal.transform, Vector3.zero, new Vector3(0.62f, 0.02f, 0.62f), goldM, false, Quaternion.Euler(90, 0, 0)).transform;
            Gen.Prim(PrimitiveType.Cylinder, "Rim", medal.transform, new Vector3(0, 0, 0.012f), new Vector3(0.7f, 0.012f, 0.7f), darkGold, false, Quaternion.Euler(90, 0, 0));
            Gen.Prim(PrimitiveType.Cylinder, "Inner", medal.transform, new Vector3(0, 0, -0.012f), new Vector3(0.5f, 0.006f, 0.5f), darkGold, false, Quaternion.Euler(90, 0, 0));
            Gen.Box("RibbonL", medal.transform, new Vector3(-0.07f, 0.75f, 0.02f), new Vector3(0.13f, 0.75f, 0.01f), Mats.Lit(Theme.NavyLift, null, 0.5f)).transform.localRotation = Quaternion.Euler(0, 0, -6f);
            Gen.Box("RibbonR", medal.transform, new Vector3(0.07f, 0.75f, 0.02f), new Vector3(0.13f, 0.75f, 0.01f), Mats.Lit(Gold * 0.9f, null, 0.5f)).transform.localRotation = Quaternion.Euler(0, 0, 6f);
            var cv = Ui.Canvas("MedalText", medal.transform, new Vector2(640, 640), new Vector3(0, 0, -0.026f));
            Ui.Label(cv.transform, "NOBEL\nPRIZE", 100, new Color(0.35f, 0.22f, 0.02f), TextAnchor.MiddleCenter, new Vector2(600, 260), new Vector2(0, 90), FontStyle.Bold);
            Ui.Label(cv.transform, "CHEMISTRY\n1993", 76, new Color(0.35f, 0.22f, 0.02f), TextAnchor.MiddleCenter, new Vector2(600, 220), new Vector2(0, -130), FontStyle.Bold);
            var halo = Gen.Prim(PrimitiveType.Quad, "Halo", medal.transform, new Vector3(0, 0, 0.1f), Vector3.one * 2.4f, Mats.Glow(new Color(1f, 0.8f, 0.3f, 0.8f)));
            halo.AddComponent<BillboardY>();
            var spot = new GameObject("MedalLight").AddComponent<Light>();
            spot.transform.SetParent(medal.transform, false);
            spot.type = LightType.Point; spot.color = new Color(1f, 0.85f, 0.5f); spot.range = 4f; spot.intensity = 0f; spot.transform.localPosition = new Vector3(0, 0.1f, -0.8f);
            var line1 = Caption("1993", new Vector3(0, 0.62f, -0.4f), 150, Gold, 2f);
            var line2 = Caption("NOBEL PRIZE IN CHEMISTRY", new Vector3(1.55f, 0.28f, 0), 54, Gold, 2.0f, TextAnchor.MiddleLeft);
            var line3 = Caption("Kary B. Mullis", new Vector3(1.55f, 0.0f, 0), 78, Color.white, 2.0f, TextAnchor.MiddleLeft);
            var line4 = Caption("For his invention of the\npolymerase chain reaction\n(PCR) method", new Vector3(1.55f, -0.38f, 0), 42, new Color(0.85f, 0.9f, 1f), 2.0f, TextAnchor.UpperLeft);
            foreach (var c in new[] { line1, line2, line3, line4 }) c.color = new Color(c.color.r, c.color.g, c.color.b, 0f);
            Say("m1993");
            snd.Cue("nobel");
            var from = new Vector3(0.1f, -1.5f, 0.2f);
            var to = new Vector3(0.1f, 0.2f, 0.2f);
            yield return Over(3.2f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                medal.transform.localPosition = Vector3.Lerp(from, to, e);
                medal.transform.localRotation = Quaternion.Euler(0, Mathf.Sin(k * Mathf.PI * 2f) * 20f, 0);
                spot.intensity = Mathf.Lerp(0f, 4.5f, e);
            });
            snd.Cue("sparkle");
            yield return Over(1.2f, k =>
            {
                foreach (var c in new[] { line1, line2, line3, line4 }) c.color = new Color(c.color.r, c.color.g, c.color.b, k);
            });
            snd.Cue("applause");
            float t = 0f;
            while (t < 6f)
            {
                t += Time.deltaTime;
                medal.transform.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.8f) * 14f, 0);
                halo.transform.localScale = Vector3.one * (2.4f + 0.2f * Mathf.Sin(t * 3f));
                yield return null;
            }
        }

        IEnumerator M1990s()
        {
            var glow = Gen.Prim(PrimitiveType.Quad, "Glow", R, R.TransformPoint(new Vector3(0, 0.1f, 0.3f)), Vector3.one * 2.6f, Mats.Glow(new Color(1f, 0.8f, 0.3f, 0.8f)));
            spawned.Add(glow);
            glow.AddComponent<BillboardY>();
            var gm = glow.GetComponent<Renderer>().material;
            snd.Cue("warm");
            yield return Over(1.8f, k =>
            {
                stage.Fill.intensity = Mathf.Lerp(0.15f, stageLightBase, k);
                stage.Fill.color = Color.Lerp(new Color(1f, 0.85f, 0.5f), new Color(0.5f, 0.75f, 1f), k);
                gm.SetColor("_BaseColor", Color.Lerp(new Color(1f, 0.8f, 0.3f, 0.8f), new Color(0.3f, 0.6f, 1f, 0.5f), k));
            });
            RenderSettings.ambientLight = LabLightingAmbient;
            Say("m1990s");
            var tubes = new List<Material>();
            for (int i = 0; i < 4; i++)
            {
                var tg = Obj("Tube" + i, new Vector3(-1.25f + i * 0.28f, -0.35f, 0));
                Gen.Prim(PrimitiveType.Cylinder, "Body", tg.transform, new Vector3(0, 0.12f, 0), new Vector3(0.1f, 0.12f, 0.1f), Glass(Color.white, 0.3f));
                var m = Mats.LitNew(new Color(0.1f, 0.3f, 0.5f), new Color(0.05f, 0.1f, 0.2f), 0.6f, 0f);
                Gen.Prim(PrimitiveType.Cylinder, "Liquid", tg.transform, new Vector3(0, 0.07f, 0), new Vector3(0.075f, 0.07f, 0.075f), m);
                tubes.Add(m);
            }
            var cap = Caption("WATCHING PCR AS IT HAPPENS", new Vector3(0, 0.92f, 0), 66, Cyan, 2.6f);
            Caption("SAMPLES IN THE MACHINE", new Vector3(-0.82f, -0.5f, 0), 40, Color.white, 1.6f);
            var axes = Obj("Axes", new Vector3(0.55f, -0.35f, 0));
            var axisM = Mats.Lit(Color.white, Color.white * 0.8f, 0.3f);
            Gen.Box("X", axes.transform, new Vector3(0.45f, 0, 0), new Vector3(0.9f, 0.012f, 0.012f), axisM);
            Gen.Box("Y", axes.transform, new Vector3(0, 0.5f, 0), new Vector3(0.012f, 1.0f, 0.012f), axisM);
            Caption("SIGNAL (LIGHT)", new Vector3(0.55f, 0.72f, 0), 40, Color.white, 1.4f);
            Caption("CYCLE NUMBER", new Vector3(1.0f, -0.52f, 0), 40, Color.white, 1.4f);
            Caption("0", new Vector3(0.55f, -0.43f, 0), 34, Color.white, 0.4f);
            Caption("30", new Vector3(1.45f, -0.43f, 0), 34, Color.white, 0.4f);
            var line = Gen.Line(axes.transform, "Curve", Cyan, 0.03f, 2, Mats.Line(Color.white));
            var dot = Gen.Prim(PrimitiveType.Sphere, "Marker", axes.transform, Vector3.zero, Vector3.one * 0.06f, Mats.Lit(Color.white, Color.white * 1.5f, 0.3f));
            var cycleNo = Caption("", new Vector3(0.9f, 0.6f, 0), 52, Cyan, 1.2f);
            const int N = 60;
            snd.Cue("riser");
            yield return Over(7f, k =>
            {
                int count = Mathf.Max(2, Mathf.RoundToInt(k * N));
                line.positionCount = count;
                Vector3 last = Vector3.zero;
                for (int i = 0; i < count; i++)
                {
                    float x = i / (N - 1f);
                    float y = 1f / (1f + Mathf.Exp(-(x - 0.55f) * 11f));
                    last = new Vector3(x * 0.9f, y * 0.95f, 0);
                    line.SetPosition(i, last);
                }
                dot.transform.localPosition = last;
                cycleNo.text = "CYCLE " + Mathf.RoundToInt(k * 30f);
                if (k > 0.5f) cap.text = "MORE DNA, MORE LIGHT";
                float glowK = Mathf.SmoothStep(0, 1, k);
                foreach (var m in tubes) { m.SetColor("_BaseColor", Color.Lerp(new Color(0.1f, 0.3f, 0.5f), new Color(0.2f, 0.9f, 1f), glowK)); m.SetColor("_EmissionColor", Color.Lerp(new Color(0.05f, 0.1f, 0.2f), new Color(0.2f, 0.9f, 1f) * 2.2f, glowK)); }
            });
            snd.Cue("sparkle");
            cap.text = "REAL-TIME PCR";
            yield return Wait(2.2f);
        }

        static readonly string[] AppImages = { "diagnostics", "genetic", "infectious", "cancer", "forensics", "research", "genomic" };
        static readonly string[] AppNames = { "MOLECULAR\nDIAGNOSTICS", "GENETIC\nTESTING", "INFECTIOUS\nDISEASE RESEARCH", "CANCER\nRESEARCH", "FORENSICS", "RESEARCH\nLABORATORIES", "GENOMIC\nANALYSIS" };

        IEnumerator MToday()
        {
            Say("today");
            const float W = 0.6f, Hh = 0.4f;
            for (int i = 0; i < AppNames.Length; i++)
            {
                int row = i < 4 ? 0 : 1, col = i < 4 ? i : i - 4;
                float x = row == 0 ? -1.05f + col * 0.7f : -0.7f + col * 0.7f;
                var g = Obj(AppNames[i].Replace("\n", " "), new Vector3(x, row == 0 ? 0.36f : -0.4f, 0));
                var plate = Mats.Lit(Theme.NavyMid, new Color(0.1f, 0.2f, 0.4f) * 0.5f, 0.5f);
                Gen.Box("Plate", g.transform, Vector3.zero, new Vector3(W + 0.04f, Hh + 0.04f, 0.03f), plate);
                AppPhoto(AppImages[i], g.transform, W, Hh);
                var cv = Ui.Canvas("Name", g.transform, new Vector2(620, 200), new Vector3(0, -0.34f, -0.03f));
                Ui.Label(cv.transform, AppNames[i], 46, Color.white, TextAnchor.MiddleCenter, new Vector2(620, 200), Vector2.zero, FontStyle.Bold, true);
                snd.Cue("pop");
                StartCoroutine(Pop(g.transform, 0.5f));
                yield return Wait(2.0f);
            }
            snd.Cue("chime");
            var cap = Caption("WHERE PCR IS USED TODAY", new Vector3(0, 0.82f, 0), 70, Cyan, 2.4f);
            yield return Wait(11f);
            cap.text = "NEW QUESTIONS: DETECT RNA, MEASURE DNA, BE MORE SPECIFIC";
            cap.fontSize = 52;
            yield return Wait(12f);
            cap.text = "DIFFERENT TYPES OF PCR, EACH FOR A PURPOSE";
            cap.fontSize = 62;
            yield return Wait(2f);
        }

        void AppPhoto(string file, Transform p, float w, float h)
        {
            var tex = Resources.Load<Texture2D>("LabAssets/Applications/" + file);
            if (tex == null) return;
            var mat = Mats.UnlitNew(Color.white);
            float texAspect = (float)tex.width / tex.height, cardAspect = w / h;
            var scale = texAspect > cardAspect ? new Vector2(cardAspect / texAspect, 1f) : new Vector2(1f, texAspect / cardAspect);
            var offset = (Vector2.one - scale) * 0.5f;
            mat.mainTexture = tex; mat.mainTextureScale = scale; mat.mainTextureOffset = offset;
            if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", tex); mat.SetTextureScale("_BaseMap", scale); mat.SetTextureOffset("_BaseMap", offset); }
            Gen.Prim(PrimitiveType.Quad, "Photo", p, new Vector3(0, 0, -0.02f), new Vector3(w, h, 1f), mat);
        }
    }
}
