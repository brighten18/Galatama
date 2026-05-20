using UnityEngine;

public class FishBase : MonoBehaviour
{
    [Header("Fish Data")]
    public AI_Fish_Data fishData;

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
            Debug.Log("[FishBase] Inventory penuh, tidak bisa menangkap: " + fishData.ItemName);
            return;
        }

        FishInstanceState fishState = FishFactory.CreateFromWildFish(fishData.ItemName, fishData);
        if (!InventorySystem.Instance.TryAddFishStateToInventory(fishState))
        {
            Debug.LogError("[FishBase] Gagal membuat state ikan saat tertangkap: " + fishData.ItemName);
            return;
        }

        Debug.Log("[FishBase] Ikan tertangkap: " + fishData.ItemName);

        OnCaughtBehavior();
        Destroy(gameObject);
    }

    public virtual void OnCaughtBehavior()
    {
        FishBrain fishBrain = GetComponent<FishBrain>();
        if (fishBrain != null)
        {
            fishBrain.OnCaptured(false);
        }
    }
}
