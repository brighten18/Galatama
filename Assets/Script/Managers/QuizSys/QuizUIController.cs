using UnityEngine;
using UnityEngine.UI;

public class QuizUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private Text waveText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text questionText;

    [Header("Answers")]
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private Text[] answerTexts;

    [Header("Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pickSfx;
    [SerializeField, Range(0f, 1f)] private float pickSfxVolume = 1f;

    public Button[] AnswerButtons => answerButtons;
    public Button RetryButton => retryButton;
    public Button NextButton => nextButton;
    public Button CloseButton => closeButton;

    public void ShowRoot(bool show)
    {
        if (root != null) root.SetActive(show);
        if (!show)
        {
            if (questionPanel != null) questionPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
        }
    }

    public void SetQuestion(string wave, string progress, string question, string[] options)
    {
        if (questionPanel != null) questionPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);

        if (waveText != null) waveText.text = wave;
        if (progressText != null) progressText.text = progress;
        if (questionText != null) questionText.text = question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            bool inRange = options != null && i < options.Length;
            if (i < answerTexts.Length && answerTexts[i] != null)
                answerTexts[i].text = inRange ? options[i] : "-";

            if (answerButtons[i] != null)
                answerButtons[i].interactable = inRange;
        }

    }

    public void LockAnswers()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
                answerButtons[i].interactable = false;
        }
    }

    public void ShowResult(string message, bool showRetry, bool showNext)
    {
        if (questionPanel != null) questionPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
        if (retryButton != null) retryButton.gameObject.SetActive(showRetry);
        if (nextButton != null) nextButton.gameObject.SetActive(showNext);
    }

    public void PlayPickSfx()
    {
        if (sfxSource == null || pickSfx == null) return;
        sfxSource.PlayOneShot(pickSfx, pickSfxVolume);
    }
}
