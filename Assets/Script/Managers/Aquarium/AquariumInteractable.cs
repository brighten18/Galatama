using UnityEngine;

public class AquariumInteractable : InteractableObject
{
    [SerializeField] private AquariumSystem aquariumSystem;

    private void Awake()
    {
        itemName = "Aquarium";

        if (aquariumSystem == null)
            aquariumSystem = GetComponentInParent<AquariumSystem>();
    }

    protected override void HandleInteract()
    {
        PlayerInputManager.Instance.ResetInteractInput();

        if (aquariumSystem == null)
        {
            Debug.LogError("[AquariumInteractable] AquariumSystem belum diassign.");
            return;
        }

        aquariumSystem.OpenAquarium();
    }

    public override string GetItemName()
    {
        return "Aquarium";
    }
}
