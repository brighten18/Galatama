using UnityEngine;

/// <summary>
/// Komponen mesin aquarium dunia (Aerator, Heater, Chiller, WaterPump).
/// Saat player berinteraksi, nilai RAS pada AquariumSystem target langsung diubah sesuai
/// parameter yang dikonfigurasi di Inspector.
///
/// RESOLUSI AQUARIUM:
///   1. Field aquariumSystem di Inspector (paling prioritas)
///   2. GetComponentInParent — jika mesin adalah child dari AquariumSystem
///   3. AquariumSystem.CurrentOpen — aquarium yang sedang dibuka player
///   4. AquariumSystem terdekat dalam radius searchRadius
///   5. FindFirstObjectByType — fallback terakhir
/// </summary>
public class AquariumMachineInteractable : InteractableObject
{
    [SerializeField] private AquariumSystem aquariumSystem;
    [SerializeField] private AquariumEquipmentRole machineRole = AquariumEquipmentRole.Aerator;

    [Header("Machine Values")]
    [Tooltip("Cooldown dalam detik setelah satu interaksi")]
    [SerializeField] private float cooldownSeconds = 4f;

    [Tooltip("Aerator: jumlah O2 yang ditambahkan per interaksi")]
    [SerializeField] private float oxygenIncrease = 0.2f;

    [Tooltip("WaterPump: perubahan salinitas per interaksi (negatif = berkurang)")]
    [SerializeField] private float salinityChange = -2f;

    [Tooltip("Heater/Chiller: suhu target (Heater lebih tinggi, Chiller lebih rendah)")]
    [SerializeField] private float targetTemperature = 26f;

    [Tooltip("Heater/Chiller: langkah perubahan suhu per interaksi")]
    [SerializeField] private float temperatureChangePerUse = 0.5f;

    [Header("Search Fallback")]
    [Tooltip("Radius pencarian AquariumSystem terdekat jika field tidak di-assign")]
    [SerializeField] private float searchRadius = 15f;

    private AquariumActionCooldowns cooldowns;

    // ─── Unity lifecycle ────────────────────────────────────────────────────────

    private void Awake()
    {
        // Inisialisasi highlight/outline via InitializeBase() dari base class agar
        // Outline.enabled = false pada Awake, sehingga tidak aktif sebelum interaksi.
        InitializeBase();

        // Coba resolusi awal — jangan simpan hasil CurrentOpen karena bisa berubah
        TryResolveFromHierarchy();

        cooldowns = GetComponent<AquariumActionCooldowns>();
        if (cooldowns == null)
            cooldowns = gameObject.AddComponent<AquariumActionCooldowns>();

        itemName = machineRole.ToString();
    }

    // ─── Interact ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Override HandleInteract: terapkan efek RAS dan trigger animasi PickUp player.
    /// </summary>
    protected override void HandleInteract()
    {
        PlayerInputManager.Instance.ResetInteractInput();

        AquariumSystem resolved = ResolveAquariumSystem();
        if (resolved == null)
        {
            Debug.LogError($"[AquariumMachine:{machineRole}] AquariumSystem tidak ditemukan! " +
                           "Assign field 'Aquarium System' di Inspector atau pastikan mesin dekat aquarium.");
            return;
        }

        string cooldownKey = $"{machineRole}_{GetInstanceID()}";
        if (!cooldowns.IsReady(cooldownKey))
        {
            float remaining = cooldowns.GetRemaining(cooldownKey);
            Debug.Log($"[AquariumMachine:{machineRole}] Masih cooldown {remaining:0.0}s.");
            return;
        }

        bool success = ApplyEffect(resolved);

        if (success)
        {
            cooldowns.StartCooldown(cooldownKey, cooldownSeconds);
            TriggerPickUpAnimation();
            Debug.Log($"[AquariumMachine:{machineRole}] Cooldown dimulai {cooldownSeconds}s.");
        }
    }

    public override string GetItemName() => machineRole.ToString();

    // ─── Private ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Memicu animasi PickUp pada ThirdPersonController player.
    /// </summary>
    private void TriggerPickUpAnimation()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        StarterAssets.ThirdPersonController controller = player.GetComponent<StarterAssets.ThirdPersonController>();
        controller?.TriggerPickUpAnimation();
    }

    /// <summary>
    /// Terapkan efek RAS sesuai machineRole, kembalikan true jika berhasil.
    /// </summary>
    private bool ApplyEffect(AquariumSystem target)
    {
        WaterQualityState wq = target.WaterQuality;

        switch (machineRole)
        {
            case AquariumEquipmentRole.Aerator:
            {
                float before = wq.oxygen;
                bool ok = target.IncreaseOxygen(oxygenIncrease);
                Debug.Log($"[RAS][Aerator] O2: {before:0.00} → {wq.oxygen:0.00} (+" + oxygenIncrease + $") | Aquarium: {target.name}");
                return ok;
            }

            case AquariumEquipmentRole.WaterPump:
            {
                float before = wq.salinity;
                bool ok = target.ChangeSalinity(salinityChange);
                Debug.Log($"[RAS][WaterPump] Salinitas: {before:0.00} → {wq.salinity:0.00} ({salinityChange:+0.00;-0.00}) | Aquarium: {target.name}");
                return ok;
            }

            case AquariumEquipmentRole.Heater:
            {
                float before = wq.temperature;
                bool ok = target.ChangeTemperature(targetTemperature, temperatureChangePerUse);
                Debug.Log($"[RAS][Heater] Suhu: {before:0.0} → {wq.temperature:0.0} (target={targetTemperature}, step={temperatureChangePerUse}) | Aquarium: {target.name}");
                return ok;
            }

            case AquariumEquipmentRole.Chiller:
            {
                float before = wq.temperature;
                bool ok = target.ChangeTemperature(targetTemperature, temperatureChangePerUse);
                Debug.Log($"[RAS][Chiller] Suhu: {before:0.0} → {wq.temperature:0.0} (target={targetTemperature}, step={temperatureChangePerUse}) | Aquarium: {target.name}");
                return ok;
            }

            default:
                Debug.LogWarning($"[AquariumMachine] machineRole '{machineRole}' tidak dikenali.");
                return false;
        }
    }

    /// <summary>
    /// Resolusi final AquariumSystem setiap kali HandleInteract dipanggil.
    /// Urutan prioritas: Inspector field → parent hierarchy → CurrentOpen → proximity → FindFirst.
    /// </summary>
    private AquariumSystem ResolveAquariumSystem()
    {
        // 1. Sudah di-assign di Inspector
        if (aquariumSystem != null)
            return aquariumSystem;

        // 2. Parent hierarchy
        aquariumSystem = GetComponentInParent<AquariumSystem>();
        if (aquariumSystem != null)
        {
            Debug.Log($"[AquariumMachine:{machineRole}] AquariumSystem ditemukan via parent: {aquariumSystem.name}");
            return aquariumSystem;
        }

        // 3. Aquarium yang sedang dibuka player
        if (AquariumSystem.CurrentOpen != null)
        {
            Debug.Log($"[AquariumMachine:{machineRole}] Menggunakan CurrentOpen: {AquariumSystem.CurrentOpen.name}");
            return AquariumSystem.CurrentOpen;
        }

        // 4. AquariumSystem terdekat dalam radius
        AquariumSystem nearest = FindNearestAquariumSystem();
        if (nearest != null)
        {
            aquariumSystem = nearest;
            Debug.Log($"[AquariumMachine:{machineRole}] AquariumSystem terdekat ditemukan: {aquariumSystem.name}");
            return aquariumSystem;
        }

        // 5. Fallback — ambil yang pertama di scene
        aquariumSystem = FindFirstObjectByType<AquariumSystem>();
        if (aquariumSystem != null)
            Debug.LogWarning($"[AquariumMachine:{machineRole}] Fallback FindFirstObjectByType: {aquariumSystem.name}");

        return aquariumSystem;
    }

    /// <summary>
    /// Resolusi dari hierarchy saja — dipanggil di Awake untuk pre-warm referensi.
    /// </summary>
    private void TryResolveFromHierarchy()
    {
        if (aquariumSystem != null) return;
        aquariumSystem = GetComponentInParent<AquariumSystem>();
    }

    /// <summary>
    /// Cari AquariumSystem dalam radius <see cref="searchRadius"/> dari posisi mesin.
    /// </summary>
    private AquariumSystem FindNearestAquariumSystem()
    {
        AquariumSystem[] all = FindObjectsByType<AquariumSystem>(FindObjectsSortMode.None);
        AquariumSystem nearest = null;
        float nearestSqr = searchRadius * searchRadius;

        foreach (AquariumSystem sys in all)
        {
            if (sys == null) continue;
            float sqrDist = (sys.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < nearestSqr)
            {
                nearestSqr = sqrDist;
                nearest = sys;
            }
        }

        return nearest;
    }
}
