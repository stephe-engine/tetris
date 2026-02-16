using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Identifies the seven standard Tetris piece shapes.
    /// </summary>
    public enum Tetromino
    {
        I,
        O,
        T,
        S,
        Z,
        J,
        L,
    }

    /// <summary>
    /// Static lookup tables for tetromino shapes and spawn configuration.
    /// Cell offsets are relative to the piece's pivot point.
    /// </summary>
    public static class Data
    {
        /// <summary>
        /// Grid position where new pieces spawn, relative to the board's local origin.
        /// Placed near the top-center of the board with headroom for rotation.
        /// </summary>
        public static readonly Vector2Int SpawnPosition = new Vector2Int(0, 8);

        /// <summary>
        /// Cell offsets for each tetromino type in its default (spawn) rotation.
        /// Each piece is defined by exactly 4 offsets from the pivot.
        /// </summary>
        public static readonly Dictionary<Tetromino, Vector2Int[]> Cells = new Dictionary<Tetromino, Vector2Int[]>()
        {
            {
                Tetromino.I,
                new Vector2Int[]
                    { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }
            },
            {
                Tetromino.O,
                new Vector2Int[]
                    { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }
            },
            {
                Tetromino.T,
                new Vector2Int[]
                    { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) }
            },
            {
                Tetromino.S,
                new Vector2Int[]
                    { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }
            },
            {
                Tetromino.Z,
                new Vector2Int[]
                    { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) }
            },
            {
                Tetromino.J,
                new Vector2Int[]
                    { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) }
            },
            {
                Tetromino.L,
                new Vector2Int[]
                    { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }
            },
        };

        /// <summary>
        /// Precomputed cell offsets for all 4 rotation states of each tetromino.
        /// State 0 matches <see cref="Cells"/>. Subsequent states are derived by
        /// applying the clockwise rotation formula <c>(x,y) → (y,-x)</c>.
        /// </summary>
        public static readonly Dictionary<Tetromino, Vector2Int[][]> AllRotations = BuildAllRotations();

        /// <summary>
        /// SRS wall kick offset tables. For each piece type, maps a
        /// <c>(fromRotation, toRotation)</c> pair to 5 kick offsets to try in order.
        /// Test 0 is always <c>(0,0)</c>. The I-piece has its own table;
        /// J, L, S, T, Z share a common table.
        /// </summary>
        public static readonly Dictionary<Tetromino, Dictionary<(int from, int to), Vector2Int[]>> WallKicks = BuildWallKicks();

        private static Dictionary<Tetromino, Vector2Int[][]> BuildAllRotations()
        {
            Dictionary<Tetromino, Vector2Int[][]> result = new();

            foreach (KeyValuePair<Tetromino, Vector2Int[]> entry in Cells)
            {
                Vector2Int[][] rotations = new Vector2Int[4][];
                rotations[0] = entry.Value;

                for (int r = 1; r < 4; r++)
                {
                    Vector2Int[] prev = rotations[r - 1];
                    Vector2Int[] next = new Vector2Int[prev.Length];

                    for (int i = 0; i < prev.Length; i++)
                    {
                        // CW rotation: (x,y) → (y,-x)
                        next[i] = new Vector2Int(prev[i].y, -prev[i].x);
                    }

                    rotations[r] = next;
                }

                result[entry.Key] = rotations;
            }

            return result;
        }

        private static Dictionary<Tetromino, Dictionary<(int from, int to), Vector2Int[]>> BuildWallKicks()
        {
            Dictionary<Tetromino, Dictionary<(int from, int to), Vector2Int[]>> result = new();

            // JLSTZ kick data (CW transitions)
            Dictionary<(int from, int to), Vector2Int[]> jlstzKicks = new()
            {
                { (0, 1), new Vector2Int[] { new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2) } },
                { (1, 2), new Vector2Int[] { new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2) } },
                { (2, 3), new Vector2Int[] { new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2) } },
                { (3, 0), new Vector2Int[] { new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2) } },
            };

            // CCW transitions: negate the offsets of the reverse CW transition
            AddCcwKicks(jlstzKicks);

            // I-piece kick data (CW transitions)
            Dictionary<(int from, int to), Vector2Int[]> iKicks = new()
            {
                { (0, 1), new Vector2Int[] { new(0, 0), new(-2, 0), new(1, 0), new(-2, -1), new(1, 2) } },
                { (1, 2), new Vector2Int[] { new(0, 0), new(-1, 0), new(2, 0), new(-1, 2), new(2, -1) } },
                { (2, 3), new Vector2Int[] { new(0, 0), new(2, 0), new(-1, 0), new(2, 1), new(-1, -2) } },
                { (3, 0), new Vector2Int[] { new(0, 0), new(1, 0), new(-2, 0), new(1, -2), new(-2, 1) } },
            };

            AddCcwKicks(iKicks);

            result[Tetromino.I] = iKicks;
            result[Tetromino.O] = jlstzKicks; // O never rotates, but entry avoids null checks
            result[Tetromino.T] = jlstzKicks;
            result[Tetromino.S] = jlstzKicks;
            result[Tetromino.Z] = jlstzKicks;
            result[Tetromino.J] = jlstzKicks;
            result[Tetromino.L] = jlstzKicks;

            return result;
        }

        /// <summary>
        /// Generates CCW kick entries by negating offsets of the reverse CW transition.
        /// For example, CCW 1→0 uses negated offsets from CW 0→1.
        /// </summary>
        private static void AddCcwKicks(Dictionary<(int from, int to), Vector2Int[]> kicks)
        {
            List<((int, int) key, Vector2Int[] value)> ccwEntries = new();

            foreach (KeyValuePair<(int from, int to), Vector2Int[]> entry in kicks)
            {
                (int from, int to) cwKey = entry.Key;
                (int from, int to) ccwKey = (cwKey.to, cwKey.from);
                Vector2Int[] cwOffsets = entry.Value;
                Vector2Int[] ccwOffsets = new Vector2Int[cwOffsets.Length];

                for (int i = 0; i < cwOffsets.Length; i++)
                {
                    ccwOffsets[i] = new Vector2Int(-cwOffsets[i].x, -cwOffsets[i].y);
                }

                ccwEntries.Add((ccwKey, ccwOffsets));
            }

            foreach (((int, int) key, Vector2Int[] value) entry in ccwEntries)
            {
                kicks[entry.key] = entry.value;
            }
        }
    }
}
