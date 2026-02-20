using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class InteractableObject : MonoBehaviour
{
    public bool isLookingAtInteractingRange { get; private set; } // Apakah pemain berada dalam jangkauan interaksi
    public StarterAssetsInputs _Input; // Referensi ke StarterAssetsInputs untuk mendeteksi input pemain
    public InteractUIManager InterUIMgr;
    [SerializeField] private string itemName; // Nama objek yang tampil di UI

    private void Awake()
    {
        _Input = new StarterAssetsInputs();
        
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        _Input = player.GetComponent<StarterAssetsInputs>();
    }

    void Update()
    {
        if (isLookingAtInteractingRange && _Input.Interact)
        {
            InteractObject();
            Destroy(gameObject); // Hapus objek setelah berinteraksi
        }
    }

    public virtual string GetItemName()
    {
        return itemName;
    }

    // Method ini akan dipanggil saat pemain menekan tombol interaksi
    public virtual void InteractObject()
    {
        Debug.Log($"Berinteraksi dengan {itemName}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isLookingAtInteractingRange = true;
        }
         
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isLookingAtInteractingRange = false;
        }
    }
}
