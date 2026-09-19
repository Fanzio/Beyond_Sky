using UnityEngine;

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

    private void ApplyVolume(float volume)
    {
        source.volume = baseVolume * volume;
    }
}
