using UnityEngine;

/// <summary>
/// Trigger ringan untuk memanggil tutorial saat game mulai atau saat player masuk area.
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        OnStart,
        OnPlayerEnter
    }

    [Header("Tutorial")]
    [SerializeField] private TutorialSequenceSO tutorial;
    [SerializeField] private TriggerMode triggerMode = TriggerMode.OnStart;
    [SerializeField] private bool ignorePlayOnce;
    [SerializeField] private bool markCompletedOnClose = true;
    [SerializeField] private bool disableAfterTriggered = true;

    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";

    private bool _hasTriggered;

    private void Start()
    {
        if (triggerMode == TriggerMode.OnStart)
            TriggerTutorial();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerMode != TriggerMode.OnPlayerEnter || _hasTriggered)
            return;

        if (!other.CompareTag(playerTag))
            return;

        TriggerTutorial();
    }

    public void TriggerTutorial()
    {
        if (_hasTriggered || TutorialManager.Instance == null)
            return;

        bool played = TutorialManager.Instance.TryPlayTutorial(tutorial, ignorePlayOnce, markCompletedOnClose);
        if (!played)
            return;

        _hasTriggered = true;

        if (disableAfterTriggered)
            enabled = false;
    }
}
