using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Gestisce l'audio del gioco: soundtrack e effetti sonori.
    /// Singleton che persiste tra le scene se necessario.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music Settings")]
        [SerializeField] private AudioClip sceneSoundtrack;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool fadeInOnStart = true;
        [SerializeField] private float fadeInDuration = 2f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("SFX Volume")]
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

        private void Awake()
        {
            // Singleton pattern (non persistente - ogni scena ha il suo AudioManager)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Crea AudioSource per la musica se non assegnato
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = loop;
            }

            // Crea AudioSource per gli SFX se non assegnato
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            if (playOnAwake && sceneSoundtrack != null)
            {
                if (fadeInOnStart)
                {
                    PlayMusicWithFadeIn(sceneSoundtrack);
                }
                else
                {
                    PlayMusic(sceneSoundtrack);
                }
            }
        }

        /// <summary>
        /// Riproduce la musica di sottofondo
        /// </summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;

            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.loop = loop;
            musicSource.Play();
        }

        /// <summary>
        /// Riproduce la musica con fade in
        /// </summary>
        public void PlayMusicWithFadeIn(AudioClip clip)
        {
            if (clip == null) return;

            musicSource.clip = clip;
            musicSource.volume = 0f;
            musicSource.loop = loop;
            musicSource.Play();

            StartCoroutine(FadeIn(musicSource, musicVolume, fadeInDuration));
        }

        /// <summary>
        /// Ferma la musica
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }

        /// <summary>
        /// Ferma la musica con fade out
        /// </summary>
        public void StopMusicWithFadeOut(float duration = 1f)
        {
            StartCoroutine(FadeOutAndStop(musicSource, duration));
        }

        /// <summary>
        /// Mette in pausa la musica
        /// </summary>
        public void PauseMusic()
        {
            musicSource.Pause();
        }

        /// <summary>
        /// Riprende la musica
        /// </summary>
        public void ResumeMusic()
        {
            musicSource.UnPause();
        }

        /// <summary>
        /// Riproduce un effetto sonoro
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        /// <summary>
        /// Riproduce un effetto sonoro con volume personalizzato
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Imposta il volume della musica
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume;
        }

        /// <summary>
        /// Imposta il volume degli effetti sonori
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Cambia la soundtrack con crossfade
        /// </summary>
        public void ChangeSoundtrack(AudioClip newClip, float fadeDuration = 1f)
        {
            StartCoroutine(CrossfadeMusic(newClip, fadeDuration));
        }

        private System.Collections.IEnumerator FadeIn(AudioSource source, float targetVolume, float duration)
        {
            float elapsed = 0f;
            float startVolume = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }

        private System.Collections.IEnumerator FadeOutAndStop(AudioSource source, float duration)
        {
            float elapsed = 0f;
            float startVolume = source.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
        {
            // Fade out current music
            float elapsed = 0f;
            float startVolume = musicSource.volume;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration / 2f));
                yield return null;
            }

            // Change clip
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in new music
            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / (duration / 2f));
                yield return null;
            }

            musicSource.volume = musicVolume;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
