using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GALATAMA.MainMenu
{
    public class SettingsRuntimeApplier : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private string musicVolumeParameter = "MusicVolume";
        [SerializeField] private string sfxVolumeParameter = "SfxVolume";

        [Header("Startup")]
        [SerializeField] private bool applyOnAwake = true;
        
        [Header("Debug Overlay")]
        [SerializeField] private bool showDebugOverlay = true;
        [SerializeField] private KeyCode toggleDebugKey = KeyCode.F8;
        [SerializeField] private int debugFontSize = 14;
        [SerializeField] private Vector2 debugBoxOffset = new Vector2(12f, 12f);
        [SerializeField] private Vector2 debugBoxSize = new Vector2(430f, 130f);

        private const string PrefQuality = "settings.quality";
        private const string PrefResolutionIndex = "settings.resolutionIndex";
        private const string PrefFullscreen = "settings.fullscreen";
        private const string PrefMusicVolume = "settings.musicVolume";
        private const string PrefSfxVolume = "settings.sfxVolume";

        private static SettingsRuntimeApplier instance;
        private readonly List<Resolution> supported169Resolutions = new List<Resolution>();
        private readonly StringBuilder debugBuilder = new StringBuilder(256);
        private int lastAppliedQualityIndex = 2;
        private bool lastAppliedFullscreen = true;
        private int lastAppliedWidth = 1920;
        private int lastAppliedHeight = 1080;
        private bool lastAppliedLodEnabled = true;
        private int lastAppliedLodGroupCount = 0;
        private GUIStyle debugStyle;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            BuildResolutionList16by9();
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (applyOnAwake)
            {
                ApplyAllSettings();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAllSettings();
        }
        
        private void Update()
        {
            if (IsToggleDebugPressed())
            {
                showDebugOverlay = !showDebugOverlay;
            }
        }

        [ContextMenu("Apply All Settings Now")]
        public void ApplyAllSettings()
        {
            ApplyQuality();
            ApplyResolutionAndFullscreen();
            ApplyAudio();
            ApplyLodEnabled();
        }

        private void ApplyQuality()
        {
            int qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefQuality, 2), 0, 2);
            QualitySettings.SetQualityLevel(qualityIndex, true);
            lastAppliedQualityIndex = qualityIndex;
        }

        private void ApplyResolutionAndFullscreen()
        {
            if (supported169Resolutions.Count == 0)
            {
                BuildResolutionList16by9();
            }

            if (supported169Resolutions.Count == 0)
            {
                return;
            }

            int fallbackIndex = FindResolutionIndex(1920, 1080);
            int index = PlayerPrefs.GetInt(PrefResolutionIndex, fallbackIndex);
            index = Mathf.Clamp(index, 0, supported169Resolutions.Count - 1);
            bool fullscreen = PlayerPrefs.GetInt(PrefFullscreen, 1) == 1;

            Resolution selected = supported169Resolutions[index];
            Screen.SetResolution(selected.width, selected.height, fullscreen);
            lastAppliedWidth = selected.width;
            lastAppliedHeight = selected.height;
            lastAppliedFullscreen = fullscreen;
        }

        private void ApplyAudio()
        {
            float music = PlayerPrefs.GetFloat(PrefMusicVolume, 0.8f);
            float sfx = PlayerPrefs.GetFloat(PrefSfxVolume, 0.8f);

            SetMixerVolume(musicVolumeParameter, music);
            SetMixerVolume(sfxVolumeParameter, sfx);
        }

        private void SetMixerVolume(string parameterName, float value)
        {
            if (masterMixer == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            float linear = Mathf.Clamp(value, 0.0001f, 1f);
            float db = Mathf.Log10(linear) * 20f;
            masterMixer.SetFloat(parameterName, db);
        }

        private void ApplyLodEnabled()
        {
            bool enabled = LodSettingsUtility.GetSavedLodEnabled();
            int appliedCount = LodSettingsUtility.ApplyLodModeToAllGroups(enabled);
            
            lastAppliedLodEnabled = enabled;
            lastAppliedLodGroupCount = appliedCount;
        }

        private void BuildResolutionList16by9()
        {
            supported169Resolutions.Clear();

            Resolution[] all = Screen.resolutions;
            for (int i = 0; i < all.Length; i++)
            {
                Resolution r = all[i];
                if (r.width * 9 != r.height * 16)
                {
                    continue;
                }

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

        private void OnGUI()
        {
            if (!showDebugOverlay)
            {
                return;
            }

            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.box);
                debugStyle.alignment = TextAnchor.UpperLeft;
                debugStyle.fontSize = Mathf.Max(10, debugFontSize);
                debugStyle.richText = false;
            }

            string qualityName = "Unknown";
            string[] names = QualitySettings.names;
            if (lastAppliedQualityIndex >= 0 && lastAppliedQualityIndex < names.Length)
            {
                qualityName = names[lastAppliedQualityIndex];
            }

            debugBuilder.Length = 0;
            debugBuilder.AppendLine("Runtime Settings Debug");
            debugBuilder.Append("LOD: ").Append(lastAppliedLodEnabled ? "ON" : "OFF")
                .Append(" | LOD Targets Applied: ").Append(lastAppliedLodGroupCount).AppendLine();
            debugBuilder.Append("Quality: ").Append(qualityName)
                .Append(" (index ").Append(lastAppliedQualityIndex).Append(')').AppendLine();
            debugBuilder.Append("Resolution: ").Append(lastAppliedWidth).Append("x").Append(lastAppliedHeight)
                .Append(" | Mode: ").Append(lastAppliedFullscreen ? "Fullscreen" : "Windowed").AppendLine();
            debugBuilder.Append("Current Scene: ").Append(SceneManager.GetActiveScene().name).AppendLine();
            debugBuilder.Append("Toggle Overlay: ").Append(toggleDebugKey);

            Rect boxRect = new Rect(debugBoxOffset.x, debugBoxOffset.y, debugBoxSize.x, debugBoxSize.y);
            GUI.Box(boxRect, debugBuilder.ToString(), debugStyle);
        }

        private bool IsToggleDebugPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            Key mappedKey = ConvertLegacyKeyCodeToInputSystemKey(toggleDebugKey);
            if (mappedKey == Key.None)
            {
                return false;
            }

            return Keyboard.current[mappedKey].wasPressedThisFrame;
#else
            return Input.GetKeyDown(toggleDebugKey);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static Key ConvertLegacyKeyCodeToInputSystemKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.F1: return Key.F1;
                case KeyCode.F2: return Key.F2;
                case KeyCode.F3: return Key.F3;
                case KeyCode.F4: return Key.F4;
                case KeyCode.F5: return Key.F5;
                case KeyCode.F6: return Key.F6;
                case KeyCode.F7: return Key.F7;
                case KeyCode.F8: return Key.F8;
                case KeyCode.F9: return Key.F9;
                case KeyCode.F10: return Key.F10;
                case KeyCode.F11: return Key.F11;
                case KeyCode.F12: return Key.F12;
                default: return Key.None;
            }
        }
#endif
    }
}
