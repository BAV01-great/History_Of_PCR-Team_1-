using UnityEngine;

namespace PCR
{
    /// <summary>
    /// Hackathon theme: navy blue on a dark background. One palette, used subtly everywhere
    /// (UI panels, fades, vignette, ceilings, trims, benches) so bright scenes still feel like part of one dark-navy product.
    /// </summary>
    public static class Theme
    {
        public static readonly Color DeepNavy = new Color(0.020f, 0.043f, 0.102f);   // #050B1A, backgrounds, fades, vignette
        public static readonly Color Navy     = new Color(0.039f, 0.082f, 0.188f);   // #0A1530, panels, ceilings, trims
        public static readonly Color NavyMid  = new Color(0.078f, 0.157f, 0.310f);   // #14284F, benches, frames, stools
        public static readonly Color NavyLift = new Color(0.150f, 0.250f, 0.450f);   // #264073, highlights on navy
        public static readonly Color PanelBg  = new Color(0.020f, 0.050f, 0.115f, 0.86f);
        public static readonly Color Accent   = new Color(0.25f, 0.90f, 1f);         // cyan, unchanged
    }
}
