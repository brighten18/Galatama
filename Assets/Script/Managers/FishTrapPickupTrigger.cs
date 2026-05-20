using UnityEngine;

public class FishTrapPickupTrigger : MonoBehaviour
{
    [SerializeField] private FishTrapWorld trapWorld;
    private int playerColliderCount;

    void Awake()
    {
        if (trapWorld == null)
        {
            trapWorld = GetComponentInParent<FishTrapWorld>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (trapWorld == null || !IsPlayer(other))
        {
            return;
        }

        playerColliderCount++;
        trapWorld.SetPlayerInPickupRange(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (trapWorld == null || !IsPlayer(other))
        {
            return;
        }

        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);
        trapWorld.SetPlayerInPickupRange(playerColliderCount > 0);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               (other.transform.root != null && other.transform.root.CompareTag("Player"));
    }
}
