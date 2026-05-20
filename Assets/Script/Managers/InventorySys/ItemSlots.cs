using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlots : MonoBehaviour, IDropHandler
{
    public GameObject Item
    {
        get
        {
            if (transform.childCount > 0)
                return transform.GetChild(0).gameObject;

            return null;
        }
    }

    /// <summary>
    /// Tangani drop item ke dalam slot inventory atau quick-slot.
    /// Guard null/destroyed pada itemBeingDragged untuk mencegah MissingReferenceException.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop");

        GameObject dragged = DragDrop.itemBeingDragged;
        if (dragged == null)
        {
            Debug.LogWarning("[ItemSlots] OnDrop dipanggil tapi itemBeingDragged null atau sudah di-destroy.");
            return;
        }

        if (!Item)
        {
            dragged.transform.SetParent(transform);
            dragged.transform.localPosition = Vector2.zero;

            InventoryItemLogic logic = dragged.GetComponent<InventoryItemLogic>();
            if (logic != null)
                logic.IsNowInsideQcSlot = transform.CompareTag("QuickSlot");
        }
    }
}
