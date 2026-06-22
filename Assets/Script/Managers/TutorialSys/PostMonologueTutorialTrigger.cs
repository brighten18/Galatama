using UnityEngine;

/// <summary>
/// Memunculkan tutorial popup secara otomatis setelah monologue berkey tertentu selesai.
/// Navigasi kategori dinonaktifkan selama tutorial berlangsung agar pemain tidak bisa
/// ganti kategori. Jika tutorial dibuka manual dari UI pause, navigasi tetap aktif.
/// </summary>
public class PostMonologueTutorialTrigger : MonoBehaviour
{
    [Tooltip("Key monologue di Monologue Library yang akan memicu tutorial ini.")]
    [SerializeField] private string targetMonologueKey;

    [Tooltip("Tutorial yang akan dimainkan setelah monologue selesai.")]
    [SerializeField] private TutorialSequenceSO tutorialToPlay;

    private bool _subscribed;

    private void OnEnable() => Subscribe();
    private void Start()    => Subscribe();
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed || MonologueManager.Instance == null) return;
        MonologueManager.Instance.OnNamedMonologueFinished += OnNamedMonologueFinished;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        if (MonologueManager.Instance != null)
            MonologueManager.Instance.OnNamedMonologueFinished -= OnNamedMonologueFinished;
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;
        _subscribed = false;
    }

    private void OnNamedMonologueFinished(string key)
    {
        if (key != targetMonologueKey) return;
        if (tutorialToPlay == null || TutorialManager.Instance == null) return;

        if (TutorialCategorySwitcher.Instance != null)
            TutorialCategorySwitcher.Instance.SetNavigationEnabled(false);

        TutorialManager.Instance.OnTutorialFinished += OnTutorialFinished;
        bool started = TutorialManager.Instance.TryPlayTutorial(tutorialToPlay, ignorePlayOnce: true);

        if (!started)
        {
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;
            if (TutorialCategorySwitcher.Instance != null)
                TutorialCategorySwitcher.Instance.SetNavigationEnabled(true);
        }
    }

    /// <summary>
    /// Dipanggil saat tutorial selesai atau ditutup pemain.
    /// Mengembalikan navigasi kategori agar bisa digunakan kembali.
    /// </summary>
    private void OnTutorialFinished(TutorialSequenceSO _)
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;

        if (TutorialCategorySwitcher.Instance != null)
            TutorialCategorySwitcher.Instance.SetNavigationEnabled(true);
    }
}
