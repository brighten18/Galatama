using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space navigation arrow pointing toward the current mission's waypoint.
/// When the target is on-screen the arrow is placed at the target position (pointing down).
/// When off-screen or behind the camera, the arrow is clamped to the screen edge and
/// rotated to face the target direction.
/// When the player is within nearDistance, the arrow always points downward at the target's
/// screen position to indicate the objective is nearby.
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
        if (arrowContainer != null)
            arrowContainer.gameObject.SetActive(_isActive && !monologueActive);

        if (!_isActive || monologueActive || _currentWaypoint == null || targetCamera == null || _canvasRect == null)
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

        float arrowAngle;
        Vector2 direction = Vector2.zero;

        if (isNear)
        {
            // Player dekat waypoint: tempatkan arrow di posisi target dan arahkan ke bawah (↓)
            // Clamp agar tetap dalam batas layar meski target sedikit di luar viewport
            Vector2 nearScreenPos = isBehind
                ? new Vector2(halfW, halfH)
                : new Vector2(
                    Mathf.Clamp(screenPos.x, edgePadding, Screen.width - edgePadding),
                    Mathf.Clamp(screenPos.y, edgePadding, Screen.height - edgePadding));

            SetArrowScreenPosition(nearScreenPos);
            arrowAngle = 0f; // Sprite default menunjuk ke bawah (↓)
        }
        else if (isOnScreen)
        {
            SetArrowScreenPosition(new Vector2(screenPos.x, screenPos.y));
            arrowAngle = 0f;
        }
        else
        {
            // Clamp ke tepi layar, rotasi panah ke arah target
            direction = new Vector2(screenPos.x - halfW, screenPos.y - halfH);
            Vector2 clamped = ClampToScreenEdge(direction, halfW - edgePadding, halfH - edgePadding);
            SetArrowScreenPosition(clamped + new Vector2(halfW, halfH));
            // Negasi direction.x untuk mengkompensasi perbedaan konvensi antara atan2 (CCW+)
            // dan Unity UI (CW+ dari perspektif viewer), sehingga arrow selalu mengarah keluar layar.
            arrowAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        }

        arrowContainer.localRotation = Quaternion.Euler(0f, 0f, arrowAngle);

        // Update label jarak (opsional)
        if (distanceText != null)
        {
            distanceText.text = $"{Mathf.RoundToInt(dist)} m";

            // Counter-rotate text agar selalu terbaca upright, tidak ikut rotasi arrow
            distanceText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -arrowAngle);

            // Saat arrow di tepi layar, geser teks ke arah dalam layar agar tidak terpotong/tertimpa.
            // Offset horizontal dan vertikal dipisah agar bisa di-tune secara independen.
            if (!isOnScreen && !isNear && direction != Vector2.zero)
            {
                Vector2 normDir = direction.normalized;
                float offsetX = normDir.x * textInwardOffsetHorizontal;
                float offsetY = normDir.y * textInwardOffsetVertical;
                distanceText.rectTransform.localPosition = -new Vector2(offsetX, offsetY);
            }
            else
                distanceText.rectTransform.localPosition = _defaultTextLocalPos;
        }
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

    private void SetVisible(bool visible)
    {
        _isActive = visible;
        if (arrowContainer != null)
            arrowContainer.gameObject.SetActive(visible);
    }
}
