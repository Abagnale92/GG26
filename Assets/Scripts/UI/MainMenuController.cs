using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        [SerializeField] private GameObject levelSelectPanel;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button backButton; // Nel pannello crediti
        [SerializeField] private Button levelSelectButton; // Bottone per aprire selezione livelli
        [SerializeField] private Button backFromLevelSelectButton; // Bottone indietro dalla selezione livelli

        [Header("Level Select Buttons")]
        [SerializeField] private Button level1Button;
        [SerializeField] private Button level2Button;
        [SerializeField] private Button level3Button;
        [SerializeField] private Button level4Button;

        [Header("Level Scene Names")]
        [SerializeField] private string level1SceneName = "GameScene1";
        [SerializeField] private string level2SceneName = "GameScene2";
        [SerializeField] private string level3SceneName = "GameScene3";
        [SerializeField] private string level4SceneName = "GameScene4";

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
            // IMPORTANTE: Assicurati che Time.timeScale sia 1 (potrebbe essere 0 se arrivi da pausa)
            Time.timeScale = 1f;

            // Resetta flag transizione
            isTransitioning = false;

            // Assicurati che SceneTransition esista
            EnsureSceneTransition();

            // Assicurati che ci sia un EventSystem nella scena
            EnsureEventSystem();

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

            Debug.Log("MainMenuController: Inizializzazione completata");
        }

        private void EnsureEventSystem()
        {
            // Controlla se esiste un EventSystem
            if (EventSystem.current == null)
            {
                // Cerca nella scena
                var existingES = FindFirstObjectByType<EventSystem>();
                if (existingES == null)
                {
                    Debug.LogWarning("MainMenuController: EventSystem mancante! Creazione automatica...");
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<EventSystem>();
                    eventSystemObj.AddComponent<StandaloneInputModule>();
                }
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

            if (levelSelectButton != null)
            {
                levelSelectButton.onClick.AddListener(OnLevelSelectClicked);
            }

            if (backFromLevelSelectButton != null)
            {
                backFromLevelSelectButton.onClick.AddListener(OnBackFromLevelSelectClicked);
            }

            // Setup bottoni livelli
            if (level1Button != null)
            {
                level1Button.onClick.AddListener(() => LoadLevel(level1SceneName));
            }

            if (level2Button != null)
            {
                level2Button.onClick.AddListener(() => LoadLevel(level2SceneName));
            }

            if (level3Button != null)
            {
                level3Button.onClick.AddListener(() => LoadLevel(level3SceneName));
            }

            if (level4Button != null)
            {
                level4Button.onClick.AddListener(() => LoadLevel(level4SceneName));
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
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        }

        private void ShowCreditsPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
            if (videoPanel != null) videoPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        }

        private void ShowVideoPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (videoPanel != null) videoPanel.SetActive(true);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        }

        private void ShowLevelSelectPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (videoPanel != null) videoPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        }

        /// <summary>
        /// Chiamato quando si preme il bottone Selezione Livelli
        /// </summary>
        public void OnLevelSelectClicked()
        {
            if (isTransitioning) return;
            ShowLevelSelectPanel();
        }

        /// <summary>
        /// Chiamato quando si preme il bottone Indietro dalla selezione livelli
        /// </summary>
        public void OnBackFromLevelSelectClicked()
        {
            if (isTransitioning) return;
            ShowMainMenu();
        }

        /// <summary>
        /// Carica direttamente un livello senza video introduttivo
        /// </summary>
        public void LoadLevel(string sceneName)
        {
            if (isTransitioning) return;
            if (string.IsNullOrEmpty(sceneName)) return;

            isTransitioning = true;

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadSceneWithFade(sceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        /// <summary>
        /// Carica il livello 1
        /// </summary>
        public void LoadLevel1() => LoadLevel(level1SceneName);

        /// <summary>
        /// Carica il livello 2
        /// </summary>
        public void LoadLevel2() => LoadLevel(level2SceneName);

        /// <summary>
        /// Carica il livello 3
        /// </summary>
        public void LoadLevel3() => LoadLevel(level3SceneName);

        /// <summary>
        /// Carica il livello 4
        /// </summary>
        public void LoadLevel4() => LoadLevel(level4SceneName);

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
            if (levelSelectButton != null) levelSelectButton.onClick.RemoveListener(OnLevelSelectClicked);
            if (backFromLevelSelectButton != null) backFromLevelSelectButton.onClick.RemoveListener(OnBackFromLevelSelectClicked);
            if (level1Button != null) level1Button.onClick.RemoveAllListeners();
            if (level2Button != null) level2Button.onClick.RemoveAllListeners();
            if (level3Button != null) level3Button.onClick.RemoveAllListeners();
            if (level4Button != null) level4Button.onClick.RemoveAllListeners();
        }
    }
}
