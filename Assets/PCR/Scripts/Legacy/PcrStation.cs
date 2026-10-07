#if PCR_LEGACY
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PCR
{
    public class PcrStation : MonoBehaviour
    {
        public enum Kind { EndPoint, QPcr, RtPcr, Nested }

        public Kind Type;
        public int Index;
        public Color Accent = Ui.Cyan;
        public Action<PcrStation> Completed;
        public bool IsComplete { get; private set; }

        const float PcrModelYaw = 180f;

        static readonly string[] Titles = { "END-POINT PCR", "qPCR", "RT-PCR", "NESTED PCR" };
        static readonly string[] Subs =
        {
            "The classic approach. Amplification is assessed at the end of the reaction.",
            "Quantitative PCR. Monitors amplification in real time.",
            "Begins with RNA, converted to complementary DNA first.",
            "Two successive rounds of amplification for specificity.",
        };
        static readonly string[] NarrationIds = { "s5_endpoint", "s5_qpcr", "s5_rtpcr", "s5_nested" };

        Transform vis;
        Text caption, counter;
        HoloPanel card, quiz;
        HoloButton startBtn;
        readonly List<HoloButton> options = new List<HoloButton>();
        Material ledMat;
        bool running;
        bool narrationDone, visDone;

        public static PcrStation Create(Transform parent, Vector3 pos, Vector3 awayFromPlayer, Kind kind, int index, Color accent)
        {
            var go = new GameObject("Station_" + Titles[(int)kind]);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(awayFromPlayer, Vector3.up);
            var s = go.AddComponent<PcrStation>();
            s.Type = kind; s.Index = index; s.Accent = accent;
            s.BuildBench();
            return s;
        }

        void BuildBench()
        {
            var top = Mats.Lit(new Color(0.92f, 0.95f, 0.97f), null, 0.85f, 0.1f);
            var body = Mats.Lit(Theme.NavyMid, null, 0.4f, 0.2f);
            var steel = Mats.Lit(new Color(0.72f, 0.75f, 0.8f), null, 0.8f, 0.8f);
            Gen.Box("BenchTop", transform, new Vector3(0, 0.9f, 0), new Vector3(1.8f, 0.06f, 0.9f), top, true);
            Gen.Box("BenchBody", transform, new Vector3(0, 0.44f, 0.02f), new Vector3(1.7f, 0.88f, 0.8f), body, true);
            float yaw = transform.eulerAngles.y;
            ledMat = Mats.LitNew(Accent * 0.4f, Accent * 2f, 0.3f);
            var pcr = LabProps.Spawn("PCRModels/PCR", transform, transform.TransformPoint(new Vector3(0, 0.93f, 0.05f)), yaw + PcrModelYaw, null, 1f, 0.62f);
            if (pcr == null)
            {
                Gen.Box("CyclerBase", transform, new Vector3(0, 1.0f, 0.05f), new Vector3(0.62f, 0.14f, 0.42f), steel);
                Gen.Box("CyclerLid", transform, new Vector3(0, 1.11f, 0.07f), new Vector3(0.6f, 0.05f, 0.4f), body);
                Gen.Box("CyclerLED", transform, new Vector3(0, 1.0f, -0.165f), new Vector3(0.4f, 0.025f, 0.01f), ledMat);
            }
            var tubeMat = Mats.Glass(new Color(0.8f, 0.95f, 1f, 0.35f));
            var liquid = Mats.Lit(Accent * 0.5f, Accent * 0.9f, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                float x = 0.55f + i * 0.07f;
                Gen.Prim(PrimitiveType.Cylinder, "Tube", transform, new Vector3(x, 0.99f, -0.1f), new Vector3(0.04f, 0.06f, 0.04f), tubeMat);
                Gen.Prim(PrimitiveType.Cylinder, "Liquid", transform, new Vector3(x, 0.96f, -0.1f), new Vector3(0.028f, 0.025f, 0.028f), liquid);
            }
            LabProps.Spawn("machine_centrifuge", transform, transform.TransformPoint(new Vector3(-0.62f, 0.93f, 0.05f)), yaw + 180f, Color.white * 1.1f, 0.9f);
            LabProps.Spawn("bottle_micropipet", transform, transform.TransformPoint(new Vector3(0.28f, 0.93f, -0.3f)), yaw, Accent * 1.4f, 1.7f);
            LabProps.Spawn("bottle_glassware_erlenmeyer_flask_small", transform, transform.TransformPoint(new Vector3(-0.3f, 0.93f, -0.28f)), yaw, Accent);
            LabProps.Spawn("bottle_glassware_vial_medium", transform, transform.TransformPoint(new Vector3(0.0f, 0.93f, -0.3f)), yaw, new Color(1f, 0.9f, 0.3f));

            vis = new GameObject("Hologram").transform;
            vis.SetParent(transform, false);
            vis.localPosition = new Vector3(0, 2.35f, 0);
            vis.gameObject.SetActive(false);
        }

        public Vector3 FrontPoint => transform.position - transform.forward * 2.3f;
        public bool CanStart => startBtn != null && !running;
        public bool QuizReady => quiz != null && !IsComplete && options.Count > 0;
        public void DemoStart() { if (startBtn != null) startBtn.Press(); }
        public void DemoAnswer() { if (QuizReady) options[PcrText.PcrTypeQuizzes[(int)Type].Correct].Press(); }

        public void Appear()
        {
            Sfx.Appear(transform.position + Vector3.up * 2f);
            card =HoloPanel.Create(transform, new Vector3(0, 3.35f, 0), new Vector2(1250, 330),
                $"{Index + 1}   {Titles[(int)Type]}", Subs[(int)Type], Accent, 64, 32);
            card.gameObject.AddComponent<Bob>().Amplitude = 0.04f;
            startBtn = HoloButton.Create(transform, new Vector3(0, 1.2f, -0.55f), "START", Accent, new Vector2(0.6f, 0.2f), 44);
            startBtn.Pressed += Begin;
        }

        void Begin()
        {
            if (running || IsComplete) return;
            running = true;
            startBtn.SetInteractable(false);
            startBtn.gameObject.SetActive(false);
            vis.gameObject.SetActive(true);
            BuildStage();
            narrationDone = visDone = false;
            NarrationManager.Instance.SetObjective($"Watch: {Titles[(int)Type]}");
            NarrationManager.Instance.Say(NarrationIds[(int)Type], () => { narrationDone = true; TryQuiz(); });
            StartCoroutine(RunVisual());
        }

        void TryQuiz()
        {
            if (narrationDone && visDone) ShowQuiz();
        }

        Canvas stageCanvas;

        void BuildStage()
        {
            var back = Gen.Prim(PrimitiveType.Quad, "Backing", vis, new Vector3(0, 0, 0.02f), new Vector3(1.6f, 0.95f, 1f),
                Mats.UnlitColor(new Color(0.01f, 0.05f, 0.09f, 0.78f), true));
            Gen.Box("FrameTop", vis, new Vector3(0, 0.475f, 0.0f), new Vector3(1.6f, 0.012f, 0.012f), Mats.Lit(Accent * 0.4f, Accent * 1.6f));
            Gen.Box("FrameBottom", vis, new Vector3(0, -0.475f, 0.0f), new Vector3(1.6f, 0.012f, 0.012f), Mats.Lit(Accent * 0.4f, Accent * 1.6f));
            stageCanvas = Ui.Canvas("StageUI", vis, new Vector2(1600, 950), new Vector3(0, 0, -0.005f));
            caption = Ui.Label(stageCanvas.transform, "", 30, Color.white, TextAnchor.MiddleCenter, new Vector2(1500, 90), new Vector2(0, -400), FontStyle.Italic);
            counter = Ui.Label(stageCanvas.transform, "", 44, Accent, TextAnchor.MiddleCenter, new Vector2(900, 70), new Vector2(0, 360), FontStyle.Bold);
        }

        void Cap(string s) => caption.text = s;

        Text Txt(string s, int size, Color c, Vector2 px, TextAnchor a = TextAnchor.MiddleCenter, float w = 500)
            => Ui.Label(stageCanvas.transform, s, size, c, a, new Vector2(w, size * 1.6f), px, FontStyle.Bold);

        Transform Bar(string name, Color c, Vector3 pos, Vector3 size, float glow = 1.6f)
            => Gen.Box(name, vis, pos, size, Mats.Lit(c * 0.4f, c * glow, 0.3f)).transform;

        static IEnumerator Anim(float dur, Action<float> f)
        {
            for (float t = 0; t < dur; t += Time.deltaTime) { f(t / dur); yield return null; }
            f(1f);
        }

        void Led(float intensity) { if (ledMat != null) ledMat.SetColor("_EmissionColor", Accent * (1f + intensity * 3f)); }

        IEnumerator RunVisual()
        {
            switch (Type)
            {
                case Kind.EndPoint: yield return VisEndPoint(); break;
                case Kind.QPcr: yield return VisQPcr(); break;
                case Kind.RtPcr: yield return VisRtPcr(); break;
                case Kind.Nested: yield return VisNested(); break;
            }
            visDone = true;
            TryQuiz();
        }

        IEnumerator VisEndPoint()
        {
            Cap("The reaction runs through its cycles...");
            var frame = Bar("Frame", Accent * 0.5f, new Vector3(0, 0.1f, -0.01f), new Vector3(1.1f, 0.09f, 0.01f), 0.6f);
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(vis, false); pivot.localPosition = new Vector3(-0.55f, 0.1f, -0.02f);
            var fill = Gen.Box("Fill", pivot, new Vector3(0.5f, 0, 0), new Vector3(1f, 0.07f, 0.01f), Mats.Lit(Accent * 0.5f, Accent * 2.2f)).transform;
            fill.localScale = new Vector3(0.0001f, 0.07f, 0.01f);
            yield return Anim(7f, u =>
            {
                int c = Mathf.RoundToInt(u * 30f);
                counter.text = $"CYCLE {c} / 30";
                float wFill = Mathf.Max(0.0001f, 1.1f * u);
                fill.localScale = new Vector3(wFill, 0.07f, 0.01f);
                fill.localPosition = new Vector3(wFill * 0.5f, 0, 0);
                Led(0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
            });
            Led(0f);
            Cap("Only now, at the END of the reaction, is the result checked.");
            yield return new WaitForSeconds(1.6f);
            frame.gameObject.SetActive(false); pivot.gameObject.SetActive(false);

            var gel = new GameObject("Gel").transform; gel.SetParent(vis, false); gel.localPosition = new Vector3(0, 0.02f, -0.015f);
            Gen.Box("GelBody", gel, Vector3.zero, new Vector3(0.9f, 0.5f, 0.01f), Mats.Lit(new Color(0.05f, 0.08f, 0.1f), null, 0.2f));
            float[] ladderY = { -0.18f, -0.08f, 0.0f, 0.09f, 0.18f };
            for (int i = 0; i < ladderY.Length; i++)
                Gen.Box("L", gel, new Vector3(-0.28f, ladderY[i], -0.008f), new Vector3(0.2f, 0.018f, 0.004f), Mats.Lit(Color.white * 0.6f, Color.white * 1.2f));
            Gen.Box("Sample", gel, new Vector3(0.0f, 0.0f, -0.008f), new Vector3(0.2f, 0.03f, 0.004f), Mats.Lit(Accent * 0.5f, Accent * 3.2f));
            Gen.Box("SampleFaint", gel, new Vector3(0.0f, -0.17f, -0.008f), new Vector3(0.2f, 0.012f, 0.004f), Mats.Lit(Accent * 0.2f, Accent * 0.8f));
            Txt("LADDER", 22, Color.white, new Vector2(-280, -285), TextAnchor.MiddleCenter, 220);
            Txt("SAMPLE", 22, Accent, new Vector2(0, -285), TextAnchor.MiddleCenter, 220);
            Txt("NEGATIVE", 22, Color.white, new Vector2(280, -285), TextAnchor.MiddleCenter, 220);
            counter.text = "RESULT: BAND AT TARGET SIZE";
            yield return Anim(1.2f, u => gel.localScale = Vector3.one * Mathf.SmoothStep(0, 1, u));
            Cap("A clear band at the expected size shows the target was amplified.");
            ProcAudio.PlayAt(ProcAudio.Chime, vis.position, 0.5f);
            yield return new WaitForSeconds(2.2f);
        }

        IEnumerator VisQPcr()
        {
            Cap("Fluorescence is measured after every cycle, in real time.");
            const int n = 80;
            float x0 = -0.55f, w = 1.1f, y0 = -0.28f, h = 0.58f;
            Bar("AxisX", Color.white * 0.8f, new Vector3(0, y0 - 0.01f, -0.01f), new Vector3(w, 0.006f, 0.005f), 0.8f);
            Bar("AxisY", Color.white * 0.8f, new Vector3(x0 - 0.01f, y0 + h * 0.5f, -0.01f), new Vector3(0.006f, h, 0.005f), 0.8f);
            Txt("FLUORESCENCE", 20, Color.white, new Vector2(-640, 40), TextAnchor.MiddleCenter, 300).transform.localRotation = Quaternion.Euler(0, 0, 90);
            Txt("CYCLE", 22, Color.white, new Vector2(0, -330), TextAnchor.MiddleCenter, 300);

            float thrY = y0 + h * 0.32f;
            Bar("Threshold", new Color(1f, 0.85f, 0.25f), new Vector3(0, thrY, -0.012f), new Vector3(w, 0.004f, 0.004f), 1.4f);
            Txt("threshold", 20, new Color(1f, 0.85f, 0.25f), new Vector2(480, (thrY * 1000f) + 24), TextAnchor.MiddleCenter, 220);

            var line = Gen.Line(vis, "Curve", Accent, 0.014f, 2, Mats.Line(Color.white));
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float c = i / (float)(n - 1) * 40f;
                float f = 1f / (1f + Mathf.Exp(-(c - 24f) / 2.4f));
                pts[i] = new Vector3(x0 + w * i / (n - 1f), y0 + h * f, -0.014f);
            }
            var dot = Gen.Prim(PrimitiveType.Quad, "Dot", vis, Vector3.zero, Vector3.one * 0.07f, Mats.Glow(Color.white));
            dot.transform.localRotation = Quaternion.identity;
            yield return Anim(8f, u =>
            {
                int count = Mathf.Max(2, Mathf.RoundToInt(u * n));
                line.positionCount = count;
                for (int i = 0; i < count; i++) line.SetPosition(i, pts[i]);
                dot.transform.localPosition = pts[count - 1];
                int cyc = Mathf.RoundToInt(u * 40f);
                counter.text = $"CYCLE {cyc}";
                Led(0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
            });
            Led(0f);
            int ct = 0; while (ct < n - 1 && pts[ct].y < thrY) ct++;
            Bar("CtLine", new Color(1f, 0.85f, 0.25f), new Vector3(pts[ct].x, (thrY + y0) * 0.5f, -0.012f), new Vector3(0.004f, thrY - y0, 0.004f), 1.4f);
            Txt("Ct", 30, new Color(1f, 0.85f, 0.25f), new Vector2(pts[ct].x * 1000f, -250), TextAnchor.MiddleCenter, 140);
            ProcAudio.PlayAt(ProcAudio.Chime, vis.position, 0.5f);
            counter.text = "EARLIER Ct = MORE STARTING DNA";
            Cap("The cycle where the curve crosses the threshold (Ct) reveals how much target DNA there was.");
            yield return new WaitForSeconds(3.2f);
        }

        DnaHelix MiniHelix(string name, Vector3 pos)
        {
            var model = Resources.Load<GameObject>("PCRModels/DNA_Ladder_Whole");
            if (model != null) return DnaHelix.WrapModel(vis, name, pos, model, 0.44f);
            return DnaHelix.Create(vis, name, pos, 12, 0.07f, 0.04f, 36f, 3, 1.2f);
        }

        IEnumerator VisRtPcr()
        {
            Cap("It starts with RNA.");
            var orange = new Color(1f, 0.6f, 0.2f);
            var rna = Gen.Line(vis, "RNA", orange, 0.016f, 60, Mats.Line(Color.white));
            for (int i = 0; i < 60; i++)
            {
                float x = -0.68f + 0.4f * i / 59f;
                rna.SetPosition(i, new Vector3(x, 0.12f + 0.05f * Mathf.Sin(i * 0.8f), -0.014f));
            }
            Txt("RNA", 34, orange, new Vector2(-480, 170), TextAnchor.MiddleCenter, 200);
            rna.widthMultiplier = 0f;
            yield return Anim(1.2f, u => rna.widthMultiplier = u);

            Cap("Reverse transcriptase converts the RNA into complementary DNA (cDNA).");
            Bar("Arrow", Accent, new Vector3(-0.1f, 0.12f, -0.012f), new Vector3(0.38f, 0.014f, 0.006f), 2f);
            Bar("ArrowHead", Accent, new Vector3(0.1f, 0.12f, -0.012f), new Vector3(0.03f, 0.05f, 0.006f), 2f);
            Txt("Reverse\ntranscriptase", 22, Accent, new Vector2(-100, 215), TextAnchor.MiddleCenter, 320);
            var cdna = MiniHelix("cDNA", new Vector3(0.18f, -0.22f, -0.02f));
            cdna.SpinDegPerSec = 30f; cdna.transform.localScale = Vector3.zero;
            Txt("cDNA", 34, Accent, new Vector2(180, 175), TextAnchor.MiddleCenter, 200);
            yield return Anim(1.6f, u => cdna.transform.localScale = Vector3.one * Mathf.SmoothStep(0, 1, u));
            ProcAudio.PlayAt(ProcAudio.Chime, vis.position, 0.4f);
            yield return new WaitForSeconds(2f);

            Cap("Now standard PCR amplifies the cDNA, copy after copy.");
            counter.text = "x1";
            var copies = new List<DnaHelix>();
            float[] xs = { 0.36f, 0.52f, 0.68f };
            for (int i = 0; i < xs.Length; i++)
            {
                var c = MiniHelix("copy" + i, new Vector3(xs[i], -0.22f, -0.02f));
                c.SpinDegPerSec = 30f; c.transform.localScale = Vector3.zero;
                copies.Add(c);
            }
            string[] labels = { "x2", "x4", "x8" };
            for (int i = 0; i < copies.Count; i++)
            {
                counter.text = labels[i];
                Led(1f);
                yield return new WaitForSeconds(0.5f);
                var cp = copies[i];
                yield return Anim(0.8f, u => cp.transform.localScale = Vector3.one * Mathf.SmoothStep(0, 1, u));
                ProcAudio.PlayAt(ProcAudio.Blip, vis.position, 0.5f);
            }
            Led(0f);
            counter.text = "RNA  >  cDNA  >  MILLIONS OF COPIES";
            yield return new WaitForSeconds(2.2f);
        }

        IEnumerator VisNested()
        {
            var grey = new Color(0.7f, 0.75f, 0.8f);
            var green = new Color(0.3f, 1f, 0.55f);
            var orange = new Color(1f, 0.6f, 0.2f);
            var red = new Color(1f, 0.3f, 0.35f);

            Cap("The template DNA, with our target region in the middle.");
            Bar("Template", grey, new Vector3(0, 0.3f, -0.01f), new Vector3(1.3f, 0.025f, 0.006f), 0.6f);
            Bar("Target", green, new Vector3(0, 0.3f, -0.012f), new Vector3(0.22f, 0.04f, 0.006f), 1.8f);
            Txt("TARGET", 22, green, new Vector2(0, 360), TextAnchor.MiddleCenter, 200);
            yield return new WaitForSeconds(2.2f);

            counter.text = "ROUND 1  |  OUTER PRIMERS";
            Cap("Round 1: outer primers amplify a larger region, but some unwanted products creep in.");
            Bar("OuterL", orange, new Vector3(-0.47f, 0.3f, -0.014f), new Vector3(0.05f, 0.07f, 0.006f), 2f);
            Bar("OuterR", orange, new Vector3(0.47f, 0.3f, -0.014f), new Vector3(0.05f, 0.07f, 0.006f), 2f);
            var r1 = Bar("Round1", orange, new Vector3(0, 0.08f, -0.01f), new Vector3(0.94f, 0.04f, 0.006f), 1.5f);
            var junk = new List<Transform>
            {
                Bar("Junk1", red, new Vector3(-0.38f, 0.0f, -0.01f), new Vector3(0.22f, 0.03f, 0.006f), 1.5f),
                Bar("Junk2", red, new Vector3(0.3f, -0.02f, -0.01f), new Vector3(0.3f, 0.03f, 0.006f), 1.5f),
                Bar("Junk3", red, new Vector3(-0.05f, -0.04f, -0.01f), new Vector3(0.14f, 0.03f, 0.006f), 1.5f),
            };
            r1.localScale = new Vector3(0.0001f, 0.04f, 0.006f);
            foreach (var j in junk) j.gameObject.SetActive(false);
            Led(1f);
            yield return Anim(2f, u => r1.localScale = new Vector3(0.94f * u + 0.0001f, 0.04f, 0.006f));
            foreach (var j in junk) { j.gameObject.SetActive(true); ProcAudio.PlayAt(ProcAudio.Buzz, vis.position, 0.15f); yield return new WaitForSeconds(0.35f); }
            yield return new WaitForSeconds(1.5f);

            counter.text = "ROUND 2  |  INNER PRIMERS";
            Cap("Round 2: inner primers sit inside the first product, so only the true target is copied.");
            foreach (var j in junk) j.gameObject.SetActive(false);
            Bar("InnerL", green, new Vector3(-0.13f, 0.08f, -0.014f), new Vector3(0.04f, 0.07f, 0.006f), 2f);
            Bar("InnerR", green, new Vector3(0.13f, 0.08f, -0.014f), new Vector3(0.04f, 0.07f, 0.006f), 2f);
            var r2 = Bar("Round2", green, new Vector3(0, -0.14f, -0.01f), new Vector3(0.26f, 0.05f, 0.006f), 2.4f);
            r2.localScale = new Vector3(0.0001f, 0.05f, 0.006f);
            yield return Anim(2f, u => r2.localScale = new Vector3(0.26f * u + 0.0001f, 0.05f, 0.006f));
            Led(0f);
            ProcAudio.PlayAt(ProcAudio.Chime, vis.position, 0.5f);
            counter.text = "RESULT: HIGHLY SPECIFIC";
            yield return new WaitForSeconds(2.6f);
        }

        void ShowQuiz()
        {
            var q = PcrText.PcrTypeQuizzes[(int)Type];
            NarrationManager.Instance.SetObjective("Answer the quick check");
            quiz = HoloPanel.Create(transform, new Vector3(0, 1.55f, -0.85f), new Vector2(1300, 700), "QUICK CHECK", q.Question, Accent, 38, 34);
            options.Clear();
            for (int i = 0; i < q.Options.Length; i++)
            {
                int idx = i;
                var b = HoloButton.Create(transform, new Vector3(0, 1.58f - i * 0.17f, -0.9f), q.Options[i], new Color(0.2f, 0.55f, 0.85f), new Vector2(1.15f, 0.14f), 30);
                b.Pressed += () => OnAnswer(idx);
                options.Add(b);
            }
        }

        void OnAnswer(int idx)
        {
            var q = PcrText.PcrTypeQuizzes[(int)Type];
            if (IsComplete) return;
            if (idx == q.Correct)
            {
                IsComplete = true;
                Sfx.Correct(transform.position + Vector3.up * 1.5f);
                options[idx].SetColor(new Color(0.2f, 1f, 0.5f));
                foreach (var o in options) o.SetInteractable(false);
                quiz.Set("CORRECT", q.Explain);
                StartCoroutine(Finish());
            }
            else
            {
                Sfx.Wrong(transform.position + Vector3.up * 1.5f);
                options[idx].SetColor(new Color(1f, 0.3f, 0.3f));
                options[idx].SetInteractable(false);
                quiz.Set("NOT QUITE", "Have another look at the hologram and try again.");
            }
        }

        IEnumerator Finish()
        {
            yield return new WaitForSeconds(3.2f);
            foreach (var o in options) if (o != null) Destroy(o.gameObject);
            if (quiz != null) quiz.Dismiss();
            if (card != null) card.Accent.color = new Color(0.25f, 1f, 0.5f);
            if (card != null) card.Set("DONE   " + Titles[(int)Type], "Station complete.");
            var tick = Gen.Prim(PrimitiveType.Quad, "Tick", transform, new Vector3(0.0f, 1.2f, -0.55f), Vector3.one * 0.35f, Mats.Glow(new Color(0.3f, 1f, 0.5f, 1f)));
            tick.AddComponent<BillboardY>();
            vis.gameObject.SetActive(false);
            Completed?.Invoke(this);
        }
    }
}
#endif
