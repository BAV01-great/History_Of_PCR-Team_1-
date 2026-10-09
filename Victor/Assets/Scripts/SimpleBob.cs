using UnityEngine;

public class SimpleBob : MonoBehaviour
{
    public float height = 0.05f;
    public float speed = 1.5f;
    public bool randomizePhase = true;

    Vector3 startPos;
    float phase;

    void OnEnable()
    {
        startPos = transform.localPosition;
        phase = randomizePhase ? Random.value * Mathf.PI * 2f : 0f;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * speed + phase) * height;
        transform.localPosition = startPos + Vector3.up * y;
    }

    void OnDisable()
    {
        transform.localPosition = startPos;
    }
}
