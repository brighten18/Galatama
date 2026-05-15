// Scripts/Fish/Zones/AquariumZone.cs

using UnityEngine;

public class AquariumZone : FishZone
{
    [Header("Aquarium Configuration")]
    [SerializeField] private int maxCapacity = 10;
    [SerializeField] private float wallThickness = 0.5f;
    
    protected override void Awake()
    {
        base.Awake();
        zoneType = FishEnvironment.Aquarium;
    }
    
    // ✏️ DIPERBAIKI: Pass bounds ke fish
    protected override void OnFishEnterZone(FishBrain fish)
    {
        if (fishInZone.Count >= maxCapacity)
        {
            Debug.LogWarning("[AquariumZone] Max capacity reached");
            return;
        }
        
        fishInZone.Add(fish);
        fish.OnEnvironmentChanged(FishEnvironment.Aquarium, zoneBounds);
        
        Debug.Log($"[AquariumZone] Fish entered: {fish.FishData.ItemName}. Total: {fishInZone.Count}/{maxCapacity}");
    }
    
    protected override void OnFishExitZone(FishBrain fish)
    {
        fishInZone.Remove(fish);
        Debug.LogWarning($"[AquariumZone] Fish exited: {fish.FishData.ItemName}");
    }
    
    public bool AddFish(FishBrain fish)
    {
        if (fishInZone.Count >= maxCapacity)
        {
            Debug.LogWarning("[AquariumZone] Cannot add fish, max capacity reached");
            return false;
        }
        
        Vector3 placePos = GetRandomPointInZone();
        fish.transform.position = placePos;
        
        fish.OnPlacedInAquarium(this);
        
        return true;
    }
    
    public void RemoveFish(FishBrain fish)
    {
        if (fishInZone.Contains(fish))
        {
            fishInZone.Remove(fish);
            Debug.Log($"[AquariumZone] Fish removed: {fish.FishData.ItemName}");
        }
    }
    
    public bool IsFull()
    {
        return fishInZone.Count >= maxCapacity;
    }
}