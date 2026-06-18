using System;
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
    [SerializeField] private Text nextButtonText;

    [Header("Visuals")]
    [SerializeField] private Image illustrationImage;

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
        if (closeButton != null) closeButton.onClick.AddListener(CloseCurrentTutorial);
    }

    private void OnDestroy()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(ShowPreviousStep);
        if (nextButton != null) nextButton.onClick.RemoveListener(ShowNextStepOrFinish);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseCurrentTutorial);

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

    public void CloseCurrentTutorial()
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

        OnTutorialFinished?.Invoke(finishedTutorial);
    }

    public bool IsTutorialCompleted(TutorialSequenceSO tutorial)
    {
        if (tutorial == null)
            return false;

        return PlayerPrefs.GetInt(GetPlayerPrefsKey(tutorial), 0) == 1;
    }

    public void ResetTutorialCompletion(TutorialSequenceSO tutorial)
    {
        if (tutorial == null)
            return;

        PlayerPrefs.DeleteKey(GetPlayerPrefsKey(tutorial));
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

        if (_currentStepIndex < _currentTutorial.StepCount - 1)
        {
            _currentStepIndex++;
            RefreshStep();
            return;
        }

        CloseCurrentTutorial();
    }

    private void RefreshStep()
    {
        if (_currentTutorial == null || _currentTutorial.Steps == null || _currentTutorial.StepCount == 0)
            return;

        TutorialSequenceSO.TutorialStep step = _currentTutorial.Steps[_currentStepIndex];

        if (headerText != null)
            headerText.text = _currentTutorial.DisplayName;

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
        }

        if (imageContainer != null)
            imageContainer.SetActive(hasImage);

        if (previousButton != null)
            previousButton.interactable = _currentStepIndex > 0;

        if (nextButtonText != null)
            nextButtonText.text = _currentStepIndex >= _currentTutorial.StepCount - 1 ? "Selesai" : "Next";
    }

    private static void SetPlayerBlocked(bool blocked)
    {
        var pm = PlayerInputManager.Instance;
        if (pm == null)
            return;

        if (blocked)
        {
            pm.SetPlayerMovement(false);
            pm.SetCursorAndLook(false, false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            pm.ResetPauseInput();
            pm.ResetInteractInput();
            pm.ResetInteractOBJInput();
            pm.ResetInventoryInput();
            pm.ResetAllQuickSlotInputs();
            return;
        }

        bool isPaused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        pm.SetPlayerMovement(!isPaused);
        pm.SetCursorAndLook(!isPaused, !isPaused);

        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private static string GetPlayerPrefsKey(TutorialSequenceSO tutorial)
    {
        return $"tutorial.completed.{tutorial.TutorialId}";
    }

    private static void MarkCompleted(TutorialSequenceSO tutorial)
    {
        PlayerPrefs.SetInt(GetPlayerPrefsKey(tutorial), 1);
        PlayerPrefs.Save();
    }
}
