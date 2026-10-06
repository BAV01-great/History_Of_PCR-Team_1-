using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    /// <summary>
    /// URP materials created in code. Few unique materials + SRP-batcher friendly = cheap on Quest.
    /// PCR Tools > Setup adds these shaders to Always Included so builds don't strip them.
    /// </summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static Texture2D glowTex;

        static Shader Find(string name, string fallback)
        {
            var s = Shader.Find(name);
            if (s == null) s = Shader.Find(fallback);
            return s;
        }

        public static Shader LitShader => Find("Universal Render Pipeline/Lit", "Standard");
        public static Shader UnlitShader => Find("Universal Render Pipeline/Unlit", "Unlit/Color");
        public static Shader ParticleShader => Find("Universal Render Pipeline/Particles/Unlit", "Sprites/Default");

        /// <summary>Cached, shared opaque Lit material. Do not modify the result.</summary>
        public static Material Lit(Color color, Color? emission = null, float smoothness = 0.5f, float metallic = 0f)
        {
            string key = $"lit|{color}|{emission}|{smoothness}|{metallic}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = LitNew(color, emission, smoothness, metallic);
            Cache[key] = m;
            return m;
        }

        /// <summary>Unique material instance (use when you animate its emission).</summary>
        public static Material LitNew(Color color, Color? emission = null, float smoothness = 0.5f, float metallic = 0f)
        {
            var m = new Material(LitShader);
            m.SetColor("_BaseColor", color);
            m.color = color;
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            m.enableInstancing = true;
            return m;
        }

        /// <summary>Lit material with a base map (and optional normal map) tiled across a surface, e.g. the concrete lab floor.</summary>
        public static Material LitTextured(Texture2D baseMap, Texture2D normalMap, Vector2 tiling, Color tint, float smoothness = 0.35f)
        {
            string key = $"tex|{(baseMap != null ? baseMap.GetInstanceID() : 0)}|{tiling}|{tint}|{smoothness}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = LitNew(tint, null, smoothness, 0f);
            if (baseMap != null) { m.SetTexture("_BaseMap", baseMap); m.SetTextureScale("_BaseMap", tiling); }
            if (normalMap != null)
            {
                m.SetTexture("_BumpMap", normalMap);
                m.SetTextureScale("_BumpMap", tiling);
                m.SetFloat("_BumpScale", 0.8f);
                m.EnableKeyword("_NORMALMAP");
            }
            Cache[key] = m;
            return m;
        }

        public static Material Glass(Color color, float smoothness = 0.9f)
        {
            string key = $"glass|{color}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitShader);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            MakeTransparent(m, false);
            Cache[key] = m;
            return m;
        }

        public static Material UnlitColor(Color color, bool transparent = false)
        {
            string key = $"unlit|{color}|{transparent}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = UnlitNew(color, transparent);
            Cache[key] = m;
            return m;
        }

        public static Material UnlitNew(Color color, bool transparent = false)
        {
            var m = new Material(UnlitShader);
            m.SetColor("_BaseColor", color);
            m.color = color;
            if (transparent) MakeTransparent(m, false);
            return m;
        }

        /// <summary>Soft additive glow, for particles, halos, lines and holograms.</summary>
        public static Material Glow(Color tint)
        {
            string key = $"glow|{tint}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(ParticleShader);
            m.SetTexture("_BaseMap", GlowTexture());
            m.mainTexture = GlowTexture();
            m.SetColor("_BaseColor", tint);
            m.color = tint;
            MakeTransparent(m, true);
            Cache[key] = m;
            return m;
        }

        /// <summary>Additive material with no texture, for LineRenderers.</summary>
        public static Material Line(Color tint)
        {
            string key = $"line|{tint}";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(ParticleShader);
            m.SetColor("_BaseColor", tint);
            m.color = tint;
            MakeTransparent(m, true);
            Cache[key] = m;
            return m;
        }

        public static void MakeTransparent(Material m, bool additive)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
        }

        public static Texture2D GlowTexture()
        {
            if (glowTex != null) return glowTex;
            const int n = 64;
            glowTex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Pow(1f - d, 2.2f);
                px[y * n + x] = new Color(1, 1, 1, a);
            }
            glowTex.SetPixels(px);
            glowTex.Apply(true, true);
            return glowTex;
        }
    }
}
