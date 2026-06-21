using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AquariumFoodPellet : MonoBehaviour
{
    [SerializeField] private float sinkSpeed = 0.45f;
    [SerializeField] private float lifeTime = 18f;
    [SerializeField] private float bottomOffset = 0.1f;

    [Tooltip("Radius dalam world-space untuk mendeteksi ikan terdekat")]
    [SerializeField] private float detectionRadius = 0.15f;

    private AquariumSystem aquarium;
    private float hungerReduction = 100f;
    private Bounds swimBounds;
    private bool initialized;
    private int fishLayerMask;

    // Buffer statis agar OverlapSphereNonAlloc tidak alokasi heap setiap frame
    private static readonly Collider[] overlapBuffer = new Collider[8];

    public float HungerReduction => hungerReduction;

    public void Initialize(AquariumSystem owner, float feedValue)
    {
        aquarium = owner;
        hungerReduction = Mathf.Abs(feedValue);
        swimBounds = aquarium != null ? aquarium.SwimBounds : new Bounds(transform.position, Vector3.one);
        initialized = true;
    }

    private void Awake()
    {
        // Pastikan collider bersifat trigger agar tidak memblokir gerakan ikan
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // Cache layer mask ikan satu kali saat awake
        fishLayerMask = LayerMask.GetMask("Fish");
    }

    private void Update()
    {
        // Turunkan posisi pelet secara perlahan
        transform.position += Vector3.down * sinkSpeed * Time.deltaTime;
        lifeTime -= Time.deltaTime;

        // Hancurkan jika menyentuh dasar atau kehabisan waktu
        if (initialized && transform.position.y <= swimBounds.min.y + bottomOffset)
        {
            Destroy(gameObject);
            return;
        }

        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Deteksi ikan menggunakan proximity check manual.
        // Lebih andal daripada OnTriggerEnter karena tidak bergantung pada
        // Rigidbody, AutoSyncTransforms, atau ukuran collider pelet.
        if (aquarium == null)
            return;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            overlapBuffer,
            fishLayerMask);

        for (int i = 0; i < count; i++)
        {
            FishBrain fish = overlapBuffer[i].GetComponentInParent<FishBrain>();
            if (fish != null)
            {
                // TryConsumeFood akan memanggil Destroy(gameObject) pada pelet ini
                aquarium.TryConsumeFood(this, fish);
                return;
            }
        }
    }

    private void OnDestroy()
    {
        if (aquarium != null)
            aquarium.NotifyFoodRemoved(this);
    }
}
