using System.Collections.Generic;
using UnityEngine;

public class GameAudio : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private AudioSource windSource;
    [SerializeField] private AudioSource laserSource;

    [Header("Effects")]
    [SerializeField] private AudioCue[] cues;
    [SerializeField] private AudioSource[] worldVoices;
    [SerializeField] private AudioSource[] interfaceVoices;

    [Header("Engine pitch")]
    [SerializeField] private float speedForMaxEnginePitch = 50f;

    [Header("Results")]
    [SerializeField] private float gameOverJingleDelay = 0.40f;

    private float[] lastPlayed;
    private List<Component> activeLasers = new List<Component>(8);
    private bool gameOverHandled;
    private bool background;
    private bool laserRunning;
    private float jingleTimer = -1f;

    private void Awake()
    {
        AudioListener.pause = false;
        lastPlayed = new float[cues.Length];
        for (int i = 0; i < lastPlayed.Length; i++)
        {
            lastPlayed[i] = -100f;
        }
        for (int i = 0; i < interfaceVoices.Length; i++)
        {
            interfaceVoices[i].ignoreListenerPause = true;
        }
    }

    private void OnEnable()
    {
        AudioSignals.SoundRequested += PlaySound;
        AudioSignals.CloseCallRequested += PlayCloseCall;
        AudioSignals.LaserChanged += SetLaser;
    }

    private void Start()
    {
        if (musicSource.clip != null)
        {
            musicSource.Play();
        }
        if (gameManager != null)
        {
            engineSource.Play();
            windSource.Play();
        }
    }

    private void LateUpdate()
    {
        AudioListener.pause = background || Time.timeScale == 0f;
        if (gameManager != null && gameManager.IsGameOver && gameOverHandled == false)
        {
            gameOverHandled = true;
            StopWorld();
            PlaySound(GameSound.PlayerCrash);
            jingleTimer = gameOverJingleDelay;
        }
        if (background)
        {
            return;
        }
        if (jingleTimer >= 0f)
        {
            jingleTimer = jingleTimer - Time.unscaledDeltaTime;
            if (jingleTimer <= 0f)
            {
                jingleTimer = -1f;
                PlaySound(GameSound.GameOverJingle);
            }
        }
        if (gameOverHandled || Time.timeScale == 0f)
        {
            return;
        }
        UpdateLaserLoop();
        if (gameManager != null && speedManager != null)
        {
            float progress = Mathf.Clamp01(speedManager.CurrentSpeed / Mathf.Max(1f, speedForMaxEnginePitch));
            engineSource.pitch = Mathf.Lerp(0.85f, 1.4f, progress);
        }
    }

    private bool IsInterfaceSound(GameSound sound)
    {
        return sound == GameSound.UIClick || sound == GameSound.UIPanelOpen ||
            sound == GameSound.PlayerCrash || sound == GameSound.GameOverJingle ||
            sound == GameSound.SpaceReached;
    }

    private void PlaySound(GameSound sound)
    {
        PlayCue(sound, 0f);
    }

    private void PlayCloseCall(int progress, int required)
    {
        float fraction = 1f;
        if (required > 1)
        {
            fraction = Mathf.Clamp01((float)(progress - 1) / (required - 1));
        }
        float pitch = Mathf.Pow(2f, fraction * 4f / 12f);
        PlayCue(GameSound.CloseCall, pitch);
    }

    private void PlayCue(GameSound sound, float fixedPitch)
    {
        if (background)
        {
            return;
        }
        bool interfaceSound = IsInterfaceSound(sound);
        if (interfaceSound == false)
        {
            if (Time.timeScale == 0f || gameOverHandled)
            {
                return;
            }
            if (gameManager != null && gameManager.IsGameOver)
            {
                return;
            }
        }
        for (int i = 0; i < cues.Length; i++)
        {
            AudioCue cue = cues[i];
            if (cue.Sound != sound || cue.Clip == null)
            {
                continue;
            }
            if (Time.unscaledTime - lastPlayed[i] < cue.MinimumInterval)
            {
                return;
            }
            AudioSource[] bank = worldVoices;
            if (interfaceSound)
            {
                bank = interfaceVoices;
            }
            AudioSource voice = GetVoice(bank, cue.Priority);
            if (voice == null)
            {
                return;
            }
            lastPlayed[i] = Time.unscaledTime;
            voice.Stop();
            voice.priority = cue.Priority;
            voice.pitch = 1f;
            if (fixedPitch > 0f)
            {
                voice.pitch = fixedPitch;
            }
            else if (cue.VaryPitch)
            {
                voice.pitch = Random.Range(0.95f, 1.05f);
            }
            voice.PlayOneShot(cue.Clip, cue.Gain);
            return;
        }
    }

    private AudioSource GetVoice(AudioSource[] bank, int priority)
    {
        AudioSource candidate = null;
        for (int i = 0; i < bank.Length; i++)
        {
            if (bank[i].isPlaying == false)
            {
                return bank[i];
            }
            if (bank[i].priority >= priority)
            {
                if (candidate == null || bank[i].priority > candidate.priority)
                {
                    candidate = bank[i];
                }
            }
        }
        return candidate;
    }

    private void SetLaser(Component owner, bool active)
    {
        if (owner == null)
        {
            return;
        }
        if (active)
        {
            if (activeLasers.Contains(owner) == false && gameOverHandled == false)
            {
                activeLasers.Add(owner);
            }
        }
        else
        {
            activeLasers.Remove(owner);
        }
    }

    private void UpdateLaserLoop()
    {
        for (int i = activeLasers.Count - 1; i >= 0; i--)
        {
            if (activeLasers[i] == null || activeLasers[i].gameObject.activeInHierarchy == false)
            {
                activeLasers.RemoveAt(i);
            }
        }
        bool shouldPlay = activeLasers.Count > 0;
        if (shouldPlay == laserRunning)
        {
            return;
        }
        laserRunning = shouldPlay;
        if (shouldPlay)
        {
            laserSource.Play();
        }
        else
        {
            laserSource.Stop();
        }
    }

    private void StopWorld()
    {
        musicSource.Stop();
        if (engineSource != null)
        {
            engineSource.Stop();
            windSource.Stop();
            laserSource.Stop();
        }
        for (int i = 0; i < worldVoices.Length; i++)
        {
            worldVoices[i].Stop();
        }
        activeLasers.Clear();
        laserRunning = false;
    }

    private void OnApplicationPause(bool paused)
    {
        background = paused;
        for (int i = 0; i < interfaceVoices.Length; i++)
        {
            interfaceVoices[i].ignoreListenerPause = paused == false;
        }
        AudioListener.pause = background || Time.timeScale == 0f;
    }

    private void OnDisable()
    {
        AudioSignals.SoundRequested -= PlaySound;
        AudioSignals.CloseCallRequested -= PlayCloseCall;
        AudioSignals.LaserChanged -= SetLaser;
        StopWorld();
        for (int i = 0; i < interfaceVoices.Length; i++)
        {
            interfaceVoices[i].Stop();
        }
        AudioListener.pause = false;
    }
}
