// Conserva i parametri di riproduzione di un effetto sonoro.
using UnityEngine;

namespace BeyondSky.Audio
{
    [System.Serializable]
    public class AudioCue
    {
        [SerializeField] private GameSound sound;
        [SerializeField] private AudioClip clip;
        [Range(0f, 1f)]
        [SerializeField] private float gain = 1f;
        [SerializeField] private float minimumInterval = 0.04f;
        [SerializeField] private bool varyPitch = true;
        [Range(0, 256)]
        [SerializeField] private int priority = 128;

        public GameSound Sound
        {
            get
            {
                return sound;
            }
        }

        public AudioClip Clip
        {
            get
            {
                return clip;
            }
        }

        public float Gain
        {
            get
            {
                return gain;
            }
        }

        public float MinimumInterval
        {
            get
            {
                return minimumInterval;
            }
        }

        public bool VaryPitch
        {
            get
            {
                return varyPitch;
            }
        }

        public int Priority
        {
            get
            {
                return priority;
            }
        }
    }
}
