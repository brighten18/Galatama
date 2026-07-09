using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Memutar beberapa ambient SFX secara bersamaan saat player memasuki area trigger.
/// Setiap clip looping secara independen. Fade in saat masuk, fade out dan stop saat keluar.
/// Opsional: volume semakin besar saat player mendekati pusat area trigger.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class AmbientSfxZone : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [Header("Audio")]
    [SerializeField] private List<AudioClip> ambientClips = new List<AudioClip>();
    [SerializeField] private AudioMixerGroup outputMixerGroup;
    [Range(0f, 1f)]
    [SerializeField] private float targetVolume = 0.6f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 2f;

    [Header("Distance Falloff")]
    [Tooltip("Aktifkan agar volume semakin besar saat player mendekati pusat area.")]
    [SerializeField] private bool useDistanceFalloff = false;
    [Range(0f, 1f)]
    [Tooltip("Volume minimum saat player berada di tepi area trigger.")]
    [SerializeField] private float minVolumeAtEdge = 0.1f;

    private readonly List<AudioSource> _audioSources = new List<AudioSource>();
    private Coroutine _fadeCoroutine;
    private int _playerCount;
    private float _masterVolume;
    private BoxCollider _boxCollider;
    private Transform _playerTransform;

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _boxCollider.isTrigger = true;

        foreach (AudioClip clip in ambientClips)
        {
            if (clip == null) continue;

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;

            if (outputMixerGroup != null)
                source.outputAudioMixerGroup = outputMixerGroup;

            _audioSources.Add(source);
        }
    }

    private void Update()
    {
        if (_playerCount <= 0 || !useDistanceFalloff || _playerTransform == null)
            return;

        float distanceVolume = CalculateDistanceVolume(_playerTransform.position);
        float finalVolume = _masterVolume * distanceVolume;

        foreach (AudioSource source in _audioSources)
            source.volume = finalVolume;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PlayerTag)) return;

        _playerCount++;

        if (_playerCount == 1)
        {
            _playerTransform = other.transform;

            foreach (AudioSource source in _audioSources)
            {
                if (!source.isPlaying)
                    source.Play();
            }

            StartFade(1f, fadeInDuration, stopOnComplete: false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(PlayerTag)) return;

        _playerCount = Mathf.Max(0, _playerCount - 1);

        if (_playerCount == 0)
        {
            _playerTransform = null;
            StartFade(0f, fadeOutDuration, stopOnComplete: true);
        }
    }

    /// <summary>
    /// Menghitung volume berdasarkan jarak player dari pusat BoxCollider.
    /// Mengembalikan targetVolume di pusat dan minVolumeAtEdge di tepi.
    /// </summary>
    private float CalculateDistanceVolume(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition) - _boxCollider.center;
        Vector3 halfSize = _boxCollider.size * 0.5f;

        float nx = halfSize.x > 0f ? Mathf.Abs(localPos.x) / halfSize.x : 0f;
        float nz = halfSize.z > 0f ? Mathf.Abs(localPos.z) / halfSize.z : 0f;

        float normalizedDist = Mathf.Clamp01(Mathf.Max(nx, nz));

        return Mathf.Lerp(targetVolume, minVolumeAtEdge, normalizedDist);
    }

    private void StartFade(float targetMaster, float duration, bool stopOnComplete)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(targetMaster, duration, stopOnComplete));
    }

    private IEnumerator FadeRoutine(float targetMaster, float duration, bool stopOnComplete)
    {
        float startMaster = _masterVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _masterVolume = Mathf.Lerp(startMaster, targetMaster, elapsed / duration);

            if (!useDistanceFalloff)
            {
                float vol = _masterVolume * targetVolume;
                foreach (AudioSource source in _audioSources)
                    source.volume = vol;
            }

            yield return null;
        }

        _masterVolume = targetMaster;

        if (stopOnComplete)
        {
            foreach (AudioSource source in _audioSources)
                source.Stop();
        }
    }
}
