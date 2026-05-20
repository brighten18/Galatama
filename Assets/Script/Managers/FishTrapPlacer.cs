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

        // Spawn dulu di posisi sementara
        GameObject spawnedTrap = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        // Hitung tinggi objek dari Bounds semua Renderer-nya
        float halfHeight = GetObjectHalfHeight(spawnedTrap);

        // Letakan trap sehingga bagian bawahnya menyentuh lantai
        // halfHeight = jarak dari pivot ke bawah objek
        Vector3 finalPosition = floorPoint + Vector3.up * (halfHeight + trapBottomOffset);
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

    private float GetObjectHalfHeight(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            // Fallback ke Collider kalau tidak ada Renderer
            Collider col = obj.GetComponentInChildren<Collider>();
            if (col != null) return col.bounds.extents.y;
            return 0f;
        }

        // Gabungkan semua bounds dari setiap child renderer
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        // extents.y = setengah tinggi total objek
        return combinedBounds.extents.y;
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

        placePosition = seaFloorHit.point + Vector3.up * trapBottomOffset;
        return true;
    }
}