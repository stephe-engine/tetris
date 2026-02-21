using System.Collections;
using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Manages all game audio: background music playback and sound effect responses
    /// to game events. Subscribes to <see cref="GameManager"/> events rather than
    /// coupling directly to game logic. Two <see cref="AudioSource"/> components are
    /// created at runtime — one for looping music, one for overlapping SFX.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        /// <summary>Reference to the game manager whose events drive audio playback.</summary>
        [SerializeField] private GameManager gameManager;

        /// <summary>Sound settings asset containing all clips and default volumes.</summary>
        [SerializeField] private SoundSettings settings;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private Coroutine trackFadeCoroutine;

        /// <summary>Current music volume [0, 1].</summary>
        public float MusicVolume { get; private set; }

        /// <summary>Current SFX volume [0, 1].</summary>
        public float SfxVolume { get; private set; }

        /// <summary>Index of the currently playing music track.</summary>
        public int CurrentTrackIndex { get; private set; }

        /// <summary>Display names for each music track, sourced from <see cref="SoundSettings"/>.</summary>
        public string[] TrackNames => settings != null ? settings.musicTrackNames : System.Array.Empty<string>();

        private void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            if (settings != null)
            {
                MusicVolume = settings.defaultMusicVolume;
                SfxVolume = settings.defaultSfxVolume;
                CurrentTrackIndex = settings.defaultMusicTrackIndex;
            }
            else
            {
                MusicVolume = 0.5f;
                SfxVolume = 1.0f;
                CurrentTrackIndex = 0;
            }

            musicSource.volume = MusicVolume;
        }

        private void OnEnable()
        {
            if (!gameManager)
                return;

            gameManager.OnGameStart += HandleGameStart;
            gameManager.OnGameOver += HandleGameOver;
            gameManager.OnMove += HandleMove;
            gameManager.OnRotate += HandleRotate;
            gameManager.OnSoftDrop += HandleSoftDrop;
            gameManager.OnHardDrop += HandleHardDrop;
            gameManager.OnLanded += HandleLanded;
            gameManager.OnLock += HandleLock;
            gameManager.OnLineClear += HandleLineClear;
        }

        private void OnDisable()
        {
            if (!gameManager)
                return;

            gameManager.OnGameStart -= HandleGameStart;
            gameManager.OnGameOver -= HandleGameOver;
            gameManager.OnMove -= HandleMove;
            gameManager.OnRotate -= HandleRotate;
            gameManager.OnSoftDrop -= HandleSoftDrop;
            gameManager.OnHardDrop -= HandleHardDrop;
            gameManager.OnLanded -= HandleLanded;
            gameManager.OnLock -= HandleLock;
            gameManager.OnLineClear -= HandleLineClear;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Sets the music volume and applies it immediately to the music source.
        /// </summary>
        /// <param name="volume">Target volume in the range [0, 1].</param>
        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            musicSource.volume = MusicVolume;
        }

        /// <summary>
        /// Sets the SFX volume used for all subsequent <see cref="PlaySfx"/> calls.
        /// </summary>
        /// <param name="volume">Target volume in the range [0, 1].</param>
        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Switches to the track at the given index with a short fade-out / fade-in transition.
        /// </summary>
        /// <param name="trackIndex">Index into <see cref="SoundSettings.musicTracks"/>.</param>
        public void SetMusicTrack(int trackIndex)
        {
            if (settings == null || settings.musicTracks == null)
                return;

            trackIndex = Mathf.Clamp(trackIndex, 0, settings.musicTracks.Length - 1);

            if (trackIndex == CurrentTrackIndex && musicSource.isPlaying)
                return;

            CurrentTrackIndex = trackIndex;

            if (trackFadeCoroutine != null)
                StopCoroutine(trackFadeCoroutine);

            trackFadeCoroutine = StartCoroutine(FadeTrack(settings.musicTracks[trackIndex]));
        }

        // -------------------------------------------------------------------------
        // Event handlers
        // -------------------------------------------------------------------------

        private void HandleGameStart()
        {
            PlaySfx(settings != null ? settings.sfxGameStart : null);
            StartMusic();
        }

        private void HandleGameOver()
        {
            musicSource.Stop();
            PlaySfx(settings != null ? settings.sfxGameOver : null);
        }

        private void HandleMove() => PlaySfx(settings != null ? settings.sfxMove : null);

        private void HandleRotate() => PlaySfx(settings != null ? settings.sfxRotate : null);

        private void HandleSoftDrop() => PlaySfx(settings != null ? settings.sfxSoftDrop : null);

        private void HandleHardDrop() => PlaySfx(settings != null ? settings.sfxHardDrop : null);

        private void HandleLanded() => PlaySfx(settings != null ? settings.sfxLanded : null);

        private void HandleLock() => PlaySfx(settings != null ? settings.sfxLock : null);

        private void HandleLineClear(int lineCount)
        {
            if (settings == null)
                return;

            PlaySfx(lineCount >= 4 ? settings.sfxTetris : settings.sfxLineClear);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Plays the given clip as a one-shot SFX, allowing overlapping playback.
        /// </summary>
        /// <param name="clip">The clip to play. Silently ignored if null.</param>
        private void PlaySfx(AudioClip clip)
        {
            if (!clip)
                return;

            sfxSource.PlayOneShot(clip, SfxVolume);
        }

        /// <summary>
        /// Starts playing the currently selected music track from the beginning.
        /// </summary>
        private void StartMusic()
        {
            if (settings == null || settings.musicTracks == null || settings.musicTracks.Length == 0)
                return;

            int index = Mathf.Clamp(CurrentTrackIndex, 0, settings.musicTracks.Length - 1);
            AudioClip track = settings.musicTracks[index];

            if (!track)
                return;

            musicSource.clip = track;
            musicSource.volume = MusicVolume;
            musicSource.Play();
        }

        /// <summary>
        /// Coroutine that fades the music out, swaps the clip, then fades back in.
        /// </summary>
        /// <param name="newClip">The clip to play after the fade-out.</param>
        private IEnumerator FadeTrack(AudioClip newClip)
        {
            const float fadeDuration = 0.5f;

            // Fade out
            float startVolume = musicSource.volume;
            for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }

            musicSource.volume = 0f;
            musicSource.Stop();

            if (!newClip)
            {
                musicSource.volume = MusicVolume;
                trackFadeCoroutine = null;
                yield break;
            }

            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in
            for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, MusicVolume, t / fadeDuration);
                yield return null;
            }

            musicSource.volume = MusicVolume;
            trackFadeCoroutine = null;
        }
    }
}
