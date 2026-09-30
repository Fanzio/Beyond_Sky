// Riproduce un suono quando viene aperto un pannello.
using UnityEngine;

namespace BeyondSky.Audio
{
    public class AudioPanelFeedback : MonoBehaviour
    {
        private void OnEnable()
        {
            AudioSignals.Play(GameSound.UIPanelOpen);
        }
    }
}
