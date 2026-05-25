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

    private Vector3 wanderTarget;

    // Bias individu — diperbarui secara berkala agar tiap ikan punya arah favorit yang berbeda
    private Vector3 individualBias;
    private float biasRefreshTimer;

    private float changeTimer;
    private float intervalOffset;
    private bool aquariumMode;

    // Simpan referensi bounds agar target bisa dihasilkan di dalam aquarium
    private bool hasBounds;
    private Bounds cachedBounds;

    void Awake()
    {
        // Offset acak agar semua ikan tidak ganti target secara bersamaan
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

        // Perbarui bias individu secara berkala agar arah tiap ikan bervariasi dari waktu ke waktu
        biasRefreshTimer -= dt;
        if (biasRefreshTimer <= 0f)
        {
            RefreshIndividualBias();
            biasRefreshTimer = biasRefreshInterval + Random.Range(0f, biasRefreshInterval * 0.5f);
        }
    }

    /// <summary>Kembalikan arah wander untuk digunakan FishBrain.</summary>
    public Vector3 CalculateWanderForce()
    {
        if (wanderTarget.sqrMagnitude <= 0.0001f)
            GenerateNewWanderTarget();

        return wanderTarget.normalized;
    }

    /// <summary>Aktifkan/nonaktifkan mode aquarium dan opsional berikan bounds untuk target generation.</summary>
    public void SetAquariumMode(bool enabled)
    {
        if (aquariumMode == enabled) return;

        aquariumMode = enabled;
        GenerateNewWanderTarget();
        changeTimer = GetNextChangeInterval();
    }

    /// <summary>Berikan bounds aquarium agar target wander dihasilkan di dalam batas yang benar.</summary>
    public void SetBounds(Bounds bounds)
    {
        cachedBounds = bounds;
        hasBounds = true;
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

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
            // Hasilkan titik target dalam bounds, lalu ubah menjadi arah dari posisi saat ini
            Vector3 pos = transform.position;
            Vector3 min = cachedBounds.min;
            Vector3 max = cachedBounds.max;

            // Padding kecil agar tidak target persis di dinding
            float pad = 0.15f;
            Vector3 targetPoint = new Vector3(
                Random.Range(min.x + pad, max.x - pad),
                Random.Range(min.y + pad, max.y - pad),
                Random.Range(min.z + pad, max.z - pad)
            );

            Vector3 toTarget = targetPoint - pos;

            // Kurangi komponen Y jika aquariumVerticalScale < 1
            toTarget.y *= aquariumVerticalScale;

            // Tambahkan bias individu yang lemah
            wanderTarget = toTarget + individualBias * individualBiasStrength;
        }
        else
        {
            // Tidak ada bounds info — hasilkan arah sphere acak
            Vector3 random = Random.insideUnitSphere;
            random.y *= aquariumVerticalScale;
            wanderTarget = random + individualBias * individualBiasStrength;
        }
    }

    private void GenerateOceanWanderTarget()
    {
        wanderTarget = new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            Random.Range(-wanderRadius * 0.3f, wanderRadius * 0.3f),
            Random.Range(-wanderRadius, wanderRadius)
        );
    }

    private void RefreshIndividualBias()
    {
        // Bias baru dengan arah horizontal yang dominan dan sedikit vertical jitter
        individualBias = Random.insideUnitSphere;
        individualBias.y *= 0.3f;
        individualBias = individualBias.normalized;
    }
}
