using UnityEngine;

/// <summary>
/// Menyelesaikan Misi 6 (index 5) saat pemain berhasil menaruh
/// ikan apapun ke dalam akuarium.
/// </summary>
public class FishAquariumMission6 : MonoBehaviour
{
    private const int TargetMissionIndex = 5;

    private void OnEnable()
    {
        AquariumSystem.OnFishPlacedInAquarium += HandleFishPlaced;
    }

    private void OnDisable()
    {
        AquariumSystem.OnFishPlacedInAquarium -= HandleFishPlaced;
    }

    private void HandleFishPlaced()
    {
        if (MissionManager.Instance == null) return;
        if (MissionManager.Instance.CurrentMissionIndex != TargetMissionIndex) return;

        MissionManager.Instance.CompleteMission(TargetMissionIndex);
        Debug.Log("[FishAquariumMission6] Ikan ditaruh di akuarium — Misi 6 selesai.");
    }
}
