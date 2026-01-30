using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

namespace UI
{
    /// <summary>
    /// Gestisce la riproduzione del video introduttivo con possibilita di skip.
    /// Compatibile con WebGL usando URL da StreamingAssets invece di VideoClip.
    /// </summary>
    public class IntroVideoPlayer : MonoBehaviour
    {
        [Header("Video Settings")]
        [Tooltip("Nome del file video in StreamingAssets (es: Introduzione.mp4)")]
        [SerializeField] private string videoFileName = "Introduzione.mp4";
        [SerializeField] private string nextSceneName = "GameScene1";

        [Header("Skip Settings")]
        [SerializeField] private float skipDelay = 1f;
        [SerializeField] private string skipText = "Premi un tasto per saltare";

        [Header("UI References")]
        [SerializeField] private RawImage videoDisplay;
        [SerializeField] private TMP_Text skipHintText;

        private VideoPlayer videoPlayer;
        private AudioSource audioSource;
        private RenderTexture renderTexture;
        private bool canSkip;
        private bool isPlaying;

        public event Action OnVideoStarted;
        public event Action OnVideoEnded;
        public event Action OnVideoSkipped;

        private void Awake()
        {
            SetupVideoPlayer();
        }

        private void SetupVideoPlayer()
        {
            // Crea VideoPlayer
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.isLooping = false;

            // IMPORTANTE per WebGL: usa URL invece di VideoClip
            videoPlayer.source = VideoSource.Url;

            // Crea AudioSource per l'audio del video
            audioSource = gameObject.AddComponent<AudioSource>();
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            // Eventi video
            videoPlayer.loopPointReached += OnVideoComplete;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.errorReceived += OnVideoError;
        }

        /// <summary>
        /// Costruisce l'URL del video in base alla piattaforma
        /// </summary>
        private string GetVideoUrl()
        {
            // StreamingAssets path varia per piattaforma
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);

            // Su WebGL e Android, il path e gia un URL
            // Su altre piattaforme, dobbiamo aggiungere file://
#if UNITY_WEBGL && !UNITY_EDITOR
            return path;
#elif UNITY_ANDROID && !UNITY_EDITOR
            return path;
#else
            return "file://" + path;
#endif
        }

        /// <summary>
        /// Avvia la riproduzione del video
        /// </summary>
        public void PlayVideo()
        {
            if (string.IsNullOrEmpty(videoFileName))
            {
                Debug.LogWarning("IntroVideoPlayer: Nessun video specificato! Passaggio alla scena successiva.");
                GoToNextScene();
                return;
            }

            StartCoroutine(PlayVideoRoutine());
        }

        private IEnumerator PlayVideoRoutine()
        {
            isPlaying = true;
            canSkip = false;

            // Nascondi la RawImage e rendila nera mentre carica
            if (videoDisplay != null)
            {
                videoDisplay.color = Color.black;
                videoDisplay.texture = null;
            }

            // Crea RenderTexture per il video
            renderTexture = new RenderTexture(1920, 1080, 0);
            videoPlayer.targetTexture = renderTexture;

            // Imposta URL del video
            string url = GetVideoUrl();
            Debug.Log($"IntroVideoPlayer: Caricamento video da {url}");
            videoPlayer.url = url;

            // Prepara il video
            videoPlayer.Prepare();

            // Aspetta che il video sia pronto (con timeout)
            float timeout = 10f;
            float elapsed = 0f;
            while (!videoPlayer.isPrepared && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!videoPlayer.isPrepared)
            {
                Debug.LogError("IntroVideoPlayer: Timeout durante la preparazione del video!");
                GoToNextScene();
                yield break;
            }

            // Riproduci
            videoPlayer.Play();

            // Aspetta il primo frame renderizzato
            while (videoPlayer.frame < 1)
            {
                yield return null;
            }

            // ORA assegna la texture e mostra il video
            if (videoDisplay != null)
            {
                videoDisplay.texture = renderTexture;
                videoDisplay.color = Color.white; // Ripristina colore normale
            }

            OnVideoStarted?.Invoke();

            // Mostra hint per skip dopo il delay
            yield return new WaitForSeconds(skipDelay);
            canSkip = true;

            if (skipHintText != null)
            {
                skipHintText.text = skipText;
                skipHintText.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Controlla se l'utente vuole skippare
            if (canSkip && AnyKeyPressed())
            {
                SkipVideo();
            }
        }

        private bool AnyKeyPressed()
        {
            return Input.anyKeyDown;
        }

        private void SkipVideo()
        {
            if (!isPlaying) return;

            isPlaying = false;
            videoPlayer.Stop();
            OnVideoSkipped?.Invoke();
            GoToNextScene();
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            Debug.Log("IntroVideoPlayer: Video preparato e pronto");
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"IntroVideoPlayer: Errore video - {message}");
            if (isPlaying)
            {
                isPlaying = false;
                GoToNextScene();
            }
        }

        private void OnVideoComplete(VideoPlayer vp)
        {
            if (!isPlaying) return;

            isPlaying = false;
            OnVideoEnded?.Invoke();
            GoToNextScene();
        }

        private void GoToNextScene()
        {
            // Nascondi hint skip
            if (skipHintText != null)
            {
                skipHintText.gameObject.SetActive(false);
            }

            // Usa SceneTransition se disponibile
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadSceneWithFade(nextSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoComplete;
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.errorReceived -= OnVideoError;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        // === SETTER METHODS ===

        public void SetVideoFileName(string fileName)
        {
            videoFileName = fileName;
        }

        public void SetNextScene(string sceneName)
        {
            nextSceneName = sceneName;
        }

        public void SetVideoDisplay(RawImage display)
        {
            videoDisplay = display;
        }

        public void SetSkipHintText(TMP_Text text)
        {
            skipHintText = text;
        }
    }
}
