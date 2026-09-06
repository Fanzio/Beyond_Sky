using UnityEngine;

public class ApplicationBootstrap : MonoBehaviour
{
    [Header("Performance")]
    [SerializeField] private int targetFrameRate = 60;

    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}
