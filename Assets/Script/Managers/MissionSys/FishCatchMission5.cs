using UnityEngine;

/// <summary>
/// Menyelesaikan Misi 5 (index 4) saat pemain berhasil menangkap
/// ikan apapun menggunakan jaring.
/// </summary>
public class FishCatchMission5 : MonoBehaviour
{
    private const int TargetMissionIndex = 4;

    private void OnEnable()
    {
        FishBase.OnAnyFishCaught += HandleFishCaught;
    }

    private void OnDisable()
    {
        FishBase.OnAnyFishCaught -= HandleFishCaught;
    }

    private void HandleFishCaught()
    {
        if (MissionManager.Instance == null) return;
        if (MissionManager.Instance.CurrentMissionIndex != TargetMissionIndex) return;

        MissionManager.Instance.CompleteMission(TargetMissionIndex);
        Debug.Log("[FishCatchMission5] Ikan tertangkap — Misi 5 selesai.");
    }
}
