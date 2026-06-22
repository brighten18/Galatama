using UnityEngine;

[RequireComponent(typeof(FishMovement))]
[RequireComponent(typeof(FishFlockingBehavior))]
[RequireComponent(typeof(FishWanderBehavior))]
public class FishBrain : MonoBehaviour
{
    [Header("Behavior Weights (Ocean)")]
    [Range(0f, 1f)]
    [SerializeField] private float flockingWeight = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float wanderWeight = 0.65f;

    [Range(0f, 1f)]
    [SerializeField] private float boundaryWeight = 0.3f;

    [Header("Ocean Behavior")]
    [Tooltip("Matikan flocking sepenuhnya di ocean untuk performa maksimal (hanya wander + boundary).")]
    [SerializeField] private bool disableFlockingInOcean = false;

    [Tooltip("Di ocean: nonaktifkan cohesion dan alignment, aktifkan hanya separation + crowd repulsion. " +
             "Mencegah ikan clustering tanpa mematikan flocking sepenuhnya.")]
    [SerializeField] private bool oceanSeparationOnly = true;

    [Header("Aquarium Behavior")]
    [Tooltip("Matikan flocking di aquarium agar ikan tidak menempel satu sama lain")]
    [SerializeField] private bool disableFlockingInAquarium = true;

    [Range(0f, 1f)]
    [Tooltip("Bobot wander di aquarium â€” lebih tinggi = ikan lebih aktif menjelajah")]
    [SerializeField] private float aquariumWanderWeight = 1f;

    [Range(0f, 1f)]
    [Tooltip("Bobot boundary di aquarium â€” jaga agar ikan tidak keluar")]
    [SerializeField] private float aquariumBoundaryWeight = 0.9f;

    [Tooltip("Pengali kecepatan di aquarium")]
    [SerializeField] private float aquariumSpeedMultiplier = 0.65f;

    [Header("Terrain Avoidance")]
    [Range(0f, 5f)]
    [Tooltip("Seberapa kuat ikan menghindari permukaan terrain")]
    [SerializeField] private float terrainAvoidanceWeight = 3f;

    private FishMovement movement;
    private FishFlockingBehavior flocking;
    private FishWanderBehavior wander;
    private FishSpawner ownerSpawner;

    private ZoneType currentZoneType = ZoneType.Ocean;
    private bool isCaptured;

    private bool hasTemporaryTarget;
    private Vector3 temporaryTarget;
    private float temporaryTargetStopDistance = 0.5f;

    private Transform foodTarget;
    private float foodTargetStopDistance = 0.45f;

    private Collider lastSetBoundaryCollider;

    [Header("Food Chase")]
    [Tooltip("Pengali kecepatan saat ikan mengejar pelet.")]
    [SerializeField] private float foodChaseSpeedMultiplier = 2.1f;

    [Header("Performance")]
    [Tooltip("Interval minimum untuk menghitung ulang arah AI normal.")]
    [SerializeField] private float decisionIntervalMin = 0.15f;

    [Tooltip("Interval maksimum untuk menghitung ulang arah AI normal.")]
    [SerializeField] private float decisionIntervalMax = 0.4f;

    private Vector3 cachedDirection;
    private float cachedSpeedMultiplier = 1f;
    private float decisionTimer;

    void Awake()
    {
        movement = GetComponent<FishMovement>();
        flocking = GetComponent<FishFlockingBehavior>();
        wander = GetComponent<FishWanderBehavior>();
        cachedDirection = transform.forward.sqrMagnitude > 0.0001f ? transform.forward : Vector3.forward;
        decisionTimer = Random.Range(decisionIntervalMin, Mathf.Max(decisionIntervalMin + 0.01f, decisionIntervalMax));
        InitOceanFlockingMode();
    }

    void Start()
    {
        RecalculateMovementDecision();
    }

    void Update()
    {
        if (isCaptured) return;

        // Kejar target sementara (misal dilempar ke posisi tertentu)
        if (hasTemporaryTarget)
        {
            Vector3 toTarget = temporaryTarget - transform.position;
            if (toTarget.sqrMagnitude > temporaryTargetStopDistance * temporaryTargetStopDistance)
                movement.Move(toTarget, 1.25f);
            return;
        }

        // Kejar makanan
        if (foodTarget != null)
        {
            Vector3 toFood = foodTarget.position - transform.position;
            if (toFood.sqrMagnitude > foodTargetStopDistance * foodTargetStopDistance)
                movement.Move(toFood, foodChaseSpeedMultiplier);
            return;
        }

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            RecalculateMovementDecision();
            decisionTimer = GetNextDecisionInterval();
        }

        if (cachedDirection.sqrMagnitude > 0.0001f)
            movement.Move(cachedDirection, cachedSpeedMultiplier);
    }

    // â”€â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€...

    /// <summary>Terapkan batas ruang gerak dan teruskan ke wander agar target dibuat di dalam bou...
    public void SetBoundary(Bounds bounds)
    {
        movement.SetBoundary(bounds);
        wander.SetBounds(bounds);
        RecalculateMovementDecision();
    }

    /// <summary>Terapkan batas ruang gerak berdasarkan collider (mendukung cylinder/mesh).</summa...
    public void SetBoundary(Collider boundaryCollider)
    {
        if (boundaryCollider == null) return;

        // Guard: skip redundant calls from FishZone.OnTriggerStay setiap frame.
        // Tanpa ini, RecalculateMovementDecision() dipanggil ribuan kali per detik
        // untuk 1000+ ikan, menyebabkan performa sangat berat.
        if (boundaryCollider == lastSetBoundaryCollider) return;
        lastSetBoundaryCollider = boundaryCollider;

        movement.SetBoundary(boundaryCollider);
        wander.SetBounds(boundaryCollider.bounds);
        RecalculateMovementDecision();
    }

    /// <summary>Ubah tipe zona (Ocean / Aquarium).</summary>
    public void SetZoneType(ZoneType zoneType)
    {
        if (currentZoneType == zoneType) return;

        currentZoneType = zoneType;
        wander.SetAquariumMode(zoneType == ZoneType.Aquarium);

        // Terrain avoidance tidak diperlukan di dalam aquarium — skip SampleHeight() per frame.
        movement.SetTerrainAvoidanceEnabled(zoneType != ZoneType.Aquarium);

        InitOceanFlockingMode();
        RecalculateMovementDecision();
        decisionTimer = GetNextDecisionInterval();
    }

    public void SetSpawner(FishSpawner spawner)
    {
        ownerSpawner = spawner;
    }

    public void SetTemporaryTarget(Vector3 target, float stopDistance)
    {
        temporaryTarget = target;
        temporaryTargetStopDistance = Mathf.Max(0.05f, stopDistance);
        hasTemporaryTarget = true;
    }

    public void ClearTemporaryTarget()
    {
        hasTemporaryTarget = false;
    }

    public void SetFoodTarget(Transform target, float stopDistance)
    {
        foodTarget = target;
        foodTargetStopDistance = Mathf.Max(0.05f, stopDistance);
    }

    public void ClearFoodTarget(Transform target)
    {
        if (foodTarget == target)
            foodTarget = null;
    }

    public void ClearFoodTarget()
    {
        foodTarget = null;
    }

    public void OnCaptured(bool destroyObject = true)
    {
        if (isCaptured) return;
        isCaptured = true;
        hasTemporaryTarget = false;
        foodTarget = null;

        FishSpawner spawner = ownerSpawner != null ? ownerSpawner : Object.FindFirstObjectByType<FishSpawner>();
        if (spawner != null)
            spawner.OnFishCaptured();

        if (destroyObject)
            Destroy(gameObject);
    }

    public bool IsCaptured => isCaptured;

    // â”€â”€â”€ Private helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â...

    private bool ShouldUseFlocking()
    {
        if (currentZoneType == ZoneType.Ocean && disableFlockingInOcean) return false;
        if (currentZoneType == ZoneType.Aquarium && disableFlockingInAquarium) return false;
        return true;
    }

    private void InitOceanFlockingMode()
    {
        bool useOceanMode = currentZoneType == ZoneType.Ocean
                            && oceanSeparationOnly
                            && !disableFlockingInOcean;
        flocking.SetOceanMode(useOceanMode);
    }

    private void RecalculateMovementDecision()
    {
        Vector3 wanderForce = wander.CalculateWanderForce();
        Vector3 boundaryForce = movement.GetBoundarySteering();
        Vector3 flockingForce = ShouldUseFlocking() ? flocking.CalculateFlockingForce() : Vector3.zero;
        Vector3 terrainForce = movement.GetTerrainAvoidanceSteering();

        float activeWanderWeight = currentZoneType == ZoneType.Aquarium ? aquariumWanderWeight : wanderWeight;
        float activeBoundaryWeight = currentZoneType == ZoneType.Aquarium ? aquariumBoundaryWeight : boundaryWeight;
        float activeFlockingWeight = ShouldUseFlocking() ? flockingWeight : 0f;
        // Di ocean separation-only, flocking hanya menghasilkan separation/repulsion (tanpa cohesion).
        // Kurangi bobotnya agar wander lebih dominan dan ikan tersebar bebas.
        if (currentZoneType == ZoneType.Ocean && oceanSeparationOnly && activeFlockingWeight > 0f)
            activeFlockingWeight *= 0.5f;

        Vector3 finalDirection =
            flockingForce * activeFlockingWeight +
            wanderForce * activeWanderWeight +
            boundaryForce * activeBoundaryWeight +
            terrainForce * terrainAvoidanceWeight;

        if (finalDirection.sqrMagnitude > 0.0001f)
            cachedDirection = finalDirection.normalized;

        cachedSpeedMultiplier = currentZoneType == ZoneType.Aquarium ? aquariumSpeedMultiplier : 1f;
    }

    private float GetNextDecisionInterval()
    {
        float min = Mathf.Max(0.01f, decisionIntervalMin);
        float max = Mathf.Max(min + 0.01f, decisionIntervalMax);
        return Random.Range(min, max);
    }
}
