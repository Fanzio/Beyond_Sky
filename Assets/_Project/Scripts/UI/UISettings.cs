using UnityEngine;
using UnityEngine.UI;

public class UISettings : MonoBehaviour
{
    public static event System.Action<float> MusicVolumeChanged;
    public static event System.Action<float> SfxVolumeChanged;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle vibrationToggle;

    private void OnEnable()
    {
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 1f));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", 1f));
        vibrationToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("VibrationEnabled", 1) == 1);
    }

    public void SetMusic(float value)
    {
        float volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("MusicVolume", volume);
        if (MusicVolumeChanged != null)
        {
            MusicVolumeChanged.Invoke(volume);
        }
    }

    public void SetSfx(float value)
    {
        float volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("SfxVolume", volume);
        if (SfxVolumeChanged != null)
        {
            SfxVolumeChanged.Invoke(volume);
        }
    }

    public void SetVibration(bool value)
    {
        int enabledValue = 0;
        if (value)
        {
            enabledValue = 1;
        }
        PlayerPrefs.SetInt("VibrationEnabled", enabledValue);
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            PlayerPrefs.Save();
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
