using UnityEngine;

public class QuizInteractable : InteractableObject
{
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private string displayName = "Quiz";
    [SerializeField, Min(1)] private int targetWaveNumber = 1;

    private void Awake()
    {
        base.Awake();
        itemName = displayName;
    }

    protected override void Update()
    {
        if (quizManager != null && quizManager.IsOpen) return;
        base.Update();
    }

    protected override void HandleInteract()
    {
        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.ResetInteractInput();

        if (quizManager == null)
        {
            Debug.LogError("[QuizInteractable] QuizManager belum di-assign.");
            return;
        }

        quizManager.OpenQuizFromWave(targetWaveNumber);
    }
}
