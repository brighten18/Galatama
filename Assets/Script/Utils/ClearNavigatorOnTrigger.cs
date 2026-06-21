using UnityEngine;

/// <summary>
/// Clears the MissionNavigator waypoint arrow when the player enters this trigger area.
/// Attach this to a GameObject with an Is Trigger Collider.
/// </summary>
public class ClearNavigatorOnTrigger : MonoBehaviour
{
    private const string PlayerTag = "Player";

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(PlayerTag)) return;

        _triggered = true;

        MissionNavigator navigator = FindAnyObjectByType<MissionNavigator>();
        if (navigator != null)
            navigator.ClearWaypoint();
    }
}
