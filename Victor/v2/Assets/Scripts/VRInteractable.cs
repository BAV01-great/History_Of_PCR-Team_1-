using UnityEngine;
using UnityEngine.Events;

/// Put on a grabbable object (XRGrabInteractable + Rigidbody + Collider).
/// Wire these in the XRGrabInteractable's events in the Inspector:
///   Hover Entered  -> ShowHighlight      Hover Exited  -> HideHighlight
///   Select Entered -> OnGrabbed          Select Exited -> OnReleased
/// The target zone is a separate object with a Collider (Is Trigger = ON).
public class VRInteractable : MonoBehaviour
{
    [Header("Highlight")]
    public GameObject highlightObject;       // e.g. a slightly larger glowing copy, or an outline mesh

    [Header("Task")]
    public bool requiresTargetZone = true;   // false = task completes on grab (e.g. a button/lever)
    public Collider targetZone;              // the zone THIS object belongs in
    public bool snapToZone = true;
    public Transform snapPoint;              // optional; defaults to the zone's transform

    public UnityEvent onDone;                // append StationZone.CompleteTask etc. in the Inspector

    bool held, inZone, done;

    void Start()
    {
        HideHighlight();
    }

    // ---- Wire to XR events ----
    public void ShowHighlight() { if (highlightObject) highlightObject.SetActive(true); }
    public void HideHighlight() { if (highlightObject) highlightObject.SetActive(false); }

    public void OnGrabbed()
    {
        held = true;
        if (!requiresTargetZone) Finish();
    }

    public void OnReleased()
    {
        held = false;
        TryFinish();
    }

    // ---- Target zone detection ----
    void OnTriggerEnter(Collider other)
    {
        if (other != targetZone) return;
        inZone = true;
        TryFinish();
    }

    void OnTriggerExit(Collider other)
    {
        if (other == targetZone) inZone = false;
    }

    void TryFinish()
    {
        if (requiresTargetZone && inZone && !held) Finish();
    }

    void Finish()
    {
        if (done) return;
        done = true;
        HideHighlight();

        if (requiresTargetZone && snapToZone)
        {
            var target = snapPoint ? snapPoint : targetZone.transform;
            transform.SetPositionAndRotation(target.position, target.rotation);

            var rb = GetComponent<Rigidbody>();
            if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
        }

        onDone.Invoke();
    }
}
