using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace PCR
{
    public class RigSelector : MonoBehaviour
    {
        public GameObject XrRig;
        public GameObject Simulator;
        public GameObject DesktopRig;
        public Transform SpawnPoint;

        public Transform ActiveRoot { get; private set; }

        void Awake()
        {
            bool xr = XrRig != null && (XRSettings.isDeviceActive || (Simulator != null && Simulator.activeSelf));
            if (XrRig != null) XrRig.SetActive(xr);
            if (Simulator != null) Simulator.SetActive(xr && Simulator.activeSelf);
            if (DesktopRig != null) DesktopRig.SetActive(!xr);
            ActiveRoot = xr ? XrRig.transform : (DesktopRig != null ? DesktopRig.transform : transform);
            if (xr) foreach (var g in XrRig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity.GravityProvider>(true)) g.useGravity = false;
            if (SpawnPoint != null)
            {
                ActiveRoot.SetPositionAndRotation(SpawnPoint.position, SpawnPoint.rotation);
            }
            Debug.Log($"[PCR] Rig: {(xr ? "XR" : "Desktop fallback")}");
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            if (ActiveRoot == null) return;
            var desk = ActiveRoot.GetComponent<DesktopRig>();
            var cam = Camera.main;
            if (desk == null && cam != null && cam.transform.IsChildOf(ActiveRoot))
            {
                var local = ActiveRoot.InverseTransformPoint(cam.transform.position);
                var lf = ActiveRoot.InverseTransformDirection(cam.transform.forward);
                float headYaw = Mathf.Atan2(lf.x, lf.z) * Mathf.Rad2Deg;
                var rot = Quaternion.Euler(0, yaw - headYaw, 0);
                ActiveRoot.rotation = rot;
                ActiveRoot.position = new Vector3(pos.x, pos.y, pos.z) - rot * new Vector3(local.x, 0, local.z);
                return;
            }
            ActiveRoot.position = pos;
            ActiveRoot.rotation = Quaternion.Euler(0, yaw, 0);
            if (desk != null) desk.SetYaw(yaw);
        }

        public IEnumerator WalkTo(Vector3 target, float speed = 1.7f)
        {
            var t = ActiveRoot;
            target.y = t.position.y;
            while (true)
            {
                var d = target - t.position; d.y = 0;
                if (d.magnitude < 0.05f) break;
                float step = Mathf.Min(d.magnitude, speed * Time.deltaTime);
                t.position += d.normalized * step;
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(d.normalized), 4f * Time.deltaTime);
                yield return null;
            }
        }

        public IEnumerator FaceTowards(Vector3 point, float seconds = 0.8f)
        {
            var t = ActiveRoot;
            var d = point - t.position; d.y = 0;
            if (d.sqrMagnitude < 0.01f) yield break;
            var from = t.rotation; var to = Quaternion.LookRotation(d.normalized);
            for (float e = 0; e < seconds; e += Time.deltaTime)
            {
                t.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, e / seconds));
                yield return null;
            }
            t.rotation = to;
        }
    }

    public static class PcrDemo
    {
        public static bool Active;
    }

    public class DesktopRig : MonoBehaviour
    {
        public float MoveSpeed = 2.2f;
        public float LookSpeed = 0.12f;
        public float EyeHeight = 1.65f;
        public float Reach = 6f;

        Camera cam;
        float pitch, yaw;
        IPressable hovered;

        public void SetYaw(float y) { yaw = y; }

        void Start()
        {
            cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.transform.localPosition = new Vector3(0, EyeHeight, 0);
            yaw = transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (PcrDemo.Active) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null || cam == null) return;

            if (kb.escapeKey.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; return; }

            var d = mouse.delta.ReadValue() * LookSpeed;
            yaw += d.x;
            pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

            var mv = Vector3.zero;
            if (kb.wKey.isPressed) mv += Vector3.forward;
            if (kb.sKey.isPressed) mv += Vector3.back;
            if (kb.aKey.isPressed) mv += Vector3.left;
            if (kb.dKey.isPressed) mv += Vector3.right;
            float speed = MoveSpeed * (kb.leftShiftKey.isPressed ? 1.8f : 1f);
            transform.Translate(mv.normalized * speed * Time.deltaTime, Space.Self);

            if (hovered is Object ho && ho == null) hovered = null;
            IPressable hit = null;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var h, Reach))
                hit = h.collider.GetComponentInParent<IPressable>();
            if (hit != hovered)
            {
                if (hovered != null) hovered.SetHover(false);
                hovered = hit;
                if (hovered != null) hovered.SetHover(true);
            }
            if (hovered != null && (mouse.leftButton.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                hovered.Press();
            if (kb.spaceKey.wasPressedThisFrame) NarrationManager.Instance.Skip();
        }
    }
}
