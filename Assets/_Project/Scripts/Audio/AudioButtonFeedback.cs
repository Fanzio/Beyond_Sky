using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AudioButtonFeedback : MonoBehaviour
{
    private Button button;

    private void OnEnable()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(PlayClick);
    }

    private void PlayClick()
    {
        AudioSignals.Play(GameSound.UIClick);
    }
}
