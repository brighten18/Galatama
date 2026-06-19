using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [SerializeField] private List<MissionData> missions = new();

    private int currentMissionIndex;

    public event Action<MissionData> OnMissionStarted;
    public event Action OnAllMissionsCompleted;

    /// <summary>Fired when a mission is completed. Parameter is the completed mission's index.</summary>
    public event Action<int> OnMissionCompleted;

    public MissionData CurrentMission =>
        currentMissionIndex < missions.Count ? missions[currentMissionIndex] : null;

    public bool AllCompleted => currentMissionIndex >= missions.Count;

    public int CurrentMissionIndex => currentMissionIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (CurrentMission != null)
            OnMissionStarted?.Invoke(CurrentMission);
    }

    /// <summary>Marks the mission at the given index as complete and advances to the next.</summary>
    public void CompleteMission(int missionIndex)
    {
        if (missionIndex != currentMissionIndex)
        {
            Debug.LogWarning($"[MissionManager] Misi {missionIndex} bukan misi aktif saat ini ({currentMissionIndex}).");
            return;
        }

        currentMissionIndex++;
        Debug.Log($"[MissionManager] Misi {missionIndex} selesai. Misi berikutnya: {currentMissionIndex}");

        OnMissionCompleted?.Invoke(missionIndex);

        if (currentMissionIndex < missions.Count)
            OnMissionStarted?.Invoke(CurrentMission);
        else
            OnAllMissionsCompleted?.Invoke();
    }
}
