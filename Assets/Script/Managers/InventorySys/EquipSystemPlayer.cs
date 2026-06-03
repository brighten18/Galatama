using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


 //selectionManager
public class EquipSystem : MonoBehaviour
{
    public static EquipSystem Instance { get; set; }
 
    public GameObject quickSlotsPanel;
    public GameObject NumbersHolder;
    public int SelectedNumber = 1;
    public GameObject SelectedItem;
    public bool isSelected;
    public bool isnowEquipped;
    public GameObject ToolsHolder;
    public bool IsHandVisible;
 
    public List<GameObject> quickSlotsList = new List<GameObject>();
    public List<string> itemList = new List<string>();

    [Header("Equipment Data List")]
    public List<EquipmentData> equipmentDataList = new List<EquipmentData>();
    private Dictionary<string, EquipmentData> equipmentDataMap;


 
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
 
    private void Start()
    {
        PopulateSlotList();
        BuildEquipmentDataMap();

        foreach (Transform child in NumbersHolder.transform)
            Debug.Log("NumbersHolder child: " + child.name);
    }

    private void BuildEquipmentDataMap()
    {
        equipmentDataMap = new Dictionary<string, EquipmentData>();
        foreach (EquipmentData data in equipmentDataList)
        {
            if (data == null) continue;
            if (equipmentDataMap.ContainsKey(data.itemName))
            {
                Debug.LogWarning($"[EquipSystem] Duplikat itemName: '{data.itemName}'");
                continue;
            }
            equipmentDataMap.Add(data.itemName, data);
        }
    }

    private void PopulateSlotList()
    {
        foreach (Transform child in quickSlotsPanel.transform)
        {
            if (child.CompareTag("QuickSlot"))
                quickSlotsList.Add(child.gameObject);
        }
    }

    void Update()
    {
        int pressedSlot = PlayerInputManager.Instance.GetPressedQuickSlot();

        if (pressedSlot != 0)
        {
            SelectedQuickSlot(pressedSlot);
            PlayerInputManager.Instance.ResetQuickSlotInput(pressedSlot);
        }
    }

    void SelectedQuickSlot(int slotIndex)
    {
        if (CheckIsQcSlotFull(slotIndex))
        {
            if (SelectedNumber != slotIndex)
            {
                // Lepas item lama sebelum equip yang baru
                if (SelectedItem != null)
                {
                    SelectedItem.GetComponent<InventoryItemLogic>().isSelected = false;
                    // DITAMBAH: Hapus model lama saat pindah slot
                    ClearToolsHolder();
                }

                SelectedNumber = slotIndex;
                SelectedItem = getSelectedItem(slotIndex);

                if (SelectedItem == null)
                {
                    Debug.LogError("SelectedItem null pada slot: " + slotIndex);
                    return;
                }

                SelectedItem.GetComponent<InventoryItemLogic>().isSelected = true;
                SetEquippedItem(SelectedItem);
                SetNumberColor(slotIndex, Color.white);

                Debug.Log("Slot " + slotIndex + " dipilih!");
            }
            else
            {
                // DITAMBAH: Tekan slot yang sama = lepas item
                UnequipCurrent();
            }
        }

        Debug.Log("QuickSlot " + slotIndex + " ditekan");
    }

    // DITAMBAH: Lepas item yang sedang diequip
    private void UnequipCurrent()
    {
        if (SelectedItem != null)
        {
            SelectedItem.GetComponent<InventoryItemLogic>().isSelected = false;
            SelectedItem = null;
        }

        SelectedNumber = -1;
        isnowEquipped = false;
        ClearToolsHolder();
        SetNumberColor(-1, Color.gray);

        Debug.Log("[EquipSystem] Item dilepas.");
    }

    // DITAMBAH: Hapus semua model di ToolsHolder
    private void ClearToolsHolder()
    {
        foreach (Transform child in ToolsHolder.transform)
            Destroy(child.gameObject);
    }

    // DITAMBAH: Atur warna semua Number, putihkan slotIndex yang dipilih
    private void SetNumberColor(int activeSlotIndex, Color activeColor)
    {
        foreach (Transform child in NumbersHolder.transform)
        {
            Transform textObj = child.Find("Text (Legacy)");
            if (textObj != null)
                textObj.GetComponent<Text>().color = Color.gray;
        }

        if (activeSlotIndex <= 0) return;

        Transform selectedNumber = NumbersHolder.transform.Find("Number" + activeSlotIndex);
        if (selectedNumber == null)
        {
            Debug.LogError("Tidak ditemukan: Number" + activeSlotIndex);
            return;
        }

        Transform selectedTextObj = selectedNumber.Find("Text (Legacy)");
        if (selectedTextObj != null)
            selectedTextObj.GetComponent<Text>().color = activeColor;
    }

    private void SetEquippedItem(GameObject itemEquipped)
    {
        if (itemEquipped == null)
        {
            isnowEquipped = false;
            return;
        }

        string itemName = ItemNameUtility.CleanName(itemEquipped.name);
        Debug.Log($"[EquipSystem] Mencari: '{itemName}'");

        foreach (var key in equipmentDataMap.Keys)
            Debug.Log($"[EquipSystem] Key tersedia: '{key}'");

        ClearToolsHolder();

        if (!equipmentDataMap.TryGetValue(itemName, out EquipmentData data))
        {
            Debug.LogError($"[EquipSystem] '{itemName}' tidak ditemukan di map.");
            isnowEquipped = false;
            return;
        }

        if (data.modelPrefab == null)
        {
            Debug.LogError($"[EquipSystem] modelPrefab null pada '{itemName}'.");
            isnowEquipped = false;
            return;
        }

        GameObject spawnedModel = Instantiate(data.modelPrefab, ToolsHolder.transform);
        spawnedModel.transform.localPosition = data.localPosition;
        spawnedModel.transform.localRotation = Quaternion.Euler(data.localRotation);
        spawnedModel.transform.localScale = data.localScale;

        isnowEquipped = true;
        Debug.Log($"[EquipSystem] Model equipped: '{itemName}'");
    }

    GameObject getSelectedItem(int slotIndex)
    {
        return quickSlotsList[slotIndex - 1].transform.GetChild(0).gameObject;
    }

    public EquipmentType GetEquippedType()
    {
        if (!isnowEquipped || SelectedItem == null)
            return EquipmentType.Default;
    
        string itemName = ItemNameUtility.CleanName(SelectedItem.name);
        if (equipmentDataMap.TryGetValue(itemName, out EquipmentData data))
            return data.itemType;
            
    
        return EquipmentType.Default;
    }

    public EquipmentData GetEquippedData()
    {
        if (!isnowEquipped || SelectedItem == null)
            return null;

        string itemName = ItemNameUtility.CleanName(SelectedItem.name);
        if (equipmentDataMap != null && equipmentDataMap.TryGetValue(itemName, out EquipmentData data))
            return data;

        return null;
    }

    public string GetEquippedItemName()
    {
        if (!isnowEquipped || SelectedItem == null)
            return string.Empty;

        return ItemNameUtility.CleanName(SelectedItem.name);
    }

    public bool TryConsumeSelectedItem(string expectedItemName)
    {
        if (SelectedItem == null)
            return false;

        string itemName = ItemNameUtility.CleanName(SelectedItem.name);
        if (!string.IsNullOrEmpty(expectedItemName) && itemName != expectedItemName)
            return false;

        InventoryItemLogic itemLogic = SelectedItem.GetComponent<InventoryItemLogic>();
        if (itemLogic != null)
        {
            itemLogic.isSelected = false;
            itemLogic.IsNowInsideQcSlot = false;
        }

        GameObject consumedItem = SelectedItem;
        consumedItem.transform.SetParent(null);
        Destroy(consumedItem);

        itemList.Remove(itemName);
        SelectedItem = null;
        SelectedNumber = -1;
        isnowEquipped = false;
        ClearToolsHolder();
        SetNumberColor(-1, Color.gray);

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ReCalculeList();
        }

        Debug.Log("[EquipSystem] Item dipakai: " + itemName);
        return true;
    }

    /// <summary>
    /// Memilih quick slot berdasarkan index (1-6). Dapat dipanggil dari klik UI maupun input keyboard.
    /// </summary>
    public void SelectSlot(int slotIndex)
    {
        SelectedQuickSlot(slotIndex);
    }

    bool CheckIsQcSlotFull(int slotIndex)
    {
        return quickSlotsList[slotIndex - 1].transform.childCount > 0;
    }

    public void AddToQuickSlots(GameObject itemToEquip)
    {
        GameObject availableSlot = FindNextEmptySlot();
        itemToEquip.transform.SetParent(availableSlot.transform, false);
        string cleanName = ItemNameUtility.CleanName(itemToEquip.name);
        itemList.Add(cleanName);
        InventorySystem.Instance.ReCalculeList();
    }
 
    private GameObject FindNextEmptySlot()
    {
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount == 0)
                return slot;
        }
        return new GameObject();
    }
 
    public bool CheckIsfFull()
    {
        int counter = 0;
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount > 0)
                counter++;
        }
        return counter == 6;
    }
}
