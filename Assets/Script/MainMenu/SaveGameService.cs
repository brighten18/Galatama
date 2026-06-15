using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GALATAMA.MainMenu
{
    [Serializable]
    public class SaveGameData
    {
        public int slotIndex;
        public string saveName;
        public bool hasSavedProgress;
        public long createdAtTicks;
        public long savedAtTicks;
        public string sceneName;
        public string summaryLocationName;
        public PlayerSaveData player = new PlayerSaveData();
        public InventorySaveData inventory = new InventorySaveData();
        public List<AquariumSaveData> aquariums = new List<AquariumSaveData>();
        public QuizSaveData quiz = new QuizSaveData();
        public List<CooldownEntrySaveData> cooldowns = new List<CooldownEntrySaveData>();
    }

    [Serializable]
    public class PlayerSaveData
    {
        public Vector3 position;
        public Vector3 rotationEuler;
    }

    [Serializable]
    public class InventorySaveData
    {
        public List<InventorySlotSaveData> mainSlots = new List<InventorySlotSaveData>();
        public List<InventorySlotSaveData> quickSlots = new List<InventorySlotSaveData>();
    }

    [Serializable]
    public class InventorySlotSaveData
    {
        public int slotIndex;
        public string itemName;
        public FishStateSaveData fishState;

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(itemName); }
        }
    }

    [Serializable]
    public class FishStateSaveData
    {
        public string instanceId;
        public string itemName;
        public Vector3 holdLocalPosition;
        public Vector3 holdLocalRotation;
        public Vector3 holdLocalScale = Vector3.one;
        public float hunger;
        public float maxHunger;
        public float health;
        public float maxHealth;
        public bool isAlive;
        public bool isStressed;

        public static FishStateSaveData FromRuntime(FishInstanceState state)
        {
            if (state == null)
                return null;

            FishInstanceState safeState = FishFactory.EnsureValid(state, state.itemName);
            return new FishStateSaveData
            {
                instanceId = safeState.instanceId,
                itemName = safeState.itemName,
                holdLocalPosition = safeState.holdLocalPosition,
                holdLocalRotation = safeState.holdLocalRotation,
                holdLocalScale = safeState.holdLocalScale,
                hunger = safeState.hunger,
                maxHunger = safeState.maxHunger,
                health = safeState.health,
                maxHealth = safeState.maxHealth,
                isAlive = safeState.isAlive,
                isStressed = safeState.isStressed
            };
        }

        public FishInstanceState ToRuntimeState()
        {
            FishInstanceState state = new FishInstanceState
            {
                instanceId = instanceId,
                itemName = itemName,
                holdLocalPosition = holdLocalPosition,
                holdLocalRotation = holdLocalRotation,
                holdLocalScale = holdLocalScale,
                hunger = hunger,
                maxHunger = maxHunger,
                health = health,
                maxHealth = maxHealth,
                isAlive = isAlive,
                isStressed = isStressed
            };

            return FishFactory.EnsureValid(state, itemName);
        }
    }

    [Serializable]
    public class AquariumSaveData
    {
        public string aquariumId;
        public List<FishStateSaveData> fish = new List<FishStateSaveData>();
        public WaterQualitySaveData waterQuality = new WaterQualitySaveData();
        public List<string> installedEquipmentItemNames = new List<string>();
    }

    [Serializable]
    public class WaterQualitySaveData
    {
        public float ammonia;
        public float oxygen;
        public float temperature;
        public float ph;
        public float salinity;

        public static WaterQualitySaveData FromRuntime(WaterQualityState state)
        {
            if (state == null)
                return new WaterQualitySaveData();

            return new WaterQualitySaveData
            {
                ammonia = state.ammonia,
                oxygen = state.oxygen,
                temperature = state.temperature,
                ph = state.ph,
                salinity = state.salinity
            };
        }

        public void ApplyTo(WaterQualityState state)
        {
            if (state == null)
                return;

            state.ammonia = ammonia;
            state.oxygen = oxygen;
            state.temperature = temperature;
            state.ph = ph;
            state.salinity = salinity;
            state.Clamp();
        }
    }

    [Serializable]
    public class QuizSaveData
    {
        public List<int> passedWaveNumbers = new List<int>();
    }

    [Serializable]
    public class CooldownEntrySaveData
    {
        public string cooldownKey;
        public long nextReadyAtUtcTicks;
    }

    [Serializable]
    public class SaveSlotHeaderData
    {
        public int slotIndex;
        public bool exists;
        public bool hasSavedProgress;
        public string saveName;
        public string sceneName;
        public string summaryLocationName;
        public long createdAtTicks;
        public long savedAtTicks;
    }

    public static class SaveGameService
    {
        public const int MaxSlots = 4;

        private const string SaveFilePrefix = "save_slot_";
        private const string SaveFileExtension = ".json";
        private const string ActiveSlotPrefKey = "save.activeSlot";
        private const string PendingSlotPrefKey = "save.pendingSlot";
        private const string PendingModePrefKey = "save.pendingMode";
        private const int PendingModeNone = 0;
        private const int PendingModeLoad = 1;
        private const int PendingModeNewGame = 2;

        public static string SaveDirectoryPath
        {
            get { return Application.persistentDataPath; }
        }

        public static bool HasAnySave()
        {
            for (int slotIndex = 1; slotIndex <= MaxSlots; slotIndex++)
            {
                if (SlotExists(slotIndex))
                    return true;
            }

            return false;
        }

        public static bool SlotExists(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
                return false;

            return File.Exists(GetSlotFilePath(slotIndex));
        }

        public static string GetSlotFilePath(int slotIndex)
        {
            return Path.Combine(SaveDirectoryPath, SaveFilePrefix + slotIndex + SaveFileExtension);
        }

        public static SaveSlotHeaderData GetSlotHeader(int slotIndex)
        {
            SaveSlotHeaderData header = new SaveSlotHeaderData
            {
                slotIndex = slotIndex,
                exists = false,
                hasSavedProgress = false,
                saveName = string.Empty,
                sceneName = string.Empty,
                summaryLocationName = string.Empty,
                createdAtTicks = 0L,
                savedAtTicks = 0L
            };

            if (!TryLoadSlot(slotIndex, out SaveGameData data))
                return header;

            header.exists = true;
            header.hasSavedProgress = data.hasSavedProgress;
            header.saveName = data.saveName;
            header.sceneName = data.sceneName;
            header.summaryLocationName = data.summaryLocationName;
            header.createdAtTicks = data.createdAtTicks;
            header.savedAtTicks = data.savedAtTicks;
            return header;
        }

        public static List<SaveSlotHeaderData> GetAllSlotHeaders()
        {
            List<SaveSlotHeaderData> headers = new List<SaveSlotHeaderData>(MaxSlots);
            for (int slotIndex = 1; slotIndex <= MaxSlots; slotIndex++)
            {
                headers.Add(GetSlotHeader(slotIndex));
            }

            return headers;
        }

        public static SaveGameData CreateNewSlotData(int slotIndex, string saveName, string initialSceneName)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            return new SaveGameData
            {
                slotIndex = slotIndex,
                saveName = string.IsNullOrWhiteSpace(saveName) ? "Save " + slotIndex : saveName.Trim(),
                hasSavedProgress = false,
                createdAtTicks = nowTicks,
                savedAtTicks = nowTicks,
                sceneName = initialSceneName,
                summaryLocationName = initialSceneName,
                player = new PlayerSaveData(),
                inventory = new InventorySaveData(),
                aquariums = new List<AquariumSaveData>(),
                quiz = new QuizSaveData(),
                cooldowns = new List<CooldownEntrySaveData>()
            };
        }

        public static void SaveSlot(SaveGameData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveGameService] Save gagal: data null.");
                return;
            }

            if (!IsValidSlotIndex(data.slotIndex))
            {
                Debug.LogError("[SaveGameService] Save gagal: slotIndex tidak valid.");
                return;
            }

            EnsureSaveDirectory();

            if (data.createdAtTicks <= 0L)
                data.createdAtTicks = DateTime.UtcNow.Ticks;

            data.savedAtTicks = DateTime.UtcNow.Ticks;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSlotFilePath(data.slotIndex), json);
            Debug.Log("[SaveGameService] Save slot " + data.slotIndex + " tersimpan di: " + GetSlotFilePath(data.slotIndex));
        }

        public static bool TryLoadSlot(int slotIndex, out SaveGameData data)
        {
            data = null;

            if (!IsValidSlotIndex(slotIndex) || !SlotExists(slotIndex))
                return false;

            string json = File.ReadAllText(GetSlotFilePath(slotIndex));
            if (string.IsNullOrWhiteSpace(json))
                return false;

            data = JsonUtility.FromJson<SaveGameData>(json);
            if (data == null)
                return false;

            data.slotIndex = slotIndex;
            if (data.player == null)
                data.player = new PlayerSaveData();
            if (data.inventory == null)
                data.inventory = new InventorySaveData();
            if (data.aquariums == null)
                data.aquariums = new List<AquariumSaveData>();
            if (data.quiz == null)
                data.quiz = new QuizSaveData();
            if (data.cooldowns == null)
                data.cooldowns = new List<CooldownEntrySaveData>();

            return true;
        }

        public static bool TryLoadActiveSlot(out SaveGameData data)
        {
            data = null;
            int activeSlot = GetActiveSlotIndex();
            return activeSlot > 0 && TryLoadSlot(activeSlot, out data);
        }

        public static void DeleteSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
                return;

            string slotPath = GetSlotFilePath(slotIndex);
            if (File.Exists(slotPath))
                File.Delete(slotPath);

            if (GetActiveSlotIndex() == slotIndex)
                ClearActiveSlot();

            if (GetPendingSlotIndex() == slotIndex)
                ClearPendingLoadRequest();
        }

        public static bool RenameSlot(int slotIndex, string newSaveName)
        {
            if (!TryLoadSlot(slotIndex, out SaveGameData data))
                return false;

            data.saveName = string.IsNullOrWhiteSpace(newSaveName) ? data.saveName : newSaveName.Trim();
            SaveSlot(data);
            return true;
        }

        public static void SetActiveSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
                return;

            PlayerPrefs.SetInt(ActiveSlotPrefKey, slotIndex);
            PlayerPrefs.Save();
        }

        public static int GetActiveSlotIndex()
        {
            int slotIndex = PlayerPrefs.GetInt(ActiveSlotPrefKey, 0);
            return IsValidSlotIndex(slotIndex) ? slotIndex : 0;
        }

        public static void ClearActiveSlot()
        {
            PlayerPrefs.DeleteKey(ActiveSlotPrefKey);
            PlayerPrefs.Save();
        }

        public static void PrepareLoadSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
                return;

            SetActiveSlot(slotIndex);
            SetPendingLoadRequest(slotIndex, PendingModeLoad);
        }

        public static void PrepareNewGameSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
                return;

            SetActiveSlot(slotIndex);
            SetPendingLoadRequest(slotIndex, PendingModeNewGame);
        }

        public static bool TryConsumePendingLoadRequest(out int slotIndex, out bool isNewGame)
        {
            slotIndex = GetPendingSlotIndex();
            int pendingMode = PlayerPrefs.GetInt(PendingModePrefKey, PendingModeNone);

            ClearPendingLoadRequest();

            if (!IsValidSlotIndex(slotIndex) || pendingMode == PendingModeNone)
            {
                slotIndex = 0;
                isNewGame = false;
                return false;
            }

            isNewGame = pendingMode == PendingModeNewGame;
            return true;
        }

        public static bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 1 && slotIndex <= MaxSlots;
        }

        private static void EnsureSaveDirectory()
        {
            if (!Directory.Exists(SaveDirectoryPath))
                Directory.CreateDirectory(SaveDirectoryPath);
        }

        private static int GetPendingSlotIndex()
        {
            int slotIndex = PlayerPrefs.GetInt(PendingSlotPrefKey, 0);
            return IsValidSlotIndex(slotIndex) ? slotIndex : 0;
        }

        private static void SetPendingLoadRequest(int slotIndex, int pendingMode)
        {
            PlayerPrefs.SetInt(PendingSlotPrefKey, slotIndex);
            PlayerPrefs.SetInt(PendingModePrefKey, pendingMode);
            PlayerPrefs.Save();
        }

        private static void ClearPendingLoadRequest()
        {
            PlayerPrefs.DeleteKey(PendingSlotPrefKey);
            PlayerPrefs.DeleteKey(PendingModePrefKey);
            PlayerPrefs.Save();
        }
    }
}
