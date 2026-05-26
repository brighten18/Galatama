using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GALATAMA.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject MainMenuPanel;

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        [Header("New Game")]
        [SerializeField] private bool deleteExistingSaveOnNewGame = false;

        private void Awake()
        {
            RegisterButtonListeners();
            ApplyLoadButtonState();

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (MainMenuPanel != null)
            {
                MainMenuPanel.SetActive(true);
            }
        }

        private void OnEnable()
        {
            ApplyLoadButtonState();
        }

        private void OnDestroy()
        {
            UnregisterButtonListeners();
        }

        private void RegisterButtonListeners()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (loadGameButton != null) loadGameButton.onClick.AddListener(OnLoadGameClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        }

        private void UnregisterButtonListeners()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGameClicked);
            if (loadGameButton != null) loadGameButton.onClick.RemoveListener(OnLoadGameClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
        }

        public void OnNewGameClicked()
        {
            if (deleteExistingSaveOnNewGame)
            {
                SaveGameService.DeleteSave();
            }

            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OnLoadGameClicked()
        {
            if (!SaveGameService.TryLoad(out SaveGameData data))
            {
                Debug.LogWarning("Tidak ada save untuk dimuat.");
                ApplyLoadButtonState();
                return;
            }

            // Untuk versi awal, kita load scene dari data save.
            string sceneToLoad = string.IsNullOrWhiteSpace(data.sceneName) ? gameplaySceneName : data.sceneName;
            SceneManager.LoadScene(sceneToLoad);
        }

        public void OnSettingsClicked()
        {
            if (settingsPanel == null)
            {
                return;
            }

            settingsPanel.SetActive(true);
            MainMenuPanel.SetActive(false);
        }

        public void CloseSettingsPanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                MainMenuPanel.SetActive(true);
            }
        }

        public void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ApplyLoadButtonState()
        {
            if (loadGameButton == null)
            {
                return;
            }

            loadGameButton.interactable = SaveGameService.HasSave();
        }
    }
}
