using UnityEngine;

public class HelixPairMover : MonoBehaviour
{
    [Header("Strands (drag the two child objects here)")]
    public Transform strandA;
    public Transform strandB;

    [Header("Trail Renderers (leave empty to use the one on each strand)")]
    public TrailRenderer trailA;
    public TrailRenderer trailB;

    [Header("Roaming box (in this object's local space)")]
    public Vector3 boxCenter = Vector3.zero;
    public Vector3 boxSize = new Vector3(4f, 2f, 4f);
    public float minTravelDistance = 1.5f;
    public Vector2 speedRange = new Vector2(0.4f, 1.0f);
    public float turnSpeed = 120f;

    [Header("Helix shape")]
    public float radius = 0.1f;
    public float rotationsPerSecond = 1.5f;

    [Header("Lifetime")]
    public Vector2 lifetimeRange = new Vector2(5f, 12f);
    public bool respawnOnTimeout = true;
    public float fadeTime = 0.5f;

    const float ArriveDistance = 0.1f;

    Vector3 pos, heading, target, perp;
    float angle, speed, timer, visibility = 1f;
    int fadeDir;

    Vector3 baseScaleA, baseScaleB;
    float baseWidthA, baseWidthB;

    void Awake()
    {
        if (!strandA || !strandB)
        {
            Debug.LogError("HelixPairMover: assign both strand objects.", this);
            enabled = false;
            return;
        }

        if (!trailA) trailA = strandA.GetComponent<TrailRenderer>();
        if (!trailB) trailB = strandB.GetComponent<TrailRenderer>();

        baseScaleA = strandA.localScale;
        baseScaleB = strandB.localScale;
        if (trailA) baseWidthA = trailA.widthMultiplier;
        if (trailB) baseWidthB = trailB.widthMultiplier;
    }

    void OnEnable()
    {
        if (strandA && strandB) Respawn();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        timer -= dt;
        if (timer <= 0f && fadeDir == 0)
        {
            if (respawnOnTimeout) fadeDir = -1;
            else { NewTarget(); timer = Random.Range(lifetimeRange.x, lifetimeRange.y); }
        }
        UpdateFade(dt);

        Vector3 toTarget = target - pos;
        if (toTarget.magnitude < ArriveDistance)
        {
            NewTarget();
            toTarget = target - pos;
        }
        heading = Vector3.RotateTowards(heading, toTarget.normalized,
                                        turnSpeed * Mathf.Deg2Rad * dt, 0f).normalized;
        pos += heading * speed * dt;

        perp = Vector3.ProjectOnPlane(perp, heading).normalized;
        angle += rotationsPerSecond * Mathf.PI * 2f * dt;

        PlaceStrands();
    }

    void PlaceStrands()
    {
        Vector3 perp2 = Vector3.Cross(heading, perp);
        Vector3 offset = (perp * Mathf.Cos(angle) + perp2 * Mathf.Sin(angle)) * radius;

        strandA.position = transform.TransformPoint(pos + offset);
        strandB.position = transform.TransformPoint(pos - offset);
    }

    void Respawn()
    {
        pos = RandomPoint();
        NewTarget();
        heading = (target - pos).normalized;

        perp = Vector3.Cross(heading, Vector3.up);
        if (perp.sqrMagnitude < 0.001f) perp = Vector3.Cross(heading, Vector3.right);
        perp.Normalize();

        angle = Random.value * Mathf.PI * 2f;
        timer = Random.Range(lifetimeRange.x, lifetimeRange.y);

        PlaceStrands();
        ClearTrails();
    }

    void NewTarget()
    {
        target = PickTarget(pos);
        speed = Random.Range(speedRange.x, speedRange.y);
    }

    Vector3 PickTarget(Vector3 from)
    {
        Vector3 best = from;
        float bestDist = -1f;
        for (int i = 0; i < 20; i++)
        {
            Vector3 p = RandomPoint();
            float d = Vector3.Distance(from, p);
            if (d >= minTravelDistance) return p;
            if (d > bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    Vector3 RandomPoint()
    {
        return boxCenter + new Vector3(
            Random.Range(-0.5f, 0.5f) * boxSize.x,
            Random.Range(-0.5f, 0.5f) * boxSize.y,
            Random.Range(-0.5f, 0.5f) * boxSize.z);
    }

    void UpdateFade(float dt)
    {
        if (fadeDir == 0) return;

        visibility += fadeDir * dt / Mathf.Max(fadeTime, 0.0001f);
        if (fadeDir < 0 && visibility <= 0f)
        {
            visibility = 0f;
            Respawn();
            fadeDir = 1;
        }
        else if (fadeDir > 0 && visibility >= 1f)
        {
            visibility = 1f;
            fadeDir = 0;
        }
        ApplyVisibility();
    }

    void ApplyVisibility()
    {
        strandA.localScale = baseScaleA * visibility;
        strandB.localScale = baseScaleB * visibility;
        if (trailA) trailA.widthMultiplier = baseWidthA * visibility;
        if (trailB) trailB.widthMultiplier = baseWidthB * visibility;
    }

    void ClearTrails()
    {
        if (trailA) trailA.Clear();
        if (trailB) trailB.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
