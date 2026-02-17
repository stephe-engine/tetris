using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project.Scripts.EditMode.Tests
{
    /// <summary>
    /// Tests for <see cref="Board"/> grid logic: bounds, position validation,
    /// cell state, row detection, and row clearing.
    /// </summary>
    public class BoardTests
    {
        private Board board;
        private GameObject boardObject;

        [SetUp]
        public void SetUp()
        {
            boardObject = new GameObject("Board");
            board = boardObject.AddComponent<Board>();

            // Initialize internal arrays (Awake may not run in EditMode, and
            // even if it does, spawnStrategy is null so it would NullRef)
            FieldInfo cellsField = typeof(Board).GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance);
            cellsField.SetValue(board, new int[board.width, board.height]);

            FieldInfo blocksField = typeof(Board).GetField("lockedBlocks", BindingFlags.NonPublic | BindingFlags.Instance);
            blocksField.SetValue(board, new GameObject[board.width, board.height]);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(boardObject);
        }

        [Test]
        public void Bounds_Returns10x20_CorrectOrigin()
        {
            RectInt bounds = board.Bounds;

            Assert.AreEqual(10, bounds.width);
            Assert.AreEqual(20, bounds.height);
            Assert.AreEqual(-5, bounds.xMin);
            Assert.AreEqual(5, bounds.xMax);
            Assert.AreEqual(-10, bounds.yMin);
            Assert.AreEqual(10, bounds.yMax);
        }

        [Test]
        public void IsValidPosition_InsideBounds_EmptyCell_ReturnsTrue()
        {
            Assert.IsTrue(board.IsValidPosition(new Vector2Int(0, 0)));
            Assert.IsTrue(board.IsValidPosition(new Vector2Int(-5, -10)));
            Assert.IsTrue(board.IsValidPosition(new Vector2Int(4, 9)));
        }

        [Test]
        public void IsValidPosition_OutOfBounds_ReturnsFalse()
        {
            Assert.IsFalse(board.IsValidPosition(new Vector2Int(-6, 0)));
            Assert.IsFalse(board.IsValidPosition(new Vector2Int(5, 0)));
            Assert.IsFalse(board.IsValidPosition(new Vector2Int(0, -11)));
            Assert.IsFalse(board.IsValidPosition(new Vector2Int(0, 10)));
        }

        [Test]
        public void IsValidPosition_OccupiedCell_ReturnsFalse()
        {
            // Cell at grid position (0, 0) maps to array index (5, 10)
            board.SetCell(5, 10, 1);

            Assert.IsFalse(board.IsValidPosition(new Vector2Int(0, 0)));
        }

        [Test]
        public void SetCell_GetCell_RoundTrip()
        {
            board.SetCell(3, 7, 1);
            Assert.AreEqual(1, board.GetCell(3, 7));

            board.SetCell(3, 7, 0);
            Assert.AreEqual(0, board.GetCell(3, 7));
        }

        [Test]
        public void FindFullRows_EmptyBoard_ReturnsEmpty()
        {
            List<int> fullRows = board.FindFullRows();
            Assert.AreEqual(0, fullRows.Count);
        }

        [Test]
        public void FindFullRows_SingleFullRow_Detected()
        {
            FillRow(0);

            List<int> fullRows = board.FindFullRows();
            Assert.AreEqual(1, fullRows.Count);
            Assert.AreEqual(0, fullRows[0]);
        }

        [Test]
        public void FindFullRows_MultipleFullRows_SortedBottomToTop()
        {
            FillRow(2);
            FillRow(5);
            FillRow(0);

            List<int> fullRows = board.FindFullRows();
            Assert.AreEqual(3, fullRows.Count);
            Assert.AreEqual(0, fullRows[0]);
            Assert.AreEqual(2, fullRows[1]);
            Assert.AreEqual(5, fullRows[2]);
        }

        [Test]
        public void FindFullRows_PartialRow_Ignored()
        {
            // Fill all but one cell in row 3
            for (int col = 0; col < board.width - 1; col++)
            {
                board.SetCell(col, 3, 1);
            }

            List<int> fullRows = board.FindFullRows();
            Assert.AreEqual(0, fullRows.Count);
        }

        [Test]
        public void ClearAndCollapseRows_ClearsTargetRow()
        {
            FillRowWithBlocks(0);
            ExpectDestroyErrors(board.width);

            List<int> rows = new() { 0 };
            int cleared = board.ClearAndCollapseRows(rows);

            Assert.AreEqual(1, cleared);

            for (int col = 0; col < board.width; col++)
            {
                Assert.AreEqual(0, board.GetCell(col, 0), $"Cell ({col}, 0) should be empty after clear");
            }
        }

        [Test]
        public void ClearAndCollapseRows_ShiftsRowAboveDown()
        {
            FillRowWithBlocks(0);

            // Place a block in row 1 at column 3
            board.SetCell(3, 1, 1);
            GameObject aboveBlock = new GameObject("AboveBlock");
            aboveBlock.transform.SetParent(boardObject.transform, false);
            SetLockedBlock(3, 1, aboveBlock);

            ExpectDestroyErrors(board.width);
            List<int> rows = new() { 0 };
            board.ClearAndCollapseRows(rows);

            // Row 1 block should have shifted down to row 0
            Assert.AreEqual(1, board.GetCell(3, 0), "Block above should shift down");
            Assert.AreEqual(0, board.GetCell(3, 1), "Original position should be empty");

            Object.DestroyImmediate(aboveBlock);
        }

        [Test]
        public void ClearAndCollapseRows_MultipleRows()
        {
            FillRowWithBlocks(0);
            FillRowWithBlocks(1);

            // Place a block in row 2 at column 5
            board.SetCell(5, 2, 1);
            GameObject aboveBlock = new GameObject("AboveBlock");
            aboveBlock.transform.SetParent(boardObject.transform, false);
            SetLockedBlock(5, 2, aboveBlock);

            ExpectDestroyErrors(board.width * 2);
            List<int> rows = new() { 0, 1 };
            int cleared = board.ClearAndCollapseRows(rows);

            Assert.AreEqual(2, cleared);
            Assert.AreEqual(1, board.GetCell(5, 0), "Block from row 2 should drop to row 0");
            Assert.AreEqual(0, board.GetCell(5, 1), "Row 1 should be empty");
            Assert.AreEqual(0, board.GetCell(5, 2), "Row 2 should be empty");

            Object.DestroyImmediate(aboveBlock);
        }

        /// <summary>Fills all cells in a row (array index) without creating GameObjects.</summary>
        private void FillRow(int row)
        {
            for (int col = 0; col < board.width; col++)
            {
                board.SetCell(col, row, 1);
            }
        }

        /// <summary>Fills a row with both cell values and dummy locked block GameObjects.</summary>
        private void FillRowWithBlocks(int row)
        {
            for (int col = 0; col < board.width; col++)
            {
                board.SetCell(col, row, 1);

                GameObject block = new GameObject($"Block_{col}_{row}");
                block.transform.SetParent(boardObject.transform, false);
                block.AddComponent<SpriteRenderer>();
                SetLockedBlock(col, row, block);
            }
        }

        /// <summary>Sets a locked block reference via reflection.</summary>
        private void SetLockedBlock(int col, int row, GameObject block)
        {
            FieldInfo field = typeof(Board).GetField("lockedBlocks", BindingFlags.NonPublic | BindingFlags.Instance);
            GameObject[,] blocks = (GameObject[,])field.GetValue(board);
            blocks[col, row] = block;
        }

        /// <summary>Expects N "Destroy in edit mode" error messages from Unity.</summary>
        private static void ExpectDestroyErrors(int count)
        {
            for (int i = 0; i < count; i++)
            {
                LogAssert.Expect(LogType.Error,
                    new Regex("Destroy may not be called from edit mode"));
            }
        }
    }
}
