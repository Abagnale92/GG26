using UnityEngine;
using Player;

namespace Hazards
{
    /// <summary>
    /// Zona letale che uccide istantaneamente il player (lava, trappole, vuoto).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LethalZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool instantKill = true;
        [SerializeField] private float damage = 1f; // Usato solo se instantKill è false

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
            if (!other.CompareTag("Player")) return;

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
        }
    }
}
