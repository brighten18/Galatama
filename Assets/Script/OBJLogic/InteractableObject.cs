using UnityEngine;
using StarterAssets;

public class InteractableObject : MonoBehaviour
{
    private StarterAssetsInputs _Input;
    [SerializeField] private string itemName;
    private bool isBeingLookedAt = false;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _Input = player.GetComponent<StarterAssetsInputs>();
    }

    void Update()
    {
        if (isBeingLookedAt && _Input != null && _Input.Interact && InteractUIManager.Instance.OnTargeted)
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