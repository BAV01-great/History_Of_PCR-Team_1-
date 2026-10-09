using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PCR
{
    public class LabInteractable : MonoBehaviour, IPressable
    {
        public string Id;
        public string Label;
        public bool ShowHoverLabel = true;

        public static LabInteractable Find(string id) { var g = StepTargets.Find(id); return g != null ? g.GetComponent<LabInteractable>() : null; }

        public static LabInteractable Make(GameObject go, string id, string label)
        {
            var li = go.GetComponent<LabInteractable>() ?? go.AddComponent<LabInteractable>();
            li.Id = id; li.Label = label;
            if (go.GetComponentInChildren<Collider>() == null) FitBoxCollider(go);
            if (go.GetComponent<XRSimpleInteractable>() == null)
            {
                var xr = go.AddComponent<XRSimpleInteractable>();
                xr.hoverEntered.AddListener(_ => li.SetHover(true));
                xr.hoverExited.AddListener(_ => li.SetHover(false));
                xr.selectEntered.AddListener(_ => li.Press());
            }
            StepTargets.Register(id, go);
            return li;
        }

        static void FitBoxCollider(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            bool first = true; Bounds b = default;
            foreach (var r in rs)
            {
                var wb = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = wb.center + Vector3.Scale(wb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var lp = go.transform.InverseTransformPoint(corner);
                    if (first) { b = new Bounds(lp, Vector3.zero); first = false; } else b.Encapsulate(lp);
                }
            }
            var bc = go.AddComponent<BoxCollider>();
            bc.center = b.center;
            bc.size = b.size + Vector3.one * 0.02f;
        }

        public void SetHover(bool hovered)
        {
            if (hovered && ShowHoverLabel && !string.IsNullOrEmpty(Label)) Tooltip.Show(Label, this);
            else Tooltip.Hide(this);
        }

        public void Press() => StepTargets.Raise(Id);

        void OnDestroy()
        {
            StepTargets.Unregister(Id, gameObject);
            Tooltip.Hide(this);
        }

        static class Tooltip
        {
            static GameObject go;
            static Text text;
            static LabInteractable owner;

            public static void Show(string label, LabInteractable who)
            {
                if (go == null)
                {
                    var c = Ui.Canvas("HoverLabel", null, new Vector2(520, 110), Vector3.zero);
                    go = c.gameObject;
                    Ui.Rect(c.transform, Theme.PanelBg, new Vector2(520, 110), Vector2.zero);
                    text = Ui.Label(c.transform, "", 44, Color.white, TextAnchor.MiddleCenter, new Vector2(500, 100), Vector2.zero, FontStyle.Bold);
                    go.AddComponent<BillboardY>();
                }
                owner = who;
                text.text = label;
                var rs = who.GetComponentsInChildren<Renderer>();
                var p = who.transform.position + Vector3.up * 0.25f;
                if (rs.Length > 0) { var b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); p = new Vector3(b.center.x, b.max.y + 0.14f, b.center.z); }
                go.transform.position = p;
                go.SetActive(true);
            }

            public static void Hide(LabInteractable who)
            {
                if (go != null && owner == who) go.SetActive(false);
            }
        }
    }
}
