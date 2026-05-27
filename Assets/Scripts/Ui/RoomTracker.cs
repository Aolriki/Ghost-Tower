using UnityEngine;

// Place this component on a GameObject in the gameplay scene, one per room.
// Do not connect events directly to this component.
// All calls come through MapManager.AddToRoomTracker(roomId).
public class RoomTracker : MonoBehaviour
{
    [Header("Room")]
    [Tooltip("Must match the roomId assigned in MapRooms for this room.")]
    public int roomId;

    [Tooltip("Total number of tasks required to complete this room.")]
    public int totalTasks = 1;

    // Current number of completed tasks. Starts at 0.
    private int _taskCount = 0;

    // Called by MapManager.AddToRoomTracker(roomId).
    // Increments the task counter and completes the room when all tasks are done.
    public void AddTask()
    {
        if (_taskCount >= totalTasks) return;

        _taskCount++;

        if (_taskCount >= totalTasks)
            MapManager.Instance?.CompleteRoom(roomId);
    }
}