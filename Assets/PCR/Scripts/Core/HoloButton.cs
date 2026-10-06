using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PCR
{
    /// <summary>
    /// A chunky 3D button. Works with VR ray / select, the XR Interaction Simulator, and the desktop fallback (DesktopRig calls Press()).
    /// Uses XRSimpleInteractable, so no UI raycaster/EventSystem setup is needed.
    /// </summary>
    public class HoloButton : MonoBehaviour, IPressable
    {
        public event Action Pressed;
        public bool Interactable = true;

        /// <summary>If set, pressing this button also raises that step target id (and the button is highlightable by the step system).</summary>
        public string StepId
        {
            get => stepId;
            set { stepId = value; if (!string.IsNullOrEmpty(value)) StepTargets.Register(value, gameObject); }
        }
        string stepId;

        Material mat;
        Color baseColor;
        Color baseEmission;
        bool hovered;
        float punch;
        Vector3 baseScale;
        Text label;

        public static HoloButton Create(Transform parent, Vector3 localPos, string text, Color color, Vector2 sizeMeters, int fontPx = 40)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "HoloButton_" + text;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(sizeMeters.x, sizeMeters.y, 0.04f);
            // Generous collider so the ray finds it easily.
            var bc = go.GetComponent<BoxCollider>();
            bc.size = new Vector3(1.15f, 1.3f, 4f);

            var b = go.AddComponent<HoloButton>();
            b.baseColor = color * 0.55f; b.baseColor.a = 1f;
            b.baseEmission = color * 0.9f;
            b.mat = Mats.LitNew(b.baseColor, b.baseEmission, 0.6f);
            go.GetComponent<Renderer>().sharedMaterial = b.mat;
            b.baseScale = go.transform.localScale;

            // Label canvas sits in front of the button face (towards the viewer, -z).
            var canvas = Ui.Canvas("Label", go.transform, new Vector2(sizeMeters.x * 1000f, sizeMeters.y * 1000f), new Vector3(0, 0, -0.52f));
            canvas.transform.localScale = new Vector3(1f / go.transform.localScale.x, 1f / go.transform.localScale.y, 1f / go.transform.localScale.z) * 0.001f;
            b.label = Ui.Label(canvas.transform, text, fontPx, Color.white, TextAnchor.MiddleCenter,
                new Vector2(sizeMeters.x * 1000f, sizeMeters.y * 1000f), Vector2.zero, FontStyle.Bold, true);

            var xr = go.AddComponent<XRSimpleInteractable>();
            xr.hoverEntered.AddListener(_ => b.SetHover(true));
            xr.hoverExited.AddListener(_ => b.SetHover(false));
            xr.selectEntered.AddListener(_ => b.Press());
            return b;
        }

        public void SetText(string text) { if (label != null) label.text = text; }

        public void SetColor(Color c)
        {
            baseColor = c * 0.55f; baseColor.a = 1f;
            baseEmission = c * 0.9f;
            Refresh();
        }

        public void SetHover(bool h)
        {
            if (hovered == h) return;
            hovered = h;
            if (h && Interactable) PlayHover();
            Refresh();
        }

        void PlayHover() => Sfx.Hover(transform.position);

        void Refresh()
        {
            if (mat == null) return;
            float k = !Interactable ? 0.15f : hovered ? 1.8f : 1f;
            mat.SetColor("_BaseColor", Interactable ? baseColor : baseColor * 0.3f);
            mat.SetColor("_EmissionColor", baseEmission * k);
        }

        public void Press()
        {
            if (!Interactable) return;
            punch = 1f;
            Sfx.Press(transform.position);
            Pressed?.Invoke();
            if (!string.IsNullOrEmpty(stepId)) StepTargets.Raise(stepId);
        }

        public void SetInteractable(bool value) { Interactable = value; Refresh(); }

        void Update()
        {
            if (punch > 0f)
            {
                punch = Mathf.Max(0f, punch - Time.deltaTime * 5f);
                float s = 1f - 0.25f * Mathf.Sin(punch * Mathf.PI);
                transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
            }
            else if (hovered) transform.localScale = Vector3.Lerp(transform.localScale, baseScale * 1.06f, Time.deltaTime * 12f);
            else transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 12f);
        }
    }
}
