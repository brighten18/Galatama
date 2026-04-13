using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
public class EquipSystem : MonoBehaviour
{
    public static EquipSystem Instance { get; set; }
 
    // -- UI -- //
    public GameObject quickSlotsPanel;
    public GameObject NumbersHolder;
    public int SelectedNumber =1;
    public GameObject SelectedItem;
    public bool isSelected;
    public bool isnowEquipped;
    public GameObject ToolsHolder;

 
    public List<GameObject> quickSlotsList = new List<GameObject>();
    public List<string> itemList = new List<string>();
 
   
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

            // Tambah sementara di Start() untuk cek nama child
    foreach (Transform child in NumbersHolder.transform)
        Debug.Log("NumbersHolder child: " + child.name);
    }
    
 
    private void PopulateSlotList()
    {
        foreach (Transform child in quickSlotsPanel.transform)
        {
            if (child.CompareTag("QuickSlot"))
            {
                quickSlotsList.Add(child.gameObject);
            }
        }
    }

    void Update()
    {
        int pressedSlot = PlayerInputManager.Instance.GetPressedQuickSlot();

        if (pressedSlot != 0)
        {
            // Gunakan pressedSlot langsung
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
                SelectedNumber = slotIndex;

                if (SelectedItem != null)
                    SelectedItem.GetComponent<InventoryItemLogic>().isSelected = false;

                SelectedItem = getSelectedItem(slotIndex);

                if (SelectedItem == null)
                {
                    Debug.LogError("SelectedItem null pada slot: " + slotIndex);
                    return;
                }

                SelectedItem.GetComponent<InventoryItemLogic>().isSelected = true;

                SetEquippedItem(SelectedItem);

                // Set semua ke abu-abu
                foreach (Transform child in NumbersHolder.transform)
                {
                    Transform textObj = child.Find("Text");
                    if (textObj != null)
                        textObj.GetComponent<Text>().color = Color.gray;
                }

                // Set yang dipilih ke putih
                Transform selectedNumber = NumbersHolder.transform.Find("Number" + slotIndex);
                if (selectedNumber == null)
                {
                    Debug.LogError("Tidak ditemukan: Number" + slotIndex);
                    return;
                }

                Transform selectedTextObj = selectedNumber.Find("Text");
                if (selectedTextObj != null)
                    selectedTextObj.GetComponent<Text>().color = Color.white;

                Debug.Log("Slot " + slotIndex + " dipilih!");
            }
            else
            {
                SelectedNumber = -1;

                if (SelectedItem != null)
                {
                    SelectedItem.GetComponent<InventoryItemLogic>().isSelected = false;
                    SelectedItem = null;
                }

                // DIPERBAIKI: Konsisten menggunakan Find("Text")
                foreach (Transform child in NumbersHolder.transform)
                {
                    Transform textObj = child.Find("Text");
                    if (textObj != null)
                        textObj.GetComponent<Text>().color = Color.gray;
                }
            }
        }
        Debug.Log("QuickSlot " + slotIndex + " ditekan");
    }

    private void SetEquippedItem(GameObject itemEquipped)
    {
        String itemName = itemEquipped.name.Replace("(Clone)", "");
        GameObject itemPrefab = Instantiate(Resources.Load<GameObject>( itemName + "_Model"),new Vector3(0.5f,1f,0.2f), Quaternion.Euler(-90, 0, 180));
        itemPrefab.transform.SetParent(ToolsHolder.transform, false);

        
        if (itemEquipped != null)
        {
            // Hapus item yang sudah ada di ToolsHolder
            foreach (Transform child in ToolsHolder.transform)
            {
                Destroy(child.gameObject);
            }

            // Clone item yang dipilih dan tempatkan di ToolsHolder
            GameObject equippedItem = Instantiate(itemEquipped, ToolsHolder.transform);
            equippedItem.transform.localPosition = Vector3.zero; // Atur posisi sesuai kebutuhan
            equippedItem.transform.localRotation = Quaternion.identity; // Atur rotasi sesuai kebutuhan
            equippedItem.transform.localScale = Vector3.one; // Atur skala sesuai kebutuhan

            isnowEquipped = true;
        }
        else
        {
            isnowEquipped = false;
        }
    }

    GameObject getSelectedItem(int slotIndex)
    {
        return quickSlotsList[slotIndex - 1].transform.GetChild(0).gameObject;
    }
    bool CheckIsQcSlotFull(int slotIndex)
    {
        if (quickSlotsList[slotIndex - 1].transform.childCount > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddToQuickSlots(GameObject itemToEquip)
    {
        // Find next free slot
        GameObject availableSlot = FindNextEmptySlot();
        // Set transform of our object
        itemToEquip.transform.SetParent(availableSlot.transform, false);
        // Getting clean name
        string cleanName = itemToEquip.name.Replace("(Clone)", "");
        // Adding item to list
        itemList.Add(cleanName);
 
        InventorySystem.Instance.ReCalculeList();
 
    }
 
    private GameObject FindNextEmptySlot()
    {
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount == 0)
            {
                return slot;
            }
        }
        return new GameObject();
    }
 
    public bool CheckIsfFull()
    {
 
        int counter = 0;
 
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount > 0)
            {
                counter += 1;
            }
        }
 
        if (counter == 6)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}