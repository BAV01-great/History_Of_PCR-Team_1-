using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PCR
{
    /// <summary>Small helpers for building scene geometry in code.</summary>
    public static class Gen
    {
        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
            Material mat, bool collider = false, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = false)
            => Prim(PrimitiveType.Cube, name, parent, pos, size, mat, collider);

        public static GameObject Empty(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        public static LineRenderer Line(Transform parent, string name, Color color, float width, int points, Material mat = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = points;
            lr.startWidth = lr.endWidth = width;
            lr.sharedMaterial = mat != null ? mat : Mats.Line(Color.white);
            lr.startColor = lr.endColor = color;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        public static void Face(Transform t, Vector3 viewerPos)
        {
            var d = t.position - viewerPos;
            d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }

    /// <summary>Flat-shaded low-poly mesh builder (prisms/boxes), merged into a single mesh = one draw call.</summary>
    public class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<int> t = new List<int>();

        public int VertexCount => v.Count;

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            var normal = Vector3.Cross(b - a, c - a);
            bool flip = Vector3.Dot(normal, outward) < 0f;
            normal = flip ? -normal.normalized : normal.normalized;
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int k = 0; k < 4; k++) n.Add(normal);
            if (!flip) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
            else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
        }

        /// <summary>Open prism from a to b (no end caps) with the given radius and side count.</summary>
        public void Prism(Vector3 a, Vector3 b, float radius, int sides)
        {
            var axis = (b - a);
            if (axis.sqrMagnitude < 1e-8f) return;
            axis.Normalize();
            var up = Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right;
            var u = Vector3.Cross(axis, up).normalized;
            var w = Vector3.Cross(axis, u);
            for (int s = 0; s < sides; s++)
            {
                float a0 = 2f * Mathf.PI * s / sides, a1 = 2f * Mathf.PI * (s + 1) / sides;
                var d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0);
                var d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                Quad(a + d0 * radius, a + d1 * radius, b + d1 * radius, b + d0 * radius, d0 + d1);
            }
        }

        /// <summary>Smooth swept tube along a path (shared vertices, smooth normals, rounded by the ring count). No end caps.</summary>
        public void Tube(IList<Vector3> path, float radius, int sides)
        {
            if (path.Count < 2) return;
            int baseIdx = v.Count;
            var prevU = Vector3.zero;
            for (int i = 0; i < path.Count; i++)
            {
                var tan = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 u;
                if (i == 0) { var up = Mathf.Abs(tan.y) < 0.9f ? Vector3.up : Vector3.right; u = Vector3.Cross(tan, up).normalized; }
                else { u = (prevU - tan * Vector3.Dot(prevU, tan)).normalized; }   // parallel transport: no twisting between rings
                var w = Vector3.Cross(tan, u);
                prevU = u;
                for (int s = 0; s < sides; s++)
                {
                    float a = 2f * Mathf.PI * s / sides;
                    var d = u * Mathf.Cos(a) + w * Mathf.Sin(a);
                    v.Add(path[i] + d * radius); n.Add(d);
                }
            }
            for (int i = 0; i < path.Count - 1; i++)
                for (int s = 0; s < sides; s++)
                {
                    int a = baseIdx + i * sides + s, b = baseIdx + i * sides + (s + 1) % sides;
                    int c = a + sides, d2 = b + sides;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d2);
                }
        }

        /// <summary>Smooth surface through a stack of rings (all with the same point count). Normals point away from each ring's centre.</summary>
        public void Loft(IList<Vector3[]> rings, bool closeBottom = false)
        {
            if (rings.Count < 2) return;
            int baseIdx = v.Count, m = rings[0].Length;
            for (int i = 0; i < rings.Count; i++)
            {
                var c = Vector3.zero; foreach (var q in rings[i]) c += q; c /= m;
                for (int j = 0; j < m; j++) { v.Add(rings[i][j]); n.Add((rings[i][j] - c).normalized); }
            }
            for (int i = 0; i < rings.Count - 1; i++)
                for (int j = 0; j < m; j++)
                {
                    int a = baseIdx + i * m + j, b = baseIdx + i * m + (j + 1) % m, c2 = a + m, d = b + m;
                    t.Add(a); t.Add(b); t.Add(c2); t.Add(b); t.Add(d); t.Add(c2);
                }
            if (closeBottom)
            {
                var cen = Vector3.zero; foreach (var q in rings[rings.Count - 1]) cen += q; cen /= m;
                int ci = v.Count; v.Add(cen); n.Add(Vector3.down);
                int last = baseIdx + (rings.Count - 1) * m;
                for (int j = 0; j < m; j++) { t.Add(ci); t.Add(last + (j + 1) % m); t.Add(last + j); }
            }
        }

        /// <summary>UV sphere with smooth normals.</summary>
        public void Sphere(Vector3 c, float r, int lon = 10, int lat = 7)
        {
            int baseIdx = v.Count;
            for (int i = 0; i <= lat; i++)
            {
                float phi = Mathf.PI * i / lat;
                for (int j = 0; j <= lon; j++)
                {
                    float th = 2f * Mathf.PI * j / lon;
                    var d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    v.Add(c + d * r); n.Add(d);
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int a = baseIdx + i * (lon + 1) + j, b = a + 1, c2 = a + lon + 1, d2 = c2 + 1;
                    t.Add(a); t.Add(b); t.Add(c2); t.Add(b); t.Add(d2); t.Add(c2);
                }
        }

        /// <summary>Small octahedron-ish blob to cap joints.</summary>
        public void Blob(Vector3 c, float r)
        {
            var px = c + Vector3.right * r; var nx = c - Vector3.right * r;
            var py = c + Vector3.up * r; var ny = c - Vector3.up * r;
            var pz = c + Vector3.forward * r; var nz = c - Vector3.forward * r;
            Tri(py, px, pz, c); Tri(py, pz, nx, c); Tri(py, nx, nz, c); Tri(py, nz, px, c);
            Tri(ny, pz, px, c); Tri(ny, nx, pz, c); Tri(ny, nz, nx, c); Tri(ny, px, nz, c);
        }

        void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 center)
        {
            var normal = Vector3.Cross(b - a, c - a);
            var outward = (a + b + c) / 3f - center;
            bool flip = Vector3.Dot(normal, outward) < 0f;
            normal = flip ? -normal.normalized : normal.normalized;
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            n.Add(normal); n.Add(normal); n.Add(normal);
            if (!flip) { t.Add(i); t.Add(i + 1); t.Add(i + 2); } else { t.Add(i); t.Add(i + 2); t.Add(i + 1); }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            m.UploadMeshData(true);
            return m;
        }
    }
}
