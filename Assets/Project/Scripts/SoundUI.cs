using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts
{
    /// <summary>
    /// Glue layer that connects the audio settings UI controls to <see cref="SoundManager"/>.
    /// Populates the track dropdown from <see cref="SoundManager.TrackNames"/> and registers
    /// UI callbacks to forward slider and dropdown changes in real time.
    /// </summary>
    public class SoundUI : MonoBehaviour
    {
        /// <summary>The SoundManager to control.</summary>
        [SerializeField] private SoundManager soundManager;

        /// <summary>Slider that controls music volume [0, 1].</summary>
        [SerializeField] private Slider musicVolumeSlider;

        /// <summary>Slider that controls SFX volume [0, 1].</summary>
        [SerializeField] private Slider sfxVolumeSlider;

        /// <summary>Dropdown for selecting the active music track.</summary>
        [SerializeField] private TMP_Dropdown musicTrackDropdown;

        private void Start()
        {
            if (!soundManager)
                return;

            // Set slider values to match current SoundManager state
            if (musicVolumeSlider)
                musicVolumeSlider.value = soundManager.MusicVolume;

            if (sfxVolumeSlider)
                sfxVolumeSlider.value = soundManager.SfxVolume;

            // Populate track dropdown
            if (musicTrackDropdown)
            {
                musicTrackDropdown.ClearOptions();
                string[] names = soundManager.TrackNames;

                if (names != null && names.Length > 0)
                {
                    System.Collections.Generic.List<string> options = new(names);
                    musicTrackDropdown.AddOptions(options);
                    musicTrackDropdown.value = soundManager.CurrentTrackIndex;
                    musicTrackDropdown.RefreshShownValue();
                }
            }
        }

        private void OnEnable()
        {
            if (musicVolumeSlider)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            if (musicTrackDropdown)
                musicTrackDropdown.onValueChanged.AddListener(OnTrackChanged);
        }

        private void OnDisable()
        {
            if (musicVolumeSlider)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

            if (musicTrackDropdown)
                musicTrackDropdown.onValueChanged.RemoveListener(OnTrackChanged);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (soundManager)
                soundManager.SetMusicVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (soundManager)
                soundManager.SetSfxVolume(value);
        }

        private void OnTrackChanged(int index)
        {
            if (soundManager)
                soundManager.SetMusicTrack(index);
        }
    }
}
