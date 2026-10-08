using System.Collections;
using UnityEngine;

/// Setup:
/// 1. Create an empty "Graph" object and add this script.
/// 2. Add two child objects, each with a LineRenderer: "Axes" and "Curve".
///    Assign them below. Set both to "Use World Space" = OFF (the script also does this).
/// 3. Give each a glowing material: Unlit/Color shader (or URP Unlit) with an HDR color.
///    Add Bloom in a Post-Processing Volume to get the glow.
/// Call StartCurve() to animate, ResetGraph() to clear.
public class FluorescenceGraph : MonoBehaviour
{
    [Header("References")]
    public LineRenderer axes;
    public LineRenderer curve;

    [Header("Size (local units)")]
    public float width = 0.5f;
    public float height = 0.3f;

    [Header("Curve")]
    [Range(10, 200)] public int pointCount = 60;
    public float duration = 4f;              // seconds to draw the full curve
    public float steepness = 12f;            // how sharp the amplification jump is
    [Range(0.1f, 0.9f)] public float midpoint = 0.5f;  // where the jump happens along x
    public float lineWidth = 0.008f;

    [Header("Glow colours (use HDR)")]
    [ColorUsage(true, true)] public Color axesColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [ColorUsage(true, true)] public Color curveColor = new Color(0f, 4f, 1f, 1f);

    Coroutine running;

    void Awake()
    {
        SetupAxes();
        SetupCurve();
        ResetGraph();
    }

    void SetupAxes()
    {
        axes.useWorldSpace = false;
        axes.widthMultiplier = lineWidth * 0.6f;
        axes.startColor = axes.endColor = axesColor;
        axes.positionCount = 3;                       // y axis top -> origin -> x axis end
        axes.SetPosition(0, new Vector3(0, height, 0));
        axes.SetPosition(1, Vector3.zero);
        axes.SetPosition(2, new Vector3(width, 0, 0));
    }

    void SetupCurve()
    {
        curve.useWorldSpace = false;
        curve.widthMultiplier = lineWidth;
        curve.startColor = curve.endColor = curveColor;
    }

    public void StartCurve()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Draw());
    }

    public void ResetGraph()
    {
        if (running != null) StopCoroutine(running);
        curve.positionCount = 0;
    }

    IEnumerator Draw()
    {
        curve.positionCount = 0;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            int visible = Mathf.Clamp(Mathf.CeilToInt(t * pointCount), 2, pointCount);

            curve.positionCount = visible;
            for (int i = 0; i < visible; i++)
                curve.SetPosition(i, PointAt(i / (pointCount - 1f)));

            yield return null;
        }
        running = null;
    }

    // S-shaped curve: flat baseline -> exponential rise -> plateau (like real qPCR)
    Vector3 PointAt(float x)
    {
        float lo = Sigmoid(0f);
        float hi = Sigmoid(1f);
        float y = (Sigmoid(x) - lo) / (hi - lo);       // normalise to 0..1
        return new Vector3(x * width, y * height, 0f);
    }

    float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-steepness * (x - midpoint)));
}
