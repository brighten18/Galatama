using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks how many gudang posters the player has read for Mission 3.
/// Each unique poster is counted only once per session.
/// Completes the target mission when all posters have been read.
/// </summary>
public class PosterMission3Tracker : MonoBehaviour
{
    public static PosterMission3Tracker Instance { get; private set; }

    [Tooltip("Index of the mission in MissionManager that this tracker completes.")]
    [SerializeField] private int targetMissionIndex = 2;

    [Tooltip("Total number of posters required to complete the mission.")]
    [SerializeField] private int totalPosters = 13;

    private readonly HashSet<int> _readPosterIDs = new();

    public int ReadCount => _readPosterIDs.Count;
    public int TotalPosters => totalPosters;
    public int TargetMissionIndex => targetMissionIndex;

    /// <summary>Fired when a new unique poster is read. Parameters: (readCount, totalPosters).</summary>
    public static event Action<int, int> OnAnyProgressChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Registers that a poster has been read. Duplicate calls for the same poster are silently ignored.
    /// Only counts when the target mission is currently active.
    /// </summary>
    /// <param name="posterInstanceID">The GetInstanceID() of the poster's GameObject.</param>
    public void RegisterRead(int posterInstanceID)
    {
        if (MissionManager.Instance == null || MissionManager.Instance.AllCompleted) return;
        if (MissionManager.Instance.CurrentMissionIndex != targetMissionIndex) return;
        if (_readPosterIDs.Contains(posterInstanceID)) return;

        _readPosterIDs.Add(posterInstanceID);
        Debug.Log($"[PosterMission3Tracker] Poster dibaca: {_readPosterIDs.Count}/{totalPosters}");

        OnAnyProgressChanged?.Invoke(_readPosterIDs.Count, totalPosters);

        if (_readPosterIDs.Count >= totalPosters)
            MissionManager.Instance.CompleteMission(targetMissionIndex);
    }
}
