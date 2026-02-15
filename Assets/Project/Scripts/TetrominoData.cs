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
    }
}
