using UnityEngine;

[DisallowMultipleComponent]
public class VisualFrameAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite originalSprite;
    [SerializeField] private Sprite[] frames;
    [Min(1f)]
    [SerializeField] private float framesPerSecond = 12f;

    private int frameIndex;
    private float timer;

    private void OnEnable()
    {
        timer = 0f;
        if (targetRenderer == null || frames == null || frames.Length == 0)
        {
            return;
        }
        frameIndex = Random.Range(0, frames.Length);
        targetRenderer.sprite = frames[frameIndex];
    }

    private void Update()
    {
        if (targetRenderer == null || frames == null || frames.Length == 0)
        {
            return;
        }
        timer = timer + Time.deltaTime;
        float interval = 1f / Mathf.Max(1f, framesPerSecond);
        if (timer < interval)
        {
            return;
        }
        int steps = Mathf.FloorToInt(timer / interval);
        timer = timer - steps * interval;
        frameIndex = (frameIndex + steps) % frames.Length;
        targetRenderer.sprite = frames[frameIndex];
    }

    private void OnDisable()
    {
        if (targetRenderer != null && originalSprite != null)
        {
            targetRenderer.sprite = originalSprite;
        }
    }
}
