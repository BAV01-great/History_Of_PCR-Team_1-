using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PCR.EditorTools
{
    public static class PcrLabPrefab
    {
        const string Folder = "Assets/Resources/LabPrefab";

        [MenuItem("PCR Tools/Save Lab Prefab")]
        public static void Save()
        {
            var prev = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Folder + "/Materials");
            AssetDatabase.Refresh();
            foreach (var f in Directory.GetFiles(Folder + "/Materials", "*.mat")) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));

            LabLighting.Tinted.Clear();
            var holder = new GameObject("LabHolder").transform;
            LabRoom.Build(holder);
            var lab = holder.GetChild(0).gameObject;
            lab.transform.SetParent(null, false);
            Object.DestroyImmediate(holder.gameObject);

            var made = new Dictionary<Material, Material>();
            int n = 0, textures = 0;
            Material Persist(Material m)
            {
                if (m == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) return m;
                if (made.TryGetValue(m, out var saved)) return saved;
                foreach (var id in m.GetTexturePropertyNames())
                {
                    var tex = m.GetTexture(id);
                    if (tex != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex))) { textures++; m.SetTexture(id, null); }
                }
                string path = AssetDatabase.GenerateUniqueAssetPath($"{Folder}/Materials/{Sanitize(m.name)}_{n++}.mat");
                AssetDatabase.CreateAsset(m, path);
                if (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.0001f)
                {
                    m.EnableKeyword("_EMISSION");
                    EditorUtility.SetDirty(m);
                }
                made[m] = m;
                return m;
            }
            foreach (var r in lab.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) ms[i] = Persist(ms[i]);
                r.sharedMaterials = ms;
            }
            var links = lab.GetComponent<LabPrefabLinks>();
            if (links != null)
            {
                links.Panel = Persist(links.Panel); links.Sky = Persist(links.Sky); links.Lawn = Persist(links.Lawn); links.Tree = Persist(links.Tree);
                for (int i = 0; i < links.Tinted.Count; i++) { var t = links.Tinted[i]; t.Mat = Persist(t.Mat); links.Tinted[i] = t; }
            }
            AssetDatabase.SaveAssets();
            foreach (var m in made.Values)
            {
                if (!m.HasProperty("_EmissionColor") || m.GetColor("_EmissionColor").maxColorComponent <= 0.0001f) continue;
                var so = new SerializedObject(m);
                var list = so.FindProperty("m_ValidKeywords");
                bool has = false;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).stringValue == "_EMISSION") has = true;
                if (!has) { list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = "_EMISSION"; so.ApplyModifiedPropertiesWithoutUndo(); }
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();

            string prefabPath = Folder + "/Lab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(lab, prefabPath);
            Object.DestroyImmediate(lab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!string.IsNullOrEmpty(prev)) EditorSceneManager.OpenScene(prev);
            Debug.Log($"[PCR] Saved the lab prefab: {prefabPath} ({(prefab != null ? prefab.GetComponentsInChildren<Transform>(true).Length : 0)} objects, {made.Count} materials{(textures > 0 ? ", " + textures + " runtime textures dropped" : "")}).");
        }

        static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return string.IsNullOrWhiteSpace(s) ? "Mat" : s.Replace(" ", "_");
        }
    }
}
