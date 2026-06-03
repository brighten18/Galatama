using UnityEngine;
using UnityEngine.UI;  
using UnityEngine.EventSystems;

public class InventoryItemLogic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool isTrashable;

    // Referensi panel info inventory
    private GameObject itemInfoUI;
    private Text itemInfoUI_itemName;
    private Text itemInfoUI_itemDescription;
    private Text itemInfoUI_itemFunctionality;

    private DragDrop dragDrop;
 
    public string thisName, thisDescription, thisFunctionality;

    public bool isEquippable;
    public bool IsNowInsideQcSlot;
    public bool isSelected;

    public bool CanMoveToQuickSlot()
    {
        return isEquippable || GetComponent<FishRuntimeData>() != null;
    }

    private void Start()
    {
        dragDrop = GetComponent<DragDrop>();

        itemInfoUI = InventorySystem.Instance.ItemInfoUI;
        itemInfoUI_itemName = itemInfoUI.transform.Find("ItemName").GetComponent<Text>();
        itemInfoUI_itemDescription = itemInfoUI.transform.Find("ItemDescription").GetComponent<Text>();
        itemInfoUI_itemFunctionality = itemInfoUI.transform.Find("ItemFunc").GetComponent<Text>();
    }

    private void Update()
    {
        if (dragDrop == null) return;
        dragDrop.enabled = !isSelected;
    }

    /// <summary>
    /// Tampilkan ItemInfoPanel dan Pop_Up-FishStatus saat cursor masuk ke item.
    /// Seluruh teks fish status diisi lewat field InventorySystem yang sudah ter-assign
    /// di Inspector, sehingga tidak ada jalur Find() ganda yang bisa bertabrakan.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // --- ItemInfoPanel ---
        itemInfoUI.SetActive(true);
        itemInfoUI_itemName.text = thisName;
        itemInfoUI_itemDescription.text = thisDescription;
        itemInfoUI_itemFunctionality.text = thisFunctionality;

        // --- Pop_Up-FishStatus ---
        FishRuntimeData fishData = GetComponent<FishRuntimeData>();
        if (fishData != null && fishData.State != null)
            ShowFishStatusUI(thisName, fishData.State);
        else
            HideFishStatusUI();
    }
 
    /// <summary>
    /// Sembunyikan semua panel info saat cursor keluar dari item.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        itemInfoUI.SetActive(false);
        HideFishStatusUI();
    }

    /// <summary>
    /// Isi dan tampilkan Pop_Up-FishStatus menggunakan field yang sudah ter-assign
    /// di InventorySystem Inspector. Dijadikan static agar bisa dipanggil dari
    /// AquariumFishSlotUI tanpa perlu instance InventoryItemLogic.
    /// </summary>
    public static void ShowFishStatusUI(string fishName, FishInstanceState state)
    {
        InventorySystem inv = InventorySystem.Instance;
        if (inv == null || inv.FishThingsUI == null) return;

        // Isi teks sebelum SetActive agar tidak ada frame flickering
        if (inv.FishName != null)
            inv.FishName.text = fishName;

        if (inv.FishHunger != null)
            inv.FishHunger.text = $"Lapar: {state.hunger:0}/{state.maxHunger:0}";

        if (inv.FishHealth != null)
            inv.FishHealth.text = $"HP: {state.health:0}/{state.maxHealth:0}";

        if (inv.FishStatus != null)
        {
            if (!state.isAlive)
                inv.FishStatus.text = "Mati";
            else if (state.isStressed)
                inv.FishStatus.text = "Stress";
            else if (state.HungerPercent >= 0.8f)
                inv.FishStatus.text = "Lapar";
            else
                inv.FishStatus.text = "Sehat";
        }

        inv.FishThingsUI.SetActive(true);
    }

    /// <summary>
    /// Sembunyikan Pop_Up-FishStatus. Dijadikan static agar bisa dipanggil
    /// dari kelas lain tanpa instance.
    /// </summary>
    public static void HideFishStatusUI()
    {
        InventorySystem inv = InventorySystem.Instance;
        if (inv != null && inv.FishThingsUI != null)
            inv.FishThingsUI.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (CanMoveToQuickSlot() && !IsNowInsideQcSlot && EquipSystem.Instance.CheckIsfFull() == false)
            {
                EquipSystem.Instance.AddToQuickSlots(gameObject);
                IsNowInsideQcSlot = true;
            }
        }
    }
 
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
        }
    }
}
