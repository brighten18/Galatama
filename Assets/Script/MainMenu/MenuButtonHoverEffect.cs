using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GALATAMA.MainMenu
{
    /// <summary>
    /// Adds a hover highlight (color tint) and zoom-in effect to a UI Button image
    /// when the pointer enters or exits the element.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class MenuButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Audio")]
        [Tooltip("Audio source yang dipakai untuk memutar SFX hover.")]
        [SerializeField] private AudioSource hoverAudioSource;
        [Tooltip("Clip yang diputar saat cursor masuk ke tombol.")]
        [SerializeField] private AudioClip hoverSfx;
        [Tooltip("Volume SFX hover.")]
        [SerializeField] [Range(0f, 1f)] private float hoverSfxVolume = 1f;

        [Header("Zoom Settings")]
        [Tooltip("Scale multiplier applied on hover.")]
        [SerializeField] private float hoveredScale = 1.12f;
        [Tooltip("Duration in seconds for the scale transition.")]
        [SerializeField] private float scaleDuration = 0.18f;

        [Header("Highlight Settings")]
        [Tooltip("Color overlay applied to the image on hover.")]
        [SerializeField] private Color highlightColor = new Color(0.65f, 0.95f, 1f, 1f);
        [Tooltip("Duration in seconds for the color transition.")]
        [SerializeField] private float colorDuration = 0.12f;

        private static readonly Color NormalColor = Color.white;

        private RectTransform rectTransform;
        private Image targetImage;
        private Button targetButton;
        private Vector3 originalScale;
        private Coroutine scaleCoroutine;
        private Coroutine colorCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            targetImage = GetComponent<Image>();
            targetButton = GetComponent<Button>();
            originalScale = rectTransform.localScale;

            if (hoverAudioSource == null)
                hoverAudioSource = GetComponent<AudioSource>();
        }

        /// <summary>Called when the pointer enters the element.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (targetButton != null && !targetButton.interactable)
                return;

            AnimateScale(originalScale * hoveredScale, scaleDuration);
            AnimateColor(highlightColor, colorDuration);
            PlayHoverSfx();
        }

        /// <summary>Called when the pointer exits the element.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateScale(originalScale, scaleDuration);
            AnimateColor(NormalColor, colorDuration);
        }

        private void PlayHoverSfx()
        {
            if (hoverAudioSource == null || hoverSfx == null)
                return;

            hoverAudioSource.PlayOneShot(hoverSfx, Mathf.Clamp01(hoverSfxVolume));
        }

        private void AnimateScale(Vector3 targetScale, float duration)
        {
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(LerpScale(targetScale, duration));
        }

        private void AnimateColor(Color targetColor, float duration)
        {
            if (colorCoroutine != null)
                StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(LerpColor(targetColor, duration));
        }

        private IEnumerator LerpScale(Vector3 targetScale, float duration)
        {
            Vector3 startScale = rectTransform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                yield return null;
            }

            rectTransform.localScale = targetScale;
            scaleCoroutine = null;
        }

        private IEnumerator LerpColor(Color targetColor, float duration)
        {
            Color startColor = targetImage.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                targetImage.color = Color.LerpUnclamped(startColor, targetColor, t);
                yield return null;
            }

            targetImage.color = targetColor;
            colorCoroutine = null;
        }

        private void OnDisable()
        {
            // Reset saat panel disembunyikan
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            rectTransform.localScale = originalScale;
            targetImage.color = NormalColor;
        }
    }
}
