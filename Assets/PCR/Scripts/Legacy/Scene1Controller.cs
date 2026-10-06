// LEGACY (archived): belongs to the old five-scene plan. Compiled only if the scripting define PCR_LEGACY is set.
// Kept for reuse of the station and bench code in later scenes.
#if PCR_LEGACY
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    /// <summary>
    /// Scene 1 - Landing. A glowing hologram pad in a dark molecular void, a hero DNA helix to walk towards,
    /// drifting particles and distant helices. Approach the helix, press START JOURNEY, and hand off to Scene 2.
    /// Everything is generated at runtime from code (VR-friendly: ~30 draw calls, no textures).
    /// </summary>
    public class Scene1Controller : MonoBehaviour
    {
        public string NextScene = "Scene2_Problem";
        public float TriggerDistance = 3.2f;
        public Vector3 HelixPosition = new Vector3(0, 0, 7f);

        Transform root;
        DnaHelix hero;
        Transform head => Camera.main != null ? Camera.main.transform : null;
        bool revealed, started;
        ParticleSystem motes;
        HoloButton startButton;
        HoloPanel titlePanel;
        Light pulseLight;

        [Tooltip("Hands-free playthrough for screen recording (Unity Recorder).")]
        public bool AutoplayDemo;

        [Tooltip("Use the team's DNA_Helix_Whole.fbx instead of the generated neon helix (the neon helix is also exported as DNA_Helix_Neon.fbx).")]
        public bool UseTeamHelixModel;

        void Awake() { PcrDemo.Active = AutoplayDemo; }

        IEnumerator DemoRun()
        {
            var rig = FindFirstObjectByType<RigSelector>();
            if (rig == null) yield break;
            yield return new WaitForSeconds(9f); // let the welcome line play
            yield return rig.WalkTo(HelixPosition + new Vector3(0, 0, -TriggerDistance + 0.4f), 1.4f);
            while (startButton == null) yield return null;
            yield return rig.FaceTowards(startButton.transform.position);
            yield return new WaitForSeconds(2f);
            startButton.Press();
        }

        void Start()
        {
            root = new GameObject("Scene1_World").transform;
            ConfigureAtmosphere();
            BuildPad();
            BuildGuidePath();
            BuildHero();
            BuildBackdropHelices();
            BuildMotes();
            BuildTitle();
            BuildColorWash();
            PostFx.Apply(0.4f, 1.25f);
            StartCoroutine(Intro());
            if (AutoplayDemo) StartCoroutine(DemoRun());
        }

        void ConfigureAtmosphere()
        {
            var navy = Color.Lerp(Theme.DeepNavy, new Color(0.015f, 0.03f, 0.075f), 0.5f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.16f, 0.26f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = navy;
            RenderSettings.fogDensity = 0.028f;
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = navy; }

            var l = new GameObject("KeyLight").AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(0.6f, 0.8f, 1f);
            l.intensity = 0.6f;
            l.shadows = LightShadows.None;
            l.transform.rotation = Quaternion.Euler(50, -30, 0);
            l.transform.SetParent(root, false);

            pulseLight = new GameObject("HelixLight").AddComponent<Light>();
            pulseLight.type = LightType.Point;
            pulseLight.color = new Color(0.3f, 0.9f, 1f);
            pulseLight.range = 9f;
            pulseLight.intensity = 0.8f;
            pulseLight.shadows = LightShadows.None;
            pulseLight.transform.SetParent(root, false);
            pulseLight.transform.position = HelixPosition + new Vector3(0, 2f, -1f);

            var amb = new GameObject("Ambience").AddComponent<AudioSource>();
            amb.transform.SetParent(root, false);
            amb.clip = ProcAudio.Ambient; amb.loop = true; amb.volume = 0.12f; amb.spatialBlend = 0f; amb.Play();
        }

        void BuildPad()
        {
            var dark = Mats.Lit(new Color(0.03f, 0.05f, 0.09f), null, 0.85f, 0.6f);
            var ring = Mats.Lit(new Color(0.05f, 0.2f, 0.3f), new Color(0.1f, 0.8f, 1f) * 0.9f, 0.4f);
            // Long runway-like pad from spawn to helix, plus a round dais under the helix.
            // Invisible walkable floor (flat box collider; a scaled primitive cylinder would get a sphere-ish capsule collider).
            var floor = Gen.Box("FloorCollider", root, new Vector3(0, -0.1f, HelixPosition.z * 0.5f), new Vector3(60f, 0.2f, 60f), dark, true);
            Destroy(floor.GetComponent<MeshRenderer>());
            Destroy(floor.GetComponent<MeshFilter>());
            Gen.Box("Runway", root, new Vector3(0, -0.02f, HelixPosition.z * 0.5f), new Vector3(3.6f, 0.04f, HelixPosition.z + 3f), dark);
            Gen.Box("RunwayEdgeL", root, new Vector3(-1.85f, 0.0f, HelixPosition.z * 0.5f), new Vector3(0.06f, 0.06f, HelixPosition.z + 3f), ring);
            Gen.Box("RunwayEdgeR", root, new Vector3(1.85f, 0.0f, HelixPosition.z * 0.5f), new Vector3(0.06f, 0.06f, HelixPosition.z + 3f), ring);
            // Concentric dais layers, stepped 6 mm apart to avoid depth fighting in VR.
            Gen.Prim(PrimitiveType.Cylinder, "Dais", root, HelixPosition + new Vector3(0, -0.03f, 0), new Vector3(5.2f, 0.03f, 5.2f), dark);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing1", root, HelixPosition + new Vector3(0, 0.0f, 0), new Vector3(5.25f, 0.012f, 5.25f), ring);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing2", root, HelixPosition + new Vector3(0, 0.008f, 0), new Vector3(3.6f, 0.012f, 3.6f), dark);
            Gen.Prim(PrimitiveType.Cylinder, "DaisRing3", root, HelixPosition + new Vector3(0, 0.016f, 0), new Vector3(3.55f, 0.012f, 3.55f), ring);
            Gen.Prim(PrimitiveType.Cylinder, "DaisCore", root, HelixPosition + new Vector3(0, 0.024f, 0), new Vector3(3.4f, 0.012f, 3.4f), dark);
        }

        void BuildGuidePath()
        {
            // Chevrons that pulse towards the helix, so "move towards the helix" needs no explanation.
            var m = Mats.Lit(new Color(0.1f, 0.3f, 0.4f), new Color(0.2f, 0.9f, 1f) * 1.0f, 0.3f);
            for (int i = 0; i < 6; i++)
            {
                float z = 1.5f + i * 0.9f;
                var c = new GameObject("Chevron" + i).transform;
                c.SetParent(root, false);
                c.localPosition = new Vector3(0, 0.01f, z);
                Gen.Box("L", c, new Vector3(-0.22f, 0, 0), new Vector3(0.07f, 0.01f, 0.5f), m).transform.localRotation = Quaternion.Euler(0, 40, 0);
                Gen.Box("R", c, new Vector3(0.22f, 0, 0), new Vector3(0.07f, 0.01f, 0.5f), m).transform.localRotation = Quaternion.Euler(0, -40, 0);
                c.gameObject.AddComponent<PulseScale>().Delay = i * 0.18f;
            }
        }

        void BuildHero()
        {
            // Use the team's Blender model if it has been dropped in Assets/Resources/PCRModels/DNA_Helix_Whole.(fbx|prefab);
            // 5x scale turns the 60 cm model into a 3 m hero helix. Otherwise fall back to the procedural helix.
            var model = UseTeamHelixModel ? Resources.Load<GameObject>("PCRModels/DNA_Helix_Whole") : null;
            if (model != null)
                hero = DnaHelix.WrapModel(root, "HeroHelix", HelixPosition + new Vector3(0, 0.35f, 0), model, 3.2f);
            else
                hero = DnaHelix.Create(root, "HeroHelix", HelixPosition + new Vector3(0, 0.35f, 0), 34, 0.42f, 0.095f, 34f, 11, 0.8f);
            hero.SpinDegPerSec = 14f;
            // Soft halo behind the helix
            var halo = Gen.Prim(PrimitiveType.Quad, "Halo", root, HelixPosition + new Vector3(0, 1.9f, 0.6f), new Vector3(5f, 5f, 1f), Mats.Glow(new Color(0.1f, 0.6f, 1f, 0.16f)));
            halo.AddComponent<BillboardY>();
        }

        void BuildBackdropHelices()
        {
            var rnd = new System.Random(5);
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f) * Mathf.PI * 2f + 0.4f;
                float d = 11f + (float)rnd.NextDouble() * 6f;
                var pos = new Vector3(Mathf.Cos(a) * d, -1f + (float)rnd.NextDouble() * 4f, HelixPosition.z * 0.6f + Mathf.Sin(a) * d);
                var h = DnaHelix.Create(root, "Backdrop" + i, pos, 26, 0.35f, 0.16f, 34f, 20 + i, 0.4f);
                h.SpinDegPerSec = 6f + i * 2f;
                h.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 50f - 25f, 0, (float)rnd.NextDouble() * 50f - 25f);
                h.transform.localScale = Vector3.one * (1.0f + (float)rnd.NextDouble() * 1.5f);
            }
        }

        /// <summary>Big soft additive glows behind the helix that slowly drift through the spectrum.</summary>
        void BuildColorWash()
        {
            for (int i = 0; i < 3; i++)
            {
                var q = Gen.Prim(PrimitiveType.Quad, "ColorWash" + i, root,
                    HelixPosition + new Vector3((i - 1) * 4.5f, 2.5f + (i % 2) * 1.2f, 4.5f + i), new Vector3(9f, 9f, 1f), Mats.Glow(Color.white));
                var hc = q.AddComponent<HueCycle>();
                hc.Offset = i / 3f; hc.Speed = 0.04f; hc.Alpha = 0.12f;
                q.AddComponent<BillboardY>();
            }
        }

        void BuildMotes()
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0, 2f, HelixPosition.z * 0.5f);
            motes = go.AddComponent<ParticleSystem>();
            var main = motes.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.9f, 1f, 0.8f), new Color(0.8f, 0.5f, 1f, 0.8f));
            main.maxParticles = 260;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = motes.emission; em.rateOverTime = 26f;
            var sh = motes.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(26, 9, 26);
            var noise = motes.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.15f;
            var col = motes.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Glow(Color.white);
            r.shadowCastingMode = ShadowCastingMode.Off;
            // Pre-warm so the space is already full at spawn
            motes.Simulate(8f, true, true);
            motes.Play();
        }

        void BuildTitle()
        {
            var pos = HelixPosition + new Vector3(0, 4.3f, 0);
            titlePanel = HoloPanel.Create(root, pos, new Vector2(1400, 360), "THE HISTORY OF PCR",
                "Polymerase Chain Reaction  |  From idea to evolving technology", Ui.Cyan, 92, 36);
            titlePanel.transform.rotation = Quaternion.LookRotation(pos - new Vector3(0, 1.6f, 0));
            titlePanel.gameObject.AddComponent<Bob>().Amplitude = 0.06f;
        }

        IEnumerator Intro()
        {
            var fader = ScreenFader.Instance;
            fader.SetAlpha(1f);
            yield return new WaitForSeconds(0.6f);
            yield return fader.FadeTo(0f, 2.2f);
            ProcAudio.PlayAt(ProcAudio.Whoosh, head != null ? head.position : Vector3.zero, 0.4f);
            NarrationManager.Instance.Say("s1_welcome", () =>
                NarrationManager.Instance.SetObjective("Walk towards the DNA helix"));
        }

        void Update()
        {
            if (hero == null || head == null) return;
            float pulse = 0.9f + 0.12f * Mathf.Sin(Time.time * 1.6f);
            if (!revealed)
            {
                hero.SetGlow(pulse);
                var flat = head.position - HelixPosition; flat.y = 0;
                if (flat.magnitude < TriggerDistance) Reveal();
            }
        }

        void Reveal()
        {
            revealed = true;
            hero.SetGlow(1.4f);
            ProcAudio.PlayAt(ProcAudio.Chime, head.position, 0.6f);
            NarrationManager.Instance.SetObjective("Press START JOURNEY");
            var pos = HelixPosition + new Vector3(0, 1.25f, -2.0f);
            startButton = HoloButton.Create(root, pos, "START JOURNEY", new Color(0.1f, 0.85f, 1f), new Vector2(0.9f, 0.26f), 44);
            startButton.transform.rotation = Quaternion.LookRotation(pos - head.position);
            startButton.gameObject.AddComponent<Bob>().Amplitude = 0.02f;
            startButton.Pressed += OnStart;
        }

        void OnStart()
        {
            if (started) return;
            started = true;
            startButton.SetInteractable(false);
            NarrationManager.Instance.SetObjective("");
            StartCoroutine(SpinUp());
            NarrationManager.Instance.Say("s1_start", () =>
                SceneFlow.LoadNext(this, NextScene, () => NarrationManager.Instance.SetObjective("Scene 2 not linked yet (Team 1 handoff)")));
        }

        IEnumerator SpinUp()
        {
            ProcAudio.PlayAt(ProcAudio.Whoosh, hero.transform.position, 0.8f);
            var main = motes.main;
            float t = 0;
            while (t < 14f)
            {
                t += Time.deltaTime;
                hero.SpinDegPerSec = Mathf.Lerp(14f, 140f, Mathf.SmoothStep(0, 1, t / 6f));
                hero.SetGlow(Mathf.Lerp(1.4f, 1.8f, Mathf.Clamp01(t / 6f)));
                if (pulseLight != null) pulseLight.intensity = Mathf.Lerp(0.8f, 2f, Mathf.Clamp01(t / 6f));
                yield return null;
            }
        }
    }
}
#endif
