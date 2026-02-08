using UnityEngine;
using Player;
using Puzzles;

namespace Hazards
{
    /// <summary>
    /// Zona letale che uccide istantaneamente il player (lava, trappole, vuoto).
    /// Respawna anche gli oggetti magnetici che ci cadono dentro.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LethalZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool instantKill = true;
        [SerializeField] private float damage = 1f; // Usato solo se instantKill è false

        [Header("Magnetic Objects")]
        [SerializeField] private string magneticTag = "Magnetic";
        [SerializeField] private bool respawnMagneticObjects = true;

        private void Awake()
        {
            // Assicurati che il collider sia trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Controlla se è il player
            if (other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    if (instantKill)
                    {
                        Debug.Log($"Player ucciso da {gameObject.name}!");
                        playerHealth.InstantKill();
                    }
                    else
                    {
                        playerHealth.TakeDamage(damage);
                    }
                }
                return;
            }

            // Controlla se è un oggetto magnetico
            if (respawnMagneticObjects && other.CompareTag(magneticTag))
            {
                MagneticObject magObj = other.GetComponent<MagneticObject>();
                if (magObj != null)
                {
                    Debug.Log($"Oggetto magnetico {other.name} caduto nella zona letale, respawn!");
                    magObj.ForceRespawn();
                }
            }
        }
    }
}
