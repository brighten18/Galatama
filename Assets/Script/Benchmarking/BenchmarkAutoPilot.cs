using StarterAssets;
using UnityEngine;

namespace GALATAMA.Benchmarking
{
    public class BenchmarkAutoPilot : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private Transform waypointRoot;
        [SerializeField] private bool loop = true;
        [SerializeField] private float reachDistance = 0.75f;
        [SerializeField] private float waitAtWaypoint = 0.5f;

        [Header("Movement")]
        [SerializeField] private bool autoplayOnStart = true;
        [SerializeField] private bool useSprint = false;

        [Header("Camera")]
        [SerializeField] private Transform[] cameraLookTargets;
        [SerializeField] private Transform cameraTargetRoot;
        [SerializeField] private bool lookToNextWaypointWhenNoTarget = true;
        [SerializeField] private float cameraTurnSpeed = 180f;

        private StarterAssetsInputs playerInputs;
        private ThirdPersonController controller;
        private int currentWaypointIndex;
        private float waitTimer;
        private bool isRunning;

        private void Awake()
        {
            playerInputs = GetComponent<StarterAssetsInputs>();
            controller = GetComponent<ThirdPersonController>();
            CacheChildrenIfNeeded();
        }

        private void Start()
        {
            if (autoplayOnStart)
            {
                StartAutopilot();
            }
        }

        private void OnDisable()
        {
            StopAutopilot();
        }

        private void Update()
        {
            if (!isRunning || playerInputs == null || controller == null || waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            Transform currentWaypoint = GetWaypoint(currentWaypointIndex);
            if (currentWaypoint == null)
            {
                AdvanceWaypoint();
                return;
            }

            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = currentWaypoint.position;
            Vector3 toTarget = targetPosition - currentPosition;
            Vector3 planarToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
            float distance = planarToTarget.magnitude;

            if (distance <= reachDistance)
            {
                playerInputs.MoveInput(Vector2.zero);
                playerInputs.SprintInput(false);

                if (waitAtWaypoint > 0f)
                {
                    waitTimer += Time.deltaTime;
                    if (waitTimer < waitAtWaypoint)
                    {
                        UpdateCamera(currentWaypoint);
                        return;
                    }
                }

                waitTimer = 0f;
                AdvanceWaypoint();
                UpdateCamera(GetWaypoint(currentWaypointIndex));
                return;
            }

            Vector3 moveDirection = planarToTarget.normalized;
            float cameraYaw = controller.GetCameraYaw();
            Vector3 localMoveDirection = Quaternion.Inverse(Quaternion.Euler(0f, cameraYaw, 0f)) * moveDirection;
            Vector2 moveInput = new Vector2(localMoveDirection.x, localMoveDirection.z);
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            playerInputs.MoveInput(moveInput);
            playerInputs.SprintInput(useSprint);

            UpdateCamera(currentWaypoint);
        }

        public void StartAutopilot()
        {
            CacheChildrenIfNeeded();
            if (playerInputs == null)
            {
                playerInputs = GetComponent<StarterAssetsInputs>();
            }

            if (controller == null)
            {
                controller = GetComponent<ThirdPersonController>();
            }

            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, Mathf.Max(0, waypoints.Length - 1));
            waitTimer = 0f;
            isRunning = waypoints != null && waypoints.Length > 0 && playerInputs != null && controller != null;
        }

        public void StopAutopilot()
        {
            isRunning = false;

            if (playerInputs != null)
            {
                playerInputs.MoveInput(Vector2.zero);
                playerInputs.SprintInput(false);
            }

            if (controller != null)
            {
                controller.ClearExternalCameraOverride();
            }
        }

        public void SetRunning(bool shouldRun)
        {
            if (shouldRun)
            {
                StartAutopilot();
                return;
            }

            StopAutopilot();
        }

        private void UpdateCamera(Transform currentWaypoint)
        {
            Transform lookTarget = GetCameraLookTarget(currentWaypoint);
            if (lookTarget == null)
            {
                controller.ClearExternalCameraOverride();
                return;
            }

            Vector3 origin = controller.CinemachineCameraTarget != null
                ? controller.CinemachineCameraTarget.transform.position
                : transform.position;
            Vector3 direction = lookTarget.position - origin;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float flatDistance = Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z);
            float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Atan2(direction.y, flatDistance) * Mathf.Rad2Deg - controller.CameraAngleOverride;

            float step = cameraTurnSpeed * Time.deltaTime;
            float nextYaw = Mathf.MoveTowardsAngle(controller.GetCameraYaw(), targetYaw, step);
            float nextPitch = Mathf.MoveTowardsAngle(controller.GetCameraPitch(), targetPitch, step);
            controller.SetExternalCameraAngles(nextYaw, nextPitch);
        }

        private Transform GetCameraLookTarget(Transform currentWaypoint)
        {
            if (cameraLookTargets != null && currentWaypointIndex >= 0 && currentWaypointIndex < cameraLookTargets.Length)
            {
                Transform target = cameraLookTargets[currentWaypointIndex];
                if (target != null)
                {
                    return target;
                }
            }

            if (!lookToNextWaypointWhenNoTarget)
            {
                return null;
            }

            return currentWaypoint;
        }

        private Transform GetWaypoint(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length)
            {
                return null;
            }

            return waypoints[index];
        }

        private void AdvanceWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                StopAutopilot();
                return;
            }

            currentWaypointIndex++;
            if (currentWaypointIndex < waypoints.Length)
            {
                return;
            }

            if (loop)
            {
                currentWaypointIndex = 0;
                return;
            }

            currentWaypointIndex = waypoints.Length - 1;
            StopAutopilot();
        }

        private void CacheChildrenIfNeeded()
        {
            if ((waypoints == null || waypoints.Length == 0) && waypointRoot != null)
            {
                waypoints = GetOrderedChildren(waypointRoot);
            }

            if ((cameraLookTargets == null || cameraLookTargets.Length == 0) && cameraTargetRoot != null)
            {
                cameraLookTargets = GetOrderedChildren(cameraTargetRoot);
            }
        }

        private static Transform[] GetOrderedChildren(Transform root)
        {
            if (root == null)
            {
                return new Transform[0];
            }

            Transform[] children = new Transform[root.childCount];
            for (int i = 0; i < root.childCount; i++)
            {
                children[i] = root.GetChild(i);
            }

            return children;
        }

        private void OnDrawGizmosSelected()
        {
            CacheChildrenIfNeeded();
            if (waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform point = waypoints[i];
                if (point == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(point.position, reachDistance);

                Transform nextPoint = null;
                if (i + 1 < waypoints.Length)
                {
                    nextPoint = waypoints[i + 1];
                }
                else if (loop)
                {
                    nextPoint = waypoints[0];
                }

                if (nextPoint != null)
                {
                    Gizmos.DrawLine(point.position, nextPoint.position);
                }
            }

            Gizmos.color = Color.yellow;
            if (cameraLookTargets == null)
            {
                return;
            }

            for (int i = 0; i < cameraLookTargets.Length; i++)
            {
                Transform lookTarget = cameraLookTargets[i];
                Transform point = i < waypoints.Length ? waypoints[i] : null;
                if (lookTarget == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(lookTarget.position, 0.2f);
                if (point != null)
                {
                    Gizmos.DrawLine(point.position, lookTarget.position);
                }
            }
        }
    }
}
