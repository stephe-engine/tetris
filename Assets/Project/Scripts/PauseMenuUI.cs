using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts
{
    /// <summary>
    /// Controls the pause menu overlay. Opened by <see cref="InputHandler"/> when Escape is pressed
    /// during gameplay. Provides Resume, Options, and Quit buttons with keyboard navigation.
    /// Delegates to <see cref="OptionsMenuUI"/> for the options sub-panel.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        /// <summary>Reference to the game manager used to pause and resume gameplay.</summary>
        [SerializeField] private GameManager gameManager;

        /// <summary>Reference to the sound manager for menu SFX playback.</summary>
        [SerializeField] private SoundManager soundManager;

        /// <summary>Sound settings asset used to retrieve menu SFX clips.</summary>
        [SerializeField] private SoundSettings soundSettings;

        /// <summary>Root GameObject of this panel (self).</summary>
        [SerializeField] private GameObject panel;

        /// <summary>CanvasGroup on the panel root, used for fade-in.</summary>
        [SerializeField] private CanvasGroup canvasGroup;

        /// <summary>The Resume button.</summary>
        [SerializeField] private Button resumeButton;

        /// <summary>The Options button.</summary>
        [SerializeField] private Button optionsButton;

        /// <summary>The Quit button.</summary>
        [SerializeField] private Button quitButton;

        /// <summary>The options sub-menu panel controller.</summary>
        [SerializeField] private OptionsMenuUI optionsMenuUI;

        /// <summary>Duration in seconds for the panel fade-in.</summary>
        [SerializeField] private float fadeDuration = 0.3f;

        [SerializeField] private Color selectedColor   = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color unselectedColor = new Color(0.25f, 0.25f, 0.25f);

        private Button[] buttons;
        private int selectedIndex;
        private bool isOpen;

        /// <summary>True while the pause menu (or its sub-panels) is visible.</summary>
        public bool IsOpen => isOpen;

        private void Awake()
        {
            buttons = new Button[] { resumeButton, optionsButton, quitButton };

            resumeButton.onClick.AddListener(OnResumeClicked);
            optionsButton.onClick.AddListener(OnOptionsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);

            foreach (Button btn in buttons)
                btn.transition = Selectable.Transition.None;
        }

        private void Update()
        {
            if (!isOpen)
                return;

            // While options sub-panel is open, let it handle its own input
            if (optionsMenuUI.IsOpen)
                return;

            if (UnityEngine.InputSystem.Keyboard.current == null)
                return;

            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;

            if (kb.upArrowKey.wasPressedThisFrame)
                Navigate(-1);
            else if (kb.downArrowKey.wasPressedThisFrame)
                Navigate(1);

            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                buttons[selectedIndex].onClick.Invoke();

            if (kb.escapeKey.wasPressedThisFrame)
                Close();
        }

        /// <summary>
        /// Opens the pause menu: activates the panel, fades it in, and pauses gameplay.
        /// </summary>
        public void Open()
        {
            if (gameManager.IsGameOver)
                return;

            isOpen = true;
            selectedIndex = 0;
            panel.SetActive(true);
            canvasGroup.alpha = 0f;
            RefreshSelection();
            StartCoroutine(FadeIn());
            gameManager.Pause();
        }

        /// <summary>
        /// Closes the pause menu, force-closes any sub-panels, and resumes gameplay.
        /// </summary>
        private void Close()
        {
            isOpen = false;
            optionsMenuUI.ForceClose();
            panel.SetActive(false);
            gameManager.Resume();
        }

        private void Navigate(int direction)
        {
            selectedIndex = (selectedIndex + direction + buttons.Length) % buttons.Length;
            if (soundManager && soundSettings)
                soundManager.PlayMenuSfx(soundSettings.sfxMenuNavigate);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Image img = buttons[i].GetComponent<Image>();
                if (img)
                    img.color = (i == selectedIndex) ? selectedColor : unselectedColor;
            }
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
        }

        private void OnResumeClicked()
        {
            if (soundManager && soundSettings)
                soundManager.PlayMenuSfx(soundSettings.sfxMenuSelect);
            Close();
        }

        private void OnOptionsClicked()
        {
            if (soundManager && soundSettings)
                soundManager.PlayMenuSfx(soundSettings.sfxMenuSelect);
            panel.SetActive(false);
            optionsMenuUI.Open(OnOptionsBack);
        }

        private void OnOptionsBack()
        {
            panel.SetActive(true);
            selectedIndex = 1;
            RefreshSelection();
        }

        private void OnQuitClicked()
        {
            if (soundManager && soundSettings)
                soundManager.PlayMenuSfx(soundSettings.sfxMenuSelect);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
