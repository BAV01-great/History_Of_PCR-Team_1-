using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PCR
{
    /// <summary>
    /// The visual beat of each timeline milestone. Each Play(i) is a coroutine run by the
    /// JourneyController: it builds the visuals under the stage's VisualRoot, times them to the sound-design offsets, queues the spoken lines,
    /// and returns when the beat is over. Stop() removes everything (REPLAY / PREVIOUS / leaving the timeline).
    /// Local axes under VisualRoot: x right, y up, z away from the player.
    /// </summary>
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

        // ------------------------------------------------------------------ helpers
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

        /// <summary>One of the team's models, centred on a point under VisualRoot.</summary>
        GameObject Model(string res, Vector3 local, float fit, Color? tint = null)
        {
            var g = LabProps.Spawn(res, R, R.TransformPoint(local), 0f, tint, 1f, fit);
            if (g == null) return null;
            var b = LabUtil.BoundsOf(g);
            g.transform.position += R.TransformPoint(local) - b.center;
            spawned.Add(g);
            return g;
        }

        // ------------------------------------------------------------------ 1869: cell, nucleus, DNA
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
            var dna = DnaHelix.Create(cell.transform, "NucleinDNA", new Vector3(0.03f, -0.1f, 0f), 11, 0.05f, 0.03f, 36f, 3, 1.4f);
            dna.SpinDegPerSec = 40f;
            snd.Cue("pop");
            StartCoroutine(Pop(cell.transform, 0.7f));
            var lbl = Caption("A CELL", new Vector3(0, -0.85f, 0), 70, Cyan, 2f);
            yield return Wait(0.5f);
            Say("m1869");                                   // 0:02 VO
            yield return Wait(1.8f);
            lbl.text = "THE NUCLEUS OPENS";
            snd.Cue("bubble");
            yield return Over(2.6f, k =>                    // the nucleus opens
            {
                nucleus.localScale = Vector3.one * Mathf.Lerp(0.42f, 0.62f, k);
                SetA(nucM, Mathf.Lerp(0.5f, 0.08f, k));
            });
            lbl.text = "NUCLEIN, LATER KNOWN AS DNA";
            snd.Cue("pluck");
            var from = dna.transform.localPosition;
            yield return Over(3.2f, k =>                    // DNA lifts out and grows
            {
                float e = Mathf.SmoothStep(0, 1, k);
                dna.transform.localPosition = Vector3.Lerp(from, new Vector3(0.1f, 0.65f, -0.3f), e);
                dna.transform.localScale = Vector3.one * Mathf.Lerp(1f, 3.4f, e);
                dna.SetGlow(Mathf.Lerp(1f, 2f, e));
                SetA(memM, Mathf.Lerp(0.28f, 0.06f, e));
            });
            yield return Wait(3f);
        }

        // ------------------------------------------------------------------ 1953: flat strand twists into a double helix
        IEnumerator M1953()
        {
            yield return Wait(1.5f);
            var h = MorphHelix.Create(R, "Helix1953", new Vector3(0, -0.8f, 0), 28, 0.2f, 0.06f, 11, 1.1f);
            h.Set(0f, 0f, 1f, 0f);
            spawned.Add(h.gameObject);
            snd.Cue("pop");
            StartCoroutine(Pop(h.transform, 0.6f));
            var lbl = Caption("A FLAT STRAND", new Vector3(0, 0.9f, 0), 74, Cyan, 2f);
            Say("m1953");                                    // 0:02 VO
            yield return Wait(2.2f);
            snd.Cue("swirl");
            lbl.text = "IT TWISTS";
            yield return Over(6f, k =>                       // flat ladder twists into the double helix
            {
                float e = Mathf.SmoothStep(0, 1, k);
                h.SetTwist(36f * e);
                h.transform.localRotation = Quaternion.Euler(0, 160f * e, 0);
            });
            lbl.text = "THE DOUBLE HELIX";
            snd.Cue("chime");
            lbl.color = Color.white;
            var glow = Glow(Cyan, 1f);
            yield return Over(2f, k => h.transform.localRotation = Quaternion.Euler(0, 160f + 90f * k, 0));
            yield return Wait(1.2f);
        }

        // ------------------------------------------------------------------ 1983: target, primers, duplication, then the principle
        IEnumerator M1983()
        {
            var h = MorphHelix.Create(R, "Helix1983", new Vector3(0, -0.85f, 0), 30, 0.2f, 0.057f, 21, 1.0f);
            spawned.Add(h.gameObject);
            var gold = Glow(Gold, 1.6f);
            StartCoroutine(Pop(h.transform, 0.5f));
            Say("m1983_a");                                  // 0:00 VO
            snd.Cue("idea");
            var cap = Caption("ONE CHOSEN SEQUENCE", new Vector3(0, 0.92f, 0), 72, Gold, 2.2f);
            yield return Wait(1.2f);
            h.Highlight(11, 19, gold);                       // the selected DNA region highlights
            yield return Wait(5.5f);

            // 0:09 two primers appear around the target, then the target duplicates
            cap.text = "TWO PRIMERS FRAME THE TARGET";
            var primer = Glow(Magenta, 1.6f);
            var p1 = Primer(h.BeadA(9) + Vector3.left * 0.8f, 5, primer); var p2 = Primer(h.BeadB(21) + Vector3.right * 0.8f, 5, primer);
            yield return Over(1.4f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                p1.position = Vector3.Lerp(h.BeadA(9) + Vector3.left * 0.8f, h.BeadA(9) + Vector3.left * 0.06f, e);
                p2.position = Vector3.Lerp(h.BeadB(21) + Vector3.right * 0.8f, h.BeadB(21) + Vector3.right * 0.06f, e);
            });
            snd.Cue("click");
            yield return Wait(0.2f);
            snd.Cue("pop");
            var copy = Obj("TargetCopy", new Vector3(0.75f, 0.1f, 0));
            for (int i = 11; i <= 19; i++)
            {
                Gen.Prim(PrimitiveType.Sphere, "Bead", copy.transform, new Vector3(-0.07f + (i % 2) * 0.14f, (i - 15) * 0.057f, 0), Vector3.one * 0.05f, gold);
            }
            StartCoroutine(Pop(copy.transform, 0.5f));
            yield return Wait(1.5f);

            // 0:12 the principle animation: separate, bind, build, repeat
            Say("m1983_b");
            Destroy(copy); Destroy(p1.gameObject); Destroy(p2.gameObject);
            cap.text = "";
            var step = Caption("", new Vector3(0, 0.92f, 0), 78, Color.white, 2.4f);
            var temp = Caption("", new Vector3(1.0f, 0.55f, 0), 100, Red, 1.2f);
            var root = h.transform;
            var basePos = root.localPosition;

            // 1. DENATURATION: strands separate
            step.text = "1   DENATURATION";   temp.text = "95 °C";   temp.color = Red;
            snd.Cue("split");
            yield return Over(2.6f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                h.SetSeparation(0.42f * e, 1f - e);
                root.localPosition = basePos + new Vector3(Mathf.Sin(k * 60f) * 0.01f * (1f - k), 0, 0);   // heat shake
            });
            root.localPosition = basePos;
            yield return Wait(0.3f);

            // 2. ANNEALING: primers bind
            step.text = "2   ANNEALING";   temp.text = "55 °C";   temp.color = Blue;
            var q1 = Primer(h.BeadA(10) + Vector3.left * 0.9f, 5, primer); var q2 = Primer(h.BeadB(22) + Vector3.right * 0.9f, 5, primer);
            yield return Over(1.8f, k =>
            {
                float e = Mathf.SmoothStep(0, 1, k);
                q1.position = Vector3.Lerp(h.BeadA(10) + Vector3.left * 0.9f, h.BeadA(10) + Vector3.left * 0.045f, e);
                q2.position = Vector3.Lerp(h.BeadB(22) + Vector3.right * 0.9f, h.BeadB(22) + Vector3.right * 0.045f, e);
            });
            snd.Cue("click");
            yield return Wait(0.5f);

            // 3. EXTENSION: polymerase builds new DNA
            step.text = "3   EXTENSION";   temp.text = "72 °C";   temp.color = Orange;
            var taq = Model("PCRModels/Taq", Vector3.zero, 0.16f, Orange * 1.2f);
            Vector3 taqStart = h.BeadA(11) + Vector3.left * 0.12f;
            if (taq != null) taq.transform.position = taqStart;
            snd.Cue("ticks");
            yield return Over(2.8f, k =>
            {
                h.SetNewProgress(k);
                if (taq != null) taq.transform.position = Vector3.Lerp(taqStart, h.BeadA(29) + Vector3.left * 0.12f, k);
            });
            if (taq != null) Destroy(taq);
            Destroy(q1.gameObject); Destroy(q2.gameObject);
            yield return Wait(0.3f);

            // 4. REPEAT: two copies, then many
            step.text = "4   REPEAT";   temp.text = "×2  ×4  ×8";   temp.color = Cyan;
            h.gameObject.SetActive(false);
            snd.Cue("sparkle");
            var copies = new List<GameObject>();
            int n = 1; float[] xs = { 0 };
            for (int round = 1; round <= 3; round++)
            {
                n *= 2;
                foreach (var g in copies) Destroy(g);
                copies.Clear();
                for (int c = 0; c < n; c++)
                {
                    float x = (c - (n - 1) / 2f) * Mathf.Min(0.42f, 1.7f / n);
                    var d = DnaHelix.Create(R, "Copy", new Vector3(x, -0.6f, 0), 13, 0.09f, 0.075f, 36f, 100 + c, 1.2f);
                    d.SpinDegPerSec = 30f;
                    spawned.Add(d.gameObject);
                    copies.Add(d.gameObject);
                    StartCoroutine(Pop(d.transform, 0.35f));
                }
                snd.Cue("pop");
                temp.text = "×" + n;
                yield return Wait(1.1f);
            }
            snd.Cue("ticks");
            yield return Wait(2.2f);
        }

        /// <summary>A short primer: a row of magenta beads (world position of its first bead).</summary>
        Transform Primer(Vector3 worldPos, int beads, Material m)
        {
            var g = new GameObject("Primer");
            g.transform.SetParent(R, true);
            g.transform.position = worldPos;
            for (int i = 0; i < beads; i++)
                Gen.Prim(PrimitiveType.Sphere, "B", g.transform, new Vector3(0, i * 0.045f, 0), Vector3.one * 0.05f, m);
            spawned.Add(g);
            return g.transform;
        }

        // ------------------------------------------------------------------ 1985: published; the catch
        IEnumerator M1985()
        {
            var journal = Obj("Journal", new Vector3(0, -0.2f, 0));
            var cover = Mats.Lit(Theme.NavyMid, null, 0.4f);
            var page = Mats.Lit(new Color(0.96f, 0.94f, 0.88f), null, 0.3f);
            Gen.Box("Cover", journal.transform, new Vector3(0, -0.02f, 0), new Vector3(1.5f, 0.03f, 1.05f), cover);
            Gen.Box("PageL", journal.transform, new Vector3(-0.37f, 0.002f, 0), new Vector3(0.72f, 0.02f, 0.98f), page);
            Gen.Box("PageR", journal.transform, new Vector3(0.37f, 0.002f, 0), new Vector3(0.72f, 0.02f, 0.98f), page);
            journal.transform.localRotation = Quaternion.Euler(-68f, 0f, 0f);       // tilted up towards the player
            var cv = Ui.Canvas("PageText", journal.transform, new Vector2(1400, 900), new Vector3(0, 0.016f, 0));
            cv.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cv.transform.localScale = Vector3.one * 0.001f;
            Ui.Label(cv.transform, "SCIENCE · 1985", 52, new Color(0.35f, 0.2f, 0.2f), TextAnchor.UpperCenter, new Vector2(1300, 80), new Vector2(0, 380), FontStyle.Bold);
            Ui.Label(cv.transform, "Enzymatic amplification of β-globin genomic sequences", 48, new Color(0.1f, 0.1f, 0.15f), TextAnchor.UpperCenter, new Vector2(1250, 150), new Vector2(0, 290), FontStyle.Bold);
            Ui.Label(cv.transform, "PCR", 120, new Color(0.1f, 0.4f, 0.8f), TextAnchor.MiddleCenter, new Vector2(1000, 200), new Vector2(0, 60), FontStyle.Bold);
            snd.Cue("rustle");
            StartCoroutine(Pop(journal.transform, 0.8f));
            Say("m1985_a");                                  // 0:00 VO
            yield return Wait(1.3f);
            // one DNA becomes many (on the page)
            var one = DnaHelix.Create(journal.transform, "One", new Vector3(-0.38f, 0.02f, -0.22f), 9, 0.05f, 0.035f, 36f, 4, 1.1f);
            one.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            one.SpinDegPerSec = 40f;
            snd.Cue("pop");
            yield return Wait(1.6f);
            for (int i = 0; i < 3; i++)
            {
                var d = DnaHelix.Create(journal.transform, "Many", new Vector3(0.18f + i * 0.2f, 0.02f, -0.22f), 9, 0.05f, 0.035f, 36f, 5 + i, 1.1f);
                d.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                d.SpinDegPerSec = 40f;
                StartCoroutine(Pop(d.transform, 0.4f));
                snd.Cue("pop");
                yield return Wait(0.35f);
            }
            snd.Cue("sparkle");
            yield return Wait(4.2f);

            // 0:10 the catch: heat splits the DNA, the enzyme falters
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

        // ------------------------------------------------------------------ 1988: the old enzyme breaks, Taq survives, cycles accelerate
        IEnumerator M1988()
        {
            // thermometer and the old enzyme
            var gauge = Obj("Gauge", new Vector3(-1.0f, -0.2f, 0));
            Gen.Box("Tube", gauge.transform, Vector3.zero, new Vector3(0.12f, 1.3f, 0.06f), Glass(Color.white, 0.25f));
            var hot = Mats.LitNew(Red * 0.7f, Red * 1.4f, 0.5f);
            var level = Gen.Box("Level", gauge.transform, new Vector3(0, -0.6f, 0), new Vector3(0.07f, 1f, 0.04f), hot).transform;
            level.localScale = new Vector3(0.07f, 0.02f, 0.04f);
            var tempText = Caption("25 °C", new Vector3(-1.0f, -1.05f, 0), 70, Red, 1.2f);
            var oldE = Model("PCRModels/Taq_Broken", new Vector3(0.1f, 0.05f, 0), 0.4f, new Color(0.65f, 0.75f, 1f));
            var cap = Caption("EARLIER POLYMERASE + HEAT", new Vector3(0, 0.92f, 0), 68, Red, 2.4f);
            yield return Over(2f, k =>
            {
                float c = Mathf.Lerp(25f, 95f, k);
                level.localScale = new Vector3(0.07f, Mathf.Lerp(0.02f, 1.2f, k), 0.04f);
                level.localPosition = new Vector3(0, -0.65f + level.localScale.y * 0.5f, 0);
                tempText.text = Mathf.RoundToInt(c) + " °C";
                if (oldE != null) oldE.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 50f) * 10f * k);
            });
            snd.Cue("sizzle");
            yield return Wait(0.5f);
            snd.Cue("crack");
            cap.text = "THE POLYMERASE IS DESTROYED";
            if (oldE != null)                                    // breaks apart and fades into pieces
            {
                var dir = new[] { Vector3.left, Vector3.right, Vector3.up, Vector3.down };
                yield return Over(1.2f, k =>
                {
                    oldE.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.001f, k * k);
                });
                Destroy(oldE);
            }
            for (int k = 0; k < 8; k++)
            {                                                    // shards
                var shard = Gen.Prim(PrimitiveType.Cube, "Shard", R, new Vector3(0.1f, 0.05f, 0), Vector3.one * 0.05f, Mats.Lit(new Color(0.5f, 0.55f, 0.7f), null, 0.4f));
                spawned.Add(shard);
                StartCoroutine(Fly(shard.transform, UnityEngine.Random.onUnitSphere * 0.9f, 1.4f));
            }
            yield return Wait(1.2f);                              // a beat of silence

            // Taq appears and survives (0:02 VO)
            Say("m1988");
            cap.text = "TAQ POLYMERASE: STILL ACTIVE AT 95 °C";
            cap.color = Orange;
            var taq = Model("PCRModels/Taq", new Vector3(0.1f, 0.1f, 0), 0.5f, new Color(1.5f, 1.15f, 0.9f));   // lifts the model's own orange
            snd.Cue("warm");
            if (taq != null) { var s0 = taq.transform.localScale; taq.transform.localScale = Vector3.zero; yield return Over(0.7f, k => taq.transform.localScale = s0 * k); }
            var cycleText = Caption("", new Vector3(1.0f, 0.55f, 0), 80, Cyan, 1.6f);
            var tc = Model("PCRModels/PCR", new Vector3(1.1f, -0.55f, 0), 0.5f, null);
            snd.Cue("ticks");
            // cycles accelerate (automated PCR)
            float elapsed = 0f; int cycle = 0; float period = 0.9f; float next = 0f;
            while (elapsed < 7.5f)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= next)
                {
                    cycle++; period = Mathf.Max(0.12f, period * 0.78f); next = elapsed + period;
                    cycleText.text = "CYCLE " + cycle;
                    float hi = cycle % 2 == 0 ? 1.2f : 0.55f;
                    level.localScale = new Vector3(0.07f, hi, 0.04f);
                    level.localPosition = new Vector3(0, -0.65f + hi * 0.5f, 0);
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

        // ------------------------------------------------------------------ 1993: the Nobel medal (the most dramatic visual)
        IEnumerator M1993()
        {
            // the timeline pauses, the screen dims, music thins out
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
            // ribbon
            Gen.Box("RibbonL", medal.transform, new Vector3(-0.07f, 0.75f, 0.02f), new Vector3(0.13f, 0.75f, 0.01f), Mats.Lit(Theme.NavyLift, null, 0.5f)).transform.localRotation = Quaternion.Euler(0, 0, -6f);
            Gen.Box("RibbonR", medal.transform, new Vector3(0.07f, 0.75f, 0.02f), new Vector3(0.13f, 0.75f, 0.01f), Mats.Lit(Gold * 0.9f, null, 0.5f)).transform.localRotation = Quaternion.Euler(0, 0, 6f);
            // engraved text on the medal face
            var cv = Ui.Canvas("MedalText", medal.transform, new Vector2(640, 640), new Vector3(0, 0, -0.026f));
            Ui.Label(cv.transform, "NOBEL\nPRIZE", 100, new Color(0.35f, 0.22f, 0.02f), TextAnchor.MiddleCenter, new Vector2(600, 260), new Vector2(0, 90), FontStyle.Bold);
            Ui.Label(cv.transform, "CHEMISTRY\n1993", 76, new Color(0.35f, 0.22f, 0.02f), TextAnchor.MiddleCenter, new Vector2(600, 220), new Vector2(0, -130), FontStyle.Bold);
            // halo, rays and a spot
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
            Say("m1993");                                      // 0:02 VO
            snd.Cue("nobel");
            var from = new Vector3(0.1f, -1.5f, 0.2f);
            var to = new Vector3(0.1f, 0.2f, 0.2f);
            yield return Over(3.2f, k =>                       // the medal rises
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
            // slow shine across the medal while the voice finishes
            float t = 0f;
            while (t < 6f)
            {
                t += Time.deltaTime;
                medal.transform.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.8f) * 14f, 0);
                halo.transform.localScale = Vector3.one * (2.4f + 0.2f * Mathf.Sin(t * 3f));
                yield return null;
            }
        }

        // ------------------------------------------------------------------ the 1990s: real-time PCR, fluorescence
        IEnumerator M1990s()
        {
            // the medal's gold glow dissolves into blue
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
            Say("m1990s");                                      // 0:02 VO
            // tubes that start to fluoresce
            var tubes = new List<Material>();
            for (int i = 0; i < 4; i++)
            {
                var tg = Obj("Tube" + i, new Vector3(-1.2f + i * 0.28f, -0.75f, 0));
                Gen.Prim(PrimitiveType.Cylinder, "Body", tg.transform, new Vector3(0, 0.12f, 0), new Vector3(0.1f, 0.12f, 0.1f), Glass(Color.white, 0.3f));
                var m = Mats.LitNew(new Color(0.1f, 0.3f, 0.5f), new Color(0.05f, 0.1f, 0.2f), 0.6f, 0f);
                Gen.Prim(PrimitiveType.Cylinder, "Liquid", tg.transform, new Vector3(0, 0.07f, 0), new Vector3(0.075f, 0.07f, 0.075f), m);
                tubes.Add(m);
            }
            var cap = Caption("FLUORESCENCE", new Vector3(0, 0.92f, 0), 78, Cyan, 2.2f);
            // axes + curve
            var axes = Obj("Axes", new Vector3(0.55f, -0.6f, 0));
            Gen.Box("X", axes.transform, new Vector3(0.45f, 0, 0), new Vector3(0.9f, 0.012f, 0.012f), Mats.Lit(Color.white, Color.white * 0.8f, 0.3f));
            Gen.Box("Y", axes.transform, new Vector3(0, 0.5f, 0), new Vector3(0.012f, 1.0f, 0.012f), Mats.Lit(Color.white, Color.white * 0.8f, 0.3f));
            Caption("CYCLES", new Vector3(0.45f, -0.9f, 0), 58, Color.white, 1.2f);
            Caption("SIGNAL", new Vector3(-0.25f, -0.1f, 0), 58, Color.white, 1.2f);
            var line = Gen.Line(axes.transform, "Curve", Cyan, 0.03f, 2, Mats.Line(Color.white));
            const int N = 60;
            snd.Cue("riser");
            yield return Over(7f, k =>
            {
                int count = Mathf.Max(2, Mathf.RoundToInt(k * N));
                line.positionCount = count;
                for (int i = 0; i < count; i++)
                {
                    float x = i / (N - 1f);
                    float y = 1f / (1f + Mathf.Exp(-(x - 0.55f) * 11f));
                    line.SetPosition(i, new Vector3(x * 0.9f, y * 0.95f, 0));
                }
                float glowK = Mathf.SmoothStep(0, 1, k);
                foreach (var m in tubes) { m.SetColor("_BaseColor", Color.Lerp(new Color(0.1f, 0.3f, 0.5f), new Color(0.2f, 0.9f, 1f), glowK)); m.SetColor("_EmissionColor", Color.Lerp(new Color(0.05f, 0.1f, 0.2f), new Color(0.2f, 0.9f, 1f) * 2.2f, glowK)); }
            });
            snd.Cue("sparkle");
            cap.text = "REAL-TIME PCR";
            yield return Wait(2.2f);
        }

        // ------------------------------------------------------------------ today: four technique icons, then EXPLORE PCR TYPES
        IEnumerator MToday()
        {
            Say("today");                                       // 0:00 VO
            string[] names = { "ENDPOINT PCR", "qPCR", "RT-PCR", "NESTED PCR" };
            for (int i = 0; i < 4; i++)
            {
                var g = Obj(names[i], new Vector3(-1.05f + i * 0.7f, 0.05f, 0));
                var plate = Mats.Lit(Theme.NavyMid, new Color(0.1f, 0.2f, 0.4f) * 0.5f, 0.5f);
                Gen.Box("Plate", g.transform, Vector3.zero, new Vector3(0.62f, 0.78f, 0.03f), plate);
                Icon(i, g.transform);
                var cv = Ui.Canvas("Name", g.transform, new Vector2(620, 200), new Vector3(0, -0.52f, -0.03f));
                Ui.Label(cv.transform, names[i], 64, Color.white, TextAnchor.MiddleCenter, new Vector2(620, 200), Vector2.zero, FontStyle.Bold, true);
                snd.Cue("pop");
                StartCoroutine(Pop(g.transform, 0.5f));
                yield return Wait(0.9f);
            }
            snd.Cue("chime");
            Caption("A FAMILY OF METHODS", new Vector3(0, 0.78f, 0), 74, Cyan, 2.4f);
            yield return Wait(3f);
        }

        void Icon(int i, Transform p)
        {
            var bright = Mats.Lit(Cyan * 0.6f, Cyan * 1.6f, 0.3f);
            switch (i)
            {
                case 0:   // gel with bands
                    Gen.Box("Gel", p, new Vector3(0, 0.1f, -0.02f), new Vector3(0.5f, 0.42f, 0.01f), Mats.Lit(new Color(0.03f, 0.05f, 0.08f), null, 0.3f));
                    for (int l = 0; l < 3; l++)
                        Gen.Box("Band", p, new Vector3(-0.15f + l * 0.15f, 0.1f + (l == 1 ? 0.05f : -0.05f), -0.03f), new Vector3(0.11f, 0.03f, 0.01f), bright);
                    break;
                case 1:   // amplification curve
                    var line = Gen.Line(p, "Curve", Cyan, 0.02f, 24, Mats.Line(Color.white));
                    line.transform.localPosition = new Vector3(-0.22f, -0.12f, -0.03f);
                    for (int k = 0; k < 24; k++) { float x = k / 23f; line.SetPosition(k, new Vector3(x * 0.44f, 0.5f / (1f + Mathf.Exp(-(x - 0.55f) * 10f)), 0)); }
                    break;
                case 2:   // RNA wave to DNA helix
                    var rna = Gen.Line(p, "RNA", Orange, 0.02f, 20, Mats.Line(Color.white));
                    rna.transform.localPosition = new Vector3(-0.22f, 0.28f, -0.03f);
                    for (int k = 0; k < 20; k++) rna.SetPosition(k, new Vector3(k / 19f * 0.44f, Mathf.Sin(k * 0.9f) * 0.04f, 0));
                    Gen.Box("Arrow", p, new Vector3(0, 0.12f, -0.03f), new Vector3(0.03f, 0.1f, 0.01f), bright);
                    var d = DnaHelix.Create(p, "cDNA", new Vector3(0, -0.12f, -0.05f), 9, 0.04f, 0.028f, 36f, 8, 1.3f);
                    d.SpinDegPerSec = 40f;
                    break;
                default:  // nested brackets: outer product, inner product
                    Gen.Box("Outer", p, new Vector3(0, 0.12f, -0.03f), new Vector3(0.5f, 0.06f, 0.01f), Mats.Lit(Orange * 0.7f, Orange * 1.4f, 0.3f));
                    Gen.Box("Inner", p, new Vector3(0, -0.02f, -0.03f), new Vector3(0.24f, 0.06f, 0.01f), Mats.Lit(Color.green * 0.7f, new Color(0.3f, 1f, 0.5f) * 1.6f, 0.3f));
                    break;
            }
        }
    }
}
