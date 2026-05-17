// Scripts/Fish/FishZone.cs - DEBUG VERSION

using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FishZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private ZoneType zoneType = ZoneType.Ocean;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true; // ✏️ DITAMBAH
    
    private BoxCollider zoneCollider;
    private Bounds zoneBounds;
    
    void Awake()
    {
        zoneCollider = GetComponent<BoxCollider>();
        
        if (!zoneCollider.isTrigger)
        {
            Debug.LogError($"[FishZone] BoxCollider pada {gameObject.name} HARUS IsTrigger = true!");
            zoneCollider.isTrigger = true;
        }
        
        // ✏️ DITAMBAH: Update bounds setiap kali (bukan hanya Awake)
        UpdateBounds();
        
        if (showDebugLog)
        {
            Debug.Log($"[FishZone] {zoneType} zone initialized. Bounds: {zoneBounds}");
        }
    }
    
    void Start()
    {
        // ✏️ DITAMBAH: Update bounds lagi di Start (setelah transform final)
        UpdateBounds();
    }
    
    // ✏️ DITAMBAH: Method untuk update bounds
    private void UpdateBounds()
    {
        zoneBounds = new Bounds(
            transform.position + zoneCollider.center,
            Vector3.Scale(zoneCollider.size, transform.lossyScale)
        );
    }
    
    void OnTriggerEnter(Collider other)
    {
        // ✏️ DITAMBAH: Debug log untuk semua trigger enter
        if (showDebugLog)
        {
            Debug.Log($"[FishZone] OnTriggerEnter detected: {other.gameObject.name}");
        }
        
        FishBrain fish = other.GetComponent<FishBrain>();
        if (fish != null)
        {
            fish.SetBoundary(zoneBounds);
            
            if (showDebugLog)
            {
                Debug.Log($"[FishZone] Fish '{other.gameObject.name}' entered {zoneType} zone. Boundary set: {zoneBounds.center}, size: {zoneBounds.size}");
            }
        }
        else
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"[FishZone] Object '{other.gameObject.name}' entered zone but has no FishBrain component");
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (showDebugLog)
        {
            Debug.LogWarning($"[FishZone] Fish '{other.gameObject.name}' EXITED zone! (This should not happen)");
        }
    }
    
    public Bounds GetBounds() => zoneBounds;
    
    public Vector3 GetRandomPointInZone()
    {
        return new Vector3(
            Random.Range(zoneBounds.min.x + 2f, zoneBounds.max.x - 2f),
            Random.Range(zoneBounds.min.y + 1f, zoneBounds.max.y - 1f),
            Random.Range(zoneBounds.min.z + 2f, zoneBounds.max.z - 2f)
        );
    }
    
    void OnDrawGizmos()
    {
        // ✏️ DIPERBAIKI: Update bounds di OnDrawGizmos juga
        if (Application.isPlaying)
        {
            UpdateBounds();
        }
        else
        {
            if (zoneCollider == null)
                zoneCollider = GetComponent<BoxCollider>();
            
            zoneBounds = new Bounds(
                transform.position + zoneCollider.center,
                Vector3.Scale(zoneCollider.size, transform.lossyScale)
            );
        }
        
        Gizmos.color = zoneType == ZoneType.Ocean ? 
            new Color(0f, 0.5f, 1f, 0.3f) : 
            new Color(0f, 1f, 0.5f, 0.3f);
        
        Gizmos.DrawCube(zoneBounds.center, zoneBounds.size);
        
        // ✏️ DITAMBAH: Draw wireframe untuk lebih jelas
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(zoneBounds.center, zoneBounds.size);
    }
}