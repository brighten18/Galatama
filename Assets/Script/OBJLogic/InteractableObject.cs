using UnityEngine;
using System.Collections;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] protected string itemName;
    [SerializeField] private bool keepInWorldAfterInteract = false;
    [SerializeField] private bool respawnAfterPickup = false;
    [SerializeField] private float respawnCooldownSeconds = 20f;

    [Header("Highlight (QuickOutline)")]
    [SerializeField] private bool useHighlight = true;
    [SerializeField] private bool autoAddOutlineIfMissing = false;
    [SerializeField] private Outline.Mode highlightMode = Outline.Mode.OutlineVisible;
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField, Range(0f, 10f)] private float highlightWidth = 4f;

    [Header("Cooldown World UI")]
    [SerializeField] protected bool showWorldCooldownUI = true;
    [SerializeField] protected Vector3 cooldownUIOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] protected Vector2 cooldownUICanvasSize = new Vector2(60f, 60f);
    [SerializeField] protected float cooldownUIWorldScale = 0.006f;
    [SerializeField, Range(0f, 0.45f)] protected float cooldownUIInnerPadding = 0.08f;
    [SerializeField] protected Color cooldownUIFillColor = new Color(0.2f, 0.82f, 1f, 0.95f);
    [SerializeField] protected Color cooldownUIBackgroundColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] protected int cooldownUIFontSize = 18;

    protected bool isBeingLookedAt = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool isRespawning;
    private float respawnStartTime;
    private float respawnTotalTime;
    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;
    private Outline quickOutline;
    private bool outlineInitialized = false;
    private WorldSpaceCooldownUI itemCooldownUI;

    protected virtual void Awake()
    {
        InitializeBase();
    }

    /// <summary>
    /// Inisialisasi semua state base class (position cache, renderer, collider, outline).
    /// Dipanggil dari Awake().
    /// atau memanggil base.Awake() agar outline berfungsi.
    /// </summary>
    protected void InitializeBase()
    {
        if (outlineInitialized) return;
        outlineInitialized = true;

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider>(true);
        SetupQuickOutline();
        SetupWorldCooldownUI();
    }

    private void SetupWorldCooldownUI()
    {
        if (!showWorldCooldownUI || !respawnAfterPickup)
            return;

        itemCooldownUI = GetComponent<WorldSpaceCooldownUI>();
        if (itemCooldownUI == null)
            itemCooldownUI = gameObject.AddComponent<WorldSpaceCooldownUI>();

        itemCooldownUI.Configure(
            cooldownUIOffset,
            cooldownUICanvasSize,
            cooldownUIWorldScale,
            cooldownUIInnerPadding,
            cooldownUIFillColor,
            cooldownUIBackgroundColor,
            cooldownUIFontSize);
    }

    protected virtual void Update()
    {
        if (QuizSessionLock.IsLocked)
            return;

        if (CanInteract())
        {
            HandleInteract();
        }
    }

    protected virtual void LateUpdate()
    {
        if (!isRespawning || itemCooldownUI == null)
            return;

        float elapsed = Time.time - respawnStartTime;
        float remaining = Mathf.Max(0f, respawnTotalTime - elapsed);
        itemCooldownUI.SetCooldown(remaining, respawnTotalTime, true);
    }

    protected bool CanInteract()
    {
        return isBeingLookedAt &&
               PlayerInputManager.Instance != null &&
               PlayerInputManager.Instance.Interact &&
               InteractUIManager.Instance != null &&
               InteractUIManager.Instance.IsCurrentInteractable(this);
    }

    protected virtual void HandleInteract()
    {
        PlayerInputManager.Instance.ResetInteractInput();

        if (ShouldKeepInWorldAfterInteract())
        {
            InteractObject();
            return;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[InteractableObject] InventorySystem tidak ditemukan.");
            return;
        }

        if (!InventorySystem.Instance.CheckFull())
        {
            TriggerPickUp();
            InventorySystem.Instance.AddItemToInventory(itemName);
            InteractObject();

            if (ShouldRespawnAfterPickup())
            {
                StartCoroutine(RespawnAfterCooldown());
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log("Inventory penuh, tidak bisa mengambil " + itemName);
        }
    }

    /// <summary>
    /// Memicu animasi PickUp pada ThirdPersonController player.
    /// </summary>
    private void TriggerPickUp()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        StarterAssets.ThirdPersonController controller = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (controller != null)
        {
            controller.TriggerPickUpAnimation();
        }
    }

    /// <summary>
    /// Dipanggil oleh InteractUIManager setiap frame untuk menandai apakah player
    /// sedang melihat object ini. Virtual agar subclass bisa mengoverride untuk
    /// menyembunyikan prompt saat kondisi tertentu (misalnya reward terkunci).
    /// </summary>
    public virtual void SetLookingAt(bool value)
    {
        isBeingLookedAt = value;
        ApplyHighlightState(value);
    }

    public virtual string GetItemName()
    {
        return itemName;
    }

    public virtual void InteractObject()
    {
        Debug.Log($"Berinteraksi dengan {itemName}");
    }

    private bool ShouldKeepInWorldAfterInteract()
    {
        if (keepInWorldAfterInteract)
            return true;

        string cleanName = ItemNameUtility.CleanName(itemName).ToLowerInvariant();
        return cleanName == "heater" ||
               cleanName == "cooler" ||
               cleanName == "chiller" ||
               cleanName == "aerator" ||
               cleanName == "waterpump" ||
               cleanName == "wateradder" ||
               cleanName == "penambahair";
    }

    private bool ShouldRespawnAfterPickup()
    {
        if (respawnAfterPickup)
            return true;

        string cleanName = ItemNameUtility.CleanName(itemName).ToLowerInvariant();
        return cleanName == "pelet" ||
               cleanName == "phbuffer" ||
               cleanName == "amoniaremover" ||
               cleanName == "garam";
    }

    private IEnumerator RespawnAfterCooldown()
    {
        if (isRespawning)
            yield break;

        isRespawning = true;
        isBeingLookedAt = false;
        ApplyHighlightState(false);
        SetWorldVisualsAndColliders(false);

        respawnTotalTime = Mathf.Max(0f, respawnCooldownSeconds);
        respawnStartTime = Time.time;

        yield return new WaitForSeconds(respawnTotalTime);

        if (itemCooldownUI != null)
            itemCooldownUI.SetCooldown(0f, 1f, false);

        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;
        SetWorldVisualsAndColliders(true);
        isRespawning = false;
    }

    private void SetupQuickOutline()
    {
        if (!useHighlight)
            return;

        quickOutline = GetComponent<Outline>();

        if (quickOutline == null)
        {
            // Hanya ambil Outline dari child yang bukan milik InteractableObject lain,
            // agar tidak mengambil Outline dari child interactable (misalnya mesin akuarium).
            foreach (Outline outline in GetComponentsInChildren<Outline>(true))
            {
                if (outline.GetComponentInParent<InteractableObject>() == this)
                {
                    quickOutline = outline;
                    break;
                }
            }
        }

        if (quickOutline == null && autoAddOutlineIfMissing)
            quickOutline = gameObject.AddComponent<Outline>();

        if (quickOutline == null)
            return;

        quickOutline.OutlineMode = highlightMode;
        quickOutline.OutlineColor = highlightColor;
        quickOutline.OutlineWidth = highlightWidth;

        // Selalu matikan outline di awal - termasuk jika sudah aktif di Inspector
        quickOutline.enabled = false;
    }

    private void ApplyHighlightState(bool enabled)
    {
        if (!useHighlight || quickOutline == null)
            return;

        quickOutline.OutlineMode = highlightMode;
        quickOutline.OutlineColor = highlightColor;
        quickOutline.OutlineWidth = highlightWidth;
        quickOutline.enabled = enabled;
    }

    private void SetWorldVisualsAndColliders(bool enabled)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);

        if (cachedColliders == null || cachedColliders.Length == 0)
            cachedColliders = GetComponentsInChildren<Collider>(true);

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }

        foreach (Collider collider in cachedColliders)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    private void OnDisable()
    {
        ApplyHighlightState(false);
    }
}
