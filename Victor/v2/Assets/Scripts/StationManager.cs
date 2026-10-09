using UnityEngine;
using UnityEngine.Events;

/// One instance in the scene. Stations unlock in the order of the list.
/// It listens to each StationZone's onComplete event automatically.
public class StationManager : MonoBehaviour
{
    [System.Serializable]
    public class Station
    {
        public string name;
        public StationZone zone;          // the station's StationZone
        public GameObject stationRoot;    // whole station; inactive until it is unlocked (can be null)
        public GameObject targetMarker;   // arrow / glow shown while this is the NEXT station
    }

    [Header("Stations (in order)")]
    public Station[] stations;

    [Header("Exit")]
    public GameObject exitDoorHighlight;  // shown when every station is complete
    public Transform exitDoor;            // where the guide line ends after the last station
    public UnityEvent onAllComplete;      // e.g. open the door, play a sound

    [Header("Guide line")]
    public LineRenderer guideLine;        // a LineRenderer in the scene (Use World Space ON)
    public float lineHeight = 0.05f;      // lift off the floor so it doesn't z-fight

    bool[] completed;
    int current;                          // index of the station the player should go to next

    public int CompletedCount { get; private set; }
    public bool AllComplete => CompletedCount >= stations.Length;

    void Start()
    {
        completed = new bool[stations.Length];

        // Hide everything, then subscribe to each station
        for (int i = 0; i < stations.Length; i++)
        {
            int index = i; // copy for the lambda
            if (stations[i].stationRoot) stations[i].stationRoot.SetActive(false);
            if (stations[i].targetMarker) stations[i].targetMarker.SetActive(false);
            if (stations[i].zone) stations[i].zone.onComplete.AddListener(() => StationCompleted(index));
        }
        if (exitDoorHighlight) exitDoorHighlight.SetActive(false);
        if (guideLine) guideLine.gameObject.SetActive(false);

        UnlockStation(0);
    }

    /// Called automatically by each StationZone. Can also be called manually from the Inspector.
    public void StationCompleted(int index)
    {
        if (index < 0 || index >= stations.Length || completed[index]) return;

        completed[index] = true;
        CompletedCount++;

        var s = stations[index];
        if (s.targetMarker) s.targetMarker.SetActive(false);
        Debug.Log($"Station completed: {s.name} ({CompletedCount}/{stations.Length})");

        if (AllComplete)
        {
            if (exitDoorHighlight) exitDoorHighlight.SetActive(true);
            DrawLine(s.zone ? s.zone.transform : null, exitDoor);
            onAllComplete.Invoke();
        }
        else
        {
            UnlockStation(index + 1);
            DrawLine(s.zone ? s.zone.transform : null,
                     stations[index + 1].zone ? stations[index + 1].zone.transform : null);
        }
    }

    // Straight line from the station just completed to the next target
    void DrawLine(Transform from, Transform to)
    {
        if (!guideLine || from == null || to == null) return;

        Vector3 lift = Vector3.up * lineHeight;
        guideLine.gameObject.SetActive(true);
        guideLine.useWorldSpace = true;
        guideLine.positionCount = 2;
        guideLine.SetPosition(0, from.position + lift);
        guideLine.SetPosition(1, to.position + lift);
    }

    void UnlockStation(int index)
    {
        if (index >= stations.Length) return;

        current = index;
        var s = stations[index];
        if (s.stationRoot) s.stationRoot.SetActive(true);
        if (s.targetMarker) s.targetMarker.SetActive(true);
    }
}
