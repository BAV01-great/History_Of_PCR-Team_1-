using UnityEngine;

// Floats an object up and down while enabled. Disable it when interaction begins.
// On disable, the object returns to its original local position.
public class SimpleBob : MonoBehaviour
{
    public float height = 0.05f;  // how far it moves up and down, in meters
    public float speed = 1.5f;    // bobs per ~6.3 seconds; raise for faster bobbing
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
