using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] protected string itemName;
    protected bool isBeingLookedAt = false;

    protected virtual void Update()
    {
        if (CanInteract())
        {
            HandleInteract();
        }
    }

    protected bool CanInteract()
    {
        return isBeingLookedAt &&
               PlayerInputManager.Instance != null &&
               PlayerInputManager.Instance.Interact &&
               InteractUIManager.Instance != null &&
               InteractUIManager.Instance.IsCurrentInteractable(this);
    }

    protected virtual void HandleInteract()
    {
        PlayerInputManager.Instance.ResetInteractInput();

        if (!InventorySystem.Instance.CheckFull())
        {
            InventorySystem.Instance.AddItemToInventory(itemName);
            InteractObject();
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory penuh, tidak bisa mengambil " + itemName);
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
