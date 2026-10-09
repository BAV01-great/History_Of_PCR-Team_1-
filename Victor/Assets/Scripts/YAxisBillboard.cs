using UnityEngine;

public class YAxisBillboard : MonoBehaviour
{
    public Transform target;
    public bool flip = false;
    public float smoothing = 0f;

    void Start()
    {
        if (!target && Camera.main) target = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!target) return;

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
