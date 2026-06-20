using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionObjectiveUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;

    [Header("Strikethrough")]
    [SerializeField] private RectTransform strikethroughLine;
    [SerializeField] private float strikethroughDuration = 0.4f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float missionTransitionDelay = 1f;
    [SerializeField] private float allCompleteDisplayDuration = 2f;

    private CanvasGroup canvasGroup;
    private Coroutine activeFadeRoutine;
    private bool _missionDisplayed;
    private MissionData _pendingMission;
    private readonly List<RectTransform> _activeStrikethroughLines = new List<RectTransform>();

    private void Awake()
    {
        if (panel == null) return;

        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        // Hide the template; instances are created dynamically per text line
        if (strikethroughLine != null)
            strikethroughLine.gameObject.SetActive(false);

        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStarted += ShowMission;
            MissionManager.Instance.OnAllMissionsCompleted += HandleAllCompleted;
        }

        if (MonologueManager.Instance != null)
            MonologueManager.Instance.OnMonologueFinished += OnMonologueFinished;

        PosterMission3Tracker.OnAnyProgressChanged += OnPosterProgress;
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStarted -= ShowMission;
            MissionManager.Instance.OnAllMissionsCompleted -= HandleAllCompleted;
        }

        if (MonologueManager.Instance != null)
            MonologueManager.Instance.OnMonologueFinished -= OnMonologueFinished;

        PosterMission3Tracker.OnAnyProgressChanged -= OnPosterProgress;
    }

    private void Start()
    {
        // Jika monolog sedang berjalan atau terjadwal, tunda tampilan misi hingga monolog selesai
        if (MonologueManager.Instance != null && MonologueManager.Instance.IsActiveOrPending)
        {
            _pendingMission = MissionManager.Instance?.CurrentMission;
            return;
        }

        // Fallback: tampilkan misi aktif hanya jika event belum menanganinya lebih dulu
        if (!_missionDisplayed && MissionManager.Instance != null && MissionManager.Instance.CurrentMission != null)
            ShowMission(MissionManager.Instance.CurrentMission);
    }

    /// <summary>Dipanggil setiap kali sebuah monolog selesai. Menampilkan misi yang sudah diantrekan.</summary>
    private void OnMonologueFinished()
    {
        // Jangan unsubscribe di sini — lifecycle subscription dikelola oleh OnEnable/OnDisable.
        // Self-unsubscribe akan membuat monolog ke-2 dst tidak memicu transisi misi berikutnya.

        if (_pendingMission != null)
        {
            var mission = _pendingMission;
            _pendingMission = null;
            ShowMission(mission);
        }
        else if (!_missionDisplayed && MissionManager.Instance != null && MissionManager.Instance.CurrentMission != null)
        {
            ShowMission(MissionManager.Instance.CurrentMission);
        }
    }

    private void ShowMission(MissionData data)
    {
        if (panel == null || data == null) return;

        // Jika monolog sedang berjalan atau terjadwal, antrekan misi dan sembunyikan panel aktif
        if (MonologueManager.Instance != null && MonologueManager.Instance.IsActiveOrPending)
        {
            _pendingMission = data;
            if (canvasGroup != null && canvasGroup.alpha > 0f)
            {
                if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
                activeFadeRoutine = StartCoroutine(StrikethroughThenFadeOut(0f));
            }
            return;
        }

        _missionDisplayed = true;

        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);

        // Jika panel sedang terlihat, coret dulu lalu fade out sebelum tampil misi baru
        if (canvasGroup != null && canvasGroup.alpha > 0f)
            activeFadeRoutine = StartCoroutine(StrikethroughThenFadeOutThenIn(data));
        else
            activeFadeRoutine = StartCoroutine(ShowMissionRoutine(data));
    }

    private void HandleAllCompleted()
    {
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(StrikethroughThenFadeOut(allCompleteDisplayDuration));
    }

    /// <summary>Animates strikethrough → fade out → transition delay → fade in next mission.</summary>
    private IEnumerator StrikethroughThenFadeOutThenIn(MissionData data)
    {
        // 1. Animasi garis coret pada deskripsi misi yang selesai
        yield return StartCoroutine(AnimateStrikethrough());

        // 2. Fade out seluruh panel
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(startAlpha * (1f - elapsed / fadeOutDuration));
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // 3. Hapus instance garis coret sebelum misi berikutnya
        ClearStrikethroughLines();

        // 4. Waktu senggang sebelum misi baru tampil
        yield return new WaitForSeconds(missionTransitionDelay);

        activeFadeRoutine = StartCoroutine(ShowMissionRoutine(data));
    }

    /// <summary>Animates strikethrough → display delay → fade out. Used when all missions are complete.</summary>
    private IEnumerator StrikethroughThenFadeOut(float delay)
    {
        // 1. Animasi garis coret pada misi terakhir
        yield return StartCoroutine(AnimateStrikethrough());

        // 2. Biarkan tampil sebentar
        yield return new WaitForSeconds(delay);

        // 3. Fade out
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
            yield return null;
        }

        SetPanelVisible(false);
        ClearStrikethroughLines();
        activeFadeRoutine = null;
    }

    /// <summary>Animates all active strikethrough lines growing from left to right simultaneously.</summary>
    private IEnumerator AnimateStrikethrough()
    {
        if (_activeStrikethroughLines.Count == 0) yield break;

        float elapsed = 0f;
        while (elapsed < strikethroughDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / strikethroughDuration);
            foreach (var line in _activeStrikethroughLines)
            {
                if (line == null) continue;
                var s = line.localScale;
                s.x = t;
                line.localScale = s;
            }
            yield return null;
        }

        // Pastikan semua tepat di 1
        foreach (var line in _activeStrikethroughLines)
        {
            if (line == null) continue;
            var s = line.localScale;
            s.x = 1f;
            line.localScale = s;
        }
    }

    /// <summary>
    /// Creates one strikethrough line instance per rendered text line, each sized and
    /// positioned to track the vertical center of its corresponding line.
    /// </summary>
    private void SetupStrikethroughLines()
    {
        ClearStrikethroughLines();

        if (strikethroughLine == null || descriptionText == null) return;

        // Force the generator to recalculate immediately with current text and rect size
        TextGenerator gen = descriptionText.cachedTextGenerator;
        gen.Populate(descriptionText.text,
            descriptionText.GetGenerationSettings(descriptionText.rectTransform.rect.size));

        int lineCount = gen.lineCount;
        if (lineCount == 0) return;

        float textWidth = descriptionText.rectTransform.rect.width;
        float lineHeight = descriptionText.preferredHeight / lineCount;

        for (int i = 0; i < lineCount; i++)
        {
            RectTransform lineRT = Instantiate(strikethroughLine, descriptionText.transform);
            lineRT.name = "Strikethrough";
            lineRT.gameObject.SetActive(true);

            // Anchor top-left, pivot left-center so localScale.x animates left → right
            lineRT.anchorMin = new Vector2(0f, 1f);
            lineRT.anchorMax = new Vector2(0f, 1f);
            lineRT.pivot = new Vector2(0f, 0.5f);

            // Match full text width; keep the template's height
            lineRT.sizeDelta = new Vector2(textWidth, strikethroughLine.sizeDelta.y);

            // Vertical center of line i, measured downward from the top of the text rect
            float centerY = -(i * lineHeight + lineHeight * 0.5f);
            lineRT.anchoredPosition = new Vector2(0f, centerY);

            // Hidden initially — will be revealed by AnimateStrikethrough
            var scale = lineRT.localScale;
            scale.x = 0f;
            lineRT.localScale = scale;

            _activeStrikethroughLines.Add(lineRT);
        }
    }

    /// <summary>Destroys all dynamically created strikethrough line instances.</summary>
    private void ClearStrikethroughLines()
    {
        foreach (var line in _activeStrikethroughLines)
            if (line != null) Destroy(line.gameObject);
        _activeStrikethroughLines.Clear();
    }

    /// <summary>Updates the panel text, sets up per-line strikethrough, then fades in.</summary>
    private IEnumerator ShowMissionRoutine(MissionData data)
    {
        if (titleText != null) titleText.text = data.MissionTitle;
        if (descriptionText != null) descriptionText.text = BuildDescription(data);

        SetupStrikethroughLines();

        yield return StartCoroutine(FadeIn());
    }

    /// <summary>Builds the description string, appending a live progress counter for the poster mission.</summary>
    private string BuildDescription(MissionData data)
    {
        if (PosterMission3Tracker.Instance != null &&
            MissionManager.Instance != null &&
            MissionManager.Instance.CurrentMissionIndex == PosterMission3Tracker.Instance.TargetMissionIndex)
        {
            return $"{data.MissionDescription} {PosterMission3Tracker.Instance.ReadCount}/{PosterMission3Tracker.Instance.TotalPosters}";
        }
        return data.MissionDescription;
    }

    /// <summary>Updates the description text live each time a new poster is read.</summary>
    private void OnPosterProgress(int readCount, int total)
    {
        if (descriptionText == null) return;
        var mission = MissionManager.Instance?.CurrentMission;
        if (mission == null) return;
        descriptionText.text = $"{mission.MissionDescription} {readCount}/{total}";
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
