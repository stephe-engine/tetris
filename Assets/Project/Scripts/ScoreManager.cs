using System;
using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Tracks score, level, play time, and per-game statistics.
    /// Subscribes to <see cref="GameManager"/> C# events and fires its own
    /// events so the HUD and game-over overlay can stay in sync without
    /// polling.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        /// <summary>Reference to the GameManager whose events drive scoring.</summary>
        [SerializeField] private GameManager gameManager;

        /// <summary>Fired whenever the score changes. Parameter is the new score.</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>Fired whenever the level changes. Parameter is the new level.</summary>
        public event Action<int> OnLevelChanged;

        /// <summary>
        /// Fired whenever lines are cleared or the level changes.
        /// Parameters are (totalLines, linesInCurrentLevel).
        /// </summary>
        public event Action<int, int> OnLinesChanged;

        /// <summary>Current score.</summary>
        public int Score => stats.Score;

        /// <summary>Current level (starts at 1).</summary>
        public int Level => stats.Level;

        /// <summary>Total lines cleared this game.</summary>
        public int TotalLines => stats.TotalLines;

        /// <summary>Lines cleared toward the next level (0–9).</summary>
        public int LinesInCurrentLevel => stats.LinesInCurrentLevel;

        /// <summary>Snapshot of all per-game statistics.</summary>
        public GameStats CurrentStats => stats;

        private GameStats stats = new GameStats();
        private int combo;
        private bool lastClearWasTetris;
        private bool isPaused;
        private bool isGameOver;
        private float levelTimer;

        // Base score per clear type (multiplied by current level)
        private const int ScoreSingle = 100;
        private const int ScoreDouble = 300;
        private const int ScoreTriple = 500;
        private const int ScoreTetris = 800;
        private const int ScoreBackToBackBonus = 400;
        private const int ScoreComboBonus = 50;

        private void OnEnable()
        {
            gameManager.OnGameStart  += HandleGameStart;
            gameManager.OnGameOver   += HandleGameOver;
            gameManager.OnPaused     += HandlePaused;
            gameManager.OnResumed    += HandleResumed;
            gameManager.OnLineClear  += HandleLineClear;
            gameManager.OnLock       += HandleLock;
        }

        private void OnDisable()
        {
            gameManager.OnGameStart  -= HandleGameStart;
            gameManager.OnGameOver   -= HandleGameOver;
            gameManager.OnPaused     -= HandlePaused;
            gameManager.OnResumed    -= HandleResumed;
            gameManager.OnLineClear  -= HandleLineClear;
            gameManager.OnLock       -= HandleLock;
        }

        private void Update()
        {
            if (isPaused || isGameOver)
                return;

            float dt = Time.deltaTime;
            stats.TotalPlayTime += dt;
            levelTimer += dt;
        }

        private void HandleGameStart()
        {
            stats = new GameStats();
            stats.Level = 1;
            stats.TimePerLevel.Add(0f);

            combo = 0;
            lastClearWasTetris = false;
            isPaused = false;
            isGameOver = false;
            levelTimer = 0f;

            OnScoreChanged?.Invoke(stats.Score);
            OnLevelChanged?.Invoke(stats.Level);
            OnLinesChanged?.Invoke(stats.TotalLines, stats.LinesInCurrentLevel);
        }

        private void HandleGameOver()
        {
            // Bank the current level timer before stopping
            int levelIndex = stats.Level - 1;
            if (levelIndex < stats.TimePerLevel.Count)
                stats.TimePerLevel[levelIndex] = levelTimer;

            isGameOver = true;
        }

        private void HandlePaused()
        {
            // Bank time spent at current level before pausing
            int levelIndex = stats.Level - 1;
            if (levelIndex < stats.TimePerLevel.Count)
                stats.TimePerLevel[levelIndex] += levelTimer;
            levelTimer = 0f;

            isPaused = true;
        }

        private void HandleResumed()
        {
            isPaused = false;
        }

        /// <summary>
        /// Core scoring logic: called each time lines are cleared.
        /// Updates score, combo, back-to-back tracking, line counts, and level.
        /// </summary>
        private void HandleLineClear(int lines)
        {
            // 1. Increment clear-type counter
            switch (lines)
            {
                case 1: stats.Singles++;  break;
                case 2: stats.Doubles++;  break;
                case 3: stats.Triples++;  break;
                case 4: stats.Tetrises++; break;
            }

            // 2. Increment combo counter and track max
            combo++;
            if (combo > stats.MaxCombo)
                stats.MaxCombo = combo;

            // 3. Compute base points
            int basePoints = lines switch
            {
                1 => ScoreSingle,
                2 => ScoreDouble,
                3 => ScoreTriple,
                4 => ScoreTetris,
                _ => 0
            };

            // 4. Back-to-back Tetris bonus
            bool isTetris = lines == 4;
            if (isTetris && lastClearWasTetris)
                basePoints += ScoreBackToBackBonus;
            lastClearWasTetris = isTetris;

            // 5. Add score: base × level + combo bonus
            stats.Score += basePoints * stats.Level + ScoreComboBonus * combo;

            bool levelChanged = false;

            // 6. Advance line counts and level
            stats.TotalLines += lines;
            stats.LinesInCurrentLevel += lines;

            while (stats.LinesInCurrentLevel >= 10)
            {
                stats.LinesInCurrentLevel -= 10;

                // Bank time at the current level before advancing
                int prevIndex = stats.Level - 1;
                if (prevIndex < stats.TimePerLevel.Count)
                    stats.TimePerLevel[prevIndex] += levelTimer;
                levelTimer = 0f;

                stats.Level++;
                stats.TimePerLevel.Add(0f);
                levelChanged = true;
            }

            // 7. Fire events
            OnScoreChanged?.Invoke(stats.Score);
            if (levelChanged)
                OnLevelChanged?.Invoke(stats.Level);
            OnLinesChanged?.Invoke(stats.TotalLines, stats.LinesInCurrentLevel);
        }

        /// <summary>
        /// Called when a piece locks without clearing any lines.
        /// Resets the combo counter.
        /// </summary>
        private void HandleLock()
        {
            // Only reset on the lock event itself; line clears come via HandleLineClear
            // We use a flag so that if a LineClear follows this Lock in the same frame
            // we do NOT reset (LineClear increments combo first).
            // The ordering in LockAndSpawn is: OnLock fires first, then OnLineClear.
            // So we reset combo here unconditionally; HandleLineClear will re-increment it.
            combo = 0;
        }

    }
}
