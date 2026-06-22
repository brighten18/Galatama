using UnityEngine;

/// <summary>
/// Memunculkan tutorial popup saat player memasuki area trigger.
/// Opsional: hanya muncul setelah misi tertentu selesai.
/// Nonaktifkan navigasi kategori selama tutorial berlangsung.
/// </summary>
public class AreaTriggerTutorial : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [Tooltip("Tutorial yang akan ditampilkan saat player memasuki area ini.")]
    [SerializeField] private TutorialSequenceSO tutorialToPlay;

    [Tooltip("Index misi (0-based) yang harus sudah selesai sebelum tutorial bisa muncul. " +
             "Set ke -1 untuk tidak ada persyaratan misi.")]
    [SerializeField] private int requiredMissionIndex = -1;

    [Tooltip("Jika true, tutorial akan ditampilkan meskipun sudah pernah dimainkan.")]
    [SerializeField] private bool ignorePlayOnce = false;

    [Tooltip("Jika true, tutorial akan ditandai selesai saat ditutup.")]
    [SerializeField] private bool markCompletedOnClose = true;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(PlayerTag)) return;
        if (tutorialToPlay == null || TutorialManager.Instance == null) return;

        // Cek apakah misi yang disyaratkan sudah selesai
        if (requiredMissionIndex >= 0)
        {
            if (MissionManager.Instance == null ||
                MissionManager.Instance.CurrentMissionIndex <= requiredMissionIndex)
                return;
        }

        _triggered = true;

        TutorialCategorySwitcher.Instance?.SetNavigationEnabled(false);
        TutorialManager.Instance.OnTutorialFinished += OnTutorialFinished;

        bool started = TutorialManager.Instance.TryPlayTutorial(tutorialToPlay, ignorePlayOnce, markCompletedOnClose);

        if (!started)
        {
            _triggered = false;
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;
            TutorialCategorySwitcher.Instance?.SetNavigationEnabled(true);
        }
    }

    /// <summary>
    /// Dipanggil saat tutorial selesai atau ditutup oleh pemain.
    /// Mengembalikan navigasi kategori agar bisa digunakan kembali.
    /// </summary>
    private void OnTutorialFinished(TutorialSequenceSO _)
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;

        TutorialCategorySwitcher.Instance?.SetNavigationEnabled(true);
    }
}
