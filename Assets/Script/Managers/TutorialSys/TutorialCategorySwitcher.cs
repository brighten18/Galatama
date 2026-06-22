using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Switches between tutorial categories (e.g., Movement, Aquarium) using arrow buttons
/// placed next to the tutorial panel's header. Each category plays its own TutorialSequenceSO
/// through the shared TutorialManager.
/// </summary>
public class TutorialCategorySwitcher : MonoBehaviour
{
    public static TutorialCategorySwitcher Instance { get; private set; }

    [Serializable]
    public struct TutorialCategory
    {
        [Tooltip("Nama kategori yang ditampilkan di indikator.")]
        public string categoryName;

        [Tooltip("TutorialSequenceSO yang akan dimainkan untuk kategori ini.")]
        public TutorialSequenceSO tutorial;
    }

    [Header("Categories")]
    [SerializeField] private TutorialCategory[] categories;

    [Header("Category Navigation Buttons")]
    [SerializeField] private Button nextCategoryButton;
    [SerializeField] private Button previousCategoryButton;

    [Header("Indicator (Optional)")]
    [Tooltip("Text yang menampilkan indeks kategori aktif, misal '1/2'.")]
    [SerializeField] private Text categoryIndicatorText;

    private int _currentIndex;
    private bool _navigationEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (nextCategoryButton != null)
            nextCategoryButton.onClick.AddListener(NextCategory);

        if (previousCategoryButton != null)
            previousCategoryButton.onClick.AddListener(PreviousCategory);

        UpdateIndicator();
        UpdateButtonStates();
    }

    private void OnDestroy()
    {
        if (nextCategoryButton != null)
            nextCategoryButton.onClick.RemoveListener(NextCategory);

        if (previousCategoryButton != null)
            previousCategoryButton.onClick.RemoveListener(PreviousCategory);

        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Aktifkan atau nonaktifkan tombol navigasi kategori.
    /// Gunakan false saat tutorial dimunculkan otomatis tanpa perlu ganti kategori.
    /// </summary>
    public void SetNavigationEnabled(bool enabled)
    {
        _navigationEnabled = enabled;
        UpdateButtonStates();
    }

    /// <summary>
    /// Sinkronisasi indeks aktif ke kategori yang sesuai dengan tutorial yang sedang dimainkan.
    /// Panggil ini dari luar jika tutorial dimulai dari sistem lain (misal OpeningSequencer).
    /// </summary>
    public void SyncToTutorial(TutorialSequenceSO tutorial)
    {
        if (categories == null) return;

        for (int i = 0; i < categories.Length; i++)
        {
            if (categories[i].tutorial == tutorial)
            {
                _currentIndex = i;
                UpdateIndicator();
                UpdateButtonStates();
                return;
            }
        }
    }

    private void NextCategory()
    {
        if (categories == null || categories.Length == 0) return;
        SyncToCurrentlyPlayingTutorial();
        _currentIndex = (_currentIndex + 1) % categories.Length;
        SwitchToCurrentCategory();
    }

    private void PreviousCategory()
    {
        if (categories == null || categories.Length == 0) return;
        SyncToCurrentlyPlayingTutorial();
        _currentIndex = (_currentIndex - 1 + categories.Length) % categories.Length;
        SwitchToCurrentCategory();
    }

    /// <summary>
    /// Sinkronisasi _currentIndex ke tutorial yang sedang aktif di TutorialManager,
    /// agar navigasi kategori selalu relatif terhadap state yang sesungguhnya.
    /// </summary>
    private void SyncToCurrentlyPlayingTutorial()
    {
        if (TutorialManager.Instance == null || TutorialManager.Instance.CurrentTutorial == null) return;
        SyncToTutorial(TutorialManager.Instance.CurrentTutorial);
    }

    private void SwitchToCurrentCategory()
    {
        if (TutorialManager.Instance == null) return;

        TutorialManager.Instance.CloseCurrentTutorial(suppressFinishedEvent: true);
        TutorialManager.Instance.TryPlayTutorial(categories[_currentIndex].tutorial, ignorePlayOnce: true);

        UpdateIndicator();
        UpdateButtonStates();
    }

    private void UpdateIndicator()
    {
        if (categoryIndicatorText == null || categories == null || categories.Length == 0) return;
        categoryIndicatorText.text = $"{_currentIndex + 1}/{categories.Length}";
    }

    private void UpdateButtonStates()
    {
        bool canInteract = _navigationEnabled && categories != null && categories.Length > 1;
        if (nextCategoryButton != null)
            nextCategoryButton.interactable = canInteract;
        if (previousCategoryButton != null)
            previousCategoryButton.interactable = canInteract;
    }
}
