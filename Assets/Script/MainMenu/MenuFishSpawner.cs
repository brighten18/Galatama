using UnityEngine;
using System.Collections.Generic;

public class MenuFishSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<GameObject> fishPrefabs;
    [SerializeField] private int fishCount = 10;
    [SerializeField] private bool spawnOnStart = true;
    
    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(20f, 10f, 20f);
    
    [Header("Fish Visuals")]
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;
    
    [Header("Global Speed Control")]
    [Range(0.1f, 3f)]
    [SerializeField] private float globalSpeedMultiplier = 1.0f;
    
    private List<GameObject> spawnedFish = new List<GameObject>();
    
    void Start()
    {
        if (spawnOnStart) SpawnAllFish();
    }
    
    public void SpawnAllFish()
    {
        ClearAllFish();
        if (fishPrefabs.Count == 0)
        {
            Debug.LogWarning("No fish prefabs assigned!");
            return;
        }
        for (int i = 0; i < fishCount; i++)
            SpawnSingleFish();
    }
    
    private void SpawnSingleFish()
    {
        GameObject prefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        Vector3 pos = spawnAreaCenter + new Vector3(
            Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
            Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f),
            Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f)
        );
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject fishObj = Instantiate(prefab, pos, rot, transform);
        fishObj.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);
        
        SimpleMenuFish fish = fishObj.GetComponent<SimpleMenuFish>();
        if (!fish) fish = fishObj.AddComponent<SimpleMenuFish>();
        
        fish.SetSwimArea(spawnAreaCenter, spawnAreaSize);
        fish.SetSpeedMultiplier(globalSpeedMultiplier);
        
        spawnedFish.Add(fishObj);
    }
    
    public void ClearAllFish()
    {
        foreach (var f in spawnedFish)
            if (f) Destroy(f);
        spawnedFish.Clear();
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0,1,1,0.3f);
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}