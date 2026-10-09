using UnityEngine;

// Shakes an object while enabled. Enable it when heating starts, disable it to stop.
// On disable, the object returns to its original local position.
public class SimpleShake : MonoBehaviour
{
    public float intensity = 0.01f;   // max offset in meters (0.01 = 1 cm)
    public float speed = 40f;         // higher = faster, more frantic shake
    public bool shakeRotation = false;
    public float rotationAmount = 3f; // degrees, used if shakeRotation is on

    Vector3 startPos;
    Quaternion startRot;
    float seed;

    void OnEnable()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
        seed = Random.value * 100f; // so multiple objects don't shake in sync
    }

    void Update()
    {
        float t = Time.time * speed + seed;
        Vector3 offset = new Vector3(
            Mathf.PerlinNoise(t, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, t) - 0.5f,
            Mathf.PerlinNoise(t, t) - 0.5f) * 2f * intensity;

        transform.localPosition = startPos + offset;

        if (shakeRotation)
        {
            Vector3 rot = offset / Mathf.Max(intensity, 0.0001f) * rotationAmount;
            transform.localRotation = startRot * Quaternion.Euler(rot);
        }
    }

    void OnDisable()
    {
        transform.localPosition = startPos;
        transform.localRotation = startRot;
    }
}
