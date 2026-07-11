using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GALATAMA.MainMenu;

[Serializable]
public class FishInstanceState
{
    public string instanceId;
    public string itemName;
    public GameObject holdPrefab;
    public Vector3 holdLocalPosition;
    public Vector3 holdLocalRotation;
    public Vector3 holdLocalScale = Vector3.one;
    public float hunger;
    public float maxHunger;
    public float health;
    public float maxHealth;
    public bool isAlive;
    public bool isStressed;

    public float HungerPercent => maxHunger <= 0f ? 0f : Mathf.Clamp01(hunger / maxHunger);
    public float HealthPercent => maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
}

public static class FishFactory
{
    public static FishInstanceState CreateFromWildFish(string itemName, AI_Fish_Data speciesData = null)
    {
        itemName = ItemNameUtility.CleanName(itemName);
        float maxHealth = speciesData != null ? Mathf.Max(1f, speciesData.baseHealth) : 100f;
        return new FishInstanceState
        {
            instanceId = Guid.NewGuid().ToString("N"),
            itemName = itemName,
            holdPrefab = speciesData != null ? speciesData.holdPrefab : null,
            holdLocalPosition = speciesData != null ? speciesData.holdLocalPosition : Vector3.zero,
            holdLocalRotation = speciesData != null ? speciesData.holdLocalRotation : Vector3.zero,
            holdLocalScale = speciesData != null ? speciesData.holdLocalScale : Vector3.one,
            hunger = 100f,
            maxHunger = 100f,
            health = maxHealth,
            maxHealth = maxHealth,
            isAlive = true,
            isStressed = false
        };
    }

    public static FishInstanceState EnsureValid(FishInstanceState state, string fallbackItemName)
    {
        if (state == null)
            return CreateFromWildFish(fallbackItemName);

        state.itemName = ItemNameUtility.CleanName(string.IsNullOrEmpty(state.itemName) ? fallbackItemName : state.itemName);
        if (string.IsNullOrEmpty(state.instanceId))
            state.instanceId = Guid.NewGuid().ToString("N");

        state.maxHunger = Mathf.Max(1f, state.maxHunger);
        state.maxHealth = Mathf.Max(1f, state.maxHealth);
        if (ApproximatelyZero(state.holdLocalScale))
            state.holdLocalScale = Vector3.one;
        state.hunger = Mathf.Clamp(state.hunger, 0f, state.maxHunger);
        if (state.health <= 0f && state.isAlive)
            state.health = state.maxHealth;
        state.health = Mathf.Clamp(state.health, 0f, state.maxHealth);
        state.isAlive = state.isAlive && state.health > 0f;
        return state;
    }

    private static bool ApproximatelyZero(Vector3 value)
    {
        return Mathf.Approximately(value.x, 0f) &&
               Mathf.Approximately(value.y, 0f) &&
               Mathf.Approximately(value.z, 0f);
    }
}

public class FishRuntimeData : MonoBehaviour
{
    [SerializeField] private FishInstanceState state;

    public FishInstanceState State => state;

    public void SetState(FishInstanceState newState)
    {
        state = newState;
    }

    public FishInstanceState TakeState(string fallbackItemName)
    {
        state = FishFactory.EnsureValid(state, fallbackItemName);
        return state;
    }
}

[Serializable]
public class WaterQualityState
{
    public float ammonia;
    public float oxygen = 8f;
    public float temperature = 26f;
    public float ph = 8.1f;
    public float salinity = 35f;

    public void Clamp()
    {
        ammonia = Mathf.Max(0f, ammonia);
        oxygen = Mathf.Max(0f, oxygen);
        temperature = Mathf.Clamp(temperature, 0f, 50f);
        ph = Mathf.Clamp(ph, 0f, 14f);
        salinity = Mathf.Max(0f, salinity);
    }
}

[Serializable]
public class WaterIndicatorText
{
    public string label;
    public Text text;
}

[Serializable]
public class WaterParameterThreshold
{
    public float batasAmanMinimum;
    public float batasAmanMaksimum;
    public bool gunakanBahayaRendah;
    public float batasBahayaRendah;
    public bool gunakanBahayaTinggi;
    public float batasBahayaTinggi;
    public bool gunakanKritisRendah;
    public float batasKritisRendah;
    public bool gunakanKritisTinggi;
    public float batasKritisTinggi;

    public static WaterParameterThreshold CreateOxygen()
    {
        return new WaterParameterThreshold
        {
            batasAmanMinimum = 5f,
            batasAmanMaksimum = 8f,
            gunakanBahayaRendah = true,
            batasBahayaRendah = 5f,
            gunakanKritisRendah = true,
            batasKritisRendah = 4f
        };
    }

    public static WaterParameterThreshold CreateTemperature()
    {
        return new WaterParameterThreshold
        {
            batasAmanMinimum = 25f,
            batasAmanMaksimum = 27f,
            gunakanBahayaRendah = true,
            batasBahayaRendah = 21f,
            gunakanBahayaTinggi = true,
            batasBahayaTinggi = 31f
        };
    }

    public static WaterParameterThreshold CreatePh()
    {
        return new WaterParameterThreshold
        {
            batasAmanMinimum = 6.5f,
            batasAmanMaksimum = 7.5f,
            gunakanBahayaRendah = true,
            batasBahayaRendah = 6f,
            gunakanBahayaTinggi = true,
            batasBahayaTinggi = 8f
        };
    }

    public static WaterParameterThreshold CreateSalinity()
    {
        return new WaterParameterThreshold
        {
            batasAmanMinimum = 32f,
            batasAmanMaksimum = 35f,
            gunakanBahayaRendah = true,
            batasBahayaRendah = 32f,
            gunakanBahayaTinggi = true,
            batasBahayaTinggi = 35f
        };
    }

    public static WaterParameterThreshold CreateAmmonia()
    {
        return new WaterParameterThreshold
        {
            batasAmanMinimum = 0f,
            batasAmanMaksimum = 0.1f,
            gunakanBahayaTinggi = true,
            batasBahayaTinggi = 0.1f,
            gunakanKritisTinggi = true,
            batasKritisTinggi = 0.5f
        };
    }
}

[DefaultExecutionOrder(-50)]
public class AquariumSystem : MonoBehaviour
{
    [Serializable]
    public class FishPrefabEntry
    {
        public string itemName;
        public GameObject aquariumPrefab;
    }

    [Header("Capacity")]
    [SerializeField] private int maxFish = 12;

    [Header("Valid Fish Items")]
    [SerializeField] private List<string> allowedFishItemNames = new List<string>();
    [SerializeField] private List<FishData> fishDataCatalog = new List<FishData>();
    [SerializeField] private List<AI_Fish_Data> aiFishDataCatalog = new List<AI_Fish_Data>();

    [Header("World Display")]
    [SerializeField] private Collider swimBounds;
    [SerializeField] private Transform fishContainer;
    [SerializeField] private bool parentSpawnedFishToContainer = false;
    [SerializeField] private List<FishPrefabEntry> fishPrefabs = new List<FishPrefabEntry>();

    [Header("UI")]
    [SerializeField] private GameObject aquariumScreenUI;
    [SerializeField] private List<AquariumFishSlotUI> fishSlots = new List<AquariumFishSlotUI>();
    [SerializeField] private Text fishCountText;
    [SerializeField] private Text waterQualityText;
    [SerializeField] private Text warningText;
    [SerializeField] private WaterIndicatorText ammoniaIndicator = new WaterIndicatorText { label = "Amonia" };
    [SerializeField] private WaterIndicatorText oxygenIndicator = new WaterIndicatorText { label = "O2" };
    [SerializeField] private WaterIndicatorText salinityIndicator = new WaterIndicatorText { label = "Salinitas" };
    [SerializeField] private WaterIndicatorText phIndicator = new WaterIndicatorText { label = "pH" };
    [SerializeField] private WaterIndicatorText temperatureIndicator = new WaterIndicatorText { label = "Temperatur" };

    [Header("RAS Simulation")]
    [SerializeField] private WaterQualityState waterQuality = new WaterQualityState();
    [Tooltip("1 = real-time. 0.5 = 2x lebih lambat. 0.25 = 4x lebih lambat.")]
    [SerializeField] private float rasTimeScale = 1f;
    [SerializeField] private float simulationTickSeconds = 5f;

    [Header("Indicator Colors")]
    [SerializeField] private Color safeIndicatorColor = new Color(0.2f, 0.85f, 0.35f);
    [SerializeField] private Color dangerIndicatorColor = new Color(1f, 0.6f, 0.15f);
    [SerializeField] private Color criticalIndicatorColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Dissolved Oxygen (DO) Thresholds")]
    [SerializeField] private WaterParameterThreshold oxygenThresholds = WaterParameterThreshold.CreateOxygen();

    [Header("Temperature Thresholds")]
    [SerializeField] private WaterParameterThreshold temperatureThresholds = WaterParameterThreshold.CreateTemperature();

    [Header("pH Thresholds")]
    [SerializeField] private WaterParameterThreshold phThresholds = WaterParameterThreshold.CreatePh();

    [Header("Salinity Thresholds")]
    [SerializeField] private WaterParameterThreshold salinityThresholds = WaterParameterThreshold.CreateSalinity();

    [Header("Ammonia (NH3) Thresholds")]
    [SerializeField] private WaterParameterThreshold ammoniaThresholds = WaterParameterThreshold.CreateAmmonia();

    [Header("Aquarium Equipment")]
    [SerializeField] private List<EquipmentData> installedEquipment = new List<EquipmentData>();
    [SerializeField] private float waterChangeAmmoniaMultiplier = 0.35f;
    [SerializeField] private float waterChangeOxygenRecovery = 2f;
    [SerializeField] private float targetOxygen = 8f;
    [SerializeField] private float targetSalinity = 35f;

    [Header("Reward Lock")]
    [SerializeField] private bool startLockedUntilRewardUnlock = false;
    [SerializeField] private string persistentAquariumId = string.Empty;

    [Header("Feeding")]
    [Tooltip("Jeda singkat agar satu ikan tidak memakan beberapa pelet sekaligus saat collider bertumpuk.")]
    [SerializeField] private float fishEatCooldownSeconds = 0.35f;

    private readonly List<FishInstanceState> storedFish = new List<FishInstanceState>();
    private readonly List<GameObject> spawnedFish = new List<GameObject>();
    private readonly List<AquariumFoodPellet> activeFoodPellets = new List<AquariumFoodPellet>();
    private readonly Dictionary<string, float> fishEatCooldownUntil = new Dictionary<string, float>();
    private readonly HashSet<int> consumedInventoryItemIds = new HashSet<int>();
    private readonly List<string> warningBuffer = new List<string>();
    private float simulationTimer;
    private bool machineAeratorControlActive;
    private float machineAeratorIncreasePerTick;
    private bool machineHeaterControlActive;
    private float machineHeaterTarget;
    private float machineHeaterStepPerTick;
    private bool machineChillerControlActive;
    private float machineChillerTarget;
    private float machineChillerStepPerTick;

    // Cached fish counts — updated only when fish are added, removed, or die.
    // Avoids iterating storedFish every frame in Update().
    private int cachedLivingFishCount;
    private int cachedDeadFishCount;

    // Cached cooler state — updated only when equipment changes.
    private bool cachedCoolerActive;

    private enum WaterIndicatorSeverity
    {
        Safe,
        Danger,
        Critical
    }

    // â”€â”€â”€ RAS Galatama Subsystems â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”...
    private RasWaterSimulator rasSimulator;
    private RasFishManager    rasFishManager;

    public static AquariumSystem CurrentOpen { get; private set; }
    public event Action<AquariumSystem> AquariumStateChanged;
    /// <summary>Fired whenever any fish is successfully placed into any aquarium.</summary>
    public static event Action OnFishPlacedInAquarium;
    public event Action<AquariumSystem, WaterQualityState> WaterQualityChanged;
    public event Action<AquariumSystem, FishInstanceState> FishStateChanged;
    public event Action<AquariumSystem, FishInstanceState> FishDied;

    private bool isOpen;
    private bool inventoryWasOpenBeforeAquarium;
    private bool isRewardUnlocked = true;
    private bool isRestoringFromSave;

    public int MaxFish => maxFish;
    public int FishCount => storedFish.Count;
    public bool IsFull => FishCount >= maxFish;
    public bool IsOpen => isOpen;
    public bool IsRewardUnlocked => isRewardUnlocked;
    public bool IsRewardLocked => !isRewardUnlocked;
    public WaterQualityState WaterQuality => waterQuality;
    public string PersistentAquariumId => string.IsNullOrWhiteSpace(persistentAquariumId) ? BuildDefaultPersistentId() : persistentAquariumId;
    public IReadOnlyList<AquariumFishSlotUI> FishSlots => fishSlots;
    public Bounds SwimBounds => swimBounds != null
        ? swimBounds.bounds
        : new Bounds(fishContainer != null ? fishContainer.position : transform.position, Vector3.one * 4f);

    private void Awake()
    {
        isRewardUnlocked = !startLockedUntilRewardUnlock;
        if (string.IsNullOrWhiteSpace(persistentAquariumId))
            persistentAquariumId = BuildDefaultPersistentId();

        if (fishContainer == null)
            fishContainer = transform;

        EnsureFishSlots();
        InitializeRasSubsystems();
        RebuildFishCountCache();
    }

    private void InitializeRasSubsystems()
    {
        rasSimulator  = gameObject.AddComponent<RasWaterSimulator>();
        rasFishManager = gameObject.AddComponent<RasFishManager>();

        bool coolerInstalled = installedEquipment.Exists(
            e => e != null && e.aquariumRole == AquariumEquipmentRole.Chiller);

        cachedCoolerActive = coolerInstalled;
        rasSimulator.Initialize(waterQuality, coolerInstalled);
        rasFishManager.Initialize(waterQuality, storedFish, rasSimulator);

        rasFishManager.OnFishDied += HandleRasFishDeath;
    }

    private void Start()
    {
        if (aquariumScreenUI != null)
            aquariumScreenUI.SetActive(false);

        WarnIfFishContainerCanDeformFish();
        RefreshUI();
    }

    private void Update()
    {
        if (IsRewardLocked)
            return;

        // Simulasi kontinu berbasis Time.deltaTime (RAS Galatama real-time)
        float dt = Time.deltaTime;
        float rasDt = dt * Mathf.Max(0f, rasTimeScale);
        rasSimulator?.Tick(rasDt, cachedLivingFishCount, cachedDeadFishCount);
        rasFishManager?.Tick(rasDt);

        // Simulasi tick lama (equipment effects, UI refresh) tetap berjalan
        TickSimulation(dt);

        if (!isOpen) return;

        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.Inventory)
        {
            CloseAquarium();
            PlayerInputManager.Instance.ResetInventoryInput();
        }
    }

    /// <summary>
    /// Sinkronisasi status cooler ke RasWaterSimulator saat equipment berubah.
    /// Hanya dipanggil saat installedEquipment dimodifikasi, bukan setiap frame.
    /// </summary>
    private void SyncCoolerState()
    {
        if (rasSimulator == null) return;

        bool coolerInstalled = installedEquipment.Exists(
            e => e != null && e.aquariumRole == AquariumEquipmentRole.Chiller);

        if (coolerInstalled == cachedCoolerActive) return;

        cachedCoolerActive = coolerInstalled;
        rasSimulator.SetCoolerActive(coolerInstalled);
    }

    /// <summary>
    /// Handler kematian ikan dari RasFishManager (DO rendah atau kelaparan).
    /// </summary>
    private void HandleRasFishDeath(string instanceId, string reason)
    {
        for (int i = 0; i < storedFish.Count; i++)
        {
            FishInstanceState fish = storedFish[i];
            if (fish == null || fish.instanceId != instanceId) continue;

            Debug.Log($"[Aquarium][RAS] Ikan '{fish.itemName}' mati karena {reason}.");
            TransitionFishToDead();
            FishDied?.Invoke(this, fish);
            FishStateChanged?.Invoke(this, fish);
            RemoveFishAt(i);
            break;
        }
    }

    public void OpenAquarium()
    {
        if (IsRewardLocked)
        {
            Debug.Log("[Aquarium] Aquarium reward masih terkunci.");
            return;
        }

        if (isOpen) return;

        isOpen = true;
        CurrentOpen = this;

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory != null)
        {
            inventoryWasOpenBeforeAquarium = inventory.isOpen;
            inventory.inventoryScreenUI.SetActive(true);
            inventory.isOpen = true;
        }

        if (aquariumScreenUI != null)
            aquariumScreenUI.SetActive(true);

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorAndLook(false, false);
            PlayerInputManager.Instance.SetPlayerMovement(false);
        }

        Cursor.visible = true;
        RefreshUI();
    }

    public void CloseAquarium()
    {
        if (!isOpen) return;

        isOpen = false;
        if (CurrentOpen == this)
            CurrentOpen = null;

        if (aquariumScreenUI != null)
            aquariumScreenUI.SetActive(false);

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory != null)
        {
            inventory.ReCalculeList();

            if (!inventoryWasOpenBeforeAquarium)
            {
                inventory.inventoryScreenUI.SetActive(false);
                inventory.isOpen = false;
            }
        }

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorAndLook(true, true);
            PlayerInputManager.Instance.SetPlayerMovement(true);
        }

        Cursor.visible = false;
    }

    public bool TryAddFishFromInventoryItem(GameObject inventoryItem)
    {
        if (IsRewardLocked) return false;
        if (inventoryItem == null) return false;

        int inventoryItemId = inventoryItem.GetInstanceID();
        if (consumedInventoryItemIds.Contains(inventoryItemId))
        {
            Debug.Log("[Aquarium] Drop item ini sudah diproses, duplikasi spawn dicegah: " + inventoryItem.name);
            return false;
        }

        if (inventoryItem.GetComponent<InventoryItemLogic>() == null)
        {
            Debug.LogWarning("[Aquarium] Object yang di-drop bukan item inventory: " + inventoryItem.name);
            return false;
        }

        string itemName = ItemNameUtility.CleanName(inventoryItem.name);
        consumedInventoryItemIds.Add(inventoryItemId);

        FishRuntimeData runtimeData = inventoryItem.GetComponent<FishRuntimeData>();
        FishInstanceState fishState = runtimeData != null
            ? runtimeData.TakeState(itemName)
            : FishFactory.CreateFromWildFish(itemName, ResolveSpeciesData(itemName));

        if (!TryAddFish(fishState))
        {
            consumedInventoryItemIds.Remove(inventoryItemId);
            return false;
        }

        inventoryItem.transform.SetParent(null);
        inventoryItem.SetActive(false);
        Destroy(inventoryItem);

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.ReCalculeList();

        return true;
    }

    public bool TryAddFish(string itemName)
    {
        if (IsRewardLocked && !isRestoringFromSave) return false;
        itemName = ItemNameUtility.CleanName(itemName);
        return TryAddFish(FishFactory.CreateFromWildFish(itemName, ResolveSpeciesData(itemName)));
    }

    public bool TryAddFish(FishInstanceState fishState)
    {
        if (IsRewardLocked && !isRestoringFromSave) return false;
        fishState = FishFactory.EnsureValid(fishState, fishState != null ? fishState.itemName : string.Empty);
        string itemName = ItemNameUtility.CleanName(fishState.itemName);
        if (string.IsNullOrEmpty(itemName))
            return false;

        if (IsFull)
        {
            Debug.Log("[Aquarium] Aquarium penuh. Maksimal ikan: " + maxFish);
            return false;
        }

        GameObject prefab = ResolveAquariumPrefab(itemName);
        if (prefab == null)
        {
            Debug.LogWarning("[Aquarium] Item bukan ikan atau prefab 3D ikan tidak ditemukan: " + itemName);
            return false;
        }

        GameObject fishObject = SpawnFish(prefab);
        storedFish.Add(fishState);
        spawnedFish.Add(fishObject);
        rasFishManager?.RegisterFish(fishState);
        IncrementLivingFishCount();
        RefreshUI();
        AquariumStateChanged?.Invoke(this);
        OnFishPlacedInAquarium?.Invoke();
        return true;
    }

    public bool TryMoveFishToInventory(int index)
    {
        if (index < 0 || index >= storedFish.Count)
        {
            Debug.LogWarning($"[Aquarium] Index {index} tidak valid. Jumlah ikan: {storedFish.Count}");
            return false;
        }

        FishInstanceState fishState = storedFish[index];
        string itemName = fishState.itemName;
        if (!fishState.isAlive)
        {
            Debug.Log("[Aquarium] Ikan sudah mati dan tidak bisa dipindahkan sebagai ikan hidup: " + itemName);
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[Aquarium] InventorySystem tidak ditemukan.");
            return false;
        }

        if (!InventorySystem.Instance.CanAddItemsToInventory(itemName))
        {
            Debug.Log("[Aquarium] Inventory penuh, tidak bisa mengambil ikan: " + itemName);
            return false;
        }

        if (!InventorySystem.Instance.TryAddFishStateToInventory(fishState))
        {
            Debug.LogError("[Aquarium] Gagal menambah item ke inventory: " + itemName);
            return false;
        }

        Debug.Log($"[Aquarium] Ikan '{itemName}' (index {index}) berhasil dipindahkan ke inventory. State tetap terbawa.");
        RemoveFishAt(index);
        return true;
    }

    public bool TryMoveFishToInventory(AquariumFishSlotUI slot)
    {
        int slotIndex = fishSlots.IndexOf(slot);
        if (slotIndex < 0)
        {
            Debug.LogWarning("[Aquarium] Slot aquarium tidak terdaftar di AquariumSystem.");
            return false;
        }

        return TryMoveFishToInventory(slotIndex);
    }

    public bool TryMoveFishToInventory(AquariumFishSlotUI slot, GameObject targetInventorySlot)
    {
        int slotIndex = fishSlots.IndexOf(slot);
        if (slotIndex < 0 || slotIndex >= storedFish.Count)
        {
            Debug.LogWarning("[Aquarium] Slot aquarium kosong atau tidak terdaftar.");
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[Aquarium] InventorySystem tidak ditemukan.");
            return false;
        }

        FishInstanceState fishState = storedFish[slotIndex];
        string itemName = fishState.itemName;
        if (!fishState.isAlive)
        {
            Debug.Log("[Aquarium] Ikan sudah mati dan tidak bisa dipindahkan sebagai ikan hidup: " + itemName);
            return false;
        }

        if (!InventorySystem.Instance.TryAddFishStateToInventorySlot(fishState, targetInventorySlot))
            return false;

        Debug.Log($"[Aquarium] Ikan '{itemName}' dipindahkan lewat drag ke inventory dengan state yang sama.");
        RemoveFishAt(slotIndex);
        return true;
    }

    public bool RemoveFishAndDestroyModel(AquariumFishSlotUI slot)
    {
        int slotIndex = fishSlots.IndexOf(slot);
        if (slotIndex < 0 || slotIndex >= storedFish.Count)
        {
            Debug.LogWarning("[Aquarium] RemoveFishAndDestroyModel: slot tidak valid.");
            return false;
        }

        string itemName = storedFish[slotIndex].itemName;
        Debug.Log($"[Aquarium] Ikan '{itemName}' (slot {slotIndex}) dihapus dari aquarium. Model 3D di-destroy.");
        RemoveFishAt(slotIndex);
        return true;
    }

    public string GetFishName(int index)
    {
        if (index < 0 || index >= storedFish.Count)
            return string.Empty;

        return storedFish[index].itemName;
    }

    public FishInstanceState GetFishState(int index)
    {
        if (index < 0 || index >= storedFish.Count)
            return null;

        return storedFish[index];
    }

    public void FeedFish(int index, float feedPoints)
    {
        if (IsRewardLocked) return;
        if (index < 0 || index >= storedFish.Count)
            return;

        FishInstanceState fish = storedFish[index];
        if (fish == null || !fish.isAlive)
            return;

        fish.hunger = Mathf.Min(fish.maxHunger, fish.hunger + Mathf.Abs(feedPoints));
        FishStateChanged?.Invoke(this, fish);
        RefreshUI();
    }

    public bool SetPh(float targetPh)
    {
        if (IsRewardLocked) return false;
        float before = waterQuality.ph;
        waterQuality.ph = targetPh;
        CommitWaterQualityChange();
        Debug.Log($"[RAS][{name}] pH: {before:0.00} â†’ {waterQuality.ph:0.00}");
        return true;
    }

    public bool SetAmmonia(float targetAmmonia)
    {
        if (IsRewardLocked) return false;
        float before = waterQuality.ammonia;
        waterQuality.ammonia = Mathf.Max(0f, targetAmmonia);
        rasSimulator?.StopAmmoniaProduction();
        CommitWaterQualityChange();
        Debug.Log($"[RAS][{name}] NH3: {before:0.00} â†’ {waterQuality.ammonia:0.00}");
        return true;
    }

    public bool ChangeSalinity(float amount)
    {
        if (IsRewardLocked) return false;
        float before = waterQuality.salinity;
        waterQuality.salinity = Mathf.Max(0f, waterQuality.salinity + amount);
        CommitWaterQualityChange();
        Debug.Log($"[RAS][{name}] Salinitas: {before:0.00} â†’ {waterQuality.salinity:0.00} (delta {amount:+0.00;-0.00})");
        return true;
    }

    public bool IncreaseOxygen(float amount)
    {
        if (IsRewardLocked) return false;
        float before = waterQuality.oxygen;
        waterQuality.oxygen = Mathf.Clamp(waterQuality.oxygen + amount, 0f, targetOxygen);
        CommitWaterQualityChange();
        Debug.Log($"[RAS][{name}] O2: {before:0.00} â†’ {waterQuality.oxygen:0.00} (+{amount:0.00})");
        return true;
    }

    public void ConfigureMachineAerator(float oxygenIncreasePerTick)
    {
        machineAeratorControlActive = oxygenIncreasePerTick > 0f;
        machineAeratorIncreasePerTick = Mathf.Max(0f, oxygenIncreasePerTick);
    }

    public void ConfigureMachineTemperature(AquariumEquipmentRole machineRole, float targetTemperature, float changePerTick)
    {
        float step = Mathf.Abs(changePerTick);

        switch (machineRole)
        {
            case AquariumEquipmentRole.Heater:
                machineHeaterControlActive = true;
                machineHeaterTarget = targetTemperature;
                machineHeaterStepPerTick = step;
                break;
            case AquariumEquipmentRole.Chiller:
                machineChillerControlActive = true;
                machineChillerTarget = targetTemperature;
                machineChillerStepPerTick = step;
                break;
        }
    }

    public bool ChangeTemperature(float targetTemperature, float changePerTick)
    {
        if (IsRewardLocked) return false;
        float before = waterQuality.temperature;
        float step = Mathf.Abs(changePerTick);
        if (step <= 0f)
            waterQuality.temperature = targetTemperature;
        else
            waterQuality.temperature = Mathf.MoveTowards(waterQuality.temperature, targetTemperature, step);

        CommitWaterQualityChange();
        Debug.Log($"[RAS][{name}] Suhu: {before:0.0} â†’ {waterQuality.temperature:0.0} (target={targetTemperature:0.0}, step={changePerTick:0.0})");
        return true;
    }

    public bool SpawnFoodPellet(GameObject pelletPrefab, float hungerReduction)
    {
        return SpawnFoodPellets(pelletPrefab, hungerReduction, 1);
    }

    public bool SpawnFoodPellets(GameObject pelletPrefab, float hungerReduction, int pelletCount)
    {
        if (IsRewardLocked) return false;
        GameObject prefab = pelletPrefab != null ? pelletPrefab : Resources.Load<GameObject>("Pelets_Fabs");
        if (prefab == null)
        {
            Debug.LogError("[Aquarium] Prefab pelet tidak ditemukan. Assign prefab atau buat Resources/Pelets_Fabs.");
            return false;
        }

        Bounds bounds = SwimBounds;
        pelletCount = Mathf.Max(1, pelletCount);

        // Diagram: Pakan (+) --> Ammonia (+) dan Pakan (+) --> pH (-)
        // Setiap pellet yang di-spawn dihitung sebagai food load.
        // Ikan yang makan akan menguranginya; sisanya membusuk secara alami.
        rasSimulator?.AddFoodLoad(pelletCount);

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spawnPosition = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y - 0.15f,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z));

            GameObject pellet = Instantiate(prefab, spawnPosition, Quaternion.identity);
            AquariumFoodPellet food = pellet.GetComponent<AquariumFoodPellet>();
            if (food == null)
                food = pellet.AddComponent<AquariumFoodPellet>();

            food.Initialize(this, hungerReduction);
            NotifyFishAboutFood(food);
        }

        Debug.Log($"[Aquarium] {pelletCount} pelet ditebar ke aquarium. Food load: {rasSimulator?.FoodLoad:0.00}");
        return true;
    }

    public void NotifyFishAboutFood(AquariumFoodPellet food)
    {
        if (IsRewardLocked) return;
        if (food == null)
            return;

        if (!activeFoodPellets.Contains(food))
            activeFoodPellets.Add(food);

        ReassignFoodTargets();
    }

    public bool TryConsumeFood(AquariumFoodPellet food, FishBrain eater)
    {
        if (IsRewardLocked) return false;
        if (food == null || eater == null)
            return false;

        int fishIndex = spawnedFish.IndexOf(eater.gameObject);
        if (fishIndex < 0 || fishIndex >= storedFish.Count)
            return false;

        FishInstanceState fishState = storedFish[fishIndex];
        if (fishState == null || !fishState.isAlive)
            return false;

        if (fishState.hunger >= fishState.maxHunger)
            return false;

        if (!string.IsNullOrEmpty(fishState.instanceId) &&
            fishEatCooldownUntil.TryGetValue(fishState.instanceId, out float cooldownUntil) &&
            Time.time < cooldownUntil)
        {
            return false;
        }

        // Kurangi food load saat pellet dimakan ikan
        // Diagram: saat Pakan dikonsumsi, efek NH3/pH dari pakan berkurang
        rasSimulator?.ConsumeFoodLoad(1f);

        // Terapkan efektivitas pakan dari kondisi air saat ini
        float baseReduction = food.HungerReduction;
        float efficiency    = rasSimulator != null ? rasSimulator.GetFeedEfficiency() : 1f;
        FeedFish(fishIndex, baseReduction * efficiency);
        rasSimulator?.RegisterFedFish();

        if (!string.IsNullOrEmpty(fishState.instanceId))
            fishEatCooldownUntil[fishState.instanceId] = Time.time + Mathf.Max(0f, fishEatCooldownSeconds);

        Destroy(food.gameObject);
        return true;
    }

    public void NotifyFoodRemoved(AquariumFoodPellet food)
    {
        if (food == null)
            return;

        if (activeFoodPellets.Remove(food))
            ReassignFoodTargets();
    }

    public bool InstallEquipment(EquipmentData equipment)
    {
        if (IsRewardLocked) return false;
        if (equipment == null || installedEquipment.Contains(equipment))
            return false;

        installedEquipment.Add(equipment);
        SyncCoolerState();
        RefreshUI();
        AquariumStateChanged?.Invoke(this);
        return true;
    }

    public bool RemoveEquipment(EquipmentData equipment)
    {
        if (IsRewardLocked) return false;
        if (equipment == null) return false;

        bool removed = installedEquipment.Remove(equipment);
        if (removed)
        {
            SyncCoolerState();
            RefreshUI();
            AquariumStateChanged?.Invoke(this);
        }

        return removed;
    }

    public void PerformWaterChange(float intensity = 1f)
    {
        if (IsRewardLocked) return;
        intensity = Mathf.Clamp01(intensity);
        waterQuality.ammonia *= Mathf.Lerp(1f, waterChangeAmmoniaMultiplier, intensity);
        waterQuality.oxygen = Mathf.Min(targetOxygen, waterQuality.oxygen + waterChangeOxygenRecovery * intensity);
        waterQuality.salinity = Mathf.Lerp(waterQuality.salinity, targetSalinity, intensity * 0.35f);
        CommitWaterQualityChange();
    }

    private void CommitWaterQualityChange()
    {
        waterQuality.Clamp();
        WaterQualityChanged?.Invoke(this, waterQuality);
        RefreshWaterQualityUI();
        AquariumStateChanged?.Invoke(this);
    }

    private GameObject SpawnFish(GameObject prefab)
    {
        Vector3 spawnPosition = GetRandomSwimPosition();
        Quaternion spawnRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        Vector3 originalScale = prefab.transform.localScale;
        GameObject fishObject = Instantiate(prefab, spawnPosition, spawnRotation);
        fishObject.transform.localScale = originalScale;

        if (parentSpawnedFishToContainer && fishContainer != null)
            fishObject.transform.SetParent(fishContainer, true);

        DisableAquariumCatchTags(fishObject);
        ApplySwimBounds(fishObject);
        return fishObject;
    }

    private Vector3 GetRandomSwimPosition()
    {
        if (swimBounds == null)
            return fishContainer != null ? fishContainer.position : transform.position;

        Bounds bounds = swimBounds.bounds;
        const int maxAttempts = 24;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );

            if (IsPointInsideSwimCollider(candidate))
                return candidate;
        }

        return swimBounds.ClosestPoint(bounds.center);
    }

    private void ApplySwimBounds(GameObject fishObject)
    {
        if (fishObject == null || swimBounds == null) return;

        FishBrain fishBrain = fishObject.GetComponent<FishBrain>();
        if (fishBrain != null)
        {
            fishBrain.SetBoundary(swimBounds);
            fishBrain.SetZoneType(ZoneType.Aquarium);
        }
    }

    private bool IsPointInsideSwimCollider(Vector3 point)
    {
        if (swimBounds == null || !swimBounds.enabled)
            return false;

        Vector3 closestPoint = swimBounds.ClosestPoint(point);
        return (closestPoint - point).sqrMagnitude <= 0.0001f;
    }

    private void DisableAquariumCatchTags(GameObject fishObject)
    {
        Transform[] children = fishObject.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.CompareTag("Fish"))
                child.tag = "Untagged";
        }
    }

    private GameObject ResolveAquariumPrefab(string itemName)
    {
        foreach (FishPrefabEntry entry in fishPrefabs)
        {
            if (entry == null || entry.aquariumPrefab == null) continue;

            if (entry.itemName == itemName && IsValidFishItem(itemName, entry.aquariumPrefab))
                return entry.aquariumPrefab;
        }

        GameObject resourcePrefab = Resources.Load<GameObject>(itemName + "_Fabs");
        return IsValidFishItem(itemName, resourcePrefab) ? resourcePrefab : null;
    }

    private bool IsValidFishItem(string itemName, GameObject prefab)
    {
        if (string.IsNullOrEmpty(itemName) || prefab == null)
            return false;

        foreach (string allowedName in allowedFishItemNames)
        {
            if (ItemNameUtility.CleanName(allowedName) == itemName)
                return true;
        }

        foreach (FishData fishData in fishDataCatalog)
        {
            if (fishData != null && ItemNameUtility.CleanName(fishData.itemName) == itemName)
                return true;
        }

        foreach (AI_Fish_Data fishData in aiFishDataCatalog)
        {
            if (fishData != null && ItemNameUtility.CleanName(fishData.ItemName) == itemName)
                return true;
        }

        return prefab.GetComponentInChildren<FishBase>(true) != null;
    }

    /// <summary>
    /// Tukar posisi dua ikan antar slot aquarium tanpa mengubah state ikan.
    /// </summary>
    public void SwapFish(int indexA, int indexB)
    {
        if (indexA == indexB) return;
        if (indexA < 0 || indexA >= storedFish.Count) return;
        if (indexB < 0 || indexB >= storedFish.Count) return;

        // Swap storedFish
        FishInstanceState temp = storedFish[indexA];
        storedFish[indexA] = storedFish[indexB];
        storedFish[indexB] = temp;

        // Swap spawnedFish jika tersedia
        if (indexA < spawnedFish.Count && indexB < spawnedFish.Count)
        {
            GameObject tempObj = spawnedFish[indexA];
            spawnedFish[indexA] = spawnedFish[indexB];
            spawnedFish[indexB] = tempObj;
        }

        RefreshUI();
        Debug.Log($"[Aquarium] Ikan ditukar: slot {indexA} â†” slot {indexB}");
    }

    private void RemoveFishAt(int index)
    {
        if (index < 0 || index >= storedFish.Count)
            return;

        FishInstanceState fish = storedFish[index];
        rasFishManager?.UnregisterFish(fish);
        if (fish != null && !string.IsNullOrEmpty(fish.instanceId))
            fishEatCooldownUntil.Remove(fish.instanceId);

        if (index < spawnedFish.Count && spawnedFish[index] != null)
            Destroy(spawnedFish[index]);

        storedFish.RemoveAt(index);

        if (index < spawnedFish.Count)
            spawnedFish.RemoveAt(index);

        // Rebuild cache after structural change to storedFish
        RebuildFishCountCache();

        ReassignFoodTargets();
        RefreshUI();
        AquariumStateChanged?.Invoke(this);
    }

    private void RefreshUI()
    {
        EnsureFishSlots();

        for (int i = 0; i < fishSlots.Count; i++)
        {
            // Selalu pastikan setiap slot tahu pemilik AquariumSystem-nya
            fishSlots[i].BindAquariumSystem(this);

            FishInstanceState fishState = i < storedFish.Count ? storedFish[i] : null;
            string itemName = fishState != null ? fishState.itemName : string.Empty;
            fishSlots[i].SetSlot(this, i, itemName, null, fishState);
        }

        if (fishCountText != null)
            fishCountText.text = FishCount + " / " + maxFish;
        RefreshWaterQualityUI();

        if (fishSlots.Count < storedFish.Count)
            Debug.LogWarning("[Aquarium] Jumlah slot UI (" + fishSlots.Count + ") lebih sedikit dari jumlah ikan (" + storedFish.Count + "). Beberapa ikan tidak tampil.");
    }

    private void EnsureFishSlots()
    {
        fishSlots.RemoveAll(slot => slot == null || !IsSlotInsideAquariumUI(slot));

        if (aquariumScreenUI == null)
            return;

        AquariumFishSlotUI[] discoveredSlots = aquariumScreenUI.GetComponentsInChildren<AquariumFishSlotUI>(true);
        foreach (AquariumFishSlotUI slot in discoveredSlots)
        {
            if (!fishSlots.Contains(slot))
                fishSlots.Add(slot);
        }

        // Urutkan berdasarkan urutan hierarchy (sibling index) agar konsisten dengan nama Aqua_A_Slo...
        fishSlots.Sort(CompareSlotsByHierarchyOrder);
    }

    private bool IsSlotInsideAquariumUI(AquariumFishSlotUI slot)
    {
        if (slot == null)
            return false;

        if (aquariumScreenUI == null)
            return true;

        return slot.transform == aquariumScreenUI.transform ||
               slot.transform.IsChildOf(aquariumScreenUI.transform);
    }

    /// <summary>
    /// Urutkan slot berdasarkan posisi dalam hierarchy (sibling index dari root ke bawah)
    /// agar urutan slot konsisten dengan urutan visual Aqua_A_Slot_1..9.
    /// </summary>
    private int CompareSlotsByHierarchyOrder(AquariumFishSlotUI left, AquariumFishSlotUI right)
    {
        if (left == right) return 0;
        if (left == null) return 1;
        if (right == null) return -1;

        // Hitung depth-first sibling path dari UI root ke slot
        string leftPath = BuildHierarchyIndexPath(left.transform, aquariumScreenUI != null ? aquariumScreenUI.transform : null);
        string rightPath = BuildHierarchyIndexPath(right.transform, aquariumScreenUI != null ? aquariumScreenUI.transform : null);

        return string.Compare(leftPath, rightPath, System.StringComparison.Ordinal);
    }

    private static string BuildHierarchyIndexPath(Transform t, Transform root)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        while (t != null && t != root)
        {
            sb.Insert(0, t.GetSiblingIndex().ToString("D4") + "/");
            t = t.parent;
        }
        return sb.ToString();
    }

    private void TickSimulation(float deltaTime)
    {
        if (IsRewardLocked)
            return;

        if (simulationTickSeconds <= 0f)
            return;

        simulationTimer += deltaTime;
        if (simulationTimer < simulationTickSeconds)
            return;

        int tickCount = Mathf.FloorToInt(simulationTimer / simulationTickSeconds);
        simulationTimer -= tickCount * simulationTickSeconds;

        for (int i = 0; i < tickCount; i++)
            RunSimulationTick();
    }

    private void RunSimulationTick()
    {
        ApplyEquipmentEffects();
        CommitWaterQualityChange();

        // Hanya perbarui teks ringkasan air + warning â€” TIDAK re-spawn icon slot
    }

    /// <summary>
    /// Perbarui hanya teks ringkasan kualitas air dan warning.
    /// Dipanggil setiap simulation tick agar icon prefab ikan tidak dihapus/respawn ulang.
    /// </summary>
    private void RefreshWaterQualityUI()
    {
        if (fishCountText != null)
            fishCountText.text = FishCount + " / " + maxFish;

        UpdateWaterIndicator(ammoniaIndicator, waterQuality.ammonia, "{0}: {1:0.00} mg/L", EvaluateParameterSeverity(waterQuality.ammonia, ammoniaThresholds));
        UpdateWaterIndicator(oxygenIndicator, waterQuality.oxygen, "{0}: {1:0.0} mg/L", EvaluateParameterSeverity(waterQuality.oxygen, oxygenThresholds));
        UpdateWaterIndicator(salinityIndicator, waterQuality.salinity, "{0}: {1:0.0} ppt", EvaluateParameterSeverity(waterQuality.salinity, salinityThresholds));
        UpdateWaterIndicator(phIndicator, waterQuality.ph, "{0}: {1:0.0}", EvaluateParameterSeverity(waterQuality.ph, phThresholds));
        UpdateWaterIndicator(temperatureIndicator, waterQuality.temperature, "{0}: {1:0.0} C", EvaluateParameterSeverity(waterQuality.temperature, temperatureThresholds));

        if (waterQualityText != null)
        {
            waterQualityText.text = HasConfiguredSplitIndicators()
                ? string.Empty
                : $"NH3 {waterQuality.ammonia:0.00} | O2 {waterQuality.oxygen:0.0} | Temp {waterQuality.temperature:0.0} | pH {waterQuality.ph:0.0} | Sal {waterQuality.salinity:0.0}";
        }

        if (warningText != null)
            warningText.text = BuildWarningText();
    }

    private void ApplyEquipmentEffects()
    {
        if (IsRewardLocked)
            return;

        bool oxygenHandledByMachine = false;
        if (machineAeratorControlActive && machineAeratorIncreasePerTick > 0f && waterQuality.oxygen < targetOxygen)
        {
            float oxygenDelta = Mathf.Min(machineAeratorIncreasePerTick, targetOxygen - waterQuality.oxygen);
            waterQuality.oxygen += oxygenDelta;
            oxygenHandledByMachine = true;
        }

        float temperatureDelta = 0f;
        bool temperatureHandledByMachine = false;

        if (machineHeaterControlActive && waterQuality.temperature < machineHeaterTarget)
        {
            float heaterDelta = machineHeaterStepPerTick <= 0f
                ? machineHeaterTarget - waterQuality.temperature
                : Mathf.Min(machineHeaterStepPerTick, machineHeaterTarget - waterQuality.temperature);

            temperatureDelta += heaterDelta;
            temperatureHandledByMachine = true;
        }

        if (machineChillerControlActive && waterQuality.temperature > machineChillerTarget)
        {
            float chillerDelta = machineChillerStepPerTick <= 0f
                ? waterQuality.temperature - machineChillerTarget
                : Mathf.Min(machineChillerStepPerTick, waterQuality.temperature - machineChillerTarget);

            temperatureDelta -= chillerDelta;
            temperatureHandledByMachine = true;
        }

        if (temperatureHandledByMachine)
            waterQuality.temperature += temperatureDelta;

        foreach (EquipmentData equipment in installedEquipment)
        {
            if (equipment == null)
                continue;

            waterQuality.ammonia = Mathf.Max(0f, waterQuality.ammonia - Mathf.Max(0f, equipment.ammoniaReductionPerTick));

            if (!oxygenHandledByMachine &&
                equipment.oxygenIncreasePerTick > 0f &&
                waterQuality.oxygen < targetOxygen)
            {
                float oxygenDelta = Mathf.Min(
                    equipment.oxygenIncreasePerTick,
                    targetOxygen - waterQuality.oxygen);
                waterQuality.oxygen += oxygenDelta;
            }

            if (!temperatureHandledByMachine && equipment.temperatureChangePerTick > 0f)
            {
                if (equipment.aquariumRole == AquariumEquipmentRole.Heater &&
                    waterQuality.temperature < equipment.targetTemperature)
                {
                    float heaterDelta = Mathf.Min(
                        equipment.temperatureChangePerTick,
                        equipment.targetTemperature - waterQuality.temperature);
                    waterQuality.temperature += heaterDelta;
                }
                else if (equipment.aquariumRole == AquariumEquipmentRole.Chiller &&
                         waterQuality.temperature > equipment.targetTemperature)
                {
                    float chillerDelta = Mathf.Min(
                        equipment.temperatureChangePerTick,
                        waterQuality.temperature - equipment.targetTemperature);
                    waterQuality.temperature -= chillerDelta;
                }
                else
                {
                    waterQuality.temperature = Mathf.MoveTowards(
                        waterQuality.temperature,
                        equipment.targetTemperature,
                        equipment.temperatureChangePerTick);
                }
            }
        }
    }


    public void SetRewardUnlocked(bool unlocked)
    {
        if (isRewardUnlocked == unlocked)
        {
            if (IsRewardLocked && isOpen)
                CloseAquarium();
            return;
        }

        isRewardUnlocked = unlocked;

        if (IsRewardLocked && isOpen)
            CloseAquarium();

        RefreshUI();
    }

    public AquariumSaveData CaptureSaveData()
    {
        AquariumSaveData data = new AquariumSaveData
        {
            aquariumId = PersistentAquariumId,
            hasRewardUnlockState = true,
            isRewardUnlocked = isRewardUnlocked,
            waterQuality = WaterQualitySaveData.FromRuntime(waterQuality),
            installedEquipmentItemNames = CaptureInstalledEquipmentNames(),
            fish = new List<FishStateSaveData>()
        };

        for (int i = 0; i < storedFish.Count; i++)
        {
            FishInstanceState fishState = storedFish[i];
            if (fishState == null)
                continue;

            data.fish.Add(FishStateSaveData.FromRuntime(fishState, fishState.itemName));
        }

        return data;
    }

    public void RestoreFromSaveData(AquariumSaveData data)
    {
        ClearAquariumForRestore();

        if (data == null)
        {
            RefreshUI();
            return;
        }

        bool originalRewardUnlocked = isRewardUnlocked;
        bool targetRewardUnlocked = ResolveRewardUnlockStateForRestore(data, originalRewardUnlocked);
        isRestoringFromSave = true;
        isRewardUnlocked = true;

        try
        {
            if (data.waterQuality != null)
                data.waterQuality.ApplyTo(waterQuality);

            RestoreInstalledEquipment(data.installedEquipmentItemNames);

            if (data.fish != null)
            {
                for (int i = 0; i < data.fish.Count; i++)
                {
                    FishStateSaveData fishSave = data.fish[i];
                    if (fishSave == null)
                        continue;

                    FishInstanceState fishState = fishSave.ToRuntimeState(fishSave.itemName);
                    if (fishState == null || string.IsNullOrEmpty(fishState.itemName))
                    {
                        Debug.LogWarning("[Aquarium] Data ikan dilewati saat restore karena itemName kosong.");
                        continue;
                    }

                    if (!TryAddFish(fishState))
                        Debug.LogWarning("[Aquarium] Gagal restore ikan ke aquarium: " + fishState.itemName);
                }
            }
        }
        finally
        {
            isRestoringFromSave = false;
            isRewardUnlocked = targetRewardUnlocked;
        }

        waterQuality.Clamp();
        SyncCoolerState();
        RebuildFishCountCache();
        RefreshUI();
    }

    private bool ResolveRewardUnlockStateForRestore(AquariumSaveData data, bool fallbackState)
    {
        if (data == null)
            return fallbackState;

        if (data.hasRewardUnlockState)
            return data.isRewardUnlocked;

        bool hasLegacyProgress =
            (data.fish != null && data.fish.Count > 0) ||
            (data.installedEquipmentItemNames != null && data.installedEquipmentItemNames.Count > 0);

        return hasLegacyProgress || fallbackState;
    }

    /// <summary>Rebuild both living and dead fish count caches from scratch.</summary>
    private void RebuildFishCountCache()
    {
        cachedLivingFishCount = 0;
        cachedDeadFishCount = 0;
        foreach (FishInstanceState fish in storedFish)
        {
            if (fish == null) continue;
            if (fish.isAlive) cachedLivingFishCount++;
            else cachedDeadFishCount++;
        }
    }

    private void IncrementLivingFishCount()
    {
        cachedLivingFishCount++;
    }

    private void TransitionFishToDead()
    {
        if (cachedLivingFishCount > 0) cachedLivingFishCount--;
        cachedDeadFishCount++;
    }

    private string BuildWarningText()
    {
        warningBuffer.Clear();
        AddWarningIfNeeded(warningBuffer, "Amonia", waterQuality.ammonia, ammoniaThresholds);
        AddWarningIfNeeded(warningBuffer, "O2", waterQuality.oxygen, oxygenThresholds);
        AddWarningIfNeeded(warningBuffer, "Salinitas", waterQuality.salinity, salinityThresholds);
        AddWarningIfNeeded(warningBuffer, "pH", waterQuality.ph, phThresholds);
        AddWarningIfNeeded(warningBuffer, "Temperatur", waterQuality.temperature, temperatureThresholds);

        return warningBuffer.Count == 0 ? string.Empty : string.Join("\n", warningBuffer);
    }

    private void UpdateWaterIndicator(WaterIndicatorText indicator, float value, string format, WaterIndicatorSeverity severity)
    {
        if (indicator == null || indicator.text == null)
            return;

        string label = string.IsNullOrEmpty(indicator.label) ? "Water" : indicator.label;
        indicator.text.text = string.Format(format, label, value);
        indicator.text.color = GetIndicatorColor(severity);
    }

    private bool HasConfiguredSplitIndicators()
    {
        return (ammoniaIndicator != null && ammoniaIndicator.text != null) ||
               (oxygenIndicator != null && oxygenIndicator.text != null) ||
               (salinityIndicator != null && salinityIndicator.text != null) ||
               (phIndicator != null && phIndicator.text != null) ||
               (temperatureIndicator != null && temperatureIndicator.text != null);
    }

    private WaterIndicatorSeverity EvaluateParameterSeverity(float value, WaterParameterThreshold thresholds)
    {
        if (thresholds == null)
            return WaterIndicatorSeverity.Safe;

        if (thresholds.gunakanKritisRendah && value < thresholds.batasKritisRendah)
            return WaterIndicatorSeverity.Critical;

        if (thresholds.gunakanKritisTinggi && value > thresholds.batasKritisTinggi)
            return WaterIndicatorSeverity.Critical;

        if (value >= thresholds.batasAmanMinimum && value <= thresholds.batasAmanMaksimum)
            return WaterIndicatorSeverity.Safe;

        bool isDangerLow = thresholds.gunakanBahayaRendah && value < thresholds.batasBahayaRendah;
        bool isDangerHigh = thresholds.gunakanBahayaTinggi && value > thresholds.batasBahayaTinggi;
        return (isDangerLow || isDangerHigh) ? WaterIndicatorSeverity.Danger : WaterIndicatorSeverity.Safe;
    }

    private Color GetIndicatorColor(WaterIndicatorSeverity severity)
    {
        switch (severity)
        {
            case WaterIndicatorSeverity.Danger:
                return dangerIndicatorColor;
            case WaterIndicatorSeverity.Critical:
                return criticalIndicatorColor;
            default:
                return safeIndicatorColor;
        }
    }

    private void AddWarningIfNeeded(List<string> warnings, string label, float value, WaterParameterThreshold thresholds)
    {
        WaterIndicatorSeverity severity = EvaluateParameterSeverity(value, thresholds);
        if (severity == WaterIndicatorSeverity.Safe)
            return;

        string direction = GetWarningDirection(value, thresholds);
        string level = severity == WaterIndicatorSeverity.Critical ? "kritikal" : "bahaya";
        warnings.Add($"{label} {direction} ({level})");
    }

    private string GetWarningDirection(float value, WaterParameterThreshold thresholds)
    {
        if (thresholds == null)
            return "tidak normal";

        if ((thresholds.gunakanKritisRendah && value < thresholds.batasKritisRendah) ||
            (thresholds.gunakanBahayaRendah && value < thresholds.batasBahayaRendah))
            return "rendah";

        if ((thresholds.gunakanKritisTinggi && value > thresholds.batasKritisTinggi) ||
            (thresholds.gunakanBahayaTinggi && value > thresholds.batasBahayaTinggi))
            return "tinggi";

        return "tidak normal";
    }

    private AI_Fish_Data ResolveSpeciesData(string itemName)
    {
        itemName = ItemNameUtility.CleanName(itemName);
        foreach (AI_Fish_Data fishData in aiFishDataCatalog)
        {
            if (fishData != null && ItemNameUtility.CleanName(fishData.ItemName) == itemName)
                return fishData;
        }

        return null;
    }

    private void WarnIfFishContainerCanDeformFish()
    {
        if (fishContainer == null || !parentSpawnedFishToContainer)
            return;

        Vector3 scale = fishContainer.lossyScale;
        bool nonUniformScale =
            !Mathf.Approximately(scale.x, scale.y) ||
            !Mathf.Approximately(scale.y, scale.z);

        if (nonUniformScale)
        {
            Debug.LogWarning("[Aquarium] FishContainer memiliki scale tidak seragam. Ikan bisa terlihat gepeng. Gunakan FishContainer dengan scale 1,1,1 atau matikan Parent Spawned Fish To Container.");
        }
    }

    private List<string> CaptureInstalledEquipmentNames()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < installedEquipment.Count; i++)
        {
            EquipmentData equipment = installedEquipment[i];
            if (equipment == null || string.IsNullOrEmpty(equipment.itemName))
                continue;

            names.Add(equipment.itemName);
        }

        return names;
    }

    private void RestoreInstalledEquipment(List<string> equipmentItemNames)
    {
        installedEquipment.Clear();
        if (equipmentItemNames == null)
            return;

        for (int i = 0; i < equipmentItemNames.Count; i++)
        {
            EquipmentData equipment = ResolveEquipmentByItemName(equipmentItemNames[i]);
            if (equipment != null && !installedEquipment.Contains(equipment))
                installedEquipment.Add(equipment);
        }
    }

    private EquipmentData ResolveEquipmentByItemName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        for (int i = 0; i < installedEquipment.Count; i++)
        {
            EquipmentData equipment = installedEquipment[i];
            if (equipment != null && equipment.itemName == itemName)
                return equipment;
        }

        if (EquipSystem.Instance != null && EquipSystem.Instance.equipmentDataList != null)
        {
            for (int i = 0; i < EquipSystem.Instance.equipmentDataList.Count; i++)
            {
                EquipmentData equipment = EquipSystem.Instance.equipmentDataList[i];
                if (equipment != null && equipment.itemName == itemName)
                    return equipment;
            }
        }

        return null;
    }

    private void ClearAquariumForRestore()
    {
        activeFoodPellets.Clear();
        fishEatCooldownUntil.Clear();

        for (int i = spawnedFish.Count - 1; i >= 0; i--)
        {
            if (spawnedFish[i] != null)
                Destroy(spawnedFish[i]);
        }

        spawnedFish.Clear();

        for (int i = 0; i < storedFish.Count; i++)
        {
            rasFishManager?.UnregisterFish(storedFish[i]);
        }

        storedFish.Clear();
        consumedInventoryItemIds.Clear();
    }

    private string BuildDefaultPersistentId()
    {
        StringBuilder builder = new StringBuilder(transform.name);
        Transform current = transform.parent;

        while (current != null)
        {
            builder.Insert(0, current.name + "/");
            current = current.parent;
        }

        return builder.ToString();
    }

    private void ReassignFoodTargets()
    {
        activeFoodPellets.RemoveAll(food => food == null);

        var availablePellets = new List<AquariumFoodPellet>(activeFoodPellets.Count);
        foreach (AquariumFoodPellet pellet in activeFoodPellets)
        {
            if (pellet != null)
                availablePellets.Add(pellet);
        }

        var prioritizedFishIndices = new List<int>(Mathf.Min(storedFish.Count, spawnedFish.Count));
        for (int i = 0; i < storedFish.Count && i < spawnedFish.Count; i++)
            prioritizedFishIndices.Add(i);

        // Ikan yang paling lapar diprioritaskan memilih pelet lebih dulu
        // agar respons makan terasa lebih agresif saat pakan baru ditebar.
        prioritizedFishIndices.Sort((left, right) =>
        {
            FishInstanceState leftFish = storedFish[left];
            FishInstanceState rightFish = storedFish[right];

            float leftPercent = leftFish != null ? leftFish.HungerPercent : 1f;
            float rightPercent = rightFish != null ? rightFish.HungerPercent : 1f;
            return leftPercent.CompareTo(rightPercent);
        });

        for (int orderIndex = 0; orderIndex < prioritizedFishIndices.Count; orderIndex++)
        {
            int i = prioritizedFishIndices[orderIndex];
            FishInstanceState state = storedFish[i];
            GameObject fishObject = spawnedFish[i];
            if (state == null || fishObject == null)
                continue;

            FishBrain brain = fishObject.GetComponent<FishBrain>();
            if (brain == null)
                continue;

            if (!state.isAlive || state.hunger >= state.maxHunger || availablePellets.Count == 0)
            {
                brain.ClearFoodTarget();
                continue;
            }

            AquariumFoodPellet nearestPellet = null;
            float nearestSqrDistance = float.MaxValue;
            Vector3 fishPosition = fishObject.transform.position;

            for (int pelletIndex = 0; pelletIndex < availablePellets.Count; pelletIndex++)
            {
                AquariumFoodPellet pellet = availablePellets[pelletIndex];
                float sqrDistance = (pellet.transform.position - fishPosition).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestPellet = pellet;
                }
            }

            if (nearestPellet != null)
            {
                brain.SetFoodTarget(nearestPellet.transform, 0.05f);
                availablePellets.Remove(nearestPellet);
            }
            else
            {
                brain.ClearFoodTarget();
            }
        }
    }
}
