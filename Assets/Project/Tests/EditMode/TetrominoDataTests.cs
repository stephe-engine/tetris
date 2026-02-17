using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Project.Scripts.EditMode.Tests
{
    /// <summary>
    /// Tests for <see cref="Data"/> static lookup tables and <see cref="Tetromino"/> enum.
    /// </summary>
    public class TetrominoDataTests
    {
        private static readonly Tetromino[] AllTypes =
            { Tetromino.I, Tetromino.O, Tetromino.T, Tetromino.S, Tetromino.Z, Tetromino.J, Tetromino.L };

        [Test]
        public void Cells_AllSevenTypes_HaveExactly4Cells()
        {
            foreach (Tetromino type in AllTypes)
            {
                Assert.IsTrue(Data.Cells.ContainsKey(type), $"Missing cells for {type}");
                Assert.AreEqual(4, Data.Cells[type].Length, $"{type} should have 4 cells");
            }
        }

        [Test]
        public void AllRotations_Has4StatesPerType_EachWith4Cells()
        {
            foreach (Tetromino type in AllTypes)
            {
                Assert.IsTrue(Data.AllRotations.ContainsKey(type), $"Missing rotations for {type}");
                Vector2Int[][] rotations = Data.AllRotations[type];
                Assert.AreEqual(4, rotations.Length, $"{type} should have 4 rotation states");

                for (int r = 0; r < 4; r++)
                {
                    Assert.AreEqual(4, rotations[r].Length, $"{type} rotation {r} should have 4 cells");
                }
            }
        }

        [Test]
        public void AllRotations_State0_MatchesCells()
        {
            foreach (Tetromino type in AllTypes)
            {
                Vector2Int[] baseCells = Data.Cells[type];
                Vector2Int[] rotation0 = Data.AllRotations[type][0];
                Assert.AreEqual(baseCells, rotation0, $"{type} rotation 0 should match base cells");
            }
        }

        [Test]
        public void AllRotations_CWFormula_YNegX()
        {
            foreach (Tetromino type in AllTypes)
            {
                Vector2Int[][] rotations = Data.AllRotations[type];

                for (int r = 0; r < 3; r++)
                {
                    Vector2Int[] current = rotations[r];
                    Vector2Int[] next = rotations[r + 1];

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int expected = new Vector2Int(current[i].y, -current[i].x);
                        Assert.AreEqual(expected, next[i],
                            $"{type} rotation {r}->{r + 1}, cell {i}: expected ({expected.x},{expected.y})");
                    }
                }
            }
        }

        [Test]
        public void OPiece_AllRotations_CWFormulaApplied()
        {
            // O-piece data applies the CW formula like all other pieces.
            // The game skips rotation for O-piece at runtime (tested in GameManagerTests).
            // Here we verify the formula is consistently applied.
            Vector2Int[][] rotations = Data.AllRotations[Tetromino.O];

            for (int r = 0; r < 3; r++)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int current = rotations[r][i];
                    Vector2Int expected = new Vector2Int(current.y, -current.x);
                    Assert.AreEqual(expected, rotations[r + 1][i],
                        $"O-piece rotation {r}->{r + 1} cell {i} should follow CW formula");
                }
            }
        }

        [Test]
        public void WallKicks_AllTypes_HaveEntries()
        {
            foreach (Tetromino type in AllTypes)
            {
                Assert.IsTrue(Data.WallKicks.ContainsKey(type), $"Missing wall kicks for {type}");
                Assert.IsTrue(Data.WallKicks[type].Count > 0, $"{type} should have wall kick entries");
            }
        }

        [Test]
        public void WallKicks_Test0_IsAlwaysZero()
        {
            foreach (Tetromino type in AllTypes)
            {
                foreach (KeyValuePair<(int from, int to), Vector2Int[]> entry in Data.WallKicks[type])
                {
                    Assert.AreEqual(5, entry.Value.Length,
                        $"{type} kick ({entry.Key.from}->{entry.Key.to}) should have 5 offsets");
                    Assert.AreEqual(Vector2Int.zero, entry.Value[0],
                        $"{type} kick ({entry.Key.from}->{entry.Key.to}) test 0 should be (0,0)");
                }
            }
        }

        [Test]
        public void WallKicks_CCWEntries_AreNegationsOfCW()
        {
            foreach (Tetromino type in AllTypes)
            {
                Dictionary<(int from, int to), Vector2Int[]> kicks = Data.WallKicks[type];

                // Check CW transitions 0->1, 1->2, 2->3, 3->0 have matching CCW
                (int, int)[] cwTransitions = { (0, 1), (1, 2), (2, 3), (3, 0) };

                foreach ((int from, int to) cw in cwTransitions)
                {
                    (int from, int to) ccw = (cw.to, cw.from);
                    Assert.IsTrue(kicks.ContainsKey(ccw),
                        $"{type} missing CCW kick ({ccw.from}->{ccw.to})");

                    Vector2Int[] cwOffsets = kicks[cw];
                    Vector2Int[] ccwOffsets = kicks[ccw];

                    for (int i = 0; i < cwOffsets.Length; i++)
                    {
                        Vector2Int expected = new Vector2Int(-cwOffsets[i].x, -cwOffsets[i].y);
                        Assert.AreEqual(expected, ccwOffsets[i],
                            $"{type} CCW ({ccw.from}->{ccw.to}) offset {i} should negate CW");
                    }
                }
            }
        }
    }
}
