// Scorre e ricicla le tessere di uno strato del fondale.
using UnityEngine;
using BeyondSky.Systems;

namespace BeyondSky.World
{
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpeedManager speedManager;
        [SerializeField] private GameObject tilePrefab;

        [Header("Tiles")]
        [SerializeField] private int columns;
        [SerializeField] private float tileWidth;

        [Header("Scroll factors - fractions of the game speed")]
        [Range(0f, 2f)]
        [SerializeField] private float horizontalFactor;

        private Transform[] tiles;
        private float spanX;
        private bool isVisible = true;
        private float verticalStartY;

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
            ScrollTiles(speed * horizontalFactor);
        }

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

        public void CaptureVerticalStart()
        {
            if (tiles == null || tiles.Length == 0)
            {
                return;
            }

            verticalStartY = tiles[0].localPosition.y;
        }

        public void SetVerticalDisplacement(float distance)
        {
            if (tiles == null)
            {
                return;
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                Vector3 position = tiles[i].localPosition;
                position.y = verticalStartY - distance;
                tiles[i].localPosition = position;
            }
        }

        private void BuildGrid()
        {
            spanX = columns * tileWidth;
            tiles = new Transform[columns];

            for (int column = 0; column < columns; column++)
            {
                GameObject tile = Instantiate(tilePrefab, transform);
                float x = column * tileWidth - spanX * 0.5f + tileWidth * 0.5f;

                tile.transform.localPosition = new Vector3(x, 0f, 0f);
                tiles[column] = tile.transform;
            }

            if (tilePrefab.scene.IsValid())
            {
                tilePrefab.SetActive(false);
            }

            SetVisible(isVisible);
        }

        private void ScrollTiles(float deltaX)
        {
            float halfX = spanX * 0.5f;

            for (int i = 0; i < tiles.Length; i++)
            {
                Vector3 position = tiles[i].localPosition;

                position.x = position.x - deltaX;

                if (position.x < -halfX)
                {
                    position.x = position.x + spanX;
                }

                tiles[i].localPosition = position;
            }
        }
    }
}
