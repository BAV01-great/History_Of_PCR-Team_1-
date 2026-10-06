using UnityEngine;
using UnityEngine.UI;

namespace PCR
{
    /// <summary>World-space UI helpers. Display-only canvases: interaction is done by 3D HoloButtons, which work with ray, poke and desktop.</summary>
    public static class Ui
    {
        static Font font;
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static readonly Color Cyan = new Color(0.25f, 0.9f, 1f);
        public static readonly Color Panel = Theme.PanelBg;

        /// <summary>World-space canvas. size is in canvas pixels; 1000 px = 1 metre.</summary>
        public static Canvas Canvas(string name, Transform parent, Vector2 size, Vector3 localPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * 0.001f;
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            go.GetComponent<RectTransform>().sizeDelta = size;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
            return c;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor,
            Vector2 boxSize, Vector2 anchoredPos, FontStyle style = FontStyle.Normal, bool outline = false)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = boxSize;
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = go.AddComponent<Outline>();
                o.effectColor = new Color(0, 0, 0, 0.9f);
                o.effectDistance = new Vector2(2, -2);
            }
            return t;
        }

        public static Image Rect(Transform parent, Color color, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject("Rect", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }

    /// <summary>Floating info panel: dark glass card with an accent bar. Pops in when created.</summary>
    public class HoloPanel : MonoBehaviour
    {
        public Text Title, Body;
        public Image Accent;
        float t;
        Vector3 targetScale = Vector3.one;

        public static HoloPanel Create(Transform parent, Vector3 localPos, Vector2 size, string title, string body, Color accent,
            int titleSize = 46, int bodySize = 30)
        {
            var canvas = Ui.Canvas("HoloPanel", parent, size, localPos);
            var p = canvas.gameObject.AddComponent<HoloPanel>();
            Ui.Rect(canvas.transform, Ui.Panel, size, Vector2.zero);
            float pad = 36f;
            float titleH = string.IsNullOrEmpty(title) ? 0f : titleSize * 1.5f;
            p.Title = Ui.Label(canvas.transform, title, titleSize, accent, TextAnchor.UpperLeft,
                new Vector2(size.x - pad * 2, titleH), new Vector2(0, size.y / 2f - pad - titleH / 2f), FontStyle.Bold);
            p.Body = Ui.Label(canvas.transform, body, bodySize, new Color(0.9f, 0.96f, 1f), TextAnchor.UpperLeft,
                new Vector2(size.x - pad * 2, size.y - titleH - pad * 2.4f),
                new Vector2(0, -(titleH + pad * 0.4f) / 2f));
            canvas.transform.localScale = Vector3.zero;
            return p;
        }

        public void Set(string title, string body)
        {
            if (Title != null) Title.text = title;
            if (Body != null) Body.text = body;
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / 0.35f);
            float e = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = targetScale * 0.001f * e;
        }

        /// <summary>Replay the pop-in (used when a panel's content changes).</summary>
        public void Pop() { t = 0f; transform.localScale = Vector3.zero; }

        public void Dismiss() => Destroy(gameObject);
    }

    /// <summary>Keeps a HUD root comfortably in front of the head. Smoothed, so it never feels glued to your face in VR.</summary>
    public class FollowHead : MonoBehaviour
    {
        public float Distance = 1.7f;
        public Vector3 Offset;
        public float Smooth = 4f;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var h = cam.transform;
            var fwd = h.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
            fwd.Normalize();
            var target = h.position + fwd * Distance + Offset;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-Smooth * Time.deltaTime));
            var look = transform.position - h.position;
            if (look.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-Smooth * Time.deltaTime));
        }
    }
}
