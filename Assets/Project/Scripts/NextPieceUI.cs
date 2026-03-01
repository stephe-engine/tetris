using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Displays the next tetromino piece in a world-space preview panel.
    /// Subscribes to <see cref="Board.OnNextTypeChanged"/> and repositions
    /// four pre-placed <see cref="SpriteRenderer"/> children to show the upcoming shape,
    /// centered within the panel. Each cell is 1 world unit, matching the board grid.
    /// </summary>
    public class NextPieceUI : MonoBehaviour
    {
        [SerializeField] private Board board;

        /// <summary>
        /// Four block SpriteRenderers pre-placed as children. Element order does not matter;
        /// they are repositioned each update.
        /// </summary>
        [SerializeField] private SpriteRenderer[] blockRenderers;

        private void OnEnable()
        {
            board.OnNextTypeChanged += HandleNextTypeChanged;
        }

        private void OnDisable()
        {
            board.OnNextTypeChanged -= HandleNextTypeChanged;
        }

        private void Start()
        {
            Refresh();
        }

        /// <summary>
        /// Reads <see cref="Board.NextType"/> and updates the display.
        /// Called in <see cref="Start"/> to initialise before any events fire.
        /// </summary>
        public void Refresh()
        {
            if (board.NextType.HasValue)
                UpdateDisplay(board.NextType.Value);
            else
                HideBlocks();
        }

        private void HandleNextTypeChanged(Tetromino type)
        {
            UpdateDisplay(type);
        }

        private void UpdateDisplay(Tetromino type)
        {
            Vector2Int[] cellOffsets = Data.Cells[type];
            Sprite sprite = board.GetSpriteForTetromino(type);

            // Compute the bounding-box center in grid units for centering within the panel.
            // Formula: localPos = (cellX - centerX, cellY - centerY).
            // Block centers and bounding-box center each have the same +0.5 offset, which cancels.
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < cellOffsets.Length; i++)
            {
                if (cellOffsets[i].x < minX) minX = cellOffsets[i].x;
                if (cellOffsets[i].y < minY) minY = cellOffsets[i].y;
                if (cellOffsets[i].x > maxX) maxX = cellOffsets[i].x;
                if (cellOffsets[i].y > maxY) maxY = cellOffsets[i].y;
            }
            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;

            for (int i = 0; i < blockRenderers.Length; i++)
            {
                blockRenderers[i].sprite = sprite;
                blockRenderers[i].gameObject.SetActive(true);
                float x = cellOffsets[i].x - centerX;
                float y = cellOffsets[i].y - centerY;
                blockRenderers[i].transform.localPosition = new Vector3(x, y, 0f);
            }
        }

        private void HideBlocks()
        {
            foreach (SpriteRenderer sr in blockRenderers)
                sr.gameObject.SetActive(false);
        }
    }
}
