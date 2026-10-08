using System.Collections.Generic;
using UnityEngine;

/// Builds a small helix from primitives.
/// DNA    = two backbone strands joined by base-pair rungs
/// RNA    = one backbone strand with bases pointing inward
/// Primer = short single strand (DNA bases) with a bigger "3' end" bead
/// Add this to an empty GameObject, assign a material, pick a type.
/// Right-click the component header -> Rebuild to preview in the editor.
public class MoleculeBuilder : MonoBehaviour
{
    public enum MoleculeType { DNA, RNA, Primer }

    [Header("What to build")]
    public MoleculeType type = MoleculeType.DNA;
    public Material baseMaterial;          // any simple Lit / Unlit material; its colour gets overridden
    public bool buildOnStart = true;
    public bool addGrabCollider = true;    // one capsule on the root so it can be grabbed in VR
    public bool lockPhysics = true;        // kinematic + no gravity, so it only moves when grabbed

    [Header("Shape (local units, metres)")]
    public float turns = 1.5f;             // use a smaller value (e.g. 0.8) for a short primer
    public int basesPerTurn = 10;
    public float radius = 0.02f;
    public float heightPerTurn = 0.07f;
    public float backboneSize = 0.009f;
    public float rungThickness = 0.004f;
    public float strandOffsetDegrees = 150f;   // DNA only: angle between the two backbones

    [Header("Colours")]
    public Color dnaBackbone = new Color(0.85f, 0.85f, 0.9f);
    public Color rnaBackbone = new Color(1f, 0.45f, 0.8f);
    public Color primerBackbone = new Color(0.6f, 0.35f, 1f);

    readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    void Start()
    {
        if (lockPhysics) LockPhysics();
        if (buildOnStart) Build();
    }

    // The grab interactable remembers this state and restores it when the molecule is released,
    // so it stays exactly where the player lets go.
    void LockPhysics()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    [ContextMenu("Rebuild")]
    public void Build()
    {
        Clear();

        bool isDNA = type == MoleculeType.DNA;
        int count = Mathf.Max(2, Mathf.RoundToInt(turns * basesPerTurn));
        float stepAngle = 360f / basesPerTurn;
        float stepHeight = heightPerTurn / basesPerTurn;

        const string dnaBases = "ATGC";
        const string complement = "TACG";
        const string rnaBases = "AUGC";

        Color backbone = type == MoleculeType.DNA ? dnaBackbone
                       : type == MoleculeType.RNA ? rnaBackbone
                       : primerBackbone;

        Vector3 prevA = Vector3.zero, prevB = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            float y = i * stepHeight;
            int pick = Random.Range(0, 4);

            // ---- Strand A ----
            Vector3 a = HelixPoint(i * stepAngle, y);
            bool isLast = i == count - 1;
            float bead = (type == MoleculeType.Primer && isLast) ? backboneSize * 1.8f : backboneSize;
            Sphere(a, bead, backbone);
            if (i > 0) Link(prevA, a, backboneSize * 0.5f, backbone);
            prevA = a;

            if (isDNA)
            {
                // ---- Strand B + base pair rung ----
                Vector3 b = HelixPoint(i * stepAngle + strandOffsetDegrees, y);
                Sphere(b, backboneSize, backbone);
                if (i > 0) Link(prevB, b, backboneSize * 0.5f, backbone);
                prevB = b;

                Vector3 mid = (a + b) * 0.5f;
                Link(a, mid, rungThickness, BaseColor(dnaBases[pick]));
                Link(mid, b, rungThickness, BaseColor(complement[pick]));
            }
            else
            {
                // ---- Single strand: base points toward the helix axis ----
                char baseLetter = (type == MoleculeType.RNA ? rnaBases : dnaBases)[pick];
                Vector3 inward = Vector3.Lerp(a, new Vector3(0, y, 0), 0.7f);
                Link(a, inward, rungThickness, BaseColor(baseLetter));
            }
        }

        if (addGrabCollider) SetupCollider(count * stepHeight);
    }

    // ---------- helpers ----------

    Vector3 HelixPoint(float angleDeg, float y)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(r) * radius, y, Mathf.Sin(r) * radius);
    }

    GameObject Sphere(Vector3 localPos, float size, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Setup(go, c);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * size * 2f;
        return go;
    }

    // A cylinder stretched between two local points
    GameObject Link(Vector3 from, Vector3 to, float thickness, Color c)
    {
        Vector3 dir = to - from;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(go, c);
        go.transform.localPosition = (from + to) * 0.5f;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        go.transform.localScale = new Vector3(thickness * 2f, dir.magnitude * 0.5f, thickness * 2f); // cylinder is 2 units tall
        return go;
    }

    void Setup(GameObject go, Color c)
    {
        go.transform.SetParent(transform, false);
        Remove(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = GetMaterial(c);
    }

    Material GetMaterial(Color c)
    {
        if (!materials.TryGetValue(c, out var m) || m == null)
        {
            m = new Material(baseMaterial);
            m.color = c;
            materials[c] = m;
        }
        return m;
    }

    static Color BaseColor(char b)
    {
        switch (b)
        {
            case 'A': return new Color(0.2f, 0.85f, 0.3f);   // green
            case 'T': return new Color(0.95f, 0.25f, 0.25f); // red
            case 'U': return new Color(1f, 0.55f, 0.1f);     // orange
            case 'G': return new Color(1f, 0.9f, 0.2f);      // yellow
            default:  return new Color(0.2f, 0.5f, 1f);      // C = blue
        }
    }

    void SetupCollider(float height)
    {
        var cc = GetComponent<CapsuleCollider>();
        if (cc == null) cc = gameObject.AddComponent<CapsuleCollider>();
        cc.direction = 1; // Y axis
        cc.height = height + radius;
        cc.radius = radius + backboneSize;
        cc.center = new Vector3(0, height * 0.5f, 0);
    }

    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Remove(transform.GetChild(i).gameObject);
    }

    static void Remove(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }
}