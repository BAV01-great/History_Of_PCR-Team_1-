using UnityEngine;

// Rotates a card around the Y axis only, so it always faces the player and stays upright.
public class YAxisBillboard : MonoBehaviour
{
    public Transform target;          // leave empty to use Camera.main (the headset camera)
    public bool flip = false;         // tick if the card appears backwards or mirrored
    public float smoothing = 0f;      // 0 = instant, ~5-10 = gentle turning

    void Start()
    {
        if (!target && Camera.main) target = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Direction from the player to the card, flattened so only Y rotation is used.
        // A world-space UI canvas is readable when its forward points away from the viewer.
        Vector3 dir = transform.position - target.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        if (flip) dir = -dir;

        Quaternion goal = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = smoothing <= 0f
            ? goal
            : Quaternion.Slerp(transform.rotation, goal, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
    }
}
