using UnityEngine;

// Scene-level component. One per gameplay scene.
// Delivers the floor map prefab to ScreenManager on Start,
// exposes room state control via MapRooms, and routes task completion to RoomTrackers.
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Map Prefab")]
    [Tooltip("Prefab root that contains MapRooms as a component and the map art as a child.")]
    public GameObject mapPrefab;

    [Header("Room Trackers")]
    [Tooltip("All RoomTrackers present in this scene. Assign in the Inspector.")]
    public RoomTracker[] roomTrackers;

    // Reference to the MapRooms component found inside the instantiated prefab.
    private MapRooms _mapRooms;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        LoadMap();
    }

    // Finds the RoomTracker with the given id and increments its task counter.
    // Called by UnityEvents from KeyItemss, KeySlots, CodeSlots, etc.
    public void AddToRoomTracker(int roomId)
    {
        if (roomTrackers == null) return;

        foreach (RoomTracker tracker in roomTrackers)
        {
            if (tracker != null && tracker.roomId == roomId)
            {
                tracker.AddTask();
                return;
            }
        }

        Debug.LogWarning($"[MapManager] No RoomTracker found with roomId {roomId}.");
    }

    // Instantiates the map prefab inside ScreenManager's mapContent and initializes rooms.
    private void LoadMap()
    {
        if (mapPrefab == null)
        {
            Debug.LogWarning("[MapManager] mapPrefab is not assigned.");
            return;
        }

        if (ScreenManager.Instance == null)
        {
            Debug.LogWarning("[MapManager] ScreenManager.Instance not found.");
            return;
        }

        GameObject instance = ScreenManager.Instance.LoadMap(mapPrefab);

        if (instance == null) return;

        _mapRooms = instance.GetComponent<MapRooms>();

        if (_mapRooms == null)
        {
            Debug.LogWarning("[MapManager] MapRooms component not found on the instantiated map prefab.");
            return;
        }

        _mapRooms.Initialize();
    }

    // Forwards room state changes to the active MapRooms instance.
    public void SetRoomState(int roomId, RoomState newState)
    {
        if (_mapRooms == null) return;
        _mapRooms.SetRoomState(roomId, newState);
    }

    public void UnlockRoom(int roomId) => _mapRooms?.UnlockRoom(roomId);
    public void CompleteRoom(int roomId) => _mapRooms?.CompleteRoom(roomId);
    public void LockRoom(int roomId) => _mapRooms?.LockRoom(roomId);

    public RoomState GetRoomState(int roomId)
    {
        if (_mapRooms == null) return RoomState.Locked;
        return _mapRooms.GetRoomState(roomId);
    }
}