using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Project.Scripts.EditMode.Tests
{
    /// <summary>
    /// Tests for <see cref="ScoreManager"/> scoring, combo, back-to-back,
    /// level-up, and event-firing logic. Private handler methods are invoked
    /// via reflection so tests remain isolated without a real GameManager.
    /// </summary>
    public class ScoreManagerTests
    {
        private GameObject scoreManagerObject;
        private ScoreManager scoreManager;

        [SetUp]
        public void SetUp()
        {
            scoreManagerObject = new GameObject("ScoreManager");
            scoreManager = scoreManagerObject.AddComponent<ScoreManager>();
            InvokeHandler("HandleGameStart");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(scoreManagerObject);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void InvokeHandler(string name, params object[] args)
        {
            typeof(ScoreManager)
                .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(scoreManager, args);
        }

        private GameStats GetStats()
        {
            return (GameStats)typeof(ScoreManager)
                .GetField("stats", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(scoreManager);
        }

        private int GetCombo()
        {
            return (int)typeof(ScoreManager)
                .GetField("combo", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(scoreManager);
        }

        // ── HandleGameStart ──────────────────────────────────────────────────

        [Test]
        public void HandleGameStart_ResetsAllStatsToInitialValues()
        {
            // Dirty state before resetting
            InvokeHandler("HandleLineClear", 4);
            InvokeHandler("HandleLineClear", 1);

            InvokeHandler("HandleGameStart");

            GameStats stats = GetStats();
            Assert.AreEqual(0, scoreManager.Score);
            Assert.AreEqual(1, scoreManager.Level);
            Assert.AreEqual(0, scoreManager.TotalLines);
            Assert.AreEqual(0, scoreManager.LinesInCurrentLevel);
            Assert.AreEqual(0, stats.Singles);
            Assert.AreEqual(0, stats.Doubles);
            Assert.AreEqual(0, stats.Triples);
            Assert.AreEqual(0, stats.Tetrises);
            Assert.AreEqual(0, stats.MaxCombo);
        }

        [Test]
        public void HandleGameStart_FiresOnScoreChanged()
        {
            int firedValue = -1;
            scoreManager.OnScoreChanged += v => firedValue = v;

            InvokeHandler("HandleGameStart");

            Assert.AreEqual(0, firedValue);
        }

        [Test]
        public void HandleGameStart_FiresOnLinesChanged()
        {
            int firedTotal = -1;
            int firedInLevel = -1;
            scoreManager.OnLinesChanged += (total, inLevel) => { firedTotal = total; firedInLevel = inLevel; };

            InvokeHandler("HandleGameStart");

            Assert.AreEqual(0, firedTotal);
            Assert.AreEqual(0, firedInLevel);
        }

        // ── Base scoring per clear type ──────────────────────────────────────
        // Formula: basePoints * level + 50 * combo. Level=1, first clear → combo=1.

        [Test]
        public void HandleLineClear_Single_CorrectScore()
        {
            InvokeHandler("HandleLineClear", 1); // 100*1 + 50*1 = 150

            Assert.AreEqual(150, scoreManager.Score);
        }

        [Test]
        public void HandleLineClear_Double_CorrectScore()
        {
            InvokeHandler("HandleLineClear", 2); // 300*1 + 50*1 = 350

            Assert.AreEqual(350, scoreManager.Score);
        }

        [Test]
        public void HandleLineClear_Triple_CorrectScore()
        {
            InvokeHandler("HandleLineClear", 3); // 500*1 + 50*1 = 550

            Assert.AreEqual(550, scoreManager.Score);
        }

        [Test]
        public void HandleLineClear_Tetris_CorrectScore()
        {
            InvokeHandler("HandleLineClear", 4); // 800*1 + 50*1 = 850

            Assert.AreEqual(850, scoreManager.Score);
        }

        // ── Counter and total line tracking ─────────────────────────────────

        [Test]
        public void HandleLineClear_UpdatesClearTypeCounters()
        {
            InvokeHandler("HandleLineClear", 1);
            Assert.AreEqual(1, GetStats().Singles, "Singles");

            InvokeHandler("HandleLineClear", 2);
            Assert.AreEqual(1, GetStats().Doubles, "Doubles");

            InvokeHandler("HandleLineClear", 3);
            Assert.AreEqual(1, GetStats().Triples, "Triples");

            InvokeHandler("HandleLineClear", 4);
            Assert.AreEqual(1, GetStats().Tetrises, "Tetrises");
        }

        [Test]
        public void HandleLineClear_UpdatesTotalLines()
        {
            InvokeHandler("HandleLineClear", 2);
            InvokeHandler("HandleLineClear", 3);

            Assert.AreEqual(5, scoreManager.TotalLines);
        }

        // ── Combo ────────────────────────────────────────────────────────────

        [Test]
        public void HandleLineClear_ComboIncrementsOnConsecutiveClears()
        {
            InvokeHandler("HandleLineClear", 1);
            InvokeHandler("HandleLineClear", 1);

            Assert.AreEqual(2, GetCombo());
        }

        [Test]
        public void HandleLineClear_ComboBonusAppliedToScore()
        {
            // First clear: combo=1, score = 100*1 + 50*1 = 150
            InvokeHandler("HandleLineClear", 1);
            // Second clear: combo=2, score += 100*1 + 50*2 = 200 → total 350
            InvokeHandler("HandleLineClear", 1);

            Assert.AreEqual(350, scoreManager.Score);
        }

        [Test]
        public void HandleLineClear_MaxComboTracked()
        {
            InvokeHandler("HandleLineClear", 1); // combo=1, MaxCombo=1
            InvokeHandler("HandleLineClear", 1); // combo=2, MaxCombo=2
            InvokeHandler("HandleLock");          // combo=0, MaxCombo stays 2
            InvokeHandler("HandleLineClear", 1); // combo=1, MaxCombo stays 2

            Assert.AreEqual(2, GetStats().MaxCombo);
        }

        [Test]
        public void HandleLock_ResetsCombo()
        {
            InvokeHandler("HandleLineClear", 1);
            InvokeHandler("HandleLineClear", 1); // combo=2

            InvokeHandler("HandleLock");

            Assert.AreEqual(0, GetCombo());
        }

        // ── Back-to-back ─────────────────────────────────────────────────────

        [Test]
        public void HandleLineClear_BackToBackTetris_AddsBonus()
        {
            // First Tetris: combo=1, score = 800*1 + 50*1 = 850
            InvokeHandler("HandleLineClear", 4);
            // Second Tetris (B2B): combo=2, score += (800+400)*1 + 50*2 = 1300 → total 2150
            InvokeHandler("HandleLineClear", 4);

            Assert.AreEqual(2150, scoreManager.Score);
        }

        [Test]
        public void HandleLineClear_NonTetrisBetweenTetrises_BreaksBackToBack()
        {
            // Tetris: combo=1, score = 800+50 = 850, lastClearWasTetris=true
            InvokeHandler("HandleLineClear", 4);
            // Single (breaks B2B): combo=2, score += 100*1 + 50*2 = 200 → 1050, lastClearWasTetris=false
            InvokeHandler("HandleLineClear", 1);
            // Tetris (no B2B): combo=3, score += 800*1 + 50*3 = 950 → 2000
            InvokeHandler("HandleLineClear", 4);

            Assert.AreEqual(2000, scoreManager.Score);
        }

        // ── Level-up ─────────────────────────────────────────────────────────

        [Test]
        public void HandleLineClear_At10Lines_IncrementsLevel()
        {
            GetStats().LinesInCurrentLevel = 8;
            InvokeHandler("HandleLineClear", 4); // 8+4=12 → level up, LinesInCurrentLevel=2

            Assert.AreEqual(2, scoreManager.Level);
        }

        [Test]
        public void HandleLineClear_LinesInCurrentLevel_RollsOver()
        {
            GetStats().LinesInCurrentLevel = 8;
            InvokeHandler("HandleLineClear", 3); // 8+3=11 → level up, LinesInCurrentLevel=1

            Assert.AreEqual(1, scoreManager.LinesInCurrentLevel);
            Assert.AreEqual(2, scoreManager.Level);
        }

        [Test]
        public void HandleLineClear_ScoreScalesWithLevel()
        {
            GetStats().Level = 2;
            InvokeHandler("HandleLineClear", 1); // combo=1, score = 100*2 + 50*1 = 250

            Assert.AreEqual(250, scoreManager.Score);
        }

        // ── Events ───────────────────────────────────────────────────────────

        [Test]
        public void HandleLineClear_FiresOnLinesChangedWithCorrectValues()
        {
            int firedTotal = -1;
            int firedInLevel = -1;
            scoreManager.OnLinesChanged += (total, inLevel) => { firedTotal = total; firedInLevel = inLevel; };

            InvokeHandler("HandleLineClear", 3);

            Assert.AreEqual(3, firedTotal);
            Assert.AreEqual(3, firedInLevel);
        }

        [Test]
        public void HandleLineClear_LevelUp_FiresOnLevelChanged()
        {
            int firedLevel = -1;
            scoreManager.OnLevelChanged += v => firedLevel = v;

            GetStats().LinesInCurrentLevel = 8;
            InvokeHandler("HandleLineClear", 4); // 8+4=12 → level up to 2

            Assert.AreEqual(2, firedLevel);
        }
    }
}
