using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PCR.EditorTools
{
    public class PcrModelPostprocessor : AssetPostprocessor
    {
        const string TeamFolder = "/Resources/PCRModels/";
        const string LabFolder = "/Resources/LabAssets/";
        const string ExtrasFolder = "/Resources/LabExtras/";

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

        public override uint GetVersion() => 3;

        void OnPreprocessModel()
        {
            if (assetPath.Contains("/Resources/PCRModels/Hands/"))
            {
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
                imp.SaveAndReimport();
                count++;
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Resources/LabAssets" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);

            Debug.Log($"[PCR] Upgraded {count} team model(s) with {mats.Count} shared materials in {MatFolder}. Lab props re-imported with Quest-friendly settings.");
        }
    }
}
