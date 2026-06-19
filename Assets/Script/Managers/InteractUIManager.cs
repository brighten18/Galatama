using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class InteractUIManager : MonoBehaviour
{
    public static InteractUIManager Instance { get; private set; }

    [Header("UI Settings")]
    public GameObject interactionInfoUI;
    private Text interactionText;

    [Header("Cooldown UI")]
    [SerializeField] private GameObject cooldownUIRoot;
    [SerializeField] private Image cooldownRadialImage;
    [SerializeField] private Text cooldownTimerText;

    [Header("Interaction Settings")]
    public float maxInteractionDistance = 5f;
    public bool OnTargeted = false;
    public RaycastHit lastNonTriggerHit;
    public bool HasNonTriggerHit => lastNonTriggerHit.collider != null;

    private InteractableObject currentInteractable;
    private Camera mainCamera;
    private bool isReady = false;

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
        mainCamera = Camera.main;

        if (interactionInfoUI != null)
            interactionText = interactionInfoUI.GetComponent<Text>();
        else
            Debug.LogError("interactionInfoUI belum diassign di Inspector!");

        SetCooldownUIVisible(false);
        isReady = mainCamera != null && interactionText != null;
    }

    private void Update()
    {
        if (!isReady)
            return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxInteractionDistance, Physics.AllLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        if (TryGetFirstValidHit(hits, out RaycastHit hit))
        {
            lastNonTriggerHit = hit;
            OnTargeted = true;

            InteractableObject interactable = hit.transform.GetComponentInParent<InteractableObject>();
            if (interactable != null && OnTargeted)
            {
                string itemName = interactable.GetItemName();
                if (string.IsNullOrEmpty(itemName))
                {
                    ClearInteraction();
                }
                else
                {
                    SetCurrentInteractable(interactable);
                    interactionText.text = itemName;
                    interactionInfoUI.SetActive(true);
                    UpdateCooldownUI(interactable);
                }
            }
            else
            {
                ClearInteraction();
            }
        }
        else
        {
            lastNonTriggerHit = default;
            ClearInteraction();
        }
    }

    private bool TryGetFirstValidHit(RaycastHit[] hits, out RaycastHit validHit)
    {
        if (hits != null)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.collider.isTrigger)
                    continue;

                if (IsHeldItemHit(hit))
                    continue;

                validHit = hit;
                return true;
            }
        }

        validHit = default;
        return false;
    }

    private bool IsHeldItemHit(RaycastHit hit)
    {
        if (hit.transform == null)
            return false;

        return hit.transform.GetComponentInParent<HeldItemVisual>() != null;
    }

    private void ClearInteraction()
    {
        if (currentInteractable != null)
            currentInteractable.SetLookingAt(false);

        currentInteractable = null;
        interactionInfoUI.SetActive(false);
        SetCooldownUIVisible(false);
        OnTargeted = false;
        lastNonTriggerHit = default;
    }

    private void SetCurrentInteractable(InteractableObject interactable)
    {
        if (currentInteractable != null && currentInteractable != interactable)
            currentInteractable.SetLookingAt(false);

        currentInteractable = interactable;
        currentInteractable.SetLookingAt(true);
    }

    public bool IsCurrentInteractable(InteractableObject interactable)
    {
        return interactable != null &&
               currentInteractable == interactable &&
               OnTargeted;
    }

    private void UpdateCooldownUI(InteractableObject interactable)
    {
        IInteractCooldownProvider cooldownProvider = interactable as IInteractCooldownProvider;
        if (cooldownProvider == null || !cooldownProvider.ShouldShowCooldownUI())
        {
            SetCooldownUIVisible(false);
            return;
        }

        float duration = Mathf.Max(0.01f, cooldownProvider.GetCooldownDurationSeconds());
        float remaining = Mathf.Clamp(cooldownProvider.GetCooldownRemainingSeconds(), 0f, duration);
        float fillAmount = remaining / duration;

        if (cooldownRadialImage != null)
            cooldownRadialImage.fillAmount = fillAmount;

        if (cooldownTimerText != null)
            cooldownTimerText.text = Mathf.CeilToInt(remaining).ToString();

        SetCooldownUIVisible(true);
    }

    private void SetCooldownUIVisible(bool visible)
    {
        if (cooldownUIRoot != null)
            cooldownUIRoot.SetActive(visible);

        if (!visible && cooldownRadialImage != null)
            cooldownRadialImage.fillAmount = 0f;

        if (!visible && cooldownTimerText != null)
            cooldownTimerText.text = string.Empty;
    }
}
