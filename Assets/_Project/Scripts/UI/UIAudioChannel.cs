// Applica il volume della categoria e l’attenuazione alla sorgente audio.
using UnityEngine;

namespace BeyondSky.UI
{
    [RequireComponent(typeof(AudioSource))]
    public class UIAudioChannel : MonoBehaviour
    {
        public enum Category
        {
            Music,
            Sfx
        }

        [SerializeField] private Category category;
        [Range(0f, 1f)]
        [SerializeField] private float baseVolume = 1f;
        private AudioSource source;
        private float settingsVolume = 1f;
        private float playbackGain = 1f;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
            UISettings.MusicVolumeChanged += OnMusicVolumeChanged;
            UISettings.SfxVolumeChanged += OnSfxVolumeChanged;
            string key = "SfxVolume";
            if (category == Category.Music)
            {
                key = "MusicVolume";
            }
            ApplyVolume(PlayerPrefs.GetFloat(key, 1f));
        }

        private void OnDisable()
        {
            UISettings.MusicVolumeChanged -= OnMusicVolumeChanged;
            UISettings.SfxVolumeChanged -= OnSfxVolumeChanged;
        }

        private void OnMusicVolumeChanged(float volume)
        {
            if (category == Category.Music)
            {
                ApplyVolume(volume);
            }
        }

        private void OnSfxVolumeChanged(float volume)
        {
            if (category == Category.Sfx)
            {
                ApplyVolume(volume);
            }
        }

        public void SetPlaybackGain(float gain)
        {
            if (playbackGain == gain)
            {
                return;
            }
            playbackGain = gain;
            ApplyVolume(settingsVolume);
        }

        private void ApplyVolume(float volume)
        {
            settingsVolume = volume;
            source.volume = baseVolume * settingsVolume * playbackGain;
        }
    }
}
