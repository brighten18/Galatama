using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AquariumFoodPellet : MonoBehaviour
{
    [SerializeField] private float sinkSpeed = 0.45f;
    [SerializeField] private float lifeTime = 18f;
    [SerializeField] private float bottomOffset = 0.1f;

    private AquariumSystem aquarium;
    private float hungerReduction = 35f;
    private Bounds swimBounds;
    private bool initialized;

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
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        transform.position += Vector3.down * sinkSpeed * Time.deltaTime;
        lifeTime -= Time.deltaTime;

        if (initialized && transform.position.y <= swimBounds.min.y + bottomOffset)
        {
            Destroy(gameObject);
            return;
        }

        if (lifeTime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        FishBrain fish = other.GetComponentInParent<FishBrain>();
        if (fish == null || aquarium == null)
            return;

        aquarium.TryConsumeFood(this, fish);
    }
}
