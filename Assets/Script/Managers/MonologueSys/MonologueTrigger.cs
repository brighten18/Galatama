using System.Collections;
using UnityEngine;

/// <summary>
/// Memicu monologue dari Monologue Library saat misi tertentu selesai.
/// Tambahkan komponen ini ke GameObject manapun di scene dan isi
/// missionIndex serta monologueKey di Inspector.
/// </summary>
public class MonologueTrigger : MonoBehaviour
{
    [Tooltip("Index misi yang, setelah selesai, akan memicu monologue ini. Sesuaikan dengan urutan missions di MissionManager (0-based).")]
    [SerializeField] private int missionIndex = 1;

    [Tooltip("Key monologue di Monologue Library pada MonologueManager.")]
    [SerializeField] private string monologueKey;

    [Tooltip("Jika true, monologue hanya akan diputar sekali.")]
    [SerializeField] private bool playOnce = true;

    [Tooltip("Jeda (detik) setelah poster popup tertutup sebelum monologue diputar. Berguna jika misi selesai saat popup masih terbuka.")]
    [SerializeField] private float postCloseDelay = 0f;

    private bool _hasPlayed;
    private bool _subscribed;

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed || MissionManager.Instance == null) return;
        if (playOnce && MonologueManager.Instance != null && MonologueManager.Instance.IsMonologueCompleted(monologueKey))
            _hasPlayed = true;
        MissionManager.Instance.OnMissionCompleted += OnMissionCompleted;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || MissionManager.Instance == null) return;
        MissionManager.Instance.OnMissionCompleted -= OnMissionCompleted;
        _subscribed = false;
    }

    private void OnMissionCompleted(int completedIndex)
    {
        if (completedIndex != missionIndex) return;
        if (playOnce && _hasPlayed) return;

        if (MonologueManager.Instance == null)
        {
            Debug.LogWarning("[MonologueTrigger] MonologueManager tidak ditemukan di scene.");
            return;
        }

        _hasPlayed = true;
        MonologueManager.Instance.SetPending(true);
        StartCoroutine(PlayAfterPopupClosed());
    }

    /// <summary>
    /// Menunggu hingga PosterPopupManager selesai ditutup, lalu menunggu
    /// postCloseDelay detik sebelum memainkan monologue.
    /// </summary>
    private IEnumerator PlayAfterPopupClosed()
    {
        while (PosterPopupManager.Instance != null && PosterPopupManager.Instance.IsOpen)
            yield return null;

        if (postCloseDelay > 0f)
            yield return new WaitForSeconds(postCloseDelay);

        MonologueManager.Instance?.PlayMonologue(monologueKey);
    }
}
