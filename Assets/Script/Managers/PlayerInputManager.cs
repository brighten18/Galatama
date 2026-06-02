using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance { get; private set; }
    private StarterAssets.ThirdPersonController playerController;

    private StarterAssetsInputs _input;

    // Character Input Values
    public Vector2 Move => _input != null ? _input.move : Vector2.zero;
    public Vector2 Look => _input != null ? _input.look : Vector2.zero;
    public bool Jump => _input != null && _input.jump;
    public bool Sprint => _input != null && _input.sprint;
    public bool Interact => _input != null && _input.Interact;
    public bool Inventory => _input != null && _input.Inventory;
    public bool InteractOBJ => _input != null && _input.InteractOBJ;
    public bool Pause => _input != null && _input.Pause;

    // Quick Slot Inputs
    public bool QuickSlot1 => _input != null && _input.QuickSlot1;
    public bool QuickSlot2 => _input != null && _input.QuickSlot2;
    public bool QuickSlot3 => _input != null && _input.QuickSlot3;
    public bool QuickSlot4 => _input != null && _input.QuickSlot4;
    public bool QuickSlot5 => _input != null && _input.QuickSlot5;
    public bool QuickSlot6 => _input != null && _input.QuickSlot6;

    // Movement Settings
    public bool AnalogMovement => _input != null && _input.analogMovement;

    // Mouse Cursor Settings
    public bool CursorLocked => _input != null && _input.cursorLocked;
    public bool CursorInputForLook => _input != null && _input.cursorInputForLook;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _input = player.GetComponent<StarterAssetsInputs>();
            playerController = player.GetComponent<StarterAssets.ThirdPersonController>();
        }
             
    }

    private void LateUpdate()
    {
        if (_input == null) return;

        _input.Interact = false;
        _input.InteractOBJ = false;
    }

    public void ResetInventoryInput()
    {
    if (_input != null)
        {
            _input.Inventory = false;

            Debug.Log("Inventory input reset via PlayerInputManager");
        }

    }

    public void ResetInteractInput()
    {
        if (_input != null)
        {
            _input.Interact = false;
            Debug.Log("Interact input reset via PlayerInputManager");
        }
    }

    public void ResetInteractOBJInput()
    {
        if (_input != null)
        {
            _input.InteractOBJ = false;
            Debug.Log("InteractOBJ input reset via PlayerInputManager");
        }
    }

    public void ResetPauseInput()
    {
        if (_input != null)
        {
            _input.Pause = false;
        }
    }

    public void SetCursorAndLook(bool locked, bool enableLook)
    {
        if (_input != null)
        {
            _input.cursorLocked = locked;
            _input.cursorInputForLook = enableLook;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }

    public void SetPlayerMovement(bool enabled)
    {
        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.jump = false;
            _input.sprint = false;
        }

        if (playerController != null)
            playerController.SetMovementBlocked(!enabled);
    }
    public int GetPressedQuickSlot()
    {
        if (QuickSlot1) return 1;
        if (QuickSlot2) return 2;
        if (QuickSlot3) return 3;
        if (QuickSlot4) return 4;
        if (QuickSlot5) return 5;
        if (QuickSlot6) return 6;
        return 0;
    }

        public void ResetQuickSlotInput(int slotIndex)
    {
        if (_input == null) return;

        switch (slotIndex)
        {
            case 1: _input.QuickSlot1 = false; break;
            case 2: _input.QuickSlot2 = false; break;
            case 3: _input.QuickSlot3 = false; break;
            case 4: _input.QuickSlot4 = false; break;
            case 5: _input.QuickSlot5 = false; break;
            case 6: _input.QuickSlot6 = false; break;
            default: Debug.LogWarning("QuickSlot index tidak valid: " + slotIndex); break;
        }
    }

    // âœï¸ DITAMBAH: Reset semua QuickSlot sekaligus
    public void ResetAllQuickSlotInputs()
    {
        if (_input == null) return;

        _input.QuickSlot1 = false;
        _input.QuickSlot2 = false;
        _input.QuickSlot3 = false;
        _input.QuickSlot4 = false;
        _input.QuickSlot5 = false;
        _input.QuickSlot6 = false;
    }
}
