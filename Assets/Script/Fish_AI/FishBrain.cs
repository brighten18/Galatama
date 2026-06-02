using UnityEngine;

[RequireComponent(typeof(FishMovement))]
[RequireComponent(typeof(FishFlockingBehavior))]
[RequireComponent(typeof(FishWanderBehavior))]
public class FishBrain : MonoBehaviour
{
    [Header("Behavior Weights (Ocean)")]
    [Range(0f, 1f)]
    [SerializeField] private float flockingWeight = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float wanderWeight = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float boundaryWeight = 0.8f;

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

    void Awake()
    {
        movement = GetComponent<FishMovement>();
        flocking = GetComponent<FishFlockingBehavior>();
        wander = GetComponent<FishWanderBehavior>();
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
                movement.Move(toFood, 1.35f);
            return;
        }

        // Hitung gaya gabungan
        Vector3 wanderForce    = wander.CalculateWanderForce();
        Vector3 boundaryForce  = movement.GetBoundarySteering();
        Vector3 flockingForce  = ShouldUseFlocking() ? flocking.CalculateFlockingForce() : Vector3.zero;
        Vector3 terrainForce   = movement.GetTerrainAvoidanceSteering();

        float activeWanderWeight   = currentZoneType == ZoneType.Aquarium ? aquariumWanderWeight   : wanderWeight;
        float activeBoundaryWeight = currentZoneType == ZoneType.Aquarium ? aquariumBoundaryWeight  : boundaryWeight;
        float activeFlockingWeight = ShouldUseFlocking() ? flockingWeight : 0f;

        Vector3 finalDirection =
            flockingForce  * activeFlockingWeight  +
            wanderForce    * activeWanderWeight    +
            boundaryForce  * activeBoundaryWeight  +
            terrainForce   * terrainAvoidanceWeight;

        if (finalDirection.sqrMagnitude > 0.0001f)
        {
            float speedMult = currentZoneType == ZoneType.Aquarium ? aquariumSpeedMultiplier : 1f;
            movement.Move(finalDirection, speedMult);
        }
    }

    // â”€â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€...

    /// <summary>Terapkan batas ruang gerak dan teruskan ke wander agar target dibuat di dalam bou...
    public void SetBoundary(Bounds bounds)
    {
        movement.SetBoundary(bounds);
        wander.SetBounds(bounds);
    }

    /// <summary>Terapkan batas ruang gerak berdasarkan collider (mendukung cylinder/mesh).</summa...
    public void SetBoundary(Collider boundaryCollider)
    {
        if (boundaryCollider == null) return;

        movement.SetBoundary(boundaryCollider);
        wander.SetBounds(boundaryCollider.bounds);
    }

    /// <summary>Ubah tipe zona (Ocean / Aquarium).</summary>
    public void SetZoneType(ZoneType zoneType)
    {
        if (currentZoneType == zoneType) return;

        currentZoneType = zoneType;
        wander.SetAquariumMode(zoneType == ZoneType.Aquarium);
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
        return currentZoneType != ZoneType.Aquarium || !disableFlockingInAquarium;
    }
}
