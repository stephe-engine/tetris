using System.Collections;
using System.Collections.Generic;
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

        /// <summary>Time in seconds a piece sits on a surface before locking.</summary>
        [SerializeField] private float lockDelay = 0.5f;

        /// <summary>Fired once when the game starts and the first piece spawns.</summary>
        public event System.Action OnGameStart;

        /// <summary>Fired when the game ends (top-out).</summary>
        public event System.Action OnGameOver;

        /// <summary>Fired each time the active piece successfully moves left or right.</summary>
        public event System.Action OnMove;

        /// <summary>Fired each time the active piece successfully rotates.</summary>
        public event System.Action OnRotate;

        /// <summary>Fired each time the active piece successfully soft-drops one row.</summary>
        public event System.Action OnSoftDrop;

        /// <summary>Fired when the player triggers a hard drop.</summary>
        public event System.Action OnHardDrop;

        /// <summary>Fired the first time a falling piece contacts a surface (lock delay begins).</summary>
        public event System.Action OnLanded;

        /// <summary>Fired when a piece permanently locks onto the board.</summary>
        public event System.Action OnLock;

        /// <summary>Fired after line clearing completes. Parameter is the number of lines cleared.</summary>
        public event System.Action<int> OnLineClear;

        private float stepTimer;
        private float lockTimer;
        private bool grounded;
        private bool gravityEnabled = true;
        private bool gameOver;
        private bool clearing;

        private void Start()
        {
            board.SpawnPiece();
            OnGameStart?.Invoke();
        }

        private void Update()
        {
            if (gameOver || clearing)
                return;

            // Lock delay timer runs independently of gravity
            if (grounded)
            {
                lockTimer -= Time.deltaTime;

                if (lockTimer <= 0f)
                {
                    grounded = false;
                    StartCoroutine(LockAndSpawn());
                    return;
                }
            }

            if (!gravityEnabled)
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
        /// If the piece reaches a surface, the lock delay timer begins.
        /// </summary>
        private void Step()
        {
            bool moved = MovePiece(Vector2Int.down);

            if (moved)
            {
                UpdateGroundedState();
                board.UpdateGhostPiece();
            }
            else if (!grounded)
            {
                // Piece was already at rest but gravity just discovered it
                grounded = true;
                lockTimer = lockDelay;
                OnLanded?.Invoke();
            }
        }

        /// <summary>
        /// Coroutine that locks the current piece, flashes and clears any full rows,
        /// then spawns the next piece. Pauses gravity and input during the flash.
        /// </summary>
        private IEnumerator LockAndSpawn()
        {
            board.LockPiece();
            OnLock?.Invoke();

            List<int> fullRows = board.FindFullRows();

            if (fullRows.Count > 0)
            {
                clearing = true;
                int lineCount = fullRows.Count;
                yield return board.FlashRows(fullRows, () => OnLineClear?.Invoke(lineCount));
                board.ClearAndCollapseRows(fullRows);
                clearing = false;
            }

            board.SpawnPiece();

            if (!IsActivePieceValid())
            {
                gameOver = true;
                OnGameOver?.Invoke();
                Debug.Log("Game O-ver");
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
            if (gameOver || clearing)
                return;

            switch (command)
            {
                case GameCommand.MoveLeft:
                    if (MovePiece(Vector2Int.left))
                    {
                        OnMove?.Invoke();
                        UpdateGroundedState();
                        board.UpdateGhostPiece();
                    }
                    break;

                case GameCommand.MoveRight:
                    if (MovePiece(Vector2Int.right))
                    {
                        OnMove?.Invoke();
                        UpdateGroundedState();
                        board.UpdateGhostPiece();
                    }
                    break;

                case GameCommand.SoftDrop:
                    if (MovePiece(Vector2Int.down))
                    {
                        OnSoftDrop?.Invoke();
                        stepTimer = 0f; // Reset gravity so it doesn't stack with soft drop
                        UpdateGroundedState();
                        board.UpdateGhostPiece();
                    }
                    break;

                case GameCommand.HardDrop:
                    while (MovePiece(Vector2Int.down)) { }
                    OnHardDrop?.Invoke();
                    grounded = false;
                    StartCoroutine(LockAndSpawn());
                    break;

                case GameCommand.RotateClockwise:
                    if (RotatePiece(1))
                    {
                        OnRotate?.Invoke();
                        UpdateGroundedState();
                        board.UpdateGhostPiece();
                    }
                    break;

                case GameCommand.RotateCounterClockwise:
                    if (RotatePiece(-1))
                    {
                        OnRotate?.Invoke();
                        UpdateGroundedState();
                        board.UpdateGhostPiece();
                    }
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
        /// Checks whether the active piece can move down one cell.
        /// </summary>
        /// <returns>True if all cells would be valid one row lower; false if blocked.</returns>
        private bool CanMoveDown()
        {
            Piece piece = board.ActivePiece;

            if (!piece)
                return false;

            Vector2Int downPosition = piece.Position + Vector2Int.down;

            for (int i = 0; i < piece.Cells.Length; i++)
            {
                Vector2Int cellPosition = downPosition + piece.Cells[i];

                if (!board.IsValidPosition(cellPosition))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the piece is resting on a surface and starts or cancels
        /// the lock delay timer accordingly. Called after any successful move or rotation.
        /// </summary>
        private void UpdateGroundedState()
        {
            if (CanMoveDown())
            {
                grounded = false;
            }
            else
            {
                bool wasGrounded = grounded;
                grounded = true;
                lockTimer = lockDelay;
                if (!wasGrounded)
                    OnLanded?.Invoke();
            }
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
