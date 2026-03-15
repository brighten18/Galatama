using UnityEngine;
using UnityEngine.EventSystems;

public class TrashSlot : MonoBehaviour, IDropHandler
{
    // ✏️ DIHAPUS: trash_closed, trash_opened, imageComponent, OnPointerEnter, OnPointerExit

    GameObject draggedItem
    {
        get { return DragDrop.itemBeingDragged; }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedItem != null && draggedItem.GetComponent<InventoryItemLogic>().isTrashable == true)
        {
            DestroyImmediate(draggedItem.gameObject);
            InventorySystem.Instance.ReCalculeList();
        }
    }
}