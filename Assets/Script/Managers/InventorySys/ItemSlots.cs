using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlots : MonoBehaviour, IDropHandler
{
 
    public GameObject Item
    {
        get
        {
            if (transform.childCount > 0 )
            {
                return transform.GetChild(0).gameObject;
            }
 
            return null;
        }
    } 
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop");
 
        if (!Item)
        {
 
            DragDrop.itemBeingDragged.transform.SetParent(transform);
            DragDrop.itemBeingDragged.transform.localPosition = new Vector2(0, 0);

            if (!transform.CompareTag("QuickSlot"))
            {
                DragDrop.itemBeingDragged.GetComponent<InventoryItemLogic>().IsNowInsideQcSlot = false;
            }

            if (transform.CompareTag("QuickSlot"))
            {
                DragDrop.itemBeingDragged.GetComponent<InventoryItemLogic>().IsNowInsideQcSlot = true;
            }
 
        }
 
 
    }
}
