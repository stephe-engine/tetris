using TMPro;
using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Displays the current level, total lines cleared, and lines remaining
    /// until the next level. Subscribes to <see cref="ScoreManager"/> events
    /// to stay in sync without polling.
    /// </summary>
    public class HudUI : MonoBehaviour
    {
        /// <summary>Reference to the ScoreManager that fires scoring events.</summary>
        [SerializeField] private ScoreManager scoreManager;

        /// <summary>Label showing the current level number.</summary>
        [SerializeField] private TextMeshProUGUI levelText;

        /// <summary>Label showing total lines cleared.</summary>
        [SerializeField] private TextMeshProUGUI totalLinesText;

        /// <summary>Label showing lines remaining until the next level.</summary>
        [SerializeField] private TextMeshProUGUI nextLevelText;

        /// <summary>Label showing the current score.</summary>
        [SerializeField] private TextMeshProUGUI scoreText;

        private void OnEnable()
        {
            scoreManager.OnScoreChanged += HandleScoreChanged;
            scoreManager.OnLevelChanged += HandleLevelChanged;
            scoreManager.OnLinesChanged += HandleLinesChanged;
        }

        private void OnDisable()
        {
            scoreManager.OnScoreChanged -= HandleScoreChanged;
            scoreManager.OnLevelChanged -= HandleLevelChanged;
            scoreManager.OnLinesChanged -= HandleLinesChanged;
        }

        private void Start()
        {
            Refresh();
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreText)
                scoreText.text = $"Score  {score:N0}";
        }

        private void HandleLevelChanged(int level)
        {
            if (levelText)
                levelText.text = $"Level  {level}";
        }

        private void HandleLinesChanged(int totalLines, int linesInCurrentLevel)
        {
            if (totalLinesText)
                totalLinesText.text = $"Lines  {totalLines}";
            if (nextLevelText)
                nextLevelText.text = $"Next   {10 - linesInCurrentLevel}";
        }

        /// <summary>
        /// Reads the current state from <see cref="ScoreManager"/> and updates
        /// all labels. Called in <see cref="Start"/> to initialise before any
        /// events fire.
        /// </summary>
        public void Refresh()
        {
            HandleScoreChanged(scoreManager.Score);
            HandleLevelChanged(scoreManager.Level);
            HandleLinesChanged(scoreManager.TotalLines, scoreManager.LinesInCurrentLevel);
        }
    }
}
