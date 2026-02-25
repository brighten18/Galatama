using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; set; }

    public GameObject inventoryScreenUI;
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
        Debug.Log("ini adalah instance InventorySystem: " + Instance);
    }

    void Update()
    {
        bool inventoryPressed = PlayerInputManager.Instance != null && PlayerInputManager.Instance.Inventory;

        if (inventoryPressed && !inventoryPressedLastFrame)
        {
            if (!isOpen)
            {
                Debug.Log("Inventory dibuka");
                inventoryScreenUI.SetActive(true);
                isOpen = true;
            }
            else
            {
                inventoryScreenUI.SetActive(false);
                isOpen = false;
            }

            // ✏️ DITAMBAH: Reset input setelah dikonsumsi agar toggle bisa terpicu lagi
            PlayerInputManager.Instance.ResetInventoryInput();
        }

        inventoryPressedLastFrame = inventoryPressed;
    }
}