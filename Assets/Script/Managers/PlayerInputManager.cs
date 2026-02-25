using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance { get; private set; }

    private StarterAssetsInputs _input;

    // Character Input Values
    public Vector2 Move => _input != null ? _input.move : Vector2.zero;
    public Vector2 Look => _input != null ? _input.look : Vector2.zero;
    public bool Jump => _input != null && _input.jump;
    public bool Sprint => _input != null && _input.sprint;
    public bool Interact => _input != null && _input.Interact;
    public bool Inventory => _input != null && _input.Inventory;

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
            _input = player.GetComponent<StarterAssetsInputs>();
        else
            Debug.LogError("Player tidak ditemukan! Pastikan Player memiliki tag 'Player'.");
    }

    public void ResetInventoryInput()
    {
    if (_input != null)
        _input.Inventory = false;
    }
}
