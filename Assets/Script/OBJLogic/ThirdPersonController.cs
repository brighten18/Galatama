using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [System.Serializable]
        public class SurfaceAudioSet
        {
            public PlayerSurfaceType SurfaceType = PlayerSurfaceType.Default;
            public AudioClip[] FootstepClips;
            public AudioClip LandingClip;
        }

        [System.Serializable]
        public class TerrainTextureSurfaceSet
        {
            public TerrainLayer TerrainLayer;
            public PlayerSurfaceType SurfaceType = PlayerSurfaceType.Default;
        }

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("Surface Footstep Audio")]
        [Tooltip("Mapping jenis permukaan ke kumpulan audio langkah dan landing.")]
        [SerializeField] private SurfaceAudioSet[] surfaceAudioSets;
        [Tooltip("Mapping Terrain Layer ke jenis permukaan player.")]
        [SerializeField] private TerrainTextureSurfaceSet[] terrainTextureSurfaceSets;
        [Tooltip("Jarak raycast untuk mendeteksi permukaan di bawah kaki player.")]
        [SerializeField] private float surfaceCheckDistance = 1.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Tooltip("Multiplier applied to gravity while the player is falling (vertical velocity < 0). Higher values make the fall feel heavier and more responsive.")]
        public float FallMultiplier = 2.5f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // Jeda setelah lompat agar GroundedCheck tidak langsung memutus jump
        private float _jumpGroundCooldown = 0f;
        private const float JumpGroundCooldownDuration = 0.15f;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDPickUp;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private bool _movementBlocked;

        // Apakah cursor sedang ditampilkan (toggle dengan Ctrl)
        private bool _cursorVisible;

        // Apakah rotasi kamera dikunci secara eksternal (misalnya saat popup/interaksi terbuka)
        private bool _cameraLookLocked;

        // Apakah klik kanan sedang ditahan untuk rotasi kamera (hanya aktif saat cursor visible)
        private bool _rightClickHeld;
        private bool _externalCameraOverrideActive;
        private float _externalCameraYaw;
        private float _externalCameraPitch;
        private PlayerSurfaceType _currentSurfaceType = PlayerSurfaceType.Default;
        private readonly List<SurfaceOverrideZone> _activeTriggerSurfaceZones = new List<SurfaceOverrideZone>();

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // Mulai dengan kursor tersembunyi dan terkurung dalam window; rotasi kamera aktif langsung
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
            _input.cursorInputForLook = true;
        }

        private void Update()
        {
            GroundedCheck();
            JumpAndGravity();
            Move();
            HandlePickUpInput();
            HandleCtrlCameraMode();
            HandleRightClickCamera();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDPickUp = Animator.StringToHash("PickUp");
        }

        private void GroundedCheck()
        {
            // Saat cooldown post-jump aktif, anggap tidak grounded agar velocity tidak di-reset
            if (_jumpGroundCooldown > 0f)
            {
                _jumpGroundCooldown -= Time.deltaTime;
                Grounded = false;
                if (_hasAnimator)
                    _animator.SetBool(_animIDGrounded, false);
                return;
            }

            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            DetectCurrentSurface();

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void DetectCurrentSurface()
        {
            if (TryGetActiveTriggerSurface(out PlayerSurfaceType triggerSurfaceType))
            {
                _currentSurfaceType = triggerSurfaceType;
                return;
            }

            // CharacterController does not fire OnTriggerEnter on its own MonoBehaviours,
            // so we perform a direct overlap check each frame to catch SurfaceOverrideZone triggers.
            if (TryGetOverlapTriggerSurface(out PlayerSurfaceType overlapSurfaceType))
            {
                _currentSurfaceType = overlapSurfaceType;
                return;
            }

            Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, surfaceCheckDistance, GroundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                if (TryGetSurfaceOverrideFromCollider(hit.collider, out PlayerSurfaceType overrideSurfaceType))
                {
                    _currentSurfaceType = overrideSurfaceType;
                    return;
                }

                if (TryGetTerrainSurface(hit, out PlayerSurfaceType terrainSurfaceType))
                {
                    _currentSurfaceType = terrainSurfaceType;
                    return;
                }
            }

            _currentSurfaceType = PlayerSurfaceType.Default;
        }

        private SurfaceAudioSet GetCurrentSurfaceAudioSet()
        {
            if (surfaceAudioSets == null || surfaceAudioSets.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < surfaceAudioSets.Length; i++)
            {
                SurfaceAudioSet surfaceAudioSet = surfaceAudioSets[i];
                if (surfaceAudioSet != null && surfaceAudioSet.SurfaceType == _currentSurfaceType)
                {
                    return surfaceAudioSet;
                }
            }

            return null;
        }

        private static AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, clips.Length);
            return clips[index];
        }

        private bool TryGetActiveTriggerSurface(out PlayerSurfaceType surfaceType)
        {
            for (int i = _activeTriggerSurfaceZones.Count - 1; i >= 0; i--)
            {
                SurfaceOverrideZone zone = _activeTriggerSurfaceZones[i];
                if (zone == null)
                {
                    _activeTriggerSurfaceZones.RemoveAt(i);
                    continue;
                }

                surfaceType = zone.SurfaceType;
                return true;
            }

            surfaceType = PlayerSurfaceType.Default;
            return false;
        }

        private static bool TryGetSurfaceOverrideFromCollider(Collider collider, out PlayerSurfaceType surfaceType)
        {
            SurfaceOverrideZone zone = collider.GetComponent<SurfaceOverrideZone>();
            if (zone == null)
            {
                zone = collider.GetComponentInParent<SurfaceOverrideZone>();
            }

            if (zone != null)
            {
                surfaceType = zone.SurfaceType;
                return true;
            }

            surfaceType = PlayerSurfaceType.Default;
            return false;
        }

        private bool TryGetTerrainSurface(RaycastHit hit, out PlayerSurfaceType surfaceType)
        {
            if (!(hit.collider is TerrainCollider terrainCollider))
            {
                surfaceType = PlayerSurfaceType.Default;
                return false;
            }

            Terrain terrain = terrainCollider.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null || terrainTextureSurfaceSets == null || terrainTextureSurfaceSets.Length == 0)
            {
                surfaceType = PlayerSurfaceType.Default;
                return false;
            }

            TerrainData terrainData = terrain.terrainData;
            Vector3 localPosition = hit.point - terrain.transform.position;
            int mapX = Mathf.Clamp((int)((localPosition.x / terrainData.size.x) * terrainData.alphamapWidth), 0, terrainData.alphamapWidth - 1);
            int mapZ = Mathf.Clamp((int)((localPosition.z / terrainData.size.z) * terrainData.alphamapHeight), 0, terrainData.alphamapHeight - 1);
            float[,,] alphaMap = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            int dominantLayerIndex = 0;
            float dominantLayerWeight = 0f;
            for (int i = 0; i < terrainData.alphamapLayers; i++)
            {
                float weight = alphaMap[0, 0, i];
                if (weight > dominantLayerWeight)
                {
                    dominantLayerWeight = weight;
                    dominantLayerIndex = i;
                }
            }

            TerrainLayer[] terrainLayers = terrainData.terrainLayers;
            if (dominantLayerIndex < 0 || dominantLayerIndex >= terrainLayers.Length)
            {
                surfaceType = PlayerSurfaceType.Default;
                return false;
            }

            TerrainLayer dominantLayer = terrainLayers[dominantLayerIndex];
            for (int i = 0; i < terrainTextureSurfaceSets.Length; i++)
            {
                TerrainTextureSurfaceSet textureSurfaceSet = terrainTextureSurfaceSets[i];
                if (textureSurfaceSet != null && textureSurfaceSet.TerrainLayer == dominantLayer)
                {
                    surfaceType = textureSurfaceSet.SurfaceType;
                    return true;
                }
            }

            surfaceType = PlayerSurfaceType.Default;
            return false;
        }

        /// <summary>
        /// Checks whether the player capsule physically overlaps any trigger SurfaceOverrideZone.
        /// Needed because CharacterController does not raise OnTrigger* callbacks on its own GameObject.
        /// </summary>
        private bool TryGetOverlapTriggerSurface(out PlayerSurfaceType surfaceType)
        {
            float halfHeight = _controller.height * 0.5f - _controller.radius;
            Vector3 worldCenter = transform.position + _controller.center;
            Vector3 capsuleBottom = worldCenter - Vector3.up * halfHeight;
            Vector3 capsuleTop = worldCenter + Vector3.up * halfHeight;

            Collider[] overlaps = Physics.OverlapCapsule(
                capsuleBottom, capsuleTop, _controller.radius,
                ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider col = overlaps[i];
                if (!col.isTrigger) continue;

                SurfaceOverrideZone zone = col.GetComponent<SurfaceOverrideZone>();
                if (zone == null)
                    zone = col.GetComponentInParent<SurfaceOverrideZone>();

                if (zone != null)
                {
                    surfaceType = zone.SurfaceType;
                    return true;
                }
            }

            surfaceType = PlayerSurfaceType.Default;
            return false;
        }

        private void CameraRotation()
        {
            if (_externalCameraOverrideActive)
            {
                _cinemachineTargetYaw = ClampAngle(_externalCameraYaw, float.MinValue, float.MaxValue);
                _cinemachineTargetPitch = ClampAngle(_externalCameraPitch, BottomClamp, TopClamp);
            }
            // if there is an input, camera is not locked, and camera position is not fixed
            else if (!_cameraLookLocked && _input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (_movementBlocked)
            {
                _input.move = Vector2.zero;
                _input.sprint = false;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iter...

            // note: Vector2's == operator uses approximation so is not floating point error prone, and i...
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // Normalize animationBlend to 0-1 range to match the blend tree thresholds (0 = Idle, 1 = Wa...
            float maxSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            float normalizedTarget = (maxSpeed > 0f && targetSpeed > 0f) ? 1f : 0f;
            _animationBlend = Mathf.Lerp(_animationBlend, normalizedTarget, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and i...
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (_movementBlocked)
            {
                _input.jump = false;
            }

            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump â€” only allowed when grounded AND jump timeout has elapsed
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // Aktifkan cooldown agar GroundedCheck tidak langsung menghentikan lompatan
                    _jumpGroundCooldown = JumpGroundCooldownDuration;

                    // Reset jump timeout so the player cannot jump again immediately
                    _jumpTimeoutDelta = JumpTimeout;

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // Count down jump timeout only while grounded (so it doesn't reset in air)
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }

                // Consume jump input so it doesn't carry over
                _input.jump = false;
            }
            else
            {
                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // Apply gravity over time if under terminal velocity.
            // When falling (verticalVelocity < 0), apply FallMultiplier to make the descent
            // feel heavier and prevent the floaty "moon gravity" effect.
            if (_verticalVelocity < _terminalVelocity)
            {
                float gravityScale = _verticalVelocity < 0f ? FallMultiplier : 1f;
                _verticalVelocity += Gravity * gravityScale * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded colli...
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        /// <summary>
        /// Trigger animasi PickUp pada Animator player.
        /// </summary>
        public void TriggerPickUpAnimation()
        {
            if (_hasAnimator)
            {
                _animator.SetTrigger(_animIDPickUp);
            }
        }

        /// <summary>
        /// Mendeteksi input Interact dan InteractOBJ setiap frame dan memicu animasi PickUp bila play...
        /// </summary>
        private void HandlePickUpInput()
        {
            if (!_hasAnimator || _movementBlocked || !Grounded) return;
            if (_input.Interact || _input.InteractOBJ)
            {
                _animator.SetTrigger(_animIDPickUp);
            }
        }

        public void SetMovementBlocked(bool blocked)
        {
            _movementBlocked = blocked;
            if (blocked)
            {
                _input.move = Vector2.zero;
                _input.sprint = false;
                _input.jump = false;
            }
        }

        /// <summary>
        /// Mengunci atau membuka kunci rotasi kamera secara eksternal.
        /// Dipanggil saat popup/UI interaksi terbuka agar kamera berhenti bergerak.
        /// </summary>
        public void SetCameraLookLocked(bool locked)
        {
            _cameraLookLocked = locked;
            if (locked)
                _input.look = Vector2.zero;
        }

        public float GetCameraYaw()
        {
            return _cinemachineTargetYaw;
        }

        public float GetCameraPitch()
        {
            return _cinemachineTargetPitch;
        }

        public void SetExternalCameraAngles(float yaw, float pitch)
        {
            _externalCameraOverrideActive = true;
            _externalCameraYaw = yaw;
            _externalCameraPitch = pitch;
        }

        public void SetExternalCameraLookAt(Vector3 worldTarget)
        {
            if (CinemachineCameraTarget == null)
            {
                return;
            }

            Vector3 origin = CinemachineCameraTarget.transform.position;
            Vector3 direction = worldTarget - origin;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float flatDistance = Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z);
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(direction.y, flatDistance) * Mathf.Rad2Deg - CameraAngleOverride;
            SetExternalCameraAngles(yaw, pitch);
        }

        public void ClearExternalCameraOverride()
        {
            _externalCameraOverrideActive = false;
        }

        /// <summary>
        /// Memantau tombol Ctrl untuk toggle visibilitas kursor:
        /// - Ctrl ditekan       : tampilkan cursor (None), nonaktifkan rotasi kamera bebas.
        /// - Ctrl ditekan lagi  : sembunyikan cursor (Confined), aktifkan kembali rotasi kamera bebas.
        /// </summary>
        private void HandleCtrlCameraMode()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return;
            if (!Keyboard.current.ctrlKey.wasPressedThisFrame) return;
            if (_cameraLookLocked) return; // Jangan izinkan toggle saat kamera dikunci eksternal

            _cursorVisible = !_cursorVisible;
            Cursor.visible = _cursorVisible;
            Cursor.lockState = _cursorVisible ? CursorLockMode.None : CursorLockMode.Confined;

            // Reset look agar tidak ada seretan kamera saat mode berganti
            _input.look = Vector2.zero;

            if (!_cursorVisible)
                _rightClickHeld = false;

            _input.cursorInputForLook = !_cursorVisible;
#endif
        }

        /// <summary>
        /// Saat cursor visible, tahan klik kanan untuk merotasi kamera.
        /// Cursor disembunyikan sementara (Confined) selama klik kanan ditahan.
        /// </summary>
        private void HandleRightClickCamera()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null || !_cursorVisible || _cameraLookLocked) return;

            bool rightPressed = Mouse.current.rightButton.isPressed;
            if (rightPressed == _rightClickHeld) return;

            _rightClickHeld = rightPressed;

            if (_rightClickHeld)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
                _input.cursorInputForLook = true;
            }
            else
            {
                _input.look = Vector2.zero;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                _input.cursorInputForLook = false;
            }
#endif
        }

        /// <summary>
        /// Pulihkan state cursor saat window mendapat fokus kembali.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;

            bool shouldHide = !_cursorVisible || _rightClickHeld;
            Cursor.visible = !shouldHide;
            Cursor.lockState = shouldHide ? CursorLockMode.Confined : CursorLockMode.None;
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterTriggerSurface(other);
        }

        private void OnTriggerStay(Collider other)
        {
            RegisterTriggerSurface(other);
        }

        private void OnTriggerExit(Collider other)
        {
            UnregisterTriggerSurface(other);
        }

        private void RegisterTriggerSurface(Collider other)
        {
            if (other == null || !other.isTrigger)
            {
                return;
            }

            SurfaceOverrideZone zone = other.GetComponent<SurfaceOverrideZone>();
            if (zone == null)
            {
                zone = other.GetComponentInParent<SurfaceOverrideZone>();
            }

            if (zone != null && !_activeTriggerSurfaceZones.Contains(zone))
            {
                _activeTriggerSurfaceZones.Add(zone);
            }
        }

        private void UnregisterTriggerSurface(Collider other)
        {
            if (other == null)
            {
                return;
            }

            SurfaceOverrideZone zone = other.GetComponent<SurfaceOverrideZone>();
            if (zone == null)
            {
                zone = other.GetComponentInParent<SurfaceOverrideZone>();
            }

            if (zone != null)
            {
                _activeTriggerSurfaceZones.Remove(zone);
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                SurfaceAudioSet currentSurfaceAudio = GetCurrentSurfaceAudioSet();
                AudioClip clip = currentSurfaceAudio != null
                    ? GetRandomClip(currentSurfaceAudio.FootstepClips)
                    : GetRandomClip(FootstepAudioClips);

                if (clip != null)
                {
                    AudioSource.PlayClipAtPoint(clip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                SurfaceAudioSet currentSurfaceAudio = GetCurrentSurfaceAudioSet();
                AudioClip clip = currentSurfaceAudio != null && currentSurfaceAudio.LandingClip != null
                    ? currentSurfaceAudio.LandingClip
                    : LandingAudioClip;

                if (clip != null)
                {
                    AudioSource.PlayClipAtPoint(clip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }
    }
}
