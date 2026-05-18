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
    }

    void Update()
    {
        bool inventoryPressed = PlayerInputManager.Instance != null && PlayerInputManager.Instance.Inventory;

        if (inventoryPressed && !inventoryPressedLastFrame)
        {
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
        whatToEquipSlot = FindNewNextSlot();
        if (whatToEquipSlot == null)
        {
            Debug.LogError("[Inventory] Tidak bisa menambah item - tidak ada slot valid!");
            return false;
        }

        GameObject itemPrefab = Resources.Load<GameObject>(ItemName);
        if (itemPrefab == null)
        {
            Debug.LogError("[Inventory] Prefab item tidak ditemukan di Resources: " + ItemName);
            return false;
        }

        ObjToAdd = Instantiate(itemPrefab, whatToEquipSlot.transform.position, whatToEquipSlot.transform.rotation);
        ObjToAdd.transform.SetParent(whatToEquipSlot.transform);
        itemList.Add(ItemName);
        CheckFull();

        Image itemImage = ObjToAdd.GetComponent<Image>();
        TriggerPickupAlert(ItemName, itemImage != null ? itemImage.sprite : null);
        return true;
    }

    public int GetEmptySlotCount()
    {
        if (slotList.Count == 0)
        {
            CountSlotList();
        }

        int emptySlots = 0;
        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount == 0)
            {
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
        PickupAlertName.text = itemName;
        PickupAlertImage.sprite = itemSprite;
        PickupAlertUI.SetActive(true);
        StartCoroutine(HidePickupAlertAfterDelay(2f));
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

    public bool CheckFull()
    {
        int couter = 0;
        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount > 0)
            {
                couter += 1;
            }
        }

        if (couter == slotList.Count)
        {
            isFull = true;
            return true;
        }
        else
        {
            isFull = false;
            return false;
        }
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
