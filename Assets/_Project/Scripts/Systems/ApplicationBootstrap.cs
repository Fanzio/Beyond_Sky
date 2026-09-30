// Imposta il limite di fotogrammi e le impostazioni iniziali di esecuzione.
using UnityEngine;

namespace BeyondSky.Systems
{
    public class ApplicationBootstrap : MonoBehaviour
    {
        [Header("Performance")]
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }
}
