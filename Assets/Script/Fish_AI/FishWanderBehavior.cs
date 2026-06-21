using UnityEngine;

public class FishWanderBehavior : MonoBehaviour
{
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float changeTargetInterval = 3f;

    [Header("Aquarium Individual Movement")]
    [SerializeField] private float aquariumChangeIntervalMin = 1.5f;
    [SerializeField] private float aquariumChangeIntervalMax = 5f;

    [Tooltip("Seberapa besar pergerakan vertikal ikan di aquarium (0=datar, 1=bebas naik turun)")]
    [SerializeField] [Range(0f, 1f)] private float aquariumVerticalScale = 0.55f;

    [Tooltip("Seberapa kuat bias individu mempengaruhi arah (0=random murni, 1=selalu ke bias)")]
    [SerializeField] [Range(0f, 1f)] private float individualBiasStrength = 0.2f;

    [Tooltip("Seberapa sering bias individu diperbarui (detik)")]
    [SerializeField] private float biasRefreshInterval = 8f;

    [Header("Ocean Patrol")]
    [Tooltip("Jarak maksimum dari posisi saat ini ke titik patrol baru di ocean.")]
    [SerializeField] private float oceanPatrolRadius = 15f;

    [Tooltip("Jarak ke titik patrol ocean saat dianggap 'sampai' dan target baru dipilih.")]
    [SerializeField] private float oceanTargetReachDistance = 3f;

    [Header("Aquarium Pathing")]
    [Tooltip("Padding aman dari dinding aquarium saat memilih target baru.")]
    [SerializeField] private float aquariumInteriorPadding = 0.35f;

    [Tooltip("Bias ke tengah aquarium agar ikan tidak terus menyisir pinggir.")]
    [SerializeField] [Range(0f, 1f)] private float aquariumCenterBias = 0.65f;

    [Tooltip("Preferensi memilih target yang masih searah gerak saat ini.")]
    [SerializeField] [Range(0f, 1f)] private float aquariumForwardPreference = 0.55f;

    [Tooltip("Jika sudah dekat target aquarium, segera pilih target baru.")]
    [SerializeField] private float aquariumTargetReachDistance = 0.6f;

    private Vector3 oceanTargetPoint;
    private bool hasOceanTargetPoint;
    private Vector3 wanderTarget;
    private Vector3 individualBias;
    private float biasRefreshTimer;
    private float changeTimer;
    private float intervalOffset;
    private bool aquariumMode;
    private bool hasBounds;
    private Bounds cachedBounds;
    private Vector3 aquariumTargetPoint;
    private bool hasAquariumTargetPoint;
    private FishMovement movement;

    void Awake()
    {
        movement = GetComponent<FishMovement>();
        intervalOffset = Random.Range(0f, changeTargetInterval);
        RefreshIndividualBias();
    }

    void Start()
    {
        GenerateNewWanderTarget();
        changeTimer = Mathf.Max(0.1f, changeTargetInterval + intervalOffset);
        biasRefreshTimer = biasRefreshInterval + Random.Range(0f, biasRefreshInterval * 0.5f);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        changeTimer -= dt;
        if (changeTimer <= 0f)
        {
            GenerateNewWanderTarget();
            changeTimer = GetNextChangeInterval();
        }

        biasRefreshTimer -= dt;
        if (biasRefreshTimer <= 0f)
        {
            RefreshIndividualBias();
            biasRefreshTimer = biasRefreshInterval + Random.Range(0f, biasRefreshInterval * 0.5f);
        }
    }

    public Vector3 CalculateWanderForce()
    {
        if (aquariumMode && hasAquariumTargetPoint)
        {
            Vector3 toPoint = aquariumTargetPoint - transform.position;
            if (toPoint.sqrMagnitude <= aquariumTargetReachDistance * aquariumTargetReachDistance)
            {
                GenerateNewWanderTarget();
                toPoint = aquariumTargetPoint - transform.position;
            }

            if (toPoint.sqrMagnitude > 0.0001f)
            {
                toPoint.y *= aquariumVerticalScale;
                return (toPoint + individualBias * individualBiasStrength).normalized;
            }
        }

        // Ocean mode: navigate toward a specific world-space patrol point so fish
        // actively move to different distant locations instead of drifting locally.
        if (!aquariumMode && hasOceanTargetPoint)
        {
            Vector3 toTarget = oceanTargetPoint - transform.position;
            if (toTarget.sqrMagnitude <= oceanTargetReachDistance * oceanTargetReachDistance)
            {
                GenerateNewWanderTarget();
                toTarget = oceanTargetPoint - transform.position;
            }

            if (toTarget.sqrMagnitude > 0.0001f)
                return (toTarget + individualBias * individualBiasStrength).normalized;
        }

        if (wanderTarget.sqrMagnitude <= 0.0001f)
            GenerateNewWanderTarget();

        return wanderTarget.normalized;
    }

    public void SetAquariumMode(bool enabled)
    {
        if (aquariumMode == enabled) return;

        aquariumMode = enabled;
        hasOceanTargetPoint = false; // Force a new patrol target when switching to ocean mode.
        GenerateNewWanderTarget();
        changeTimer = GetNextChangeInterval();
    }

    public void SetBounds(Bounds bounds)
    {
        cachedBounds = bounds;
        hasBounds = true;
    }

    private float GetNextChangeInterval()
    {
        if (aquariumMode)
        {
            float min = Mathf.Max(0.1f, aquariumChangeIntervalMin);
            float max = Mathf.Max(min + 0.1f, aquariumChangeIntervalMax);
            return Random.Range(min, max);
        }

        float jitter = changeTargetInterval * 0.25f;
        return Mathf.Max(0.1f, changeTargetInterval + Random.Range(-jitter, jitter));
    }

    private void GenerateNewWanderTarget()
    {
        if (aquariumMode)
            GenerateAquariumWanderTarget();
        else
            GenerateOceanWanderTarget();

        if (wanderTarget.sqrMagnitude <= 0.0001f)
            wanderTarget = individualBias.sqrMagnitude > 0.0001f ? individualBias : Vector3.forward;
    }

    private void GenerateAquariumWanderTarget()
    {
        if (hasBounds)
        {
            Vector3 pos = transform.position;
            aquariumTargetPoint = GetBestAquariumTargetPoint(pos);
            hasAquariumTargetPoint = true;

            Vector3 toTarget = aquariumTargetPoint - pos;
            toTarget.y *= aquariumVerticalScale;
            wanderTarget = toTarget + individualBias * individualBiasStrength;
            return;
        }

        hasAquariumTargetPoint = false;
        Vector3 random = Random.insideUnitSphere;
        random.y *= aquariumVerticalScale;
        wanderTarget = random + individualBias * individualBiasStrength;
    }

    private void GenerateOceanWanderTarget()
    {
        // Pick a world-space patrol point within oceanPatrolRadius of the current position.
        // Each fish picks a different distant destination, so fish naturally spread across the zone.
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir.y *= 0.15f; // Limit vertical variance — fish stay roughly at their current depth.
        if (randomDir.sqrMagnitude <= 0.0001f)
            randomDir = Vector3.forward;
        randomDir.Normalize();

        float distance = Random.Range(oceanPatrolRadius * 0.4f, oceanPatrolRadius);
        oceanTargetPoint = transform.position + randomDir * distance;
        hasOceanTargetPoint = true;

        // wanderTarget is kept as a fallback direction in case hasOceanTargetPoint fails.
        wanderTarget = randomDir;
    }

    private void RefreshIndividualBias()
    {
        individualBias = Random.insideUnitSphere;
        individualBias.y *= 0.3f;
        individualBias = individualBias.normalized;
    }

    private Vector3 GetBestAquariumTargetPoint(Vector3 currentPosition)
    {
        Vector3 center = cachedBounds.center;
        Vector3 min = cachedBounds.min;
        Vector3 max = cachedBounds.max;
        float pad = Mathf.Max(0.01f, aquariumInteriorPadding);
        Vector3 forward = movement != null ? movement.GetForward() : transform.forward;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;

        Vector3 bestPoint = center;
        float bestScore = float.NegativeInfinity;

        const int candidateCount = 4;
        for (int i = 0; i < candidateCount; i++)
        {
            Vector3 candidate = new Vector3(
                GetSafeRandomRange(min.x, max.x, pad),
                GetSafeRandomRange(min.y, max.y, pad),
                GetSafeRandomRange(min.z, max.z, pad)
            );

            candidate.y = Mathf.Lerp(currentPosition.y, candidate.y, aquariumVerticalScale);

            Vector3 toCandidate = candidate - currentPosition;
            float directionScore = 0f;
            if (toCandidate.sqrMagnitude > 0.0001f)
                directionScore = Vector3.Dot(forward.normalized, toCandidate.normalized);

            float centerDistance = Vector3.Distance(candidate, center);
            float maxCenterDistance = cachedBounds.extents.magnitude;
            float centerScore = 1f - Mathf.Clamp01(centerDistance / Mathf.Max(0.01f, maxCenterDistance));

            float score =
                directionScore * aquariumForwardPreference +
                centerScore * aquariumCenterBias +
                Random.Range(0f, 0.15f);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = candidate;
            }
        }

        return bestPoint;
    }

    private float GetSafeRandomRange(float min, float max, float pad)
    {
        float paddedMin = min + pad;
        float paddedMax = max - pad;

        if (paddedMin > paddedMax)
            return (min + max) * 0.5f;

        return Random.Range(paddedMin, paddedMax);
    }
}
