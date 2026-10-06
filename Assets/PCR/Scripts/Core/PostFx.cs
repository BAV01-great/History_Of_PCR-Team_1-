using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PCR
{
    /// <summary>
    /// Bloom/vignette/tonemapping for PC and PC-VR. Skipped on standalone Quest (Android), where full-screen
    /// post-processing is too expensive; the scenes rely on emissive materials + additive glow sprites there instead.
    /// </summary>
    public static class PostFx
    {
        public static bool Enabled => Application.platform != RuntimePlatform.Android;

        public static void Apply(float bloomIntensity = 0.9f, float bloomThreshold = 1.0f)
        {
            if (!Enabled) return;
            var go = new GameObject("PCR Global Volume");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = p;

            var bloom = p.Add<Bloom>(true);
            bloom.intensity.Override(bloomIntensity);
            bloom.threshold.Override(bloomThreshold);
            bloom.scatter.Override(0.6f);
            bloom.highQualityFiltering.Override(false);

            var tone = p.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            // Punchy, saturated grade for the vibrant look
            var grade = p.Add<ColorAdjustments>(true);
            grade.saturation.Override(28f);
            grade.contrast.Override(10f);

            var vig = p.Add<Vignette>(true);
            vig.color.Override(Theme.DeepNavy);   // navy vignette: the theme at the edges of every scene
            vig.intensity.Override(0.26f);
            vig.smoothness.Override(0.6f);

            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
            }
        }
    }
}
