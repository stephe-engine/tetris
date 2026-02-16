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

        private void Start()
        {
            board.SpawnPiece();
        }

        private void Update()
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= stepDelay)
            {
                stepTimer = 0f;
                Step();
            }
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
                // Piece has landed — spawn the next one
                // TODO: lock piece cells into the board grid before spawning
                board.SpawnPiece();
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
    }
}
