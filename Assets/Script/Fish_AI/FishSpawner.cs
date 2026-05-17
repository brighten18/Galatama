// Scripts/Fish/FishSpawner.cs - MULTI-SPECIES VERSION

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    // ✏️ DIUBAH: Dari single prefab ke list
    [SerializeField] private List<GameObject> fishPrefabs = new List<GameObject>();
    [SerializeField] private FishZone spawnZone;
    
    [Header("Population")]
    [SerializeField] private int maxPopulation = 200;
    [SerializeField] private int initialSpawnCount = 40;
    
    [Header("Respawn")]
    [SerializeField] private bool autoRespawn = true;
    [SerializeField] private float respawnDelay = 3f;
    
    [Header("Wave Spawn (Optional)")]
    [SerializeField] private bool spawnInWaves = false;
    [SerializeField] private int fishPerWave = 40;
    [SerializeField] private float delayBetweenWaves = 2f;
    
    // ✏️ DITAMBAH: Tracking per species
    [Header("Species Distribution (Optional)")]
    [SerializeField] private bool useSpeciesWeights = false;
    [SerializeField] private List<float> speciesWeights = new List<float>();
    
    private int currentPopulation = 0;
    
    void Start()
    {
        // ✏️ DITAMBAH: Validation
        if (fishPrefabs.Count == 0)
        {
            Debug.LogError("[FishSpawner] No fish prefabs assigned!");
            return;
        }
        
        if (spawnZone == null)
        {
            spawnZone = GetComponent<FishZone>();
        }
        
        // ✏️ DITAMBAH: Setup default weights jika tidak diset
        if (useSpeciesWeights && speciesWeights.Count != fishPrefabs.Count)
        {
            Debug.LogWarning("[FishSpawner] Species weights count mismatch, using equal distribution");
            useSpeciesWeights = false;
        }
        
        if (spawnInWaves)
        {
            StartCoroutine(SpawnInWaves());
        }
        else
        {
            SpawnInitialFish();
        }
    }
    
    private void SpawnInitialFish()
    {
        int spawnCount = Mathf.Min(initialSpawnCount, maxPopulation);
        
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnSingleFish();
        }
        
        Debug.Log($"[FishSpawner] Spawned {spawnCount} fish of {fishPrefabs.Count} species");
    }
    
    private IEnumerator SpawnInWaves()
    {
        int totalSpawned = 0;
        int targetSpawn = Mathf.Min(initialSpawnCount, maxPopulation);
        
        while (totalSpawned < targetSpawn)
        {
            int spawnThisWave = Mathf.Min(fishPerWave, targetSpawn - totalSpawned);
            
            for (int i = 0; i < spawnThisWave; i++)
            {
                SpawnSingleFish();
                totalSpawned++;
            }
            
            Debug.Log($"[FishSpawner] Wave spawned {spawnThisWave} fish. Total: {totalSpawned}/{targetSpawn}");
            
            if (totalSpawned < targetSpawn)
            {
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }
    }
    
    // ✏️ DIUBAH: Support multiple species
    private void SpawnSingleFish()
    {
        if (currentPopulation >= maxPopulation)
        {
            Debug.LogWarning("[FishSpawner] Max population reached!");
            return;
        }
        
        // ✏️ DITAMBAH: Pilih random species
        GameObject selectedPrefab = GetRandomFishPrefab();
        
        Vector3 spawnPos = spawnZone.GetRandomPointInZone();
        Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        
        GameObject fishObj = Instantiate(selectedPrefab, spawnPos, spawnRot);
        currentPopulation++;
    }
    
    // ✏️ DITAMBAH: Get random fish dengan weighted distribution
    private GameObject GetRandomFishPrefab()
    {
        if (!useSpeciesWeights || speciesWeights.Count != fishPrefabs.Count)
        {
            // Equal distribution
            return fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        }
        
        // Weighted distribution
        float totalWeight = 0f;
        foreach (float weight in speciesWeights)
        {
            totalWeight += weight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;
        
        for (int i = 0; i < fishPrefabs.Count; i++)
        {
            cumulativeWeight += speciesWeights[i];
            if (randomValue <= cumulativeWeight)
            {
                return fishPrefabs[i];
            }
        }
        
        return fishPrefabs[0]; // Fallback
    }
    
    public void OnFishCaptured()
    {
        currentPopulation--;
        
        Debug.Log($"[FishSpawner] Fish captured. Population: {currentPopulation}/{maxPopulation}");
        
        if (autoRespawn && currentPopulation < maxPopulation)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }
    
    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        
        if (currentPopulation < maxPopulation)
        {
            SpawnSingleFish();
            Debug.Log($"[FishSpawner] Auto-respawned fish. Population: {currentPopulation}/{maxPopulation}");
        }
    }
}