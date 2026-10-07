using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    public static class LabLighting
    {
        public static Light Sun;
        public static Material Panel, Sky, Lawn, Tree;
        public static readonly List<(Material m, Color dim, Color bright)> Tinted = new List<(Material, Color, Color)>();

        static readonly Color AmbDim = new Color(0.165f, 0.215f, 0.380f), AmbBright = new Color(0.72f, 0.75f, 0.80f);
        static readonly Color SunDimC = new Color(0.45f, 0.60f, 1f), SunBrightC = new Color(1f, 0.97f, 0.92f);
        static readonly Color FogDim = Theme.DeepNavy, FogBright = new Color(0.82f, 0.88f, 0.95f);
        static readonly Color PanelDim = new Color(0.55f, 0.70f, 1f) * 0.7f, PanelBright = new Color(1f, 1f, 1f) * 1.4f;
        static readonly Color SkyDim = new Color(0.05f, 0.08f, 0.20f), SkyBright = new Color(0.72f, 0.86f, 0.96f);
        static readonly Color LawnDim = new Color(0.020f, 0.045f, 0.045f), LawnBright = new Color(0.28f, 0.52f, 0.25f);
        static readonly Color TreeDim = new Color(0.015f, 0.035f, 0.040f), TreeBright = new Color(0.16f, 0.36f, 0.18f);

        public static float Level { get; private set; }

        public static void Apply(float t)
        {
            Level = t;
            RenderSettings.ambientLight = Color.Lerp(AmbDim, AmbBright, t);
            RenderSettings.fogColor = Color.Lerp(FogDim, FogBright, t);
            if (Sun != null) { Sun.intensity = Mathf.Lerp(0.38f, 0.85f, t); Sun.color = Color.Lerp(SunDimC, SunBrightC, t); }
            if (Panel != null) Panel.SetColor("_EmissionColor", Color.Lerp(PanelDim, PanelBright, t));
            Set(Sky, Color.Lerp(SkyDim, SkyBright, t));
            Set(Lawn, Color.Lerp(LawnDim, LawnBright, t));
            Set(Tree, Color.Lerp(TreeDim, TreeBright, t));
            foreach (var (m, dim, bright) in Tinted) Set(m, Color.Lerp(dim, bright, t));
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = Color.Lerp(Theme.DeepNavy, FogBright, t);
        }

        static void Set(Material m, Color c) { if (m != null) { m.SetColor("_BaseColor", c); m.color = c; } }

        public static IEnumerator Fade(float from, float to, float seconds)
        {
            for (float e = 0; e < seconds; e += Time.deltaTime)
            {
                Apply(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, e / seconds)));
                yield return null;
            }
            Apply(to);
        }
    }
}
