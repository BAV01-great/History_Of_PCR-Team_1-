using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    public static class ProcAudio
    {
        const int SampleRate = 44100;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip Make(string key, float seconds, Func<float, float, float> sample)
        {
            if (Cache.TryGetValue(key, out var c) && c != null) return c;
            int n = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(sample(t, t / seconds), -1f, 1f);
            }
            var clip = AudioClip.Create(key, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            Cache[key] = clip;
            return clip;
        }

        static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);

        public static AudioClip Blip => Make("blip", 0.09f, (t, u) => Sin(1100f, t) * (1f - u) * 0.35f);

        public static AudioClip Hover => Make("hover", 0.05f, (t, u) => Sin(1600f, t) * (1f - u) * 0.12f);

        public static AudioClip Chime => Make("chime", 0.7f, (t, u) =>
        {
            float e = Mathf.Exp(-4f * u);
            float a = Sin(660f, t);
            float b = t > 0.12f ? Sin(880f, t) : 0f;
            float c = t > 0.24f ? Sin(1320f, t) : 0f;
            return (a + b + c) * 0.18f * e;
        });

        public static AudioClip Buzz => Make("buzz", 0.22f, (t, u) =>
            (Mathf.Sign(Sin(110f, t)) * 0.5f + Sin(160f, t) * 0.5f) * 0.22f * (1f - u));

        public static AudioClip Whoosh
        {
            get
            {
                var rnd = new System.Random(7);
                float lp = 0f;
                return Make("whoosh", 1.2f, (t, u) =>
                {
                    float noise = (float)(rnd.NextDouble() * 2 - 1);
                    lp += (noise - lp) * Mathf.Lerp(0.02f, 0.25f, Mathf.Sin(u * Mathf.PI));
                    return lp * 1.6f * Mathf.Sin(u * Mathf.PI);
                });
            }
        }

        public static AudioClip LabHum => Make("labhum", 2f, (t, u) =>
        {
            float am = 0.75f + 0.25f * Sin(2f, t);
            return (Sin(100f, t) * 0.5f + Sin(200f, t) * 0.25f + Sin(401f, t) * 0.08f) * am * 0.5f;
        });

        public static AudioClip Ambient => Make("ambient", 4f, (t, u) =>
            (Sin(55f, t) * 0.4f + Sin(110.5f, t) * 0.25f + Sin(165f, t) * 0.12f) * (0.8f + 0.2f * Sin(0.5f, t)) * 0.5f);

        public static AudioClip DoorRumble
        {
            get
            {
                var rnd = new System.Random(3);
                float lp = 0f;
                return Make("rumble", 2.4f, (t, u) =>
                {
                    float noise = (float)(rnd.NextDouble() * 2 - 1);
                    lp += (noise - lp) * 0.04f;
                    return (lp * 3f + Sin(48f, t) * 0.4f) * Mathf.Sin(u * Mathf.PI) * 0.7f;
                });
            }
        }

        public static void PlayAt(AudioClip clip, Vector3 pos, float volume = 0.6f)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, pos, volume);
        }
    }
}
