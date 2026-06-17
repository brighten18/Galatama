using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace GALATAMA.Cutscene
{
    /// <summary>
    /// Controls the intro cutscene sequence with a typewriter text effect and fade transitions.
    /// Panels are defined via the Inspector. First "Next" click completes the typewriter;
    /// second click advances to the next panel (or loads gameplay after the last panel).
    /// </summary>
    public class CutsceneController : MonoBehaviour
    {
        [System.Serializable]
        public struct PanelData
        {
            [TextArea(2, 5)]
            public string body;
        }

        [Header("UI References")]
        [SerializeField] private RawImage backgroundImage;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text panelIndicatorText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Image fadeOverlay;

        [Header("Panel UI Root")]
        [SerializeField] private GameObject panelUI;

        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoRawImage;
        [SerializeField] private GameObject videoCanvas;

        [Header("Panels")]
        [SerializeField] private PanelData[] panels;

        [Header("Settings")]
        [SerializeField] private string gameplaySceneName = "Galatama";
        [SerializeField] private float typewriterSpeed = 0.04f;
        [SerializeField] private float fadeDuration = 0.5f;

        private int _currentIndex;
        private bool _isTyping;
        private Coroutine _typewriterCoroutine;

        private void Start()
        {
            nextButton.onClick.AddListener(OnNextClicked);
            skipButton.onClick.AddListener(OnSkipClicked);

            // Pre-prepare video di background selagi panel berjalan,
            // agar tidak ada jeda saat transisi ke video.
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();

            StartCoroutine(PlayIntro());
        }

        private void OnDestroy()
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }

        /// <summary>
        /// Dipanggil saat VideoPlayer selesai di-prepare.
        /// Membuat RenderTexture sesuai dimensi video dan langsung menghubungkannya.
        /// </summary>
        private void OnVideoPrepared(VideoPlayer vp)
        {
            vp.prepareCompleted -= OnVideoPrepared;
            RenderTexture rt = new RenderTexture((int)vp.width, (int)vp.height, 0);
            vp.targetTexture = rt;
            if (videoRawImage != null) videoRawImage.texture = rt;
        }

        private IEnumerator PlayIntro()
        {
            if (fadeOverlay != null)
                yield return StartCoroutine(Fade(1f, 0f));
            ShowPanel(0);
        }

        private void ShowPanel(int index)
        {
            _currentIndex = index;
            if (bodyText != null) bodyText.text = string.Empty;
            UpdateIndicator();
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            if (panels != null && panels.Length > index)
                _typewriterCoroutine = StartCoroutine(TypewriterEffect(panels[index].body));
        }

        private void UpdateIndicator()
        {
            if (panelIndicatorText != null && panels != null)
                panelIndicatorText.text = $"{_currentIndex + 1} / {panels.Length}";
        }

        private IEnumerator TypewriterEffect(string fullText)
        {
            _isTyping = true;
            if (bodyText != null) bodyText.text = string.Empty;
            foreach (char c in fullText)
            {
                if (bodyText != null) bodyText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
            _isTyping = false;
        }

        private void OnNextClicked()
        {
            if (_isTyping)
            {
                // First click: instantly complete current panel text.
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                _isTyping = false;
                if (bodyText != null && panels != null && panels.Length > _currentIndex)
                    bodyText.text = panels[_currentIndex].body;
                return;
            }

            int nextIndex = _currentIndex + 1;
            if (panels != null && nextIndex < panels.Length)
                StartCoroutine(TransitionToPanel(nextIndex));
            else
                StartCoroutine(LoadGameplay());
        }

        private void OnSkipClicked() => StartCoroutine(LoadGameplay());

        private IEnumerator TransitionToPanel(int nextIndex)
        {
            SetButtonsInteractable(false);
            if (fadeOverlay != null) yield return StartCoroutine(Fade(0f, 1f));
            ShowPanel(nextIndex);
            if (fadeOverlay != null) yield return StartCoroutine(Fade(1f, 0f));
            SetButtonsInteractable(true);
        }

        private IEnumerator LoadGameplay()
        {
            SetButtonsInteractable(false);

            // Fade out panel UI
            if (fadeOverlay != null) yield return StartCoroutine(Fade(0f, 1f));

            // Sembunyikan panel UI, tampilkan video canvas
            if (panelUI != null) panelUI.SetActive(false);
            if (videoCanvas != null) videoCanvas.SetActive(true);

            // Fallback: tunggu hanya jika prepare belum selesai (seharusnya sudah selesai dari Start)
            if (!videoPlayer.isPrepared)
                yield return new WaitUntil(() => videoPlayer.isPrepared);

            // Mulai video sebelum fade-in agar frame pertama sudah terrender ke RenderTexture
            // saat overlay menghilang — mencegah environment 3D bocor lewat RT yang masih kosong.
            videoPlayer.Play();
            yield return null; // tunggu satu frame agar frame pertama video masuk ke RT

            // Fade in video
            if (fadeOverlay != null) yield return StartCoroutine(Fade(1f, 0f));

            // Tunggu sampai video selesai — tidak bisa di-skip
            yield return new WaitUntil(() => !videoPlayer.isPlaying);

            // Fade out lalu load gameplay
            if (fadeOverlay != null) yield return StartCoroutine(Fade(0f, 1f));
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void SetButtonsInteractable(bool state)
        {
            if (nextButton != null) nextButton.interactable = state;
            if (skipButton != null) skipButton.interactable = state;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeOverlay == null) yield break;
            float elapsed = 0f;
            Color c = fadeOverlay.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
                fadeOverlay.color = c;
                yield return null;
            }
            c.a = to;
            fadeOverlay.color = c;
        }
    }
}
