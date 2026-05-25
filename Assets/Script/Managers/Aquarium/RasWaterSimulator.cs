using UnityEngine;

/// <summary>
/// Status DO (Dissolved Oxygen) berdasarkan ambang batas RAS Galatama.
/// </summary>
public enum DOStatus
{
    Safe,     // DO >= 5 mg/L
    Danger,   // DO 4-5 mg/L
    Critical  // DO < 4 mg/L
}

/// <summary>
/// Simulasi kualitas air RAS Galatama berbasis Time.deltaTime (5x Slower System).
///
/// Hubungan antar parameter (sesuai diagram):
///   Temperature (+) --> Salinitas (+)
///   Temperature (+) --> pH (+)
///   Temperature (+/-) --> Pakan (-)   [kedua ekstrem suhu menurunkan efektivitas pakan]
///   Salinitas (+>35) --> DO (-)
///   Salinitas (-<32) --> Ammonia (+)  [x1.2 produksi NH3]
///   pH (+>8.5) --> Ammonia (+)        [toksisitas NH3 x2]
///   Ammonia (safe) --> DO (+)
///   Ammonia (danger/critical) --> DO (-)
///   Jumlah Ikan (Feses) --> Ammonia (+)
///   Pakan (sisa) --> Ammonia (+)      [pembusukan pakan]
///   Pakan (sisa) --> pH (-)           [asam organik dari dekomposisi]
/// </summary>
public class RasWaterSimulator : MonoBehaviour
{
    // =========================================================================
    // KONSTANTA LAJU PER-DETIK (5x Slower System)
    // =========================================================================

    // --- Temperature ---
    // +1 derajat per 5 menit jika Cooler mati
    private const float TEMP_RISE_PER_SEC           = 1f / 300f;
    private const float TEMP_THRESHOLD_HIGH          = 31f;
    private const float TEMP_STATIC_DO_BASELINE      = 27f; // batas bawah efek statis DO

    // --- DO akibat Temperature tinggi ---
    // -0.03 mg/L per 25 detik saat suhu > 31
    private const float DO_LOSS_HIGH_TEMP_PER_SEC    = 0.03f / 25f;

    // -0.05 mg/L per +1 derajat di atas 27, dihaluskan per 5 menit
    private const float DO_STATIC_PER_DEG_PER_SEC    = 0.05f / 300f;

    // --- Salinitas akibat Temperature tinggi ---
    // +1 ppt per 5 menit saat suhu > 31
    private const float SAL_RISE_HIGH_TEMP_PER_SEC   = 1f / 300f;

    // --- pH akibat Temperature tinggi ---
    // +0.1 per 5 menit saat suhu > 31
    private const float PH_RISE_HIGH_TEMP_PER_SEC    = 0.1f / 300f;

    // --- Salinitas ---
    private const float SAL_THRESHOLD_HIGH           = 35f;   // ambang efek DO
    private const float SAL_THRESHOLD_LOW_STRESS     = 32f;   // ambang stres / NH3 naik

    // DO turun -0.1 per 2 menit per +1 ppt di atas 35
    private const float DO_LOSS_SAL_PER_PPT_PER_SEC  = 0.1f / 120f;

    // NH3 naik 20% saat salinitas < 32
    private const float NH3_LOW_SAL_MULTIPLIER        = 1.20f;

    // --- Ammonia ---
    private const float NH3_SAFE_MAX                  = 0.1f;
    private const float NH3_DANGER_MAX                = 0.5f;
    private const float PH_NH3_TOXIC_THRESHOLD        = 8.5f;

    // NH3 naik dasar per ikan: 0.08 per 5 menit
    private const float NH3_PER_FISH_PER_SEC          = 0.08f / 300f;

    // --- DO akibat Ammonia ---
    // SAFE: +0.1 per 4 menit
    private const float DO_GAIN_NH3_SAFE_PER_SEC      = 0.1f / 240f;

    // DANGER: -0.02 per 5 menit per ikan
    private const float DO_LOSS_NH3_DANGER_PER_SEC    = 0.02f / 300f;

    // CRITICAL: -0.04 per 5 menit per ikan
    private const float DO_LOSS_NH3_CRITICAL_PER_SEC  = 0.04f / 300f;

    // --- Pakan (Food Load) ---
    // Setiap pellet yang tidak dimakan busuk dalam ~2 menit (lifeTime pellet = 18 detik,
    // tapi food load melambangkan efek akumulatif dari sesi makan).
    // Nilai per-unit mengikuti 5x Slower: pakan jadi lebih lambat bereaksi ke kimia air.
    private const float FOOD_LOAD_DECAY_PER_SEC       = 1f / 120f;  // habis dalam ~2 menit
    private const float FOOD_NH3_PER_UNIT_PER_SEC     = 0.0002f;    // pakan busuk -> NH3
    private const float FOOD_PH_DROP_PER_UNIT_PER_SEC = 0.00004f;   // asam organik -> pH turun

    // --- Efektivitas Pakan ---
    // KEDUA ekstrem suhu + pH rendah menurunkan efektivitas pakan ke 60%
    private const float TEMP_LOW_FEED_THRESHOLD       = 24f;
    private const float FEED_EFFICIENCY_LOW            = 0.60f;
    private const float FEED_NORMAL                    = 1.00f;
    private const float PH_LOW_FEED_THRESHOLD          = 7.8f;

    // --- DO Thresholds ---
    private const float DO_DANGER_THRESHOLD            = 5f;
    private const float DO_CRITICAL_THRESHOLD          = 4f;

    // =========================================================================
    // STATE
    // =========================================================================

    private WaterQualityState water;
    private bool              coolerActive;

    /// <summary>
    /// Beban pakan sisa yang belum dimakan (food load). Naik saat pellet di-spawn,
    /// turun saat dimakan ikan atau membusuk alami.
    /// </summary>
    private float foodLoad;

    // =========================================================================
    // INISIALISASI
    // =========================================================================

    /// <summary>
    /// Inisialisasi simulator. Harus dipanggil sekali dari AquariumSystem.Awake().
    /// </summary>
    public void Initialize(WaterQualityState waterState, bool isCoolerInstalled)
    {
        water        = waterState;
        coolerActive = isCoolerInstalled;
        foodLoad     = 0f;
    }

    /// <summary>
    /// Perbarui status Cooler (dipanggil saat equipment dipasang/dilepas).
    /// </summary>
    public void SetCoolerActive(bool active) => coolerActive = active;

    // =========================================================================
    // FOOD LOAD API
    // =========================================================================

    /// <summary>
    /// Tambahkan beban pakan saat pellet di-spawn (sebelum dimakan ikan).
    /// Diagram: Pakan (+) -> NH3 (+) dan Pakan (+) -> pH (-)
    /// </summary>
    /// <param name="amount">Jumlah unit pakan (biasanya pelletCount * loadPerPellet).</param>
    public void AddFoodLoad(float amount)
    {
        if (amount <= 0f) return;
        foodLoad += amount;
    }

    /// <summary>
    /// Kurangi beban pakan saat satu pellet dimakan oleh ikan.
    /// </summary>
    /// <param name="amount">Jumlah unit pakan yang dikonsumsi.</param>
    public void ConsumeFoodLoad(float amount)
    {
        if (amount <= 0f) return;
        foodLoad = Mathf.Max(0f, foodLoad - amount);
    }

    /// <summary>Kembalikan beban pakan saat ini (untuk debugging/UI).</summary>
    public float FoodLoad => foodLoad;

    // =========================================================================
    // TICK UTAMA
    // =========================================================================

    /// <summary>
    /// Dipanggil tiap frame dari AquariumSystem.Update().
    /// Menerapkan semua hubungan diagram secara kontinu berbasis waktu nyata.
    /// </summary>
    /// <param name="dt">Time.deltaTime dari frame saat ini.</param>
    /// <param name="livingFishCount">Jumlah ikan hidup di akuarium.</param>
    public void Tick(float dt, int livingFishCount)
    {
        if (water == null) return;

        // Urutan eksekusi mengikuti arah alir diagram (upstream ke downstream)
        TickTemperature(dt);              // Temperature

        TickSalinityFromTemperature(dt);  // Temperature --> Salinitas
        TickPhFromTemperature(dt);        // Temperature --> pH
        TickDOFromTemperature(dt);        // Temperature --> DO

        TickDOFromSalinity(dt);           // Salinitas --> DO
        TickAmmoniaFromFish(dt, livingFishCount); // Jumlah Ikan --> Ammonia

        TickFoodDecomposition(dt);        // Pakan --> Ammonia, Pakan --> pH

        TickDOFromAmmonia(dt, livingFishCount);   // Ammonia --> DO

        water.Clamp();
    }

    // =========================================================================
    // QUERY API
    // =========================================================================

    /// <summary>Kembalikan status DO saat ini sebagai enum.</summary>
    public DOStatus GetDOStatus()
    {
        if (water == null) return DOStatus.Safe;
        if (water.oxygen < DO_CRITICAL_THRESHOLD) return DOStatus.Critical;
        if (water.oxygen < DO_DANGER_THRESHOLD)   return DOStatus.Danger;
        return DOStatus.Safe;
    }

    /// <summary>
    /// Kembalikan multiplier efektivitas pakan berdasarkan kondisi air.
    /// Diagram: Temperature (+/-) --> Pakan (-) dan pH (-) --> Pakan (-)
    /// Kedua ekstrem suhu (terlalu dingin ATAU terlalu panas) menurunkan efektivitas.
    /// </summary>
    public float GetFeedEfficiency()
    {
        if (water == null) return FEED_NORMAL;

        bool tooCold = water.temperature < TEMP_LOW_FEED_THRESHOLD;
        bool tooHot  = water.temperature > TEMP_THRESHOLD_HIGH;      // suhu > 31 juga merusak nafsu makan
        bool lowPh   = water.ph < PH_LOW_FEED_THRESHOLD;

        return (tooCold || tooHot || lowPh) ? FEED_EFFICIENCY_LOW : FEED_NORMAL;
    }

    /// <summary>
    /// Kembalikan apakah pH saat ini menyebabkan toksisitas NH3 berlipat ganda.
    /// Diagram: pH (+) --> Ammonia (+)
    /// </summary>
    public bool IsNH3ToxicityDoubled() => water != null && water.ph > PH_NH3_TOXIC_THRESHOLD;

    // =========================================================================
    // SUB-SIMULASI PRIVAT
    // =========================================================================

    // --- Temperature ---

    private void TickTemperature(float dt)
    {
        if (!coolerActive)
            water.temperature += TEMP_RISE_PER_SEC * dt;
    }

    // --- Temperature --> DO ---

    private void TickDOFromTemperature(float dt)
    {
        // DO turun saat suhu melewati 31°C (efek dinamis)
        if (water.temperature > TEMP_THRESHOLD_HIGH)
            water.oxygen -= DO_LOSS_HIGH_TEMP_PER_SEC * dt;

        // Efek statis: setiap +1 derajat di atas 27°C mengurangi DO secara permanen
        float excessDeg = Mathf.Max(0f, water.temperature - TEMP_STATIC_DO_BASELINE);
        if (excessDeg > 0f)
            water.oxygen -= DO_STATIC_PER_DEG_PER_SEC * excessDeg * dt;
    }

    // --- Temperature --> Salinitas ---

    private void TickSalinityFromTemperature(float dt)
    {
        if (water.temperature > TEMP_THRESHOLD_HIGH)
            water.salinity += SAL_RISE_HIGH_TEMP_PER_SEC * dt;
    }

    // --- Temperature --> pH ---

    private void TickPhFromTemperature(float dt)
    {
        if (water.temperature > TEMP_THRESHOLD_HIGH)
            water.ph += PH_RISE_HIGH_TEMP_PER_SEC * dt;
    }

    // --- Salinitas --> DO ---

    private void TickDOFromSalinity(float dt)
    {
        // DO turun untuk setiap +1 ppt salinitas di atas 35
        float excessPpt = Mathf.Max(0f, water.salinity - SAL_THRESHOLD_HIGH);
        if (excessPpt > 0f)
            water.oxygen -= DO_LOSS_SAL_PER_PPT_PER_SEC * excessPpt * dt;
    }

    // --- Jumlah Ikan (Feses) --> Ammonia ---

    private void TickAmmoniaFromFish(float dt, int livingFishCount)
    {
        if (livingFishCount <= 0) return;

        // Salinitas rendah (<32 ppt) mempercepat produksi NH3 sebesar 20%
        // Diagram: Salinitas (-) --> Ammonia (+)
        float salMultiplier = (water.salinity < SAL_THRESHOLD_LOW_STRESS)
            ? NH3_LOW_SAL_MULTIPLIER
            : 1f;

        water.ammonia += NH3_PER_FISH_PER_SEC * livingFishCount * salMultiplier * dt;
    }

    // --- Pakan --> Ammonia (+) dan Pakan --> pH (-) ---

    private void TickFoodDecomposition(float dt)
    {
        if (foodLoad <= 0f) return;

        // Pakan yang tidak dimakan membusuk dan menghasilkan ammonia
        // Diagram: Pakan (+) --> Ammonia (+)
        water.ammonia += FOOD_NH3_PER_UNIT_PER_SEC * foodLoad * dt;

        // Dekomposisi pakan menghasilkan asam organik yang menurunkan pH
        // Diagram: Pakan (+) --> pH (-)
        water.ph -= FOOD_PH_DROP_PER_UNIT_PER_SEC * foodLoad * dt;

        // Pakan membusuk secara eksponensial (meluruh dari jumlah saat ini)
        foodLoad = Mathf.Max(0f, foodLoad - FOOD_LOAD_DECAY_PER_SEC * foodLoad * dt);
    }

    // --- Ammonia --> DO ---

    private void TickDOFromAmmonia(float dt, int livingFishCount)
    {
        float nh3 = water.ammonia;

        if (nh3 <= NH3_SAFE_MAX)
        {
            // Ammonia terkontrol -> sistem nitrifikasi aktif -> DO naik
            water.oxygen += DO_GAIN_NH3_SAFE_PER_SEC * dt;
        }
        else if (nh3 <= NH3_DANGER_MAX)
        {
            // Ammonia berbahaya -> mengganggu respirasi ikan -> DO turun per ikan
            water.oxygen -= DO_LOSS_NH3_DANGER_PER_SEC * Mathf.Max(1, livingFishCount) * dt;
        }
        else
        {
            // Ammonia kritis -> respirasi terganggu berat -> DO turun cepat per ikan
            water.oxygen -= DO_LOSS_NH3_CRITICAL_PER_SEC * Mathf.Max(1, livingFishCount) * dt;
        }
    }
}
