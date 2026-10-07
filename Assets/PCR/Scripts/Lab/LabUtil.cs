using System.Collections;
using UnityEngine;

namespace PCR
{
    public static class LabUtil
    {
        public static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in rs)
            {
                if (r.transform.name == "OutlineHull") continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        public static Vector3 Center(GameObject go) => BoundsOf(go).center;

        public static Vector3 Approach(GameObject go, float distance = 0.2f)
        {
            var c = Center(go);
            var cam = Camera.main;
            if (cam == null) return c;
            var toPlayer = cam.transform.position - c;
            toPlayer.y = Mathf.Min(toPlayer.y, 0.15f);
            return c + toPlayer.normalized * distance;
        }

        public static IEnumerator Move(Transform t, Vector3 to, float seconds, Quaternion? rot = null, float? scaleMul = null)
        {
            var from = t.position; var fromRot = t.rotation; var fromScale = t.localScale;
            for (float e = 0; e < seconds; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, e / seconds);
                t.position = Vector3.Lerp(from, to, k);
                if (rot.HasValue) t.rotation = Quaternion.Slerp(fromRot, rot.Value, k);
                if (scaleMul.HasValue) t.localScale = Vector3.Lerp(fromScale, fromScale * scaleMul.Value, k);
                yield return null;
            }
            t.position = to;
            if (rot.HasValue) t.rotation = rot.Value;
            if (scaleMul.HasValue) t.localScale = fromScale * scaleMul.Value;
        }
    }
}
