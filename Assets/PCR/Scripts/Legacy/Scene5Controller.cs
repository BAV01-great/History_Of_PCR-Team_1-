// LEGACY (archived): belongs to the old five-scene plan. Compiled only if the scripting define PCR_LEGACY is set.
// Kept for reuse of the station and bench code in later scenes.
#if PCR_LEGACY
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    /// <summary>
    /// Scene 5 - Types of PCR. A bright modern lab with four interactive stations (end-point, qPCR, RT-PCR, nested).
    /// Finish all four knowledge checks, the PCR LAB door unlocks, walk to it, open it, hand off to Team 2.
    /// Everything is generated at runtime from code and kept cheap for standalone Quest.
    /// </summary>
    public class Scene5Controller : MonoBehaviour
    {
        public string NextScene = "Scene6_PCRLab"; // Team 2's scene

        const float HalfWidth = 6f, ZMin = -11f, ZMax = 12f, Height = 4f;
        static readonly Vector3 DoorPos = new Vector3(0, 0, ZMax - 0.2f);

        Transform root;
        readonly List<PcrStation> stations = new List<PcrStation>();
        int completed;
        bool doorUnlocked, doorOpening;
        Material signMat, guideMat;
        Light doorLight;
        Transform leafL, leafR;
        HoloButton openButton;
        GameObject spill;
        Transform head => Camera.main != null ? Camera.main.transform : null;

        [Tooltip("Hands-free playthrough for screen recording (Unity Recorder).")]
        public bool AutoplayDemo;

        void Awake() { PcrDemo.Active = AutoplayDemo; }

        IEnumerator DemoRun()
        {
            var rig = FindFirstObjectByType<RigSelector>();
            if (rig == null) yield break;
            // wait for all four cards to appear and the intro line to finish
            while (stations.Exists(s => !s.CanStart) || NarrationManager.Instance.IsSpeaking) yield return null;
            foreach (var s in stations)
            {
                yield return rig.WalkTo(s.FrontPoint, 1.8f);
                yield return rig.FaceTowards(s.transform.position + Vector3.up * 1.5f);
                yield return new WaitForSeconds(1f);
                s.DemoStart();
                while (!s.QuizReady) yield return null;
                yield return new WaitForSeconds(2.5f);
                s.DemoAnswer();
                while (!s.IsComplete) yield return null;
                yield return new WaitForSeconds(4f);
            }
            while (!doorUnlocked) yield return null;
            yield return rig.WalkTo(DoorPos + new Vector3(0, 0, -4.2f), 1.6f);
            while (openButton == null) yield return null;
            yield return rig.FaceTowards(openButton.transform.position);
            yield return new WaitForSeconds(2.5f);
            openButton.Press();
        }

        void Start()
        {
            root = new GameObject("Scene5_World").transform;
            Atmosphere();
            BuildRoom();
            BuildProps();
            BuildDoor();
            BuildStations();
            PostFx.Apply(0.4f, 1.1f);
            StartCoroutine(Intro());
            if (AutoplayDemo) StartCoroutine(DemoRun());
        }

        void Atmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.68f, 0.74f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(new Color(0.85f, 0.92f, 0.96f), Theme.Navy, 0.45f);
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 50f;
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Theme.DeepNavy; }

            var sun = new GameObject("KeyLight").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.98f, 0.94f);
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(60, 20, 0);

            doorLight = new GameObject("DoorLight").AddComponent<Light>();
            doorLight.transform.SetParent(root, false);
            doorLight.type = LightType.Point;
            doorLight.color = new Color(0.8f, 1f, 1f);
            doorLight.range = 14f; doorLight.intensity = 0f;
            doorLight.shadows = LightShadows.None;
            doorLight.transform.position = DoorPos + new Vector3(0, 2f, 1.2f);
        }

        void BuildRoom()
        {
            // Vibrant palette: pale lilac floor and walls so the saturated accents pop
            var floorM = Mats.Lit(new Color(0.72f, 0.69f, 0.92f), null, 0.3f, 0.05f);
            var wallM = Mats.Lit(new Color(0.98f, 0.96f, 1f), null, 0.35f);
            var accent = Mats.Lit(new Color(0.9f, 0.2f, 0.65f), new Color(1f, 0.25f, 0.7f) * 0.9f, 0.4f);
            var ceilM = Mats.Lit(Theme.Navy, null, 0.2f);          // navy ceiling

            float len = ZMax - ZMin, zc = (ZMax + ZMin) / 2f;
            Gen.Box("Floor", root, new Vector3(0, -0.1f, zc), new Vector3(HalfWidth * 2, 0.2f, len), floorM, true);
            Gen.Box("Ceiling", root, new Vector3(0, Height + 0.1f, zc), new Vector3(HalfWidth * 2, 0.2f, len), ceilM);
            Gen.Box("WallL", root, new Vector3(-HalfWidth - 0.15f, Height / 2, zc), new Vector3(0.3f, Height, len), wallM, true);
            Gen.Box("WallR", root, new Vector3(HalfWidth + 0.15f, Height / 2, zc), new Vector3(0.3f, Height, len), wallM, true);
            Gen.Box("WallFront", root, new Vector3(0, Height / 2, ZMin - 0.15f), new Vector3(HalfWidth * 2, Height, 0.3f), wallM, true);
            // Back wall in three pieces around the door opening (3 m wide, 3 m tall)
            float sideW = HalfWidth - 1.5f;
            Gen.Box("WallBackL", root, new Vector3(-(1.5f + sideW / 2), Height / 2, ZMax + 0.15f), new Vector3(sideW, Height, 0.3f), wallM, true);
            Gen.Box("WallBackR", root, new Vector3((1.5f + sideW / 2), Height / 2, ZMax + 0.15f), new Vector3(sideW, Height, 0.3f), wallM, true);
            Gen.Box("WallBackTop", root, new Vector3(0, 3f + (Height - 3f) / 2, ZMax + 0.15f), new Vector3(3f, Height - 3f, 0.3f), wallM, true);

            // Navy dado band along the lower wall (theme), then the accent stripe + skirting
            foreach (int sd in new[] { -1, 1 })
                Gen.Box("NavyDado", root, new Vector3(sd * (HalfWidth - 0.015f), 0.55f, zc), new Vector3(0.03f, 1.1f, len), Mats.Lit(Theme.NavyMid, null, 0.35f));
            Gen.Box("NavyDadoFront", root, new Vector3(0, 0.55f, ZMin + 0.015f), new Vector3(HalfWidth * 2, 1.1f, 0.03f), Mats.Lit(Theme.NavyMid, null, 0.35f));
            // Teal skirting + a stripe along both walls
            foreach (int s in new[] { -1, 1 })
            {
                Gen.Box("Stripe", root, new Vector3(s * (HalfWidth - 0.02f), 1.25f, zc), new Vector3(0.04f, 0.12f, len), accent);
                Gen.Box("Skirting", root, new Vector3(s * (HalfWidth - 0.02f), 0.1f, zc), new Vector3(0.05f, 0.2f, len), accent);
            }
            // Ceiling light strips, one colour each, sweeping through the spectrum
            for (int i = 0; i < 6; i++)
            {
                var c = Color.HSVToRGB(i / 6f, 0.75f, 1f);
                Gen.Box("LightStrip", root, new Vector3(0, Height - 0.02f, ZMin + 2.5f + i * 3.8f), new Vector3(2.4f, 0.04f, 0.5f),
                    Mats.Lit(Color.white, c * 1.5f, 0.2f));
            }
            // Coloured wall panels behind each station (matches the station accent)
            var panelColors = new[]
            {
                new Color(0.30f, 0.80f, 1.00f), new Color(0.40f, 1.00f, 0.60f), new Color(1.00f, 0.65f, 0.25f), new Color(0.80f, 0.50f, 1.00f),
            };
            float[] panelZ = { -5.0f, -1.6f, 1.8f, 5.2f };
            int[] panelSide = { -1, 1, -1, 1 };
            for (int i = 0; i < 4; i++)
                Gen.Box("WallPanel", root, new Vector3(panelSide[i] * (HalfWidth - 0.05f), 2.2f, panelZ[i]), new Vector3(0.05f, 2.2f, 3.2f),
                    Mats.Lit(panelColors[i] * 0.7f, panelColors[i] * 1.1f, 0.4f));
            // Floor edge lines
            for (int i = 0; i < 2; i++)
            {
                var c = i == 0 ? new Color(0.6f, 0.4f, 1f) : new Color(1f, 0.6f, 0.2f);
                Gen.Box("FloorLine", root, new Vector3((i == 0 ? -1 : 1) * 1.25f, 0.006f, zc), new Vector3(0.06f, 0.012f, len - 1f), Mats.Lit(c * 0.6f, c * 1.4f, 0.3f));
            }

            // Floor guide towards the door (glows green once unlocked)
            guideMat = Mats.LitNew(new Color(0.1f, 0.3f, 0.35f), new Color(0.1f, 0.5f, 0.6f) * 0.5f, 0.3f);
            for (int i = 0; i < 14; i++)
                Gen.Box("Guide", root, new Vector3(0, 0.006f, ZMin + 2f + i * 1.0f), new Vector3(0.5f, 0.012f, 0.25f), guideMat);
        }

        void BuildProps()
        {
            var shelfM = Mats.Lit(new Color(0.75f, 0.8f, 0.85f), null, 0.6f, 0.5f);
            var rnd = new System.Random(9);
            Color[] liquids =
            {
                new Color(0.3f, 0.8f, 1f), new Color(1f, 0.6f, 0.25f), new Color(0.4f, 1f, 0.55f), new Color(0.85f, 0.45f, 1f), new Color(1f, 0.9f, 0.3f),
            };
            // Fallback wall shelving, only used if the Lab Assets models aren't in the project
            if (Resources.Load<GameObject>("LabAssets/counter_counter") == null)
            foreach (int side in new[] { -1, 1 })
            foreach (float z in new[] { -8f, 8f })
            {
                var unit = new GameObject("Shelf").transform;
                unit.SetParent(root, false);
                unit.position = new Vector3(side * (HalfWidth - 0.3f), 0, z);
                Gen.Box("Back", unit, new Vector3(0, 1.4f, 0), new Vector3(0.08f, 2.2f, 2.4f), shelfM);
                for (int lvl = 0; lvl < 3; lvl++)
                {
                    float y = 0.9f + lvl * 0.65f;
                    Gen.Box("Board", unit, new Vector3(-side * 0.14f, y, 0), new Vector3(0.28f, 0.03f, 2.4f), shelfM);
                    for (int i = 0; i < 7; i++)
                    {
                        var c = liquids[rnd.Next(liquids.Length)];
                        float h = 0.12f + (float)rnd.NextDouble() * 0.14f;
                        Gen.Prim(PrimitiveType.Cylinder, "Bottle", unit, new Vector3(-side * 0.14f, y + 0.015f + h / 2f, -1f + i * 0.33f),
                            new Vector3(0.09f, h / 2f, 0.09f), Mats.Lit(c * 0.6f, c * 0.5f, 0.7f));
                    }
                }
            }
            BuildLabEquipment();

            // Floating signage over the start of the lab
            var pos = new Vector3(0, 2.6f, ZMin + 3.2f);
            var title = HoloPanel.Create(root, pos, new Vector2(1300, 260), "MOLECULAR BIOLOGY LAB", "Four ways PCR evolved. Explore each station.", Ui.Cyan, 64, 32);
            title.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        }

        // If counters look sideways in the editor, change this (degrees). Counters should face into the room.
        const float CounterYaw = 90f;

        /// <summary>Real lab furniture and equipment (CC0 Lab Assets), tinted vividly, plus decorative rotating helices.</summary>
        void BuildLabEquipment()
        {
            var vivid = new[]
            {
                new Color(0.2f, 1.1f, 1.2f), new Color(1.3f, 0.35f, 0.9f), new Color(1.3f, 0.8f, 0.2f), new Color(0.8f, 0.55f, 1.4f),
            };
            var glass = new[]
            {
                new Color(0.3f, 0.9f, 1f), new Color(1f, 0.4f, 0.8f), new Color(1f, 0.85f, 0.25f), new Color(0.5f, 1f, 0.5f), new Color(0.7f, 0.5f, 1f),
            };
            string[] counters = { "counter_counter_sink", "counter_counter_3_shelves", "counter_counter", "counter_counter_top_outlet", "counter_counter_2_shelves" };
            float[] zs = { -9f, -7.2f, 6.8f, 8.6f, 10.4f };
            int g = 0;
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < zs.Length; i++)
                {
                    var pos = new Vector3(side * 5.35f, 0f, zs[i]);
                    float yaw = side < 0 ? CounterYaw : -CounterYaw;
                    LabProps.Spawn(counters[(i + (side > 0 ? 1 : 0)) % counters.Length], root, pos, yaw, vivid[(i + (side > 0 ? 2 : 0)) % vivid.Length]);
                    // equipment on top of each counter
                    var top = pos + Vector3.up * 0.9f;
                    switch (i % 5)
                    {
                        case 0: LabProps.Spawn("machine_microscope", root, top, yaw + 180f, Color.white * 1.1f); break;
                        case 1: LabProps.Spawn("bottle_test_tube_rack", root, top, yaw, new Color(1.2f, 0.7f, 0.2f)); break;
                        case 2: LabProps.Spawn("machine_hot_plate", root, top, yaw, Color.white); break;
                        case 3: LabProps.Spawn("machine_centrifuge", root, top, yaw, Color.white * 1.1f); break;
                        default: LabProps.Spawn("machine_electronic_scale", root, top, yaw, Color.white); break;
                    }
                    var gtint = glass[g++ % glass.Length];
                    LabProps.Spawn(g % 2 == 0 ? "bottle_glassware_erlenmeyer_flask_medium" : "bottle_glassware_florence_flask_medium", root,
                        top + new Vector3(side * -0.05f, 0, 0.32f), 0f, gtint);
                    LabProps.Spawn("bottle_glassware_reagent_bottle_small", root, top + new Vector3(side * -0.05f, 0, -0.32f), 0f, glass[(g + 2) % glass.Length]);
                }
            }
            // Safety touches near the entrance
            LabProps.Spawn("ppe_fire_extinguisher_clamp", root, new Vector3(-5.7f, 0f, -10.3f), 90f, new Color(1.4f, 0.3f, 0.3f));
            LabProps.Spawn("ppe_lab_gown", root, new Vector3(5.8f, 0.4f, -10.3f), -90f, Color.white);

            // Decorative rotating helices in the corners of the room (6 draw calls each)
            float[] hz = { -9.5f, 10.5f };
            for (int i = 0; i < hz.Length; i++)
                foreach (int side in new[] { -1, 1 })
                {
                    var h = DnaHelix.Create(root, "FeatureHelix", new Vector3(side * 4.2f, 0.4f, hz[i]), 26, 0.28f, 0.1f, 36f, 40 + i + (side > 0 ? 7 : 0), 0.9f);
                    h.SpinDegPerSec = 24f * side;
                }
        }

        void BuildDoor()
        {
            var frameM = Mats.Lit(Theme.NavyMid, null, 0.5f, 0.3f);
            var leafM = Mats.Lit(new Color(0.78f, 0.82f, 0.87f), null, 0.55f, 0.2f);
            float z = ZMax - 0.05f;

            // Bright room behind the door (revealed when it opens)
            spill = Gen.Box("Beyond", root, new Vector3(0, 1.5f, ZMax + 1.2f), new Vector3(3.2f, 3f, 0.05f),
                Mats.Lit(Color.white, new Color(0.85f, 1f, 1f) * 3.2f), false);
            spill.SetActive(false);
            Gen.Box("FrameL", root, new Vector3(-1.58f, 1.5f, z), new Vector3(0.16f, 3.1f, 0.35f), frameM);
            Gen.Box("FrameR", root, new Vector3(1.58f, 1.5f, z), new Vector3(0.16f, 3.1f, 0.35f), frameM);
            Gen.Box("FrameT", root, new Vector3(0, 3.06f, z), new Vector3(3.3f, 0.16f, 0.35f), frameM);

            leafL = Gen.Box("LeafL", root, new Vector3(-0.75f, 1.5f, z - 0.3f), new Vector3(1.5f, 2.98f, 0.12f), leafM).transform;
            leafR = Gen.Box("LeafR", root, new Vector3(0.75f, 1.5f, z - 0.3f), new Vector3(1.5f, 2.98f, 0.12f), leafM).transform;
            foreach (var l in new[] { leafL, leafR })
                Gen.Box("Window", l, new Vector3(0, 0.2f, -0.55f), new Vector3(0.5f, 0.35f, 0.4f), Mats.Glass(new Color(0.5f, 0.9f, 1f, 0.5f)));

            // Sign (red = locked, green = unlocked)
            signMat = Mats.LitNew(new Color(0.4f, 0.05f, 0.05f), new Color(1f, 0.2f, 0.2f) * 1.6f, 0.3f);
            Gen.Box("Sign", root, new Vector3(0, 3.55f, z - 0.2f), new Vector3(2.6f, 0.55f, 0.06f), signMat);
            var canvas = Ui.Canvas("SignText", root, new Vector2(2600, 550), new Vector3(0, 3.55f, z - 0.25f));
            Ui.Label(canvas.transform, "PCR LAB", 280, Color.white, TextAnchor.MiddleCenter, new Vector2(2600, 550), Vector2.zero, FontStyle.Bold, true);
            canvas.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            canvas.transform.localScale = Vector3.one * 0.001f;

            // Quiet equipment hum from behind the door (3D, so it grows as you approach)
            var a = new GameObject("DoorHum").AddComponent<AudioSource>();
            a.transform.SetParent(root, false);
            a.transform.position = DoorPos + new Vector3(0, 1.5f, 1.5f);
            a.clip = ProcAudio.LabHum; a.loop = true; a.spatialBlend = 1f; a.minDistance = 2f; a.maxDistance = 20f; a.volume = 0.05f;
            a.Play();
        }

        void BuildStations()
        {
            // (x, z, direction pointing away from the player = away from the room centre)
            var layout = new[]
            {
                (new Vector3(-3.4f, 0, -5.0f), Vector3.left,  PcrStation.Kind.EndPoint, new Color(0.30f, 0.80f, 1.00f)),
                (new Vector3( 3.4f, 0, -1.6f), Vector3.right, PcrStation.Kind.QPcr,     new Color(0.40f, 1.00f, 0.60f)),
                (new Vector3(-3.4f, 0,  1.8f), Vector3.left,  PcrStation.Kind.RtPcr,    new Color(1.00f, 0.65f, 0.25f)),
                (new Vector3( 3.4f, 0,  5.2f), Vector3.right, PcrStation.Kind.Nested,   new Color(0.80f, 0.50f, 1.00f)),
            };
            for (int i = 0; i < layout.Length; i++)
            {
                var (pos, dir, kind, col) = layout[i];
                var s = PcrStation.Create(root, pos, dir, kind, i, col);
                s.Completed += OnStationDone;
                stations.Add(s);
            }
        }

        IEnumerator Intro()
        {
            var fader = ScreenFader.Instance;
            fader.SetAlpha(1f);
            yield return new WaitForSeconds(0.5f);
            yield return fader.FadeTo(0f, 1.8f);
            bool introDone = false;
            NarrationManager.Instance.Say("s5_intro", () => introDone = true);
            // Four cards appear one after another
            foreach (var s in stations)
            {
                yield return new WaitForSeconds(1.6f);
                s.Appear();
            }
            while (!introDone) yield return null;
            NarrationManager.Instance.SetObjective("Explore each PCR type (0 / 4)");
        }

        void OnStationDone(PcrStation s)
        {
            completed++;
            if (completed < stations.Count)
            {
                NarrationManager.Instance.SetObjective($"Explore each PCR type ({completed} / {stations.Count})");
                return;
            }
            StartCoroutine(UnlockSequence());
        }

        IEnumerator UnlockSequence()
        {
            NarrationManager.Instance.SetObjective("");
            bool a = false, b = false;
            NarrationManager.Instance.Say("s5_outro", () => a = true);
            while (!a) yield return null;

            doorUnlocked = true;
            signMat.SetColor("_BaseColor", new Color(0.05f, 0.4f, 0.12f));
            signMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.4f) * 1.8f);
            guideMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.5f) * 1.6f);
            ProcAudio.PlayAt(ProcAudio.Chime, DoorPos + Vector3.up * 2f, 1f);
            NarrationManager.Instance.Say("s5_door", () => b = true);
            NarrationManager.Instance.SetObjective("Walk to the PCR LAB door");
            while (!b) yield return null;
        }

        void Update()
        {
            if (!doorUnlocked || doorOpening || head == null || openButton != null) return;
            var d = head.position - DoorPos; d.y = 0;
            if (d.magnitude < 5f)
            {
                var pos = new Vector3(0, 1.3f, DoorPos.z - 1.4f);
                openButton = HoloButton.Create(root, pos, "OPEN DOOR", new Color(0.3f, 1f, 0.55f), new Vector2(0.8f, 0.24f), 44);
                openButton.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                openButton.gameObject.AddComponent<Bob>().Amplitude = 0.02f;
                openButton.Pressed += OpenDoor;
                NarrationManager.Instance.SetObjective("Press OPEN DOOR");
            }
        }

        void OpenDoor()
        {
            if (doorOpening) return;
            doorOpening = true;
            openButton.SetInteractable(false);
            NarrationManager.Instance.SetObjective("");
            StartCoroutine(DoorSequence());
        }

        IEnumerator DoorSequence()
        {
            ProcAudio.PlayAt(ProcAudio.DoorRumble, DoorPos, 1f);
            spill.SetActive(true);
            float t = 0f;
            const float dur = 3.2f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0, 1, t / dur);
                leafL.localPosition = new Vector3(Mathf.Lerp(-0.75f, -2.25f, u), leafL.localPosition.y, leafL.localPosition.z);
                leafR.localPosition = new Vector3(Mathf.Lerp(0.75f, 2.25f, u), leafR.localPosition.y, leafR.localPosition.z);
                doorLight.intensity = Mathf.Lerp(0f, 6f, u);
                yield return null;
            }
            NarrationManager.Instance.Say("s5_open", () =>
                SceneFlow.LoadNext(this, NextScene, () => NarrationManager.Instance.SetObjective("Team 2 scene not linked yet")));
        }
    }
}
#endif
