using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PCR
{
    public class NarrationManager : MonoBehaviour
    {
        static NarrationManager instance;
        public static NarrationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("NarrationManager");
                    instance = go.AddComponent<NarrationManager>();
                }
                return instance;
            }
        }

        AudioSource voice;
        GameObject hud;
        Text subtitle, objective;
        RectTransform objCanvas;
        Image subBg, objBg;
        readonly Queue<(string id, Action done)> queue = new Queue<(string, Action)>();
        Coroutine runner;
        bool skip;

        public bool IsSpeaking { get; private set; }

        void Awake()
        {
            instance = this;
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0f;
            voice.playOnAwake = false;
            BuildHud();
        }

        void BuildHud()
        {
            hud = new GameObject("NarrationHUD");
            DontDestroyOnLoad(gameObject);
            hud.transform.SetParent(transform, false);
            var follow = hud.AddComponent<FollowHead>();
            follow.Distance = 1.6f;
            follow.Smooth = 3.5f;

            var sc = Ui.Canvas("Subtitles", hud.transform, new Vector2(1000, 150), new Vector3(0, -0.42f, 0));
            subBg = Ui.Rect(sc.transform, new Color(Theme.DeepNavy.r, Theme.DeepNavy.g, Theme.DeepNavy.b, 0.72f), new Vector2(1000, 150), Vector2.zero);
            subtitle = Ui.Label(sc.transform, "", 34, Color.white, TextAnchor.MiddleCenter, new Vector2(940, 140), Vector2.zero, FontStyle.Normal, true);

            var oc = Ui.Canvas("Objective", hud.transform, new Vector2(1100, 110), new Vector3(0, 0.5f, 0));
            objCanvas = oc.GetComponent<RectTransform>();
            objBg = Ui.Rect(oc.transform, new Color(Theme.Navy.r, Theme.Navy.g, Theme.Navy.b, 0.88f), new Vector2(1100, 110), Vector2.zero);
            objective = Ui.Label(oc.transform, "", 40, Ui.Cyan, TextAnchor.MiddleLeft, new Vector2(1030, 100), new Vector2(0, 0), FontStyle.Bold);
            subBg.enabled = false; objBg.enabled = false;
        }

        public void SetObjective(string text)
        {
            if (objective == null) return;
            objective.text = string.IsNullOrEmpty(text) ? "" : "OBJECTIVE   " + text;
            objBg.enabled = !string.IsNullOrEmpty(text);
            if (!string.IsNullOrEmpty(text))
            {
                const float W = 1100f;
                objective.rectTransform.sizeDelta = new Vector2(W - 70f, 100f);
                float h = Mathf.Max(110f, objective.preferredHeight + 50f);
                objCanvas.sizeDelta = new Vector2(W, h);
                objBg.rectTransform.sizeDelta = new Vector2(W, h);
                objective.rectTransform.sizeDelta = new Vector2(W - 70f, h - 20f);
            }
            if (!string.IsNullOrEmpty(text)) Sfx.Objective(Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }

        public void Say(string id, Action done = null)
        {
            queue.Enqueue((id, done));
            if (runner == null) runner = StartCoroutine(Run());
        }

        public void ShowMessage(string text, float seconds = 3f)
        {
            if (IsSpeaking || subtitle == null) return;
            StartCoroutine(Message(text, seconds));
        }

        IEnumerator Message(string text, float seconds)
        {
            subtitle.text = text;
            subBg.enabled = true;
            yield return new WaitForSeconds(seconds);
            if (!IsSpeaking) { subtitle.text = ""; subBg.enabled = false; }
        }

        public void Skip() => skip = true;

        public void StopAll() { queue.Clear(); skip = true; }

        static List<string> SubtitleChunks(string text, int maxChars)
        {
            var parts = new List<string>();
            if (text.Length <= maxChars) { parts.Add(text); return parts; }
            var sentences = System.Text.RegularExpressions.Regex.Split(text, @"(?<=[.!?])\s+");
            var cur = "";
            foreach (var sen in sentences)
            {
                if (cur.Length > 0 && cur.Length + 1 + sen.Length > maxChars) { parts.Add(cur); cur = sen; }
                else cur = cur.Length == 0 ? sen : cur + " " + sen;
            }
            if (cur.Length > 0) parts.Add(cur);
            return parts;
        }

        IEnumerator Run()
        {
            while (queue.Count > 0)
            {
                var (id, done) = queue.Dequeue();
                string text = PcrText.Get(id);
                var clip = Resources.Load<AudioClip>("Voiceover/" + id);
                IsSpeaking = true;
                subBg.enabled = true;
                float dur;
                if (clip != null) { voice.clip = clip; voice.Play(); dur = clip.length + 0.35f; }
                else { dur = Mathf.Max(2.5f, text.Length / 14f + 1.2f); Debug.Log("[PCR] No voice-over clip for '" + id + "' (Resources/Voiceover/" + id + "), using timed subtitles."); }
                var chunks = SubtitleChunks(text, 150);
                var ends = new float[chunks.Count]; int total = 0;
                foreach (var c in chunks) total += c.Length;
                int acc = 0;
                for (int c = 0; c < chunks.Count; c++) { acc += chunks[c].Length; ends[c] = (clip != null ? clip.length : dur) * acc / Mathf.Max(1, total); }

                float t = 0f;
                skip = false;
                while (t < dur && !skip)
                {
                    int shown = 0;
                    while (shown < chunks.Count - 1 && t > ends[shown]) shown++;
                    if (subtitle.text != chunks[shown]) subtitle.text = chunks[shown];
                    t += Time.deltaTime;
                    yield return null;
                }
                voice.Stop();
                subtitle.text = "";
                subBg.enabled = false;
                IsSpeaking = false;
                done?.Invoke();
                yield return new WaitForSeconds(0.25f);
            }
            runner = null;
        }
    }
}
