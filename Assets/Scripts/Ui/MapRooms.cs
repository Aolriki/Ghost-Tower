using System;
using UnityEngine;

// State a room can be in at any given moment.
public enum RoomState
{
    Locked,
    Default,
    Completed,
}

// Data for a single room entry. Assigned manually in the Inspector.
[Serializable]
public class RoomEntry
{
    [Tooltip("Unique identifier for this room. Used by MapManager to reference it.")]
    public int roomId;

    [Tooltip("Base panel of the room. Visible in Default and Locked states.")]
    public GameObject defaultRoomPanel;

    [Tooltip("Overlay panel shown when the room is Completed.")]
    public GameObject completedRoomPanel;

    [Tooltip("Overlay panel shown on top of the base when the room is Locked.")]
    public GameObject lockedRoomPanel;

    [Tooltip("State this room starts in when the scene loads.")]
    public RoomState initialState = RoomState.Default;

    // Runtime state, not serialized.
    [NonSerialized] public RoomState currentState;
}

// Holds the room panel references for a floor map prefab.
// Lives inside the map prefab as a component on the root GameObject.
// Accessed by MapManager after instantiation.
public class MapRooms : MonoBehaviour
{
    [Header("Rooms")]
    public RoomEntry[] rooms;

    // Applies initial states to all rooms. Called by MapManager after instantiation.
    public void Initialize()
    {
        if (rooms == null) return;

        foreach (RoomEntry room in rooms)
            ApplyState(room, room.initialState);
    }

    // Changes the state of a room by its ID.
    public void SetRoomState(int roomId, RoomState newState)
    {
        RoomEntry room = FindRoom(roomId);

        if (room == null)
        {
            Debug.LogWarning($"[MapRooms] Room with id {roomId} not found.");
            return;
        }

        ApplyState(room, newState);
    }

    // Returns the current state of a room by its ID.
    public RoomState GetRoomState(int roomId)
    {
        RoomEntry room = FindRoom(roomId);
        return room != null ? room.currentState : RoomState.Locked;
    }

    // Convenience wrappers for UnityEvent binding in the Inspector.
    public void UnlockRoom(int roomId) => SetRoomState(roomId, RoomState.Default);
    public void CompleteRoom(int roomId) => SetRoomState(roomId, RoomState.Completed);
    public void LockRoom(int roomId) => SetRoomState(roomId, RoomState.Locked);

    // Activates and deactivates the correct panels for the given state.
    private void ApplyState(RoomEntry room, RoomState state)
    {
        room.currentState = state;

        bool isLocked = state == RoomState.Locked;
        bool isCompleted = state == RoomState.Completed;

        // Default panel stays visible under the locked overlay.
        SetActive(room.defaultRoomPanel, !isCompleted);
        SetActive(room.completedRoomPanel, isCompleted);
        SetActive(room.lockedRoomPanel, isLocked);
    }

    private RoomEntry FindRoom(int roomId)
    {
        if (rooms == null) return null;

        foreach (RoomEntry room in rooms)
            if (room.roomId == roomId) return room;

        return null;
    }

    private static void SetActive(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}