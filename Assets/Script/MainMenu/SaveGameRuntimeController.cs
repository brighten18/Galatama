using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GALATAMA.MainMenu
{
    public class SaveGameRuntimeController : MonoBehaviour
    {
        public static SaveGameRuntimeController Instance { get; private set; }
        private const string RuntimeObjectName = "__SaveGameRuntime";
        private const float SceneReadyTimeoutSeconds = 5f;
        private const string TrapWorldPrefabResourcePath = "Perangkap_World";

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

        public static SaveGameRuntimeController GetOrCreateInstance()
        {
            if (Instance != null)
                return Instance;

            SaveGameRuntimeController existing = FindFirstObjectByType<SaveGameRuntimeController>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            return runtimeObject.AddComponent<SaveGameRuntimeController>();
        }

        public static SaveGameRuntimeController GetOrCreateInstanceForSaving()
        {
            SaveGameRuntimeController controller = GetOrCreateInstance();
            if (controller != null)
                controller.DisableAutomaticInitialLoad();

            return controller;
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

        public void DisableAutomaticInitialLoad()
        {
            applySaveOnStart = false;
            initialStateApplied = true;
            StopAllCoroutines();
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
            data.traps = CaptureTrapData();
            data.quiz = QuizManager.Instance != null ? QuizManager.Instance.CaptureSaveData() : new QuizSaveData();
            data.cooldowns = CaptureCooldownData();
            data.completedTutorials = TutorialManager.Instance != null
                ? TutorialManager.Instance.CaptureSaveData()
                : new List<string>();
            data.mission = MissionManager.Instance != null ? MissionManager.Instance.CaptureSaveData() : new MissionSaveData();
            data.monologue = MonologueManager.Instance != null ? MonologueManager.Instance.CaptureSaveData() : new MonologueSaveData();

            SaveGameService.SaveSlot(data);
            return true;
        }

        private IEnumerator ApplyInitialStateRoutine()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            if (initialStateApplied)
                yield break;

            if (!SaveGameService.TryPeekPendingLoadRequest(out int slotIndex, out bool isNewGame))
                yield break;

            if (!SaveGameService.TryLoadSlot(slotIndex, out SaveGameData data))
            {
                SaveGameService.ClearPendingLoadRequestPublic();
                yield break;
            }

            yield return StartCoroutine(WaitForSceneReadyRoutine(data));
            if (!IsSceneReadyForApply(data))
                yield break;

            initialStateApplied = true;

            if (isNewGame || !data.hasSavedProgress)
            {
                if (TutorialManager.Instance != null)
                    TutorialManager.Instance.ResetAllTutorials();

                if (MissionManager.Instance != null)
                    MissionManager.Instance.RestoreFromSaveData(new MissionSaveData());

                if (QuizManager.Instance != null)
                    QuizManager.Instance.RestoreFromSaveData(new QuizSaveData());

                RestoreCooldownData(new List<CooldownEntrySaveData>());
                SaveGameService.ClearPendingLoadRequestPublic();
                yield break;
            }

            ApplySaveData(data);
            SaveGameService.ClearPendingLoadRequestPublic();
        }

        private void ApplySaveData(SaveGameData data)
        {
            if (data == null)
                return;

            RestorePlayerData(data.player);

            if (InventorySystem.Instance != null)
                InventorySystem.Instance.RestoreFromSaveData(data.inventory);

            RestoreAquariumData(data.aquariums);
            RestoreTrapData(data.traps);

            if (MissionManager.Instance != null)
                MissionManager.Instance.RestoreFromSaveData(data.mission);

            if (QuizManager.Instance != null)
                QuizManager.Instance.RestoreFromSaveData(data.quiz);

            RestoreCooldownData(data.cooldowns);

            if (TutorialManager.Instance != null)
                TutorialManager.Instance.RestoreFromSaveData(data.completedTutorials);

            if (MonologueManager.Instance != null)
                MonologueManager.Instance.RestoreFromSaveData(data.monologue);
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
            List<AquariumSaveData> unmatchedAquariumData = new List<AquariumSaveData>();
            if (saveData != null)
            {
                for (int i = 0; i < saveData.Count; i++)
                {
                    AquariumSaveData data = saveData[i];
                    if (data == null || string.IsNullOrEmpty(data.aquariumId))
                        continue;

                    aquariumMap[data.aquariumId] = data;
                    unmatchedAquariumData.Add(data);
                }
            }

            AquariumSystem[] sceneAquariums = FindObjectsByType<AquariumSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneAquariums.Length; i++)
            {
                AquariumSystem aquarium = sceneAquariums[i];
                if (aquarium == null)
                    continue;

                AquariumSaveData aquariumData = null;
                if (aquariumMap.TryGetValue(aquarium.PersistentAquariumId, out aquariumData))
                {
                    unmatchedAquariumData.Remove(aquariumData);
                }
                else if (unmatchedAquariumData.Count > 0)
                {
                    aquariumData = unmatchedAquariumData[0];
                    unmatchedAquariumData.RemoveAt(0);
                    Debug.LogWarning("[SaveGameRuntime] Aquarium ID tidak cocok. Fallback restore berdasarkan urutan untuk aquarium: " + aquarium.PersistentAquariumId);
                }

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

        private List<TrapSaveData> CaptureTrapData()
        {
            List<TrapSaveData> result = new List<TrapSaveData>();
            FishTrapWorld[] traps = FindObjectsByType<FishTrapWorld>(FindObjectsSortMode.None);
            for (int i = 0; i < traps.Length; i++)
            {
                FishTrapWorld trap = traps[i];
                if (trap == null)
                    continue;

                TrapSaveData trapData = trap.CaptureSaveData();
                if (trapData != null)
                    result.Add(trapData);
            }

            return result;
        }

        private void RestoreTrapData(List<TrapSaveData> saveData)
        {
            FishTrapWorld[] existingTraps = FindObjectsByType<FishTrapWorld>(FindObjectsSortMode.None);
            for (int i = 0; i < existingTraps.Length; i++)
            {
                if (existingTraps[i] != null)
                    Destroy(existingTraps[i].gameObject);
            }

            if (saveData == null || saveData.Count == 0)
                return;

            GameObject trapPrefab = Resources.Load<GameObject>(TrapWorldPrefabResourcePath);
            if (trapPrefab == null)
            {
                Debug.LogError("[SaveGameRuntime] Prefab trap world tidak ditemukan di Resources: " + TrapWorldPrefabResourcePath);
                return;
            }

            for (int i = 0; i < saveData.Count; i++)
            {
                TrapSaveData trapData = saveData[i];
                if (trapData == null)
                    continue;

                GameObject trapObject = Instantiate(
                    trapPrefab,
                    trapData.position,
                    Quaternion.Euler(trapData.rotationEuler));

                FishTrapWorld trapWorld = trapObject.GetComponent<FishTrapWorld>();
                if (trapWorld == null)
                {
                    Debug.LogWarning("[SaveGameRuntime] Prefab trap world tidak memiliki FishTrapWorld.");
                    Destroy(trapObject);
                    continue;
                }

                trapWorld.RestoreFromSaveData(trapData);
            }
        }

        private IEnumerator WaitForSceneReadyRoutine(SaveGameData data)
        {
            float elapsed = 0f;

            while (elapsed < SceneReadyTimeoutSeconds)
            {
                if (IsSceneReadyForApply(data))
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.LogWarning("[SaveGameRuntime] Scene belum siap untuk restore dalam batas waktu. Pending load dibiarkan tetap aktif.");
        }

        private bool IsSceneReadyForApply(SaveGameData data)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
                return false;

            if (data != null && data.inventory != null)
            {
                bool hasInventoryData =
                    (data.inventory.mainSlots != null && data.inventory.mainSlots.Count > 0) ||
                    (data.inventory.quickSlots != null && data.inventory.quickSlots.Count > 0);

                if (hasInventoryData && InventorySystem.Instance == null)
                    return false;
            }

            if (data != null && data.aquariums != null && data.aquariums.Count > 0)
            {
                AquariumSystem[] aquariums = FindObjectsByType<AquariumSystem>(FindObjectsSortMode.None);
                if (aquariums == null || aquariums.Length == 0)
                    return false;
            }

            return true;
        }
    }
}
