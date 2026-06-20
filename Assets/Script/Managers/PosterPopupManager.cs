using System;
using UnityEngine;
using UnityEngine.UI;

public class PosterPopupManager : MonoBehaviour
{
    public static PosterPopupManager Instance { get; private set; }

    [Header("Popup References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private RectTransform posterImageRect;
    [SerializeField] private Image posterImage;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip openPosterSfx;
    [SerializeField] private float sfxVolume = 1f;

    private bool isOpen;

    public bool IsOpen => isOpen;

    /// <summary>Fired whenever the poster popup is closed.</summary>
    public event Action OnPosterClosed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void OpenPoster(PosterData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[PosterPopup] PosterData null.");
            return;
        }

        if (posterImage == null || popupRoot == null)
        {
            Debug.LogError("[PosterPopup] UI reference belum lengkap.");
            return;
        }

        posterImage.sprite = data.PosterSprite;
        posterImage.preserveAspect = true;

        if (posterImageRect != null)
        {
            posterImageRect.anchoredPosition = data.AnchoredPosition;
            posterImageRect.localScale = data.LocalScale;
            posterImageRect.sizeDelta = data.SizeDelta;
        }

        popupRoot.SetActive(true);
        isOpen = true;

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorAndLook(false, false);
            PlayerInputManager.Instance.SetPlayerMovement(false);
            PlayerInputManager.Instance.ResetInteractInput();
        }

        Cursor.visible = true;
        PlayOpenSfx();
    }

    public void ClosePoster()
    {
        if (!isOpen)
            return;

        if (popupRoot != null)
            popupRoot.SetActive(false);

        isOpen = false;

        OnPosterClosed?.Invoke();

        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.SetCursorAndLook(true, true);
            PlayerInputManager.Instance.SetPlayerMovement(true);
            PlayerInputManager.Instance.ResetInteractInput();
        }

        Cursor.visible = false;
    }

    public void OnCloseButtonPressed()
    {
        ClosePoster();
    }

    private void PlayOpenSfx()
    {
        if (sfxSource == null || openPosterSfx == null)
            return;

        sfxSource.PlayOneShot(openPosterSfx, Mathf.Clamp01(sfxVolume));
    }
}
