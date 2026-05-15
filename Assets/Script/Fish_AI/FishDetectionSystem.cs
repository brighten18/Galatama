// Scripts/Fish/Utility/FishDetectionSystem.cs

using UnityEngine;

public class FishDetectionSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 8.0f;
    [SerializeField] private LayerMask detectionLayerMask = -1;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private float detectionInterval = 0.2f;
    
    private float timeSinceLastDetection = 0f;
    private Transform lastDetectedPlayer;
    private Transform lastDetectedFood;
    
    void Update()
    {
        timeSinceLastDetection += Time.deltaTime;
    }
    
    public Transform DetectPlayer()
    {
        if (timeSinceLastDetection < detectionInterval)
        {
            return lastDetectedPlayer;
        }
        
        timeSinceLastDetection = 0f;
        
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            detectionLayerMask
        );
        
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                if (!requireLineOfSight || IsInLineOfSight(col.transform))
                {
                    lastDetectedPlayer = col.transform;
                    return col.transform;
                }
            }
        }
        
        lastDetectedPlayer = null;
        return null;
    }
    
    public Transform DetectFood()
    {
        if (timeSinceLastDetection < detectionInterval)
        {
            return lastDetectedFood;
        }
        
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            detectionLayerMask
        );
        
        Transform nearestFood = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Food"))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFood = col.transform;
                }
            }
        }
        
        lastDetectedFood = nearestFood;
        return nearestFood;
    }
    
    public bool IsInLineOfSight(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, direction, out hit, detectionRadius))
        {
            return hit.transform == target;
        }
        
        return false;
    }
    
    public void SetDetectionRadius(float radius)
    {
        detectionRadius = radius;
    }
    
    public float GetDetectionRadius()
    {
        return detectionRadius;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}