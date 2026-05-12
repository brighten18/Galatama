using UnityEngine;

public class FishBase : MonoBehaviour
{
    [Header("Fish Data")]
    public FishData fishData;

    // dipanggil oleh FishingNet saat ikan tertangkap
    public virtual void GetCaught()
    {
        if (fishData == null)
        {
            Debug.LogError("[FishBase] fishData belum diassign pada: " + gameObject.name);
            return;
        }

        if (InventorySystem.Instance.CheckFull())
        {
            Debug.Log("[FishBase] Inventory penuh, tidak bisa menangkap: " + fishData.itemName);
            return;
        }

        InventorySystem.Instance.AddItemToInventory(fishData.itemName);
        Debug.Log("[FishBase] Ikan tertangkap: " + fishData.itemName);
        Destroy(gameObject);
    }

    // method kosong untuk AI nanti, override di subclass
    public virtual void OnCaughtBehavior() { }
}