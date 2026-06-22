using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menampilkan tutorial berbasis ScriptableObject menggunakan satu panel UI reusable.
/// </summary>
[DefaultExecutionOrder(-90)]
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private GameObject imageContainer;

    [Header("Texts")]
    [SerializeField] private Text headerText;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text stepIndicatorText;
    [SerializeField] private Text previousButtonText;
    [SerializeField] private Text nextButtonText;

    [Header("Visuals")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private float maxIllustrationWidth  = 500f;
    [SerializeField] private float maxIllustrationHeight = 300f;

    [Header("Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    public event Action<TutorialSequenceSO> OnTutorialFinished;

    public bool IsPlaying { get; private set; }
    public TutorialSequenceSO CurrentTutorial => _currentTutorial;

    private TutorialSequenceSO _currentTutorial;
    private int _currentStepIndex;
    private bool _playerWasLockedByTutorial;
    private bool _didFreezeTime;
    private readonly HashSet<string> _completedIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tutorialRoot != null)
            tutorialRoot.SetActive(false);
    }

    private void Start()
    {
        if (previousButton != null) previousButton.onClick.AddListener(ShowPreviousStep);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextStepOrFinish);
        if (closeButton != null) closeButton.onClick.AddListener(() => CloseCurrentTutorial());
    }

    private void OnDestroy()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(ShowPreviousStep);
        if (nextButton != null) nextButton.onClick.RemoveListener(ShowNextStepOrFinish);
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();

        if (Instance == this)
            Instance = null;
    }

    public bool TryPlayTutorial(TutorialSequenceSO tutorial, bool ignorePlayOnce = false, bool markCompletedOnClose = true)
    {
        if (!CanPlay(tutorial, ignorePlayOnce))
            return false;

        PlayTutorialInternal(tutorial, markCompletedOnClose);
        return true;
    }

    /// <param name="suppressFinishedEvent">
    /// Jika true, event OnTutorialFinished tidak akan di-fire.
    /// Gunakan ini saat menutup tutorial untuk keperluan internal (misal: ganti kategori),
    /// bukan saat user benar-benar selesai/menutup tutorial.
    /// </param>
    public void CloseCurrentTutorial(bool suppressFinishedEvent = false)
    {
        if (!IsPlaying)
            return;

        bool shouldMarkCompleted = _markCompletedOnClose;
        TutorialSequenceSO finishedTutorial = _currentTutorial;

        if (tutorialRoot != null)
            tutorialRoot.SetActive(false);

        if (_playerWasLockedByTutorial)
            SetPlayerBlocked(false);

        if (shouldMarkCompleted && finishedTutorial != null)
            MarkCompleted(finishedTutorial);

        _currentTutorial = null;
        _currentStepIndex = 0;
        _playerWasLockedByTutorial = false;
        _markCompletedOnClose = true;
        IsPlaying = false;

        if (!suppressFinishedEvent)
            OnTutorialFinished?.Invoke(finishedTutorial);
    }

    public bool IsTutorialCompleted(TutorialSequenceSO tutorial)
    {
        if (tutorial == null)
            return false;

        return _completedIds.Contains(tutorial.TutorialId);
    }

    public void ResetTutorialCompletion(TutorialSequenceSO tutorial)
    {
        if (tutorial == null)
            return;

        _completedIds.Remove(tutorial.TutorialId);
    }

    /// <summary>
    /// Menghapus semua catatan tutorial yang sudah selesai.
    /// Dipanggil saat memulai New Game agar tutorial muncul kembali dari awal.
    /// </summary>
    public void ResetAllTutorials()
    {
        _completedIds.Clear();
    }

    /// <summary>
    /// Mengembalikan daftar tutorial ID yang sudah selesai untuk disimpan ke save data.
    /// </summary>
    public List<string> CaptureSaveData()
    {
        return new List<string>(_completedIds);
    }

    /// <summary>
    /// Memuat daftar tutorial ID yang sudah selesai dari save data.
    /// </summary>
    public void RestoreFromSaveData(List<string> ids)
    {
        _completedIds.Clear();
        if (ids == null)
            return;

        foreach (string id in ids)
        {
            if (!string.IsNullOrEmpty(id))
                _completedIds.Add(id);
        }
    }

    private bool _markCompletedOnClose = true;

    private bool CanPlay(TutorialSequenceSO tutorial, bool ignorePlayOnce)
    {
        if (tutorial == null || tutorial.Steps == null || tutorial.StepCount == 0)
            return false;

        if (IsPlaying)
            return false;

        if (!ignorePlayOnce && tutorial.PlayOnce && IsTutorialCompleted(tutorial))
            return false;

        return true;
    }

    private void PlayTutorialInternal(TutorialSequenceSO tutorial, bool markCompletedOnClose)
    {
        _currentTutorial = tutorial;
        _currentStepIndex = 0;
        _markCompletedOnClose = markCompletedOnClose;
        IsPlaying = true;

        _playerWasLockedByTutorial = tutorial.LockPlayerWhileOpen;
        if (_playerWasLockedByTutorial)
            SetPlayerBlocked(true);

        if (tutorialRoot != null)
            tutorialRoot.SetActive(true);

        RefreshStep();
    }

    private void ShowPreviousStep()
    {
        if (!IsPlaying || _currentTutorial == null || _currentStepIndex <= 0)
            return;

        _currentStepIndex--;
        RefreshStep();
    }

    private void ShowNextStepOrFinish()
    {
        if (!IsPlaying || _currentTutorial == null)
            return;

        _currentStepIndex = (_currentStepIndex + 1) % _currentTutorial.StepCount;
        RefreshStep();
    }

    private void RefreshStep()
    {
        if (_currentTutorial == null || _currentTutorial.Steps == null || _currentTutorial.StepCount == 0)
            return;

        TutorialSequenceSO.TutorialStep step = _currentTutorial.Steps[_currentStepIndex];

        if (headerText != null)
            headerText.text = $"{_currentTutorial.DisplayName} ({_currentStepIndex + 1}/{_currentTutorial.StepCount})";

        if (titleText != null)
            titleText.text = step != null ? step.Title : string.Empty;

        if (bodyText != null)
            bodyText.text = step != null ? step.Description : string.Empty;

        if (stepIndicatorText != null)
            stepIndicatorText.text = $"{_currentStepIndex + 1} / {_currentTutorial.StepCount}";

        bool hasImage = step != null && step.Illustration != null;
        if (illustrationImage != null)
        {
            illustrationImage.sprite = hasImage ? step.Illustration : null;
            illustrationImage.enabled = hasImage;

            if (hasImage)
                FitIllustrationToSprite(step.Illustration);
        }

        if (imageContainer != null)
            imageContainer.SetActive(hasImage);

        if (previousButton != null)
            previousButton.interactable = _currentStepIndex > 0;

        if (previousButtonText != null)
            previousButtonText.text = "Sebelumnya";

        if (nextButtonText != null)
            nextButtonText.text = "Selanjutnya";
    }

    /// <summary>
    /// Memblokir atau membuka seluruh input dan membekukan waktu saat tutorial aktif.
    /// Jika game sudah di-pause oleh PauseManager, timeScale tidak diubah.
    /// </summary>
    private void SetPlayerBlocked(bool blocked)
    {
        var pm = PlayerInputManager.Instance;
        if (pm == null)
            return;

        if (blocked)
        {
            pm.SetPlayerMovement(false);
            pm.SetCursorAndLook(false, false);
            pm.SetInteractionBlocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Bekukan waktu hanya jika PauseManager belum melakukannya
            bool alreadyPaused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
            if (!alreadyPaused)
            {
                Time.timeScale = 0f;
                _didFreezeTime = true;
            }
            return;
        }

        // Unblock
        pm.SetInteractionBlocked(false);

        bool isPaused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        pm.SetPlayerMovement(!isPaused);
        pm.SetCursorAndLook(!isPaused, !isPaused);

        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Kembalikan timeScale hanya jika tutorial ini yang membekukannya
        if (_didFreezeTime)
        {
            Time.timeScale = 1f;
            _didFreezeTime = false;
        }
    }

    /// <summary>
    /// Menyesuaikan ukuran IlustrasionImage agar fit ke dalam container parent
    /// sambil mempertahankan rasio asli sprite.
    /// </summary>
    private void FitIllustrationToSprite(Sprite sprite)
    {
        if (illustrationImage == null || sprite == null)
            return;

        RectTransform rt = illustrationImage.rectTransform;
        float aspectRatio = (float)sprite.texture.width / sprite.texture.height;

        float width  = maxIllustrationWidth;
        float height = width / aspectRatio;

        if (height > maxIllustrationHeight)
        {
            height = maxIllustrationHeight;
            width  = height * aspectRatio;
        }

        rt.sizeDelta = new Vector2(width, height);
    }

    private static void MarkCompleted(TutorialSequenceSO tutorial)
    {
        if (Instance == null || tutorial == null)
            return;

        Instance._completedIds.Add(tutorial.TutorialId);
    }
}
