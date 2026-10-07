using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PCR
{
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;
        public static ScreenFader Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameObject("ScreenFader").AddComponent<ScreenFader>();
                return instance;
            }
        }

        Material mat;
        Renderer quad;
        float alpha = 1f;
        public float Alpha => alpha;
        Color tint = Theme.DeepNavy;

        void Awake()
        {
            instance = this;
            mat = Mats.UnlitNew(new Color(Theme.DeepNavy.r, Theme.DeepNavy.g, Theme.DeepNavy.b, 1f), true);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "FadeQuad";
            q.transform.SetParent(transform, false);
            q.transform.localScale = Vector3.one * 3f;
            quad = q.GetComponent<Renderer>();
            quad.sharedMaterial = mat;
            quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Apply();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (transform.parent != cam.transform)
            {
                transform.SetParent(cam.transform, false);
                transform.localPosition = new Vector3(0, 0, 0.3f);
                transform.localRotation = Quaternion.identity;
            }
        }

        void Apply()
        {
            mat.SetColor("_BaseColor", new Color(tint.r, tint.g, tint.b, alpha));
            quad.enabled = alpha > 0.002f;
        }

        public void SetAlpha(float a) { alpha = a; Apply(); }

        public void SetTint(Color c) { tint = c; Apply(); }

        public IEnumerator FadeTo(float target, float seconds)
        {
            float start = alpha, t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t / seconds));
                Apply();
                yield return null;
            }
            alpha = target;
            Apply();
        }
    }

    public static class SceneFlow
    {
        public static void LoadNext(MonoBehaviour host, string sceneName, Action onMissing = null)
        {
            host.StartCoroutine(Go(sceneName, onMissing));
        }

        static IEnumerator Go(string sceneName, Action onMissing)
        {
            var f = ScreenFader.Instance;
            yield return f.FadeTo(1f, 1.2f);
            if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[PCR] Next scene '{sceneName}' is not in Build Settings yet. Add it (File > Build Profiles) to hand off.");
                onMissing?.Invoke();
                yield return f.FadeTo(0f, 1.2f);
            }
        }
    }
}
