using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MissionObjectiveUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float allCompleteDisplayDuration = 2f;

    private CanvasGroup canvasGroup;
    private Coroutine activeFadeRoutine;

    private void Awake()
    {
        if (panel == null) return;

        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        if (MissionManager.Instance == null) return;
        MissionManager.Instance.OnMissionStarted += ShowMission;
        MissionManager.Instance.OnAllMissionsCompleted += HandleAllCompleted;
    }

    private void OnDisable()
    {
        if (MissionManager.Instance == null) return;
        MissionManager.Instance.OnMissionStarted -= ShowMission;
        MissionManager.Instance.OnAllMissionsCompleted -= HandleAllCompleted;
    }

    private void Start()
    {
        // Tampilkan misi aktif saat ini jika event sudah terlanjur fire sebelum subscription
        if (MissionManager.Instance != null && MissionManager.Instance.CurrentMission != null)
            ShowMission(MissionManager.Instance.CurrentMission);
    }

    private void ShowMission(MissionData data)
    {
        if (panel == null || data == null) return;

        if (titleText != null) titleText.text = data.MissionTitle;
        if (descriptionText != null) descriptionText.text = data.MissionDescription;

        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FadeIn());
    }

    private void HandleAllCompleted()
    {
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FadeOutDelayed(allCompleteDisplayDuration));
    }

    private IEnumerator FadeIn()
    {
        SetPanelVisible(true);
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        activeFadeRoutine = null;
    }

    private IEnumerator FadeOutDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
            yield return null;
        }

        SetPanelVisible(false);
        activeFadeRoutine = null;
    }

    /// <summary>Shows or hides the panel using CanvasGroup without calling SetActive,
    /// so the component stays enabled and keeps its event subscriptions.</summary>
    private void SetPanelVisible(bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
