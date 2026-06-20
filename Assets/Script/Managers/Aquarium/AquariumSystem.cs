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
    [SerializeField] private Color safeIndicatorColor = new Color(0.2f, 0.85f, 0.35f);
    [SerializeField] private Color dangerIndicatorColor = new Color(1f, 0.6f, 0.15f);
    [SerializeField] private Color criticalIndicatorColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private float oxygenDangerMargin = 1f;
    [SerializeField] private float ammoniaDangerMargin = 0.35f;
    [SerializeField] private float temperatureDangerMargin = 1.5f;
    [SerializeField] private float phDangerMargin = 0.4f;
    [SerializeField] private float salinityDangerMargin = 3f;

    [Header("Aquarium Equipment")]
    [SerializeField] private List<EquipmentData> installedEquipment = new List<EquipmentData>();
    [SerializeField] private float waterChangeAmmoniaMultiplier = 0.35f;
    [SerializeField] private float waterChangeOxygenRecovery = 2f;
    [SerializeField] private float targetOxygen = 8f;
    [SerializeField] private float targetSalinity = 35f;

    [Header("Reward Lock")]
    [SerializeField] private bool startLockedUntilRewardUnlock = false;
    [SerializeField] private string persistentAquariumId = string.Empty;

    private readonly List<FishInstanceState> storedFish = new List<FishInstanceState>();
    private readonly List<GameObject> spawnedFish = new List<GameObject>();
    private readonly HashSet<int> consumedInventoryItemIds = new HashSet<int>();
    private float simulationTimer;

    private enum WaterIndicatorSeverity
    {
        Safe,
        Danger,
        Critical
    }

    private struct WaterThresholdProfile
    {
        public float minOxygen;
        public float maxAmmonia;
        public float minTemperature;
        public float maxTemperature;
        public float minPh;
        public float maxPh;
        public float minSalinity;
        public float maxSalinity;
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
    }

    private void InitializeRasSubsystems()
    {
        rasSimulator  = gameObject.AddComponent<RasWaterSimulator>();
        rasFishManager = gameObject.AddComponent<RasFishManager>();

        bool coolerInstalled = installedEquipment.Exists(
            e => e != null && e.aquariumRole == AquariumEquipmentRole.Chiller);

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
        rasSimulator?.Tick(rasDt, CountLivingFish(), CountDeadFish());
        rasFishManager?.Tick(rasDt);
        SyncCoolerState();

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
    /// </summary>
    private void SyncCoolerState()
    {
        if (rasSimulator == null) return;

        bool coolerInstalled = installedEquipment.Exists(
            e => e != null && e.aquariumRole == AquariumEquipmentRole.Chiller);

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
        if (IsRewardLocked) return false;
        itemName = ItemNameUtility.CleanName(itemName);
        return TryAddFish(FishFactory.CreateFromWildFish(itemName, ResolveSpeciesData(itemName)));
    }

    public bool TryAddFish(FishInstanceState fishState)
    {
        if (IsRewardLocked) return false;
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
        waterQuality.oxygen = Mathf.Max(0f, waterQuality.oxygen + amount);
        CommitWaterQualityChange();
        Debug.Log($"[RAS][{name}] O2: {before:0.00} â†’ {waterQuality.oxygen:0.00} (+{amount:0.00})");
        return true;
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

        for (int i = 0; i < storedFish.Count && i < spawnedFish.Count; i++)
        {
            FishInstanceState state = storedFish[i];
            GameObject fishObject = spawnedFish[i];
            if (state == null || fishObject == null || !state.isAlive)
                continue;

            float hungerValue = state.hunger;
            float chaseChance = hungerValue <= 40f ? 1f : 0.35f;
            if (UnityEngine.Random.value > chaseChance)
                continue;

            FishBrain brain = fishObject.GetComponent<FishBrain>();
            if (brain != null)
                brain.SetFoodTarget(food.transform, 0.45f);
        }
    }

    public bool TryConsumeFood(AquariumFoodPellet food, FishBrain eater)
    {
        if (IsRewardLocked) return false;
        if (food == null || eater == null)
            return false;

        int fishIndex = spawnedFish.IndexOf(eater.gameObject);
        if (fishIndex < 0 || fishIndex >= storedFish.Count)
            return false;

        // Kurangi food load saat pellet dimakan ikan
        // Diagram: saat Pakan dikonsumsi, efek NH3/pH dari pakan berkurang
        rasSimulator?.ConsumeFoodLoad(1f);

        // Terapkan efektivitas pakan dari kondisi air saat ini
        float baseReduction = food.HungerReduction;
        float efficiency    = rasSimulator != null ? rasSimulator.GetFeedEfficiency() : 1f;
        FeedFish(fishIndex, baseReduction * efficiency);
        rasSimulator?.RegisterFedFish();

        for (int i = 0; i < spawnedFish.Count; i++)
        {
            GameObject fishObject = spawnedFish[i];
            if (fishObject == null)
                continue;

            FishBrain brain = fishObject.GetComponent<FishBrain>();
            if (brain != null)
                brain.ClearFoodTarget(food.transform);
        }

        Destroy(food.gameObject);
        return true;
    }

    public bool InstallEquipment(EquipmentData equipment)
    {
        if (IsRewardLocked) return false;
        if (equipment == null || installedEquipment.Contains(equipment))
            return false;

        installedEquipment.Add(equipment);
        RefreshUI();
        AquariumStateChanged?.Invoke(this);
        return true;
    }

    public bool RemoveEquipment(EquipmentData equipment)
    {
        if (IsRewardLocked) return false;
        if (equipment == null)
            return false;

        bool removed = installedEquipment.Remove(equipment);
        if (removed)
        {
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
        CommitWaterQualityChange();
        // Hanya perbarui teks kualitas air, tidak re-spawn icon ikan
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

        if (index < spawnedFish.Count && spawnedFish[index] != null)
            Destroy(spawnedFish[index]);

        storedFish.RemoveAt(index);

        if (index < spawnedFish.Count)
            spawnedFish.RemoveAt(index);

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

        WaterThresholdProfile thresholds = GetWaterThresholdProfile();
        UpdateWaterIndicator(ammoniaIndicator, waterQuality.ammonia, "{0}: {1:0.00}", EvaluateMaxValue(waterQuality.ammonia, thresholds.maxAmmonia, ammoniaDangerMargin));
        UpdateWaterIndicator(oxygenIndicator, waterQuality.oxygen, "{0}: {1:0.0}", EvaluateMinValue(waterQuality.oxygen, thresholds.minOxygen, oxygenDangerMargin));
        UpdateWaterIndicator(salinityIndicator, waterQuality.salinity, "{0}: {1:0.0}", EvaluateRangeValue(waterQuality.salinity, thresholds.minSalinity, thresholds.maxSalinity, salinityDangerMargin));
        UpdateWaterIndicator(phIndicator, waterQuality.ph, "{0}: {1:0.0}", EvaluateRangeValue(waterQuality.ph, thresholds.minPh, thresholds.maxPh, phDangerMargin));
        UpdateWaterIndicator(temperatureIndicator, waterQuality.temperature, "{0}: {1:0.0} C", EvaluateRangeValue(waterQuality.temperature, thresholds.minTemperature, thresholds.maxTemperature, temperatureDangerMargin));

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

        foreach (EquipmentData equipment in installedEquipment)
        {
            if (equipment == null)
                continue;

            waterQuality.ammonia = Mathf.Max(0f, waterQuality.ammonia - Mathf.Max(0f, equipment.ammoniaReductionPerTick));
            waterQuality.oxygen = Mathf.Min(targetOxygen, waterQuality.oxygen + Mathf.Max(0f, equipment.oxygenIncreasePerTick));

            if (equipment.temperatureChangePerTick > 0f)
                waterQuality.temperature = Mathf.MoveTowards(waterQuality.temperature, equipment.targetTemperature, equipment.temperatureChangePerTick);
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
            waterQuality = WaterQualitySaveData.FromRuntime(waterQuality),
            installedEquipmentItemNames = CaptureInstalledEquipmentNames(),
            fish = new List<FishStateSaveData>()
        };

        for (int i = 0; i < storedFish.Count; i++)
        {
            FishInstanceState fishState = storedFish[i];
            if (fishState == null)
                continue;

            data.fish.Add(FishStateSaveData.FromRuntime(fishState));
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

                TryAddFish(fishSave.ToRuntimeState());
            }
        }

        waterQuality.Clamp();
        RefreshUI();
    }

    private int CountLivingFish()
    {
        int count = 0;
        foreach (FishInstanceState fish in storedFish)
        {
            if (fish != null && fish.isAlive)
                count++;
        }

        return count;
    }

    private int CountDeadFish()
    {
        int count = 0;
        foreach (FishInstanceState fish in storedFish)
        {
            if (fish != null && !fish.isAlive)
                count++;
        }

        return count;
    }

    private string BuildWarningText()
    {
        List<string> warnings = new List<string>();
        WaterThresholdProfile thresholds = GetWaterThresholdProfile();

        AddWarningIfNeeded(warnings, "Amonia", EvaluateMaxValue(waterQuality.ammonia, thresholds.maxAmmonia, ammoniaDangerMargin), true);
        AddWarningIfNeeded(warnings, "O2", EvaluateMinValue(waterQuality.oxygen, thresholds.minOxygen, oxygenDangerMargin), false);
        AddWarningIfNeeded(warnings, "Salinitas", EvaluateRangeValue(waterQuality.salinity, thresholds.minSalinity, thresholds.maxSalinity, salinityDangerMargin), waterQuality.salinity > thresholds.maxSalinity);
        AddWarningIfNeeded(warnings, "pH", EvaluateRangeValue(waterQuality.ph, thresholds.minPh, thresholds.maxPh, phDangerMargin), waterQuality.ph > thresholds.maxPh);
        AddWarningIfNeeded(warnings, "Temperatur", EvaluateRangeValue(waterQuality.temperature, thresholds.minTemperature, thresholds.maxTemperature, temperatureDangerMargin), waterQuality.temperature > thresholds.maxTemperature);

        return warnings.Count == 0 ? string.Empty : string.Join("\n", warnings);
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

    private WaterThresholdProfile GetWaterThresholdProfile()
    {
        WaterThresholdProfile profile = new WaterThresholdProfile
        {
            minOxygen = 4f,
            maxAmmonia = 1f,
            minTemperature = 23f,
            maxTemperature = 30f,
            minPh = 7.8f,
            maxPh = 8.5f,
            minSalinity = 30f,
            maxSalinity = 38f
        };

        bool foundSpeciesThreshold = false;

        for (int i = 0; i < storedFish.Count; i++)
        {
            FishInstanceState fish = storedFish[i];
            if (fish == null || !fish.isAlive)
                continue;

            AI_Fish_Data species = ResolveSpeciesData(fish.itemName);
            if (species == null)
                continue;

            if (!foundSpeciesThreshold)
            {
                profile.minOxygen = species.minOxygen;
                profile.maxAmmonia = species.maxAmmonia;
                profile.minTemperature = species.minTemperature;
                profile.maxTemperature = species.maxTemperature;
                profile.minPh = species.minPh;
                profile.maxPh = species.maxPh;
                profile.minSalinity = species.minSalinity;
                profile.maxSalinity = species.maxSalinity;
                foundSpeciesThreshold = true;
                continue;
            }

            profile.minOxygen = Mathf.Max(profile.minOxygen, species.minOxygen);
            profile.maxAmmonia = Mathf.Min(profile.maxAmmonia, species.maxAmmonia);
            profile.minTemperature = Mathf.Max(profile.minTemperature, species.minTemperature);
            profile.maxTemperature = Mathf.Min(profile.maxTemperature, species.maxTemperature);
            profile.minPh = Mathf.Max(profile.minPh, species.minPh);
            profile.maxPh = Mathf.Min(profile.maxPh, species.maxPh);
            profile.minSalinity = Mathf.Max(profile.minSalinity, species.minSalinity);
            profile.maxSalinity = Mathf.Min(profile.maxSalinity, species.maxSalinity);
        }

        profile.maxAmmonia = Mathf.Max(0f, profile.maxAmmonia);
        profile.minOxygen = Mathf.Max(0f, profile.minOxygen);
        profile.minTemperature = Mathf.Min(profile.minTemperature, profile.maxTemperature);
        profile.maxTemperature = Mathf.Max(profile.minTemperature, profile.maxTemperature);
        profile.minPh = Mathf.Min(profile.minPh, profile.maxPh);
        profile.maxPh = Mathf.Max(profile.minPh, profile.maxPh);
        profile.minSalinity = Mathf.Min(profile.minSalinity, profile.maxSalinity);
        profile.maxSalinity = Mathf.Max(profile.minSalinity, profile.maxSalinity);

        return profile;
    }

    private WaterIndicatorSeverity EvaluateMinValue(float value, float safeMinimum, float dangerMargin)
    {
        if (value >= safeMinimum)
            return WaterIndicatorSeverity.Safe;

        return value >= safeMinimum - Mathf.Abs(dangerMargin)
            ? WaterIndicatorSeverity.Danger
            : WaterIndicatorSeverity.Critical;
    }

    private WaterIndicatorSeverity EvaluateMaxValue(float value, float safeMaximum, float dangerMargin)
    {
        if (value <= safeMaximum)
            return WaterIndicatorSeverity.Safe;

        return value <= safeMaximum + Mathf.Abs(dangerMargin)
            ? WaterIndicatorSeverity.Danger
            : WaterIndicatorSeverity.Critical;
    }

    private WaterIndicatorSeverity EvaluateRangeValue(float value, float safeMinimum, float safeMaximum, float dangerMargin)
    {
        if (value >= safeMinimum && value <= safeMaximum)
            return WaterIndicatorSeverity.Safe;

        float margin = Mathf.Abs(dangerMargin);
        return value >= safeMinimum - margin && value <= safeMaximum + margin
            ? WaterIndicatorSeverity.Danger
            : WaterIndicatorSeverity.Critical;
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

    private void AddWarningIfNeeded(List<string> warnings, string label, WaterIndicatorSeverity severity, bool isHighDirection)
    {
        if (severity == WaterIndicatorSeverity.Safe)
            return;

        string level = severity == WaterIndicatorSeverity.Critical ? "kritikal" : "bahaya";
        string direction = isHighDirection ? "tinggi" : "rendah";
        warnings.Add($"{label} {direction} ({level})");
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
}
