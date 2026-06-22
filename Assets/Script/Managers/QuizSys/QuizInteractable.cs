using UnityEngine;

public class QuizInteractable : InteractableObject
{
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private string displayName = "Quiz";
    [SerializeField, Min(1)] private int targetWaveNumber = 1;

    [Header("Popup UI")]
    [Tooltip("Anak GameObject 'PopUPQuizNPC' — ditampilkan saat quiz belum selesai, disembunyikan saat lulus.")]
    [SerializeField] private GameObject popupQuizNPC;

    private bool isExplicitlyLocked = false;

    private void Awake()
    {
        base.Awake();
        itemName = displayName;
    }

    private void Start()
    {
        if (quizManager != null)
            quizManager.OnWavePassed += OnWavePassed;

        RefreshPopup();
    }

    private void OnDestroy()
    {
        if (quizManager != null)
            quizManager.OnWavePassed -= OnWavePassed;
    }

    protected override void Update()
    {
        if (quizManager != null && quizManager.IsOpen) return;
        base.Update();
    }

    /// <summary>
    /// Menyembunyikan prompt interaksi jika NPC terkunci atau wave belum bisa diakses.
    /// </summary>
    public override void SetLookingAt(bool value)
    {
        if (value && !IsAccessible()) return;
        base.SetLookingAt(value);
    }

    /// <summary>
    /// Mengembalikan nama kosong saat NPC tidak bisa diakses sehingga
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

    /// <summary>Mengunci NPC ini sepenuhnya. Popup juga disembunyikan selama terkunci.</summary>
    public void SetLocked(bool locked)
    {
        isExplicitlyLocked = locked;
        RefreshPopup();
    }

    /// <summary>Mengembalikan wave number target saat ini.</summary>
    public int GetTargetWaveNumber() => targetWaveNumber;

    /// <summary>Mengubah wave number target secara runtime. Digunakan oleh Mission8QuizTracker.</summary>
    public void SetTargetWaveNumber(int waveNumber)
    {
        targetWaveNumber = waveNumber;
        RefreshPopup();
    }

    private bool IsAccessible()
    {
        if (isExplicitlyLocked) return false;
        if (quizManager == null) return false;
        return true;
    }

    /// <summary>Dipanggil oleh QuizManager saat sebuah wave lulus.</summary>
    private void OnWavePassed(int waveNumber)
    {
        if (waveNumber == targetWaveNumber)
            RefreshPopup();
    }

    /// <summary>
    /// Mengatur visibilitas PopUPQuizNPC:
    /// - Tampil  → NPC tidak terkunci DAN wave belum pernah lulus.
    /// - Sembunyi → NPC terkunci ATAU wave sudah lulus.
    /// </summary>
    private void RefreshPopup()
    {
        if (popupQuizNPC == null || quizManager == null) return;

        bool wavePassed = quizManager.IsWavePassed(targetWaveNumber);
        popupQuizNPC.SetActive(!isExplicitlyLocked && !wavePassed);
    }
}
