using UnityEngine;
using UnityEngine.Events;

/// Put on an empty GameObject with a Collider (Is Trigger = ON) around the station.
/// The player (XR Origin / camera rig) must have the tag "Player" and a Collider or CharacterController.
public class StationZone : MonoBehaviour
{
    [Header("Player approaches")]
    public GameObject[] activateOnEnter;
    public GameObject[] deactivateOnEnter;
    public bool resetOnExit = true;          // undo the enter state when the player leaves

    [Header("Task completed")]
    public GameObject[] activateOnComplete;
    public GameObject[] deactivateOnComplete;
    public UnityEvent onComplete;            // optional extra hooks (play sound, start graph, etc.)

    bool completed;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SetActive(activateOnEnter, true);
        SetActive(deactivateOnEnter, false);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !resetOnExit) return;
        SetActive(activateOnEnter, false);
    }

    /// Hook this to an interactable's "On Done" event.
    public void CompleteTask()
    {
        if (completed) return;
        completed = true;

        SetActive(deactivateOnComplete, false);
        SetActive(activateOnComplete, true);
        onComplete.Invoke();
    }

    /// Optional: lets the player redo the station.
    public void ResetStation()
    {
        completed = false;
        SetActive(activateOnComplete, false);
        SetActive(deactivateOnComplete, true);
    }

    static void SetActive(GameObject[] objs, bool state)
    {
        foreach (var o in objs)
            if (o != null) o.SetActive(state);
    }
}
