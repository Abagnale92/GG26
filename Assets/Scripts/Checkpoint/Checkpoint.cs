using UnityEngine;

namespace Checkpoints
{
    /// <summary>
    /// Punto di respawn. Quando il player lo tocca, diventa il checkpoint attivo.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Color inactiveColor = Color.gray;
        [SerializeField] private Color activeColor = Color.green;

        [Header("Audio")]
        [SerializeField] private AudioClip activationSound;
        [SerializeField][Range(0f, 1f)] private float soundVolume = 1f;

        private Renderer checkpointRenderer;
        private AudioSource audioSource;
        private bool isActive = false;
        private bool hasBeenActivated = false; // Per riprodurre il suono solo la prima volta

        public bool IsActive => isActive;

        private void Awake()
        {
            // Assicurati che il collider sia trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            checkpointRenderer = GetComponent<Renderer>();
            if (checkpointRenderer == null)
            {
                checkpointRenderer = GetComponentInChildren<Renderer>();
            }

            // Setup AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }

            // Imposta colore iniziale
            SetVisualState(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // Attiva questo checkpoint
            CheckpointManager manager = FindFirstObjectByType<CheckpointManager>();
            if (manager != null)
            {
                manager.SetActiveCheckpoint(this);
            }
        }

        /// <summary>
        /// Chiamato dal CheckpointManager quando questo checkpoint viene attivato/disattivato
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            SetVisualState(active);

            if (active)
            {
                // Riproduci il suono solo la prima volta che viene attivato
                if (!hasBeenActivated)
                {
                    hasBeenActivated = true;
                    PlayActivationSound();
                }

                Debug.Log($"Checkpoint attivato: {gameObject.name}");
            }
        }

        private void PlayActivationSound()
        {
            if (activationSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(activationSound, soundVolume);
            }
        }

        private void SetVisualState(bool active)
        {
            if (checkpointRenderer != null)
            {
                checkpointRenderer.material.color = active ? activeColor : inactiveColor;
            }
        }

        /// <summary>
        /// Ritorna la posizione di respawn (leggermente sopra il checkpoint)
        /// </summary>
        public Vector3 GetRespawnPosition()
        {
            return transform.position + Vector3.up * 1f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isActive ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Mostra posizione di respawn
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.3f);
        }
    }
}
