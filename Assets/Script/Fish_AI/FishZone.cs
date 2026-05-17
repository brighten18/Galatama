using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FishZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private ZoneType zoneType = ZoneType.Ocean;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private BoxCollider zoneCollider;
    private Bounds zoneBounds;

    void Awake()
    {
        zoneCollider = GetComponent<BoxCollider>();

        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning($"[FishZone] BoxCollider pada {gameObject.name} diubah menjadi trigger.");
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
            zoneCollider = GetComponent<BoxCollider>();
        }

        zoneBounds = new Bounds(
            transform.TransformPoint(zoneCollider.center),
            Vector3.Scale(zoneCollider.size, transform.lossyScale)
        );
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
        fish.SetBoundary(zoneBounds);

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

    public Vector3 GetRandomPointInZone(float padding = 2f)
    {
        UpdateBounds();

        Vector3 safePadding = new Vector3(
            Mathf.Min(padding, Mathf.Max(0f, zoneBounds.extents.x - 0.01f)),
            Mathf.Min(padding, Mathf.Max(0f, zoneBounds.extents.y - 0.01f)),
            Mathf.Min(padding, Mathf.Max(0f, zoneBounds.extents.z - 0.01f))
        );

        Vector3 min = zoneBounds.min + safePadding;
        Vector3 max = zoneBounds.max - safePadding;

        return new Vector3(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y),
            Random.Range(min.z, max.z)
        );
    }

    void OnDrawGizmos()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<BoxCollider>();
        }

        if (zoneCollider == null) return;

        zoneBounds = new Bounds(
            transform.TransformPoint(zoneCollider.center),
            Vector3.Scale(zoneCollider.size, transform.lossyScale)
        );

        Gizmos.color = zoneType == ZoneType.Ocean
            ? new Color(0f, 0.5f, 1f, 0.25f)
            : new Color(0f, 1f, 0.5f, 0.25f);

        Gizmos.DrawCube(zoneBounds.center, zoneBounds.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(zoneBounds.center, zoneBounds.size);
    }
}