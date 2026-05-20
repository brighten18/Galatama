using UnityEngine;

public enum EquipmentType
{
    Default,
    FishingNet,
    Tool,
    Trap
}

public enum AquariumEquipmentRole
{
    None,
    Filter,
    Aerator,
    Heater,
    Chiller
}

[CreateAssetMenu(fileName = "EquipmentData", menuName = "Equipment/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    public string itemName;
    // DITAMBAH: tipe item untuk validasi
    public EquipmentType itemType;
    public GameObject modelPrefab;
    public Vector3 localPosition;
    public Vector3 localRotation;
    public Vector3 localScale = Vector3.one;

    [Header("Aquarium RAS Effect")]
    public AquariumEquipmentRole aquariumRole = AquariumEquipmentRole.None;
    public float ammoniaReductionPerTick;
    public float oxygenIncreasePerTick;
    public float temperatureChangePerTick;
    public float targetTemperature = 26f;
}
