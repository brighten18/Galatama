using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Entry tunggal dalam library monologue. Beri key unik agar script lain
/// bisa memanggil PlayMonologue("key") tanpa perlu referensi langsung ke asset.
/// </summary>
[Serializable]
public struct MonologueEntry
{
    [Tooltip("Nama unik untuk monologue ini. Dipakai saat memanggil PlayMonologue(\"key\").")]
    public string key;

    [Tooltip("Asset MonologueData yang berisi panel-panel teks.")]
    public MonologueData data;
}

/// <summary>
/// Manages the opening monologue sequence in the gameplay scene.
/// Displays panel-by-panel with a typewriter effect and a "Lanjut" button.
/// Blocks player movement and camera look while active.
/// Triggered automatically on scene start if an opening monologue is assigned.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MonologueManager : MonoBehaviour
{
    public static MonologueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject monologuePanel;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text panelIndicatorText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image fadeOverlay;

    [Header("Character Pose")]
    [SerializeField] private Image characterImage;
    [SerializeField] private RectTransform characterRectTransform;
    [SerializeField] private Sprite[] characterPoses;

    [Tooltip("Skala per pose — urutannya harus sama dengan array characterPoses. Nilai 1.0 = ukuran pixel asli sprite.")]
    [SerializeField] private float[] characterScales;

    [Tooltip("Posisi anchor yang sama untuk semua pose (anchoredPosition dalam pixel).")]
    [SerializeField] private Vector2 characterPosition;

    [Header("Opening Monologue")]
    [SerializeField] private MonologueData openingMonologue;

    [Header("Monologue Library")]
    [Tooltip("Daftar semua monologue tambahan. Isi Key unik untuk setiap entry.")]
    [SerializeField] private MonologueEntry[] monologues;

    [Header("Settings")]
    [SerializeField] private float typewriterSpeed = 0.04f;
    [SerializeField] private float fadeDuration = 0.3f;

    private int _currentIndex;
    private bool _isTyping;
    private Coroutine _typewriterCoroutine;
    private string[] _processedPanels;
    private int _lastPoseIndex = -1;

    // True jika monologue ini yang membekukan Time.timeScale, bukan PauseManager.
    private bool _didFreezeTime;

    // Key dari monologue yang sedang dimainkan via PlayMonologue(string key).
    private string _currentPlayingKey;

    /// <summary>Fired when all monologue panels have been displayed and dismissed.</summary>
    public event Action OnMonologueFinished;

    /// <summary>
    /// Fired when a named monologue (played via key) finishes.
    /// Passes the key of the finished monologue.
    /// </summary>
    public event Action<string> OnNamedMonologueFinished;

    /// <summary>True while the monologue sequence is on screen.</summary>
    public bool IsPlaying { get; private set; }

    private bool _isPending;

    /// <summary>True while a monologue is either scheduled to play or actively playing.</summary>
    public bool IsActiveOrPending => IsPlaying || _isPending;

    /// <summary>Marks a monologue as pending (scheduled but not yet started). Called by MonologueTrigger.</summary>
    public void SetPending(bool pending) => _isPending = pending;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Kosongkan sprite segera sebagai safety net agar tidak ada pose yang bocor
        // sebelum monolog mulai, meskipun ada sprite ter-assign di scene.
        if (characterImage != null)
            characterImage.sprite = null;

        // Set IsPlaying di Awake agar semua Start() lain (MissionObjectiveUI, MissionManager)
        // sudah melihat flag yang benar sebelum mereka jalan.
        if (openingMonologue != null && openingMonologue.Panels != null && openingMonologue.Panels.Length > 0)
            IsPlaying = true;
    }

    private IEnumerator Start()
    {
        // Yield one frame so all other Start() methods (PlayerInputManager, MissionManager) complete first.
        yield return null;

        if (openingMonologue == null || openingMonologue.Panels == null || openingMonologue.Panels.Length == 0)
        {
            if (monologuePanel != null) monologuePanel.SetActive(false);
            yield break;
        }

        _processedPanels = PreprocessPanels(openingMonologue.Panels);
        nextButton.onClick.AddListener(OnNextClicked);

        IsPlaying = true;
        SetPlayerBlocked(true);

        monologuePanel.SetActive(true);
        UpdateIndicator();

        yield return StartCoroutine(PlayIntro());
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays the given monologue sequence programmatically.
    /// Call this to trigger a monologue from code at any point during gameplay.
    /// </summary>
    public void PlayMonologue(MonologueData data)
    {
        if (data == null || data.Panels == null || data.Panels.Length == 0) return;
        StartCoroutine(PlayMonologueRoutine(data));
    }

    /// <summary>
    /// Plays a monologue from the library by its key (case-sensitive).
    /// </summary>
    public void PlayMonologue(string key)
    {
        if (monologues == null || monologues.Length == 0)
        {
            Debug.LogWarning($"[MonologueManager] Library kosong, key '{key}' tidak ditemukan.");
            return;
        }

        foreach (MonologueEntry entry in monologues)
        {
            if (entry.key == key)
            {
                _currentPlayingKey = key;
                PlayMonologue(entry.data);
                return;
            }
        }

        Debug.LogWarning($"[MonologueManager] Monologue dengan key '{key}' tidak ditemukan di library.");
    }

    /// <summary>
    /// Plays a monologue from the library by its index in the monologues array.
    /// </summary>
    public void PlayMonologue(int index)
    {
        if (monologues == null || index < 0 || index >= monologues.Length)
        {
            Debug.LogWarning($"[MonologueManager] Index {index} di luar range library.");
            return;
        }

        PlayMonologue(monologues[index].data);
    }

    // -------------------------------------------------------------------------
    // Core sequence
    // -------------------------------------------------------------------------

    private IEnumerator PlayIntro()
    {
        // Set pose sebelum fade dimulai agar overlay masih menutupi panel saat pose di-assign,
        // sehingga pose yang benar langsung tampil begitu fade selesai.
        SetRandomPose();

        if (fadeOverlay != null)
            yield return StartCoroutine(Fade(1f, 0f));

        // Re-enable button setelah fade agar button selalu aktif saat panel pertama tampil,
        // baik untuk opening monologue maupun monologue subsequent.
        SetNextButtonInteractable(true);

        ShowPanel(0, setPose: false);
    }

    private IEnumerator PlayMonologueRoutine(MonologueData data)
    {
        _isPending = false;
        _processedPanels = PreprocessPanels(data.Panels);
        _currentIndex = 0;
        IsPlaying = true;
        SetPlayerBlocked(true);

        // Bersihkan konten lama sebelum panel ditampilkan agar tidak ada visual gap.
        if (bodyText != null) bodyText.text = string.Empty;
        if (characterImage != null) characterImage.sprite = null;
        _lastPoseIndex = -1;

        monologuePanel.SetActive(true);
        UpdateIndicator();

        yield return StartCoroutine(PlayIntro());
    }

    private void ShowPanel(int index, bool setPose = true)
    {
        _currentIndex = index;

        if (bodyText != null)
            bodyText.text = string.Empty;

        UpdateIndicator();

        if (setPose)
            SetRandomPose();

        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _typewriterCoroutine = StartCoroutine(TypewriterEffect(_processedPanels[index]));
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        _isTyping = true;
        int totalVisible = CountVisibleChars(fullText);

        for (int i = 0; i <= totalVisible; i++)
        {
            if (bodyText != null)
                bodyText.text = RevealRichText(fullText, i);
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        _isTyping = false;
    }

    // -------------------------------------------------------------------------
    // Button handler
    // -------------------------------------------------------------------------

    private void OnNextClicked()
    {
        if (_isTyping)
        {
            // First click: instantly reveal the full text of the current panel.
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _isTyping = false;

            if (bodyText != null)
                bodyText.text = _processedPanels[_currentIndex];

            return;
        }

        int next = _currentIndex + 1;

        if (next < _processedPanels.Length)
            StartCoroutine(TransitionToPanel(next));
        else
            StartCoroutine(FinishMonologue());
    }

    private IEnumerator TransitionToPanel(int nextIndex)
    {
        SetNextButtonInteractable(false);

        if (fadeOverlay != null)
            yield return StartCoroutine(Fade(0f, 1f));

        ShowPanel(nextIndex);

        if (fadeOverlay != null)
            yield return StartCoroutine(Fade(1f, 0f));

        SetNextButtonInteractable(true);
    }

    private IEnumerator FinishMonologue()
    {
        SetNextButtonInteractable(false);

        if (fadeOverlay != null)
            yield return StartCoroutine(Fade(0f, 1f));

        monologuePanel.SetActive(false);
        IsPlaying = false;
        SetPlayerBlocked(false);
        OnMonologueFinished?.Invoke();

        if (!string.IsNullOrEmpty(_currentPlayingKey))
        {
            string finishedKey = _currentPlayingKey;
            _currentPlayingKey = null;
            OnNamedMonologueFinished?.Invoke(finishedKey);
        }
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------

    private void UpdateIndicator()
    {
        if (panelIndicatorText != null && _processedPanels != null)
            panelIndicatorText.text = $"{_currentIndex + 1} / {_processedPanels.Length}";
    }

    private void SetNextButtonInteractable(bool state)
    {
        if (nextButton != null)
            nextButton.interactable = state;
    }

    /// <summary>Assigns a random pose sprite, avoiding repeating the same pose twice in a row.
    /// Uses the sprite's natural pixel dimensions as base size, then applies a per-pose scale
    /// and a shared position for all poses.</summary>
    private void SetRandomPose()
    {
        if (characterImage == null || characterPoses == null || characterPoses.Length == 0) return;

        int index;
        if (characterPoses.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = UnityEngine.Random.Range(0, characterPoses.Length); }
            while (index == _lastPoseIndex);
        }

        _lastPoseIndex = index;
        Sprite sprite = characterPoses[index];
        characterImage.sprite = sprite;

        if (characterRectTransform != null)
        {
            // Base size = dimensi pixel asli sprite
            characterRectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);

            // Scale per pose; fallback ke 1.0 jika array tidak ter-isi untuk index ini
            float scale = (characterScales != null && index < characterScales.Length)
                ? characterScales[index]
                : 1f;
            characterRectTransform.localScale = new Vector3(scale, scale, 1f);

            characterRectTransform.anchoredPosition = characterPosition;
        }
    }

    // -------------------------------------------------------------------------
    // Application focus
    // -------------------------------------------------------------------------

    /// <summary>
    /// Re-enforces cursor visibility and input block state when the application
    /// regains focus after an alt+tab. Prevents cursor from disappearing or
    /// input from leaking back during an active monologue.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !IsPlaying) return;

        var pm = PlayerInputManager.Instance;
        if (pm != null)
        {
            pm.SetPlayerMovement(false);
            pm.SetCursorAndLook(false, false);
        }

        // SetCursorAndLook may internally alter cursor state — override to ensure
        // cursor is always visible and unlocked while monologue is active.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // -------------------------------------------------------------------------
    // Player input control
    // -------------------------------------------------------------------------

    /// <summary>
    /// Memblokir atau membuka seluruh input dan membekukan waktu saat monologue aktif.
    /// Jika game sudah di-pause oleh PauseManager, timeScale tidak diubah.
    /// </summary>
    private void SetPlayerBlocked(bool blocked)
    {
        var pm = PlayerInputManager.Instance;
        if (pm == null) return;

        pm.SetPlayerMovement(!blocked);
        pm.SetCursorAndLook(!blocked, !blocked);
        pm.SetInteractionBlocked(blocked);

        if (blocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Bekukan waktu hanya jika PauseManager belum melakukannya
            bool alreadyPaused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
            if (!alreadyPaused)
            {
                Time.timeScale = 0f;
                _didFreezeTime = true;
            }
        }
        else
        {
            // Kembalikan timeScale hanya jika monologue ini yang membekukannya
            if (_didFreezeTime)
            {
                Time.timeScale = 1f;
                _didFreezeTime = false;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Rich text typewriter helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts **bold** markdown to Unity rich text tags and trims whitespace.
    /// </summary>
    private static string[] PreprocessPanels(string[] raw)
    {
        string[] result = new string[raw.Length];
        for (int i = 0; i < raw.Length; i++)
            result[i] = Regex.Replace(raw[i].Trim(), @"\*\*(.+?)\*\*", "<b>$1</b>");
        return result;
    }

    /// <summary>
    /// Counts visible (non-tag) characters in a Unity rich text string.
    /// </summary>
    private static int CountVisibleChars(string richText)
    {
        int count = 0;
        bool inTag = false;

        foreach (char c in richText)
        {
            if (c == '<') { inTag = true; continue; }
            if (c == '>') { inTag = false; continue; }
            if (!inTag) count++;
        }

        return count;
    }

    /// <summary>
    /// Returns a substring of a rich text string revealing exactly <paramref name="visibleCount"/>
    /// non-tag characters, keeping HTML tags intact in the output.
    /// </summary>
    private static string RevealRichText(string richText, int visibleCount)
    {
        var sb = new StringBuilder();
        int visible = 0;
        bool inTag = false;

        foreach (char c in richText)
        {
            if (c == '<')
            {
                inTag = true;
                sb.Append(c);
                continue;
            }

            if (inTag)
            {
                sb.Append(c);
                if (c == '>') inTag = false;
                continue;
            }

            if (visible >= visibleCount)
                break;

            sb.Append(c);
            visible++;
        }

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Fade
    // -------------------------------------------------------------------------

    private IEnumerator Fade(float from, float to)
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        Color color = fadeOverlay.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeOverlay.color = color;
            yield return null;
        }

        color.a = to;
        fadeOverlay.color = color;
    }
}
