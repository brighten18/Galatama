using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class InteractUIManager : MonoBehaviour
{   
    public static InteractUIManager Instance { get; private set; }

    [Header("UI Settings")]
    public GameObject interactionInfoUI;
    private Text interactionText;

    [Header("Interaction Settings")]
    public float maxInteractionDistance = 5f;
    public bool OnTargeted = false;
    public RaycastHit lastNonTriggerHit;
    public bool HasNonTriggerHit => lastNonTriggerHit.collider != null;

    private InteractableObject currentInteractable;
    // âœï¸ DIHAPUS: public StarterAssetsInputs _Input;

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

        // âœï¸ DIHAPUS: FindGameObjectWithTag dan GetComponent StarterAssetsInputs

        if (interactionInfoUI != null)
            interactionText = interactionInfoUI.GetComponent<Text>();
        else
            Debug.LogError("interactionInfoUI belum diassign di Inspector!");

        // âœï¸ DIUBAH: Hapus _Input != null dari pengecekan isReady
        isReady = mainCamera != null && interactionText != null;
    }

    private void Update()
    {
        if (!isReady) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxInteractionDistance, Physics.AllLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        RaycastHit hit;
        if (TryGetFirstValidHit(hits, out hit))
        {
            lastNonTriggerHit = hit;
            OnTargeted = true;

            InteractableObject interactable = hit.transform.GetComponentInParent<InteractableObject>();

            if (interactable != null && OnTargeted)
            {
                SetCurrentInteractable(interactable);
                interactionText.text = interactable.GetItemName();
                interactionInfoUI.SetActive(true);
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
}
