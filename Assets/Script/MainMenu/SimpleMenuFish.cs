using UnityEngine;

public enum ForwardAxis
{
    PositiveZ,   // Z+ (biru, default Unity forward)
    NegativeZ,   // Z-
    PositiveX,   // X+ (merah)
    NegativeX,   // X-
    PositiveY,   // Y+ (hijau)
    NegativeY    // Y-
}

public class SimpleMenuFish : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 2.0f;
    [SerializeField] private float speedVariation = 1.0f;
    [SerializeField] private float rotationSpeed = 180f;
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 5.0f;
    [SerializeField] private float waypointReachThreshold = 1.5f;
    [SerializeField] private float changeWaypointInterval = 5.0f;
    
    [Header("Orientation")]
    [Tooltip("Sumbu lokal yang menjadi arah depan ikan.")]
    [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.NegativeX; // default -X
    
    [Header("Bounds")]
    [SerializeField] private Vector3 swimAreaCenter = Vector3.zero;
    [SerializeField] private Vector3 swimAreaSize = new Vector3(20f, 10f, 20f);
    
    private Vector3 currentWaypoint;
    private float speedOffset;
    private float waypointTimer;
    private Transform fishTransform;
    private float speedMultiplier = 1.0f;
    private Quaternion forwardCorrection = Quaternion.identity;
    
    void Start()
    {
        fishTransform = transform;
        UpdateForwardCorrection(); // inisialisasi koreksi rotasi
        GenerateNewWaypoint();
        waypointTimer = Random.Range(0f, changeWaypointInterval);
    }
    
    // Panggil saat nilai enum diubah di Inspector (hanya editor)
    void OnValidate()
    {
        if (Application.isPlaying)
            UpdateForwardCorrection();
    }
    
    private void UpdateForwardCorrection()
    {
        // Tentukan vektor arah depan lokal berdasarkan enum
        Vector3 localForward = GetLocalForwardVector();
        // Rotasi yang memetakan localForward menjadi world forward (Vector3.forward)
        // Saat kita melakukan LookRotation(direction), sumbu Z akan menuju direction.
        // Kita ingin localForward yang menunjuk ke direction, jadi kita perlu koreksi:
        // targetRotation = LookRotation(direction) * Quaternion.FromToRotation(localForward, Vector3.forward)
        // Karena FromToRotation(localForward, Vector3.forward) memutar localForward ke Z+,
        // maka setelah rotasi oleh LookRotation, localForward akan sejajar dengan direction.
        forwardCorrection = Quaternion.FromToRotation(localForward, Vector3.forward);
    }
    
    private Vector3 GetLocalForwardVector()
    {
        switch (forwardAxis)
        {
            case ForwardAxis.PositiveZ:  return Vector3.forward;
            case ForwardAxis.NegativeZ:  return Vector3.back;
            case ForwardAxis.PositiveX:  return Vector3.right;
            case ForwardAxis.NegativeX:  return Vector3.left;
            case ForwardAxis.PositiveY:  return Vector3.up;
            case ForwardAxis.NegativeY:  return Vector3.down;
            default:                     return Vector3.forward;
        }
    }
    
    void Update()
    {
        MoveToWaypoint();
        CheckWaypointReached();
        
        // Debug: tunjukkan arah depan ikan yang sebenarnya
        Debug.DrawRay(transform.position, transform.TransformDirection(GetLocalForwardVector()), Color.red);
        // Untuk referensi, sumbu Z+ Unity (biru)
        Debug.DrawRay(transform.position, transform.forward, Color.blue);
    }
    
    private void MoveToWaypoint()
    {
        Vector3 direction = (currentWaypoint - fishTransform.position).normalized;
        if (direction == Vector3.zero) return;
        
        // Target rotasi dengan koreksi sumbu depan
        Quaternion targetRotation = Quaternion.LookRotation(direction) * forwardCorrection;
        
        fishTransform.rotation = Quaternion.RotateTowards(
            fishTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
        
        float effectiveSpeed = (baseSpeed + speedOffset) * speedMultiplier;
        fishTransform.position += direction * effectiveSpeed * Time.deltaTime;
    }
    
    private void CheckWaypointReached()
    {
        float distance = Vector3.Distance(fishTransform.position, currentWaypoint);
        waypointTimer -= Time.deltaTime;
        
        if (distance < waypointReachThreshold || waypointTimer <= 0f)
        {
            GenerateNewWaypoint();
            waypointTimer = changeWaypointInterval;
        }
    }
    
    private void GenerateNewWaypoint()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-swimAreaSize.x * 0.5f, swimAreaSize.x * 0.5f),
            Random.Range(-swimAreaSize.y * 0.5f, swimAreaSize.y * 0.5f),
            Random.Range(-swimAreaSize.z * 0.5f, swimAreaSize.z * 0.5f)
        );
        currentWaypoint = swimAreaCenter + randomOffset;
        speedOffset = Random.Range(-speedVariation, speedVariation);
    }
    
    public void SetSwimArea(Vector3 center, Vector3 size)
    {
        swimAreaCenter = center;
        swimAreaSize = size;
    }
    
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(swimAreaCenter, swimAreaSize);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentWaypoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
    }
}