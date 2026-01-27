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

        private Renderer checkpointRenderer;
        private bool isActive = false;

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
                Debug.Log($"Checkpoint attivato: {gameObject.name}");
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
