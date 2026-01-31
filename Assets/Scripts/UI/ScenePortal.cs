using UnityEngine;
using UI;

namespace Interaction
{
    /// <summary>
    /// Portale che trasporta il player in una nuova scena quando lo tocca.
    /// Usa SceneTransition per l'effetto fade.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScenePortal : MonoBehaviour
    {
        [Header("Scene Settings")]
        [Tooltip("Nome della scena da caricare (deve essere aggiunta in Build Settings)")]
        [SerializeField] private string targetSceneName;

        [Tooltip("Oppure usa l'indice della scena (usato solo se targetSceneName è vuoto)")]
        [SerializeField] private int targetSceneIndex = -1;

        [Header("Portal Settings")]
        [Tooltip("Se true, il portale funziona solo una volta")]
        [SerializeField] private bool oneTimeUse = false;

        [Header("Audio")]
        [SerializeField] private AudioClip portalSound;

        private bool hasBeenUsed = false;
        private AudioSource audioSource;

        private void Start()
        {
            // Assicurati che il collider sia trigger
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"{gameObject.name}: Il collider del portale dovrebbe essere un Trigger!");
            }

            // Setup audio
            if (portalSound != null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (oneTimeUse && hasBeenUsed) return;

            // Controlla che SceneTransition esista
            if (SceneTransition.Instance == null)
            {
                Debug.LogError("SceneTransition non trovato! Assicurati che esista nella scena.");
                return;
            }

            // Controlla che non sia già in transizione
            if (SceneTransition.Instance.IsTransitioning) return;

            hasBeenUsed = true;

            // Riproduci suono
            if (portalSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(portalSound);
            }

            // Carica la nuova scena con fade
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log($"Portale attivato! Caricamento scena: {targetSceneName}");
                SceneTransition.Instance.LoadSceneWithFade(targetSceneName);
            }
            else if (targetSceneIndex >= 0)
            {
                Debug.Log($"Portale attivato! Caricamento scena index: {targetSceneIndex}");
                SceneTransition.Instance.LoadSceneWithFade(targetSceneIndex);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Nessuna scena configurata per il portale!");
            }
        }

        private void OnDrawGizmos()
        {
            // Visualizza il portale nella Scene View
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Ciano trasparente

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.center, box.size);
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                    Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
                }
            }
        }
    }
}
