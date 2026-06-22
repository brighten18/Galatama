using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GALATAMA.MainMenu
{
    public class SaveGameRuntimeController : MonoBehaviour
    {
        public static SaveGameRuntimeController Instance { get; private set; }

        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool applySaveOnStart = true;

        private bool initialStateApplied;

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
            if (applySaveOnStart)
                StartCoroutine(ApplyInitialStateRoutine());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool SaveActiveSlotFromScene()
        {
            int slotIndex = SaveGameService.GetActiveSlotIndex();
            if (!SaveGameService.IsValidSlotIndex(slotIndex))
            {
                Debug.LogWarning("[SaveGameRuntime] Tidak ada slot aktif untuk disimpan.");
                return false;
            }

            SaveGameData data;
            if (!SaveGameService.TryLoadSlot(slotIndex, out data))
            {
                data = SaveGameService.CreateNewSlotData(slotIndex, "Save " + slotIndex, SceneManager.GetActiveScene().name);
            }

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
            {
                Debug.LogError("[SaveGameRuntime] Player tidak ditemukan dengan tag '" + playerTag + "'.");
                return false;
            }

            data.slotIndex = slotIndex;
            data.hasSavedProgress = true;
            data.sceneName = SceneManager.GetActiveScene().name;
            data.summaryLocationName = data.sceneName;

            data.player = CapturePlayerData(player);
            data.inventory = InventorySystem.Instance != null ? InventorySystem.Instance.CaptureSaveData() : new InventorySaveData();
            data.aquariums = CaptureAquariumData();
            data.quiz = QuizManager.Instance != null ? QuizManager.Instance.CaptureSaveData() : new QuizSaveData();
            data.cooldowns = CaptureCooldownData();
            data.completedTutorials = TutorialManager.Instance != null
                ? TutorialManager.Instance.CaptureSaveData()
                : new List<string>();

            SaveGameService.SaveSlot(data);
            return true;
        }

        private IEnumerator ApplyInitialStateRoutine()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            if (initialStateApplied)
                yield break;

            initialStateApplied = true;

            if (!SaveGameService.TryConsumePendingLoadRequest(out int slotIndex, out bool isNewGame))
                yield break;

            if (!SaveGameService.TryLoadSlot(slotIndex, out SaveGameData data))
                yield break;

            if (isNewGame || !data.hasSavedProgress)
            {
                if (TutorialManager.Instance != null)
                    TutorialManager.Instance.ResetAllTutorials();

                if (QuizManager.Instance != null)
                    QuizManager.Instance.RestoreFromSaveData(new QuizSaveData());

                RestoreCooldownData(new List<CooldownEntrySaveData>());
                yield break;
            }

            ApplySaveData(data);
        }

        private void ApplySaveData(SaveGameData data)
        {
            if (data == null)
                return;

            RestorePlayerData(data.player);

            if (InventorySystem.Instance != null)
                InventorySystem.Instance.RestoreFromSaveData(data.inventory);

            RestoreAquariumData(data.aquariums);

            if (QuizManager.Instance != null)
                QuizManager.Instance.RestoreFromSaveData(data.quiz);

            RestoreCooldownData(data.cooldowns);

            if (TutorialManager.Instance != null)
                TutorialManager.Instance.RestoreFromSaveData(data.completedTutorials);
        }

        private PlayerSaveData CapturePlayerData(GameObject player)
        {
            return new PlayerSaveData
            {
                position = player.transform.position,
                rotationEuler = player.transform.eulerAngles
            };
        }

        private void RestorePlayerData(PlayerSaveData data)
        {
            if (data == null)
                return;

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
                return;

            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
                characterController.enabled = false;

            player.transform.position = data.position;
            player.transform.rotation = Quaternion.Euler(data.rotationEuler);

            StarterAssets.ThirdPersonController thirdPersonController = player.GetComponent<StarterAssets.ThirdPersonController>();
            if (thirdPersonController != null && thirdPersonController.CinemachineCameraTarget != null)
                thirdPersonController.CinemachineCameraTarget.transform.rotation = Quaternion.Euler(data.rotationEuler);

            if (characterController != null)
                characterController.enabled = true;
        }

        private List<AquariumSaveData> CaptureAquariumData()
        {
            List<AquariumSaveData> result = new List<AquariumSaveData>();
            AquariumSystem[] aquariums = FindObjectsByType<AquariumSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < aquariums.Length; i++)
            {
                if (aquariums[i] != null)
                    result.Add(aquariums[i].CaptureSaveData());
            }

            return result;
        }

        private void RestoreAquariumData(List<AquariumSaveData> saveData)
        {
            Dictionary<string, AquariumSaveData> aquariumMap = new Dictionary<string, AquariumSaveData>();
            if (saveData != null)
            {
                for (int i = 0; i < saveData.Count; i++)
                {
                    AquariumSaveData data = saveData[i];
                    if (data == null || string.IsNullOrEmpty(data.aquariumId))
                        continue;

                    aquariumMap[data.aquariumId] = data;
                }
            }

            AquariumSystem[] sceneAquariums = FindObjectsByType<AquariumSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneAquariums.Length; i++)
            {
                AquariumSystem aquarium = sceneAquariums[i];
                if (aquarium == null)
                    continue;

                aquariumMap.TryGetValue(aquarium.PersistentAquariumId, out AquariumSaveData aquariumData);
                aquarium.RestoreFromSaveData(aquariumData);
            }
        }

        private List<CooldownEntrySaveData> CaptureCooldownData()
        {
            List<CooldownEntrySaveData> result = new List<CooldownEntrySaveData>();
            AquariumActionCooldowns[] cooldownComponents = FindObjectsByType<AquariumActionCooldowns>(FindObjectsSortMode.None);
            for (int i = 0; i < cooldownComponents.Length; i++)
            {
                if (cooldownComponents[i] == null)
                    continue;

                result.AddRange(cooldownComponents[i].CaptureSaveData());
            }

            return result;
        }

        private void RestoreCooldownData(List<CooldownEntrySaveData> data)
        {
            AquariumActionCooldowns[] cooldownComponents = FindObjectsByType<AquariumActionCooldowns>(FindObjectsSortMode.None);
            for (int i = 0; i < cooldownComponents.Length; i++)
            {
                if (cooldownComponents[i] != null)
                    cooldownComponents[i].RestoreSaveData(data);
            }
        }
    }
}
