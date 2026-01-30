using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Gestisce le transizioni tra scene con effetto fade.
    /// Singleton accessibile da qualsiasi script.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private Color fadeColor = Color.black;

        private Image fadeImage;
        private Canvas fadeCanvas;
        private bool isTransitioning;

        public bool IsTransitioning => isTransitioning;

        public event Action OnFadeOutComplete;
        public event Action OnFadeInComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
        }

        private void CreateFadeCanvas()
        {
            // Crea Canvas per il fade
            fadeCanvas = gameObject.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // Sempre in primo piano

            // Aggiungi CanvasScaler
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Crea immagine per il fade
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(transform, false);

            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.raycastTarget = false;

            // Riempie tutto lo schermo
            RectTransform rect = fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Esegue fade out (da trasparente a nero)
        /// </summary>
        public void FadeOut(Action onComplete = null)
        {
            if (!isTransitioning)
            {
                StartCoroutine(FadeRoutine(0f, 1f, () =>
                {
                    OnFadeOutComplete?.Invoke();
                    onComplete?.Invoke();
                }));
            }
        }

        /// <summary>
        /// Esegue fade in (da nero a trasparente)
        /// </summary>
        public void FadeIn(Action onComplete = null)
        {
            if (!isTransitioning)
            {
                StartCoroutine(FadeRoutine(1f, 0f, () =>
                {
                    OnFadeInComplete?.Invoke();
                    onComplete?.Invoke();
                }));
            }
        }

        /// <summary>
        /// Carica una scena con effetto fade
        /// </summary>
        public void LoadSceneWithFade(string sceneName)
        {
            if (!isTransitioning)
            {
                StartCoroutine(LoadSceneRoutine(sceneName));
            }
        }

        /// <summary>
        /// Carica una scena con effetto fade (by index)
        /// </summary>
        public void LoadSceneWithFade(int sceneIndex)
        {
            if (!isTransitioning)
            {
                StartCoroutine(LoadSceneRoutine(sceneIndex));
            }
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isTransitioning = true;

            // Fade out
            yield return StartCoroutine(FadeRoutine(0f, 1f, null));
            OnFadeOutComplete?.Invoke();

            // Carica scena
            SceneManager.LoadScene(sceneName);

            // Aspetta un frame per il caricamento
            yield return null;

            // Fade in
            yield return StartCoroutine(FadeRoutine(1f, 0f, null));
            OnFadeInComplete?.Invoke();

            isTransitioning = false;
        }

        private IEnumerator LoadSceneRoutine(int sceneIndex)
        {
            isTransitioning = true;

            // Fade out
            yield return StartCoroutine(FadeRoutine(0f, 1f, null));
            OnFadeOutComplete?.Invoke();

            // Carica scena
            SceneManager.LoadSceneAsync(sceneIndex);

            // Aspetta un frame per il caricamento
            yield return null;

            // Fade in
            yield return StartCoroutine(FadeRoutine(1f, 0f, null));
            OnFadeInComplete?.Invoke();

            isTransitioning = false;
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, Action onComplete)
        {
            isTransitioning = true;
            float elapsed = 0f;

            Color color = fadeImage.color;
            color.a = startAlpha;
            fadeImage.color = color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);

                // Usa smooth step per transizione piu fluida
                t = t * t * (3f - 2f * t);

                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                fadeImage.color = color;
                yield return null;
            }

            color.a = endAlpha;
            fadeImage.color = color;

            isTransitioning = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Imposta immediatamente lo schermo a nero (utile per inizio video)
        /// </summary>
        public void SetBlack()
        {
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;
        }

        /// <summary>
        /// Imposta immediatamente lo schermo a trasparente
        /// </summary>
        public void SetClear()
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }

        public void SetFadeDuration(float duration)
        {
            fadeDuration = duration;
        }
    }
}
