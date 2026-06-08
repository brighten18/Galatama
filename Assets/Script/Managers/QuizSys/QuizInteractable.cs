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

    /// <summary>
    /// Menyembunyikan prompt interaksi jika wave sebelumnya belum diselesaikan.
    /// Ini mencegah player melihat prompt untuk Wave 2/3 yang belum terbuka.
    /// </summary>
    public override void SetLookingAt(bool value)
    {
        if (value && !IsAccessible()) return;
        base.SetLookingAt(value);
    }

    /// <summary>
    /// Mengembalikan nama kosong saat wave belum bisa diakses sehingga
    /// InteractUIManager tidak menampilkan panel interaksi.
    /// </summary>
    public override string GetItemName()
    {
        return IsAccessible() ? displayName : string.Empty;
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

    private bool IsAccessible()
    {
        return quizManager != null && quizManager.IsWaveAccessible(targetWaveNumber);
    }
}
