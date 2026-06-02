using UnityEngine;

public class BasicRigidBodyPush : MonoBehaviour
{
	public LayerMask pushLayers;
	public bool canPush;
	[Range(0.5f, 5f)] public float strength = 1.1f;

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (canPush) PushRigidBodies(hit);
	}

	private void PushRigidBodies(ControllerColliderHit hit)
	{
		// We dont want to push objects below us
		if (hit.moveDirection.y < -0.3f) return;

		// Calculate push direction from move direction, horizontal motion only
		Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

		// Try standard Rigidbody push first
		Rigidbody body = hit.collider.attachedRigidbody;
		if (body != null && !body.isKinematic)
		{
			var bodyLayerMask = 1 << body.gameObject.layer;
			if ((bodyLayerMask & pushLayers.value) != 0)
			{
				body.AddForce(pushDir * strength, ForceMode.Impulse);
			}
			return;
		}

		// Fallback: push fish or other Transform-based objects via FishPushResponse
		var fishPush = hit.collider.GetComponent<FishPushResponse>();
		if (fishPush != null)
		{
			var layerMask = 1 << hit.collider.gameObject.layer;
			if ((layerMask & pushLayers.value) != 0)
			{
				fishPush.ApplyPush(pushDir * strength);
			}
		}
	}
}
