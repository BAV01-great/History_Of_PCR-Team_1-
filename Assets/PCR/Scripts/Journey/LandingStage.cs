using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    public class LandingStage : MonoBehaviour
    {
        public Vector3 Spot;
        public Vector3 HelixPos;
        public Vector3 PortalPos;
        public bool Entered, Opened;
        public event System.Action PortalOpened;
        public const float TriggerDistance = 0.9f;
        public const float ReachDistance = 3.2f;

        Transform portalRoot;
        float openT = -1f;

        DnaHelix hero;
        Transform ring;
        Light portalLight, helixLight;

        public static LandingStage Build(Transform parent, Vector3 spot)
        {
            var go = new GameObject("LandingStage");
            go.transform.SetParent(parent, false);
            var ls = go.AddComponent<LandingStage>();
            ls.Spot = spot;
            ls.Construct(go.transform);
            return ls;
        }

        public void Atmosphere()
        {
            var navy = Color.Lerp(Theme.DeepNavy, new Color(0.015f, 0.03f, 0.075f), 0.5f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.16f, 0.26f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = navy; RenderSettings.fogDensity = 0.028f;
            var cam = Camera.main; if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = navy; }
        }

        void Construct(Transform t)
        {
            HelixPos = Spot + new Vector3(0, 0, 9f);
            PortalPos = HelixPos + new Vector3(0f, 1.95f, 0f);
            var dark = Mats.Lit(new Color(0.03f, 0.05f, 0.09f), null, 0.85f, 0.6f);
            var edge = Mats.Lit(new Color(0.05f, 0.2f, 0.3f), new Color(0.1f, 0.8f, 1f) * 0.9f, 0.4f);

            var floor = Gen.Box("LandingFloor", t, Spot + new Vector3(0, -0.1f, 8f), new Vector3(60f, 0.2f, 60f), dark, true);
            Destroy(floor.GetComponent<MeshRenderer>()); Destroy(floor.GetComponent<MeshFilter>());
            float len = 19f, mid = Spot.z + len * 0.5f - 1f;
            Gen.Box("Runway", t, new Vector3(Spot.x, -0.02f, mid), new Vector3(3.6f, 0.04f, len), dark);
            Gen.Box("RunwayEdgeL", t, new Vector3(Spot.x - 1.85f, 0f, mid), new Vector3(0.06f, 0.06f, len), edge);
            Gen.Box("RunwayEdgeR", t, new Vector3(Spot.x + 1.85f, 0f, mid), new Vector3(0.06f, 0.06f, len), edge);

            var chev = Mats.Lit(new Color(0.1f, 0.3f, 0.4f), new Color(0.2f, 0.9f, 1f), 0.3f);
            for (int i = 0; i < 6; i++)
            {
                var c = new GameObject("Chevron" + i).transform;
                c.SetParent(t, false);
                c.position = Spot + new Vector3(0, 0.01f, 1.5f + i * 0.9f);
                Gen.Box("L", c, new Vector3(-0.22f, 0, 0), new Vector3(0.07f, 0.01f, 0.5f), chev).transform.localRotation = Quaternion.Euler(0, 40, 0);
                Gen.Box("R", c, new Vector3(0.22f, 0, 0), new Vector3(0.07f, 0.01f, 0.5f), chev).transform.localRotation = Quaternion.Euler(0, -40, 0);
                c.gameObject.AddComponent<PulseScale>().Delay = i * 0.18f;
            }

            Gen.Prim(PrimitiveType.Cylinder, "Dais", t, HelixPos + new Vector3(0, -0.03f, 0), new Vector3(5.2f, 0.03f, 5.2f), dark);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing1", t, HelixPos, new Vector3(5.25f, 0.012f, 5.25f), edge);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing2", t, HelixPos + new Vector3(0, 0.008f, 0), new Vector3(3.6f, 0.012f, 3.6f), dark);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing3", t, HelixPos + new Vector3(0, 0.016f, 0), new Vector3(3.55f, 0.012f, 3.55f), edge);
            Gen.Prim(PrimitiveType.Cylinder, "DaisCore", t, HelixPos + new Vector3(0, 0.024f, 0), new Vector3(3.4f, 0.012f, 3.4f), dark);

            var teamHelix = JourneyController.UseTeamHelixForHero ? Resources.Load<GameObject>("PCRModels/DNA_Helix_Whole") : null;
            hero = teamHelix != null ? DnaHelix.WrapModel(t, "HeroHelix", HelixPos + new Vector3(0, 0.35f, 0), teamHelix, 3.2f)
                                     : DnaHelix.Create(t, "HeroHelix", HelixPos + new Vector3(0, 0.35f, 0), 34, 0.42f, 0.095f, 34f, 11, 0.8f);
            hero.SpinDegPerSec = 14f;
            hero.transform.localScale = Vector3.one * 1.3f;
            var halo = Gen.Prim(PrimitiveType.Quad, "Halo", t, HelixPos + new Vector3(0, 1.9f, 0.6f), new Vector3(5f, 5f, 1f), Mats.Glow(new Color(0.1f, 0.6f, 1f, 0.16f)));
            halo.AddComponent<BillboardY>(); Destroy(halo.GetComponent<Collider>());
            helixLight = new GameObject("HelixLight").AddComponent<Light>();
            helixLight.transform.SetParent(t, false); helixLight.transform.position = HelixPos + new Vector3(0, 2f, -1f);
            helixLight.type = LightType.Point; helixLight.color = new Color(0.3f, 0.9f, 1f); helixLight.range = 9f; helixLight.intensity = 0.8f; helixLight.shadows = LightShadows.None;

            for (int i = 0; i < 3; i++)
            {
                var q = Gen.Prim(PrimitiveType.Quad, "ColorWash" + i, t, HelixPos + new Vector3((i - 1) * 4.5f, 2.5f + (i % 2) * 1.2f, 4.5f + i), new Vector3(9f, 9f, 1f), Mats.Glow(Color.white));
                var hc = q.AddComponent<HueCycle>(); hc.Offset = i / 3f; hc.Speed = 0.04f; hc.Alpha = 0.12f;
                q.AddComponent<BillboardY>(); Destroy(q.GetComponent<Collider>());
            }

            var tp = HelixPos + new Vector3(0, 5.9f, 0);
            var tcv = Ui.Canvas("Title", t, new Vector2(1400, 180), tp);
            tcv.transform.localScale = Vector3.one * 0.006f;
            Ui.Label(tcv.transform, "THE HISTORY OF PCR", 110, Ui.Cyan, TextAnchor.MiddleCenter, new Vector2(1400, 180), Vector2.zero, FontStyle.Bold, true);
            tcv.transform.rotation = Quaternion.LookRotation(tp - (Spot + new Vector3(0, 1.6f, 0)));
            tcv.gameObject.AddComponent<Bob>().Amplitude = 0.06f;

            BuildPortal(t);
            BuildMotes(t);
        }

        void BuildPortal(Transform t)
        {
            portalRoot = new GameObject("Portal").transform;
            portalRoot.SetParent(t, false);
            ring = new GameObject("PortalRing").transform;
            ring.SetParent(portalRoot, false); ring.position = PortalPos; ring.rotation = Quaternion.identity;
            var ringMat = Mats.LitNew(new Color(0.1f, 0.6f, 0.9f), new Color(0.2f, 0.9f, 1f) * 1.4f, 0.4f);
            const int n = 64; const float r = 1.9f;
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n;
                var seg = Gen.Prim(PrimitiveType.Cube, "seg", ring, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0), new Vector3(0.34f, 0.14f, 0.14f), ringMat);
                seg.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f);
            }
            var disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
            disc.name = "PortalDisc"; disc.transform.SetParent(ring, false);
            disc.transform.localPosition = new Vector3(0, 0, 0.5f); disc.transform.localRotation = Quaternion.identity; disc.transform.localScale = new Vector3(4.6f, 4.6f, 1f);
            Destroy(disc.GetComponent<Collider>());
            disc.GetComponent<Renderer>().sharedMaterial = Mats.Glow(new Color(0.25f, 0.85f, 1f));
            portalLight = new GameObject("PortalLight").AddComponent<Light>();
            portalLight.transform.SetParent(portalRoot, false); portalLight.transform.position = PortalPos + new Vector3(0, 0, -1.8f);
            portalLight.type = LightType.Point; portalLight.color = new Color(0.3f, 0.85f, 1f); portalLight.range = 9f; portalLight.intensity = 2f; portalLight.shadows = LightShadows.None;
            portalRoot.gameObject.SetActive(false);
        }

        public void ForceOpen() { if (!Opened) Open(); }

        void Open()
        {
            Opened = true; openT = 0f;
            portalRoot.gameObject.SetActive(true);
            ring.localScale = Vector3.one * 0.01f;
            PortalOpened?.Invoke();
        }

        void BuildMotes(Transform t)
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(t, false);
            go.transform.position = Spot + new Vector3(0, 2f, 8f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.9f, 1f, 0.8f), new Color(0.8f, 0.5f, 1f, 0.8f));
            main.maxParticles = 260; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 26f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(26, 9, 30);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Glow(Color.white); r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Simulate(8f, true, true); ps.Play();
        }

        void Update()
        {
            if (helixLight != null) helixLight.intensity = 0.8f + 0.15f * Mathf.Sin(Time.time * 1.6f);
            if (hero != null) hero.transform.position = HelixPos + new Vector3(0, 0.35f + 0.07f * Mathf.Sin(Time.time * 1.1f), 0);
            if (hero != null) hero.SetGlow((Opened ? 1.0f : 0.9f) + 0.12f * Mathf.Sin(Time.time * 1.6f));
            if (Opened)
            {
                if (portalLight != null) portalLight.intensity = 2f + 0.5f * Mathf.Sin(Time.time * 2.2f);
                if (openT >= 0f && openT < 1f)
                {
                    openT = Mathf.Min(1f, openT + Time.deltaTime / 1.4f);
                    float k = 1f - Mathf.Pow(1f - openT, 3f);
                    ring.localScale = Vector3.one * Mathf.Max(0.01f, k);
                }
                else if (ring != null) ring.Rotate(0, 0, 12f * Time.deltaTime, Space.Self);
            }
            var cam = Camera.main;
            if (cam == null || Entered) return;
            if (!Opened)
            {
                var h = cam.transform.position - HelixPos; h.y = 0;
                if (h.magnitude < ReachDistance && cam.transform.position.z > Spot.z - 1f) Open();
                return;
            }
            var d = cam.transform.position - HelixPos; d.y = 0;
            if (openT >= 1f && d.magnitude < TriggerDistance && cam.transform.position.z > Spot.z - 1f) Entered = true;
        }
    }
}
