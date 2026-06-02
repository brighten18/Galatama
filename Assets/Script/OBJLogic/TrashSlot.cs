using UnityEngine;
using UnityEngine.EventSystems;

public class TrashSlot : MonoBehaviour, IDropHandler
{
    GameObject draggedItem
    {
        get { return DragDrop.itemBeingDragged; }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedItem != null && draggedItem.GetComponent<InventoryItemLogic>().isTrashable == true)
        {
            // âœï¸ DIPERBAIKI: Ganti DestroyImmediate dengan Destroy
            //               DestroyImmediate saat event berjalan menyebabkan error di OnEndDrag
            Destroy(draggedItem.gameObject);
            InventorySystem.Instance.ReCalculeList();
        }
    }
}
