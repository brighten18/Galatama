using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

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
    public StarterAssetsInputs _Input;

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

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _Input = player.GetComponent<StarterAssetsInputs>();

        if (interactionInfoUI != null)
            interactionText = interactionInfoUI.GetComponent<Text>();
        else
            Debug.LogError("interactionInfoUI belum diassign di Inspector!");

        isReady = mainCamera != null && _Input != null && interactionText != null;
    }

    private void Update()
    {
        if (!isReady) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxInteractionDistance, Physics.AllLayers, QueryTriggerInteraction.Collide))
        {
            // Simpan hit hanya jika collider bukan trigger
            if (!hit.collider.isTrigger)
            {
                lastNonTriggerHit = hit;   // <<-- nilai hit non-trigger disimpan
                OnTargeted = true;
                Debug.Log("Kena collide");
            }
            else
            {
                // Jika mengenai trigger, reset lastNonTriggerHit (opsional, bisa juga dibiarkan)
                lastNonTriggerHit = default;
                OnTargeted = false;
                Debug.Log("Kena Istrigger");
            }

            InteractableObject interactable = hit.transform.GetComponent<InteractableObject>();

            if (interactable != null)
            {
                currentInteractable = interactable;
                interactionText.text = interactable.GetItemName();
                interactionInfoUI.SetActive(true);
                // OnTargeted sudah diatur di atas, tidak perlu diubah lagi
                interactable.SetLookingAt(true);
            }
            else
            {
                if (currentInteractable != null)
                    currentInteractable.SetLookingAt(false);

                ClearInteraction();   // Di dalamnya OnTargeted di-set false
            }
        }
        else
        {
            // Tidak ada hit sama sekali → reset lastNonTriggerHit
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