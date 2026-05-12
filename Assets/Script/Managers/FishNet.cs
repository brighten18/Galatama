using UnityEngine;

public class FishingNet : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // cek tag ikan
        if (!other.CompareTag("Fish")) return;

        // cek apakah yang diequip adalah jaring
        if (EquipSystem.Instance.GetEquippedType() != EquipmentType.FishingNet)
        {
            Debug.Log("[FishingNet] Item yang diequip bukan jaring.");
            return;
        }

        FishBase fish = other.GetComponent<FishBase>();
        if (fish == null)
        {
            Debug.LogError("[FishingNet] Object dengan tag Fish tidak punya FishBase: " + other.name);
            return;
        }

        fish.GetCaught();
    }
}