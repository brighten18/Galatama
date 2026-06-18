using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Mengelola satu slot ikan di UI aquarium.
///
/// DESAIN:
///   - Field fishNameText / hungerText / healthText / statusText di sini mengarah ke
///     Pop_Up-FishStatus yang merupakan panel SHARED. Panel itu HANYA diisi saat hover
///     (OnPointerEnter) â€” TIDAK boleh ditulis saat RefreshUI/SetSlot supaya slot lain
///     tidak menimpa tulisan slot sebelumnya.
///   - Icon ikan ditampilkan melalui prefab yang di-spawn sebagai child slot.
///   - filledState / emptyState adalah child GameObject opsional untuk mengubah tampilan
///     slot saat berisi / kosong (bisa null jika tidak dipakai).
/// </summary>
public class AquariumFishSlotUI : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Field ini dipertahankan di Inspector untuk kompatibilitas, tapi TIDAK digunakan
    // saat RefreshUI â€” hanya dipakai saat hover (OnPointerEnter).
    [SerializeField] private Image fishIcon;
    [SerializeField] private Text fishNameText;
    [SerializeField] private Text hungerText;
    [SerializeField] private Text healthText;
    [SerializeField] private Text statusText;

    [Tooltip("Aktifkan saat slot berisi ikan (opsional)")]
    [SerializeField] private GameObject filledState;
    [Tooltip("Aktifkan saat slot kosong (opsional)")]
    [SerializeField] private GameObject emptyState;

    // â”€â”€â”€ Runtime state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€...
    private AquariumSystem aquariumSystem;
    private int fishIndex = -1;
    private bool hasFish;
    private Image slotRaycastImage;

    private string currentFishName;
    private FishInstanceState currentFishState;

    // Icon visual prefab yang di-spawn sebagai child
    private GameObject spawnedIconItem;
    // Nama prefab yang sedang ditampilkan â€” untuk menghindari Destroy+Instantiate berulang
    private string spawnedPrefabName;

    private Canvas parentCanvas;
    private GameObject dragPreview;
    private bool isDragging;
    private bool suppressNextClick;

    // â”€â”€â”€ Unity lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â...

    private void Reset()
    {
        EnsureSlotRaycastImage();
    }

    private void Awake()
    {
        EnsureSlotRaycastImage();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    // â”€â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€...

    /// <summary>
    /// Ikat slot ke AquariumSystem tertentu tanpa mereset data ikan.
    /// Dipanggil setiap RefreshUI().
    /// </summary>
    public void BindAquariumSystem(AquariumSystem owner)
    {
        aquariumSystem = owner;
    }

    /// <summary>
    /// Overload tanpa FishInstanceState (slot kosong).
    /// </summary>
    public void SetSlot(AquariumSystem owner, int index, string itemName, Sprite icon)
    {
        SetSlot(owner, index, itemName, icon, null);
    }

    /// <summary>
    /// Perbarui tampilan slot. Hanya mengelola:
    ///   1. Icon prefab sebagai child (spawn/hapus jika nama berubah)
    ///   2. filledState / emptyState
    ///
    /// TIDAK menulis ke fishNameText/hungerText/healthText/statusText di sini
    /// karena field tersebut mengarah ke panel SHARED (Pop_Up-FishStatus) yang
    /// akan ditimpa oleh slot-slot lain jika ditulis setiap RefreshUI.
    /// Penulisan panel tersebut dilakukan hanya di OnPointerEnter.
    /// </summary>
    public void SetSlot(AquariumSystem owner, int index, string itemName, Sprite icon, FishInstanceState fishState)
    {
        EnsureSlotRaycastImage();

        aquariumSystem = owner;
        fishIndex = index;
        hasFish = !string.IsNullOrEmpty(itemName);

        currentFishName = itemName;
        currentFishState = fishState;

        // Perbarui icon prefab hanya jika nama berubah
        RefreshIconItem(itemName);

        // Tampilkan/sembunyikan state visual slot
        if (filledState != null) filledState.SetActive(hasFish);
        if (emptyState != null)  emptyState.SetActive(!hasFish);
    }

    // â”€â”€â”€ Pointer events â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”...

    /// <summary>
    /// Tampilkan Pop_Up-FishStatus saat hover â€” satu-satunya tempat panel shared diisi.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hasFish) return;

        // Panel info item â€” isi nama, deskripsi status, dan fungsi berdasarkan state ikan
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory != null)
        {
            string description = string.Empty;
            if (currentFishState != null)
            {
                string statusLabel = !currentFishState.isAlive ? "Mati"
                    : currentFishState.isStressed ? "Stress"
                    : currentFishState.HungerPercent <= 0.2f ? "Lapar"
                    : "Kenyang";
                description = $"HP: {currentFishState.health:0}/{currentFishState.maxHealth:0} | Status: {statusLabel}";
            }

            string functionality = currentFishState != null
                ? $"Kenyang: {currentFishState.hunger:0}/{currentFishState.maxHunger:0} | Klik untuk pindah ke inventory"
                : "Klik untuk pindah ke inventory";

            inventory.ShowItemInfoPanel(currentFishName, description, functionality);
        }

        // Pop_Up-FishStatus
        if (currentFishState != null)
            InventoryItemLogic.ShowFishStatusUI(currentFishName, currentFishState);
    }

    /// <summary>
    /// Sembunyikan panel info saat hover keluar.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory != null)
            inventory.HideItemInfoPanel();
        InventoryItemLogic.HideFishStatusUI();
    }

    /// <summary>Klik kiri â†’ kembalikan ikan ke inventory.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (suppressNextClick)
        {
            suppressNextClick = false;
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left) return;

        InventoryItemLogic.HideFishStatusUI();
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory != null)
            inventory.HideItemInfoPanel();

        TakeFishToInventory();
    }

    /// <summary>Pindahkan ikan dari slot aquarium ke inventory.</summary>
    public void TakeFishToInventory()
    {
        AquariumSystem target = ResolveAquarium();
        if (!hasFish || target == null)
        {
            Debug.Log($"[AquariumSlot] Klik diabaikan â€” hasFish:{hasFish}, aquarium:{target != null}");
            return;
        }

        Debug.Log($"[AquariumSlot] Mengambil ikan dari slot index {fishIndex}â€¦");
        target.TryMoveFishToInventory(this);
    }

    /// <summary>Drop item inventory ke slot aquarium ini, atau tukar antar slot aquarium.</summar...
    public void OnDrop(PointerEventData eventData)
    {
        AquariumSystem target = ResolveAquarium();
        if (target == null)
        {
            Debug.LogWarning("[AquariumFishSlotUI] AquariumSystem tidak ditemukan.");
            return;
        }

        // Cek apakah drag berasal dari slot aquarium lain (swap antar slot aquarium)
        if (isDraggingFromSlot != null && isDraggingFromSlot != this)
        {
            AquariumFishSlotUI sourceSlot = isDraggingFromSlot;
            AquariumSystem sourceAquarium = sourceSlot.ResolveAquarium();

            if (sourceAquarium == target)
            {
                int sourceIndex = FindSlotIndex(target, sourceSlot);
                int destIndex   = FindSlotIndex(target, this);

                if (sourceIndex >= 0 && destIndex >= 0)
                {
                    target.SwapFish(sourceIndex, destIndex);
                    Debug.Log($"[AquariumSlot] Swap ikan: slot {sourceIndex} â†” slot {destIndex}");
                    return;
                }
            }
        }

        // Drop dari inventory
        GameObject draggedItem = DragDrop.itemBeingDragged != null
            ? DragDrop.itemBeingDragged
            : eventData.pointerDrag;

        if (draggedItem == null)
        {
            Debug.LogWarning("[AquariumFishSlotUI] Tidak ada item yang sedang di-drag.");
            return;
        }

        target.TryAddFishFromInventoryItem(draggedItem);
    }

    // Slot yang sedang di-drag (static agar semua slot bisa melihatnya)
    private static AquariumFishSlotUI isDraggingFromSlot;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!hasFish) return;

        parentCanvas = parentCanvas != null ? parentCanvas : GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning("[AquariumSlot] Canvas tidak ditemukan, drag dibatalkan.");
            return;
        }

        isDragging = true;
        isDraggingFromSlot = this;
        suppressNextClick = true;
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
        isDraggingFromSlot = null;
        DestroyDragPreview();

        ItemSlots targetSlot = FindInventorySlotUnderPointer(eventData);
        if (targetSlot == null)
        {
            Debug.Log("[AquariumSlot] Drop dibatalkan, target bukan slot inventory.");
            return;
        }

        AquariumSystem target = ResolveAquarium();
        if (target == null)
        {
            Debug.LogWarning("[AquariumSlot] AquariumSystem tidak ditemukan.");
            return;
        }

        target.TryMoveFishToInventory(this, targetSlot.gameObject);
    }

    // â”€â”€â”€ Private helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â...

    private AquariumSystem ResolveAquarium()
    {
        return aquariumSystem != null ? aquariumSystem : AquariumSystem.CurrentOpen;
    }

    private static int FindSlotIndex(AquariumSystem aquarium, AquariumFishSlotUI slot)
    {
        if (aquarium == null || slot == null) return -1;
        IReadOnlyList<AquariumFishSlotUI> slots = aquarium.FishSlots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == slot) return i;
        }
        return -1;
    }

    /// <summary>
    /// Spawn/update icon prefab sebagai child slot.
    /// Hanya Destroy+Instantiate jika nama prefab berbeda dari yang sedang ditampilkan.
    /// </summary>
    private void RefreshIconItem(string itemName)
    {
        if (!hasFish)
        {
            // Slot kosong â€” hapus icon jika masih ada
            if (spawnedIconItem != null)
            {
                Destroy(spawnedIconItem);
                spawnedIconItem = null;
                spawnedPrefabName = null;
            }
            return;
        }

        // Slot berisi ikan â€” jika prefab sudah benar, tidak perlu re-spawn
        if (spawnedIconItem != null && spawnedPrefabName == itemName)
            return;

        // Prefab berbeda atau belum ada â€” hapus yang lama
        if (spawnedIconItem != null)
        {
            Destroy(spawnedIconItem);
            spawnedIconItem = null;
            spawnedPrefabName = null;
        }

        GameObject prefab = Resources.Load<GameObject>(itemName);
        if (prefab == null)
        {
            Debug.LogWarning($"[AquariumSlot] Prefab '{itemName}' tidak ditemukan di Resources. Icon tidak ditampilkan.");
            return;
        }

        spawnedIconItem = Instantiate(prefab, transform);
        spawnedIconItem.transform.localPosition = Vector3.zero;
        spawnedIconItem.name = itemName;
        spawnedPrefabName = itemName;

        DisableIconItemComponents(spawnedIconItem);
        Debug.Log($"[AquariumSlot] Slot {fishIndex} â€” icon '{itemName}' di-spawn.");
    }

    private void DisableIconItemComponents(GameObject iconItem)
    {
        InventoryItemLogic itemLogic = iconItem.GetComponent<InventoryItemLogic>();
        if (itemLogic != null) Destroy(itemLogic);

        DragDrop dragDrop = iconItem.GetComponent<DragDrop>();
        if (dragDrop != null) Destroy(dragDrop);

        CanvasGroup cg = iconItem.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        foreach (Image img in iconItem.GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;

        foreach (Text txt in iconItem.GetComponentsInChildren<Text>(true))
            txt.raycastTarget = false;
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

        RectTransform rt = dragPreview.GetComponent<RectTransform>();
        rt.sizeDelta = ((RectTransform)transform).rect.size;

        Image img = dragPreview.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        CanvasGroup cg = dragPreview.GetComponent<CanvasGroup>();
        cg.alpha = 0.75f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
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
            Image img = spawnedIconItem.GetComponent<Image>();
            if (img != null && img.sprite != null) return img.sprite;
        }
        return fishIcon != null ? fishIcon.sprite : null;
    }

    private ItemSlots FindInventorySlotUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return null;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            ItemSlots slot = result.gameObject.GetComponentInParent<ItemSlots>();
            if (slot != null && slot.Item == null) return slot;
        }
        return null;
    }
}
