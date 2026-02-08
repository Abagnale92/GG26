using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    /// <summary>
    /// Gestisce la pausa del gioco e il ritorno al menu.
    /// - In gioco: ESC apre il menu pausa (opzione per tornare al menu o continuare)
    /// - Nel menu: ESC chiude il gioco
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string menuSceneName = "MenuScene";
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        [Header("Pause UI (Opzionale)")]
        [SerializeField] private GameObject pausePanel;

        [Header("Audio")]
        [SerializeField] private AudioClip pauseSound;
        [SerializeField] private AudioClip unpauseSound;

        private bool isPaused = false;
        private bool isInMenu = false;
        private AudioSource audioSource;

        public bool IsPaused => isPaused;

        public static PauseMenu Instance { get; private set; }

        private void Awake()
        {
            // Singleton semplice (non persistente)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            // Controlla se siamo nel menu
            string currentScene = SceneManager.GetActiveScene().name;
            isInMenu = currentScene.ToLower().Contains("menu");

            // Nascondi il pannello pausa all'inizio
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (isInMenu)
                {
                    // Nel menu: chiudi il gioco
                    QuitGame();
                }
                else
                {
                    // In gioco: toggle pausa
                    if (isPaused)
                    {
                        ResumeGame();
                    }
                    else
                    {
                        PauseGame();
                    }
                }
            }
        }

        /// <summary>
        /// Mette in pausa il gioco
        /// </summary>
        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;

            // Mostra UI pausa
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            // Suono
            PlaySound(pauseSound);

            Debug.Log("Gioco in pausa");
        }

        /// <summary>
        /// Riprende il gioco
        /// </summary>
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            // Nascondi UI pausa
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            // Suono
            PlaySound(unpauseSound);

            Debug.Log("Gioco ripreso");
        }

        /// <summary>
        /// Torna al menu principale
        /// </summary>
        public void ReturnToMenu()
        {
            // Riprendi il tempo prima di cambiare scena
            Time.timeScale = 1f;
            isPaused = false;

            // Usa SceneTransition se disponibile
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadSceneWithFade(menuSceneName);
            }
            else
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }

        /// <summary>
        /// Chiude il gioco
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Chiusura gioco...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void OnDestroy()
        {
            // Assicurati che il tempo sia ripristinato
            Time.timeScale = 1f;

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
