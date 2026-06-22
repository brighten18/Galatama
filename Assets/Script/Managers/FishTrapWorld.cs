using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GALATAMA.MainMenu;

public class FishTrapWorld : InteractableObject
{
    /// <summary>Fired when a fish is captured. Parameter is the fish item name.</summary>
    public static event System.Action<string> OnFishCaptured;

    [Header("Capture")]
    [SerializeField] private float lureRadius = 8f;
    [SerializeField] private float captureRadius = 0.8f;
    [SerializeField] private float minCaptureDelay = 6f;
    [SerializeField] private float maxCaptureDelay = 14f;
    [SerializeField] private float targetTimeout = 12f;
    [SerializeField] private LayerMask fishLayer = ~0;

    private bool isActive;
    private bool hasCaughtFish;
    private bool playerInPickupRange;
    private string capturedFishItemName;
    private Coroutine captureRoutine;
    private TrapWorldMarker worldMarker;

    public bool IsTrapActive => isActive;
    public bool HasCaughtFish => hasCaughtFish;
    public string CapturedFishItemName => capturedFishItemName;

    void Awake()
    {
        base.Awake();
        itemName = "Perangkap";
        worldMarker = GetComponent<TrapWorldMarker>();
    }

    protected override void Update()
    {
        if (QuizSessionLock.IsLocked)
        {
            if (PlayerInputManager.Instance != null)
                PlayerInputManager.Instance.ResetInteractInput();
            return;
        }

        if (playerInPickupRange &&
            PlayerInputManager.Instance != null &&
            PlayerInputManager.Instance.Interact &&
            InventorySystem.Instance != null &&
            !InventorySystem.Instance.isOpen)
        {
            HandleInteract();
            return;
        }

        base.Update();
    }

    public void ActivateTrap()
    {
        if (isActive || hasCaughtFish)
        {
            return;
        }

        isActive = true;
        captureRoutine = StartCoroutine(CaptureLoop());
        worldMarker?.SetCapturedState(false);
    }

    public void SetPlayerInPickupRange(bool value)
    {
        playerInPickupRange = value;
    }

    protected override void HandleInteract()
    {
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.ResetInteractInput();
        }

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogError("[FishTrapWorld] InventorySystem tidak ditemukan.");
            return;
        }

        if (hasCaughtFish)
        {
            if (!inventory.CanAddItemsToInventory(itemName, capturedFishItemName))
            {
                Debug.Log("[FishTrapWorld] Butuh 2 slot kosong untuk mengambil perangkap berisi ikan.");
                return;
            }

            inventory.TryAddItemToInventory(itemName);
            inventory.TryAddItemToInventory(capturedFishItemName);
            Destroy(gameObject);
            return;
        }

        if (!inventory.CanAddItemsToInventory(itemName))
        {
            Debug.Log("[FishTrapWorld] Inventory penuh, tidak bisa mengambil perangkap kosong.");
            return;
        }

        inventory.TryAddItemToInventory(itemName);
        Destroy(gameObject);
    }

    public override string GetItemName()
    {
        if (hasCaughtFish && !string.IsNullOrEmpty(capturedFishItemName))
        {
            return itemName + " (" + capturedFishItemName + ")";
        }

        return itemName;
    }

    private IEnumerator CaptureLoop()
    {
        while (isActive && !hasCaughtFish)
        {
            yield return new WaitForSeconds(Random.Range(minCaptureDelay, maxCaptureDelay));

            FishBrain targetFish = FindFishTarget();
            if (targetFish == null)
            {
                continue;
            }

            yield return LureFish(targetFish);
        }
    }

    private IEnumerator LureFish(FishBrain targetFish)
    {
        targetFish.SetTemporaryTarget(transform.position, captureRadius);

        float elapsed = 0f;
        while (targetFish != null && !targetFish.IsCaptured && elapsed < targetTimeout)
        {
            if (Vector3.Distance(targetFish.transform.position, transform.position) <= captureRadius)
            {
                CaptureFish(targetFish);
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (targetFish != null && !targetFish.IsCaptured)
        {
            targetFish.ClearTemporaryTarget();
        }
    }

    private FishBrain FindFishTarget()
    {
        Collider[] fishColliders = Physics.OverlapSphere(transform.position, lureRadius, fishLayer, QueryTriggerInteraction.Collide);
        List<FishBrain> candidates = new List<FishBrain>();

        foreach (Collider fishCollider in fishColliders)
        {
            FishBrain fishBrain = fishCollider.GetComponentInParent<FishBrain>();
            FishBase fishBase = fishCollider.GetComponentInParent<FishBase>();
            if (fishBrain != null && fishBase != null && !fishBrain.IsCaptured && fishBase.fishData != null && !candidates.Contains(fishBrain))
            {
                candidates.Add(fishBrain);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void CaptureFish(FishBrain targetFish)
    {
        FishBase fishBase = targetFish.GetComponent<FishBase>();
        if (fishBase == null || fishBase.fishData == null)
        {
            targetFish.ClearTemporaryTarget();
            return;
        }

        capturedFishItemName = fishBase.fishData.ItemName;
        hasCaughtFish = true;
        isActive = false;

        targetFish.OnCaptured(false);
        Destroy(targetFish.gameObject);

        if (captureRoutine != null)
        {
            StopCoroutine(captureRoutine);
            captureRoutine = null;
        }

        // Notify the world marker and the notification UI
        worldMarker?.SetCapturedState(true);
        OnFishCaptured?.Invoke(capturedFishItemName);

        Debug.Log("[FishTrapWorld] Perangkap menangkap ikan: " + capturedFishItemName);
    }

    public TrapSaveData CaptureSaveData()
    {
        return new TrapSaveData
        {
            position = transform.position,
            rotationEuler = transform.eulerAngles,
            isActive = isActive,
            hasCaughtFish = hasCaughtFish,
            capturedFishItemName = capturedFishItemName
        };
    }

    public void RestoreFromSaveData(TrapSaveData data)
    {
        if (data == null)
            return;

        if (captureRoutine != null)
        {
            StopCoroutine(captureRoutine);
            captureRoutine = null;
        }

        transform.position = data.position;
        transform.rotation = Quaternion.Euler(data.rotationEuler);
        playerInPickupRange = false;
        capturedFishItemName = data.capturedFishItemName;
        hasCaughtFish = data.hasCaughtFish && !string.IsNullOrEmpty(capturedFishItemName);
        isActive = data.isActive && !hasCaughtFish;

        worldMarker?.SetCapturedState(hasCaughtFish);

        if (isActive)
            captureRoutine = StartCoroutine(CaptureLoop());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lureRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}
