using UnityEngine;

public class MenuFishAutoDestroy : MonoBehaviour
{
    [Header("Auto Destroy Settings")]
    [SerializeField] private float lifetime = 30f;
    [SerializeField] private bool respawnOnDestroy = true;
    
    private MenuFishSpawner spawner;
    private float spawnTime;
    
    void Start()
    {
        spawnTime = Time.time;
        spawner = GetComponentInParent<MenuFishSpawner>();
    }
    
    void Update()
    {
        if (Time.time - spawnTime > lifetime)
        {
            if (respawnOnDestroy && spawner != null)
            {
                // Spawner akan handle respawn
            }
            
            Destroy(gameObject);
        }
    }
}
