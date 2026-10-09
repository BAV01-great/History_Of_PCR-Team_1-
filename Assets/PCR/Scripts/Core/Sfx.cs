using System.Collections.Generic;
using UnityEngine;

namespace PCR
{
    public static class Sfx
    {
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        static AudioClip Clip(string name)
        {
            if (Cache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("UiSfx/" + name);
            Cache[name] = c;
            return c;
        }

        public static void Play(string name, Vector3 pos, float volume = 0.7f, float pitch = 1f, AudioClip fallback = null)
        {
            var clip = Clip(name);
            if (clip == null) clip = fallback;
            if (clip == null) return;
            var go = new GameObject("Sfx_" + name);
            go.transform.position = pos;
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.volume = volume; a.pitch = pitch; a.spatialBlend = 0.5f;
            a.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        public static void Hover(Vector3 p) => Play("tick_001", p, 0.5f, 1f, ProcAudio.Hover);
        public static void Press(Vector3 p) => Play("select_002", p, 0.8f, 1f, ProcAudio.Blip);
        public static void Correct(Vector3 p) => Play("confirmation_002", p, 0.9f, 1f, ProcAudio.Chime);
        public static void Wrong(Vector3 p) => Play("error_004", p, 0.7f, 1f, ProcAudio.Buzz);
        public static void Pickup(Vector3 p, int n) => Play("pluck_001", p, 0.9f, Mathf.Min(2.4f, 1f + 0.07f * n), ProcAudio.Blip);
        public static void Badge(Vector3 p) => Play("confirmation_001", p, 0.9f, 1f, ProcAudio.Chime);
        public static void Appear(Vector3 p) => Play("maximize_003", p, 0.6f, 1f, ProcAudio.Whoosh);
        public static void DoorOpen(Vector3 p) => Play("open_001", p, 0.8f, 0.7f, ProcAudio.DoorRumble);
        public static void Objective(Vector3 p) => Play("question_002", p, 0.4f, 1f, ProcAudio.Chime);
    }
}
