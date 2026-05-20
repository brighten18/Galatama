using UnityEngine;
using UnityEngine.EventSystems;

public class AquariumDropArea : MonoBehaviour, IDropHandler
{
    [SerializeField] private AquariumSystem aquariumSystem;

    private void Awake()
    {
        ResolveAquariumSystem();
    }

    public void OnDrop(PointerEventData eventData)
    {
        AquariumSystem targetAquarium = ResolveAquariumSystem();
        if (targetAquarium == null)
        {
            Debug.LogWarning("[AquariumDropArea] AquariumSystem tidak ditemukan. Assign field Aquarium System atau buka aquarium lewat interaksi.");
            return;
        }

        GameObject draggedItem = DragDrop.itemBeingDragged != null
            ? DragDrop.itemBeingDragged
            : eventData.pointerDrag;

        if (draggedItem == null)
        {
            Debug.LogWarning("[AquariumDropArea] Tidak ada item yang sedang di-drag.");
            return;
        }

        targetAquarium.TryAddFishFromInventoryItem(draggedItem);
    }

    private AquariumSystem ResolveAquariumSystem()
    {
        if (AquariumSystem.CurrentOpen != null)
            return AquariumSystem.CurrentOpen;

        if (aquariumSystem != null)
            return aquariumSystem;

        aquariumSystem = GetComponentInParent<AquariumSystem>();
        if (aquariumSystem != null)
            return aquariumSystem;

        aquariumSystem = FindFirstObjectByType<AquariumSystem>();
        return aquariumSystem;
    }
}
