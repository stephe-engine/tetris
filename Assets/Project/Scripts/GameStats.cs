using System.Collections.Generic;

namespace Project.Scripts
{
    /// <summary>
    /// Plain data container holding all per-game statistics.
    /// Owned and populated by <see cref="ScoreManager"/> and passed
    /// to the game-over overlay for display.
    /// </summary>
    public class GameStats
    {
        /// <summary>Total score accumulated this game.</summary>
        public int Score;

        /// <summary>Current level (starts at 1).</summary>
        public int Level;

        /// <summary>Total lines cleared this game.</summary>
        public int TotalLines;

        /// <summary>Lines cleared in the current level (0–9).</summary>
        public int LinesInCurrentLevel;

        /// <summary>Number of single-line clears.</summary>
        public int Singles;

        /// <summary>Number of double-line clears.</summary>
        public int Doubles;

        /// <summary>Number of triple-line clears.</summary>
        public int Triples;

        /// <summary>Number of four-line clears (Tetrises).</summary>
        public int Tetrises;

        /// <summary>Highest combo chain reached this game.</summary>
        public int MaxCombo;

        /// <summary>Total play time in seconds (paused time excluded).</summary>
        public float TotalPlayTime;

        /// <summary>Seconds spent at each level index (index 0 = level 1).</summary>
        public List<float> TimePerLevel = new List<float>();
    }
}
