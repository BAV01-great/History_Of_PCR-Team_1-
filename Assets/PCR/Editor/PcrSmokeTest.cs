using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PCR.EditorTools
{
    /// <summary>
    /// Headless play-through of Scene 1 for checking the build without a person at the keyboard.
    /// Run from the command line (not while the same project is open in the editor):
    ///   Unity -batchmode -projectPath . -executeMethod PCR.EditorTools.PcrSmokeTest.RunScene1 -smokeOut Out -smokeSeconds 420
    /// It plays the scene with Autoplay Demo on at 4x speed, saves a screenshot whenever the guided step changes (and every few seconds),
    /// records every error, exception and assert, and exits with a report (0 = clean, 1 = problems).
    /// </summary>
    public static class PcrSmokeTest
    {
        const string ScenePath = "Assets/Scenes/Scene1_Journey.unity";
        static string outDir;
        static double startReal, nextShot, endAt;
        static string lastStep = "";
        static float stepSeenAt;
        static int shots;
        static bool started, finishedFlagged, sawRealStep;
        static readonly List<string> errors = new List<string>();
        static readonly HashSet<string> warnings = new HashSet<string>();
        static readonly List<string> steps = new List<string>();

        static string Arg(string name, string fallback)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return fallback;
        }

        public static void RunScene1()
        {
            outDir = Arg("-smokeOut", "SmokeOut");
            Directory.CreateDirectory(outDir);
            float seconds = float.Parse(Arg("-smokeSeconds", "420"));
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorSceneManager.OpenScene(ScenePath);
            var jc = Object.FindFirstObjectByType<JourneyController>();
            if (jc == null) { Finish("JourneyController not found in " + ScenePath, 1); return; }
            jc.AutoplayDemo = true;
            if (Arg("-smokeXr", "0") == "1")      // exercise the headset path through the XR Interaction Simulator
            {
                var rs = Object.FindFirstObjectByType<RigSelector>();
                if (rs != null && rs.Simulator != null) rs.Simulator.SetActive(true);
            }
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
            startReal = EditorApplication.timeSinceStartup;
            endAt = startReal + seconds;
            EditorApplication.isPlaying = true;
        }

        // ------------------------------------------------------------------ hands gallery: every pose, one screenshot each
        static int galleryIndex = -1;
        static double galleryNext;
        static bool galleryStarted, galleryShifted;

        public static void RunHandsGallery()
        {
            outDir = Arg("-smokeOut", "SmokeOut");
            Directory.CreateDirectory(outDir);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorSceneManager.OpenScene(ScenePath);
            Application.logMessageReceived += OnLog;
            EditorApplication.update += GalleryTick;
            startReal = EditorApplication.timeSinceStartup;
            galleryNext = startReal + 25;      // let the scene fade in and the hands attach
            EditorApplication.isPlaying = true;
        }

        // ------------------------------------------------------------------ prop close-ups from separate cameras (landing, portal, lab coat)
        static int propStage;
        public static void RunPropShots()
        {
            outDir = Arg("-smokeOut", "SmokeOut");
            Directory.CreateDirectory(outDir);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorSceneManager.OpenScene(ScenePath);
            Application.logMessageReceived += OnLog;
            EditorApplication.update += PropTick;
            startReal = EditorApplication.timeSinceStartup;
            EditorApplication.isPlaying = true;
        }

        static void ShotFrom(string name, Vector3 pos, Vector3 look, float fov = 55f)
        {
            var go = new GameObject("PropCam"); var cam = go.AddComponent<Camera>();
            go.transform.position = pos; go.transform.LookAt(look); cam.fieldOfView = fov;
            var old = Camera.main; if (old != null) old.tag = "Untagged"; go.tag = "MainCamera";
            Shot(name); go.tag = "Untagged"; if (old != null) old.tag = "MainCamera";
            Object.DestroyImmediate(go);
        }

        static void PropTick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (!EditorApplication.isPlaying) { if (propStage >= 5 || now - startReal > 120) { EditorApplication.update -= PropTick; Finish("Prop shots done", 0); } return; }
            double t = now - startReal;
            var land = new Vector3(0, 0, 600);
            if (propStage == 0 && t > 38) { propStage = 1; var ls = Object.FindFirstObjectByType<LandingStage>(); if (ls != null) ls.ForceOpen(); ShotFrom("landing_wide", land + new Vector3(2.5f, 2.6f, -1f), land + new Vector3(0f, 1.6f, 10f), 70f); }
            else if (propStage == 1 && t > 42) { propStage = 2; ShotFrom("portal", land + new Vector3(0.6f, 1.6f, 4.4f), land + new Vector3(0, 1.9f, 9f)); }
            else if (propStage == 2 && t > 44) { propStage = 3; Shot("landing_view"); var hook = new Vector3(-6.3f, 0f, -6.9f); ShotFrom("coat", hook + new Vector3(0.9f, 1.4f, 2.6f), hook + new Vector3(0, 1.15f, 0), 50f); propStage = 4; } else if (propStage == 4 && t > 54) { var mc = Camera.main; Debug.Log("[PCR] PROBE cam=" + (mc != null ? mc.transform.position.ToString() + " far=" + mc.farClipPlane + " bg=" + mc.backgroundColor + " flags=" + mc.clearFlags + " mask=" + mc.cullingMask : "none") + " fader=" + (ScreenFader.Instance != null ? ScreenFader.Instance.transform.parent != null ? ScreenFader.Instance.transform.parent.name : "noparent" : "null")); Shot("landing_view_late"); propStage = 5; EditorApplication.isPlaying = false; }
        }

        // ------------------------------------------------------------------ model probe: sizes and pivots of the imported team models
        public static void RunModelProbe()
        {
            outDir = Arg("-smokeOut", "SmokeOut");
            Directory.CreateDirectory(outDir);
            var sb = new System.Text.StringBuilder();
            foreach (var n in new[] { "wardrobe", "sink", "biosafety_cabinet", "chair_bench", "refrigerator", "clock", "discard_bin" })
            {
                var prefab = Resources.Load<GameObject>("LabAssets/Team/team_" + n);
                if (prefab == null) { sb.AppendLine(n + ": NOT LOADED"); continue; }
                var go = Object.Instantiate(prefab);
                var rs = go.GetComponentsInChildren<Renderer>();
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                int tris = 0; foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) tris += mf.sharedMesh != null ? mf.sharedMesh.triangles.Length / 3 : 0;
                sb.AppendLine($"{n}: size={b.size.ToString("F3")} center={b.center.ToString("F3")} minY={b.min.y:F3} renderers={rs.Length} tris={tris} mats={rs[0].sharedMaterials.Length}");
                Object.DestroyImmediate(go);
            }
            File.WriteAllText(Path.Combine(outDir, "models.txt"), sb.ToString());
            EditorApplication.Exit(0);
        }

        /// <summary>Edit-mode close-up of each team model on a grey backdrop (no play mode needed).</summary>
        public static void RunModelShots()
        {
            outDir = Arg("-smokeOut", "SmokeOut");
            Directory.CreateDirectory(outDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var lg = new GameObject("L").AddComponent<Light>(); lg.type = LightType.Directional; lg.intensity = 1.2f; lg.transform.rotation = Quaternion.Euler(40, 150, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
            var camGo = new GameObject("C"); var cam = camGo.AddComponent<Camera>(); camGo.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.35f, 0.38f, 0.45f);
            foreach (var n in Arg("-smokeModels", "LabAssets/ppe_lab_gown").Split(','))
            {
                var prefab = Resources.Load<GameObject>(n); if (prefab == null) { Debug.LogWarning("missing " + n); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var rs = go.GetComponentsInChildren<Renderer>(); Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float d = b.size.magnitude * 1.1f;
                camGo.transform.position = b.center + new Vector3(0.6f, 0.35f, -1f).normalized * d; camGo.transform.LookAt(b.center);
                Shot(n.Replace("/", "_"));
                Object.DestroyImmediate(go);
            }
            Finish("Model shots done", 0);
        }

        static void GalleryTick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (!EditorApplication.isPlaying) { if (galleryStarted || now - startReal > 120) { EditorApplication.update -= GalleryTick; Finish("Hands gallery done", 0); } return; }
            galleryStarted = true;
            var hands = GloveHands.Instance;
            if (hands == null || now < galleryNext) return;
            if (!galleryShifted) { galleryShifted = true; hands.ShiftRest(new Vector3(-0.06f, 0.14f, 0.02f)); galleryNext = now + 1.5; return; }   // hands to the middle of the view
            var poses = (HandPose[])System.Enum.GetValues(typeof(HandPose));
            if (galleryIndex >= 0) Shot("hands_" + poses[galleryIndex]);
            galleryIndex++;
            if (galleryIndex >= poses.Length) { EditorApplication.isPlaying = false; return; }
            hands.SetPose(true, poses[galleryIndex]); hands.SetPose(false, poses[galleryIndex]);
            galleryNext = now + 1.5;
        }

        static void OnLog(string cond, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                if (!(stack ?? "").Contains("UnityEditor.Search")) errors.Add($"[{type}] {cond}\n    {stack?.Split('\n').FirstOrDefault()}");   // ignore Unity's own search-indexer exception in batch mode
            else if (type == LogType.Warning && warnings.Count < 40) warnings.Add(cond);
            else if (type == LogType.Log && cond.StartsWith("[PCR]")) steps.Add($"{(EditorApplication.timeSinceStartup - startReal):F0}s real: {cond}");
        }

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (!EditorApplication.isPlaying)
            {
                if (started) Finish("Play mode ended", errors.Count > 0 ? 1 : 0);
                else if (now - startReal > 120) Finish("Play mode never started", 1);
                return;
            }
            if (!started) { started = true; Time.timeScale = 4f; nextShot = now + 4; }

            var sm = StepManager.Instance;
            string cur = sm != null && sm.Current != null ? sm.Current.id : (sm != null && !sm.Running ? "(finished)" : "");
            if (cur != lastStep)
            {
                lastStep = cur; stepSeenAt = (float)now;
                steps.Add($"{(now - startReal):F0}s real: step {cur}");
                pending.Add((now + 2.0, "step_" + (cur.Length > 0 ? cur : "none")));
                if (cur == "m1869") { pending.Add((now + 0.15, "flash1_a")); pending.Add((now + 0.3, "flash1_b")); pending.Add((now + 0.45, "flash1_c")); }
                if (cur == "(finished)" && sawRealStep) { pending.Add((now + 0.9, "flash2_a")); pending.Add((now + 1.2, "flash2_b")); pending.Add((now + 1.5, "flash2_c")); pending.Add((now + 1.8, "flash2_d")); }
            }
            for (int i = pending.Count - 1; i >= 0; i--) if (now >= pending[i].at) { Shot(pending[i].name); pending.RemoveAt(i); }
            if (now >= nextShot) { Shot("t" + ((int)(now - startReal)).ToString("D3")); nextShot = now + 9; }

            if (cur.Length > 0 && cur != "(finished)") sawRealStep = true;
            if (cur == "(finished)" && sawRealStep && !finishedFlagged) { finishedFlagged = true; endAt = System.Math.Min(endAt, now + 40); }
            if (now >= endAt) { Shot("end"); EditorApplication.isPlaying = false; }
        }

        static readonly List<(double at, string name)> pending = new List<(double at, string name)>();

        static void Shot(string name)
        {
            var cam = Camera.main;
            if (cam == null) return;
            try
            {
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                var prevT = cam.targetTexture; var prevA = RenderTexture.active;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = prevT;
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                RenderTexture.active = prevA;
                File.WriteAllBytes(Path.Combine(outDir, $"{shots++:D2}_{name}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            }
            catch (System.Exception e) { errors.Add("[Screenshot] " + e.Message); }
        }

        static void Finish(string why, int code)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Scene 1 smoke test: {why}");
            sb.AppendLine($"Steps seen ({steps.Count}):"); foreach (var s in steps) sb.AppendLine("  " + s);
            sb.AppendLine($"Screenshots: {shots}");
            sb.AppendLine($"Errors/exceptions: {errors.Count}"); foreach (var e in errors.Distinct().Take(40)) sb.AppendLine("  " + e);
            sb.AppendLine($"Warnings (first {warnings.Count}):"); foreach (var w in warnings) sb.AppendLine("  " + w);
            File.WriteAllText(Path.Combine(outDir, "report.txt"), sb.ToString());
            EditorApplication.Exit(errors.Count > 0 ? 1 : code);
        }
    }
}
