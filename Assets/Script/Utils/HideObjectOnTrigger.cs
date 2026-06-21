using UnityEngine;

/// <summary>
/// Hides the target GameObjects when the player enters this trigger area.
/// Attach this to a GameObject with an Is Trigger Collider.
/// </summary>
public class HideObjectOnTrigger : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [SerializeField] private GameObject[] objectsToHide;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(PlayerTag)) return;

        _triggered = true;

        foreach (var obj in objectsToHide)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}
