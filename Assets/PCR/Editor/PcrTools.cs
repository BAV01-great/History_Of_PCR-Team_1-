using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;

namespace PCR.EditorTools
{
    /// <summary>
    /// PCR Tools menu. Run in order:  1 Setup Project  ->  (wait for import/compile)  ->  Build Both Scenes.
    /// </summary>
    public static class PcrTools
    {
        const string OldFolder = "Assets/Scenes/Old";

        // ---------------------------------------------------------------- 1. Setup
        [MenuItem("PCR Tools/1 - Setup Project (XR samples, Quest, shaders)")]
        public static void Setup()
        {
            ImportXrSamples();
            AlwaysIncludeShaders();
            ConfigurePlayerSettings();
            ConfigureUrpAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("[PCR] Setup done. Wait for the sample import to finish, then run 'PCR Tools > Build Both Scenes'.\n" +
                      "Manual step (once): Edit > Project Settings > XR Plug-in Management > enable OpenXR on the PC tab and the Android tab; " +
                      "on Android also tick 'Meta Quest Support' under OpenXR features, and add the interaction profiles " +
                      "(Oculus Touch Controller, Meta Quest Touch Pro/Plus). Then click 'Fix All' in the OpenXR Project Validation window.");
        }

        static void ImportXrSamples()
        {
            int n = 0;
            foreach (var s in UnityEditor.PackageManager.UI.Sample.FindByPackage("com.unity.xr.interaction.toolkit", null))
            {
                bool want = s.displayName.Contains("Starter Assets") || s.displayName.Contains("Simulator");
                if (!want || s.isImported) continue;
                s.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
                n++;
            }
            Debug.Log($"[PCR] Imported {n} XR Interaction Toolkit sample(s).");
        }

        static void AlwaysIncludeShaders()
        {
            string[] names =
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Particles/Unlit",
                "PCR/Outline",
            };
            var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var arr = gs.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in names)
            {
                var sh = Shader.Find(name);
                if (sh == null) continue;
                bool has = false;
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { has = true; break; }
                if (has) continue;
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            }
            gs.ApplyModifiedProperties();
        }

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            // Meta Quest (standalone Android) defaults
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.productName = "History of PCR";
        }

        static void ConfigureUrpAssets()
        {
            foreach (var path in new[] { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null) continue;
                asset.msaaSampleCount = 4;     // MSAA is the right AA for VR
                asset.renderScale = 1f;
                asset.shadowDistance = 20f;
                if (path.Contains("Mobile")) asset.supportsHDR = false; // cheaper on Quest
                EditorUtility.SetDirty(asset);
            }
        }

        // ---------------------------------------------------------------- 2. Scenes
        // The old five-scene plan is archived (define PCR_LEGACY to compile these builders); they write into Assets/Scenes/Old
        // and never touch Build Settings. The current scenes are built by the menu items below the legacy block.
#if PCR_LEGACY
        [MenuItem("PCR Tools/Legacy/Build old Scene 1 (Landing)")]
        public static void BuildOldScene1() => BuildScene(OldFolder + "/Scene1_Landing.unity", Vector3.zero, 0f, go => go.AddComponent<Scene1Controller>());

        [MenuItem("PCR Tools/Legacy/Build old Scene 5 (Types of PCR)")]
        public static void BuildOldScene5() => BuildScene(OldFolder + "/Scene5_TypesOfPCR.unity", new Vector3(0, 0, -9.5f), 0f, go => go.AddComponent<Scene5Controller>());

        [MenuItem("PCR Tools/Legacy/Build old lab scene")]
        public static void BuildOldLab() => BuildScene(OldFolder + "/Scene6_PCRLab.unity", new Vector3(-4.8f, 0, -4.3f), -141f, go => go.AddComponent<LabController>());
#endif

        // ---------------------------------------------------------------- Scene 1 (Journey Through Time)
        const string Scene1Path = "Assets/Scenes/Scene1_Journey.unity";
        const string StepsFolder = "Assets/Resources/Steps";

        [MenuItem("PCR Tools/Build Scene 1 (Journey Through Time)")]
        public static void BuildScene1Journey()
        {
            var seq = CreateScene1Steps();
            // spawn inside the lab entrance, looking towards the coat hook and the DNA beyond
            BuildScene(Scene1Path, new Vector3(-4.8f, 0f, -4.3f), -141f, go =>
            {
                var jc = go.AddComponent<JourneyController>();
                jc.Steps = seq;
            });
            var keep = EditorBuildSettings.scenes.Where(s => s.path != Scene1Path && !s.path.Contains("/Old/")).ToList();
            keep.Insert(0, new EditorBuildSettingsScene(Scene1Path, true));
            EditorBuildSettings.scenes = keep.ToArray();
            EditorSceneManager.OpenScene(Scene1Path);
            Debug.Log("[PCR] Built Scene1_Journey and the Scene1_Steps asset, and set Build Settings. Press Play.");
        }

        /// <summary>Scene 1 steps, straight from the final script's flow. Edit the asset (or this list) to change the flow.</summary>
        static StepSequence CreateScene1Steps()
        {
            System.IO.Directory.CreateDirectory(StepsFolder);
            string path = StepsFolder + "/Scene1_Steps.asset";
            var seq = AssetDatabase.LoadAssetAtPath<StepSequence>(path);
            if (seq == null) { seq = ScriptableObject.CreateInstance<StepSequence>(); AssetDatabase.CreateAsset(seq, path); }
            seq.sequenceId = "scene1"; seq.title = "Journey Through Time";
            seq.steps = new System.Collections.Generic.List<StepDef>
            {
                new StepDef { id = "wear_coat", target = "lab_coat", action = "wear", prompt = "Put on the lab coat", hint = "Put on the lab coat first. It hangs by the door." },
                new StepDef { id = "approach_dna", target = "dna", action = "approach", prompt = "Approach the DNA to begin your journey." },
                new StepDef { id = "start_journey", target = "start_button", action = "press", prompt = "Press the glowing button" },
            };
            foreach (var m in JourneyContent.Milestones)
                seq.steps.Add(new StepDef
                {
                    id = m.StepId, action = "press", gated = true,
                    target = m.StepId == "today" ? "explore_button" : "next_button",
                    prompt = m.StepId == "today" ? "Press EXPLORE PCR TYPES" : "Press NEXT",
                    hint = "Press the highlighted button to continue."
                });
            EditorUtility.SetDirty(seq);
            AssetDatabase.SaveAssets();
            return seq;
        }

        static GameObject FindPrefab(params string[] names)
        {
            foreach (var n in names)
            {
                var guid = AssetDatabase.FindAssets($"{n} t:Prefab")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == n);
                if (guid != null) return AssetDatabase.LoadAssetAtPath<GameObject>(guid);
            }
            return null;
        }

        static void BuildScene(string path, Vector3 spawnPos, float yaw, System.Action<GameObject> addController)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var rigRoot = new GameObject("Player Rig");
            var sel = rigRoot.AddComponent<RigSelector>();
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(0, yaw, 0));
            sel.SpawnPoint = spawn.transform;

            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            var xrPrefab = FindPrefab("Complete XR Origin Set Up", "XR Origin (XR Rig)", "XR Origin Hands (XR Rig)");
            if (xrPrefab != null)
            {
                var xr = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
                xr.transform.SetParent(rigRoot.transform, false);
                xr.SetActive(false); // RigSelector decides at runtime
                sel.XrRig = xr;
            }
            else Debug.LogWarning("[PCR] XR Origin prefab not found. Run 'PCR Tools > 1 - Setup Project' and wait for the sample import, then rebuild. Desktop fallback rig will be used.");

            var simPrefab = FindPrefab("XR Interaction Simulator", "XR Device Simulator");
            if (simPrefab != null)
            {
                var sim = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
                sim.transform.SetParent(rigRoot.transform, false);
                sel.Simulator = sim; // active = use XR rig with simulated controllers in the editor
            }

            var desktop = new GameObject("DesktopRig");
            desktop.transform.SetParent(rigRoot.transform, false);
            desktop.AddComponent<DesktopRig>();
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(desktop.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 150f;
            camGo.AddComponent<AudioListener>();
            desktop.SetActive(false);
            sel.DesktopRig = desktop;

            var ctl = new GameObject("Scene Controller");
            addController(ctl);

            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
