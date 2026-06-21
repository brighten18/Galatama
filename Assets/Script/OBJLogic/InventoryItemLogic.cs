using UnityEngine;
using UnityEngine.UI;  
using UnityEngine.EventSystems;

public class InventoryItemLogic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool isTrashable;

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
        InventorySystem inv = InventorySystem.Instance;
        if (inv != null)
        {
            inv.ShowItemInfoPanel(thisName, thisDescription, thisFunctionality);
        }

        // --- Pop_Up-FishStatus ---
        FishRuntimeData fishData = GetComponent<FishRuntimeData>();
        if (fishData != null && fishData.State != null)
        {
            // Ambil sprite langsung dari Image komponen item ini agar gambar selalu sesuai
            Image itemImage = GetComponent<Image>();
            ShowFishStatusUI(thisName, fishData.State, itemImage != null ? itemImage.sprite : null);
        }
        else
            HideFishStatusUI();
    }
 
    /// <summary>
    /// Sembunyikan semua panel info saat cursor keluar dari item.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        InventorySystem inv = InventorySystem.Instance;
        if (inv != null)
            inv.HideItemInfoPanel();

        HideFishStatusUI();
    }

    /// <summary>
    /// Isi dan tampilkan Pop_Up-FishStatus menggunakan field yang sudah ter-assign
    /// di InventorySystem Inspector. Dijadikan static agar bisa dipanggil dari
    /// AquariumFishSlotUI tanpa perlu instance InventoryItemLogic.
    /// Parameter <paramref name="icon"/> opsional — jika diisi, sprite FishImage diperbarui
    /// sehingga gambar selalu sesuai ikan yang sedang di-hover.
    /// </summary>
    public static void ShowFishStatusUI(string fishName, FishInstanceState state, Sprite icon = null)
    {
        InventorySystem inv = InventorySystem.Instance;
        if (inv == null || inv.FishThingsUI == null) return;

        // Isi teks sebelum SetActive agar tidak ada frame flickering
        if (inv.FishName != null)
            inv.FishName.text = fishName;

        // Perbarui gambar ikan agar sesuai dengan ikan yang sedang di-hover
        if (inv.FishImage != null && icon != null)
            inv.FishImage.sprite = icon;

        if (inv.FishHunger != null)
            inv.FishHunger.text = $"Kenyang: {state.hunger:0}/{state.maxHunger:0}";

        if (inv.FishHealth != null)
            inv.FishHealth.text = $"HP: {state.health:0}/{state.maxHealth:0}";

        if (inv.FishStatus != null)
        {
            if (!state.isAlive)
                inv.FishStatus.text = "Mati";
            else if (state.isStressed)
                inv.FishStatus.text = "Stress";
            else if (state.HungerPercent <= 0.2f)
                inv.FishStatus.text = "Lapar";
            else
                inv.FishStatus.text = "Kenyang";
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
