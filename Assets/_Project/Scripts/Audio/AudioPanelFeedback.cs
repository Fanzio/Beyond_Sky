using UnityEngine;

public class AudioPanelFeedback : MonoBehaviour
{
    private void OnEnable()
    {
        AudioSignals.Play(GameSound.UIPanelOpen);
    }
}
