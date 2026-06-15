using System;
using UnityEngine;

public class FishTrapPlacer : MonoBehaviour
{
    [Header("Trap Setup")]
    [SerializeField] private string trapItemName = "Perangkap";
    [SerializeField] private GameObject worldTrapPrefab;

    [Header("Placement")]
    [SerializeField] private float maxPlacementDistance = 10f;
    [SerializeField] private float seaFloorRayDistance = 30f;
    [SerializeField] private float trapBottomOffset = 0.1f;
    [SerializeField] private LayerMask seaFloorLayer = ~0;

    private bool isPlacing;

    void Update()
    {
        if (QuizSessionLock.IsLocked)
        {
            if (PlayerInputManager.Instance != null)
                PlayerInputManager.Instance.ResetInteractOBJInput();
            return;
        }

        if (!IsEquippedTrap() || isPlacing)
        {
            return;
        }

        if (PlayerInputManager.Instance != null &&
            InventorySystem.Instance != null &&
            PlayerInputManager.Instance.InteractOBJ &&
            !InventorySystem.Instance.isOpen)
        {
            isPlacing = true;
            TryPlaceTrap();
            PlayerInputManager.Instance.ResetInteractOBJInput();
            isPlacing = false;
        }
    }

    private bool IsEquippedTrap()
    {
        return EquipSystem.Instance != null &&
               EquipSystem.Instance.isnowEquipped &&
               EquipSystem.Instance.GetEquippedType() == EquipmentType.Trap &&
               EquipSystem.Instance.GetEquippedItemName() == trapItemName &&
               transform.parent == EquipSystem.Instance.ToolsHolder.transform;
    }

    private void TryPlaceTrap()
    {
        if (!TryGetOceanPlacement(out Vector3 floorPoint))
        {
            Debug.Log("[FishTrapPlacer] Perangkap hanya bisa dipasang di dasar FishZone Ocean.");
            return;
        }

        GameObject prefab = worldTrapPrefab != null
            ? worldTrapPrefab
            : Resources.Load<GameObject>("Perangkap_World");

        if (prefab == null)
        {
            Debug.LogError("[FishTrapPlacer] Prefab tidak ditemukan.");
            return;
        }

        // Spawn sementara di origin untuk mengukur bounds dalam world space
        GameObject spawnedTrap = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        // Hitung offset pivot-ke-bawah: seberapa jauh pivot perlu diangkat agar
        // bagian bawah mesh tepat menyentuh lantai (tidak tergantung posisi pivot model)
        float pivotLift = GetPivotToBottomOffset(spawnedTrap);

        // Tempatkan pivot tepat di atas lantai; trapBottomOffset hanya diterapkan sekali di sini
        Vector3 finalPosition = floorPoint + Vector3.up * (pivotLift + trapBottomOffset);
        spawnedTrap.transform.position = finalPosition;

        FishTrapWorld trapWorld = spawnedTrap.GetComponent<FishTrapWorld>();
        if (trapWorld != null)
        {
            trapWorld.ActivateTrap();
        }

        if (!EquipSystem.Instance.TryConsumeSelectedItem(trapItemName))
        {
            Destroy(spawnedTrap);
            Debug.LogWarning("[FishTrapPlacer] Gagal konsumsi Perangkap dari quickslot.");
        }
    }

    /// <summary>
    /// Menghitung seberapa jauh pivot perlu diangkat dari lantai agar bagian bawah mesh
    /// tepat menyentuh lantai, terlepas dari posisi pivot di dalam model.
    /// </summary>
    private float GetPivotToBottomOffset(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        Bounds combined;
        if (renderers.Length > 0)
        {
            combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combined.Encapsulate(renderers[i].bounds);
        }
        else
        {
            // Fallback ke Collider non-trigger jika tidak ada Renderer
            Collider col = obj.GetComponentInChildren<Collider>();
            if (col == null || col.isTrigger) return 0f;
            combined = col.bounds;
        }

        // Saat objek berada di origin, combined.min.y = posisi Y terbawah mesh dalam world space.
        // Jika positif: pivot sudah di atas bawah mesh → tidak perlu angkat.
        // Jika negatif: bawah mesh di bawah pivot → pivot perlu diangkat sebesar |min.y|.
        float bottomY = combined.min.y;
        return Mathf.Max(0f, -bottomY);
    }

    private bool TryGetOceanPlacement(out Vector3 placePosition)
    {
        placePosition = Vector3.zero;

        // Titik awal ray dari posisi objek ini sendiri
        Vector3 rayOrigin = transform.position;

        // Tembak ke bawah sejauh seaFloorRayDistance
        if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit seaFloorHit,
                seaFloorRayDistance,
                seaFloorLayer,
                QueryTriggerInteraction.Ignore))
        {
            Debug.Log("[FishTrapPlacer] Dasar laut tidak ditemukan.");
            return false;
        }

        Collider[] nearby = Physics.OverlapSphere(seaFloorHit.point, 2f,
            Physics.AllLayers, QueryTriggerInteraction.Collide);

        bool insideOceanZone = false;
        foreach (Collider col in nearby)
        {
            FishZone zone = col.GetComponentInParent<FishZone>();
            if (zone != null && zone.ZoneType == ZoneType.Ocean)
            {
                insideOceanZone = true;
                break;
            }
        }

        if (!insideOceanZone)
        {
            Debug.Log("[FishTrapPlacer] Titik bukan bagian dari FishZone Ocean.");
            return false;
        }

        placePosition = seaFloorHit.point;
        return true;
    }
}
