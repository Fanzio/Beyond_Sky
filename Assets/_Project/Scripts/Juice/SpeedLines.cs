using UnityEngine;

public class SpeedLines : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SpriteRenderer[] lines;
    [SerializeField] private float maximumAlpha = 0.12f;
    [SerializeField] private float speedFactor = 1.3f;
    [SerializeField] private float visibleFromSpeed = 0.15f;
    private Vector3 center;
    private System.Random random = new System.Random(481);

    private void Start()
    {
        center = worldCamera.transform.position;
        float halfWidth = worldCamera.orthographicSize * worldCamera.aspect;
        for (int i = 0; i < lines.Length; i++)
        {
            PlaceLine(i, center.x - halfWidth + (i + 0.5f) * halfWidth * 2f / lines.Length);
        }
    }

    private void Update()
    {
        if (gameManager.IsGameOver)
        {
            Hide();
            return;
        }
        float alpha = maximumAlpha * Mathf.InverseLerp(visibleFromSpeed, 1f, speedManager.NormalizedSpeed);
        float halfWidth = worldCamera.orthographicSize * worldCamera.aspect;
        for (int i = 0; i < lines.Length; i++)
        {
            SpriteRenderer line = lines[i];
            Vector3 position = line.transform.position;
            position.x = position.x - speedManager.CurrentSpeed * speedFactor * Time.deltaTime;
            line.transform.position = position;
            if (position.x + line.bounds.extents.x < center.x - halfWidth)
            {
                PlaceLine(i, center.x + halfWidth + line.bounds.extents.x);
            }
            Color color = Color.white;
            color.a = alpha;
            line.color = color;
            line.enabled = alpha > 0f;
        }
    }

    private void PlaceLine(int index, float x)
    {
        float y = center.y + ((float)random.NextDouble() * 1.5f - 0.65f) * worldCamera.orthographicSize;
        lines[index].transform.position = new Vector3(x, y, 0f);
    }

    private void Hide()
    {
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].enabled = false;
        }
    }

    private void OnDisable()
    {
        if (lines != null)
        {
            Hide();
        }
    }
}
