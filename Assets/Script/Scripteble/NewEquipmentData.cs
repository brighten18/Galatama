using UnityEngine;

public enum EquipmentType
{
    Default,
    FishingNet,
    Tool,
    Trap
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
}