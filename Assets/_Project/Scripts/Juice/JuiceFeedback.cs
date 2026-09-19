using UnityEngine;

public class JuiceFeedback : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform player;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private CloseCallFlash closeCallFlash;
    [SerializeField] private PlayerDeathBurst deathBurst;

    [Header("Trauma per event")]
    [SerializeField] private float deathTrauma = 0.9f;
    [SerializeField] private float explosionTrauma = 0.35f;
    [SerializeField] private float shieldTrauma = 0.5f;
    [SerializeField] private float bossDefeatedTrauma = 0.7f;
    [SerializeField] private float bossHitTrauma = 0.15f;
    [SerializeField] private float obstacleTrauma = 0.12f;
    [SerializeField] private float shotTrauma = 0.05f;
    [SerializeField] private float closeCallTrauma = 0.1f;

    private bool deathHandled;
    private bool applicationPaused;

    private void OnEnable()
    {
        AudioSignals.SoundRequested += OnSoundRequested;
        AudioSignals.CloseCallRequested += OnCloseCall;
    }

    private void LateUpdate()
    {
        if (applicationPaused || deathHandled || gameManager.IsGameOver == false)
        {
            return;
        }
        deathHandled = true;
        cameraShake.AddTrauma(deathTrauma);
        deathBurst.PlayAt(player.position);
    }

    private void OnSoundRequested(GameSound sound)
    {
        if (applicationPaused || Time.timeScale == 0f || gameManager.IsGameOver)
        {
            return;
        }
        if (sound == GameSound.PlayerShot)
        {
            cameraShake.AddTrauma(shotTrauma);
        }
        else if (sound == GameSound.ShieldBreak)
        {
            cameraShake.AddTrauma(shieldTrauma);
        }
        else if (sound == GameSound.Explosion)
        {
            cameraShake.AddTrauma(explosionTrauma);
        }
        else if (sound == GameSound.BossDefeated)
        {
            cameraShake.AddTrauma(bossDefeatedTrauma);
        }
        else if (sound == GameSound.BossHit)
        {
            cameraShake.AddTrauma(bossHitTrauma);
        }
        else if (sound == GameSound.ObstacleDestroyed)
        {
            cameraShake.AddTrauma(obstacleTrauma);
        }
    }

    private void OnCloseCall(int progress, int required)
    {
        if (applicationPaused || Time.timeScale == 0f || gameManager.IsGameOver)
        {
            return;
        }
        cameraShake.AddTrauma(closeCallTrauma);
        closeCallFlash.Play();
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
    }

    private void OnDisable()
    {
        AudioSignals.SoundRequested -= OnSoundRequested;
        AudioSignals.CloseCallRequested -= OnCloseCall;
    }
}
