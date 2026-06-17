using UnityEngine;

[CreateAssetMenu(fileName = "MissionData", menuName = "GALATAMA/Mission Data")]
public class MissionData : ScriptableObject
{
    [SerializeField] private string missionTitle;
    [SerializeField, TextArea(2, 5)] private string missionDescription;

    [Header("Navigation")]
    [Tooltip("Index ke array Waypoints di MissionNavigator. Isi -1 jika misi ini tidak memiliki waypoint.")]
    [SerializeField] private int waypointIndex = -1;

    public string MissionTitle => missionTitle;
    public string MissionDescription => missionDescription;
    public int WaypointIndex => waypointIndex;

    /// <summary>Returns true if this mission has a valid waypoint assigned.</summary>
    public bool HasWaypoint => waypointIndex >= 0;
}
