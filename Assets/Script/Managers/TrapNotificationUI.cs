using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton toast notification that appears on screen when a fish enters the trap.
/// Subscribes to FishTrapWorld.OnFishCaptured automatically.
/// Attach this component to a GameObject in the Canvas hierarchy.
/// </summary>
public class TrapNotificationUI : MonoBehaviour
{
    public static TrapNotificationUI Instance { get; private set; }

    [Header("References")]
    [Tooltip("Root panel to toggle on/off. Must have a CanvasGroup component (auto-added if missing).")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Text messageText;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 3.5f;
    [SerializeField] private float fadeInSpeed = 6f;
    [SerializeField] private float fadeOutSpeed = 2.5f;

    private CanvasGroup canvasGroup;
    private Coroutine activeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (notificationPanel != null)
        {
            canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = notificationPanel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            notificationPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[TrapNotificationUI] notificationPanel belum di-assign di Inspector.");
        }
    }

    private void OnEnable()
    {
        FishTrapWorld.OnFishCaptured += HandleFishCaptured;
    }

    private void OnDisable()
    {
        FishTrapWorld.OnFishCaptured -= HandleFishCaptured;
    }

    private void HandleFishCaptured(string fishName)
    {
        string display = string.IsNullOrEmpty(fishName) ? "Ikan" : fishName;
        ShowNotification($"Ikan masuk perangkap!\n{display}");
    }

    /// <summary>Displays a toast notification with the provided message.</summary>
    public void ShowNotification(string message)
    {
        if (notificationPanel == null || messageText == null || canvasGroup == null)
        {
            Debug.LogWarning("[TrapNotificationUI] Referensi UI belum lengkap, notifikasi tidak ditampilkan.");
            return;
        }

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        messageText.text = message;
        notificationPanel.SetActive(true);
        activeRoutine = StartCoroutine(NotificationRoutine());
    }

    private IEnumerator NotificationRoutine()
    {
        // Fade in
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeInSpeed;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        // Fade out
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeOutSpeed;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        notificationPanel.SetActive(false);
        activeRoutine = null;
    }
}
