using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pauseScreenUI;

    public bool IsPaused { get; private set; }

    private bool pausePressedLastFrame;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (pauseScreenUI != null)
            pauseScreenUI.SetActive(false);

        ResumeGame();
    }

    private void Update()
    {
        if (QuizSessionLock.IsLocked)
        {
            if (PlayerInputManager.Instance != null)
                PlayerInputManager.Instance.ResetPauseInput();

            pausePressedLastFrame = false;
            return;
        }

        bool pausePressed = PlayerInputManager.Instance != null && PlayerInputManager.Instance.Pause;

        if (PosterPopupManager.Instance != null && PosterPopupManager.Instance.IsOpen)
        {
            if (pausePressed && !pausePressedLastFrame)
            {
                PosterPopupManager.Instance.ClosePoster();
                PlayerInputManager.Instance.ResetPauseInput();
            }

            pausePressedLastFrame = pausePressed;
            return;
        }

        if (pausePressed && !pausePressedLastFrame)
        {
            TogglePause();
            PlayerInputManager.Instance.ResetPauseInput();
        }

        pausePressedLastFrame = pausePressed;
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseScreenUI != null)
            pauseScreenUI.SetActive(true);

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorAndLook(false, false);
            PlayerInputManager.Instance.SetPlayerMovement(false);
            PlayerInputManager.Instance.ResetInteractInput();
            PlayerInputManager.Instance.ResetInteractOBJInput();
            PlayerInputManager.Instance.ResetInventoryInput();
            PlayerInputManager.Instance.ResetAllQuickSlotInputs();
        }

        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseScreenUI != null)
            pauseScreenUI.SetActive(false);

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorAndLook(true, true);
            PlayerInputManager.Instance.SetPlayerMovement(true);
        }

        Cursor.visible = false;
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
        }
    }

}
