using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts
{
    /// <summary>
    /// Shim attached to the AudioPanel. Routes Escape back to <see cref="OptionsMenuUI"/>
    /// so the player can close the sound panel with the keyboard. Only active while
    /// the AudioPanel is active (Unity's SetActive semantics).
    /// </summary>
    public class SoundPanelEscape : MonoBehaviour
    {
        /// <summary>The options menu that owns this audio panel.</summary>
        [SerializeField] private OptionsMenuUI optionsMenuUI;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                optionsMenuUI.OnAudioPanelBack();
        }
    }
}
