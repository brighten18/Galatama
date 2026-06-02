using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FishZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private ZoneType zoneType = ZoneType.Ocean;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private Collider zoneCollider;
    private Bounds zoneBounds;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning($"[FishZone] Collider pada {gameObject.name} diubah menjadi trigger.");
            zoneCollider.isTrigger = true;
        }

        UpdateBounds();
    }

    void Start()
    {
        UpdateBounds();
    }

    public void UpdateBounds()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider>();
        }

        if (zoneCollider != null)
            zoneBounds = zoneCollider.bounds;
    }

    void OnTriggerEnter(Collider other)
    {
        ApplyBoundary(other);
    }

    void OnTriggerStay(Collider other)
    {
        ApplyBoundary(other);
    }

    private void ApplyBoundary(Collider other)
    {
        FishBrain fish = other.GetComponentInParent<FishBrain>();
        if (fish == null) return;

        UpdateBounds();
        fish.SetZoneType(zoneType);
        fish.SetBoundary(zoneCollider);

        if (showDebugLog)
        {
            Debug.Log($"[FishZone] Boundary {zoneType} diterapkan ke {fish.name}. Center: {zoneBounds.center}, Size: {zoneBounds.size}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        FishBrain fish = other.GetComponentInParent<FishBrain>();
        if (fish != null && showDebugLog)
        {
            Debug.LogWarning($"[FishZone] {fish.name} keluar dari {zoneType}. Movement akan clamp balik saat boundary masih tersimpan.");
        }
    }

    public Bounds GetBounds()
    {
        UpdateBounds();
        return zoneBounds;
    }

    public Collider GetCollider()
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<Collider>();

        return zoneCollider;
    }

    public ZoneType ZoneType => zoneType;

    public bool ContainsPoint(Vector3 point)
    {
        UpdateBounds();
        if (zoneCollider == null || !zoneCollider.enabled) return false;

        Vector3 closest = zoneCollider.ClosestPoint(point);
        return (closest - point).sqrMagnitude <= 0.0001f;
    }

    public Vector3 GetRandomPointInZone(float padding = 2f)
    {
        UpdateBounds();
        if (zoneCollider == null || !zoneCollider.enabled)
            return transform.position;

        Vector3 ext = zoneBounds.extents;
        Vector3 safePadding = new Vector3(
            Mathf.Min(padding, Mathf.Max(0f, ext.x - 0.01f)),
            Mathf.Min(padding, Mathf.Max(0f, ext.y - 0.01f)),
            Mathf.Min(padding, Mathf.Max(0f, ext.z - 0.01f))
        );
        Vector3 min = zoneBounds.min + safePadding;
        Vector3 max = zoneBounds.max - safePadding;

        const int maxAttempts = 32;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z)
            );

            if (ContainsPoint(candidate))
                return candidate;
        }

        return zoneCollider.ClosestPoint(zoneBounds.center);
    }

    void OnDrawGizmos()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider>();
        }

        if (zoneCollider == null) return;

        zoneBounds = zoneCollider.bounds;

        Gizmos.color = zoneType == ZoneType.Ocean
            ? new Color(0f, 0.5f, 1f, 0.25f)
            : new Color(0f, 1f, 0.5f, 0.25f);

        Gizmos.DrawCube(zoneBounds.center, zoneBounds.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(zoneBounds.center, zoneBounds.size);
    }
}
