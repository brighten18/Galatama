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
    }
        
    public void CountSlotList()
    {
        slotList.Clear();
        foreach (Transform child in inventoryScreenUI.transform)
        {
            if (child.CompareTag("Slot"))
                slotList.Add(child.gameObject);
        }

        // Fallback: jika slotList masih kosong, cari semua slot di scene
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
        whatToEquipSlot = FindNewNextSlot();
        if (whatToEquipSlot == null)
        {
            Debug.LogError("[Inventory] Tidak bisa menambah item — tidak ada slot valid!");
            return;
        }

        ObjToAdd = Instantiate(Resources.Load<GameObject>(ItemName),
        whatToEquipSlot.transform.position, whatToEquipSlot.transform.rotation);
        ObjToAdd.transform.SetParent(whatToEquipSlot.transform);
        itemList.Add(ItemName);
        CheckFull();
    }
    private GameObject FindNewNextSlot()
    {
        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount == 0)
                return slot;
        }

        Debug.LogError("[Inventory] Tidak ada slot kosong ditemukan! slotList.Count: " + slotList.Count);
        return null; // return null instead of gameObject
    }

    public bool CheckFull()
    {
        int couter = 0;
        foreach (GameObject slot in slotList)        {
            if (slot.transform.childCount > 0)
            {
                couter+=1;
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

    // ✏️ DITAMBAH: Recalculate itemList setelah item dihapus dari inventory
    public void ReCalculeList()
    {
        itemList.Clear();

        foreach (GameObject slot in slotList)
        {
            if (slot.transform.childCount > 0)
            {
                string itemName = slot.transform.GetChild(0).name;
                string cleaned = itemName.Replace("(Clone)", "").Trim();
                itemList.Add(cleaned);
            }
        }

        // Update status isFull setelah recalculate
        CheckFull();

        Debug.Log("Item list setelah recalculate: " + itemList.Count);
    }
}