using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    /// <summary>
    /// Sound design for Scene 1 (the final script's Sound Design section). One continuous music track built from layers that come in as the story
    /// progresses: A pad (whole experience), B pulse (1983), C strings (1988), D sparkle (1990s, carried into Scene 2); thinned to the pad at the
    /// Nobel moment. A glassy DNA hum follows the DNA and rises as the player nears. Music is ducked while the voice plays.
    /// Everything is procedural so the scene is never silent; a file named like a cue in Resources/Sfx replaces the stand-in.
    /// Palette is soft on purpose: no heavy bass or harsh sounds in a headset.
    /// </summary>
    public class JourneyAudio : MonoBehaviour
    {
        const float LoopSeconds = 8f;
        readonly Dictionary<char, AudioSource> layers = new Dictionary<char, AudioSource>();
        readonly Dictionary<char, float> layerTarget = new Dictionary<char, float>();
        readonly Dictionary<string, AudioClip> cueCache = new Dictionary<string, AudioClip>();
        AudioSource hum, room, sfx;
        Transform humTarget;
        float duck = 1f, nextPitch = 1f;

        static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);

        void Awake()
        {
            layers['A'] = Loop("MusicPad", MakeLayerA(), 0f);
            layers['B'] = Loop("MusicPulse", MakeLayerB(), 0f);
            layers['C'] = Loop("MusicStrings", MakeLayerC(), 0f);
            layers['D'] = Loop("MusicSparkle", MakeLayerD(), 0f);
            foreach (var k in layers.Keys) layerTarget[k] = 0f;
            hum = Loop("DnaHum", ProcAudio.Make("dnahum", 2f, (t, u) => (Sin(330f, t) * 0.5f + Sin(660f, t) * 0.22f + Sin(992f, t) * 0.08f) * (0.8f + 0.2f * Sin(3f, t)) * 0.5f), 0f);
            hum.spatialBlend = 1f; hum.minDistance = 1.5f; hum.maxDistance = 14f;
            room = Loop("RoomTone", ProcAudio.LabHum, 0f);
            sfx = new GameObject("Cues").AddComponent<AudioSource>();
            sfx.transform.SetParent(transform, false);
            sfx.spatialBlend = 0f;
        }

        AudioSource Loop(string name, AudioClip clip, float vol)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.loop = true; a.volume = vol; a.spatialBlend = 0f; a.playOnAwake = false;
            a.Play();
            return a;
        }

        // ------------------------------------------------------------------ music layers (8 s loops; frequencies are multiples of 1/8 Hz so they loop cleanly)
        static AudioClip MakeLayerA() => ProcAudio.Make("layerA", LoopSeconds, (t, u) =>
            (Sin(220f, t) * 0.5f + Sin(261.625f, t) * 0.35f + Sin(329.625f, t) * 0.3f + Sin(392f, t) * 0.22f + Sin(110f, t) * 0.25f) * (0.75f + 0.25f * Sin(0.125f, t)) * 0.35f);

        static AudioClip MakeLayerB() => ProcAudio.Make("layerB", LoopSeconds, (t, u) =>
        {
            float ph = Mathf.Repeat(t * 2f, 1f);                 // a soft pulse every half second
            return Sin(110f, t) * Mathf.Exp(-ph * 7f) * 0.5f;
        });

        static AudioClip MakeLayerC() => ProcAudio.Make("layerC", LoopSeconds, (t, u) =>
        {
            float v = 0f;
            foreach (float f in new[] { 220f, 277.25f, 329.625f })
                for (int h = 1; h <= 4; h++) v += Sin(f * h, t) / (h * h);   // warm, band-limited "strings"
            return v * 0.22f * (0.7f + 0.3f * Sin(0.25f, t));
        });

        static AudioClip MakeLayerD() => ProcAudio.Make("layerD", LoopSeconds, (t, u) =>
        {
            // light plucks on a fixed pattern
            float[] notes = { 1318.5f, 1568f, 1975.5f, 1568f, 2093f, 1568f, 1318.5f, 1760f };
            int i = Mathf.FloorToInt(t / 0.5f) % notes.Length;
            float ph = Mathf.Repeat(t, 0.5f);
            return Sin(notes[i], t) * Mathf.Exp(-ph * 6f) * 0.22f;
        });

        public void Begin()
        {
            layerTarget['A'] = 0.30f;
            room.volume = 0.05f;
            StartCoroutine(Drips());
        }

        IEnumerator Drips()
        {
            while (true) { yield return new WaitForSeconds(Random.Range(6f, 13f)); if (LabLighting.Level < 0.5f) Cue("drip"); }
        }

        /// <summary>Music layers by milestone: B from 1983, C from 1988, pad only at the Nobel, B+C+D from the 1990s.</summary>
        public void OnMilestone(int i)
        {
            layerTarget['B'] = i >= 2 && i != 5 ? 0.22f : 0f;
            layerTarget['C'] = i >= 4 && i != 5 ? 0.30f : 0f;
            layerTarget['D'] = i >= 6 ? 0.22f : 0f;
            layerTarget['A'] = i == 5 ? 0.20f : 0.30f;
        }

        /// <summary>Scene 2 mix: all layers, warmest.</summary>
        public void OnSceneTwo() { layerTarget['A'] = 0.34f; layerTarget['B'] = 0.26f; layerTarget['C'] = 0.34f; layerTarget['D'] = 0.28f; }

        // ------------------------------------------------------------------ DNA hum
        public void SetHum(Transform t, float near)
        {
            humTarget = t;
            if (t == null) { hum.volume = 0f; return; }
            hum.transform.position = t.position;
            hum.volume = Mathf.Lerp(0.04f, 0.30f, near);
            hum.pitch = Mathf.Lerp(0.9f, 1.35f, near);   // rises in pitch and volume as the user nears
        }

        void Update()
        {
            bool speaking = NarrationManager.Instance != null && NarrationManager.Instance.IsSpeaking;
            duck = Mathf.MoveTowards(duck, speaking ? 0.4f : 1f, Time.deltaTime * 2.5f);   // about -8 dB under the voice
            foreach (var kv in layers)
                kv.Value.volume = Mathf.MoveTowards(kv.Value.volume, layerTarget[kv.Key] * duck, Time.deltaTime * 0.18f);
            if (humTarget != null) hum.transform.position = humTarget.position;
        }

        // ------------------------------------------------------------------ cues
        public void Cue(string name, float volume = 0.7f)
        {
            var clip = Clip(name);
            if (clip == null) return;
            float pitch = 1f;
            if (name == "next") { nextPitch += 0.04f; pitch = nextPitch; }   // NEXT: slightly higher each time
            sfx.pitch = pitch;
            sfx.PlayOneShot(clip, volume * Mathf.Lerp(1f, 0.6f, 1f - duck));
        }

        AudioClip Clip(string name)
        {
            if (cueCache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Sfx/" + name) ?? Make(name);
            cueCache[name] = c;
            return c;
        }

        static AudioClip Make(string n)
        {
            var rnd = new System.Random(n.GetHashCode());
            float lp = 0f;
            float Noise() => (float)(rnd.NextDouble() * 2 - 1);
            switch (n)
            {
                case "tick": return ProcAudio.Make("c_tick", 0.05f, (t, u) => Sin(1500f, t) * (1f - u) * 0.18f);
                case "pop": return ProcAudio.Make("c_pop", 0.12f, (t, u) => Sin(520f * (1f + u), t) * Mathf.Exp(-u * 6f) * 0.4f);
                case "click": return ProcAudio.Blip;
                case "ping": return ProcAudio.Make("c_ping", 0.5f, (t, u) => (Sin(1760f, t) + Sin(2637f, t) * 0.4f) * Mathf.Exp(-u * 5f) * 0.2f);
                case "chime": return ProcAudio.Chime;
                case "whoosh": return ProcAudio.Whoosh;
                case "revwhoosh": return ProcAudio.Make("c_rev", 1.4f, (t, u) => { lp += (Noise() - lp) * Mathf.Lerp(0.25f, 0.02f, u); return lp * 1.6f * u * u; });
                case "next": return ProcAudio.Make("c_next", 0.3f, (t, u) => { lp += (Noise() - lp) * Mathf.Lerp(0.04f, 0.25f, u); return lp * 1.2f * Mathf.Sin(u * Mathf.PI) * 0.8f; });
                case "shimmer": return ProcAudio.Make("c_shimmer", 2.4f, (t, u) => (Sin(880f, t) + Sin(1320f, t) * 0.6f + Sin(1760f, t) * 0.4f) * Mathf.Sin(u * Mathf.PI) * (0.6f + 0.4f * Sin(6f, t)) * 0.1f);
                case "flash": return ProcAudio.Make("c_flash", 0.9f, (t, u) => (Sin(Mathf.Lerp(2400f, 300f, u), t) * 0.4f + Noise() * 0.3f) * Mathf.Exp(-u * 4f) * 0.6f);
                case "riser": return ProcAudio.Make("c_riser", 1.2f, (t, u) => Sin(Mathf.Lerp(330f, 880f, u * u), t) * Mathf.Sin(u * Mathf.PI) * 0.18f);
                case "powerup": return ProcAudio.Make("c_power", 3.6f, (t, u) => (Sin(Mathf.Lerp(120f, 880f, u * u), t) * 0.4f + Sin(Mathf.Lerp(240f, 1320f, u * u), t) * 0.2f) * Mathf.Sin(Mathf.Min(1f, u * 1.15f) * Mathf.PI * 0.5f) * (1f - u * 0.4f) * 0.35f);
                case "drip": return ProcAudio.Make("c_drip", 0.25f, (t, u) => Sin(Mathf.Lerp(1500f, 600f, u), t) * Mathf.Exp(-u * 9f) * 0.18f);
                case "pluck": return ProcAudio.Make("c_pluck", 0.5f, (t, u) => (Sin(660f, t) + Sin(1320f, t) * 0.3f) * Mathf.Exp(-u * 7f) * 0.3f);
                case "bubble": return ProcAudio.Make("c_bubble", 0.6f, (t, u) => Sin(Mathf.Lerp(400f, 900f, Mathf.Repeat(t * 6f, 1f)), t) * Mathf.Sin(u * Mathf.PI) * 0.12f);
                case "idea": return ProcAudio.Make("c_idea", 0.6f, (t, u) => (Sin(1568f, t) + Sin(2349f, t) * 0.5f) * Mathf.Exp(-u * 4f) * 0.25f);
                case "rustle": return ProcAudio.Make("c_rustle", 0.5f, (t, u) => { lp += (Noise() - lp) * 0.35f; return lp * Mathf.Sin(u * Mathf.PI) * 0.35f; });
                case "swirl": return ProcAudio.Make("c_swirl", 2f, (t, u) => Sin(Mathf.Lerp(300f, 1200f, u) + 40f * Sin(5f, t), t) * Mathf.Sin(u * Mathf.PI) * 0.2f);
                case "sizzle": return ProcAudio.Make("c_sizzle", 1.2f, (t, u) => { float n1 = Noise(); return (n1 * n1 * n1) * Mathf.Exp(-u * 3f) * 0.6f; });
                case "crack": return ProcAudio.Make("c_crack", 0.2f, (t, u) => Noise() * Mathf.Exp(-u * 18f) * 0.7f);
                case "warm": return ProcAudio.Make("c_warm", 1.6f, (t, u) => (Sin(220f, t) + Sin(330f, t) * 0.6f + Sin(440f, t) * 0.3f) * Mathf.Sin(u * Mathf.PI) * 0.2f);
                case "nobel": return ProcAudio.Make("c_nobel", 4f, (t, u) =>
                {
                    float swell = Mathf.Pow(Mathf.Clamp01(u / 0.55f), 2f) * (1f - Mathf.Clamp01((u - 0.7f) / 0.3f));
                    float chord = Sin(261.625f, t) + Sin(329.625f, t) + Sin(392f, t) + Sin(523.25f, t) * 0.7f;
                    float shine = (Sin(2349f, t) + Sin(3136f, t) * 0.6f) * Mathf.Exp(-Mathf.Abs(u - 0.55f) * 12f) * 0.2f;
                    return chord * swell * 0.22f + shine;
                });
                case "applause": return ProcAudio.Make("c_applause", 2.2f, (t, u) => { lp += (Noise() - lp) * 0.5f; return lp * Mathf.Exp(-u * 2.2f) * (0.5f + 0.5f * Mathf.Abs(Sin(9f, t))) * 0.35f; });
                case "sparkle": return ProcAudio.Make("c_sparkle", 0.6f, (t, u) => (Sin(2637f, t) + Sin(3520f, t) * 0.5f) * Mathf.Exp(-u * 8f) * 0.2f);
                case "ticks": return ProcAudio.Make("c_ticks", 2f, (t, u) => { float rate = Mathf.Lerp(3f, 16f, u); float ph = Mathf.Repeat(t * rate, 1f); return Sin(1800f, t) * Mathf.Exp(-ph * 30f) * (1f - u) * 0.25f; });
                case "split": return ProcAudio.Make("c_split", 0.8f, (t, u) => (Sin(70f, t) * 0.4f + Noise() * 0.25f * Mathf.Exp(-u * 20f)) * Mathf.Exp(-u * 4f));
                default: return ProcAudio.Blip;
            }
        }
    }
}
