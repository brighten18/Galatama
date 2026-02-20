using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem.Controls;
using StarterAssets;


public class InteractUIManager : MonoBehaviour
{   
    [Header("UI Settings")]
    public GameObject interactionInfoUI;      // Panel UI yang akan ditampilkan
    private Text interactionText;             // Komponen Text di dalam panel

    [Header("Interaction Settings")]
    public float maxInteractionDistance = 5f;  // Jarak maksimal interaksi

    public bool isLookingAtInteractingRange { get; private set; } = false; // Status apakah sedang berinteraksi
    private InteractableObject currentInteractable; // Objek yang sedang dilihat
    // InputControler inputController referensi ke InputController
    public StarterAssetsInputs _Input;

    private void Awake()
    {
        _Input = new StarterAssetsInputs();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        _Input = player.GetComponent<StarterAssetsInputs>();

        // Ambil komponen Text dari UI
        if (interactionInfoUI != null)
            interactionText = interactionInfoUI.GetComponent<Text>();
        else
            Debug.LogError("interactionInfoUI belum diassign di Inspector!");
    }

    private void Update()
    {
        // 1. Buat ray dari tengah layar (viewport point 0.5, 0.5)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

            // 2. Lakukan raycast dengan jarak terbatas
        if (_Input != null && interactionText != null)
        {
            if (Physics.Raycast(ray, out hit, maxInteractionDistance))
            {
                InteractableObject interactable = hit.transform.GetComponent<InteractableObject>();
                isLookingAtInteractingRange = true;

                if (interactable != null)
                {
                    // Jika objek memiliki komponen InteractableObject
                    currentInteractable = interactable;
                    interactionText.text = interactable.GetItemName(); // Tampilkan nama item
                    interactionInfoUI.SetActive(true);

                    // 3. Cek jika tombol interaksi ditekan
                    if (_Input.Interact)
                    {
                        interactable.InteractObject(); // Panggil method interaksi
                    }
                }
                else
                {
                    // Bukan objek interaksi
                    ClearInteraction();
                }
            }
            else
            {
                isLookingAtInteractingRange = false;
                // Tidak ada yang terkena ray
                ClearInteraction();
            }
        }  
    }

    private void ClearInteraction()
    {
        currentInteractable = null;
        interactionInfoUI.SetActive(false);
    }
}
