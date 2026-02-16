using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// The main game board controller. Manages the 10x20 grid state,
    /// orchestrates piece spawning, and provides position validation.
    /// </summary>
    public class Board : MonoBehaviour
    {
        public int width = 10;
        public int height = 20;

        /// <summary>Total duration in seconds for the line clear flash animation.</summary>
        [SerializeField] private float flashDuration = 0.75f;

        /// <summary>Number of on/off blink cycles during the flash animation.</summary>
        [SerializeField] private int flashCount = 3;

        /// <summary>Alpha transparency of the ghost piece preview (0 = invisible, 1 = opaque).</summary>
        [SerializeField] [Range(0f, 1f)] private float ghostAlpha = 0.1f;

        [SerializeField] private Sprite spriteI;
        [SerializeField] private Sprite spriteO;
        [SerializeField] private Sprite spriteT;
        [SerializeField] private Sprite spriteS;
        [SerializeField] private Sprite spriteZ;
        [SerializeField] private Sprite spriteJ;
        [SerializeField] private Sprite spriteL;

        private static readonly Vector2 CellCenterOffset = new(0.5f, 0.5f);

        private int[,] cells;
        private GameObject[,] lockedBlocks;
        private Piece activePiece;
        private GameObject ghostPiece;

        /// <summary>The currently active (falling) piece, or null if none.</summary>
        public Piece ActivePiece => activePiece;

        /// <summary>
        /// The board's logical bounds in grid coordinates.
        /// Origin is offset so (0,0) is near the center of the board.
        /// </summary>
        public RectInt Bounds
        {
            get
            {
                Vector2Int position = new Vector2Int(-width / 2, -height / 2);
                return new RectInt(position, new Vector2Int(width, height));
            }
        }

        private void Awake()
        {
            cells = new int[width, height];
            lockedBlocks = new GameObject[width, height];
        }

        /// <summary>
        /// Spawns a new random tetromino at the default spawn position.
        /// </summary>
        public void SpawnPiece()
        {
            Tetromino type = (Tetromino) Random.Range(0, System.Enum.GetValues(typeof(Tetromino)).Length);
            Sprite sprite = GetSpriteForTetromino(type);

            GameObject pieceObject = new GameObject("Piece");
            pieceObject.transform.SetParent(transform, false);

            activePiece = pieceObject.AddComponent<Piece>();
            activePiece.Initialize(type, Data.SpawnPosition, sprite);

            UpdateGhostPiece();
        }

        /// <summary>
        /// Returns the sprite assigned to the given tetromino type.
        /// </summary>
        /// <param name="type">The tetromino type to look up.</param>
        /// <returns>The corresponding sprite, or the I-piece sprite as a fallback.</returns>
        private Sprite GetSpriteForTetromino(Tetromino type)
        {
            return type switch
            {
                Tetromino.I => spriteI,
                Tetromino.O => spriteO,
                Tetromino.T => spriteT,
                Tetromino.S => spriteS,
                Tetromino.Z => spriteZ,
                Tetromino.J => spriteJ,
                Tetromino.L => spriteL,
                _ => spriteI,
            };
        }

        /// <summary>
        /// Checks whether a grid position is within bounds and unoccupied.
        /// </summary>
        /// <param name="position">The grid position to validate.</param>
        /// <returns>True if the position is valid and empty; false otherwise.</returns>
        public bool IsValidPosition(Vector2Int position)
        {
            RectInt bounds = Bounds;

            if (position.x < bounds.xMin || position.x >= bounds.xMax)
                return false;
            if (position.y < bounds.yMin || position.y >= bounds.yMax)
                return false;

            int x = position.x - bounds.xMin;
            int y = position.y - bounds.yMin;

            return cells[x, y] == 0;
        }

        /// <summary>
        /// Locks the active piece into the board grid. Writes cell values,
        /// reparents block GameObjects from the Piece to the Board, and
        /// destroys the now-empty Piece GameObject.
        /// </summary>
        public void LockPiece()
        {
            ClearGhostPiece();

            if (!activePiece)
                return;

            RectInt bounds = Bounds;

            for (int i = 0; i < activePiece.Cells.Length; i++)
            {
                Vector2Int gridPos = activePiece.Position + activePiece.Cells[i];
                int x = gridPos.x - bounds.xMin;
                int y = gridPos.y - bounds.yMin;

                cells[x, y] = 1;

                GameObject block = activePiece.Blocks[i];
                block.transform.SetParent(transform, true);
                lockedBlocks[x, y] = block;
            }

            Destroy(activePiece.gameObject);
            activePiece = null;
        }

        /// <summary>
        /// Detects fully occupied rows without modifying the grid.
        /// </summary>
        /// <returns>A list of full row indices (array indices, not grid coords), sorted bottom to top.</returns>
        public List<int> FindFullRows()
        {
            List<int> fullRows = new();

            for (int row = 0; row < height; row++)
            {
                bool full = true;

                for (int col = 0; col < width; col++)
                {
                    if (cells[col, row] == 0)
                    {
                        full = false;
                        break;
                    }
                }

                if (full)
                {
                    fullRows.Add(row);
                }
            }

            return fullRows;
        }

        /// <summary>
        /// Coroutine that blinks the blocks in the given rows between white and their
        /// original color. Blinks <see cref="flashCount"/> times over <see cref="flashDuration"/> seconds.
        /// </summary>
        /// <param name="rows">Row indices (array indices) to flash.</param>
        public IEnumerator FlashRows(List<int> rows)
        {
            float interval = flashDuration / (flashCount * 2);

            // Cache original colors so we can restore them
            Dictionary<SpriteRenderer, Color> originalColors = new();
            foreach (int row in rows)
            {
                for (int col = 0; col < width; col++)
                {
                    if (lockedBlocks[col, row])
                    {
                        SpriteRenderer sr = lockedBlocks[col, row].GetComponent<SpriteRenderer>();
                        if (sr)
                        {
                            originalColors[sr] = sr.color;
                        }
                    }
                }
            }

            for (int i = 0; i < flashCount; i++)
            {
                // Flash transparent (hide blocks)
                foreach (SpriteRenderer sr in originalColors.Keys)
                {
                    sr.color = Color.clear;
                }
                yield return new WaitForSeconds(interval);

                // Restore original color (show blocks)
                foreach (KeyValuePair<SpriteRenderer, Color> pair in originalColors)
                {
                    pair.Key.color = pair.Value;
                }
                yield return new WaitForSeconds(interval);
            }
        }

        /// <summary>
        /// Destroys blocks in the given rows and collapses rows above downward to fill gaps.
        /// </summary>
        /// <param name="rows">Sorted (bottom to top) list of full row indices to clear.</param>
        /// <returns>The number of lines cleared.</returns>
        public int ClearAndCollapseRows(List<int> rows)
        {
            RectInt bounds = Bounds;
            int linesCleared = 0;

            // Process from bottom; adjust indices as rows collapse
            foreach (int originalRow in rows)
            {
                int row = originalRow - linesCleared;

                // Destroy all blocks in this row
                for (int col = 0; col < width; col++)
                {
                    Destroy(lockedBlocks[col, row]);
                    lockedBlocks[col, row] = null;
                    cells[col, row] = 0;
                }

                // Shift all rows above this one down by one
                for (int aboveRow = row + 1; aboveRow < height; aboveRow++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        cells[col, aboveRow - 1] = cells[col, aboveRow];
                        cells[col, aboveRow] = 0;

                        lockedBlocks[col, aboveRow - 1] = lockedBlocks[col, aboveRow];
                        lockedBlocks[col, aboveRow] = null;

                        if (lockedBlocks[col, aboveRow - 1])
                        {
                            Vector2 newPos = new Vector2(
                                col + bounds.xMin,
                                aboveRow - 1 + bounds.yMin
                            ) + CellCenterOffset;
                            lockedBlocks[col, aboveRow - 1].transform.localPosition = newPos;
                        }
                    }
                }

                linesCleared++;
            }

            return linesCleared;
        }

        /// <summary>
        /// Updates the ghost piece to show where the active piece would land
        /// if hard-dropped. Creates the ghost GameObject on first call, then
        /// repositions its blocks to match the active piece's drop destination.
        /// </summary>
        public void UpdateGhostPiece()
        {
            if (!activePiece)
                return;

            // Calculate the drop destination by walking the piece down
            Vector2Int ghostPosition = activePiece.Position;

            while (true)
            {
                Vector2Int testPosition = ghostPosition + Vector2Int.down;
                bool valid = true;

                for (int i = 0; i < activePiece.Cells.Length; i++)
                {
                    Vector2Int cellPosition = testPosition + activePiece.Cells[i];

                    if (!IsValidPosition(cellPosition))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    break;

                ghostPosition = testPosition;
            }

            // Recreate ghost if it doesn't exist or block count changed
            if (!ghostPiece)
            {
                ghostPiece = new GameObject("Ghost");
                ghostPiece.transform.SetParent(transform, false);
            }

            // Ensure correct number of child blocks
            int existingBlocks = ghostPiece.transform.childCount;
            int neededBlocks = activePiece.Cells.Length;

            for (int i = existingBlocks; i < neededBlocks; i++)
            {
                GameObject block = new($"GhostBlock {i}");
                block.transform.SetParent(ghostPiece.transform, false);
                block.AddComponent<SpriteRenderer>();
            }

            for (int i = existingBlocks - 1; i >= neededBlocks; i--)
            {
                Destroy(ghostPiece.transform.GetChild(i).gameObject);
            }

            // Update each ghost block's position and appearance
            Sprite activeSprite = activePiece.Blocks[0].GetComponent<SpriteRenderer>().sprite;

            for (int i = 0; i < neededBlocks; i++)
            {
                Transform ghostBlock = ghostPiece.transform.GetChild(i);
                SpriteRenderer sr = ghostBlock.GetComponent<SpriteRenderer>();
                sr.sprite = activeSprite;
                sr.color = new Color(1f, 1f, 1f, ghostAlpha);
                sr.sortingOrder = 1;

                Vector2 cellPosition = new Vector2(
                    ghostPosition.x + activePiece.Cells[i].x,
                    ghostPosition.y + activePiece.Cells[i].y
                ) + CellCenterOffset;
                ghostBlock.localPosition = cellPosition;
            }
        }

        /// <summary>
        /// Destroys the ghost piece GameObject so it doesn't linger during
        /// lock, flash, and clear sequences.
        /// </summary>
        public void ClearGhostPiece()
        {
            if (ghostPiece)
            {
                Destroy(ghostPiece);
                ghostPiece = null;
            }
        }

        /// <summary>
        /// Gets the value of a cell in the internal grid array.
        /// </summary>
        /// <param name="x">Column index (0-based from left).</param>
        /// <param name="y">Row index (0-based from bottom).</param>
        /// <returns>The cell value (0 = empty).</returns>
        public int GetCell(int x, int y)
        {
            return cells[x, y];
        }

        /// <summary>
        /// Sets the value of a cell in the internal grid array.
        /// </summary>
        /// <param name="x">Column index (0-based from left).</param>
        /// <param name="y">Row index (0-based from bottom).</param>
        /// <param name="value">The value to store (0 = empty).</param>
        public void SetCell(int x, int y, int value)
        {
            cells[x, y] = value;
        }
    }
}
