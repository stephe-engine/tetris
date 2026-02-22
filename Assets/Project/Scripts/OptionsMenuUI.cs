using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts
{
    /// <summary>
    /// Controls the options sub-menu panel. Accessed from the pause menu.
    /// Provides a Sound button (opens the AudioPanel) and a disabled Keybindings placeholder.
    /// Keyboard navigation mirrors <see cref="PauseMenuUI"/>.
    /// </summary>
    public class OptionsMenuUI : MonoBehaviour
    {
        /// <summary>Reference to the sound manager for menu SFX playback.</summary>
        [SerializeField] private SoundManager soundManager;

        /// <summary>Sound settings asset used to retrieve menu SFX clips.</summary>
        [SerializeField] private SoundSettings soundSettings;

        /// <summary>Root GameObject of this panel (self).</summary>
        [SerializeField] private GameObject panel;

        /// <summary>The Sound button that opens the AudioPanel.</summary>
        [SerializeField] private Button soundButton;

        /// <summary>The Keybindings placeholder button (non-interactable).</summary>
        [SerializeField] private Button keybindingsButton;

        /// <summary>The AudioPanel child GameObject shown when Sound is selected.</summary>
        [SerializeField] private GameObject audioPanel;

        [SerializeField] private Color selectedColor   = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color unselectedColor = new Color(0.25f, 0.25f, 0.25f);
        [SerializeField] private Color disabledColor   = new Color(0.15f, 0.15f, 0.15f);

        private Button[] buttons;
        private int selectedIndex;
        private bool isOpen;
        private bool audioPanelOpen;
        private Action onBack;

        /// <summary>True while this options panel (or its AudioPanel child) is visible.</summary>
        public bool IsOpen => isOpen;

        private void Awake()
        {
            buttons = new Button[] { soundButton, keybindingsButton };

            soundButton.onClick.AddListener(OnSoundClicked);

            foreach (Button btn in buttons)
                btn.transition = Selectable.Transition.None;

            keybindingsButton.interactable = false;
            Image keybindImage = keybindingsButton.GetComponent<Image>();
            if (keybindImage)
                keybindImage.color = disabledColor;
        }

        private void Update()
        {
            if (!isOpen)
                return;

            // While AudioPanel is open, SoundPanelEscape handles its Escape
            if (audioPanelOpen)
                return;

            if (UnityEngine.InputSystem.Keyboard.current == null)
                return;

            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;

            if (kb.upArrowKey.wasPressedThisFrame)
                Navigate(-1);
            else if (kb.downArrowKey.wasPressedThisFrame)
                Navigate(1);

            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                if (buttons[selectedIndex].interactable)
                    buttons[selectedIndex].onClick.Invoke();
            }

            if (kb.escapeKey.wasPressedThisFrame)
                GoBack();
        }

        /// <summary>
        /// Opens the options panel and registers the callback to invoke on back.
        /// </summary>
        /// <param name="backCallback">Called when the player navigates back to the pause menu.</param>
        public void Open(Action backCallback)
        {
            isOpen = true;
            onBack = backCallback;
            selectedIndex = 0;
            panel.SetActive(true);
            audioPanel.SetActive(false);
            audioPanelOpen = false;
            RefreshSelection();
        }

        /// <summary>
        /// Hides both the options panel and AudioPanel without invoking the back callback.
        /// Called when the pause menu itself is closed.
        /// </summary>
        public void ForceClose()
        {
            isOpen = false;
            audioPanelOpen = false;
            panel.SetActive(false);
            audioPanel.SetActive(false);
        }

        /// <summary>
        /// Called by <see cref="SoundPanelEscape"/> when the player presses Escape on the AudioPanel.
        /// </summary>
        public void OnAudioPanelBack()
        {
            audioPanelOpen = false;
            audioPanel.SetActive(false);
            RefreshSelection();
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
                if (!img)
                    continue;

                if (!buttons[i].interactable)
                    img.color = disabledColor;
                else
                    img.color = (i == selectedIndex) ? selectedColor : unselectedColor;
            }
        }

        private void OnSoundClicked()
        {
            if (soundManager && soundSettings)
                soundManager.PlayMenuSfx(soundSettings.sfxMenuSelect);
            audioPanelOpen = true;
            audioPanel.SetActive(true);
        }

        private void GoBack()
        {
            if (soundManager && soundSettings)
                soundManager.PlayMenuSfx(soundSettings.sfxMenuSelect);
            panel.SetActive(false);
            isOpen = false;
            onBack?.Invoke();
        }
    }
}
