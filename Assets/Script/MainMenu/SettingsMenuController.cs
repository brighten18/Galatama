using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace GALATAMA.MainMenu
{
    public class SettingsMenuController : MonoBehaviour
    {
        [Header("Quality (Radio/Toggles)")]
        [SerializeField] private Toggle lowToggle;
        [SerializeField] private Toggle mediumToggle;
        [SerializeField] private Toggle highToggle;

        [Header("Resolution")]
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("LOD")]
        [SerializeField] private Toggle lodToggle;

        [Header("Audio")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private string musicVolumeParameter = "MusicVolume";
        [SerializeField] private string sfxVolumeParameter = "SfxVolume";
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        private const string PrefQuality = "settings.quality";
        private const string PrefResolutionIndex = "settings.resolutionIndex";
        private const string PrefFullscreen = "settings.fullscreen";
        private const string PrefLodEnabled = "settings.lodEnabled";
        private const string PrefMusicVolume = "settings.musicVolume";
        private const string PrefSfxVolume = "settings.sfxVolume";

        private readonly List<Resolution> supported169Resolutions = new List<Resolution>();

        private void Awake()
        {
            BuildResolutionList16by9();
            SetupResolutionDropdown();
            LoadAndApplySettings();
            RegisterListeners();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
        }

        private void RegisterListeners()
        {
            if (lowToggle != null) lowToggle.onValueChanged.AddListener(OnLowToggleChanged);
            if (mediumToggle != null) mediumToggle.onValueChanged.AddListener(OnMediumToggleChanged);
            if (highToggle != null) highToggle.onValueChanged.AddListener(OnHighToggleChanged);

            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (lodToggle != null) lodToggle.onValueChanged.AddListener(OnLodToggleChanged);

            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        private void UnregisterListeners()
        {
            if (lowToggle != null) lowToggle.onValueChanged.RemoveListener(OnLowToggleChanged);
            if (mediumToggle != null) mediumToggle.onValueChanged.RemoveListener(OnMediumToggleChanged);
            if (highToggle != null) highToggle.onValueChanged.RemoveListener(OnHighToggleChanged);

            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            if (lodToggle != null) lodToggle.onValueChanged.RemoveListener(OnLodToggleChanged);

            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        private void BuildResolutionList16by9()
        {
            supported169Resolutions.Clear();

            Resolution[] all = Screen.resolutions;
            for (int i = 0; i < all.Length; i++)
            {
                Resolution r = all[i];
                if (r.width * 9 == r.height * 16)
                {
                    bool exists = false;
                    for (int j = 0; j < supported169Resolutions.Count; j++)
                    {
                        Resolution existing = supported169Resolutions[j];
                        if (existing.width == r.width && existing.height == r.height)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        supported169Resolutions.Add(r);
                    }
                }
            }

            supported169Resolutions.Sort((a, b) =>
            {
                int compareWidth = a.width.CompareTo(b.width);
                if (compareWidth != 0) return compareWidth;
                return a.height.CompareTo(b.height);
            });

            if (supported169Resolutions.Count == 0)
            {
                supported169Resolutions.Add(new Resolution { width = 1920, height = 1080 });
            }
        }

        private void SetupResolutionDropdown()
        {
            if (resolutionDropdown == null)
            {
                return;
            }

            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            for (int i = 0; i < supported169Resolutions.Count; i++)
            {
                Resolution r = supported169Resolutions[i];
                options.Add(r.width + " x " + r.height);
            }

            resolutionDropdown.AddOptions(options);
        }

        private void LoadAndApplySettings()
        {
            int qualityIndex = PlayerPrefs.GetInt(PrefQuality, 2);
            ApplyQuality(qualityIndex, true);

            int defaultResolutionIndex = FindResolutionIndex(1920, 1080);
            int resolutionIndex = PlayerPrefs.GetInt(PrefResolutionIndex, defaultResolutionIndex);
            resolutionIndex = Mathf.Clamp(resolutionIndex, 0, supported169Resolutions.Count - 1);
            ApplyResolution(resolutionIndex, PlayerPrefs.GetInt(PrefFullscreen, 1) == 1, true);

            bool lodEnabled = PlayerPrefs.GetInt(PrefLodEnabled, 1) == 1;
            ApplyLodEnabled(lodEnabled, true);

            float music = PlayerPrefs.GetFloat(PrefMusicVolume, 0.8f);
            float sfx = PlayerPrefs.GetFloat(PrefSfxVolume, 0.8f);

            if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);

            ApplyMusicVolume(music, true);
            ApplySfxVolume(sfx, true);
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < supported169Resolutions.Count; i++)
            {
                if (supported169Resolutions[i].width == width && supported169Resolutions[i].height == height)
                {
                    return i;
                }
            }

            return supported169Resolutions.Count - 1;
        }

        private void ApplyQuality(int qualityIndex, bool silent)
        {
            qualityIndex = Mathf.Clamp(qualityIndex, 0, 2);
            QualitySettings.SetQualityLevel(qualityIndex, true);
            PlayerPrefs.SetInt(PrefQuality, qualityIndex);

            if (lowToggle != null)
            {
                if (silent) lowToggle.SetIsOnWithoutNotify(qualityIndex == 0);
                else lowToggle.isOn = qualityIndex == 0;
            }

            if (mediumToggle != null)
            {
                if (silent) mediumToggle.SetIsOnWithoutNotify(qualityIndex == 1);
                else mediumToggle.isOn = qualityIndex == 1;
            }

            if (highToggle != null)
            {
                if (silent) highToggle.SetIsOnWithoutNotify(qualityIndex == 2);
                else highToggle.isOn = qualityIndex == 2;
            }
        }

        private void ApplyResolution(int index, bool fullscreen, bool silent)
        {
            if (supported169Resolutions.Count == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, supported169Resolutions.Count - 1);
            Resolution selected = supported169Resolutions[index];
            Screen.SetResolution(selected.width, selected.height, fullscreen);

            PlayerPrefs.SetInt(PrefResolutionIndex, index);
            PlayerPrefs.SetInt(PrefFullscreen, fullscreen ? 1 : 0);

            if (resolutionDropdown != null)
            {
                if (silent) resolutionDropdown.SetValueWithoutNotify(index);
                else resolutionDropdown.value = index;
            }

            if (fullscreenToggle != null)
            {
                if (silent) fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
                else fullscreenToggle.isOn = fullscreen;
            }
        }

        private void ApplyLodEnabled(bool enabled, bool silent)
        {
            PlayerPrefs.SetInt(PrefLodEnabled, enabled ? 1 : 0);

            if (lodToggle != null)
            {
                if (silent) lodToggle.SetIsOnWithoutNotify(enabled);
                else lodToggle.isOn = enabled;
            }

            LodSettingsUtility.ApplyLodModeToAllGroups(enabled);
        }

        private void ApplyMusicVolume(float value, bool save)
        {
            float linear = Mathf.Clamp(value, 0.0001f, 1f);
            float db = Mathf.Log10(linear) * 20f;

            if (masterMixer != null)
            {
                masterMixer.SetFloat(musicVolumeParameter, db);
            }

            if (save)
            {
                PlayerPrefs.SetFloat(PrefMusicVolume, value);
            }
        }

        private void ApplySfxVolume(float value, bool save)
        {
            float linear = Mathf.Clamp(value, 0.0001f, 1f);
            float db = Mathf.Log10(linear) * 20f;

            if (masterMixer != null)
            {
                masterMixer.SetFloat(sfxVolumeParameter, db);
            }

            if (save)
            {
                PlayerPrefs.SetFloat(PrefSfxVolume, value);
            }
        }

        private void SavePrefs()
        {
            PlayerPrefs.Save();
        }

        private void OnLowToggleChanged(bool isOn)
        {
            if (!isOn) return;
            ApplyQuality(0, false);
            SavePrefs();
        }

        private void OnMediumToggleChanged(bool isOn)
        {
            if (!isOn) return;
            ApplyQuality(1, false);
            SavePrefs();
        }

        private void OnHighToggleChanged(bool isOn)
        {
            if (!isOn) return;
            ApplyQuality(2, false);
            SavePrefs();
        }

        private void OnResolutionChanged(int index)
        {
            bool fullscreen = fullscreenToggle == null || fullscreenToggle.isOn;
            ApplyResolution(index, fullscreen, false);
            SavePrefs();
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            int index = resolutionDropdown != null ? resolutionDropdown.value : FindResolutionIndex(1920, 1080);
            ApplyResolution(index, isFullscreen, false);
            SavePrefs();
        }

        private void OnLodToggleChanged(bool isOn)
        {
            ApplyLodEnabled(isOn, false);
            SavePrefs();
        }

        private void OnMusicVolumeChanged(float value)
        {
            ApplyMusicVolume(value, true);
            SavePrefs();
        }

        private void OnSfxVolumeChanged(float value)
        {
            ApplySfxVolume(value, true);
            SavePrefs();
        }

        public void OpenCredits()
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
            }
        }

        public void CloseCredits()
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
    }
}
