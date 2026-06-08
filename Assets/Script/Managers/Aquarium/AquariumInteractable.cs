using UnityEngine;

public class AquariumInteractable : InteractableObject
{
    [SerializeField] private AquariumSystem aquariumSystem;

    [Header("Held Item Actions")]
    [SerializeField] private float defaultCooldownSeconds = 3f;
    [SerializeField] private float defaultTargetPh = 8f;
    [SerializeField] private float defaultTargetAmmonia = 0f;
    [SerializeField] private float defaultSaltIncrease = 2f;
    [SerializeField] private float defaultFeedHungerReduction = 100f;
    [SerializeField] private int defaultPelletSpawnCount = 8;
    [SerializeField] private GameObject foodPelletPrefab;

    private AquariumActionCooldowns cooldowns;

    private void Awake()
    {
        base.Awake();
        itemName = "Aquarium";

        if (aquariumSystem == null)
            aquariumSystem = GetComponentInParent<AquariumSystem>();

        cooldowns = GetComponent<AquariumActionCooldowns>();
        if (cooldowns == null)
            cooldowns = gameObject.AddComponent<AquariumActionCooldowns>();
    }

    protected override void HandleInteract()
    {
        PlayerInputManager.Instance.ResetInteractInput();

        if (aquariumSystem == null)
        {
            Debug.LogError("[AquariumInteractable] AquariumSystem belum diassign.");
            return;
        }

        if (TryUseHeldItemOnAquarium())
            return;

        aquariumSystem.OpenAquarium();
    }

    public override string GetItemName()
    {
        return (aquariumSystem != null && aquariumSystem.IsRewardLocked)
            ? string.Empty
            : "Aquarium";
    }

    /// <summary>
    /// Menyembunyikan prompt dan highlight saat aquarium reward masih terkunci.
    /// </summary>
    public override void SetLookingAt(bool value)
    {
        if (value && aquariumSystem != null && aquariumSystem.IsRewardLocked) return;
        base.SetLookingAt(value);
    }

    private bool TryUseHeldItemOnAquarium()
    {
        if (EquipSystem.Instance == null || !EquipSystem.Instance.isnowEquipped)
            return false;

        EquipmentData equipment = EquipSystem.Instance.GetEquippedData();
        EquipmentType equippedType = equipment != null
            ? equipment.itemType
            : EquipSystem.Instance.GetEquippedType();

        switch (equippedType)
        {
            case EquipmentType.PhControl:
                return UsePhControl(equipment);
            case EquipmentType.AmoniaControl:
                return UseAmoniaControl(equipment);
            case EquipmentType.SaltControl:
                return UseSaltControl(equipment);
            case EquipmentType.FishFood:
                return UseFishFood(equipment);
            default:
                return false;
        }
    }

    private bool UsePhControl(EquipmentData equipment)
    {
        string key = BuildCooldownKey(equipment, "ph");
        if (!CheckCooldown(key))
            return true;

        float targetPh = equipment != null ? equipment.targetPh : defaultTargetPh;
        aquariumSystem.SetPh(targetPh);
        StartCooldown(key, equipment);
        ConsumeHeldItem(equipment);
        return true;
    }

    private bool UseAmoniaControl(EquipmentData equipment)
    {
        string key = BuildCooldownKey(equipment, "amonia");
        if (!CheckCooldown(key))
            return true;

        float targetAmmonia = equipment != null ? equipment.targetAmmonia : defaultTargetAmmonia;
        aquariumSystem.SetAmmonia(targetAmmonia);
        StartCooldown(key, equipment);
        ConsumeHeldItem(equipment);
        return true;
    }

    private bool UseSaltControl(EquipmentData equipment)
    {
        float amount = equipment != null && !Mathf.Approximately(equipment.salinityChange, 0f)
            ? equipment.salinityChange
            : defaultSaltIncrease;

        aquariumSystem.ChangeSalinity(amount);
        ConsumeHeldItem(equipment);
        return true;
    }

    private bool UseFishFood(EquipmentData equipment)
    {
        float feedValue = equipment != null && equipment.feedHungerReduction > 0f
            ? equipment.feedHungerReduction
            : defaultFeedHungerReduction;

        int pelletCount = equipment != null && equipment.pelletSpawnCount > 0
            ? equipment.pelletSpawnCount
            : defaultPelletSpawnCount;

        if (!aquariumSystem.SpawnFoodPellets(foodPelletPrefab, feedValue, pelletCount))
            return true;

        ConsumeHeldItem(equipment);
        return true;
    }

    private void ConsumeHeldItem(EquipmentData equipment)
    {
        if (EquipSystem.Instance == null)
            return;

        string expectedItemName = equipment != null ? equipment.itemName : EquipSystem.Instance.GetEquippedItemName();
        if (!EquipSystem.Instance.TryConsumeSelectedItem(expectedItemName))
            Debug.LogWarning("[Aquarium] Efek berhasil, tetapi item gagal dikonsumsi dari quickslot: " + expectedItemName);
    }

    private bool CheckCooldown(string key)
    {
        if (cooldowns.IsReady(key))
            return true;

        Debug.Log($"[Aquarium] Item masih cooldown {cooldowns.GetRemaining(key):0.0}s.");
        return false;
    }

    private void StartCooldown(string key, EquipmentData equipment)
    {
        float cooldown = equipment != null && equipment.cooldownSeconds > 0f
            ? equipment.cooldownSeconds
            : defaultCooldownSeconds;

        cooldowns.StartCooldown(key, cooldown);
    }

    private string BuildCooldownKey(EquipmentData equipment, string fallback)
    {
        string itemKey = equipment != null && !string.IsNullOrEmpty(equipment.itemName)
            ? equipment.itemName
            : fallback;

        return $"{GetInstanceID()}:{itemKey}";
    }
}
