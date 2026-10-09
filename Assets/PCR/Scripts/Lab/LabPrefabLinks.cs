using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    public class LabPrefabLinks : MonoBehaviour
    {
        [Serializable] public struct Tint { public Material Mat; public Color Dim, Bright; }

        public Light Sun;
        public Material Panel, Sky, Lawn, Tree;
        public List<Tint> Tinted = new List<Tint>();

        public void Capture(Transform root)
        {
            Sun = LabLighting.Sun; Panel = LabLighting.Panel; Sky = LabLighting.Sky; Lawn = LabLighting.Lawn; Tree = LabLighting.Tree;
            Tinted.Clear();
            foreach (var (m, dim, bright) in LabLighting.Tinted) Tinted.Add(new Tint { Mat = m, Dim = dim, Bright = bright });
        }

        public void Apply()
        {
            var copies = new Dictionary<Material, Material>();
            Material Own(Material m)
            {
                if (m == null) return null;
                if (!copies.TryGetValue(m, out var c)) copies[m] = c = new Material(m) { name = m.name };
                return c;
            }
            LabLighting.Sun = Sun;
            LabLighting.Panel = Own(Panel); LabLighting.Sky = Own(Sky); LabLighting.Lawn = Own(Lawn); LabLighting.Tree = Own(Tree);
            LabLighting.Tinted.Clear();
            foreach (var t in Tinted) LabLighting.Tinted.Add((Own(t.Mat), t.Dim, t.Bright));
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    bool emissive = m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.0001f;
                    bool transparent = m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f && m.GetFloat("_Blend") < 1.5f;
                    if (emissive || transparent) Own(m);
                }
            foreach (var c in copies.Values)
                if (c.HasProperty("_Surface") && c.GetFloat("_Surface") > 0.5f && c.GetFloat("_Blend") < 1.5f)
                {
                    Mats.MakeTransparent(c, false);
                    c.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    c.DisableKeyword("_ALPHAMODULATE_ON");
                }
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < ms.Length; i++)
                    if (ms[i] != null && copies.TryGetValue(ms[i], out var c)) { ms[i] = c; changed = true; }
                if (changed) r.sharedMaterials = ms;
            }
            foreach (var c in copies.Values)
                if (c.HasProperty("_EmissionColor") && c.GetColor("_EmissionColor").maxColorComponent > 0.0001f) c.EnableKeyword("_EMISSION");
        }
    }
}
