using UnityEngine;

/// <summary>
/// Completes the target mission when the player enters this trigger area.
/// Attach this to a GameObject with an Is Trigger Collider.
/// </summary>
public class AreaTriggerMission : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [SerializeField] private int targetMissionIndex = 1;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(PlayerTag)) return;

        _triggered = true;

        if (MissionManager.Instance != null)
            MissionManager.Instance.CompleteMission(targetMissionIndex);
    }
}
