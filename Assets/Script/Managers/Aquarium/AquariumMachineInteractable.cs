using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Komponen mesin aquarium dunia (Aerator, Heater, Chiller, WaterPump).
/// Saat player berinteraksi, nilai RAS pada AquariumSystem target langsung diubah sesuai
/// parameter yang dikonfigurasi di Inspector.
///
/// RESOLUSI AQUARIUM:
///   1. Field aquariumSystem di Inspector (paling prioritas)
///   2. GetComponentInParent â€” jika mesin adalah child dari AquariumSystem
///   3. AquariumSystem.CurrentOpen â€” aquarium yang sedang dibuka player
///   4. AquariumSystem terdekat dalam radius searchRadius
///   5. FindFirstObjectByType â€” fallback terakhir
/// </summary>
public class AquariumMachineInteractable : InteractableObject, IInteractCooldownProvider
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
    private string cooldownKey;
    private WorldSpaceCooldownUI machineWorldCooldownUI;

    // â”€â”€â”€ Unity lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â...

    private void Awake()
    {
        // Inisialisasi highlight/outline via InitializeBase() dari base class agar
        // Outline.enabled = false pada Awake, sehingga tidak aktif sebelum interaksi.
        InitializeBase();

        // Coba resolusi awal â€” jangan simpan hasil CurrentOpen karena bisa berubah
        TryResolveFromHierarchy();

        cooldowns = GetComponent<AquariumActionCooldowns>();
        if (cooldowns == null)
            cooldowns = gameObject.AddComponent<AquariumActionCooldowns>();

        machineWorldCooldownUI = GetComponent<WorldSpaceCooldownUI>();
        if (showWorldCooldownUI && machineWorldCooldownUI == null)
            machineWorldCooldownUI = gameObject.AddComponent<WorldSpaceCooldownUI>();

        if (machineWorldCooldownUI != null)
            ApplyWorldCooldownUISettings();

        cooldownKey = $"{machineRole}_{BuildPersistentObjectKey(transform)}";
        itemName = machineRole.ToString();
    }

    private void OnValidate()
    {
        if (machineWorldCooldownUI == null)
            machineWorldCooldownUI = GetComponent<WorldSpaceCooldownUI>();

        if (machineWorldCooldownUI != null)
            ApplyWorldCooldownUISettings();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (!showWorldCooldownUI || machineWorldCooldownUI == null)
            return;

        float remaining = GetCooldownRemainingSeconds();
        bool shouldShow = isBeingLookedAt && remaining > 0f;
        machineWorldCooldownUI.SetCooldown(remaining, GetCooldownDurationSeconds(), shouldShow);
    }

    // â”€â”€â”€ Interact â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”...

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

        if (resolved.IsRewardLocked)
        {
            Debug.Log($"[AquariumMachine:{machineRole}] Aquarium masih terkunci oleh reward wave quiz.");
            return;
        }

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

    public bool ShouldShowCooldownUI()
    {
        return false;
    }

    public float GetCooldownRemainingSeconds()
    {
        if (cooldowns == null || string.IsNullOrEmpty(cooldownKey))
            return 0f;

        return cooldowns.GetRemaining(cooldownKey);
    }

    public float GetCooldownDurationSeconds()
    {
        return Mathf.Max(0f, cooldownSeconds);
    }

    private void ApplyWorldCooldownUISettings()
    {
        machineWorldCooldownUI.Configure(
            cooldownUIOffset,
            cooldownUICanvasSize,
            cooldownUIWorldScale,
            cooldownUIInnerPadding,
            cooldownUIFillColor,
            cooldownUIBackgroundColor,
            cooldownUIFontSize);
    }

    /// <summary>
    /// Menyembunyikan prompt dan highlight saat aquarium reward masih terkunci.
    /// </summary>
    public override void SetLookingAt(bool value)
    {
        if (value && IsRewardLocked()) return;
        base.SetLookingAt(value);
    }

    /// <summary>
    /// Mengembalikan nama kosong saat reward terkunci sehingga InteractUIManager
    /// tidak menampilkan panel interaksi sama sekali.
    /// </summary>
    public override string GetItemName()
    {
        return IsRewardLocked() ? string.Empty : machineRole.ToString();
    }

    // â”€â”€â”€ Private â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€...

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
                Debug.Log($"[RAS][Aerator] O2: {before:0.00} â†’ {wq.oxygen:0.00} (+" + oxygenIncrease + $") | Aquarium: {target.name}");
                return ok;
            }

            case AquariumEquipmentRole.WaterPump:
            {
                float before = wq.salinity;
                bool ok = target.ChangeSalinity(salinityChange);
                Debug.Log($"[RAS][WaterPump] Salinitas: {before:0.00} â†’ {wq.salinity:0.00} ({salinityChange:+0.00;-0.00}) | Aquarium: {target.name}");
                return ok;
            }

            case AquariumEquipmentRole.Heater:
            {
                float before = wq.temperature;
                bool ok = target.ChangeTemperature(targetTemperature, temperatureChangePerUse);
                Debug.Log($"[RAS][Heater] Suhu: {before:0.0} â†’ {wq.temperature:0.0} (target={targetTemperature}, step={temperatureChangePerUse}) | Aquarium: {target.name}");
                return ok;
            }

            case AquariumEquipmentRole.Chiller:
            {
                float before = wq.temperature;
                bool ok = target.ChangeTemperature(targetTemperature, temperatureChangePerUse);
                Debug.Log($"[RAS][Chiller] Suhu: {before:0.0} â†’ {wq.temperature:0.0} (target={targetTemperature}, step={temperatureChangePerUse}) | Aquarium: {target.name}");
                return ok;
            }

            default:
                Debug.LogWarning($"[AquariumMachine] machineRole '{machineRole}' tidak dikenali.");
                return false;
        }
    }

    /// <summary>
    /// Resolusi final AquariumSystem setiap kali HandleInteract dipanggil.
    /// Urutan prioritas: Inspector field â†’ parent hierarchy â†’ CurrentOpen â†’ proximity â†’ F...
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

        // 5. Fallback â€” ambil yang pertama di scene
        aquariumSystem = FindFirstObjectByType<AquariumSystem>();
        if (aquariumSystem != null)
            Debug.LogWarning($"[AquariumMachine:{machineRole}] Fallback FindFirstObjectByType: {aquariumSystem.name}");

        return aquariumSystem;
    }

    /// <summary>
    /// Resolusi dari hierarchy saja â€” dipanggil di Awake untuk pre-warm referensi.
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

    /// <summary>
    /// Mengecek apakah AquariumSystem yang terkait masih terkunci oleh reward wave quiz.
    /// Menggunakan referensi <see cref="aquariumSystem"/> yang sudah di-resolve di Awake.
    /// </summary>
    private bool IsRewardLocked()
    {
        return aquariumSystem != null && aquariumSystem.IsRewardLocked;
    }

    private static string BuildPersistentObjectKey(Transform target)
    {
        if (target == null)
            return "missing";

        System.Text.StringBuilder builder = new System.Text.StringBuilder(target.name);
        Transform current = target.parent;

        while (current != null)
        {
            builder.Insert(0, current.name + "/");
            current = current.parent;
        }

        return builder.ToString();
    }
}

public class WorldSpaceCooldownUI : MonoBehaviour
{
    [Header("World Space UI")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private Vector2 canvasSize = new Vector2(60f, 60f);
    [SerializeField] private float worldScale = 0.006f;
    [SerializeField, Range(0f, 0.45f)] private float innerPadding = 0.08f;
    [SerializeField] private Color fillColor = new Color(0.2f, 0.82f, 1f, 0.95f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private int fontSize = 18;

    [Header("Optional References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text timerText;

    private Camera targetCamera;
    private static Sprite cachedCircleSprite;

    private void Awake()
    {
        EnsureUI();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (worldCanvas != null)
            Destroy(worldCanvas.gameObject);
    }

    private void OnDisable()
    {
        if (worldCanvas != null)
            worldCanvas.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (worldCanvas == null || !worldCanvas.gameObject.activeSelf)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Transform canvasTransform = worldCanvas.transform;
        canvasTransform.position = transform.position + worldOffset;

        // Uniform world scale applied directly — no parent scale inheritance
        canvasTransform.localScale = Vector3.one * worldScale;

        Vector3 direction = canvasTransform.position - targetCamera.transform.position;
        if (direction.sqrMagnitude > 0.001f)
            canvasTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    public void SetWorldOffset(Vector3 offset)
    {
        worldOffset = offset;
    }

    public void Configure(
        Vector3 offset,
        Vector2 size,
        float scale,
        float padding,
        Color newFillColor,
        Color newBackgroundColor,
        int newFontSize)
    {
        worldOffset = offset;
        canvasSize = size;
        worldScale = Mathf.Max(0.0001f, scale);
        innerPadding = Mathf.Clamp(padding, 0f, 0.45f);
        fillColor = newFillColor;
        backgroundColor = newBackgroundColor;
        fontSize = Mathf.Max(1, newFontSize);
        EnsureUI();
        ApplyVisualSettings();
    }

    public void SetCooldown(float remainingSeconds, float totalSeconds, bool forceVisible)
    {
        EnsureUI();

        float safeTotal = Mathf.Max(0.01f, totalSeconds);
        float clampedRemaining = Mathf.Clamp(remainingSeconds, 0f, safeTotal);
        bool visible = forceVisible && clampedRemaining > 0f;

        if (fillImage != null)
            fillImage.fillAmount = clampedRemaining / safeTotal;

        if (timerText != null)
            timerText.text = visible ? Mathf.CeilToInt(clampedRemaining).ToString() : string.Empty;

        SetVisible(visible);
    }

    private void SetVisible(bool visible)
    {
        if (worldCanvas != null)
            worldCanvas.gameObject.SetActive(visible);
    }

    private void EnsureUI()
    {
        if (worldCanvas == null)
        {
            Transform existing = transform.Find("CooldownWorldCanvas");
            if (existing != null)
                worldCanvas = existing.GetComponent<Canvas>();
        }

        if (worldCanvas == null)
            CreateCanvasHierarchy();

        if (worldCanvas == null)
            return;

        // Detach from parent so inherited non-uniform scale cannot distort the UI
        if (worldCanvas.transform.parent != null)
            worldCanvas.transform.SetParent(null, false);

        worldCanvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize;
        canvasRect.localScale = Vector3.one * worldScale;

        if (fillImage == null)
        {
            Transform fill = worldCanvas.transform.Find("CooldownFill");
            if (fill != null)
                fillImage = fill.GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            Transform bg = worldCanvas.transform.Find("CooldownBackground");
            if (bg != null)
                backgroundImage = bg.GetComponent<Image>();
        }

        if (timerText == null)
        {
            Transform label = worldCanvas.transform.Find("CooldownTimerText");
            if (label != null)
                timerText = label.GetComponent<Text>();
        }

        ApplyVisualSettings();
    }

    private void CreateCanvasHierarchy()
    {
        GameObject canvasObject = new GameObject("CooldownWorldCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        // No parent — canvas lives at scene root to avoid scale inheritance
        canvasObject.transform.SetParent(null);

        worldCanvas = canvasObject.GetComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize;
        canvasRect.localScale = Vector3.one * worldScale;

        GameObject backgroundObject = CreateImageObject("CooldownBackground", canvasObject.transform, false);
        backgroundImage = backgroundObject.GetComponent<Image>();

        GameObject fillObject = CreateImageObject("CooldownFill", canvasObject.transform, true);
        fillImage = fillObject.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = false;
        fillImage.fillAmount = 1f;

        GameObject textObject = new GameObject("CooldownTimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        timerText = textObject.GetComponent<Text>();
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = fontSize;
        timerText.color = Color.white;
        timerText.raycastTarget = false;
    }

    private void ApplyVisualSettings()
    {
        if (worldCanvas != null)
        {
            RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = canvasSize;
                canvasRect.localScale = Vector3.one * worldScale;
            }
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = GetCircleSprite();
            backgroundImage.color = backgroundColor;
            backgroundImage.type = Image.Type.Simple;
        }

        if (fillImage != null)
        {
            fillImage.sprite = GetCircleSprite();
            fillImage.color = fillColor;
        }

        if (timerText != null)
            timerText.fontSize = fontSize;

        ApplyPadding(backgroundImage != null ? backgroundImage.rectTransform : null, false);
        ApplyPadding(fillImage != null ? fillImage.rectTransform : null, true);
    }

    private void ApplyPadding(RectTransform rect, bool inset)
    {
        if (rect == null)
            return;

        if (inset)
        {
            rect.anchorMin = new Vector2(innerPadding, innerPadding);
            rect.anchorMax = new Vector2(1f - innerPadding, 1f - innerPadding);
        }
        else
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
        }

        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private GameObject CreateImageObject(string objectName, Transform parent, bool inset)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        ApplyPadding(rect, inset);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = GetCircleSprite();
        image.raycastTarget = false;
        return imageObject;
    }

    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null)
            return cachedCircleSprite;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "GeneratedCooldownCircle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 1) * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        cachedCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return cachedCircleSprite;
    }
}
