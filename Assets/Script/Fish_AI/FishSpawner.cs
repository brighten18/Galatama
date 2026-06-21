using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<GameObject> fishPrefabs = new List<GameObject>();
    [SerializeField] private FishZone spawnZone;
    [SerializeField] private bool parentSpawnedFish = true;
    [SerializeField] private float spawnPadding = 2f;
    [SerializeField] private float initialSpawnSeparation = 1.5f;
    [SerializeField] private int spawnSearchAttempts = 12;

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

    [Header("Species Distribution (Optional)")]
    [SerializeField] private bool useSpeciesWeights = false;
    [SerializeField] private List<float> speciesWeights = new List<float>();

    private int currentPopulation = 0;
    private float totalSpeciesWeight = 0f;
    private WaitForSeconds respawnWait;
    private WaitForSeconds waveWait;
    private readonly List<Vector3> recentSpawnPositions = new List<Vector3>();

    void Awake()
    {
        if (spawnZone == null)
        {
            spawnZone = GetComponent<FishZone>();
        }

        respawnWait = new WaitForSeconds(respawnDelay);
        waveWait = new WaitForSeconds(delayBetweenWaves);
        RecalculateSpeciesWeights();
    }

    void Start()
    {
        if (!ValidateSetup()) return;

        if (spawnInWaves)
        {
            StartCoroutine(SpawnInWaves());
        }
        else
        {
            SpawnInitialFish();
        }
    }

    private bool ValidateSetup()
    {
        fishPrefabs.RemoveAll(prefab => prefab == null);

        if (fishPrefabs.Count == 0)
        {
            Debug.LogError("[FishSpawner] No fish prefabs assigned!");
            return false;
        }

        if (spawnZone == null)
        {
            Debug.LogError("[FishSpawner] Spawn zone belum diassign.");
            return false;
        }

        if (maxPopulation < 0) maxPopulation = 0;
        if (initialSpawnCount < 0) initialSpawnCount = 0;
        if (fishPerWave <= 0) fishPerWave = Mathf.Max(1, initialSpawnCount);

        RecalculateSpeciesWeights();
        return true;
    }

    private void RecalculateSpeciesWeights()
    {
        totalSpeciesWeight = 0f;

        if (!useSpeciesWeights || speciesWeights.Count != fishPrefabs.Count)
        {
            useSpeciesWeights = false;
            return;
        }

        for (int i = 0; i < speciesWeights.Count; i++)
        {
            totalSpeciesWeight += Mathf.Max(0f, speciesWeights[i]);
        }

        if (totalSpeciesWeight <= 0f)
        {
            useSpeciesWeights = false;
        }
    }

    private void SpawnInitialFish()
    {
        int spawnCount = Mathf.Min(initialSpawnCount, maxPopulation);
        recentSpawnPositions.Clear();

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
                if (SpawnSingleFish() != null)
                {
                    totalSpawned++;
                }
            }

            Debug.Log($"[FishSpawner] Wave spawned {spawnThisWave} fish. Total: {totalSpawned}/{targetSpawn}");

            if (totalSpawned < targetSpawn)
            {
                yield return waveWait;
            }
        }
    }

    private GameObject SpawnSingleFish()
    {
        if (currentPopulation >= maxPopulation)
        {
            return null;
        }

        GameObject selectedPrefab = GetRandomFishPrefab();
        if (selectedPrefab == null) return null;

        Vector3 spawnPos = GetSpawnPosition();
        Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject fishObj = Instantiate(selectedPrefab, spawnPos, spawnRot);
        if (parentSpawnedFish)
        {
            fishObj.transform.SetParent(transform, true);
        }

        FishBrain fishBrain = fishObj.GetComponent<FishBrain>();
        if (fishBrain != null)
        {
            fishBrain.SetSpawner(this);
            fishBrain.SetZoneType(spawnZone.ZoneType);
            fishBrain.SetBoundary(spawnZone.GetCollider());
        }
        else
        {
            Debug.LogWarning($"[FishSpawner] {fishObj.name} tidak punya FishBrain, boundary AI tidak diterapkan.");
        }

        recentSpawnPositions.Add(spawnPos);
        currentPopulation++;
        return fishObj;
    }

    private GameObject GetRandomFishPrefab()
    {
        if (!useSpeciesWeights)
        {
            return fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        }

        float randomValue = Random.Range(0f, totalSpeciesWeight);
        float cumulativeWeight = 0f;

        for (int i = 0; i < fishPrefabs.Count; i++)
        {
            cumulativeWeight += Mathf.Max(0f, speciesWeights[i]);
            if (randomValue <= cumulativeWeight)
            {
                return fishPrefabs[i];
            }
        }

        return fishPrefabs[0];
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 fallback = spawnZone.GetRandomPointInZone(spawnPadding);
        if (recentSpawnPositions.Count == 0 || initialSpawnSeparation <= 0f)
            return fallback;

        Vector3 bestCandidate = fallback;
        float bestDistanceScore = float.NegativeInfinity;
        int attempts = Mathf.Max(1, spawnSearchAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = spawnZone.GetRandomPointInZone(spawnPadding);
            float nearestSqrDistance = GetNearestSpawnSqrDistance(candidate);

            if (nearestSqrDistance >= initialSpawnSeparation * initialSpawnSeparation)
                return candidate;

            if (nearestSqrDistance > bestDistanceScore)
            {
                bestDistanceScore = nearestSqrDistance;
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }

    private float GetNearestSpawnSqrDistance(Vector3 candidate)
    {
        float nearest = float.MaxValue;

        for (int i = 0; i < recentSpawnPositions.Count; i++)
        {
            float sqrDistance = (candidate - recentSpawnPositions[i]).sqrMagnitude;
            if (sqrDistance < nearest)
                nearest = sqrDistance;
        }

        return nearest;
    }

    public void OnFishCaptured()
    {
        currentPopulation = Mathf.Max(0, currentPopulation - 1);

        Debug.Log($"[FishSpawner] Fish captured. Population: {currentPopulation}/{maxPopulation}");

        if (autoRespawn && currentPopulation < maxPopulation)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return respawnWait;

        if (currentPopulation < maxPopulation)
        {
            SpawnSingleFish();
            Debug.Log($"[FishSpawner] Auto-respawned fish. Population: {currentPopulation}/{maxPopulation}");
        }
    }
}
