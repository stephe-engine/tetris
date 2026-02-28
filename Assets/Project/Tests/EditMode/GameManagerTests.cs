using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Project.Scripts.EditMode.Tests
{
    /// <summary>
    /// Tests for <see cref="GameManager"/> command execution, movement validation,
    /// and SRS wall kick logic.
    /// </summary>
    public class GameManagerTests
    {
        private GameObject boardObject;
        private Board board;
        private GameObject managerObject;
        private GameManager manager;

        [SetUp]
        public void SetUp()
        {
            boardObject = new GameObject("Board");
            board = boardObject.AddComponent<Board>();

            // Initialize Board internals via reflection
            FieldInfo cellsField = typeof(Board).GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance);
            cellsField.SetValue(board, new int[board.width, board.height]);

            FieldInfo blocksField = typeof(Board).GetField("lockedBlocks", BindingFlags.NonPublic | BindingFlags.Instance);
            blocksField.SetValue(board, new GameObject[board.width, board.height]);

            managerObject = new GameObject("GameManager");
            manager = managerObject.AddComponent<GameManager>();

            // Wire up the board reference
            FieldInfo boardField = typeof(GameManager).GetField("board", BindingFlags.NonPublic | BindingFlags.Instance);
            boardField.SetValue(manager, board);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(boardObject);
        }

        [Test]
        public void ExecuteCommand_MoveLeft_MovesActivePiece()
        {
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(0, 0));

            manager.ExecuteCommand(GameCommand.MoveLeft);

            Assert.AreEqual(new Vector2Int(-1, 0), piece.Position);
        }

        [Test]
        public void ExecuteCommand_MoveRight_MovesActivePiece()
        {
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(0, 0));

            manager.ExecuteCommand(GameCommand.MoveRight);

            Assert.AreEqual(new Vector2Int(1, 0), piece.Position);
        }

        [Test]
        public void ExecuteCommand_SoftDrop_MovesActivePieceDown()
        {
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(0, 0));

            manager.ExecuteCommand(GameCommand.SoftDrop);

            Assert.AreEqual(new Vector2Int(0, -1), piece.Position);
        }

        [Test]
        public void Movement_BlockedAtLeftEdge()
        {
            // T-piece at leftmost valid position: cells are at (-1,0),(0,0),(1,0),(0,1)
            // Position (-4, 0) puts leftmost cell at -4 + (-1) = -5, which is xMin (valid)
            // Position (-5, 0) would put leftmost cell at -6, which is out of bounds
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(-4, 0));

            manager.ExecuteCommand(GameCommand.MoveLeft);

            Assert.AreEqual(new Vector2Int(-4, 0), piece.Position, "Should not move past left edge");
        }

        [Test]
        public void Movement_BlockedAtRightEdge()
        {
            // T-piece cells: (-1,0),(0,0),(1,0),(0,1)
            // Position (4, 0) puts rightmost cell at 4 + 1 = 5, which is xMax (invalid, >= xMax)
            // So max valid x is 3 (rightmost cell at 4, which is < xMax=5)
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(3, 0));

            manager.ExecuteCommand(GameCommand.MoveRight);

            Assert.AreEqual(new Vector2Int(3, 0), piece.Position, "Should not move past right edge");
        }

        [Test]
        public void OPiece_Rotation_IsSkipped()
        {
            Piece piece = SpawnTestPiece(Tetromino.O, new Vector2Int(0, 0));
            Vector2Int[] originalCells = (Vector2Int[])piece.Cells.Clone();

            manager.ExecuteCommand(GameCommand.RotateClockwise);

            Assert.AreEqual(0, piece.RotationIndex, "O-piece should not rotate");
            for (int i = 0; i < originalCells.Length; i++)
            {
                Assert.AreEqual(originalCells[i], piece.Cells[i]);
            }
        }

        [Test]
        public void RotateClockwise_TPiece_UpdatesRotation()
        {
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(0, 0));

            manager.ExecuteCommand(GameCommand.RotateClockwise);

            Assert.AreEqual(1, piece.RotationIndex);
        }

        [Test]
        public void RotateCounterClockwise_TPiece_UpdatesRotation()
        {
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(0, 0));

            manager.ExecuteCommand(GameCommand.RotateCounterClockwise);

            Assert.AreEqual(3, piece.RotationIndex);
        }

        [Test]
        public void SRS_WallKick_SucceedsAgainstWall()
        {
            // Place T-piece at left wall: position (-4, 0)
            // T-piece rotation 0 cells: (-1,0),(0,0),(1,0),(0,1)
            // Occupied at: (-5,0),(-4,0),(-3,0),(-4,1)
            //
            // CW rotation to state 1 cells: (0,-1),(0,0),(0,1),(1,0)
            // At position (-4,0): cells at (-4,-1),(-4,0),(-4,1),(-3,0) — all valid
            // Test 0 (0,0) should work since cells fit
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(-4, 0));

            manager.ExecuteCommand(GameCommand.RotateClockwise);

            Assert.AreEqual(1, piece.RotationIndex, "Rotation should succeed (possibly via wall kick)");
        }

        [Test]
        public void SRS_WallKick_UsesKickOffset_WhenBaseRotationBlocked()
        {
            // Place T-piece at position (-4, 0), rotation 0
            // Block the cell needed for base rotation to force a wall kick
            // T-piece CW 0->1: rotated cells are (0,-1),(0,0),(0,1),(1,0)
            // At base position (-4, 0): (-4,-1),(-4,0),(-4,1),(-3,0) — all valid without kicks
            //
            // Instead, use I-piece at left wall for a clearer kick scenario
            // I-piece rotation 0 cells: (-1,0),(0,0),(1,0),(2,0)
            // At position (-3, 0): cells at (-4,0),(-3,0),(-2,0),(-1,0)
            //
            // CW rotation to state 1 cells: (0,1),(0,0),(0,-1),(0,-2)
            // At base position (-3, 0): (-3,1),(-3,0),(-3,-1),(-3,-2) — all valid
            //
            // Better approach: put I-piece flush against left wall
            // Position (-3, 0): leftmost cell = -3 + (-1) = -4 (valid)
            // After CW rotation state 1: (0,1),(0,0),(0,-1),(0,-2) at pos (-3,0)
            // = (-3,1),(-3,0),(-3,-1),(-3,-2) — valid, no kick needed
            //
            // Let's block a cell to force a kick. Place I-piece at (-3,0),
            // block cell (-3,1) so base rotation fails, kick should shift.
            Piece piece = SpawnTestPiece(Tetromino.I, new Vector2Int(-3, 0));

            // Block the cell at grid (-3, 1) = array index (2, 11)
            board.SetCell(2, 11, 1);

            manager.ExecuteCommand(GameCommand.RotateClockwise);

            // The rotation should succeed via a wall kick offset
            Assert.AreEqual(1, piece.RotationIndex, "Rotation should succeed via wall kick");
        }

        // ── Pause / Resume ───────────────────────────────────────────────────

        [Test]
        public void Pause_SetsPausedState()
        {
            manager.Pause();

            Assert.IsTrue(manager.IsPaused);
        }

        [Test]
        public void Resume_ClearsPausedState()
        {
            manager.Pause();
            manager.Resume();

            Assert.IsFalse(manager.IsPaused);
        }

        [Test]
        public void Pause_WhenAlreadyPaused_IsNoOp()
        {
            manager.Pause();
            manager.Pause(); // Second call is a no-op

            Assert.IsTrue(manager.IsPaused);
        }

        [Test]
        public void Resume_WhenNotPaused_IsNoOp()
        {
            manager.Resume(); // Calling when not paused is a no-op

            Assert.IsFalse(manager.IsPaused);
        }

        [Test]
        public void ExecuteCommand_WhilePaused_DoesNotMovePiece()
        {
            Piece piece = SpawnTestPiece(Tetromino.T, new Vector2Int(0, 0));

            manager.Pause();
            manager.ExecuteCommand(GameCommand.MoveLeft);

            Assert.AreEqual(new Vector2Int(0, 0), piece.Position);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>Creates a Piece and sets it as the Board's active piece.</summary>
        private Piece SpawnTestPiece(Tetromino type, Vector2Int position)
        {
            GameObject pieceObject = new GameObject("Piece");
            pieceObject.transform.SetParent(boardObject.transform, false);

            Piece piece = pieceObject.AddComponent<Piece>();
            piece.Initialize(type, position, null);

            FieldInfo field = typeof(Board).GetField("activePiece", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(board, piece);

            return piece;
        }
    }
}
