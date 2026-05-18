using System;
using UnityEngine;

public class FishTrapPlacer : MonoBehaviour
{
    [Header("Trap Setup")]
    [SerializeField] private string trapItemName = "Perangkap";
    [SerializeField] private GameObject worldTrapPrefab;

    [Header("Placement")]
    [SerializeField] private float maxPlacementDistance = 10f;

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
        if (!TryGetOceanPlacement(out Vector3 placePosition))
        {
            Debug.Log("[FishTrapPlacer] Perangkap hanya bisa dipasang di FishZone Ocean.");
            return;
        }

        GameObject prefab = worldTrapPrefab != null ? worldTrapPrefab : Resources.Load<GameObject>("Perangkap_World");
        if (prefab == null)
        {
            Debug.LogError("[FishTrapPlacer] Prefab Perangkap_World belum tersedia di Resources.");
            return;
        }

        GameObject spawnedTrap = Instantiate(prefab, placePosition, Quaternion.identity);
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

    private bool TryGetOceanPlacement(out Vector3 placePosition)
    {
        placePosition = Vector3.zero;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[FishTrapPlacer] Camera.main tidak ditemukan.");
            return false;
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxPlacementDistance, Physics.AllLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            FishZone fishZone = hit.collider.GetComponentInParent<FishZone>();
            if (fishZone != null && fishZone.ZoneType == ZoneType.Ocean)
            {
                placePosition = hit.point;
                return true;
            }
        }

        return false;
    }
}
