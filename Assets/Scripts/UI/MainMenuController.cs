using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// Controller principale per il menu di gioco.
    /// Gestisce bottoni Gioca/Crediti e la transizione al video introduttivo.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject videoPanel;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button backButton; // Nel pannello crediti

        [Header("Video Settings")]
        [Tooltip("Nome del file video in StreamingAssets (es: Introduzione.mp4)")]
        [SerializeField] private string videoFileName = "Introduzione.mp4";
        [SerializeField] private RawImage videoDisplay;
        [SerializeField] private TMP_Text skipHintText;
        [SerializeField] private string gameSceneName = "GameScene1";

        [Header("Credits Info")]
        [SerializeField] private TMP_Text creditsText;

        private IntroVideoPlayer videoPlayer;
        private bool isTransitioning;

        private void Start()
        {
            // Assicurati che SceneTransition esista
            EnsureSceneTransition();

            // Setup iniziale pannelli
            ShowMainMenu();

            // Collega i bottoni
            SetupButtons();

            // Fade in all'avvio della scena
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.SetBlack();
                SceneTransition.Instance.FadeIn();
            }
        }

        private void EnsureSceneTransition()
        {
            if (SceneTransition.Instance == null)
            {
                GameObject transitionObj = new GameObject("SceneTransition");
                transitionObj.AddComponent<SceneTransition>();
            }
        }

        private void SetupButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(OnCreditsClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        /// <summary>
        /// Chiamato quando si preme il bottone Gioca
        /// </summary>
        public void OnPlayClicked()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            // Fade out dal menu, poi mostra video
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.FadeOut(() =>
                {
                    ShowVideoPanel();
                    StartVideo();
                });
            }
            else
            {
                ShowVideoPanel();
                StartVideo();
            }
        }

        /// <summary>
        /// Chiamato quando si preme il bottone Crediti
        /// </summary>
        public void OnCreditsClicked()
        {
            if (isTransitioning) return;
            ShowCreditsPanel();
        }

        /// <summary>
        /// Chiamato quando si preme il bottone Indietro dai crediti
        /// </summary>
        public void OnBackClicked()
        {
            if (isTransitioning) return;
            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (videoPanel != null) videoPanel.SetActive(false);
        }

        private void ShowCreditsPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
            if (videoPanel != null) videoPanel.SetActive(false);
        }

        private void ShowVideoPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (videoPanel != null) videoPanel.SetActive(true);
        }

        private void StartVideo()
        {
            // Crea IntroVideoPlayer se non esiste
            if (videoPlayer == null)
            {
                GameObject videoObj = new GameObject("IntroVideoPlayer");
                videoPlayer = videoObj.AddComponent<IntroVideoPlayer>();
            }

            // Configura il video player con tutti i riferimenti necessari
            videoPlayer.SetVideoFileName(videoFileName);
            videoPlayer.SetNextScene(gameSceneName);
            videoPlayer.SetVideoDisplay(videoDisplay);
            videoPlayer.SetSkipHintText(skipHintText);

            // Fade in per mostrare il video
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.FadeIn(() =>
                {
                    videoPlayer.PlayVideo();
                });
            }
            else
            {
                videoPlayer.PlayVideo();
            }
        }

        /// <summary>
        /// Imposta il testo dei crediti da codice
        /// </summary>
        public void SetCreditsText(string credits)
        {
            if (creditsText != null)
            {
                creditsText.text = credits;
            }
        }

        private void OnDestroy()
        {
            // Rimuovi listener
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
