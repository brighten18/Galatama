using System;
using UnityEngine;

/// <summary>
/// Orchestrates the opening sequence: opening monologue → tutorial panel → mission 1 begins.
/// Subscribe to OnOpeningSequenceComplete to know when the full sequence is done.
/// </summary>
[DefaultExecutionOrder(-70)]
public class OpeningSequencer : MonoBehaviour
{
    public static OpeningSequencer Instance { get; private set; }

    [SerializeField] private TutorialSequenceSO openingTutorial;

    /// <summary>Fired once both the opening monologue and tutorial have finished.</summary>
    public event Action OnOpeningSequenceComplete;

    /// <summary>True once the entire opening sequence (monologue + tutorial) is done.</summary>
    public bool IsSequenceComplete { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsSequenceComplete = false;
    }

    private void OnEnable()
    {
        if (MonologueManager.Instance != null)
            MonologueManager.Instance.OnMonologueFinished += OnMonologueFinished;
    }

    private void OnDisable()
    {
        if (MonologueManager.Instance != null)
            MonologueManager.Instance.OnMonologueFinished -= OnMonologueFinished;

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;
    }

    private void Start()
    {
        // If there is no opening monologue active, go directly to tutorial (or complete)
        if (MonologueManager.Instance == null || !MonologueManager.Instance.IsActiveOrPending)
            TryPlayTutorialOrComplete();
    }

    private void OnMonologueFinished()
    {
        if (IsSequenceComplete) return;
        TryPlayTutorialOrComplete();
    }

    private void TryPlayTutorialOrComplete()
    {
        if (openingTutorial != null && TutorialManager.Instance != null)
        {
            if (TutorialCategorySwitcher.Instance != null)
                TutorialCategorySwitcher.Instance.SetNavigationEnabled(false);

            TutorialManager.Instance.OnTutorialFinished += OnTutorialFinished;
            bool started = TutorialManager.Instance.TryPlayTutorial(openingTutorial, ignorePlayOnce: true);
            if (!started)
            {
                if (TutorialCategorySwitcher.Instance != null)
                    TutorialCategorySwitcher.Instance.SetNavigationEnabled(true);
                CompleteSequence();
            }
        }
        else
        {
            CompleteSequence();
        }
    }

    private void OnTutorialFinished(TutorialSequenceSO _)
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialFinished -= OnTutorialFinished;

        if (TutorialCategorySwitcher.Instance != null)
            TutorialCategorySwitcher.Instance.SetNavigationEnabled(true);

        CompleteSequence();
    }

    private void CompleteSequence()
    {
        IsSequenceComplete = true;
        OnOpeningSequenceComplete?.Invoke();
    }
}
