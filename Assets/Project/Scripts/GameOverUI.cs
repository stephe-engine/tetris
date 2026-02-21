using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.Scripts
{
    /// <summary>
    /// Displays a full-screen game-over overlay when the player tops out.
    /// Subscribes to <see cref="GameManager.OnGameOver"/> in Awake so the
    /// subscription persists even when the panel is hidden via SetActive(false).
    /// Supports mouse clicks, Up/Down arrow navigation, and Enter/Space to confirm.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        /// <summary>Reference to the GameManager that fires game events.</summary>
        [SerializeField] private GameManager gameManager;

        /// <summary>Root GameObject of the overlay panel (same as this GameObject).</summary>
        [SerializeField] private GameObject panel;

        /// <summary>CanvasGroup on the panel used to drive the fade animation.</summary>
        [SerializeField] private CanvasGroup canvasGroup;

        /// <summary>Button that restarts the game.</summary>
        [SerializeField] private Button restartButton;

        /// <summary>Button that quits the application.</summary>
        [SerializeField] private Button quitButton;

        /// <summary>Duration in seconds for the fade-in animation.</summary>
        [SerializeField] private float fadeDuration = 0.5f;

        private static readonly Color NormalColor   = new(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color SelectedColor = new(0.75f, 0.65f, 0.20f, 1f); // gold highlight

        private Button[] buttons;
        private int selectedIndex;

        private void Awake()
        {
            // Subscribe here rather than OnEnable/OnDisable so the listener
            // remains active while the panel is hidden (panel.SetActive(false)
            // would otherwise trigger OnDisable and drop the subscription).
            gameManager.OnGameOver += HandleGameOver;
        }

        private void OnDestroy()
        {
            gameManager.OnGameOver -= HandleGameOver;
        }

        private void Start()
        {
            buttons = new Button[] { restartButton, quitButton };
            restartButton.onClick.AddListener(OnRestartClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            panel.SetActive(false);
        }

        // Update only runs while the panel is active (SetActive(false) stops it).
        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.upArrowKey.wasPressedThisFrame)
                Navigate(-1);
            else if (kb.downArrowKey.wasPressedThisFrame)
                Navigate(1);

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame
                || kb.spaceKey.wasPressedThisFrame)
                buttons[selectedIndex].onClick.Invoke();
        }

        private void Navigate(int delta)
        {
            selectedIndex = (selectedIndex + delta + buttons.Length) % buttons.Length;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Image img = buttons[i].GetComponent<Image>();
                img.color = i == selectedIndex ? SelectedColor : NormalColor;
            }
        }

        private void HandleGameOver()
        {
            panel.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            selectedIndex = 0;
            RefreshSelection();
            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
        }

        private void OnRestartClicked()
        {
            panel.SetActive(false);
            gameManager.Restart();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
