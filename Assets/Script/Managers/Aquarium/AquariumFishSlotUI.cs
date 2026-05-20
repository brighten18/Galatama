using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class AquariumFishSlotUI : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Field lama dipertahankan untuk kompatibilitas inspector
    [SerializeField] private Image fishIcon;
    [SerializeField] private Text fishNameText;
    [SerializeField] private Text hungerText;
    [SerializeField] private Text healthText;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject filledState;
    [SerializeField] private GameObject emptyState;

    private AquariumSystem aquariumSystem;
    private int fishIndex = -1;
    private bool hasFish;
    private Image slotRaycastImage;

    // Nama ikan dan state aktif — disimpan agar bisa ditampilkan saat hover
    private string currentFishName;
    private FishInstanceState currentFishState;

    // Child item prefab yang di-instantiate sebagai icon visual
    private GameObject spawnedIconItem;
    private Canvas parentCanvas;
    private GameObject dragPreview;
    private bool isDragging;
    private bool suppressNextClick;

    private void Reset()
    {
        EnsureSlotRaycastImage();
    }

    private void Awake()
    {
        EnsureSlotRaycastImage();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Mengisi slot dengan data ikan. Instantiate prefab item sebagai child visual.
    /// </summary>
    public void SetSlot(AquariumSystem owner, int index, string itemName, Sprite icon)
    {
        SetSlot(owner, index, itemName, icon, null);
    }

    /// <summary>
    /// Mengisi slot dengan data ikan lengkap termasuk FishInstanceState.
    /// </summary>
    public void SetSlot(AquariumSystem owner, int index, string itemName, Sprite icon, FishInstanceState fishState)
    {
        EnsureSlotRaycastImage();

        aquariumSystem = owner;
        fishIndex = index;
        hasFish = !string.IsNullOrEmpty(itemName);

        // Simpan state aktif untuk dipakai saat hover
        currentFishName = itemName;
        currentFishState = fishState;

        ClearIconItem();

        if (hasFish)
        {
            GameObject prefab = Resources.Load<GameObject>(itemName);
            if (prefab != null)
            {
                spawnedIconItem = Instantiate(prefab, transform);
                spawnedIconItem.transform.localPosition = Vector3.zero;
                spawnedIconItem.name = itemName;

                DisableIconItemComponents(spawnedIconItem);

                Debug.Log($"[AquariumSlot] Slot {index} diisi: {itemName}");
            }
            else
            {
                Debug.LogWarning($"[AquariumSlot] Prefab '{itemName}' tidak ditemukan di Resources.");
            }

            if (fishNameText != null) fishNameText.text = itemName;

            if (hungerText != null)
                hungerText.text = fishState != null ? $"Lapar {fishState.hunger:0}/{fishState.maxHunger:0}" : string.Empty;

            if (healthText != null)
                healthText.text = fishState != null ? $"HP {fishState.health:0}/{fishState.maxHealth:0}" : string.Empty;

            if (statusText != null)
            {
                if (fishState == null)
                    statusText.text = string.Empty;
                else if (!fishState.isAlive)
                    statusText.text = "Mati";
                else if (fishState.isStressed)
                    statusText.text = "Stress";
                else if (fishState.HungerPercent >= 0.8f)
                    statusText.text = "Lapar";
                else
                    statusText.text = "Sehat";
            }
        }
        else
        {
            if (fishNameText != null) fishNameText.text = string.Empty;
            if (hungerText != null) hungerText.text = string.Empty;
            if (healthText != null) healthText.text = string.Empty;
            if (statusText != null) statusText.text = string.Empty;
        }

        if (filledState != null) filledState.SetActive(hasFish);
        if (emptyState != null) emptyState.SetActive(!hasFish);
    }

    /// <summary>
    /// Tampilkan ItemInfoPanel dan Pop_Up-FishStatus saat cursor masuk ke slot aquarium
    /// yang berisi ikan.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hasFish) return;

        // Tampilkan ItemInfoPanel dengan nama ikan
        GameObject itemInfoUI = InventorySystem.Instance?.ItemInfoUI;
        if (itemInfoUI != null)
        {
            itemInfoUI.SetActive(true);

            Text nameText = itemInfoUI.transform.Find("ItemName")?.GetComponent<Text>();
            Text descText = itemInfoUI.transform.Find("ItemDescription")?.GetComponent<Text>();
            Text funcText = itemInfoUI.transform.Find("ItemFunc")?.GetComponent<Text>();

            if (nameText != null) nameText.text = currentFishName;
            if (descText != null) descText.text = string.Empty;
            if (funcText != null) funcText.text = string.Empty;
        }

        // Tampilkan Pop_Up-FishStatus
        if (currentFishState != null)
            InventoryItemLogic.ShowFishStatusUI(currentFishName, currentFishState);
    }

    /// <summary>
    /// Sembunyikan panel info saat cursor keluar dari slot aquarium.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        GameObject itemInfoUI = InventorySystem.Instance?.ItemInfoUI;
        if (itemInfoUI != null)
            itemInfoUI.SetActive(false);

        InventoryItemLogic.HideFishStatusUI();
    }

    private void DisableIconItemComponents(GameObject iconItem)
    {
        InventoryItemLogic itemLogic = iconItem.GetComponent<InventoryItemLogic>();
        if (itemLogic != null)
            Destroy(itemLogic);

        DragDrop dragDrop = iconItem.GetComponent<DragDrop>();
        if (dragDrop != null)
            Destroy(dragDrop);

        CanvasGroup cg = iconItem.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        Image[] allImages = iconItem.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
            img.raycastTarget = false;

        Text[] allTexts = iconItem.GetComponentsInChildren<Text>(true);
        foreach (Text txt in allTexts)
            txt.raycastTarget = false;
    }

    /// <summary>
    /// Klik pada slot aquarium → kembalikan ikan ke inventory.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (suppressNextClick)
        {
            suppressNextClick = false;
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Sembunyikan popup sebelum ikan dipindah
        InventoryItemLogic.HideFishStatusUI();

        GameObject itemInfoUI = InventorySystem.Instance?.ItemInfoUI;
        if (itemInfoUI != null) itemInfoUI.SetActive(false);

        TakeFishToInventory();
    }

    /// <summary>
    /// Memindahkan ikan dari slot aquarium kembali ke inventory player.
    /// </summary>
    public void TakeFishToInventory()
    {
        AquariumSystem targetAquarium = aquariumSystem != null
            ? aquariumSystem
            : AquariumSystem.CurrentOpen;

        if (!hasFish || targetAquarium == null)
        {
            Debug.Log($"[AquariumSlot] Klik diabaikan — hasFish:{hasFish}, aquarium:{targetAquarium != null}");
            return;
        }

        Debug.Log($"[AquariumSlot] Mengambil ikan dari slot index {fishIndex}...");
        targetAquarium.TryMoveFishToInventory(this);
    }

    /// <summary>
    /// Drop item dari inventory ke slot aquarium ini.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        AquariumSystem targetAquarium = aquariumSystem != null
            ? aquariumSystem
            : AquariumSystem.CurrentOpen;

        if (targetAquarium == null)
        {
            Debug.LogWarning("[AquariumFishSlotUI] AquariumSystem tidak ditemukan.");
            return;
        }

        GameObject draggedItem = DragDrop.itemBeingDragged != null
            ? DragDrop.itemBeingDragged
            : eventData.pointerDrag;

        if (draggedItem == null)
        {
            Debug.LogWarning("[AquariumFishSlotUI] Tidak ada item yang sedang di-drag.");
            return;
        }

        targetAquarium.TryAddFishFromInventoryItem(draggedItem);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!hasFish) return;

        parentCanvas = parentCanvas != null ? parentCanvas : GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning("[AquariumSlot] Canvas tidak ditemukan, drag aquarium dibatalkan.");
            return;
        }

        isDragging = true;
        suppressNextClick = true;

        // Sembunyikan popup saat mulai drag
        InventoryItemLogic.HideFishStatusUI();

        CreateDragPreview(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragPreview == null) return;
        dragPreview.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        DestroyDragPreview();

        ItemSlots targetSlot = FindInventorySlotUnderPointer(eventData);
        if (targetSlot == null)
        {
            Debug.Log("[AquariumSlot] Drop dibatalkan, target bukan slot inventory.");
            return;
        }

        AquariumSystem targetAquarium = aquariumSystem != null
            ? aquariumSystem
            : AquariumSystem.CurrentOpen;

        if (targetAquarium == null)
        {
            Debug.LogWarning("[AquariumSlot] AquariumSystem tidak ditemukan.");
            return;
        }

        targetAquarium.TryMoveFishToInventory(this, targetSlot.gameObject);
    }

    private void ClearIconItem()
    {
        if (spawnedIconItem != null)
        {
            Destroy(spawnedIconItem);
            spawnedIconItem = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private void EnsureSlotRaycastImage()
    {
        slotRaycastImage = GetComponent<Image>();
        if (slotRaycastImage == null)
        {
            slotRaycastImage = gameObject.AddComponent<Image>();
            slotRaycastImage.color = new Color(1f, 1f, 1f, 0.01f);
        }

        slotRaycastImage.raycastTarget = true;
    }

    private void CreateDragPreview(Vector2 screenPosition)
    {
        DestroyDragPreview();

        Sprite sprite = GetCurrentIconSprite();
        if (sprite == null) return;

        dragPreview = new GameObject("AquariumDragPreview", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragPreview.transform.SetParent(parentCanvas.transform, false);
        dragPreview.transform.position = screenPosition;

        RectTransform previewRect = dragPreview.GetComponent<RectTransform>();
        previewRect.sizeDelta = ((RectTransform)transform).rect.size;

        Image previewImage = dragPreview.GetComponent<Image>();
        previewImage.sprite = sprite;
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;

        CanvasGroup previewGroup = dragPreview.GetComponent<CanvasGroup>();
        previewGroup.alpha = 0.75f;
        previewGroup.blocksRaycasts = false;
        previewGroup.interactable = false;
    }

    private void DestroyDragPreview()
    {
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }

    private Sprite GetCurrentIconSprite()
    {
        if (spawnedIconItem != null)
        {
            Image spawnedImage = spawnedIconItem.GetComponent<Image>();
            if (spawnedImage != null && spawnedImage.sprite != null)
                return spawnedImage.sprite;
        }

        return fishIcon != null ? fishIcon.sprite : null;
    }

    private ItemSlots FindInventorySlotUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return null;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            ItemSlots slot = result.gameObject.GetComponentInParent<ItemSlots>();
            if (slot != null && slot.Item == null)
                return slot;
        }

        return null;
    }
}
