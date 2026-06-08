using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; set; }

    public GameObject inventoryScreenUI;
    public List<GameObject> slotList = new List<GameObject>();
    public List<String> itemList = new List<String>();
    private GameObject ObjToAdd;
    public GameObject ItemInfoUI;
    private GameObject whatToEquipSlot;
    public bool isFull;
    public bool isOpen;
    private bool inventoryPressedLastFrame = false;

    public GameObject PickupAlertUI;
    public Text PickupAlertName;
    public Image PickupAlertImage;

    [Header("Fish Pickup Info UI")]
    public GameObject FishThingsUI;
    public Text FishName;
    public Image FishImage;
    public Text FishHunger;
    public Text FishHealth;
    public Text FishStatus;

    [Header("Pickup Alert Timing")]
    [SerializeField] private float pickupAlertDuration = 2f;
    [SerializeField] private float fishPickupAlertDuration = 3f;

    private readonly Queue<PickupAlertEntry> pickupAlertQueue = new Queue<PickupAlertEntry>();
    private Coroutine pickupAlertRoutine;

    private struct PickupAlertEntry
    {
        public string itemName;
        public Sprite icon;
        public FishInstanceState fishState;
        public bool isFish;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        isOpen = false;
        isFull = false;
        Debug.Log("ini adalah instance InventorySystem: " + Instance);

        CountSlotList();
        Cursor.visible = false;
        PickupAlertUI.SetActive(false);

        if (FishThingsUI != null)
        {
            FishThingsUI.SetActive(false);
        }
        #if !UNITY_EDITOR && DEVELOPMENT_BUILD
        Debug.developerConsoleVisible = false;
        // Atau
        Debug.developerConsoleEnabled = false;
        #endif
    }

    public void CountSlotList()
    {
        slotList.Clear();
        foreach (Transform child in inventoryScreenUI.transform)
        {
            if (child.CompareTag("Slot"))
                slotList.Add(child.gameObject);
        }

        if (slotList.Count == 0)
        {
            Debug.LogWarning("[Inventory] Slot tidak ditemukan via inventoryScreenUI, mencari manual...");
            GameObject[] allSlots = GameObject.FindGameObjectsWithTag("Slot");
            slotList.AddRange(allSlots);
        }

        Debug.Log($"[Inventory] slotList populated: {slotList.Count} slots");

        Debug.Log("Ikannya" + FishName);
    }

    void Update()
    {
        if (QuizSessionLock.IsLocked)
        {
            if (PlayerInputManager.Instance != null)
                PlayerInputManager.Instance.ResetInventoryInput();
            return;
        }

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        bool inventoryPressed = PlayerInputManager.Instance != null && PlayerInputManager.Instance.Inventory;

        if (inventoryPressed && !inventoryPressedLastFrame)
        {
            if (PosterPopupManager.Instance != null && PosterPopupManager.Instance.IsOpen)
            {
                PosterPopupManager.Instance.ClosePoster();
            }

            if (!isOpen)
            {
                inventoryScreenUI.SetActive(true);
                isOpen = true;
                PlayerInputManager.Instance.SetCursorAndLook(false, false);
                PlayerInputManager.Instance.SetPlayerMovement(false);
                Cursor.visible = true;
            }
            else
            {
                inventoryScreenUI.SetActive(false);
                isOpen = false;
                PlayerInputManager.Instance.SetCursorAndLook(true, true);
                PlayerInputManager.Instance.SetPlayerMovement(true);
                ReCalculeList();
                Cursor.visible = false;
            }

            PlayerInputManager.Instance.ResetInventoryInput();
        }

        inventoryPressedLastFrame = inventoryPressed;
    }

    public void AddItemToInventory(string ItemName)
    {
        TryAddItemToInventory(ItemName);
    }

    public bool TryAddItemToInventory(string ItemName)
    {
        // Prioritas 1: Isi hotbar (QuickSlot 1-6) terlebih dahulu dari kiri ke kanan
        GameObject hotbarSlot = FindNextEmptyHotbarSlot();
        if (hotbarSlot != null)
        {
            GameObject hotbarItem;
            if (!TryCreateItemInSlot(ItemName, hotbarSlot, out hotbarItem))
                return false;

            if (EquipSystem.Instance != null)
                EquipSystem.Instance.itemList.Add(ItemNameUtility.CleanName(ItemName));

            CheckFull();
            Image hotbarImage = hotbarItem.GetComponent<Image>();
            TriggerPickupAlert(ItemName, hotbarImage != null ? hotbarImage.sprite : null);
            Debug.Log($"[Inventory] '{ItemName}' masuk ke hotbar slot: {hotbarSlot.name}");
            return true;
        }

        // Prioritas 2: Hotbar penuh, alihkan ke inventaris utama
        whatToEquipSlot = FindNewNextSlot();
        if (whatToEquipSlot == null)
        {
            Debug.LogError("[Inventory] Tidak bisa menambah item - tidak ada slot valid!");
            return false;
        }

        GameObject inventoryItem;
        if (!TryCreateItemInSlot(ItemName, whatToEquipSlot, out inventoryItem))
            return false;

        itemList.Add(ItemName);
        CheckFull();

        Image itemImage = inventoryItem.GetComponent<Image>();
        TriggerPickupAlert(ItemName, itemImage != null ? itemImage.sprite : null);
        Debug.Log($"[Inventory] '{ItemName}' masuk ke inventaris utama: {whatToEquipSlot.name}");
        return true;
    }

    public bool TryAddFishStateToInventory(FishInstanceState fishState)
    {
        if (fishState == null)
            return false;

        fishState = FishFactory.EnsureValid(fishState, fishState.itemName);

        // Prioritas 1: Isi hotbar terlebih dahulu
        GameObject hotbarSlot = FindNextEmptyHotbarSlot();
        if (hotbarSlot != null)
        {
            GameObject hotbarItem;
            if (!TryCreateItemInSlot(fishState.itemName, hotbarSlot, out hotbarItem))
                return false;

            AttachFishState(hotbarItem, fishState);

            if (EquipSystem.Instance != null)
                EquipSystem.Instance.itemList.Add(ItemNameUtility.CleanName(fishState.itemName));

            CheckFull();
            Image hotbarImage = hotbarItem.GetComponent<Image>();
            TriggerFishPickupAlert(fishState.itemName, hotbarImage != null ? hotbarImage.sprite : null, fishState);
            Debug.Log($"[Inventory] Ikan '{fishState.itemName}' masuk ke hotbar slot: {hotbarSlot.name}");
            return true;
        }

        // Prioritas 2: Hotbar penuh, alihkan ke inventaris utama
        GameObject inventoryItem;
        if (!TryCreateItemInNextSlot(fishState.itemName, out inventoryItem))
            return false;

        AttachFishState(inventoryItem, fishState);
        Image itemImage = inventoryItem.GetComponent<Image>();
        TriggerFishPickupAlert(fishState.itemName, itemImage != null ? itemImage.sprite : null, fishState);
        Debug.Log($"[Inventory] Ikan '{fishState.itemName}' masuk ke inventaris utama.");
        return true;
    }

    public bool TryAddItemToInventorySlot(string ItemName, GameObject targetSlot)
    {
        if (targetSlot == null)
            return TryAddItemToInventory(ItemName);

        if (targetSlot.transform.childCount > 0)
        {
            Debug.Log("[Inventory] Slot tujuan sudah terisi.");
            return false;
        }

        GameObject itemPrefab = Resources.Load<GameObject>(ItemName);
        if (itemPrefab == null)
        {
            Debug.LogError("[Inventory] Prefab item tidak ditemukan di Resources: " + ItemName);
            return false;
        }

        GameObject itemObject = Instantiate(itemPrefab, targetSlot.transform.position, targetSlot.transform.rotation);
        itemObject.transform.SetParent(targetSlot.transform);
        itemObject.transform.localPosition = Vector3.zero;

        InventoryItemLogic itemLogic = itemObject.GetComponent<InventoryItemLogic>();
        if (itemLogic != null)
            itemLogic.IsNowInsideQcSlot = targetSlot.CompareTag("QuickSlot");

        ReCalculeList();
        CheckFull();

        Image itemImage = itemObject.GetComponent<Image>();
        TriggerPickupAlert(ItemName, itemImage != null ? itemImage.sprite : null);
        return true;
    }

    public bool TryAddFishStateToInventorySlot(FishInstanceState fishState, GameObject targetSlot)
    {
        if (fishState == null)
            return false;

        fishState = FishFactory.EnsureValid(fishState, fishState.itemName);
        if (targetSlot == null)
            return TryAddFishStateToInventory(fishState);

        if (targetSlot.transform.childCount > 0)
        {
            Debug.Log("[Inventory] Slot tujuan sudah terisi.");
            return false;
        }

        GameObject itemObject;
        if (!TryCreateItemInSlot(fishState.itemName, targetSlot, out itemObject))
            return false;

        AttachFishState(itemObject, fishState);
        ReCalculeList();
        CheckFull();

        Image itemImage = itemObject.GetComponent<Image>();
        TriggerFishPickupAlert(fishState.itemName, itemImage != null ? itemImage.sprite : null, fishState);
        return true;
    }

    public int GetEmptySlotCount()
    {
        if (slotList.Count == 0)
            CountSlotList();

        int emptySlots = 0;
        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount == 0)
                emptySlots++;
        }

        // Hitung juga slot hotbar yang kosong
        if (EquipSystem.Instance != null)
        {
            foreach (GameObject slot in EquipSystem.Instance.quickSlotsList)
            {
                if (slot.transform.childCount == 0)
                    emptySlots++;
            }
        }

        return emptySlots;
    }

    public bool HasSpaceForItems(int itemCount)
    {
        return GetEmptySlotCount() >= itemCount;
    }

    public bool CanAddItemsToInventory(params string[] itemNames)
    {
        if (itemNames == null || itemNames.Length == 0)
        {
            return true;
        }

        if (!HasSpaceForItems(itemNames.Length))
        {
            return false;
        }

        foreach (string itemName in itemNames)
        {
            if (Resources.Load<GameObject>(itemName) == null)
            {
                Debug.LogError("[Inventory] Prefab item tidak ditemukan di Resources: " + itemName);
                return false;
            }
        }

        return true;
    }

    public void TriggerPickupAlert(string itemName, Sprite itemSprite)
    {
        EnqueuePickupAlert(new PickupAlertEntry
        {
            itemName = itemName,
            icon = itemSprite,
            fishState = null,
            isFish = false
        });
    }

    public void TriggerFishPickupAlert(string fishName, Sprite icon, FishInstanceState state)
    {
        EnqueuePickupAlert(new PickupAlertEntry
        {
            itemName = fishName,
            icon = icon,
            fishState = state,
            isFish = true
        });
    }

    private void EnqueuePickupAlert(PickupAlertEntry entry)
    {
        pickupAlertQueue.Enqueue(entry);

        if (pickupAlertRoutine == null)
        {
            pickupAlertRoutine = StartCoroutine(ProcessPickupAlertQueue());
        }
    }

    private IEnumerator ProcessPickupAlertQueue()
    {
        while (pickupAlertQueue.Count > 0)
        {
            PickupAlertEntry entry = pickupAlertQueue.Dequeue();
            if (entry.isFish)
            {
                ShowPickupAlert(entry.itemName, entry.icon);

                if (FishThingsUI != null)
                {
                    ShowFishPickupAlert(entry.itemName, entry.icon, entry.fishState);
                }

                yield return new WaitForSeconds(fishPickupAlertDuration);

                if (PickupAlertUI != null)
                {
                    PickupAlertUI.SetActive(false);
                }

                if (FishThingsUI != null)
                {
                    FishThingsUI.SetActive(false);
                }
            }
            else
            {
                ShowPickupAlert(entry.itemName, entry.icon);
                if (FishThingsUI != null)
                {
                    FishThingsUI.SetActive(false);
                }

                yield return new WaitForSeconds(pickupAlertDuration);

                if (PickupAlertUI != null)
                {
                    PickupAlertUI.SetActive(false);
                }
            }
        }

        pickupAlertRoutine = null;
    }

    private void ShowPickupAlert(string itemName, Sprite itemSprite)
    {
        if (PickupAlertUI == null)
        {
            return;
        }

        if (PickupAlertName != null)
        {
            PickupAlertName.text = itemName;
        }

        if (PickupAlertImage != null)
        {
            PickupAlertImage.sprite = itemSprite;
        }

        PickupAlertUI.SetActive(true);
    }

    private void ShowFishPickupAlert(string fishName, Sprite icon, FishInstanceState state)
    {
        if (FishThingsUI == null) return;

        if (FishName != null)
            FishName.text = fishName;

        if (FishImage != null)
            FishImage.sprite = icon;

        if (state != null)
        {
            if (FishHunger != null)
                FishHunger.text = $"Kenyang: {state.hunger:0}/{state.maxHunger:0}";

            if (FishHealth != null)
                FishHealth.text = $"HP: {state.health:0}/{state.maxHealth:0}";

            if (FishStatus != null)
            {
                if (!state.isAlive)
                    FishStatus.text = "Mati";
                else if (state.isStressed)
                    FishStatus.text = "Stress";
                else if (state.HungerPercent <= 0.2f)
                    FishStatus.text = "Lapar";
                else
                    FishStatus.text = "Kenyang";
            }
        }

        FishThingsUI.SetActive(true);
    }

    private IEnumerator HidePickupAlertAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PickupAlertUI.SetActive(false);
    }

    private GameObject FindNewNextSlot()
    {
        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount == 0)
                return slot;
        }

        Debug.LogError("[Inventory] Tidak ada slot kosong ditemukan! slotList.Count: " + slotList.Count);
        return null;
    }

    /// <summary>
    /// Mencari slot hotbar (QuickSlot) kosong pertama dari kiri ke kanan.
    /// Mengembalikan null jika semua slot hotbar sudah penuh.
    /// </summary>
    private GameObject FindNextEmptyHotbarSlot()
    {
        if (EquipSystem.Instance == null) return null;
        foreach (GameObject slot in EquipSystem.Instance.quickSlotsList)
        {
            if (slot.transform.childCount == 0)
                return slot;
        }
        return null;
    }

    private bool TryCreateItemInNextSlot(string itemName, out GameObject itemObject)
    {
        itemObject = null;
        whatToEquipSlot = FindNewNextSlot();
        if (whatToEquipSlot == null)
        {
            Debug.LogError("[Inventory] Tidak bisa menambah item - tidak ada slot valid!");
            return false;
        }

        if (!TryCreateItemInSlot(itemName, whatToEquipSlot, out itemObject))
            return false;

        itemList.Add(itemName);
        CheckFull();
        return true;
    }

    private bool TryCreateItemInSlot(string itemName, GameObject targetSlot, out GameObject itemObject)
    {
        itemObject = null;
        GameObject itemPrefab = Resources.Load<GameObject>(itemName);
        if (itemPrefab == null)
        {
            Debug.LogError("[Inventory] Prefab item tidak ditemukan di Resources: " + itemName);
            return false;
        }

        itemObject = Instantiate(itemPrefab, targetSlot.transform.position, targetSlot.transform.rotation);
        itemObject.transform.SetParent(targetSlot.transform);
        itemObject.transform.localPosition = Vector3.zero;

        InventoryItemLogic itemLogic = itemObject.GetComponent<InventoryItemLogic>();
        if (itemLogic != null)
            itemLogic.IsNowInsideQcSlot = targetSlot.CompareTag("QuickSlot");

        return true;
    }

    private void AttachFishState(GameObject itemObject, FishInstanceState fishState)
    {
        if (itemObject == null || fishState == null)
            return;

        FishRuntimeData runtimeData = itemObject.GetComponent<FishRuntimeData>();
        if (runtimeData == null)
            runtimeData = itemObject.AddComponent<FishRuntimeData>();

        runtimeData.SetState(fishState);
    }

    public bool CheckFull()
    {
        // Cek inventaris utama
        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount == 0)
            {
                isFull = false;
                return false;
            }
        }

        // Cek hotbar
        if (EquipSystem.Instance != null)
        {
            foreach (GameObject slot in EquipSystem.Instance.quickSlotsList)
            {
                if (slot.transform.childCount == 0)
                {
                    isFull = false;
                    return false;
                }
            }
        }

        isFull = true;
        return true;
    }

    public void ReCalculeList()
    {
        itemList.Clear();

        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount > 0)
            {
                string rawName = slot.transform.GetChild(0).name;
                string cleaned = ItemNameUtility.CleanName(rawName);
                itemList.Add(cleaned);
            }
        }

        CheckFull();
        Debug.Log("Item list setelah recalculate: " + itemList.Count);
    }
}
