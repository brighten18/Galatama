using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GALATAMA.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject newGameSlotsPanel;
        [SerializeField] private GameObject loadGameSlotsPanel;

        [Header("Slot Lists")]
        [SerializeField] private SaveSlotButtonUI[] newGameSlotButtons;
        [SerializeField] private SaveSlotButtonUI[] loadGameSlotButtons;
        [SerializeField] private Button backFromNewGameButton;
        [SerializeField] private Button backFromLoadGameButton;

        [Header("Create Save Popup")]
        [SerializeField] private GameObject createSavePopup;
        [SerializeField] private Text createSaveTitleText;
        [SerializeField] private InputField createSaveNameInput;
        [SerializeField] private Button createSaveConfirmButton;
        [SerializeField] private Button createSaveCancelButton;

        [Header("Slot Action Popup")]
        [SerializeField] private GameObject slotActionPopup;
        [SerializeField] private Text slotActionTitleText;
        [SerializeField] private Button slotActionLoadButton;
        [SerializeField] private Button slotActionNewGameButton;
        [SerializeField] private Button slotActionRenameButton;
        [SerializeField] private Button slotActionDeleteButton;
        [SerializeField] private Button slotActionCancelButton;

        [Header("Rename Popup")]
        [SerializeField] private GameObject renamePopup;
        [SerializeField] private InputField renameInputField;
        [SerializeField] private Button renameConfirmButton;
        [SerializeField] private Button renameCancelButton;

        [Header("Confirm Popup")]
        [SerializeField] private GameObject confirmPopup;
        [SerializeField] private Text confirmMessageText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "Galatama";

        private int selectedSlotIndex;
        private System.Action pendingConfirmAction;

        private enum SlotScreenMode
        {
            None,
            NewGame,
            LoadGame
        }

        private SlotScreenMode currentSlotScreenMode = SlotScreenMode.None;

        private void Awake()
        {
            RegisterButtonListeners();
            ShowOnlyPanel(mainMenuPanel);
            CloseAllPopups();

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnEnable()
        {
            RefreshAllSlotViews();
            ApplyLoadButtonState();
        }

        private void OnDestroy()
        {
            UnregisterButtonListeners();
        }

        public void OnStartClicked()
        {
            currentSlotScreenMode = SlotScreenMode.NewGame;
            RefreshNewGameSlots();
            ShowOnlyPanel(newGameSlotsPanel);
        }

        public void OnLoadGameClicked()
        {
            currentSlotScreenMode = SlotScreenMode.LoadGame;
            RefreshLoadGameSlots();
            ShowOnlyPanel(loadGameSlotsPanel);
        }

        public void OnSettingsClicked()
        {
            ShowOnlyPanel(settingsPanel);
        }

        public void CloseSettingsPanel()
        {
            ShowOnlyPanel(mainMenuPanel);
        }

        public void BackToMainMenu()
        {
            currentSlotScreenMode = SlotScreenMode.None;
            CloseAllPopups();
            ShowOnlyPanel(mainMenuPanel);
            ApplyLoadButtonState();
        }

        public void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleNewGameSlotClicked(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
            SaveSlotHeaderData header = SaveGameService.GetSlotHeader(slotIndex);

            if (header.exists)
            {
                OpenConfirmPopup(
                    "Slot ini sudah memiliki progress. Mulai game baru dan ganti progress lama?",
                    OpenCreateSavePopupForSelectedSlot);
                return;
            }

            OpenCreateSavePopupForSelectedSlot();
        }

        private void HandleLoadSlotClicked(int slotIndex)
        {
            SaveSlotHeaderData header = SaveGameService.GetSlotHeader(slotIndex);
            if (!header.exists)
                return;

            selectedSlotIndex = slotIndex;
            if (slotActionTitleText != null)
                slotActionTitleText.text = string.IsNullOrWhiteSpace(header.saveName) ? "Slot " + slotIndex : header.saveName;

            if (slotActionPopup != null)
                slotActionPopup.SetActive(true);
        }

        private void OpenCreateSavePopupForSelectedSlot()
        {
            SaveSlotHeaderData header = SaveGameService.GetSlotHeader(selectedSlotIndex);
            if (createSaveTitleText != null)
                createSaveTitleText.text = "Buat Save Baru - Slot " + selectedSlotIndex;

            if (createSaveNameInput != null)
            {
                string defaultName = header.exists && !string.IsNullOrWhiteSpace(header.saveName)
                    ? header.saveName
                    : "Save " + selectedSlotIndex;
                createSaveNameInput.text = defaultName;
                createSaveNameInput.Select();
                createSaveNameInput.ActivateInputField();
            }

            if (createSavePopup != null)
                createSavePopup.SetActive(true);
        }

        private void ConfirmCreateSave()
        {
            string saveName = createSaveNameInput != null ? createSaveNameInput.text : string.Empty;
            SaveGameData data = SaveGameService.CreateNewSlotData(selectedSlotIndex, saveName, gameplaySceneName);
            SaveGameService.SaveSlot(data);
            SaveGameService.PrepareNewGameSlot(selectedSlotIndex);
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void ConfirmLoadSelectedSlot()
        {
            if (!SaveGameService.TryLoadSlot(selectedSlotIndex, out SaveGameData data))
                return;

            SaveGameService.PrepareLoadSlot(selectedSlotIndex);
            string sceneToLoad = string.IsNullOrWhiteSpace(data.sceneName) ? gameplaySceneName : data.sceneName;
            SceneManager.LoadScene(sceneToLoad);
        }

        private void OpenRenamePopup()
        {
            SaveSlotHeaderData header = SaveGameService.GetSlotHeader(selectedSlotIndex);
            if (!header.exists)
                return;

            if (renameInputField != null)
            {
                renameInputField.text = string.IsNullOrWhiteSpace(header.saveName) ? "Save " + selectedSlotIndex : header.saveName;
                renameInputField.Select();
                renameInputField.ActivateInputField();
            }

            if (renamePopup != null)
                renamePopup.SetActive(true);
        }

        private void ConfirmRenameSlot()
        {
            string newName = renameInputField != null ? renameInputField.text : string.Empty;
            SaveGameService.RenameSlot(selectedSlotIndex, newName);
            if (renamePopup != null)
                renamePopup.SetActive(false);

            if (slotActionPopup != null)
                slotActionPopup.SetActive(false);

            RefreshAllSlotViews();
        }

        private void RequestDeleteSelectedSlot()
        {
            OpenConfirmPopup("Yakin ingin menghapus progress ini?", DeleteSelectedSlot);
        }

        private void DeleteSelectedSlot()
        {
            SaveGameService.DeleteSlot(selectedSlotIndex);
            if (slotActionPopup != null)
                slotActionPopup.SetActive(false);

            RefreshAllSlotViews();
            ApplyLoadButtonState();
        }

        private void OpenConfirmPopup(string message, System.Action onYes)
        {
            pendingConfirmAction = onYes;

            if (confirmMessageText != null)
                confirmMessageText.text = message;

            if (confirmPopup != null)
                confirmPopup.SetActive(true);
        }

        private void ConfirmYes()
        {
            System.Action action = pendingConfirmAction;
            pendingConfirmAction = null;

            if (confirmPopup != null)
                confirmPopup.SetActive(false);

            action?.Invoke();
        }

        private void ConfirmNo()
        {
            pendingConfirmAction = null;
            if (confirmPopup != null)
                confirmPopup.SetActive(false);
        }

        private void RefreshAllSlotViews()
        {
            RefreshNewGameSlots();
            RefreshLoadGameSlots();
        }

        private void RefreshNewGameSlots()
        {
            RefreshSlotButtonArray(newGameSlotButtons, true);
        }

        private void RefreshLoadGameSlots()
        {
            RefreshSlotButtonArray(loadGameSlotButtons, false);
        }

        private void RefreshSlotButtonArray(SaveSlotButtonUI[] buttonArray, bool allowEmptyClick)
        {
            if (buttonArray == null)
                return;

            for (int i = 0; i < buttonArray.Length; i++)
            {
                SaveSlotButtonUI slotButton = buttonArray[i];
                if (slotButton == null)
                    continue;

                int slotIndex = i + 1;
                SaveSlotHeaderData header = SaveGameService.GetSlotHeader(slotIndex);
                slotButton.SetVisual(slotIndex, header);
                slotButton.SetInteractable(allowEmptyClick || header.exists);
            }
        }

        private void ApplyLoadButtonState()
        {
            if (loadGameButton != null)
                loadGameButton.interactable = SaveGameService.HasAnySave();
        }

        private void ShowOnlyPanel(GameObject targetPanel)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(targetPanel == mainMenuPanel);
            if (settingsPanel != null) settingsPanel.SetActive(targetPanel == settingsPanel);
            if (newGameSlotsPanel != null) newGameSlotsPanel.SetActive(targetPanel == newGameSlotsPanel);
            if (loadGameSlotsPanel != null) loadGameSlotsPanel.SetActive(targetPanel == loadGameSlotsPanel);
        }

        private void CloseAllPopups()
        {
            if (createSavePopup != null) createSavePopup.SetActive(false);
            if (slotActionPopup != null) slotActionPopup.SetActive(false);
            if (renamePopup != null) renamePopup.SetActive(false);
            if (confirmPopup != null) confirmPopup.SetActive(false);
            pendingConfirmAction = null;
        }

        private void RegisterButtonListeners()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (loadGameButton != null) loadGameButton.onClick.AddListener(OnLoadGameClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            RegisterSlotButtons(newGameSlotButtons, HandleNewGameSlotClicked);
            RegisterSlotButtons(loadGameSlotButtons, HandleLoadSlotClicked);

            if (backFromNewGameButton != null) backFromNewGameButton.onClick.AddListener(BackToMainMenu);
            if (backFromLoadGameButton != null) backFromLoadGameButton.onClick.AddListener(BackToMainMenu);

            if (createSaveConfirmButton != null) createSaveConfirmButton.onClick.AddListener(ConfirmCreateSave);
            if (createSaveCancelButton != null) createSaveCancelButton.onClick.AddListener(() =>
            {
                if (createSavePopup != null)
                    createSavePopup.SetActive(false);
            });

            if (slotActionLoadButton != null) slotActionLoadButton.onClick.AddListener(ConfirmLoadSelectedSlot);
            if (slotActionNewGameButton != null) slotActionNewGameButton.onClick.AddListener(() =>
            {
                if (slotActionPopup != null)
                    slotActionPopup.SetActive(false);
                OpenConfirmPopup("Progress lama pada slot ini akan diganti. Lanjut?", OpenCreateSavePopupForSelectedSlot);
            });
            if (slotActionRenameButton != null) slotActionRenameButton.onClick.AddListener(OpenRenamePopup);
            if (slotActionDeleteButton != null) slotActionDeleteButton.onClick.AddListener(RequestDeleteSelectedSlot);
            if (slotActionCancelButton != null) slotActionCancelButton.onClick.AddListener(() =>
            {
                if (slotActionPopup != null)
                    slotActionPopup.SetActive(false);
            });

            if (renameConfirmButton != null) renameConfirmButton.onClick.AddListener(ConfirmRenameSlot);
            if (renameCancelButton != null) renameCancelButton.onClick.AddListener(() =>
            {
                if (renamePopup != null)
                    renamePopup.SetActive(false);
            });

            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(ConfirmYes);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(ConfirmNo);
        }

        private void UnregisterButtonListeners()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
            if (loadGameButton != null) loadGameButton.onClick.RemoveListener(OnLoadGameClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);

            UnregisterSlotButtons(newGameSlotButtons);
            UnregisterSlotButtons(loadGameSlotButtons);
        }

        private void RegisterSlotButtons(SaveSlotButtonUI[] buttonArray, System.Action<int> onClick)
        {
            if (buttonArray == null)
                return;

            for (int i = 0; i < buttonArray.Length; i++)
            {
                SaveSlotButtonUI slotButton = buttonArray[i];
                if (slotButton == null || slotButton.Button == null)
                    continue;

                int slotIndex = i + 1;
                slotButton.Button.onClick.AddListener(() => onClick(slotIndex));
            }
        }

        private void UnregisterSlotButtons(SaveSlotButtonUI[] buttonArray)
        {
            if (buttonArray == null)
                return;

            for (int i = 0; i < buttonArray.Length; i++)
            {
                SaveSlotButtonUI slotButton = buttonArray[i];
                if (slotButton != null && slotButton.Button != null)
                    slotButton.Button.onClick.RemoveAllListeners();
            }
        }
    }
}
