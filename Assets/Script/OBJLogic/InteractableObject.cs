using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    // ✏️ DIHAPUS: private StarterAssetsInputs _Input;
    [SerializeField] private string itemName;
    private bool isBeingLookedAt = false;

    // ✏️ DIHAPUS: Start() karena tidak ada lagi GetComponent yang diperlukan

    void Update()
    {
        // ✏️ DIUBAH: _Input.Interact diganti PlayerInputManager.Instance.Interact
        if (isBeingLookedAt && PlayerInputManager.Instance != null && PlayerInputManager.Instance.Interact && InteractUIManager.Instance.OnTargeted)
        {
            InteractObject();
            Destroy(gameObject);
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