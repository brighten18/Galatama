using UnityEngine;

/// <summary>
/// Subclass InteractableObject untuk objek Jaring.
/// Menyelesaikan Misi 4 (index 3) saat Jaring diambil oleh pemain.
/// </summary>
public class JaringPickupMission : InteractableObject
{
    private const int TargetMissionIndex = 3;

    /// <summary>Dipanggil oleh base class setelah item berhasil diambil ke inventory.</summary>
    public override void InteractObject()
    {
        base.InteractObject();

        if (MissionManager.Instance == null) return;
        if (MissionManager.Instance.CurrentMissionIndex != TargetMissionIndex) return;

        MissionManager.Instance.CompleteMission(TargetMissionIndex);
        Debug.Log("[JaringPickupMission] Jaring diambil — Misi 4 selesai.");
    }
}
