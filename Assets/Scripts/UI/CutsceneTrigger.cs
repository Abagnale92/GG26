using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

namespace UI
{
    /// <summary>
    /// Trigger che fa partire un filmato quando il player entra nell'area.
    /// Al termine o premendo un tasto, torna al menu principale.
    /// Perfetto per cutscene finali, ending, ecc.
    /// </summary>
    public class CutsceneTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOnce = true;

        [Header("Video Settings")]
        [Tooltip("VideoPlayer da usare (se vuoto, ne crea uno automaticamente)")]
        [SerializeField] private VideoPlayer videoPlayer;
        [Tooltip("Nome del file video in StreamingAssets (es: Finale.mp4). Usato solo se non c'e VideoClip assegnato al VideoPlayer")]
        [SerializeField] private string videoFileName = "Finale.mp4";

        [Header("After Video")]
        [SerializeField] private string menuSceneName = "MenuScene";
        [SerializeField] private bool returnToMenuAfterVideo = true;

        [Header("Skip Settings")]
        [SerializeField] private bool canSkipVideo = true;
        [SerializeField] private float skipDelay = 2f;
        [SerializeField] private KeyCode skipKey = KeyCode.Space;
        [SerializeField] private bool anyKeyToSkip = true;
        [SerializeField] private string skipText = "Premi SPAZIO per continuare";

        [Header("UI References")]
        [Tooltip("Canvas che contiene il video (verra attivato automaticamente)")]
        [SerializeField] private GameObject cutsceneCanvas;
        [Tooltip("RawImage per mostrare il video (verra attivata automaticamente)")]
        [SerializeField] private RawImage videoDisplay;
        [SerializeField] private TMP_Text skipHintText;

        [Header("Audio")]
        [SerializeField] private bool muteGameAudio = true;
        [SerializeField] private AudioClip cutsceneMusic; // Musica opzionale durante il video

        [Header("Fade")]
        [SerializeField] private bool useFadeIn = true;
        [SerializeField] private bool useFadeOut = true;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private Image fadeImage; // Immagine nera per fade

        private AudioSource videoAudioSource;
        private AudioSource musicAudioSource;
        private RenderTexture renderTexture;
        private bool hasTriggered = false;
        private bool isPlaying = false;
        private bool canSkip = false;
        private float originalTimeScale;

        public event System.Action OnCutsceneStarted;
        public event System.Action OnCutsceneEnded;
        public event System.Action OnCutsceneSkipped;

        private void Start()
        {
            // Nascondi il canvas all'inizio
            if (cutsceneCanvas != null)
            {
                cutsceneCanvas.SetActive(false);
            }

            // Nascondi la RawImage all'inizio
            if (videoDisplay != null)
            {
                videoDisplay.gameObject.SetActive(false);
            }

            // Nascondi hint skip
            if (skipHintText != null)
            {
                skipHintText.gameObject.SetActive(false);
            }

            // Setup fade image
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnce && hasTriggered) return;

            if (other.CompareTag(playerTag))
            {
                hasTriggered = true;
                StartCutscene();
            }
        }

        /// <summary>
        /// Avvia la cutscene (puo essere chiamato anche da altri script)
        /// </summary>
        public void StartCutscene()
        {
            if (isPlaying) return;

            StartCoroutine(PlayCutsceneRoutine());
        }

        private IEnumerator PlayCutsceneRoutine()
        {
            isPlaying = true;
            canSkip = false;
            originalTimeScale = Time.timeScale;

            OnCutsceneStarted?.Invoke();

            // Pausa il gioco
            Time.timeScale = 0f;

            // Muta l'audio di gioco se richiesto
            if (muteGameAudio)
            {
                AudioListener.volume = 0f;
            }

            // Fade in (schermo nero)
            if (useFadeIn && fadeImage != null)
            {
                yield return StartCoroutine(FadeRoutine(0f, 1f, fadeDuration));
            }

            // Attiva il canvas
            if (cutsceneCanvas != null)
            {
                cutsceneCanvas.SetActive(true);
            }

            // Ripristina audio per il video
            AudioListener.volume = 1f;

            // Setup e avvia il video
            yield return StartCoroutine(SetupAndPlayVideo());

            // Fade out dal nero (mostra video)
            if (useFadeIn && fadeImage != null)
            {
                yield return StartCoroutine(FadeRoutine(1f, 0f, fadeDuration));
            }

            // Avvia musica cutscene se presente
            if (cutsceneMusic != null)
            {
                PlayCutsceneMusic();
            }

            // Aspetta prima di permettere skip
            float elapsed = 0f;
            while (elapsed < skipDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Abilita skip
            if (canSkipVideo)
            {
                canSkip = true;
                if (skipHintText != null)
                {
                    skipHintText.text = skipText;
                    skipHintText.gameObject.SetActive(true);
                }
            }
        }

        private IEnumerator SetupAndPlayVideo()
        {
            bool createdVideoPlayer = false;

            // Crea VideoPlayer se non assegnato dall'inspector
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.isLooping = false;
                videoPlayer.source = VideoSource.Url;
                createdVideoPlayer = true;
            }

            // Setup audio se necessario
            if (videoAudioSource == null)
            {
                videoAudioSource = videoPlayer.GetComponent<AudioSource>();
                if (videoAudioSource == null)
                {
                    videoAudioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
                }
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, videoAudioSource);
            }

            // Registra eventi
            videoPlayer.loopPointReached += OnVideoComplete;
            videoPlayer.errorReceived += OnVideoError;

            // Crea RenderTexture se il VideoPlayer usa RenderTexture mode
            if (videoPlayer.renderMode == VideoRenderMode.RenderTexture)
            {
                if (renderTexture == null)
                {
                    renderTexture = new RenderTexture(1920, 1080, 0);
                }
                videoPlayer.targetTexture = renderTexture;
            }

            // Imposta URL solo se non c'e gia un VideoClip assegnato
            if (videoPlayer.source == VideoSource.Url || (videoPlayer.source == VideoSource.VideoClip && videoPlayer.clip == null))
            {
                videoPlayer.source = VideoSource.Url;
                string url = GetVideoUrl();
                Debug.Log($"CutsceneTrigger: Caricamento video da {url}");
                videoPlayer.url = url;
            }
            else
            {
                Debug.Log($"CutsceneTrigger: Usando VideoClip assegnato: {videoPlayer.clip?.name}");
            }

            // Prepara
            videoPlayer.Prepare();

            // Aspetta preparazione (con timeout)
            float timeout = 15f;
            float elapsed = 0f;
            while (!videoPlayer.isPrepared && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!videoPlayer.isPrepared)
            {
                Debug.LogError("CutsceneTrigger: Timeout preparazione video!");
                EndCutscene();
                yield break;
            }

            // Attiva la RawImage prima di riprodurre
            if (videoDisplay != null)
            {
                videoDisplay.gameObject.SetActive(true);

                // Assegna la texture
                if (videoPlayer.renderMode == VideoRenderMode.RenderTexture && renderTexture != null)
                {
                    videoDisplay.texture = renderTexture;
                }
                else if (videoPlayer.targetTexture != null)
                {
                    videoDisplay.texture = videoPlayer.targetTexture;
                }

                videoDisplay.color = Color.white;
            }

            // Play
            videoPlayer.Play();

            // Aspetta primo frame
            while (videoPlayer.frame < 1)
            {
                yield return null;
            }
        }

        private string GetVideoUrl()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);

#if UNITY_WEBGL && !UNITY_EDITOR
            return path;
#elif UNITY_ANDROID && !UNITY_EDITOR
            return path;
#else
            return "file://" + path;
#endif
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Controlla skip
            if (canSkip)
            {
                bool shouldSkip = false;

                if (anyKeyToSkip && Input.anyKeyDown)
                {
                    shouldSkip = true;
                }
                else if (Input.GetKeyDown(skipKey))
                {
                    shouldSkip = true;
                }

                if (shouldSkip)
                {
                    SkipCutscene();
                }
            }
        }

        private void SkipCutscene()
        {
            if (!isPlaying) return;

            OnCutsceneSkipped?.Invoke();
            EndCutscene();
        }

        private void OnVideoComplete(VideoPlayer vp)
        {
            if (!isPlaying) return;

            OnCutsceneEnded?.Invoke();
            EndCutscene();
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"CutsceneTrigger: Errore video - {message}");
            EndCutscene();
        }

        private void EndCutscene()
        {
            StartCoroutine(EndCutsceneRoutine());
        }

        private IEnumerator EndCutsceneRoutine()
        {
            isPlaying = false;
            canSkip = false;

            // Nascondi hint
            if (skipHintText != null)
            {
                skipHintText.gameObject.SetActive(false);
            }

            // Stop video
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            // Stop musica
            if (musicAudioSource != null)
            {
                musicAudioSource.Stop();
            }

            // Fade out (schermo diventa nero)
            if (useFadeOut && fadeImage != null)
            {
                yield return StartCoroutine(FadeRoutine(0f, 1f, fadeDuration));
            }

            // IMPORTANTE: Nascondi la RawImage MA mantieni il fade nero visibile!
            if (videoDisplay != null)
            {
                videoDisplay.gameObject.SetActive(false);
            }

            // NON nascondere il canvas - mantienilo visibile con lo schermo nero
            // fino al cambio scena per evitare di vedere la scena di gioco

            // Ripristina time scale
            Time.timeScale = originalTimeScale > 0 ? originalTimeScale : 1f;

            // Ripristina audio
            AudioListener.volume = 1f;

            // Vai al menu o scena successiva
            if (returnToMenuAfterVideo)
            {
                GoToMenu();
            }
            else
            {
                // Se non torna al menu, nascondi il canvas
                if (cutsceneCanvas != null)
                {
                    cutsceneCanvas.SetActive(false);
                }
            }
        }

        private void GoToMenu()
        {
            // Mantieni il fadeImage attivo e nero fino al cambio scena
            if (fadeImage != null)
            {
                // Assicurati che sia completamente nero
                Color c = fadeImage.color;
                c.a = 1f;
                fadeImage.color = c;
                fadeImage.gameObject.SetActive(true);

                // Rendi persistente il canvas durante il caricamento scena
                if (cutsceneCanvas != null)
                {
                    DontDestroyOnLoad(cutsceneCanvas);
                }
            }

            // Usa SceneTransition se disponibile
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadSceneWithFade(menuSceneName);
            }
            else
            {
                // Carica la scena - lo schermo rimarrà nero durante il caricamento
                UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
            }
        }

        private void PlayCutsceneMusic()
        {
            if (musicAudioSource == null)
            {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
            }

            musicAudioSource.clip = cutsceneMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (fadeImage == null) yield break;

            fadeImage.gameObject.SetActive(true);
            Color color = fadeImage.color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                color.a = Mathf.Lerp(from, to, t);
                fadeImage.color = color;
                yield return null;
            }

            color.a = to;
            fadeImage.color = color;

            if (to <= 0f)
            {
                fadeImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Resetta il trigger per poterlo riattivare
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoComplete;
                videoPlayer.errorReceived -= OnVideoError;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            // Assicurati di ripristinare time scale e audio
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = hasTriggered ? Color.green : Color.cyan;

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
                }
            }
        }
    }
}
