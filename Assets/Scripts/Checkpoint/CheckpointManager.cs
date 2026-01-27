using UnityEngine;

namespace Checkpoints
{
    /// <summary>
    /// Gestisce i checkpoint e tiene traccia di quello attivo.
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        [Header("Default Spawn")]
        [SerializeField] private Vector3 defaultSpawnPosition = Vector3.zero;

        private Checkpoint activeCheckpoint;

        /// <summary>
        /// Imposta un nuovo checkpoint come attivo, disattivando il precedente
        /// </summary>
        public void SetActiveCheckpoint(Checkpoint checkpoint)
        {
            // Disattiva il checkpoint precedente
            if (activeCheckpoint != null && activeCheckpoint != checkpoint)
            {
                activeCheckpoint.SetActive(false);
            }

            // Attiva il nuovo
            activeCheckpoint = checkpoint;
            if (activeCheckpoint != null)
            {
                activeCheckpoint.SetActive(true);
            }
        }

        /// <summary>
        /// Ritorna la posizione di respawn
        /// </summary>
        public Vector3 GetRespawnPosition()
        {
            if (activeCheckpoint != null)
            {
                return activeCheckpoint.GetRespawnPosition();
            }

            return defaultSpawnPosition;
        }

        /// <summary>
        /// Ritorna il checkpoint attivo
        /// </summary>
        public Checkpoint GetActiveCheckpoint()
        {
            return activeCheckpoint;
        }

        /// <summary>
        /// Resetta tutti i checkpoint (utile per nuovo gioco)
        /// </summary>
        public void ResetAllCheckpoints()
        {
            Checkpoint[] allCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            foreach (var cp in allCheckpoints)
            {
                cp.SetActive(false);
            }
            activeCheckpoint = null;
        }

        private void OnDrawGizmos()
        {
            // Mostra la posizione di spawn di default
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(defaultSpawnPosition, 0.5f);
            Gizmos.DrawLine(defaultSpawnPosition, defaultSpawnPosition + Vector3.up * 2f);
        }
    }
}
