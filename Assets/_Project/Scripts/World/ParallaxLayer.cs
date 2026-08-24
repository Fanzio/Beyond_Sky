using UnityEngine;

// A background layer that scrolls left, wrapping on both axes.
//
// Vertical scrolling stays at zero during normal play: the plane holds a floor, it is not
// climbing, and pretending otherwise would be a lie the player eventually notices. The
// ascension sequence adds a vertical factor on top for a couple of seconds, which is the
// one moment when climbing is actually true.
//
// The extra factor is ADDED, not multiplied. Multiplying a resting value of zero would
// leave it at zero and the effect would never appear.
//
// Vertical wrapping is switched off for layers holding buildings. Clouds look fine coming
// back round from the top; a skyline does not. With wrapping off the city simply sinks
// away during the ascension and is gone, which is what climbing out of it should look
// like anyway.
public class ParallaxLayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private GameObject tilePrefab;

    [Header("Grid")]
    [SerializeField] private int columns;
    [SerializeField] private int rows;
    [SerializeField] private Vector2 tileSize;

    [Header("Scroll factors - fractions of the game speed")]
    [Range(0f, 2f)]
    [SerializeField] private float horizontalFactor;

    [Range(0f, 1f)]
    [SerializeField] private float verticalFactor;

    [Header("Wrapping")]
    [SerializeField] private bool wrapVertically = true;

    private Transform[] tiles;
    private float spanX;
    private float spanY;
    private bool isVisible = true;
    private float extraVerticalFactor;

    private void Start()
    {
        BuildGrid();
    }
    private void Update()
    {
        if (isVisible == false)
        {
            return;
        }

        float speed = speedManager.CurrentSpeed * Time.deltaTime;
        float vertical = verticalFactor + extraVerticalFactor;

        ScrollTiles(speed * horizontalFactor, speed * vertical);
    }

    // Hides the tiles without deactivating this object, so the grid stays built and never
    // has to be recreated when the floor changes.
    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (tiles == null)
        {
            return;
        }
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].gameObject.SetActive(visible);
        }
    }
    public void SetExtraVerticalFactor(float extra)
    {
        extraVerticalFactor = extra;
    }
    private void BuildGrid()
    {
        spanX = columns * tileSize.x;
        spanY = rows * tileSize.y;

        tiles = new Transform[columns * rows];
        int index = 0;

        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                GameObject tile = Instantiate(tilePrefab, transform);

                float x = column * tileSize.x - spanX * 0.5f + tileSize.x * 0.5f;
                float y = row * tileSize.y - spanY * 0.5f + tileSize.y * 0.5f;

                tile.transform.localPosition = new Vector3(x, y, 0f);
                tiles[index] = tile.transform;
                index = index + 1;
            }
        }
        if (tilePrefab.scene.IsValid())
        {
            tilePrefab.SetActive(false);
        }
        SetVisible(isVisible);
    }
    private void ScrollTiles(float deltaX, float deltaY)
    {
        float halfX = spanX * 0.5f;
        float halfY = spanY * 0.5f;

        for (int i = 0; i < tiles.Length; i++)
        {
            Vector3 position = tiles[i].localPosition;

            position.x = position.x - deltaX;
            position.y = position.y - deltaY;

            if (position.x < -halfX)
            {
                position.x = position.x + spanX;
            }
            if (wrapVertically)
            {
                if (position.y < -halfY)
                {
                    position.y = position.y + spanY;
                }
            }
            tiles[i].localPosition = position;
        }
    }
}