using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Represents the active falling tetromino piece.
    /// Manages its grid position, cell offsets, and visual rendering
    /// via child GameObjects with SpriteRenderers.
    /// </summary>
    public class Piece : MonoBehaviour
    {
        /// <summary>The tetromino shape type (I, O, T, S, Z, J, or L).</summary>
        public Tetromino TetrominoType { get; private set; }

        /// <summary>The piece's pivot position in grid coordinates.</summary>
        public Vector2Int Position { get; private set; }

        /// <summary>Current rotation state index (0–3), where 0 is the spawn orientation.</summary>
        public int RotationIndex { get; private set; }

        /// <summary>Cell offsets relative to <see cref="Position"/>. Copied from <see cref="Data.Cells"/> so rotation
        /// can modify them safely.</summary>
        public Vector2Int[] Cells { get; private set; }

        private static readonly Vector2 CellCenterOffset = new(0.5f, 0.5f);

        private GameObject[] blocks;

        /// <summary>The child block GameObjects (one per cell). Used by <see cref="Board"/> to reparent blocks during locking.</summary>
        public GameObject[] Blocks => blocks;

        /// <summary>
        /// Initializes the piece with a shape, grid position, and sprite.
        /// Creates 4 child GameObjects, each with a <see cref="SpriteRenderer"/>,
        /// and positions them according to the tetromino's cell offsets.
        /// </summary>
        /// <param name="type">The tetromino shape to use.</param>
        /// <param name="position">Starting grid position for the piece's pivot.</param>
        /// <param name="sprite">The sprite to assign to each block's renderer.</param>
        public void Initialize(Tetromino type, Vector2Int position, Sprite sprite)
        {
            TetrominoType = type;
            Position = position;
            RotationIndex = 0;

            // Copy cells so rotation won't corrupt shared data
            Vector2Int[] source = Data.Cells[type];
            Cells = new Vector2Int[source.Length];
            source.CopyTo(Cells, 0);

            // Create 4 child block GameObjects with SpriteRenderers
            blocks = new GameObject[Cells.Length];
            for (int i = 0; i < Cells.Length; i++)
            {
                GameObject block = new($"Block {i}");
                block.transform.SetParent(transform, false);

                SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 1;

                blocks[i] = block;
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Moves the piece to a new grid position and updates the visual block positions.
        /// </summary>
        /// <param name="newPosition">The new grid position for the piece's pivot.</param>
        public void Move(Vector2Int newPosition)
        {
            Position = newPosition;
            UpdateVisuals();
        }

        /// <summary>
        /// Replaces the cell offsets (e.g. after rotation) and updates the visual block positions.
        /// </summary>
        /// <param name="newCells">The new cell offsets to apply. Must have the same length as <see cref="Cells"/>.
        /// </param>
        public void SetCells(Vector2Int[] newCells)
        {
            newCells.CopyTo(Cells, 0);
            UpdateVisuals();
        }

        /// <summary>
        /// Updates the rotation state and cell offsets simultaneously.
        /// Called after a successful rotation with wall kick resolution.
        /// </summary>
        /// <param name="newRotationIndex">The new rotation state (0–3).</param>
        /// <param name="newCells">The rotated cell offsets.</param>
        public void Rotate(int newRotationIndex, Vector2Int[] newCells)
        {
            RotationIndex = newRotationIndex;
            SetCells(newCells);
        }

        /// <summary>
        /// Repositions each block's transform to match the current grid state.
        /// Adds a half-unit offset to center blocks within their grid cells.
        /// </summary>
        private void UpdateVisuals()
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                // Offset by 0.5 to center blocks within grid cells
                Vector2 cellPosition = new Vector2(Position.x + Cells[i].x, Position.y + Cells[i].y) + CellCenterOffset;
                blocks[i].transform.localPosition = cellPosition;
            }
        }
    }
}
