using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PCR
{
    public class TimelineStage : MonoBehaviour
    {
        public Vector3 Spot { get; private set; }
        public Transform Root { get; private set; }
        public Transform VisualRoot { get; private set; }
        public HoloButton Next, Previous, Replay, Explore;
        public HoloPanel Panel;
        public Light Fill { get; private set; }

        const float R = 3.6f, ArcY = 2.65f;
        readonly List<Transform> nodes = new List<Transform>();
        readonly List<Text> labels = new List<Text>();
        readonly List<Transform> links = new List<Transform>();
        Material linkOn, linkOff, nodeOn, nodeOff, nodeNow;
        int current = -1;
        Text yearText;

        public Vector3 VisualCenter => Spot + new Vector3(0.7f, 1.45f, 3.3f);

        public static TimelineStage Build(Transform parent, Vector3 spot)
        {
            var go = new GameObject("TimelineStage");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<TimelineStage>();
            s.Spot = spot;
            s.Root = go.transform;
            s.Construct();
            return s;
        }

        void Construct()
        {
            var navy = Mats.Lit(Theme.DeepNavy * 1.1f, null, 0.35f, 0f);
            var ring = Mats.Lit(Theme.NavyMid, new Color(0.25f, 0.9f, 1f) * 0.9f, 0.4f);
            var floor = Gen.Box("StageFloor", Root, Spot + new Vector3(0, -0.1f, 4f), new Vector3(30f, 0.2f, 30f), navy, true);
            Gen.Prim(PrimitiveType.Cylinder, "PlayerRing", Root, Spot + new Vector3(0, 0.006f, 0), new Vector3(2.2f, 0.004f, 2.2f), ring);
            Gen.Prim(PrimitiveType.Cylinder, "PlayerDisc", Root, Spot + new Vector3(0, 0.003f, 0), new Vector3(2.0f, 0.004f, 2.0f), Mats.Lit(Theme.Navy, null, 0.6f));

            VisualRoot = new GameObject("Visuals").transform;
            VisualRoot.SetParent(Root, false);
            VisualRoot.position = VisualCenter;

            Fill = new GameObject("StageLight").AddComponent<Light>();
            Fill.transform.SetParent(Root, false);
            Fill.type = LightType.Point; Fill.color = new Color(0.72f, 0.86f, 1f); Fill.range = 9f; Fill.intensity = 1.6f; Fill.shadows = LightShadows.None;
            Fill.transform.position = Spot + new Vector3(0.3f, 2.4f, 1.6f);

            BuildArc();
            BuildMotes();

            var panelPos = Spot + new Vector3(-1.25f, 1.42f, 2.5f);
            Panel = HoloPanel.Create(Root, panelPos, new Vector2(1300, 900), "TITLE", "body", Ui.Cyan, 64, 44);
            Panel.transform.rotation = FaceAway(panelPos);
            var yc = Ui.Canvas("YearBanner", Root, new Vector2(1300, 260), panelPos + new Vector3(0, 0.7f, 0));
            yc.transform.rotation = FaceAway(yc.transform.position);
            yearText = Ui.Label(yc.transform, "", 210, new Color(0.25f, 0.92f, 1f), TextAnchor.MiddleLeft, new Vector2(1300, 260), Vector2.zero, FontStyle.Bold, true);
            Panel.gameObject.SetActive(false); yc.gameObject.SetActive(false);
            panelRoot = Panel.gameObject; bannerRoot = yc.gameObject;

            var bp = Spot + new Vector3(-1.25f, 0.80f, 2.3f);
            Next = Button("NEXT  >", new Color(0.1f, 0.85f, 1f), bp, new Vector2(0.78f, 0.26f), 58, "next_button");
            Previous = Button("<  BACK", new Color(0.35f, 0.5f, 0.8f), bp + new Vector3(-1.05f, 0f, -0.04f), new Vector2(0.5f, 0.2f), 40, null);
            Replay = Button("REPLAY", new Color(0.35f, 0.5f, 0.8f), bp + new Vector3(1.05f, 0f, 0.04f), new Vector2(0.5f, 0.2f), 40, null);
            Explore = Button("EXPLORE PCR TYPES  >", new Color(0.2f, 1f, 0.6f), bp, new Vector2(1.2f, 0.27f), 42, "explore_button");
            ShowButtons(false, false);
        }

        GameObject panelRoot, bannerRoot;

        Quaternion FaceAway(Vector3 from)
        {
            var d = from - Spot; d.y = 0;
            return Quaternion.LookRotation(d.normalized);
        }

        HoloButton Button(string text, Color c, Vector3 pos, Vector2 size, int font, string stepId)
        {
            var b = HoloButton.Create(Root, pos, text, c, size, font);
            b.transform.rotation = FaceAway(pos);
            if (!string.IsNullOrEmpty(stepId)) b.StepId = stepId;
            return b;
        }

        void BuildArc()
        {
            linkOn = Mats.Lit(Theme.NavyMid, new Color(0.25f, 0.9f, 1f) * 1.4f, 0.3f);
            linkOff = Mats.Lit(Theme.NavyMid, new Color(0.15f, 0.25f, 0.5f) * 0.6f, 0.3f);
            nodeOn = Mats.Lit(new Color(0.2f, 0.7f, 0.9f), new Color(0.25f, 0.9f, 1f) * 1.6f, 0.3f);
            nodeOff = Mats.Lit(Theme.NavyMid, new Color(0.15f, 0.25f, 0.5f) * 0.8f, 0.3f);
            nodeNow = Mats.Lit(Color.white, Color.white * 2.4f, 0.3f);
            int n = JourneyContent.Years.Length;
            Vector3 prev = default;
            for (int i = 0; i < n; i++)
            {
                float ang = Mathf.Lerp(-47f, 47f, i / (n - 1f)) * Mathf.Deg2Rad;
                var p = Spot + new Vector3(Mathf.Sin(ang) * R, ArcY, Mathf.Cos(ang) * R);
                var node = Gen.Prim(PrimitiveType.Sphere, "Node" + i, Root, p, Vector3.one * 0.13f, nodeOff).transform;
                nodes.Add(node);
                var c = Ui.Canvas("Year" + i, Root, new Vector2(700, 260), p + new Vector3(0, 0.3f, 0));
                c.transform.rotation = FaceAway(p);
                var t = Ui.Label(c.transform, JourneyContent.Years[i], 170, new Color(0.45f, 0.55f, 0.8f), TextAnchor.MiddleCenter, new Vector2(700, 260), Vector2.zero, FontStyle.Bold);
                labels.Add(t);
                if (i > 0)
                {
                    var d = p - prev;
                    var link = Gen.Box("Link" + i, Root, (p + prev) * 0.5f, new Vector3(0.04f, 0.04f, d.magnitude), linkOff);
                    link.transform.rotation = Quaternion.LookRotation(d.normalized);
                    links.Add(link.transform);
                }
                prev = p;
            }
        }

        void BuildMotes()
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(Root, false);
            go.transform.position = Spot + new Vector3(0, 2.5f, 4f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f); main.maxParticles = 160; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.85f, 1f, 0.7f), new Color(0.6f, 0.5f, 1f, 0.7f));
            var em = ps.emission; em.rateOverTime = 14f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(18, 6, 18);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Glow(Color.white); r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Simulate(6f, true, true); ps.Play();
        }

        public void SetCurrent(int i)
        {
            current = i;
            for (int k = 0; k < nodes.Count; k++)
            {
                var r = nodes[k].GetComponent<Renderer>();
                r.sharedMaterial = k == i ? nodeNow : (k < i ? nodeOn : nodeOff);
                nodes[k].localScale = Vector3.one * (k == i ? 0.22f : 0.13f);
                labels[k].color = k == i ? Color.white : (k < i ? new Color(0.45f, 0.85f, 1f) : new Color(0.45f, 0.55f, 0.8f));
                labels[k].fontSize = k == i ? 215 : 170;
            }
            for (int k = 0; k < links.Count; k++) links[k].GetComponent<Renderer>().sharedMaterial = k < i ? linkOn : linkOff;
        }

        public void ShowPanel(string year, string title, string body)
        {
            panelRoot.SetActive(true); bannerRoot.SetActive(true);
            yearText.text = year;
            Panel.Set(title, body);
            Panel.Pop();
        }

        public void HidePanel() { panelRoot.SetActive(false); bannerRoot.SetActive(false); }

        public void ShowButtons(bool next, bool explore)
        {
            Next.gameObject.SetActive(next);
            Explore.gameObject.SetActive(explore);
            Previous.gameObject.SetActive(next && current > 0);
            Replay.gameObject.SetActive(next || explore);
        }

        void Update()
        {
            if (current >= 0 && current < nodes.Count)
                nodes[current].localScale = Vector3.one * (0.22f + 0.03f * Mathf.Sin(Time.time * 3.2f));
        }
    }
}
