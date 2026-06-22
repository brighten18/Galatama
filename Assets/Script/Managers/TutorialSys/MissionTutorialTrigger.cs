using UnityEngine;

/// <summary>
/// Menampilkan tutorial setelah misi dengan index tertentu selesai.
/// Letakkan komponen ini di scene dan isi field tutorial serta targetMissionIndex.
/// </summary>
public class MissionTutorialTrigger : MonoBehaviour
{
    [Header("Mission")]
    [Tooltip("Index misi yang harus selesai sebelum tutorial ditampilkan (0-based). Misi 4 = index 3.")]
    [SerializeField] private int targetMissionIndex = 3;

    [Header("Tutorial")]
    [SerializeField] private TutorialSequenceSO tutorial;
    [SerializeField] private bool ignorePlayOnce = false;
    [SerializeField] private bool markCompletedOnClose = true;

    private bool _hasTriggered;

    private void Start()
    {
        if (MissionManager.Instance == null)
        {
            Debug.LogWarning("[MissionTutorialTrigger] MissionManager.Instance tidak ditemukan saat Start.");
            return;
        }

        MissionManager.Instance.OnMissionCompleted += OnMissionCompleted;
    }

    private void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionCompleted -= OnMissionCompleted;
    }

    private void OnMissionCompleted(int missionIndex)
    {
        if (_hasTriggered || missionIndex != targetMissionIndex)
            return;

        if (TutorialManager.Instance == null)
        {
            Debug.LogWarning("[MissionTutorialTrigger] TutorialManager.Instance tidak ditemukan.");
            return;
        }

        TutorialCategorySwitcher.Instance?.SetNavigationEnabled(false);
        TutorialManager.Instance.OnTutorialFinished += OnTutorialFinished;

        bool played = TutorialManager.Instance.TryPlayTutorial(tutorial, ignorePlayOnce, markCompletedOnClose);

        if (played)
        {
            _hasTriggered = true;
            Debug.Log($"[MissionTutorialTrigger] Tutorial '{tutorial?.name}' ditampilkan setelah misi {missionIndex} selesai.");
        }
        else
        {
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;
            TutorialCategorySwitcher.Instance?.SetNavigationEnabled(true);
        }
    }

    private void OnTutorialFinished(TutorialSequenceSO _)
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;

        TutorialCategorySwitcher.Instance?.SetNavigationEnabled(true);
    }
}
