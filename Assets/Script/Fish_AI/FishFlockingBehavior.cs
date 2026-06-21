using System.Collections.Generic;
using UnityEngine;

public class FishFlockingBehavior : MonoBehaviour
{
    [Header("Flocking Settings")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask fishLayer;

    [Header("Weights")]
    [Range(0f, 2f)]
    [SerializeField] private float cohesionWeight = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float separationWeight = 1.5f;

    [Range(0f, 2f)]
    [SerializeField] private float alignmentWeight = 1f;

    [Header("Separation")]
    [SerializeField] private float minSeparationDistance = 1.5f;

    [Header("Crowd Control")]
    [Tooltip("Jumlah maksimum ikan yang diizinkan dalam detectionRadius sebelum crowd repulsion aktif. " +
             "Saat fish count >= nilai ini, ikan akan didorong kuat keluar dari kerumunan.")]
    [SerializeField] private int maxFishInArea = 5;

    [Tooltip("Kekuatan dorong ikan keluar saat area terlalu padat (fish count >= maxFishInArea).")]
    [Range(0f, 5f)]
    [SerializeField] private float crowdRepulsionStrength = 3f;

    [Header("Performance")]
    [Tooltip("Batas maksimum tetangga yang diproses per evaluasi.")]
    [SerializeField] private int maxNeighbors = 12;

    [Tooltip("Interval evaluasi flocking agar tidak dihitung tiap frame.")]
    [SerializeField] private float refreshInterval = 0.12f;

    // Set oleh FishBrain saat zona berubah — tidak perlu diserialisasi.
    private bool oceanMode = false;

    private readonly List<FishMovement> nearbyFish = new List<FishMovement>();
    private Collider[] overlapResults;
    private Vector3 cachedFlockingForce;
    private float refreshTimer;

    void Awake()
    {
        overlapResults = new Collider[Mathf.Max(8, maxNeighbors * 2)];
        refreshTimer = Random.Range(0f, Mathf.Max(0.02f, refreshInterval));
    }

    /// <summary>
    /// Aktifkan/nonaktifkan ocean mode. Di ocean, cohesion dan alignment dinonaktifkan
    /// sehingga ikan tidak saling tertarik. Hanya separation dan crowd repulsion yang aktif.
    /// Dipanggil oleh FishBrain saat zona berubah.
    /// </summary>
    public void SetOceanMode(bool isOcean)
    {
        oceanMode = isOcean;
    }

    public Vector3 CalculateFlockingForce()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return cachedFlockingForce;

        refreshTimer = Mathf.Max(0.02f, refreshInterval);
        FindNearbyFish();

        if (nearbyFish.Count == 0)
        {
            cachedFlockingForce = Vector3.zero;
            return cachedFlockingForce;
        }

        bool overcrowded = nearbyFish.Count >= maxFishInArea;
        Vector3 separation = CalculateSeparation();
        Vector3 combined;

        if (overcrowded)
        {
            // Area terlalu padat: dorong kuat keluar dari pusat massa.
            // Cohesion dan alignment diabaikan sepenuhnya.
            Vector3 repulsion = CalculateCrowdRepulsion();
            combined = separation * separationWeight + repulsion * crowdRepulsionStrength;
        }
        else if (oceanMode)
        {
            // Ocean tidak padat: hanya separation aktif.
            // Tanpa cohesion/alignment, ikan tidak saling tertarik.
            combined = separation * separationWeight;
        }
        else
        {
            // Aquarium / mode normal: semua komponen flocking aktif.
            Vector3 cohesion = CalculateCohesion();
            Vector3 alignment = CalculateAlignment();
            combined = cohesion * cohesionWeight
                     + separation * separationWeight
                     + alignment * alignmentWeight;
        }

        cachedFlockingForce = combined.sqrMagnitude > 0.0001f
            ? combined.normalized
            : Vector3.zero;

        return cachedFlockingForce;
    }

    private void FindNearbyFish()
    {
        nearbyFish.Clear();

        if (overlapResults == null || overlapResults.Length < Mathf.Max(8, maxNeighbors * 2))
            overlapResults = new Collider[Mathf.Max(8, maxNeighbors * 2)];

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            overlapResults,
            fishLayer
        );

        int processed = 0;
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapResults[i];
            if (col == null || col.transform == transform)
                continue;

            FishMovement otherFish = col.GetComponent<FishMovement>();
            if (otherFish == null)
                continue;

            nearbyFish.Add(otherFish);
            processed++;

            if (processed >= maxNeighbors)
                break;
        }
    }

    /// <summary>Hitung arah dorong keluar dari pusat massa semua tetangga.</summary>
    private Vector3 CalculateCrowdRepulsion()
    {
        Vector3 centerOfMass = Vector3.zero;
        for (int i = 0; i < nearbyFish.Count; i++)
            centerOfMass += nearbyFish[i].GetPosition();

        centerOfMass /= nearbyFish.Count;

        Vector3 repulsion = transform.position - centerOfMass;
        if (repulsion.sqrMagnitude > 0.0001f)
            return repulsion.normalized;

        // Tepat di tengah massa: pilih arah horizontal acak agar tidak stuck.
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir.y = 0f;
        return randomDir.sqrMagnitude > 0.0001f ? randomDir.normalized : Vector3.right;
    }

    private Vector3 CalculateCohesion()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        for (int i = 0; i < nearbyFish.Count; i++)
            centerOfMass += nearbyFish[i].GetPosition();

        centerOfMass /= nearbyFish.Count;
        return (centerOfMass - transform.position).normalized;
    }

    private Vector3 CalculateSeparation()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;

        Vector3 separationForce = Vector3.zero;
        Vector3 currentPosition = transform.position;

        for (int i = 0; i < nearbyFish.Count; i++)
        {
            Vector3 otherPosition = nearbyFish[i].GetPosition();
            Vector3 offset = currentPosition - otherPosition;
            float sqrDistance = offset.sqrMagnitude;

            if (sqrDistance > 0f && sqrDistance < minSeparationDistance * minSeparationDistance)
            {
                float distance = Mathf.Sqrt(sqrDistance);
                separationForce += offset.normalized / distance;
            }
        }

        return separationForce.sqrMagnitude > 0.0001f ? separationForce.normalized : Vector3.zero;
    }

    private Vector3 CalculateAlignment()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;

        Vector3 averageDirection = Vector3.zero;
        for (int i = 0; i < nearbyFish.Count; i++)
            averageDirection += nearbyFish[i].GetForward();

        averageDirection /= nearbyFish.Count;
        return averageDirection.sqrMagnitude > 0.0001f ? averageDirection.normalized : Vector3.zero;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minSeparationDistance);
    }
}
