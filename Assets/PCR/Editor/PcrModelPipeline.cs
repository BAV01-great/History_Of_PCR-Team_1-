using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PCR.EditorTools
{
    /// <summary>
    /// Non-destructive upgrade of the team's Blender models (Assets/Resources/PCRModels) and the CC0 lab props (Assets/Resources/LabAssets):
    ///  - import settings tuned for Quest: mesh compression, no read/write, vertex/polygon optimisation, no cameras/lights/animation,
    ///    and smoother shading on the organic DNA/Taq pieces;
    ///  - one shared, glossy, subtly glowing URP material per colour in the handoff colour key, remapped onto every model
    ///    (a handful of materials for the whole molecule set = batching-friendly, GPU instancing on).
    /// The original FBX files are never modified. Run: PCR Tools > 4 - Upgrade Team Models.
    /// </summary>
    public class PcrModelPostprocessor : AssetPostprocessor
    {
        const string TeamFolder = "/Resources/PCRModels/";
        const string LabFolder = "/Resources/LabAssets/";
        const string ExtrasFolder = "/Resources/LabExtras/";

        // Textures for the sourced lab extras: 1K max, crunch-compressed, mipmapped; *_nor_* are normal maps.
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains(ExtrasFolder)) return;
            var t = (TextureImporter)assetImporter;
            t.maxTextureSize = 1024;
            t.textureCompression = TextureImporterCompression.Compressed;
            t.crunchedCompression = true;
            t.compressionQuality = 50;
            t.mipmapEnabled = true;
            if (assetPath.Contains("_nor_")) t.textureType = TextureImporterType.NormalMap;
        }

        // bump when the import rules change so Unity re-imports the models
        public override uint GetVersion() => 3;

        void OnPreprocessModel()
        {
            if (assetPath.Contains("/Resources/PCRModels/Hands/"))
            {
                // the rigged hand models keep their skin and bones (the finger bones are driven by GloveHands)
                var hm = (ModelImporter)assetImporter;
                hm.animationType = ModelImporterAnimationType.Generic;
                hm.importAnimation = false;
                hm.importCameras = false; hm.importLights = false; hm.importBlendShapes = false;
                hm.isReadable = false;
                hm.meshCompression = ModelImporterMeshCompression.Off;
                hm.optimizeGameObjects = false;
                return;
            }
            bool team = assetPath.Contains(TeamFolder), lab = assetPath.Contains(LabFolder) || assetPath.Contains(ExtrasFolder);
            if (!team && !lab) return;
            var m = (ModelImporter)assetImporter;
            m.importCameras = false;
            m.importLights = false;
            m.importBlendShapes = false;
            m.importVisibility = false;
            m.importAnimation = false;
            m.animationType = ModelImporterAnimationType.None;
            m.isReadable = false;
            m.meshCompression = ModelImporterMeshCompression.Medium;
            m.optimizeMeshPolygons = true;
            m.optimizeMeshVertices = true;
            m.weldVertices = true;
            m.generateSecondaryUV = false;
            if (team)
            {
                // Smoother, rounder-looking DNA without adding triangles
                m.importNormals = ModelImporterNormals.Calculate;
                m.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
                m.normalSmoothingAngle = 70f;
            }
        }
    }

    public static class PcrModelPipeline
    {
        const string MatFolder = "Assets/Art/Materials/PCR";

        struct Spec { public string Name; public Color Color; public float Emission; public float Smoothness; public float Metallic; }

        // Colour key from History_of_PCR_3D_Asset_Handoff.docx
        static readonly Spec[] Specs =
        {
            new Spec { Name = "Mat_Backbone_A",        Color = new Color(0.30f, 0.33f, 0.38f), Emission = 0.15f, Smoothness = 0.60f },
            new Spec { Name = "Mat_Backbone_B",        Color = new Color(0.78f, 0.82f, 0.88f), Emission = 0.15f, Smoothness = 0.65f },
            new Spec { Name = "Mat_Primer_Backbone",   Color = new Color(0.95f, 0.20f, 0.70f), Emission = 0.50f, Smoothness = 0.60f },
            new Spec { Name = "Mat_Primer_Backbone_R", Color = new Color(1.00f, 0.65f, 0.80f), Emission = 0.40f, Smoothness = 0.60f },
            new Spec { Name = "Mat_Base_A",            Color = new Color(1.00f, 0.25f, 0.25f), Emission = 0.60f, Smoothness = 0.50f },
            new Spec { Name = "Mat_Base_T",            Color = new Color(0.25f, 0.45f, 1.00f), Emission = 0.60f, Smoothness = 0.50f },
            new Spec { Name = "Mat_Base_C",            Color = new Color(0.25f, 0.90f, 0.40f), Emission = 0.60f, Smoothness = 0.50f },
            new Spec { Name = "Mat_Base_G",            Color = new Color(1.00f, 0.85f, 0.20f), Emission = 0.60f, Smoothness = 0.50f },
            new Spec { Name = "Mat_Cap_5prime",        Color = new Color(0.95f, 0.95f, 0.95f), Emission = 0.30f, Smoothness = 0.70f },
            new Spec { Name = "Mat_Phosphate",         Color = new Color(0.55f, 0.58f, 0.65f), Emission = 0.00f, Smoothness = 0.50f },
            new Spec { Name = "Mat_Taq",               Color = new Color(1.00f, 0.55f, 0.10f), Emission = 0.40f, Smoothness = 0.75f },
            new Spec { Name = "Mat_Taq_Broken",        Color = new Color(0.45f, 0.22f, 0.08f), Emission = 0.10f, Smoothness = 0.40f },
        };

        [MenuItem("PCR Tools/4 - Upgrade Team Models (materials + import settings)")]
        public static void UpgradeTeamModels()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("[PCR] URP Lit shader not found. Is the project using URP?"); return; }
            Directory.CreateDirectory(MatFolder);

            var mats = new Dictionary<string, Material>();
            foreach (var s in Specs)
            {
                string path = $"{MatFolder}/{s.Name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                m.SetColor("_BaseColor", s.Color);
                m.SetFloat("_Smoothness", s.Smoothness);
                m.SetFloat("_Metallic", s.Metallic);
                if (s.Emission > 0f)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", s.Color * s.Emission);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                mats[s.Name] = m;
            }
            AssetDatabase.SaveAssets();

            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Resources/PCRModels" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) continue;
                foreach (var kv in mats)
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                imp.SaveAndReimport(); // also re-runs OnPreprocessModel
                count++;
            }
            // lab props: import settings only (their colours come from the shared atlas and are tinted at runtime)
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Resources/LabAssets" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);

            Debug.Log($"[PCR] Upgraded {count} team model(s) with {mats.Count} shared materials in {MatFolder}. Lab props re-imported with Quest-friendly settings.");
        }
    }
}
