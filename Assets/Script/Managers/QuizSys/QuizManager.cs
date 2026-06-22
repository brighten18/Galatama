using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GALATAMA.MainMenu;

public class QuizManager : MonoBehaviour
{
    private class RuntimeQuestion
    {
        public string question;
        public string[] options;
        public int correctIndex;
    }

    public static QuizManager Instance { get; private set; }

    [Header("Quiz Data")]
    [SerializeField] private QuizWaveSO[] waves;
    [SerializeField] private int passPercent = 80;
    [SerializeField] private float nextQuestionDelay = 0.5f;

    [Header("References")]
    [SerializeField] private QuizUIController ui;
    [SerializeField] private QuizRewardUnlockManager rewardUnlockManager;
    [SerializeField] private InventorySystem inventorySystem;

    private readonly List<RuntimeQuestion> activeQuestions = new List<RuntimeQuestion>();
    private readonly HashSet<int> passedWaveNumbers = new HashSet<int>();
    private int currentWaveIndex;
    private int currentQuestionIndex;
    private int correctCount;
    private bool isOpen;
    private bool inventoryWasOpenBeforeQuiz;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (ui != null)
        {
            ui.ShowRoot(false);
            HookButtons();
        }
    }

    private void HookButtons()
    {
        ButtonBindAnswers();

        if (ui.RetryButton != null) ui.RetryButton.onClick.AddListener(RestartCurrentWave);
        if (ui.NextButton != null) ui.NextButton.onClick.AddListener(GoNextWave);
        if (ui.CloseButton != null) ui.CloseButton.onClick.AddListener(CloseQuiz);
    }

    private void ButtonBindAnswers()
    {
        Button[] buttons = ui.AnswerButtons;
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            if (buttons[i] != null)
                buttons[i].onClick.AddListener(() => OnAnswerPicked(idx));
        }
    }

    public void OpenQuiz()
    {
        int firstUnpassed = Mathf.Clamp(GetFirstUnpassedWaveIndex(), 0, waves.Length - 1);
        OpenQuizInternal(firstUnpassed);
    }

    public void OpenQuizFromWave(int waveNumber)
    {
        if (waves == null || waves.Length == 0) return;

        int targetIndex = FindWaveIndexByNumber(waveNumber);
        if (targetIndex < 0)
        {
            Debug.LogWarning("[Quiz] Wave number tidak ditemukan: " + waveNumber);
            return;
        }

        // Linear progression: wave N hanya bisa dibuka jika wave N-1 sudah lulus.
        if (targetIndex > 0)
        {
            QuizWaveSO prevWave = waves[targetIndex - 1];
            if (prevWave != null && !IsWavePassed(prevWave.waveNumber))
            {
                Debug.Log("[Quiz] Wave sebelumnya belum lulus. Selesaikan Wave " + prevWave.waveNumber + " dulu.");
                return;
            }
        }

        OpenQuizInternal(targetIndex);
    }

    private void OpenQuizInternal(int startWaveIndex)
    {
        if (isOpen || waves == null || waves.Length == 0 || ui == null) return;

        isOpen = true;
        QuizSessionLock.SetLocked(true);

        if (inventorySystem == null) inventorySystem = InventorySystem.Instance;
        if (inventorySystem != null)
        {
            inventoryWasOpenBeforeQuiz = inventorySystem.isOpen;
            if (inventorySystem.isOpen && inventorySystem.inventoryScreenUI != null)
            {
                inventorySystem.inventoryScreenUI.SetActive(false);
                inventorySystem.isOpen = false;
            }
        }

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            PauseManager.Instance.ResumeGame();
        }

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorAndLook(false, false);
            PlayerInputManager.Instance.SetPlayerMovement(false);
            PlayerInputManager.Instance.ResetInteractInput();
            PlayerInputManager.Instance.ResetInteractOBJInput();
            PlayerInputManager.Instance.ResetInventoryInput();
            PlayerInputManager.Instance.ResetPauseInput();
            PlayerInputManager.Instance.ResetAllQuickSlotInputs();
        }

        ui.ShowRoot(true);
        currentWaveIndex = Mathf.Clamp(startWaveIndex, 0, waves.Length - 1);
        StartWave(currentWaveIndex);
    }

    public void CloseQuiz()
    {
        if (!isOpen) return;

        isOpen = false;
        QuizSessionLock.SetLocked(false);
        ui.ShowRoot(false);

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorAndLook(true, true);
            PlayerInputManager.Instance.SetPlayerMovement(true);
            PlayerInputManager.Instance.ResetInteractInput();
            PlayerInputManager.Instance.ResetInteractOBJInput();
            PlayerInputManager.Instance.ResetInventoryInput();
            PlayerInputManager.Instance.ResetPauseInput();
        }

        if (inventorySystem != null && inventoryWasOpenBeforeQuiz && inventorySystem.inventoryScreenUI != null)
        {
            inventorySystem.inventoryScreenUI.SetActive(true);
            inventorySystem.isOpen = true;
        }
    }

    private void StartWave(int waveIndex)
    {
        currentWaveIndex = waveIndex;
        currentQuestionIndex = 0;
        correctCount = 0;

        activeQuestions.Clear();
        activeQuestions.AddRange(BuildQuestionsForWave(waves[currentWaveIndex]));

        if (activeQuestions.Count == 0)
        {
            ui.ShowResult("Wave tidak punya soal valid. Cek data wave/question.", true, false, false);
            return;
        }

        ShowCurrentQuestion();
    }

    private List<RuntimeQuestion> BuildQuestionsForWave(QuizWaveSO wave)
    {
        List<QuizQuestionSO> copy = new List<QuizQuestionSO>();
        for (int i = 0; i < wave.questionPool.Count; i++)
        {
            QuizQuestionSO q = wave.questionPool[i];
            if (q != null && q.IsValid()) copy.Add(q);
        }

        Shuffle(copy);
        int take = Mathf.Min(wave.questionCountToAsk, copy.Count);
        if (take <= 0) return new List<RuntimeQuestion>();

        List<RuntimeQuestion> selected = new List<RuntimeQuestion>();
        for (int i = 0; i < take; i++)
        {
            selected.Add(BuildRuntimeQuestion(copy[i]));
        }
        return selected;
    }

    private RuntimeQuestion BuildRuntimeQuestion(QuizQuestionSO source)
    {
        List<int> order = new List<int> { 0, 1, 2, 3 };
        Shuffle(order);

        string[] shuffledOptions = new string[4];
        int shuffledCorrectIndex = 0;

        for (int i = 0; i < order.Count; i++)
        {
            int originalIndex = order[i];
            shuffledOptions[i] = source.options[originalIndex];
            if (originalIndex == source.correctIndex)
                shuffledCorrectIndex = i;
        }

        return new RuntimeQuestion
        {
            question = source.question,
            options = shuffledOptions,
            correctIndex = shuffledCorrectIndex
        };
    }

    private void ShowCurrentQuestion()
    {
        RuntimeQuestion q = activeQuestions[currentQuestionIndex];
        ui.SetQuestion(
            "Gelombang " + waves[currentWaveIndex].waveNumber,
            (currentQuestionIndex + 1) + "/" + activeQuestions.Count,
            q.question,
            q.options
        );
    }

    private void OnAnswerPicked(int pickedIndex)
    {
        if (!isOpen) return;
        ui.PlayPickSfx();
        ui.LockAnswers();

        RuntimeQuestion q = activeQuestions[currentQuestionIndex];
        if (pickedIndex == q.correctIndex) correctCount++;

        StartCoroutine(NextQuestionAfterDelay());
    }

    private IEnumerator NextQuestionAfterDelay()
    {
        yield return new WaitForSecondsRealtime(nextQuestionDelay);

        currentQuestionIndex++;
        if (currentQuestionIndex >= activeQuestions.Count)
        {
            CompleteWave();
            yield break;
        }

        ShowCurrentQuestion();
    }

    private void CompleteWave()
    {
        int total = activeQuestions.Count;
        int percent = Mathf.RoundToInt((correctCount / (float)total) * 100f);
        bool passed = percent >= passPercent;

        string message = passed
            ? "LULUS Gelombang " + waves[currentWaveIndex].waveNumber + "\nBenar: " + correctCount + "/" + total + " (" + percent + "%)"
            : "GAGAL Gelombang " + waves[currentWaveIndex].waveNumber + "\nBenar: " + correctCount + "/" + total + " (" + percent + "%)\nMinimal " + passPercent + "%";

        if (passed)
        {
            int passedWaveNumber = waves[currentWaveIndex].waveNumber;
            SaveWavePassed(passedWaveNumber);
            if (rewardUnlockManager != null)
                rewardUnlockManager.OnWavePassed(passedWaveNumber);

            ShowRewardResult(passedWaveNumber);

            if (AreAllWavesPassed())
            {
                message += "\nSemua gelombang lulus.";
                ui.ShowResult(message, false, false, true);
                return;
            }

            ui.ShowResult(message, false, true, true);
            return;
        }

        ui.ShowResult(message, true, false, false);
    }

    private void ShowRewardResult(int passedWaveNumber)
    {
        if (ui == null)
            return;

        if (rewardUnlockManager != null
            && rewardUnlockManager.TryGetRewardDisplayData(passedWaveNumber, out QuizRewardUnlockManager.RewardDisplayData rewardDisplay))
        {
            ui.ShowRewardInfo(
                rewardDisplay.rewardTitle,
                rewardDisplay.rewardDescription,
                rewardDisplay.rewardIcon
            );
            return;
        }

        ui.HideRewardInfo();
    }

    private void RestartCurrentWave()
    {
        StartWave(currentWaveIndex);
    }

    private void GoNextWave()
    {
        int nextIndex = currentWaveIndex + 1;
        if (nextIndex >= waves.Length)
        {
            CloseQuiz();
            return;
        }

        StartWave(nextIndex);
    }

    private void SaveWavePassed(int waveNumber)
    {
        if (waveNumber > 0)
            passedWaveNumbers.Add(waveNumber);
    }

    public bool IsWavePassed(int waveNumber)
    {
        return passedWaveNumbers.Contains(waveNumber);
    }

    private int GetFirstUnpassedWaveIndex()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i] != null && !IsWavePassed(waves[i].waveNumber))
                return i;
        }
        return 0;
    }

    private int FindWaveIndexByNumber(int waveNumber)
    {
        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i] != null && waves[i].waveNumber == waveNumber)
                return i;
        }

        return -1;
    }

    private bool AreAllWavesPassed()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i] == null) return false;
            if (!IsWavePassed(waves[i].waveNumber)) return false;
        }
        return true;
    }

    /// <summary>
    /// Mengembalikan true jika wave dengan nomor <paramref name="waveNumber"/> dapat dibuka
    /// oleh player. Wave 1 (index 0) selalu bisa diakses. Wave N hanya bisa diakses jika
    /// wave N-1 sudah diselesaikan.
    /// </summary>
    public bool IsWaveAccessible(int waveNumber)
    {
        if (waves == null || waves.Length == 0) return false;

        int targetIndex = FindWaveIndexByNumber(waveNumber);
        if (targetIndex < 0) return false;
        if (targetIndex == 0) return true;

        QuizWaveSO prevWave = waves[targetIndex - 1];
        return prevWave == null || IsWavePassed(prevWave.waveNumber);
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }

    /// <summary>
    /// Menghapus semua data progres quiz dari PlayerPrefs dan me-refresh state reward.
    /// Gunakan hanya untuk keperluan debugging/testing.
    /// </summary>
    [ContextMenu("DEBUG: Reset All Quiz Progress")]
    public void DebugResetAllProgress()
    {
        passedWaveNumbers.Clear();
        Debug.Log("[QuizManager] Semua progres quiz telah di-reset.");

        if (rewardUnlockManager != null)
            rewardUnlockManager.RefreshRewardsFromSave();
    }

    public QuizSaveData CaptureSaveData()
    {
        QuizSaveData data = new QuizSaveData();
        foreach (int waveNumber in passedWaveNumbers)
        {
            data.passedWaveNumbers.Add(waveNumber);
        }

        data.passedWaveNumbers.Sort();
        return data;
    }

    public void RestoreFromSaveData(QuizSaveData data)
    {
        passedWaveNumbers.Clear();
        if (data != null && data.passedWaveNumbers != null)
        {
            for (int i = 0; i < data.passedWaveNumbers.Count; i++)
            {
                if (data.passedWaveNumbers[i] > 0)
                    passedWaveNumbers.Add(data.passedWaveNumbers[i]);
            }
        }

        if (rewardUnlockManager != null)
            rewardUnlockManager.RefreshRewardsFromSave();
    }
}
