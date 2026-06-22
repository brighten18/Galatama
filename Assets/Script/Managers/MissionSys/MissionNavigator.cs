using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space navigation arrow pointing toward the current mission's waypoint.
/// Saat target TIDAK terlihat (terhalang tembok/collider atau di luar layar), arrow di-clamp ke
/// tepi layar dan dirotasi sebagai kompas penunjuk arah.
/// Saat target TERLIHAT langsung oleh kamera (Physics.Raycast tembus), arrow dipindahkan ke atas
/// objek target di world-space sehingga tidak mengikuti gerakan kamera.
/// Saat player dalam jarak nearDistance dan target terlihat, arrow tetap floating di atas objek.
/// </summary>
public class MissionNavigator : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Scene Transform references indexed by MissionData.WaypointIndex.")]
    [SerializeField] private Transform[] waypoints;

    [Header("UI References")]
    [Tooltip("Root Canvas yang memuat WaypointArrow.")]
    [SerializeField] private Canvas canvas;
    [Tooltip("RectTransform dari arrow image. Harus menjadi child langsung dari Canvas.")]
    [SerializeField] private RectTransform arrowContainer;
    [Tooltip("(Opsional) Text untuk menampilkan jarak ke tujuan.")]
    [SerializeField] private Text distanceText;

    [Header("Settings")]
    [Tooltip("Jarak (pixel) dari tepi layar tempat arrow di-clamp.")]
    [SerializeField] private float edgePadding = 70f;
    [Tooltip("Seberapa jauh teks digeser ke dalam layar saat arrow berada di tepi kiri/kanan (canvas-space pixel).")]
    [SerializeField] private float textInwardOffsetHorizontal = 50f;
    [Tooltip("Seberapa jauh teks digeser ke dalam layar saat arrow berada di tepi atas/bawah (canvas-space pixel).")]
    [SerializeField] private float textInwardOffsetVertical = 80f;
    [Tooltip("Jarak dunia (meter) dari waypoint saat arrow beralih ke mode menunjuk bawah.")]
    [SerializeField] private float nearDistance = 10f;
    [Tooltip("Kamera dunia yang digunakan untuk proyeksi. Otomatis menggunakan Camera.main jika kosong.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Offset ketinggian (meter) di atas waypoint saat target terlihat langsung oleh kamera.")]
    [SerializeField] private float visibleHeightOffset = 2f;
    [Tooltip("Layer mask untuk raycast line-of-sight. Centang semua layer fisik, kecuali Player, Fish, UI, dan FishBoundary.")]
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;

    private Transform _currentWaypoint;
    private bool _isActive;
    private RectTransform _canvasRect;
    private Vector2 _defaultTextLocalPos;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (canvas != null)
            _canvasRect = canvas.GetComponent<RectTransform>();

        if (distanceText != null)
            _defaultTextLocalPos = distanceText.rectTransform.localPosition;

        SetVisible(false);
    }

    private void OnEnable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStarted += OnMissionStarted;
            MissionManager.Instance.OnAllMissionsCompleted += OnAllMissionsCompleted;
        }
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStarted -= OnMissionStarted;
            MissionManager.Instance.OnAllMissionsCompleted -= OnAllMissionsCompleted;
        }
    }

    private void Start()
    {
        // Inisialisasi dari misi yang sedang aktif saat scene dimuat
        if (MissionManager.Instance?.CurrentMission != null)
            OnMissionStarted(MissionManager.Instance.CurrentMission);
    }

    /// <summary>Dipanggil saat misi baru dimulai. Mengatur waypoint target navigasi.</summary>
    private void OnMissionStarted(MissionData data)
    {
        if (data == null || !data.HasWaypoint || data.WaypointIndex >= waypoints.Length)
        {
            _currentWaypoint = null;
            SetVisible(false);
            return;
        }

        _currentWaypoint = waypoints[data.WaypointIndex];

        if (_currentWaypoint == null)
            Debug.LogWarning($"[MissionNavigator] Waypoint index {data.WaypointIndex} belum di-assign di Inspector.");

        SetVisible(_currentWaypoint != null);
    }

    private void OnAllMissionsCompleted()
    {
        _currentWaypoint = null;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        bool monologueActive = MonologueManager.Instance != null && MonologueManager.Instance.IsActiveOrPending;
        bool tutorialActive  = TutorialManager.Instance != null && TutorialManager.Instance.IsPlaying;
        if (arrowContainer != null)
            arrowContainer.gameObject.SetActive(_isActive && !monologueActive && !tutorialActive);

        if (!_isActive || monologueActive || tutorialActive || _currentWaypoint == null || targetCamera == null || _canvasRect == null)
            return;

        UpdateArrow();
    }

    private void UpdateArrow()
    {
        Vector3 screenPos = targetCamera.WorldToScreenPoint(_currentWaypoint.position);
        bool isBehind = screenPos.z < 0f;

        // Saat target di belakang kamera, balik posisi layar agar arrow tetap mengarah keluar
        if (isBehind)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        float halfW = Screen.width * 0.5f;
        float halfH = Screen.height * 0.5f;

        float dist = Vector3.Distance(targetCamera.transform.position, _currentWaypoint.position);
        bool isNear = dist <= nearDistance;

        bool isOnScreen = !isBehind
            && screenPos.x > edgePadding
            && screenPos.x < Screen.width - edgePadding
            && screenPos.y > edgePadding
            && screenPos.y < Screen.height - edgePadding;

        // Cek line-of-sight hanya saat target on-screen — tidak perlu raycast jika sudah pasti di belakang kamera
        bool isVisible = isOnScreen && IsTargetVisible();

        float arrowAngle;
        Vector2 direction = Vector2.zero;

        if (isNear && isVisible)
        {
            // Dekat DAN terlihat: arrow floating di atas objek, pointing down (↓)
            Vector3 aboveScreen = targetCamera.WorldToScreenPoint(
                _currentWaypoint.position + Vector3.up * visibleHeightOffset);
            Vector2 nearPos = new Vector2(
                Mathf.Clamp(aboveScreen.x, edgePadding, Screen.width - edgePadding),
                Mathf.Clamp(aboveScreen.y, edgePadding, Screen.height - edgePadding));

            SetArrowScreenPosition(nearPos);
            arrowAngle = 0f;
        }
        else if (isVisible)
        {
            // Target terlihat langsung: tempatkan arrow floating di atas objek di world-space
            Vector3 aboveScreen = targetCamera.WorldToScreenPoint(
                _currentWaypoint.position + Vector3.up * visibleHeightOffset);
            SetArrowScreenPosition(new Vector2(aboveScreen.x, aboveScreen.y));
            arrowAngle = 0f;
        }
        else
        {
            // Target tidak terlihat (terhalang atau di luar layar): arrow menempel ke tepi layar sebagai kompas
            direction = new Vector2(screenPos.x - halfW, screenPos.y - halfH);
            Vector2 clamped = ClampToScreenEdge(direction, halfW - edgePadding, halfH - edgePadding);
            SetArrowScreenPosition(clamped + new Vector2(halfW, halfH));
            arrowAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        }

        arrowContainer.localRotation = Quaternion.Euler(0f, 0f, arrowAngle);

        // Update label jarak
        if (distanceText != null)
        {
            distanceText.text = $"{Mathf.RoundToInt(dist)} m";

            // Counter-rotate text agar selalu terbaca upright, tidak ikut rotasi arrow
            distanceText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -arrowAngle);

            // Saat arrow di tepi layar, geser teks ke dalam agar tidak terpotong
            if (!isVisible && direction != Vector2.zero)
            {
                Vector2 normDir = direction.normalized;
                distanceText.rectTransform.localPosition = -new Vector2(
                    normDir.x * textInwardOffsetHorizontal,
                    normDir.y * textInwardOffsetVertical);
            }
            else
                distanceText.rectTransform.localPosition = _defaultTextLocalPos;
        }
    }

    /// <summary>
    /// Cek apakah target terlihat langsung dari kamera menggunakan Physics.Raycast.
    /// Mengembalikan true jika tidak ada collider yang menghalangi garis pandang.
    /// </summary>
    private bool IsTargetVisible()
    {
        Vector3 origin = targetCamera.transform.position;
        Vector3 toTarget = _currentWaypoint.position - origin;
        return !Physics.Raycast(origin, toTarget.normalized, toTarget.magnitude, occlusionMask);
    }

    /// <summary>
    /// Mengonversi titik screen-space ke canvas local space dan memindahkan arrowContainer ke sana.
    /// </summary>
    private void SetArrowScreenPosition(Vector2 screenPoint)
    {
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
        {
            arrowContainer.localPosition = localPoint;
        }
    }

    /// <summary>
    /// Memperkecil vektor direction agar tidak melampaui batas (halfW, halfH),
    /// mempertahankan arahnya.
    /// </summary>
    private static Vector2 ClampToScreenEdge(Vector2 direction, float halfW, float halfH)
    {
        if (direction == Vector2.zero)
            return Vector2.zero;

        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        float scaleX = absX > 0f ? halfW / absX : float.MaxValue;
        float scaleY = absY > 0f ? halfH / absY : float.MaxValue;

        return direction * Mathf.Min(scaleX, scaleY);
    }

    /// <summary>
    /// Clears the current waypoint and hides the navigation arrow immediately.
    /// Call this when the player arrives at the destination.
    /// </summary>
    public void ClearWaypoint()
    {
        _currentWaypoint = null;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        _isActive = visible;
        if (arrowContainer != null)
            arrowContainer.gameObject.SetActive(visible);
    }
}
