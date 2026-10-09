using UnityEngine;

public class SimpleShake : MonoBehaviour
{
    public float intensity = 0.01f;
    public float speed = 40f;
    public bool shakeRotation = false;
    public float rotationAmount = 3f;

    Vector3 startPos;
    Quaternion startRot;
    float seed;

    void OnEnable()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
        seed = Random.value * 100f;
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
