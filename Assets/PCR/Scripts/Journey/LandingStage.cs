using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    /// <summary>
    /// The old Scene 1 landing, recreated: a dark navy molecular void, a glowing runway with guide chevrons to a round dais, a hero DNA helix on
    /// the dais, drifting helices and colour washes behind, drifting motes, and the title above. Beyond the dais a portal leads into the lab:
    /// walking into it ends the landing. Built from code at its own spot in the scene.
    /// </summary>
    public class LandingStage : MonoBehaviour
    {
        public Vector3 Spot;
        public Vector3 HelixPos;
        public Vector3 PortalPos;
        public bool Entered, Opened;
        public event System.Action PortalOpened;
        public const float TriggerDistance = 0.9f;      // walking right up to the helix inside the ring counts as stepping in
        public const float ReachDistance = 3.2f;        // how close to the helix opens the portal

        Transform portalRoot;
        float openT = -1f;

        DnaHelix hero;
        Transform ring;
        Light portalLight, helixLight;

        public static LandingStage Build(Transform parent, Vector3 spot)
        {
            var go = new GameObject("LandingStage");
            go.transform.SetParent(parent, false);          // stays at the origin: children use world positions
            var ls = go.AddComponent<LandingStage>();
            ls.Spot = spot;
            ls.Construct(go.transform);
            return ls;
        }

        /// <summary>The landing's own air: deep navy fog and a cool ambient. The lab restores its own through LabLighting.Apply.</summary>
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
            PortalPos = HelixPos + new Vector3(0f, 1.95f, 0f);        // the ring forms around the hovering helix once it is reached
            var dark = Mats.Lit(new Color(0.03f, 0.05f, 0.09f), null, 0.85f, 0.6f);
            var edge = Mats.Lit(new Color(0.05f, 0.2f, 0.3f), new Color(0.1f, 0.8f, 1f) * 0.9f, 0.4f);

            // walkable floor (a flat box, so the player stands on it) and the runway with glowing edges
            var floor = Gen.Box("LandingFloor", t, Spot + new Vector3(0, -0.1f, 8f), new Vector3(60f, 0.2f, 60f), dark, true);
            Destroy(floor.GetComponent<MeshRenderer>()); Destroy(floor.GetComponent<MeshFilter>());
            float len = 19f, mid = Spot.z + len * 0.5f - 1f;
            Gen.Box("Runway", t, new Vector3(Spot.x, -0.02f, mid), new Vector3(3.6f, 0.04f, len), dark);
            Gen.Box("RunwayEdgeL", t, new Vector3(Spot.x - 1.85f, 0f, mid), new Vector3(0.06f, 0.06f, len), edge);
            Gen.Box("RunwayEdgeR", t, new Vector3(Spot.x + 1.85f, 0f, mid), new Vector3(0.06f, 0.06f, len), edge);

            // chevrons that pulse towards the helix, so "move towards it" needs no explanation
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

            // dais: stepped rings, 8 mm apart so they do not z-fight in VR
            Gen.Prim(PrimitiveType.Cylinder, "Dais", t, HelixPos + new Vector3(0, -0.03f, 0), new Vector3(5.2f, 0.03f, 5.2f), dark);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing1", t, HelixPos, new Vector3(5.25f, 0.012f, 5.25f), edge);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing2", t, HelixPos + new Vector3(0, 0.008f, 0), new Vector3(3.6f, 0.012f, 3.6f), dark);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing3", t, HelixPos + new Vector3(0, 0.016f, 0), new Vector3(3.55f, 0.012f, 3.55f), edge);
            Gen.Prim(PrimitiveType.Cylinder, "DaisCore", t, HelixPos + new Vector3(0, 0.024f, 0), new Vector3(3.4f, 0.012f, 3.4f), dark);

            // hero helix with a soft halo
            var teamHelix = JourneyController.UseTeamHelixForHero ? Resources.Load<GameObject>("PCRModels/DNA_Helix_Neon") : null;
            hero = teamHelix != null ? DnaHelix.WrapModel(t, "HeroHelix", HelixPos + new Vector3(0, 0.35f, 0), teamHelix, 3.2f)
                                     : DnaHelix.Create(t, "HeroHelix", HelixPos + new Vector3(0, 0.35f, 0), 34, 0.42f, 0.095f, 34f, 11, 0.8f);
            hero.SpinDegPerSec = 14f;
            var halo = Gen.Prim(PrimitiveType.Quad, "Halo", t, HelixPos + new Vector3(0, 1.9f, 0.6f), new Vector3(5f, 5f, 1f), Mats.Glow(new Color(0.1f, 0.6f, 1f, 0.16f)));
            halo.AddComponent<BillboardY>(); Destroy(halo.GetComponent<Collider>());
            helixLight = new GameObject("HelixLight").AddComponent<Light>();
            helixLight.transform.SetParent(t, false); helixLight.transform.position = HelixPos + new Vector3(0, 2f, -1f);
            helixLight.type = LightType.Point; helixLight.color = new Color(0.3f, 0.9f, 1f); helixLight.range = 9f; helixLight.intensity = 0.8f; helixLight.shadows = LightShadows.None;

            // distant helices drifting in the dark
            var rnd = new System.Random(5);
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f) * Mathf.PI * 2f + 0.4f, d = 11f + (float)rnd.NextDouble() * 6f;
                var pos = HelixPos + new Vector3(Mathf.Cos(a) * d, -1f + (float)rnd.NextDouble() * 4f, Mathf.Sin(a) * d - 3f);
                var neon = Resources.Load<GameObject>("PCRModels/DNA_Helix_Neon");       // the team's FBX, seen from a distance
                var h = neon != null ? DnaHelix.WrapModel(t, "Backdrop" + i, pos, neon, 4.2f)
                                     : DnaHelix.Create(t, "Backdrop" + i, pos, 26, 0.35f, 0.16f, 34f, 20 + i, 0.4f);
                h.SpinDegPerSec = 6f + i * 2f;
                h.SetGlow(1.3f);
                h.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 16f - 8f, 0, (float)rnd.NextDouble() * 16f - 8f);
                h.transform.localScale = Vector3.one * (0.8f + (float)rnd.NextDouble() * 1.2f);
            }

            // big soft colour washes that drift through the spectrum behind the helix
            for (int i = 0; i < 3; i++)
            {
                var q = Gen.Prim(PrimitiveType.Quad, "ColorWash" + i, t, HelixPos + new Vector3((i - 1) * 4.5f, 2.5f + (i % 2) * 1.2f, 4.5f + i), new Vector3(9f, 9f, 1f), Mats.Glow(Color.white));
                var hc = q.AddComponent<HueCycle>(); hc.Offset = i / 3f; hc.Speed = 0.04f; hc.Alpha = 0.12f;
                q.AddComponent<BillboardY>(); Destroy(q.GetComponent<Collider>());
            }

            // title above the helix
            var tp = HelixPos + new Vector3(0, 4.9f, 0);
            var title = HoloPanel.Create(t, tp, new Vector2(4400, 1100), "THE HISTORY OF PCR",
                "Polymerase Chain Reaction  |  From idea to evolving technology", Ui.Cyan, 290, 112);   // sized for viewing from ~14 m
            title.transform.rotation = Quaternion.LookRotation(tp - (Spot + new Vector3(0, 1.6f, 0)));
            title.gameObject.AddComponent<Bob>().Amplitude = 0.06f;

            BuildPortal(t);
            BuildMotes(t);
        }

        void BuildPortal(Transform t)
        {
            // a pad from the dais to the portal, chevrons along it, and a ring of glowing blocks around a soft disc: all hidden until the helix is reached
            portalRoot = new GameObject("Portal").transform;
            portalRoot.SetParent(t, false);
            ring = new GameObject("PortalRing").transform;
            ring.SetParent(portalRoot, false); ring.position = PortalPos; ring.rotation = Quaternion.identity;      // faces the runway
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
            disc.transform.localPosition = new Vector3(0, 0, 0.5f); disc.transform.localRotation = Quaternion.identity; disc.transform.localScale = new Vector3(4.6f, 4.6f, 1f);   // just behind the helix, so it is silhouetted against the glow
            Destroy(disc.GetComponent<Collider>());
            disc.GetComponent<Renderer>().sharedMaterial = Mats.Glow(new Color(0.25f, 0.85f, 1f));
            portalLight = new GameObject("PortalLight").AddComponent<Light>();
            portalLight.transform.SetParent(portalRoot, false); portalLight.transform.position = PortalPos + new Vector3(0, 0, -1.8f);
            portalLight.type = LightType.Point; portalLight.color = new Color(0.3f, 0.85f, 1f); portalLight.range = 9f; portalLight.intensity = 2f; portalLight.shadows = LightShadows.None;
            portalRoot.gameObject.SetActive(false);
        }

        /// <summary>Opens the portal now (used by tests and by reaching the helix).</summary>
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
            if (hero != null) hero.transform.position = HelixPos + new Vector3(0, 0.35f + 0.07f * Mathf.Sin(Time.time * 1.1f), 0);      // it hovers over the dais
            if (hero != null) hero.SetGlow((Opened ? 1.0f : 0.9f) + 0.12f * Mathf.Sin(Time.time * 1.6f));
            if (Opened)
            {
                if (portalLight != null) portalLight.intensity = 2f + 0.5f * Mathf.Sin(Time.time * 2.2f);
                if (openT >= 0f && openT < 1f)
                {
                    openT = Mathf.Min(1f, openT + Time.deltaTime / 1.4f);        // the ring blooms open
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
