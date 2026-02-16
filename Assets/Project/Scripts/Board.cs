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

        [SerializeField] private Sprite spriteI;
        [SerializeField] private Sprite spriteO;
        [SerializeField] private Sprite spriteT;
        [SerializeField] private Sprite spriteS;
        [SerializeField] private Sprite spriteZ;
        [SerializeField] private Sprite spriteJ;
        [SerializeField] private Sprite spriteL;

        private int[,] cells;
        private Piece activePiece;

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
        }

        /// <summary>
        /// Spawns a new random tetromino at the default spawn position.
        /// Destroys any existing active piece before creating the new one.
        /// </summary>
        public void SpawnPiece()
        {
            Tetromino type = (Tetromino) Random.Range(0, System.Enum.GetValues(typeof(Tetromino)).Length);
            Sprite sprite = GetSpriteForTetromino(type);

            if (activePiece)
            {
                Destroy(activePiece.gameObject);
            }

            GameObject pieceObject = new GameObject("Piece");
            pieceObject.transform.SetParent(transform, false);

            activePiece = pieceObject.AddComponent<Piece>();
            activePiece.Initialize(type, Data.SpawnPosition, sprite);
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
