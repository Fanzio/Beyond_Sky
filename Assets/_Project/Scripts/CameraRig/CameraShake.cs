using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraShake : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float maximumOffset = 0.3f;
    [SerializeField] private float maximumAngle = 1f;
    [SerializeField] private float traumaDecay = 1.6f;
    [SerializeField] private float frequency = 22f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float trauma;
    private float noiseTime;
    private bool initialized;
    private bool applicationPaused;

    private void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        initialized = true;
    }

    public void AddTrauma(float amount)
    {
        if (isActiveAndEnabled == false || applicationPaused)
        {
            return;
        }
        trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, amount));
    }

    private void LateUpdate()
    {
        if (applicationPaused || (Time.timeScale == 0f && gameManager.IsGameOver == false))
        {
            trauma = 0f;
        }
        if (trauma <= 0f)
        {
            RestoreCamera();
            return;
        }
        noiseTime = noiseTime + Time.unscaledDeltaTime * frequency;
        float shake = trauma * trauma;
        float x = Mathf.PerlinNoise(11.7f, noiseTime) * 2f - 1f;
        float y = Mathf.PerlinNoise(38.2f, noiseTime) * 2f - 1f;
        float angle = Mathf.PerlinNoise(74.6f, noiseTime) * 2f - 1f;
        transform.localPosition = basePosition + new Vector3(x, y, 0f) * maximumOffset * shake;
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle * maximumAngle * shake);
        trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
        if (trauma == 0f)
        {
            RestoreCamera();
        }
    }

    private void RestoreCamera()
    {
        if (initialized)
        {
            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
    }

    private void OnDisable()
    {
        trauma = 0f;
        RestoreCamera();
    }
}
