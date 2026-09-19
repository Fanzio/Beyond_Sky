using System;
using UnityEngine;

public enum GameSound
{
    PlayerShot,
    CloseCall,
    AmmoPickup,
    Dodge,
    Coin,
    EnemyWarning,
    ShieldPickup,
    ShieldBreak,
    PlayerCrash,
    EnemyShot,
    ObstacleDestroyed,
    EnemyDestroyed,
    Explosion,
    Ascension,
    BossIncoming,
    BossDefeated,
    BossHit,
    UIClick,
    UIPanelOpen,
    GameOverJingle,
    SpaceReached
}

public static class AudioSignals
{
    public static event Action<GameSound> SoundRequested;
    public static event Action<int, int> CloseCallRequested;
    public static event Action<Component, bool> LaserChanged;

    public static void Play(GameSound sound)
    {
        if (SoundRequested != null)
        {
            SoundRequested.Invoke(sound);
        }
    }

    public static void PlayCloseCall(int progress, int required)
    {
        if (CloseCallRequested != null)
        {
            CloseCallRequested.Invoke(progress, required);
        }
    }

    public static void SetLaser(Component owner, bool active)
    {
        if (LaserChanged != null)
        {
            LaserChanged.Invoke(owner, active);
        }
    }
}
