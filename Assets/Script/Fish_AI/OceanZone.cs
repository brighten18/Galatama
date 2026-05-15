using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OceanZone : FishZone
{
    [Header("Ocean Configuration")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private int maxFishCount = 50;
    [SerializeField] private List<GameObject> fishPrefabs = new List<GameObject>();

    [Header("Spawn Settings")]
    [SerializeField] private int initialSpawnCount = 50;
    [SerializeField] private float spawnDelay = 1f;
    
    protected override void Awake()
    {
        base.Awake();
        zoneType = FishEnvironment.Ocean;
    }

    private void Start()
    {
        StartCoroutine(SpawnFishWithDelay());
    }

    private IEnumerator SpawnFishWithDelay()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnRandomFish();
            yield return new WaitForSeconds(spawnDelay);
        }
    }
    
    protected override void OnFishEnterZone(FishBrain fish)
    {
        fishInZone.Add(fish);
        fish.OnEnvironmentChanged(FishEnvironment.Ocean, zoneBounds);
        Debug.Log($"[OceanZone] Fish entered: {fish.FishData.ItemName}. Total: {fishInZone.Count}");
    }
    
    protected override void OnFishExitZone(FishBrain fish)
    {
        fishInZone.Remove(fish);
        Debug.Log($"[OceanZone] Fish exited: {fish.FishData.ItemName}. Total: {fishInZone.Count}");
    }
    
    public GameObject SpawnFish(GameObject fishPrefab)
    {
        if (fishInZone.Count >= maxFishCount)
        {
            Debug.LogWarning("[OceanZone] Max fish count reached");
            return null;
        }
        
        Vector3 spawnPos = GetSpawnPosition();
        GameObject fishObj = Instantiate(fishPrefab, spawnPos, Quaternion.identity);
        
        FishBrain fish = fishObj.GetComponent<FishBrain>();
        if (fish != null)
        {
            Debug.Log($"[OceanZone] Spawned fish at {spawnPos}");
        }
        
        return fishObj;
    }
    
    public void SpawnRandomFish()
    {
        if (fishPrefabs.Count == 0)
        {
            Debug.LogWarning("[OceanZone] No fish prefabs assigned");
            return;
        }
        
        GameObject randomPrefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        SpawnFish(randomPrefab);
    }
    
    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Count > 0)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            return randomPoint.position;
        }
        
        return GetRandomPointInZone();
    }
    
    public List<FishBrain> GetActiveOceanFish()
    {
        return GetFishInZone();
    }
}