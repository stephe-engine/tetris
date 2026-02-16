using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Top-level game orchestrator. Manages gravity timing and command execution.
    /// Owns the game rules (when pieces fall, what happens on input) while
    /// <see cref="Board"/> owns the grid state. This separation supports multiplayer:
    /// each player gets their own Board, but game rules are shared.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>Reference to the game board that this manager controls.</summary>
        [SerializeField] private Board board;

        /// <summary>Time in seconds between automatic gravity steps.</summary>
        [SerializeField] private float stepDelay = 1.0f;

        private float stepTimer;
        private bool gravityEnabled = true;
        private bool gameOver;

        private void Start()
        {
            board.SpawnPiece();
        }

        private void Update()
        {
            if (gameOver || !gravityEnabled)
                return;

            stepTimer += Time.deltaTime;

            if (stepTimer >= stepDelay)
            {
                stepTimer = 0f;
                Step();
            }
        }

        /// <summary>
        /// Toggles automatic gravity on or off. Editor-only debug feature.
        /// When gravity is re-enabled, the step timer resets to avoid an
        /// immediate catch-up step.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ToggleGravity()
        {
            gravityEnabled = !gravityEnabled;
            stepTimer = 0f;
            Debug.Log($"Gravity {(gravityEnabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Performs one gravity step: attempts to move the active piece down by one cell.
        /// If the piece cannot move down, it locks in place and a new piece spawns.
        /// </summary>
        private void Step()
        {
            bool moved = MovePiece(Vector2Int.down);

            if (!moved)
            {
                board.LockPiece();
                board.ClearLines();
                board.SpawnPiece();

                if (!IsActivePieceValid())
                {
                    gameOver = true;
                    Debug.Log("Game Over");
                }
            }
        }

        /// <summary>
        /// Executes a game command by translating it into the appropriate piece movement.
        /// Commands are the abstraction boundary for input — they can originate from
        /// local input, network, or AI without changing this logic.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        public void ExecuteCommand(GameCommand command)
        {
            if (gameOver)
                return;

            switch (command)
            {
                case GameCommand.MoveLeft:
                    MovePiece(Vector2Int.left);
                    break;

                case GameCommand.MoveRight:
                    MovePiece(Vector2Int.right);
                    break;

                case GameCommand.SoftDrop:
                    MovePiece(Vector2Int.down);
                    stepTimer = 0f; // Reset gravity so it doesn't stack with soft drop
                    break;

                case GameCommand.HardDrop:
                    Debug.Log("HardDrop not implemented");
                    break;

                case GameCommand.RotateClockwise:
                    RotatePiece(1);
                    break;

                case GameCommand.RotateCounterClockwise:
                    RotatePiece(-1);
                    break;
            }
        }

        /// <summary>
        /// Attempts to move the active piece by the given direction.
        /// Validates that all cells of the piece would occupy valid board positions
        /// before committing the move.
        /// </summary>
        /// <param name="direction">The direction to move (e.g. <see cref="Vector2Int.left"/>).</param>
        /// <returns>True if the move was valid and applied; false if blocked.</returns>
        private bool MovePiece(Vector2Int direction)
        {
            Piece piece = board.ActivePiece;

            if (!piece)
                return false;

            Vector2Int newPosition = piece.Position + direction;

            // Validate every cell of the piece at the new position
            for (int i = 0; i < piece.Cells.Length; i++)
            {
                Vector2Int cellPosition = newPosition + piece.Cells[i];

                if (!board.IsValidPosition(cellPosition))
                    return false;
            }

            piece.Move(newPosition);
            return true;
        }

        /// <summary>
        /// Attempts to rotate the active piece using the Super Rotation System (SRS).
        /// Tries up to 5 wall kick offsets for the given rotation transition.
        /// Skips rotation entirely for the O-piece.
        /// </summary>
        /// <param name="direction">Rotation direction: +1 for clockwise, -1 for counter-clockwise.</param>
        /// <returns>True if the rotation was valid and applied; false if all kick tests failed.</returns>
        private bool RotatePiece(int direction)
        {
            Piece piece = board.ActivePiece;

            if (!piece)
                return false;

            // O-piece does not rotate
            if (piece.TetrominoType == Tetromino.O)
                return false;

            int fromRotation = piece.RotationIndex;
            int toRotation = (fromRotation + direction + 4) % 4;

            Vector2Int[] rotatedCells = Data.AllRotations[piece.TetrominoType][toRotation];
            Vector2Int[] kickOffsets = Data.WallKicks[piece.TetrominoType][(fromRotation, toRotation)];

            // Try each of the 5 SRS kick offsets (test 0 is always (0,0) — no shift)
            for (int k = 0; k < kickOffsets.Length; k++)
            {
                // Candidate pivot = current position shifted by this kick offset
                Vector2Int kickedPosition = piece.Position + kickOffsets[k];
                bool valid = true;

                // Check all 4 cells of the rotated piece at the kicked position
                for (int i = 0; i < rotatedCells.Length; i++)
                {
                    Vector2Int cellPosition = kickedPosition + rotatedCells[i];

                    if (!board.IsValidPosition(cellPosition))
                    {
                        // Any out-of-bounds or occupied cell fails this kick test
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    // Apply the kick translation only if it's non-zero
                    if (kickOffsets[k] != Vector2Int.zero)
                    {
                        piece.Move(kickedPosition);
                    }

                    piece.Rotate(toRotation, rotatedCells);
                    return true;
                }
            }

            // All 5 kick tests failed — rotation is blocked
            return false;
        }

        /// <summary>
        /// Checks whether the active piece's cells all occupy valid board positions.
        /// Used after spawning to detect game over (top-out).
        /// </summary>
        /// <returns>True if every cell is within bounds and unoccupied; false otherwise.</returns>
        private bool IsActivePieceValid()
        {
            Piece piece = board.ActivePiece;

            if (!piece)
                return false;

            for (int i = 0; i < piece.Cells.Length; i++)
            {
                Vector2Int cellPosition = piece.Position + piece.Cells[i];

                if (!board.IsValidPosition(cellPosition))
                    return false;
            }

            return true;
        }
    }
}
