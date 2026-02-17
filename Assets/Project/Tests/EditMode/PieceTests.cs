using NUnit.Framework;
using UnityEngine;

namespace Project.Scripts.EditMode.Tests
{
    /// <summary>
    /// Tests for <see cref="Piece"/> movement, rotation, and cell management.
    /// </summary>
    public class PieceTests
    {
        private GameObject pieceObject;
        private Piece piece;

        [SetUp]
        public void SetUp()
        {
            pieceObject = new GameObject("Piece");
            piece = pieceObject.AddComponent<Piece>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(pieceObject);
        }

        [Test]
        public void Initialize_SetsTypePositionAndRotation()
        {
            Vector2Int position = new Vector2Int(0, 8);
            piece.Initialize(Tetromino.T, position, null);

            Assert.AreEqual(Tetromino.T, piece.TetrominoType);
            Assert.AreEqual(position, piece.Position);
            Assert.AreEqual(0, piece.RotationIndex);
        }

        [Test]
        public void Initialize_CopiesCellsFromData()
        {
            piece.Initialize(Tetromino.T, new Vector2Int(0, 8), null);

            Vector2Int[] expected = Data.Cells[Tetromino.T];
            Assert.AreEqual(expected.Length, piece.Cells.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], piece.Cells[i]);
            }
        }

        [Test]
        public void Initialize_Creates4Blocks()
        {
            piece.Initialize(Tetromino.I, new Vector2Int(0, 8), null);

            Assert.AreEqual(4, piece.Blocks.Length);
            foreach (GameObject block in piece.Blocks)
            {
                Assert.IsNotNull(block);
                Assert.IsNotNull(block.GetComponent<SpriteRenderer>());
            }
        }

        [Test]
        public void Move_UpdatesPosition()
        {
            piece.Initialize(Tetromino.T, new Vector2Int(0, 8), null);

            piece.Move(new Vector2Int(2, 5));

            Assert.AreEqual(new Vector2Int(2, 5), piece.Position);
        }

        [Test]
        public void Rotate_UpdatesRotationIndexAndCells()
        {
            piece.Initialize(Tetromino.T, new Vector2Int(0, 8), null);

            Vector2Int[] rotatedCells = Data.AllRotations[Tetromino.T][1];
            piece.Rotate(1, rotatedCells);

            Assert.AreEqual(1, piece.RotationIndex);

            for (int i = 0; i < rotatedCells.Length; i++)
            {
                Assert.AreEqual(rotatedCells[i], piece.Cells[i]);
            }
        }

        [Test]
        public void Cells_AreCopy_ModifyingDoesNotCorruptData()
        {
            piece.Initialize(Tetromino.T, new Vector2Int(0, 8), null);

            Vector2Int original = Data.Cells[Tetromino.T][0];

            // Mutate the piece's cells
            piece.Cells[0] = new Vector2Int(99, 99);

            // Original data should be unaffected
            Assert.AreEqual(original, Data.Cells[Tetromino.T][0]);
        }
    }
}
