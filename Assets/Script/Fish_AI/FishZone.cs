// Scripts/Fish/Zones/FishZone.cs

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public abstract class FishZone : MonoBehaviour
{
    [Header("Zone Configuration")]
    [SerializeField] protected FishEnvironment zoneType;
    
    protected Bounds zoneBounds;
    protected List<FishBrain> fishInZone = new List<FishBrain>();
    protected Collider zoneCollider;
    
    protected virtual void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        
        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning($"[FishZone] Collider pada {gameObject.name} harus IsTrigger = true");
            zoneCollider.isTrigger = true;
        }
        
        zoneBounds = zoneCollider.bounds;
    }
    
    protected virtual void OnTriggerEnter(Collider other)
    {
        FishBrain fish = other.GetComponent<FishBrain>();
        if (fish != null && !fishInZone.Contains(fish))
        {
            OnFishEnterZone(fish);
        }
    }
    
    protected virtual void OnTriggerExit(Collider other)
    {
        FishBrain fish = other.GetComponent<FishBrain>();
        if (fish != null && fishInZone.Contains(fish))
        {
            OnFishExitZone(fish);
        }
    }
    
    protected abstract void OnFishEnterZone(FishBrain fish);
    protected abstract void OnFishExitZone(FishBrain fish);
    
    public Bounds GetBounds()
    {
        return zoneBounds;
    }
    
    public bool IsPointInZone(Vector3 point)
    {
        return zoneBounds.Contains(point);
    }
    
    public Vector3 GetRandomPointInZone()
    {
        return new Vector3(
            Random.Range(zoneBounds.min.x, zoneBounds.max.x),
            Random.Range(zoneBounds.min.y, zoneBounds.max.y),
            Random.Range(zoneBounds.min.z, zoneBounds.max.z)
        );
    }
    
    public int GetFishCount()
    {
        return fishInZone.Count;
    }
    
    public List<FishBrain> GetFishInZone()
    {
        return new List<FishBrain>(fishInZone);
    }
}