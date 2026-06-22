using System;
using UnityEngine;

/// <summary>
/// Menyelesaikan Misi 7 (index 6) saat Aquarium 1 penuh (9/9 ikan)
/// dan seluruh ikan berhasil dipertahankan hidup selama 3 menit.
/// Countdown direset jika ada ikan yang mati atau jumlah ikan turun.
/// </summary>
public class FishAquariumMission7 : MonoBehaviour
{
    private const int TargetMissionIndex = 6;
    private const float MaintainDurationSeconds = 180f;

    /// <summary>
    /// Dipanggil setiap frame saat Misi 7 aktif.
    /// Parameter: fishCount, maxFish, remainingSeconds, isCountingDown.
    /// </summary>
    public static event Action<int, int, float, bool> OnProgressChanged;

    [Tooltip("Drag Fixed_Aquarium_1 (GameObject yang punya komponen AquariumSystem) ke sini.")]
    [SerializeField] private AquariumSystem aquarium1;

    private float maintainTimer;
    private bool isTracking;
    private bool missionCompleted;

    private void OnEnable()
    {
        if (aquarium1 == null) return;

        aquarium1.AquariumStateChanged += HandleAquariumStateChanged;
        aquarium1.FishDied += HandleFishDied;
    }

    private void OnDisable()
    {
        if (aquarium1 == null) return;

        aquarium1.AquariumStateChanged -= HandleAquariumStateChanged;
        aquarium1.FishDied -= HandleFishDied;
    }

    private void Update()
    {
        if (missionCompleted) return;
        if (MissionManager.Instance == null) return;
        if (MissionManager.Instance.CurrentMissionIndex != TargetMissionIndex) return;

        if (isTracking)
        {
            maintainTimer += Time.deltaTime;

            if (maintainTimer >= MaintainDurationSeconds)
            {
                missionCompleted = true;
                // Tetap tampilkan instruksi 2 (isCountingDown: true) agar teks yang dicoret benar
                OnProgressChanged?.Invoke(aquarium1.FishCount, aquarium1.MaxFish, 0f, true);
                MissionManager.Instance.CompleteMission(TargetMissionIndex);
                Debug.Log("[FishAquariumMission7] Akuarium penuh dipertahankan 3 menit — Misi 7 selesai.");
                return;
            }
        }

        float remaining = MaintainDurationSeconds - maintainTimer;
        OnProgressChanged?.Invoke(
            aquarium1 != null ? aquarium1.FishCount : 0,
            aquarium1 != null ? aquarium1.MaxFish : 9,
            remaining,
            isTracking
        );
    }

    /// <summary>
    /// Dipanggil setiap kali jumlah ikan atau state akuarium berubah.
    /// Memulai atau mereset countdown berdasarkan kapasitas akuarium.
    /// </summary>
    private void HandleAquariumStateChanged(AquariumSystem aquarium)
    {
        if (missionCompleted) return;
        if (MissionManager.Instance == null || MissionManager.Instance.CurrentMissionIndex != TargetMissionIndex) return;

        if (aquarium.IsFull && !isTracking)
        {
            isTracking = true;
            maintainTimer = 0f;
            Debug.Log("[FishAquariumMission7] Akuarium penuh (9/9). Countdown 3 menit dimulai.");
        }
        else if (!aquarium.IsFull && isTracking)
        {
            isTracking = false;
            maintainTimer = 0f;
            Debug.Log($"[FishAquariumMission7] Ikan berkurang ({aquarium.FishCount}/{aquarium.MaxFish}). Countdown direset.");
        }
    }

    /// <summary>
    /// Dipanggil sesaat sebelum ikan dihapus dari daftar.
    /// Mereset countdown secara langsung tanpa menunggu AquariumStateChanged.
    /// </summary>
    private void HandleFishDied(AquariumSystem aquarium, FishInstanceState fish)
    {
        if (missionCompleted) return;
        if (MissionManager.Instance == null || MissionManager.Instance.CurrentMissionIndex != TargetMissionIndex) return;

        isTracking = false;
        maintainTimer = 0f;
        Debug.Log($"[FishAquariumMission7] Ikan '{fish.itemName}' mati. Countdown direset.");
    }
}
