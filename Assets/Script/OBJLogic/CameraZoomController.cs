using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    /// <summary>
    /// Menambahkan fitur zoom in/out kamera menggunakan scroll mouse pada Cinemachine ThirdPersonFollow.
    /// Tambahkan script ini pada GameObject yang sama dengan PlayerFollowCamera (root virtual camera).
    /// </summary>
    public class CameraZoomController : MonoBehaviour
    {
        [Header("Zoom Settings")]
        [Tooltip("Jarak kamera minimum (zoom in)")]
        public float MinDistance = 1.5f;

        [Tooltip("Jarak kamera maksimum (zoom out)")]
        public float MaxDistance = 10f;

        [Tooltip("Kecepatan zoom per scroll tick")]
        public float ZoomSpeed = 1.5f;

        [Tooltip("Kecepatan interpolasi menuju jarak target (0 = instan)")]
        public float ZoomSmoothTime = 8f;

        // Referensi ke komponen ThirdPersonFollow pada virtual camera anak
        private Cinemachine3rdPersonFollow _thirdPersonFollow;

        // Target jarak yang akan dicapai secara smooth
        private float _targetDistance;

        private void Awake()
        {
            var vcam = GetComponentInChildren<CinemachineVirtualCamera>();
            if (vcam != null)
                _thirdPersonFollow = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

            if (_thirdPersonFollow == null)
                Debug.LogWarning("[CameraZoomController] Cinemachine3rdPersonFollow tidak ditemukan.", this);
            else
                _targetDistance = _thirdPersonFollow.CameraDistance;
        }

        private void Update()
        {
            if (_thirdPersonFollow == null) return;

            ReadScrollInput();
            ApplyZoomSmooth();
        }

        /// <summary>
        /// Membaca nilai scroll mouse dan mengubah target jarak kamera.
        /// </summary>
        private void ReadScrollInput()
        {
            // Disable zoom while a monologue is playing.
            if (MonologueManager.Instance != null && MonologueManager.Instance.IsPlaying) return;

            if (Mouse.current == null) return;

            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) < 0.01f) return;

            // Scroll ke atas = zoom in (jarak mengecil), scroll ke bawah = zoom out (jarak membesar)
            float scrollDelta = -Mathf.Sign(scrollY) * ZoomSpeed;
            _targetDistance = Mathf.Clamp(_targetDistance + scrollDelta, MinDistance, MaxDistance);
        }

        /// <summary>
        /// Menginterpolasi CameraDistance menuju _targetDistance secara smooth.
        /// </summary>
        private void ApplyZoomSmooth()
        {
            _thirdPersonFollow.CameraDistance = Mathf.Lerp(
                _thirdPersonFollow.CameraDistance,
                _targetDistance,
                Time.deltaTime * ZoomSmoothTime
            );
        }
    }
}
