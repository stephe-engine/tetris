using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// ScriptableObject data container for all sound settings.
    /// Holds music tracks, track display names, default volumes, and
    /// SFX clips for every game event. Assign in the Inspector after
    /// creating an asset via Assets > Create > Tetris > Sound Settings.
    /// </summary>
    [CreateAssetMenu(menuName = "Tetris/Sound Settings")]
    public class SoundSettings : ScriptableObject
    {
        [Header("Music")]
        /// <summary>Background music tracks available for playback.</summary>
        [SerializeField] public AudioClip[] musicTracks;

        /// <summary>Display names for each music track (parallel to <see cref="musicTracks"/>).</summary>
        [SerializeField] public string[] musicTrackNames;

        /// <summary>Index into <see cref="musicTracks"/> that plays on game start.</summary>
        [SerializeField] public int defaultMusicTrackIndex = 0;

        /// <summary>Default music volume [0, 1].</summary>
        [SerializeField] public float defaultMusicVolume = 0.5f;

        /// <summary>Default sound effects volume [0, 1].</summary>
        [SerializeField] public float defaultSfxVolume = 1.0f;

        [Header("SFX")]
        /// <summary>Plays when the active piece moves left or right.</summary>
        [SerializeField] public AudioClip sfxMove;

        /// <summary>Plays when the active piece rotates.</summary>
        [SerializeField] public AudioClip sfxRotate;

        /// <summary>Plays when the player soft-drops the piece.</summary>
        [SerializeField] public AudioClip sfxSoftDrop;

        /// <summary>Plays when the player hard-drops the piece.</summary>
        [SerializeField] public AudioClip sfxHardDrop;

        /// <summary>Plays the first time a falling piece contacts a surface (lock delay begins).</summary>
        [SerializeField] public AudioClip sfxLanded;

        /// <summary>Plays when a piece permanently locks onto the board.</summary>
        [SerializeField] public AudioClip sfxLock;

        /// <summary>Plays after clearing 1–3 lines.</summary>
        [SerializeField] public AudioClip sfxLineClear;

        /// <summary>Plays after clearing 4 lines simultaneously (Tetris).</summary>
        [SerializeField] public AudioClip sfxTetris;

        /// <summary>Plays when a new game begins.</summary>
        [SerializeField] public AudioClip sfxGameStart;

        /// <summary>Plays when the game ends (top-out).</summary>
        [SerializeField] public AudioClip sfxGameOver;
    }
}
