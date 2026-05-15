using UnityEngine;
using UnityEngine.UI;

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
    // ✏️ DIHAPUS: public StarterAssetsInputs _Input;

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

        // ✏️ DIHAPUS: FindGameObjectWithTag dan GetComponent StarterAssetsInputs

        if (interactionInfoUI != null)
            interactionText = interactionInfoUI.GetComponent<Text>();
        else
            Debug.LogError("interactionInfoUI belum diassign di Inspector!");

        // ✏️ DIUBAH: Hapus _Input != null dari pengecekan isReady
        isReady = mainCamera != null && interactionText != null;
    }

    private void Update()
    {
        if (!isReady) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxInteractionDistance, Physics.AllLayers, QueryTriggerInteraction.Collide))
        {
            if (!hit.collider.isTrigger)
            {
                lastNonTriggerHit = hit;
                OnTargeted = true;

            }
            else
            {
                lastNonTriggerHit = default;
                OnTargeted = false;
            }

            InteractableObject interactable = hit.transform.GetComponent<InteractableObject>();

            if (interactable != null)
            {
                currentInteractable = interactable;
                interactionText.text = interactable.GetItemName();
                interactionInfoUI.SetActive(true);
                interactable.SetLookingAt(true);
            }
            else
            {
                if (currentInteractable != null)
                    currentInteractable.SetLookingAt(false);

                ClearInteraction();
            }
        }
        else
        {
            lastNonTriggerHit = default;

            if (currentInteractable != null)
                currentInteractable.SetLookingAt(false);

            ClearInteraction();
        }
    }

    private void ClearInteraction()
    {
        currentInteractable = null;
        interactionInfoUI.SetActive(false);
        OnTargeted = false;
        lastNonTriggerHit = default;
    }
}