using UnityEngine;

/// <summary>
/// Mengatur alur Misi 8:
/// - Sebelum Misi 8: PAKUM, RANGGA, dan NONA disembunyikan (SetActive false).
/// - Saat Misi 8 dimulai: semua NPC diaktifkan, RANGGA dan NONA dikunci dari interaksi.
/// - Setelah Wave PAKUM lulus: Misi 8 selesai, kunci RANGGA dan NONA dibuka.
/// - NPC tetap aktif selamanya setelah Misi 8 dimulai.
/// </summary>
public class Mission8QuizTracker : MonoBehaviour
{
    private const int TargetMissionIndex = 7;

    [Tooltip("QuizInteractable milik PAKUM.")]
    [SerializeField] private QuizInteractable pakumQuizInteractable;

    [Tooltip("QuizInteractable milik RANGGA — dikunci selama Misi 8 berjalan.")]
    [SerializeField] private QuizInteractable ranggaQuizInteractable;

    [Tooltip("QuizInteractable milik NONA — dikunci selama Misi 8 berjalan.")]
    [SerializeField] private QuizInteractable nonaQuizInteractable;

    private bool missionActive;
    private bool missionCompleted;

    private void OnEnable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStarted += OnMissionStarted;
            MissionManager.Instance.OnAllMissionsCompleted += OnAllMissionsCompleted;
        }
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStarted -= OnMissionStarted;
            MissionManager.Instance.OnAllMissionsCompleted -= OnAllMissionsCompleted;
        }

        if (QuizManager.Instance != null)
            QuizManager.Instance.OnWavePassed -= OnWavePassed;
    }

    private void Start()
    {
        if (QuizManager.Instance != null)
            QuizManager.Instance.OnWavePassed += OnWavePassed;

        int missionIndex = MissionManager.Instance?.CurrentMissionIndex ?? 0;

        if (missionIndex < TargetMissionIndex)
        {
            // Sebelum Misi 8: semua NPC disembunyikan
            SetNPCsActive(false);
        }
        else if (missionIndex == TargetMissionIndex)
        {
            // Misi 8 sedang aktif saat load dari save
            ActivateMission();
        }
        else
        {
            // Misi 8 sudah selesai (missionIndex > 7): pastikan semua NPC aktif
            SetNPCsActive(true);
            missionCompleted = true;
        }
    }

    private void OnMissionStarted(MissionData data)
    {
        if (MissionManager.Instance == null) return;
        if (MissionManager.Instance.CurrentMissionIndex == TargetMissionIndex)
            ActivateMission();
    }

    private void ActivateMission()
    {
        if (missionActive) return;
        missionActive = true;

        // Aktifkan semua NPC saat Misi 8 dimulai
        SetNPCsActive(true);

        // RANGGA & NONA: kunci interaksi selama Misi 8 berjalan
        ranggaQuizInteractable?.SetLocked(true);
        nonaQuizInteractable?.SetLocked(true);

        Debug.Log("[Mission8QuizTracker] Misi 8 aktif — semua NPC aktif, RANGGA & NONA dikunci.");
    }

    private void UnlockOtherNPCs()
    {
        ranggaQuizInteractable?.SetLocked(false);
        nonaQuizInteractable?.SetLocked(false);

        Debug.Log("[Mission8QuizTracker] Misi 8 selesai — RANGGA & NONA dibuka.");
    }

    /// <summary>Mengaktifkan atau menonaktifkan seluruh GameObject root setiap NPC.</summary>
    private void SetNPCsActive(bool active)
    {
        if (pakumQuizInteractable != null)
            pakumQuizInteractable.gameObject.SetActive(active);

        if (ranggaQuizInteractable != null)
            ranggaQuizInteractable.gameObject.SetActive(active);

        if (nonaQuizInteractable != null)
            nonaQuizInteractable.gameObject.SetActive(active);

        Debug.Log($"[Mission8QuizTracker] NPC SetActive({active}).");
    }

    /// <summary>Dipanggil saat QuizManager menyelesaikan sebuah wave.</summary>
    private void OnWavePassed(int waveNumber)
    {
        if (missionCompleted || !missionActive) return;
        if (pakumQuizInteractable == null) return;
        if (waveNumber != pakumQuizInteractable.GetTargetWaveNumber()) return;
        if (MissionManager.Instance == null) return;
        if (MissionManager.Instance.CurrentMissionIndex != TargetMissionIndex) return;

        missionCompleted = true;
        MissionManager.Instance.CompleteMission(TargetMissionIndex);
        UnlockOtherNPCs();

        Debug.Log("[Mission8QuizTracker] Kuis PAKUM selesai — Misi 8 selesai.");
    }

    private void OnAllMissionsCompleted()
    {
        missionActive = false;
    }
}
