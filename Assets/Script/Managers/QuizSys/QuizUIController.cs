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
    [SerializeField] private GameObject rewardInfoRoot;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private Text rewardTitleText;
    [SerializeField] private Text rewardDescriptionText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pickSfx;
    [SerializeField, Range(0f, 1f)] private float pickSfxVolume = 1f;

    private Vector2 originalResultTextPosition;
    private Vector2 originalResultTextSizeDelta;

    // Visual center Y of the result panel background, used to center text on fail.
    private const float FailTextCenterY = 93.5f;
    private const float FailTextHeight = 400f;

    public Button[] AnswerButtons => answerButtons;
    public Button RetryButton => retryButton;
    public Button NextButton => nextButton;
    public Button CloseButton => closeButton;

    private void Awake()
    {
        if (resultText != null)
        {
            RectTransform rt = resultText.GetComponent<RectTransform>();
            originalResultTextPosition = rt.anchoredPosition;
            originalResultTextSizeDelta = rt.sizeDelta;
        }
    }

    public void ShowRoot(bool show)
    {
        if (root != null) root.SetActive(show);
        if (!show)
        {
            if (questionPanel != null) questionPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            HideRewardInfo();
        }
    }

    public void SetQuestion(string wave, string progress, string question, string[] options)
    {
        if (questionPanel != null) questionPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);

        if (waveText != null) waveText.text = wave;
        if (progressText != null) progressText.text = progress;
        if (questionText != null) questionText.text = question;
        HideRewardInfo();

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

    public void ShowResult(string message, bool showRetry, bool showNext, bool passed)
    {
        if (questionPanel != null) questionPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);
        if (rewardInfoRoot != null) rewardInfoRoot.SetActive(true);
        if (resultText != null)
        {
            resultText.text = message;
            RectTransform rt = resultText.GetComponent<RectTransform>();
            if (passed)
            {
                rt.anchoredPosition = originalResultTextPosition;
                rt.sizeDelta = originalResultTextSizeDelta;
            }
            else
            {
                rt.anchoredPosition = new Vector2(originalResultTextPosition.x, FailTextCenterY);
                rt.sizeDelta = new Vector2(originalResultTextSizeDelta.x, FailTextHeight);
            }
        }
        if (retryButton != null) retryButton.gameObject.SetActive(showRetry);
        if (nextButton != null) nextButton.gameObject.SetActive(showNext);
    }

    public void ShowRewardInfo(string title, string description, Sprite icon)
    {
        bool hasContent = !string.IsNullOrWhiteSpace(title)
            || !string.IsNullOrWhiteSpace(description)
            || icon != null;

        if (rewardInfoRoot != null)
            rewardInfoRoot.SetActive(hasContent);

        if (!hasContent)
        {
            HideRewardInfo();
            return;
        }

        if (rewardTitleText != null)
            rewardTitleText.text = string.IsNullOrWhiteSpace(title) ? "Hadiah Baru" : title;

        if (rewardDescriptionText != null)
            rewardDescriptionText.text = description ?? string.Empty;

        if (rewardIcon != null)
        {
            rewardIcon.sprite = icon;
            rewardIcon.enabled = icon != null;
        }
    }

    public void HideRewardInfo()
    {
        if (rewardTitleText != null)
            rewardTitleText.text = string.Empty;

        if (rewardDescriptionText != null)
            rewardDescriptionText.text = string.Empty;

        if (rewardIcon != null)
        {
            rewardIcon.sprite = null;
            rewardIcon.enabled = false;
        }
    }

    public void PlayPickSfx()
    {
        if (sfxSource == null || pickSfx == null) return;
        sfxSource.PlayOneShot(pickSfx, pickSfxVolume);
    }
}
