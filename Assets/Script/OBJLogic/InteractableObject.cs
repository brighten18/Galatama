 using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    // ✏️ DIHAPUS: private StarterAssetsInputs _Input;
    [SerializeField] private string itemName;
    private bool isBeingLookedAt = false;

    // ✏️ DIHAPUS: Start() karena tidak ada lagi GetComponent yang diperlukan
    void Update()
    {
        if (isBeingLookedAt && PlayerInputManager.Instance != null && PlayerInputManager.Instance.Interact && InteractUIManager.Instance.OnTargeted)
        { 
            if(!InventorySystem.Instance.CheckFull())
            {            
                PlayerInputManager.Instance.ResetInteractInput();
                InteractObject();
                Destroy(gameObject);
                InventorySystem.Instance.AddItemToInventory(itemName);
            }else
            {
                Debug.Log("Inventory penuh, tidak bisa mengambil " + itemName);
            }
        }
    }    

    public void SetLookingAt(bool value)
    {
        isBeingLookedAt = value;
    }

    public virtual string GetItemName()
    {
        return itemName;
    }

    public virtual void InteractObject()
    {
        Debug.Log($"Berinteraksi dengan {itemName}");
    }
}